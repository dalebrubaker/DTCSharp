using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using DTCCommon;
using DTCCommon.Codecs;
using DTCPB;
using Google.Protobuf;
using Timer = System.Timers.Timer;

namespace DTCServer
{
    public class ClientHandler : IDisposable
    {
        private readonly Action<ClientHandler, DTCMessageType, IMessage> _callback;
        private TcpClient _tcpClient;
        private bool _useHeartbeat;
        private bool _isDisposed;
        private Timer _timerHeartbeat;
        private readonly string _localEndPoint;
        private ICodecDTC _currentCodec;
        private NetworkStream _networkStream;
        private BinaryWriter _binaryWriter;
        private DateTime _lastHeartbeatReceivedTime;
        private readonly CancellationTokenSource _ctsRequestReader;
        private bool _isBinaryWriterZipped;
        private DeflateStream _deflateStream;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="callback">The callback to the DTC service implementation. Every request will be sent to the callback</param>
        /// <param name="tcpClient"></param>
        public ClientHandler(Action<ClientHandler, DTCMessageType, IMessage> callback, TcpClient tcpClient)
        {
            _callback = callback;
            _tcpClient = tcpClient;
            _currentCodec = new CodecBinary();
            RemoteEndPoint = tcpClient.Client.RemoteEndPoint.ToString();
            _localEndPoint = tcpClient.Client.LocalEndPoint.ToString();
            _ctsRequestReader = new CancellationTokenSource();
            _networkStream = _tcpClient.GetStream();
            _binaryWriter = new BinaryWriter(_networkStream);
            _isBinaryWriterZipped = false;

        }

        public string RemoteEndPoint { get; }

        private void TimerHeartbeatElapsed(object sender, ElapsedEventArgs e)
        {
            if (!_useHeartbeat || _isBinaryWriterZipped)
            {
                // Don't send a heartbeat if output is zipped
                return;
            }
            var maxWaitForHeartbeatTime = TimeSpan.FromMilliseconds(Math.Max(_timerHeartbeat.Interval * 2, 5000));
            var timeSinceHeartbeat = (DateTime.Now - _lastHeartbeatReceivedTime);
            if (timeSinceHeartbeat > maxWaitForHeartbeatTime)
            {
                Dispose();
            }

            // Send a heartbeat to the server
            var heartbeat = new Heartbeat();
            try
            {
                SendResponse(DTCMessageType.Heartbeat, heartbeat);
            }
            catch (IOException ex)
            {
                // perhaps the other side disconnected
                Dispose(true);
            }
        }

        /// <summary>
        /// This method runs "forever", reading requests and throwing them as events until the network stream is closed.
        /// All reads are done async on this thread.
        /// </summary>
        internal Task RequestReaderAsync()
        {
            var binaryReader = new BinaryReader(_networkStream); // Note that binaryReader may be redefined below in HistoricalPriceDataResponseHeader
            while (!_ctsRequestReader.Token.IsCancellationRequested)
            {
                try
                {
                    var size = binaryReader.ReadUInt16();
                    var messageType = (DTCMessageType)binaryReader.ReadUInt16();
#if DEBUG
                    if (messageType != DTCMessageType.Heartbeat)
                    {
                        var debug = 1;
                    }
                    DebugHelpers.AddRequestReceived(messageType, _currentCodec, false, size);
                    var port = ((IPEndPoint)_tcpClient?.Client.LocalEndPoint)?.Port;
                    if (port == 49998 && messageType == DTCMessageType.LogonRequest)
                    {
                        var debug2 = 1;
                        var requestsSent = DebugHelpers.RequestsSent;
                        var requestsReceived = DebugHelpers.RequestsReceived;
                        var responsesReceived = DebugHelpers.ResponsesReceived;
                        var responsesSent = DebugHelpers.ResponsesSent;
                    }
#endif
                    var bytes = binaryReader.ReadBytes(size - 4); // size included the header size+type
                    ProcessRequest(messageType, bytes);
                }
                catch (IOException ex)
                {
                    // Ignore this if it results from disconnected client or other socket error
                    if (!_ctsRequestReader.Token.IsCancellationRequested)
                    {
                        Dispose();
                    }
                }
                catch (Exception ex)
                {
                    var typeName = ex.GetType().Name;
                    throw;
                }
            }
            return Task.WhenAll(); // fake return
        }

