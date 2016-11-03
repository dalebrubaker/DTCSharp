using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using DTCCommon;
using DTCCommon.Codecs;
using DTCCommon.Exceptions;
using DTCPB;
using Google.Protobuf;
using Timer = System.Timers.Timer;

namespace DTCServer
{
    public class ClientHandler : IDisposable
    {
        private readonly Func<ClientHandler, DTCMessageType, IMessage, Task> _callback;
        private TcpClient _tcpClient;
        private bool _useHeartbeat;
        private readonly int _timeoutNoActivity;
        private bool _isDisposed;
        private Timer _timerHeartbeat;
        private readonly Timer _timerNoActivity;
        private readonly string _localEndPoint;
        private ICodecDTC _currentCodec;
        private NetworkStream _networkStream;
        private BinaryWriter _binaryWriter;
        private DateTime _lastHeartbeatReceivedTime;
        private DateTime _lastActivityTime;
        private TaskScheduler _taskSchedulerCurrContext;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="callback">The callback to the DTC service implementation. Every request will be sent to the callback</param>
        /// <param name="tcpClient"></param>
        /// <param name="timeoutNoActivity">milliseconds timeout to assume disconnected if no activity</param>
        public ClientHandler(Func<ClientHandler, DTCMessageType, IMessage, Task> callback, TcpClient tcpClient, int timeoutNoActivity)
        {
            _callback = callback;
            _tcpClient = tcpClient;
            _timeoutNoActivity = timeoutNoActivity;
            _currentCodec = new CodecBinary();
            RemoteEndPoint = tcpClient.Client.RemoteEndPoint.ToString();
            _localEndPoint = tcpClient.Client.LocalEndPoint.ToString();
            _timerNoActivity = new Timer(timeoutNoActivity);
            _timerNoActivity.Elapsed += TimerNoActivity_Elapsed;
            _timerNoActivity.Start();

            if (SynchronizationContext.Current != null)
            {
                _taskSchedulerCurrContext = TaskScheduler.FromCurrentSynchronizationContext();
            }
            else
            {
                // If there is no SyncContext for this thread (e.g. we are in a unit test
                // or console scenario instead of running in an app), then just use the
                // default scheduler because there is no UI thread to sync with.
                _taskSchedulerCurrContext = TaskScheduler.Current;
            }
        }

        private void TimerNoActivity_Elapsed(object sender, ElapsedEventArgs e)
        {
            if ((DateTime.Now - _lastActivityTime).TotalMilliseconds > _timeoutNoActivity)
            {
                Dispose();
            }
        }

        public string RemoteEndPoint { get; }

        private void TimerHeartbeatElapsed(object sender, ElapsedEventArgs e)
        {
            if (!_useHeartbeat)
            {
                return;
            }
            var maxWaitForHeartbeatTime = TimeSpan.FromMilliseconds(Math.Max(_timerHeartbeat.Interval * 2, 5000));
            var timeSinceHeartbeat = (DateTime.Now - _lastHeartbeatReceivedTime);
            if (timeSinceHeartbeat > maxWaitForHeartbeatTime)
            {
                Dispose(true);
                throw new DTCSharpException($"Too long since Server sent us a heartbeat. Closing clientHandler: {this}");
            }

            // Send a heartbeat to the server
            var heartbeat = new Heartbeat();
            SendResponseAsync(DTCMessageType.Heartbeat, heartbeat);
        }

