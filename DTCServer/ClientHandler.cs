using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Timers;
using DTCCommon;
using DTCCommon.Codecs;
using DTCCommon.Enums;
using DTCPB;
using Google.Protobuf;
using NLog;
using Timer = System.Timers.Timer;

namespace DTCServer
{
    /// <summary>
    /// A ClientHandler is an instance created by Server to communicate with each connected client
    /// </summary>
    public sealed class ClientHandler : IDisposable
    {
        private static readonly ILogger s_logger = LogManager.GetCurrentClassLogger();

        private readonly Action<ClientHandler, DTCMessageType, IMessage> _callback;
        private TcpClient _tcpClient;
        private bool _useHeartbeat;
        private bool _isDisposed;
        private Timer _timerHeartbeat;
        private Codec _currentCodec;
        private readonly NetworkStream _networkStreamServer;
        private DateTime _lastHeartbeatReceivedTime;
        private readonly CancellationTokenSource _ctsRequestReader;
        private bool _isConnected;

        /// <summary>
        /// Create instance of ClientHandler
        /// </summary>
        /// <param name="callback">The callback to the DTC service implementation. Every request will be sent to the callback</param>
        /// <param name="tcpClient"></param>
        /// <param name="server">back-pointer for the server owning this ClientHandler</param>
        public ClientHandler(Action<ClientHandler, DTCMessageType, IMessage> callback, TcpClient tcpClient)
        {
            _callback = callback;
            _tcpClient = tcpClient;
            RemoteEndPoint = tcpClient.Client.RemoteEndPoint.ToString();
            _ctsRequestReader = new CancellationTokenSource();
            _networkStreamServer = _tcpClient.GetStream();
            _currentCodec = new CodecBinary(_networkStreamServer, ClientOrServer.Server);
        }

        public string RemoteEndPoint { get; }

        public bool IsConnected => _isConnected;

        public event EventHandler<string> Connected;

        internal void OnConnected(string message)
        {
            _isConnected = true;
            var temp = Connected;
            temp?.Invoke(this, message);
        }

        public event EventHandler<Error> Disconnected;

        internal void OnDisconnected(Error error)
        {
            if (!_isConnected)
            {
                return;
            }
            _isConnected = false;
            var temp = Disconnected;
            temp?.Invoke(this, error);
        }

        private void TimerHeartbeatElapsed(object sender, ElapsedEventArgs e)
        {
            if (!_useHeartbeat)
            {
                // Don't send a heartbeat if output is zipped
                return;
            }
            var maxWaitForHeartbeatTime = TimeSpan.FromMilliseconds(Math.Max(_timerHeartbeat.Interval * 2, 5000));
            var timeSinceHeartbeat = (DateTime.Now - _lastHeartbeatReceivedTime);
            if (timeSinceHeartbeat > maxWaitForHeartbeatTime)
            {
                OnDisconnected(new Error("Disconnecting because late heartbeat."));
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
                OnDisconnected(new Error($"Disconnecting because {ex.Message}."));
                Dispose();
            }
        }