        /// <summary>
        /// Process the message represented by bytes.
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="messageBytes"></param>
        private void ProcessRequest(DTCMessageType messageType, byte[] messageBytes)
        {
#if DEBUG
            var port = ((IPEndPoint)_tcpClient.Client.LocalEndPoint).Port;
            if (port == 49998)
            {
                var debug = 1;
            }
#endif
            switch (messageType)
            {
                case DTCMessageType.LogonRequest:
                    if (_timerHeartbeat != null)
                    {
                        DisposeTimerHeartbeat();
                    }
                    var logonRequest = _currentCodec.Load<LogonRequest>(messageType, messageBytes);
                    _useHeartbeat = logonRequest.HeartbeatIntervalInSeconds > 0;
                    if (_useHeartbeat)
                    {
                        // start the heartbeat
                        _timerHeartbeat = new Timer(logonRequest.HeartbeatIntervalInSeconds * 1000);
                        _timerHeartbeat.Elapsed += TimerHeartbeatElapsed;
                        _lastHeartbeatReceivedTime = DateTime.Now;
                        _timerHeartbeat.Start();
                    }
                    _callback(this, messageType, logonRequest);
                    break;
                case DTCMessageType.Heartbeat:
                    _lastHeartbeatReceivedTime = DateTime.Now;
                    var heartbeat = _currentCodec.Load<Heartbeat>(messageType, messageBytes);
                    _callback(this, messageType, heartbeat);
                    break;
                case DTCMessageType.Logoff:
                    if (_useHeartbeat && _timerHeartbeat != null)
                    {
                        // stop the heartbeat
                        DisposeTimerHeartbeat();
                    }
                    var logoff = _currentCodec.Load<Logoff>(messageType, messageBytes);
                    _callback(this, messageType, logoff);
                    break;
                case DTCMessageType.EncodingRequest:
                    // This is an exception where we don't make a callback. 
                    //     This requires an immediate response using BinaryEncoding then set the _currentCodec before another message can be processed
                    var encodingRequest = _currentCodec.Load<EncodingRequest>(messageType, messageBytes);
                    var newEncoding = EncodingEnum.BinaryEncoding;
                    switch (encodingRequest.Encoding)
                    {
                        case EncodingEnum.BinaryEncoding:
                            newEncoding = EncodingEnum.BinaryEncoding;
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
                    SendResponse(DTCMessageType.EncodingResponse, encodingResponse);

                    // BE SURE to set this immediately AFTER the SendResponse line above
                    SetCurrentCodec(encodingResponse.Encoding);

                    // send this to the callback for informational purposes
                    _callback(this, messageType, encodingRequest);
                    break;
                case DTCMessageType.MarketDataRequest:
                    var marketDataRequest = _currentCodec.Load<MarketDataRequest>(messageType, messageBytes);
                    _callback(this, messageType, marketDataRequest);
                    break;
                case DTCMessageType.MarketDepthRequest:
                    var marketDepthRequest = _currentCodec.Load<MarketDepthRequest>(messageType, messageBytes);
                    _callback(this, messageType, marketDepthRequest);
                    break;
                case DTCMessageType.SubmitNewSingleOrder:
                    var submitNewSingleOrder = _currentCodec.Load<SubmitNewSingleOrder>(messageType, messageBytes);
                    _callback(this, messageType, submitNewSingleOrder);
                    break;
                case DTCMessageType.SubmitNewSingleOrderInt:
                    var submitNewSingleOrderInt = _currentCodec.Load<SubmitNewSingleOrderInt>(messageType, messageBytes);
                    _callback(this, messageType, submitNewSingleOrderInt);
                    break;
                case DTCMessageType.SubmitNewOcoOrder:
                    var submitNewOcoOrder = _currentCodec.Load<SubmitNewOCOOrder>(messageType, messageBytes);
                    _callback(this, messageType, submitNewOcoOrder);
                    break;
                case DTCMessageType.SubmitNewOcoOrderInt:
                    var submitNewOcoOrderInt = _currentCodec.Load<SubmitNewOCOOrderInt>(messageType, messageBytes);
                    _callback(this, messageType, submitNewOcoOrderInt);
                    break;
                case DTCMessageType.CancelOrder:
                    var cancelOrder = _currentCodec.Load<CancelOrder>(messageType, messageBytes);
                    _callback(this, messageType, cancelOrder);
                    break;
                case DTCMessageType.CancelReplaceOrder:
                    var cancelReplaceOrder = _currentCodec.Load<CancelReplaceOrder>(messageType, messageBytes);
                    _callback(this, messageType, cancelReplaceOrder);
                    break;
                case DTCMessageType.CancelReplaceOrderInt:
                    var cancelReplaceOrderInt = _currentCodec.Load<CancelReplaceOrderInt>(messageType, messageBytes);
                    _callback(this, messageType, cancelReplaceOrderInt);
                    break;
                case DTCMessageType.OpenOrdersRequest:
                    var openOrdersRequest = _currentCodec.Load<OpenOrdersRequest>(messageType, messageBytes);
                    _callback(this, messageType, openOrdersRequest);
                    break;
                case DTCMessageType.HistoricalOrderFillsRequest:
                    var historicalOrderFillsRequest = _currentCodec.Load<HistoricalOrderFillsRequest>(messageType, messageBytes);
                    _callback(this, messageType, historicalOrderFillsRequest);
                    break;
                case DTCMessageType.CurrentPositionsRequest:
                    var currentPositionsRequest = _currentCodec.Load<CurrentPositionsRequest>(messageType, messageBytes);
                    _callback(this, messageType, currentPositionsRequest);
                    break;
                case DTCMessageType.TradeAccountsRequest:
                    var tradeAccountsRequest = _currentCodec.Load<TradeAccountsRequest>(messageType, messageBytes);
                    _callback(this, messageType, tradeAccountsRequest);
                    break;
                case DTCMessageType.ExchangeListRequest:
                    var exchangeListRequest = _currentCodec.Load<ExchangeListRequest>(messageType, messageBytes);
                    _callback(this, messageType, exchangeListRequest);
                    break;
                case DTCMessageType.SymbolsForExchangeRequest:
                    var symbolsForExchangeRequest = _currentCodec.Load<SymbolsForExchangeRequest>(messageType, messageBytes);
                    _callback(this, messageType, symbolsForExchangeRequest);
                    break;
                case DTCMessageType.UnderlyingSymbolsForExchangeRequest:
                    var underlyingSymbolsForExchangeRequest = _currentCodec.Load<UnderlyingSymbolsForExchangeRequest>(messageType, messageBytes);
                    _callback(this, messageType, underlyingSymbolsForExchangeRequest);
                    break;
                case DTCMessageType.SymbolsForUnderlyingRequest:
                    var symbolsForUnderlyingRequest = _currentCodec.Load<SymbolsForUnderlyingRequest>(messageType, messageBytes);
                    _callback(this, messageType, symbolsForUnderlyingRequest);
                    break;
                case DTCMessageType.SecurityDefinitionForSymbolRequest:
                    var securityDefinitionForSymbolRequest = _currentCodec.Load<SecurityDefinitionForSymbolRequest>(messageType, messageBytes);
                    _callback(this, messageType, securityDefinitionForSymbolRequest);
                    break;
                case DTCMessageType.SymbolSearchRequest:
                    var symbolSearchRequest = _currentCodec.Load<SymbolSearchRequest>(messageType, messageBytes);
                    _callback(this, messageType, symbolSearchRequest);
                    break;
                case DTCMessageType.AccountBalanceRequest:
                    var accountBalanceRequest = _currentCodec.Load<AccountBalanceRequest>(messageType, messageBytes);
                    _callback(this, messageType, accountBalanceRequest);
                    break;
                case DTCMessageType.HistoricalPriceDataRequest:
                    var historicalPriceDataRequest = _currentCodec.Load<HistoricalPriceDataRequest>(messageType, messageBytes);
                    _callback(this, messageType, historicalPriceDataRequest);
                    break;
                case DTCMessageType.MessageTypeUnset:
                case DTCMessageType.LogonResponse:
                case DTCMessageType.EncodingResponse:
                case DTCMessageType.MarketDataReject:
                case DTCMessageType.MarketDataSnapshot:
                case DTCMessageType.MarketDataSnapshotInt:
                case DTCMessageType.MarketDataUpdateTrade:
                case DTCMessageType.MarketDataUpdateTradeCompact:
                case DTCMessageType.MarketDataUpdateTradeInt:
                case DTCMessageType.MarketDataUpdateLastTradeSnapshot:
                case DTCMessageType.MarketDataUpdateBidAsk:
                case DTCMessageType.MarketDataUpdateBidAskCompact:
                case DTCMessageType.MarketDataUpdateBidAskInt:
                case DTCMessageType.MarketDataUpdateSessionOpen:
                case DTCMessageType.MarketDataUpdateSessionOpenInt:
                case DTCMessageType.MarketDataUpdateSessionHigh:
                case DTCMessageType.MarketDataUpdateSessionHighInt:
                case DTCMessageType.MarketDataUpdateSessionLow:
                case DTCMessageType.MarketDataUpdateSessionLowInt:
                case DTCMessageType.MarketDataUpdateSessionVolume:
                case DTCMessageType.MarketDataUpdateOpenInterest:
                case DTCMessageType.MarketDataUpdateSessionSettlement:
                case DTCMessageType.MarketDataUpdateSessionSettlementInt:
                case DTCMessageType.MarketDataUpdateSessionNumTrades:
                case DTCMessageType.MarketDataUpdateTradingSessionDate:
                case DTCMessageType.MarketDepthReject:
                case DTCMessageType.MarketDepthSnapshotLevel:
                case DTCMessageType.MarketDepthSnapshotLevelInt:
                case DTCMessageType.MarketDepthUpdateLevel:
                case DTCMessageType.MarketDepthUpdateLevelCompact:
                case DTCMessageType.MarketDepthUpdateLevelInt:
                case DTCMessageType.MarketDepthFullUpdate10:
                case DTCMessageType.MarketDepthFullUpdate20:
                case DTCMessageType.MarketDataFeedStatus:
                case DTCMessageType.MarketDataFeedSymbolStatus:
                case DTCMessageType.OpenOrdersReject:
                case DTCMessageType.OrderUpdate:
                case DTCMessageType.HistoricalOrderFillResponse:
                case DTCMessageType.CurrentPositionsReject:
                case DTCMessageType.PositionUpdate:
                case DTCMessageType.TradeAccountResponse:
                case DTCMessageType.ExchangeListResponse:
                case DTCMessageType.SecurityDefinitionResponse:
                case DTCMessageType.SecurityDefinitionReject:
                case DTCMessageType.AccountBalanceReject:
                case DTCMessageType.AccountBalanceUpdate:
                case DTCMessageType.UserMessage:
                case DTCMessageType.GeneralLogMessage:
                case DTCMessageType.HistoricalPriceDataResponseHeader:
                case DTCMessageType.HistoricalPriceDataReject:
                case DTCMessageType.HistoricalPriceDataRecordResponse:
                case DTCMessageType.HistoricalPriceDataTickRecordResponse:
                case DTCMessageType.HistoricalPriceDataRecordResponseInt:
                case DTCMessageType.HistoricalPriceDataTickRecordResponseInt:
                default:
                    throw new ArgumentOutOfRangeException($"Unexpected MessageType {messageType} received by {this} {nameof(ProcessRequest)}.");
            }
        }

        private void DisposeTimerHeartbeat()
        {
            if (_timerHeartbeat != null)
            {
                _timerHeartbeat.Elapsed -= TimerHeartbeatElapsed;
                _timerHeartbeat?.Stop();
                _timerHeartbeat?.Dispose();
                _timerHeartbeat = null;
            }
        }

        /// <summary>
        /// Write the response. If thenSwitchToZipped, then switch the write stream to zlib format
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageType"></param>
        /// <param name="message"></param>
        /// <param name="thenSwitchToZipped"></param>
        public void SendResponse<T>(DTCMessageType messageType, T message, bool thenSwitchToZipped = false) where T : IMessage
        {
#if DEBUG
            if (messageType != DTCMessageType.Heartbeat)
            {
                var debug = 1;
            }
            else
            {
                var debug3 = 1;
            }
            DebugHelpers.AddResponseSent(messageType, _currentCodec, _isBinaryWriterZipped);;
#endif
            try
            {
#if DEBUG
                var port = ((IPEndPoint)_tcpClient?.Client.LocalEndPoint)?.Port;
                if (port == 49998 && messageType == DTCMessageType.LogonResponse)
                {
                    var debug2 = 1;
                    var requestsSent = DebugHelpers.RequestsSent;
                    var requestsReceived = DebugHelpers.RequestsReceived;
                    var responsesReceived = DebugHelpers.ResponsesReceived;
                    var responsesSent = DebugHelpers.ResponsesSent;
                }
#endif
                _currentCodec.Write(messageType, message, _binaryWriter);
                if (thenSwitchToZipped)
                {
                    // Switch to writing zipped
                    // Write the 2-byte header that Sierra Chart has coming from ZLib. See https://tools.ietf.org/html/rfc1950
                    _binaryWriter.Write((byte)120); // zlibCmf 120 = 0111 1000 means Deflate 
                    _binaryWriter.Write((byte)156); // zlibFlg 156 = 1001 1100

                    _deflateStream = new DeflateStream(_networkStream, CompressionMode.Compress, true);
                    _deflateStream.Flush();
                    _binaryWriter = new BinaryWriter(_deflateStream);
                    _isBinaryWriterZipped = true;
                    _useHeartbeat = false;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Do this when you are done writing compressed data.
        /// And you can't write any future info to this ClientHandler
        /// </summary>
        public void EndZippedWriting()
        {
            _deflateStream.Close();
            _deflateStream?.Dispose();
            _deflateStream = null;
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
                _ctsRequestReader.Cancel();
                DisposeTimerHeartbeat();
                _networkStream?.Close();
                _networkStream?.Dispose();
                _networkStream = null;
                _binaryWriter?.Dispose();
                _binaryWriter = null;
                _tcpClient?.Close();
                _tcpClient = null;
                _isDisposed = true;
            }
        }

        public override string ToString()
        {
            return $"Remote:{RemoteEndPoint}";
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