        /// <summary>
        /// This method runs "forever", reading requests and making callbacks to the _callback service.
        /// All reads and writes are done on this thread.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            _taskSchedulerCurrContext = TaskScheduler.FromCurrentSynchronizationContext();
            _networkStream = _tcpClient.GetStream();
            _binaryWriter = new BinaryWriter(_networkStream);
            var binaryReader = new BinaryReader(_networkStream); // Note that binaryReader may be redefined below in HistoricalPriceDataResponseHeader
            while (!cancellationToken.IsCancellationRequested && _networkStream != null)
            {
                if (!_networkStream.DataAvailable)
                {
                    await Task.Delay(1, cancellationToken).ConfigureAwait(true);
                    continue;
                }

                // Read the header
                _lastActivityTime = DateTime.Now;
                var size = binaryReader.ReadUInt16();
                var messageType = (DTCMessageType)binaryReader.ReadUInt16();
#if DEBUG
                if (messageType != DTCMessageType.Heartbeat)
                {
                    var debug = 1;
                }
#endif
                var bytes = binaryReader.ReadBytes(size - 4); // size included the header size+type
                switch (messageType)
                {
                    case DTCMessageType.LogonRequest:
                        var logonRequest = _currentCodec.Load<LogonRequest>(messageType, bytes);
                        if (_useHeartbeat)
                        {
                            // start the heartbeat
                            _timerHeartbeat = new Timer(logonRequest.HeartbeatIntervalInSeconds * 1000);
                            _timerHeartbeat.Elapsed += TimerHeartbeatElapsed;
                            _lastHeartbeatReceivedTime = DateTime.Now;
                            _timerHeartbeat.Start();
                        }
                        await _callback(this, messageType, logonRequest).ConfigureAwait(true);
                        break;
                    case DTCMessageType.Heartbeat:
                        _lastHeartbeatReceivedTime = DateTime.Now;
                        var heartbeat = _currentCodec.Load<Heartbeat>(messageType, bytes);
                        await _callback(this, messageType, heartbeat).ConfigureAwait(true);
                        break;
                    case DTCMessageType.Logoff:
                        if (_useHeartbeat && _timerHeartbeat != null)
                        {
                            // stop the heartbeat
                            _timerHeartbeat.Elapsed -= TimerHeartbeatElapsed;
                            _timerHeartbeat.Stop();
                        }
                        var logoff = _currentCodec.Load<Logoff>(messageType, bytes);
                        await _callback(this, messageType, logoff).ConfigureAwait(true);
                        break;
                    case DTCMessageType.EncodingRequest:
                        // This is an exception where we don't make a callback. 
                        //     This requires an immediate response using BinaryEncoding then set the _currentCodec before another message can be processed
                        var encodingRequest = _currentCodec.Load<EncodingRequest>(messageType, bytes);
                        var newEncoding = EncodingEnum.BinaryEncoding;
                        switch (encodingRequest.Encoding)
                        {
                            case EncodingEnum.BinaryEncoding:
                                break;
                            case EncodingEnum.BinaryWithVariableLengthStrings:
                            case EncodingEnum.JsonEncoding:
                            case EncodingEnum.JsonCompactEncoding:
                                // not supported. Ignore
                                break;
                            case EncodingEnum.ProtocolBuffers:
                                newEncoding = EncodingEnum.ProtocolBuffers;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        var encodingResponse = new EncodingResponse
                        {
                            ProtocolType = encodingRequest.ProtocolType,
                            ProtocolVersion = encodingRequest.ProtocolVersion,
                            Encoding = newEncoding
                        };
                        await SendResponseAsync(DTCMessageType.EncodingResponse, encodingResponse).ConfigureAwait(true);

                        // BE SURE to set this immediately AFTER the SendResponse line above
                        SetCurrentCodec(encodingResponse.Encoding);

                        // send this to the callback for informational purposes
                        await _callback(this, DTCMessageType.EncodingRequest, encodingRequest).ConfigureAwait(true);
                        break;
                    case DTCMessageType.MarketDataRequest:
                        break;
                    case DTCMessageType.MarketDataReject:
                        break;
                    case DTCMessageType.MarketDataSnapshot:
                        break;
                    case DTCMessageType.MarketDataSnapshotInt:
                        break;
                    case DTCMessageType.MarketDataUpdateTrade:
                        break;
                    case DTCMessageType.MarketDataUpdateTradeCompact:
                        break;
                    case DTCMessageType.MarketDataUpdateTradeInt:
                        break;
                    case DTCMessageType.MarketDataUpdateLastTradeSnapshot:
                        break;
                    case DTCMessageType.MarketDataUpdateBidAsk:
                        break;
                    case DTCMessageType.MarketDataUpdateBidAskCompact:
                        break;
                    case DTCMessageType.MarketDataUpdateBidAskInt:
                        break;
                    case DTCMessageType.MarketDataUpdateSessionOpen:
                        break;
                    case DTCMessageType.MarketDataUpdateSessionOpenInt:
                        break;
                    case DTCMessageType.MarketDataUpdateSessionHigh:
                        break;
                    case DTCMessageType.MarketDataUpdateSessionHighInt:
                        break;
                    case DTCMessageType.MarketDataUpdateSessionLow:
                        break;
                    case DTCMessageType.MarketDataUpdateSessionLowInt:
                        break;
                    case DTCMessageType.MarketDataUpdateSessionVolume:
                        break;
                    case DTCMessageType.MarketDataUpdateOpenInterest:
                        break;
                    case DTCMessageType.MarketDataUpdateSessionSettlement:
                        break;
                    case DTCMessageType.MarketDataUpdateSessionSettlementInt:
                        break;
                    case DTCMessageType.MarketDataUpdateSessionNumTrades:
                        break;
                    case DTCMessageType.MarketDataUpdateTradingSessionDate:
                        break;
                    case DTCMessageType.MarketDepthRequest:
                        break;
                    case DTCMessageType.MarketDepthReject:
                        break;
                    case DTCMessageType.MarketDepthSnapshotLevel:
                        break;
                    case DTCMessageType.MarketDepthSnapshotLevelInt:
                        break;
                    case DTCMessageType.MarketDepthUpdateLevel:
                        break;
                    case DTCMessageType.MarketDepthUpdateLevelCompact:
                        break;
                    case DTCMessageType.MarketDepthUpdateLevelInt:
                        break;
                    case DTCMessageType.MarketDepthFullUpdate10:
                        break;
                    case DTCMessageType.MarketDepthFullUpdate20:
                        break;
                    case DTCMessageType.MarketDataFeedStatus:
                        break;
                    case DTCMessageType.MarketDataFeedSymbolStatus:
                        break;
                    case DTCMessageType.SubmitNewSingleOrder:
                        break;
                    case DTCMessageType.SubmitNewSingleOrderInt:
                        break;
                    case DTCMessageType.SubmitNewOcoOrder:
                        break;
                    case DTCMessageType.SubmitNewOcoOrderInt:
                        break;
                    case DTCMessageType.CancelOrder:
                        break;
                    case DTCMessageType.CancelReplaceOrder:
                        break;
                    case DTCMessageType.CancelReplaceOrderInt:
                        break;
                    case DTCMessageType.OpenOrdersRequest:
                        break;
                    case DTCMessageType.OpenOrdersReject:
                        break;
                    case DTCMessageType.OrderUpdate:
                        break;
                    case DTCMessageType.HistoricalOrderFillsRequest:
                        break;
                    case DTCMessageType.HistoricalOrderFillResponse:
                        break;
                    case DTCMessageType.CurrentPositionsRequest:
                        break;
                    case DTCMessageType.CurrentPositionsReject:
                        break;
                    case DTCMessageType.PositionUpdate:
                        break;
                    case DTCMessageType.TradeAccountsRequest:
                        break;
                    case DTCMessageType.TradeAccountResponse:
                        break;
                    case DTCMessageType.ExchangeListRequest:
                        break;
                    case DTCMessageType.ExchangeListResponse:
                        break;
                    case DTCMessageType.SymbolsForExchangeRequest:
                        break;
                    case DTCMessageType.UnderlyingSymbolsForExchangeRequest:
                        break;
                    case DTCMessageType.SymbolsForUnderlyingRequest:
                        break;
                    case DTCMessageType.SecurityDefinitionForSymbolRequest:
                        break;
                    case DTCMessageType.SecurityDefinitionResponse:
                        break;
                    case DTCMessageType.SymbolSearchRequest:
                        break;
                    case DTCMessageType.SecurityDefinitionReject:
                        break;
                    case DTCMessageType.AccountBalanceRequest:
                        break;
                    case DTCMessageType.AccountBalanceReject:
                        break;
                    case DTCMessageType.AccountBalanceUpdate:
                        break;
                    case DTCMessageType.UserMessage:
                        break;
                    case DTCMessageType.GeneralLogMessage:
                        break;
                    case DTCMessageType.HistoricalPriceDataRequest:
                        break;
                    case DTCMessageType.HistoricalPriceDataResponseHeader:
                        break;
                    case DTCMessageType.HistoricalPriceDataReject:
                        break;
                    case DTCMessageType.HistoricalPriceDataRecordResponse:
                        break;
                    case DTCMessageType.HistoricalPriceDataTickRecordResponse:
                        break;
                    case DTCMessageType.HistoricalPriceDataRecordResponseInt:
                        break;
                    case DTCMessageType.HistoricalPriceDataTickRecordResponseInt:
                        break;
                    case DTCMessageType.MessageTypeUnset:
                    case DTCMessageType.LogonResponse:
                    case DTCMessageType.EncodingResponse:
                    default:
                        throw new ArgumentOutOfRangeException($"Unexpected MessageType {messageType} received by {this} {nameof(RunAsync)}.");
                }
            }
        }


