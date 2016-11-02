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
        private readonly Action<ClientHandler, DTCMessageType, IMessage> _callback;
        private readonly TcpClient _tcpClient;
        private readonly bool _useHeartbeat;
        private bool _isDisposed;
        private Timer _heartbeatTimer;
        private readonly string _localEndPoint;
        private ICodecDTC _currentCodec;
        private NetworkStream _networkStream;
        private BinaryWriter _binaryWriter;
        private DateTime _lastHeartbeatReceivedTime;
        private TaskScheduler _taskSchedulerCurrContext;
        private SynchronizationContext _synchronizationContext;

        public ClientHandler(Action<ClientHandler, DTCMessageType, IMessage> callback, TcpClient tcpClient, bool useHeartbeat)
        {
            _callback = callback;
            _tcpClient = tcpClient;
            _useHeartbeat = useHeartbeat;
            _currentCodec = new CodecBinary();
            RemoteEndPoint = tcpClient.Client.RemoteEndPoint.ToString();
            _localEndPoint = tcpClient.Client.LocalEndPoint.ToString();
        }


        public string RemoteEndPoint { get; }

        private void HeartbeatTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!_useHeartbeat)
            {
                return;
            }
            var maxWaitForHeartbeatTime = TimeSpan.FromMilliseconds(Math.Max(_heartbeatTimer.Interval * 2, 5000));
            var timeSinceHeartbeat = (DateTime.Now - _lastHeartbeatReceivedTime);
            if (timeSinceHeartbeat > maxWaitForHeartbeatTime)
            {
                Dispose(true);
                throw new DTCSharpException($"Too long since Server sent us a heartbeat. Closing clientHandler: {this}");
            }

            // Send a heartbeat to the server
            var heartbeat = new Heartbeat();
            SendMessage(DTCMessageType.Heartbeat, heartbeat);
        }

        /// <summary>
        /// This method runs "forever".
        /// All reads and writes are done on this thread.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            _taskSchedulerCurrContext = TaskScheduler.FromCurrentSynchronizationContext();
            _synchronizationContext = SynchronizationContext.Current;
            _networkStream = _tcpClient.GetStream();
            _binaryWriter = new BinaryWriter(_networkStream);
            var binaryReader = new BinaryReader(_networkStream); // Note that binaryReader may be redefined below in HistoricalPriceDataResponseHeader
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!_networkStream.DataAvailable)
                {
                    await Task.Delay(1, cancellationToken).ConfigureAwait(true);
                    continue;
                }

                // Read the header
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
                            _heartbeatTimer = new Timer(logonRequest.HeartbeatIntervalInSeconds * 1000);
                            _heartbeatTimer.Elapsed += HeartbeatTimer_Elapsed;
                            _lastHeartbeatReceivedTime = DateTime.Now;
                            _heartbeatTimer.Start();
                        }
                        _callback(this, messageType, logonRequest);
                        break;
                    case DTCMessageType.Heartbeat:
                        _lastHeartbeatReceivedTime = DateTime.Now;
                        var heartbeat = _currentCodec.Load<Heartbeat>(messageType, bytes);
                        _callback(this, messageType, heartbeat);
                        break;
                    case DTCMessageType.Logoff:
                        if (_useHeartbeat && _heartbeatTimer != null)
                        {
                            // stop the heartbeat
                            _heartbeatTimer.Elapsed -= HeartbeatTimer_Elapsed;
                            _heartbeatTimer.Stop();
                        }
                        var logoff = _currentCodec.Load<Logoff>(messageType, bytes);
                        _callback(this, messageType, logoff);
                        break;
                    case DTCMessageType.EncodingRequest:
                        var encodingRequest = _currentCodec.Load<EncodingRequest>(messageType, bytes);
                        _callback(this, messageType, encodingRequest);
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
        /// Send the message. It will always be posted to the current synchronization context
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="message"></param>
        public void SendMessage<T>(DTCMessageType messageType, T message) where T : IMessage
        {
            try
            {
                _synchronizationContext.Post(s => _currentCodec.Write(messageType, message, _binaryWriter), null);
            }
            catch (Exception ex)
            {
                throw new DTCSharpException(ex.Message);
            }
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
                _heartbeatTimer?.Dispose();
                _networkStream?.Dispose();
                _binaryWriter?.Dispose();
                _tcpClient.Close();
                _isDisposed = true;
            }
        }

        public override string ToString()
        {
            return $"Local:{_localEndPoint} Remote:{RemoteEndPoint}";
        }

        /// <summary>
        /// This must be called whenever the encoding changes, right AFTER the EncodingResponse is sent to the client
        /// </summary>
        /// <param name="encoding"></param>
        public void SetCurrentCodec(EncodingEnum encoding)
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