        /// <summary>
        /// This method runs "forever", reading requests and throwing them as events until the network stream is closed.
        /// All reads are done async on this thread.
        /// </summary>
        internal void RequestReaderLoop()
        {
            var binaryReader = new BinaryReader(_networkStreamServer); // Note that binaryReader may be redefined below in HistoricalPriceDataResponseHeader
            while (!_ctsRequestReader.Token.IsCancellationRequested)
            {
                try
                {
                    //if (!stream.DataAvailable)
                    //{
                    //    await Task.Delay(1).ConfigureAwait(false);
                    //    continue;
                    //}
                    s_logger.Debug($"Waiting in {nameof(ClientHandler)}.{nameof(RequestReaderLoop)} to read a message");
                    var (messageType, messageBytes) = _currentCodec.ReadMessage();
                    ProcessClientRequest(messageType, messageBytes, ref binaryReader);
                    //await Task.Delay(1).ConfigureAwait(false);
                }
#pragma warning disable 168
                catch (IOException ex)
#pragma warning restore 168
                {
                    // Ignore this if it results from disconnected client or other socket error
                    if (_ctsRequestReader.Token.IsCancellationRequested)
                    {
                        OnDisconnected(new Error("Disconnecting because cancellation is requested."));
                        Dispose();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    var typeName = ex.GetType().Name;
                    throw;
                }
            }
        }

        /// <summary>
        /// Process the request received from a client
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="messageBytes"></param>
        /// <param name="binaryReader"></param>
        private void ProcessClientRequest(DTCMessageType messageType, byte[] messageBytes, ref BinaryReader binaryReader)
        {
//#if DEBUG
//            var port = ((IPEndPoint)_tcpClient.Client.LocalEndPoint).Port;
//            if (port == 49998)
//            {
//#pragma warning disable 219
//                var debug = 1;
//#pragma warning restore 219
//            }
//#endif
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
                    SendResponse(DTCMessageType.Heartbeat, heartbeat);
                    _callback(this, messageType, heartbeat); // send this to the callback for informational purposes
                    break;
                case DTCMessageType.Logoff:
                    var logoffRequest = _currentCodec.Load<Logoff>(messageType, messageBytes);
                    if (_useHeartbeat && _timerHeartbeat != null)
                    {
                        // stop the heartbeat
                        DisposeTimerHeartbeat();
                    }
                    Dispose();
                    _callback(this, messageType, logoffRequest); // send this to the callback for informational purposes
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

                    _callback(this, messageType, encodingRequest); // send this to the callback for informational purposes
                    OnConnected("Handler connected");
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
                case DTCMessageType.MarketDataUpdateTradeWithUnbundledIndicator:
                case DTCMessageType.MarketDataUpdateTradeWithUnbundledIndicator2:
                case DTCMessageType.MarketDataUpdateTradeNoTimestamp:
                case DTCMessageType.MarketDataUpdateBidAskNoTimestamp:
                case DTCMessageType.MarketDepthSnapshotLevelFloat:
                case DTCMessageType.MarketDepthUpdateLevelFloatWithMilliseconds:
                case DTCMessageType.MarketDepthUpdateLevelNoTimestamp:
                case DTCMessageType.TradingSymbolStatus:
                case DTCMessageType.SubmitFlattenPositionOrder:
                case DTCMessageType.HistoricalOrderFillsReject:
                case DTCMessageType.AccountBalanceAdjustment:
                case DTCMessageType.AccountBalanceAdjustmentReject:
                case DTCMessageType.AccountBalanceAdjustmentComplete:
                case DTCMessageType.HistoricalAccountBalancesRequest:
                case DTCMessageType.HistoricalAccountBalancesReject:
                case DTCMessageType.HistoricalAccountBalanceResponse:
                case DTCMessageType.AlertMessage:
                case DTCMessageType.JournalEntryAdd:
                case DTCMessageType.JournalEntriesRequest:
                case DTCMessageType.JournalEntriesReject:
                case DTCMessageType.JournalEntryResponse:
                case DTCMessageType.HistoricalPriceDataResponseTrailer:
                case DTCMessageType.HistoricalMarketDepthDataRequest:
                case DTCMessageType.HistoricalMarketDepthDataReject:
                    break;
                case DTCMessageType.HistoricalMarketDepthDataResponseHeader:
                case DTCMessageType.HistoricalMarketDepthDataRecordResponse:
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
                case DTCMessageType.MarketDepthUpdateLevelInt:
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
                    throw new ArgumentOutOfRangeException($"Unexpected MessageType {messageType} received by {this} {nameof(ProcessClientRequest)}.");
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
        public void SendResponse<T>(DTCMessageType messageType, T message) where T : IMessage
        {
#if DEBUG
//            if (messageType != DTCMessageType.Heartbeat)
//            {
//#pragma warning disable 219
//                var debug = 1;
//#pragma warning restore 219
//            }
//            else
//            {
//#pragma warning disable 219
//                var debug3 = 1;
//#pragma warning restore 219
//            }

            DebugHelpers.AddResponseSent(messageType, _currentCodec);
#endif
            _currentCodec.Write(messageType, message);
            if (messageType == DTCMessageType.HistoricalPriceDataResponseHeader)
            {
                var historicalPriceDataResponseHeader = message as HistoricalPriceDataResponseHeader;
                if (historicalPriceDataResponseHeader.UseZLibCompression != 0)
                {
                    // Switch to writing zipped
                    s_logger.Debug($"{nameof(ClientHandler)}.{nameof(SendResponse)} is switching server stream to write zipped.");
                    _currentCodec.WriteSwitchToZipped();
                    _useHeartbeat = false;
                }
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                OnDisconnected(new Error("Disposed"));
                _ctsRequestReader.Cancel();
                _currentCodec.Close();
                DisposeTimerHeartbeat();
                _tcpClient?.Close();
                _tcpClient = null;
                _isDisposed = true;
            }
            GC.SuppressFinalize(this);
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
                    _currentCodec = new CodecBinary(_networkStreamServer, ClientOrServer.Server);
                    break;
                case EncodingEnum.BinaryWithVariableLengthStrings:
                case EncodingEnum.JsonEncoding:
                case EncodingEnum.JsonCompactEncoding:
                    throw new NotImplementedException($"Not implemented in {nameof(ClientHandler)}: {nameof(encoding)}");
                case EncodingEnum.ProtocolBuffers:
                    _currentCodec = new CodecProtobuf(_networkStreamServer, ClientOrServer.Server);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(encoding), encoding, null);
            }
        }
    }
}