        /// <summary>
        /// Send the message on the _taskSchedulerCurrContext.
        /// So you can call this from any thread, and the message will be marshalled to the thread for this class for writing.
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="message"></param>
        public Task SendResponseAsync<T>(DTCMessageType messageType, T message) where T : IMessage
        {
            var task = new Task(() => SendResponse(messageType, message));
            task.RunSynchronously(_taskSchedulerCurrContext);
            return Task.WhenAll(); // return completed task
        }

        public void SendResponse<T>(DTCMessageType messageType, T message) where T : IMessage
        {
            _currentCodec.Write(messageType, message, _binaryWriter);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_isDisposed)
            {
                _timerHeartbeat?.Dispose();
                _timerNoActivity.Dispose();
                _networkStream?.Close();
                _networkStream?.Dispose();
                _networkStream = null;
                _binaryWriter?.Dispose();
                _binaryWriter = null;
                _tcpClient.Close();
                _tcpClient = null;
                _isDisposed = true;
            }
        }

        public override string ToString()
        {
            return $"Local:{_localEndPoint} Remote:{RemoteEndPoint}";
        }

        /// <summary>
        /// This must be called whenever the encoding changes, immediately AFTER the EncodingResponse is sent to the client
        /// </summary>
        /// <param name="encoding"></param>
        private void SetCurrentCodec(EncodingEnum encoding)
        {
            switch (encoding)
            {
                case EncodingEnum.BinaryEncoding:
                    _currentCodec = new CodecBinary();
                    break;
                case EncodingEnum.BinaryWithVariableLengthStrings:
                case EncodingEnum.JsonEncoding:
                case EncodingEnum.JsonCompactEncoding:
                    throw new NotImplementedException($"Not implemented in {nameof(ClientHandler)}: {nameof(encoding)}");
                case EncodingEnum.ProtocolBuffers:
                    _currentCodec = new CodecProtobuf();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(encoding), encoding, null);
            }
        }
    }
}
