using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using DTCCommon;
using DTCCommon.Codecs;
using DTCCommon.Enums;
using DTCPB;
using Google.Protobuf;
using NLog;
using Timer = System.Timers.Timer;

namespace DTCClient
{
    public class ClientBase : IDisposable
    {
        protected static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        protected readonly int TimeoutNoActivity;
        private Timer _timerHeartbeat;
        private bool _isDisposed;
        private TcpClient _tcpClient;
        private DateTime _lastHeartbeatReceivedTime;
        private NetworkStream _networkStream;
        protected Codec _currentCodec;
        private CancellationTokenSource _cts;
        private int _nextRequestId;
        protected bool _useHeartbeat;
        private bool _isConnected;
        private readonly BlockingCollection<MessageWithType> _blockingCollection;
        private ConfiguredTaskAwaitable _taskConsumer;
        private ConfiguredTaskAwaitable _taskProducer;

        /// <summary>
        /// Constructor for a client
        /// </summary>
        /// <param name="serverAddress">the machine name or an IP address for the server to which we want to connect</param>
        /// <param name="serverPort">the port for the server to which we want to connect</param>
        /// <param name="timeoutNoActivity">milliseconds timeout to assume disconnected if no activity. Set to 0 for Infinite</param>
        protected ClientBase(string serverAddress, int serverPort, int timeoutNoActivity)
        {
            ServerAddress = serverAddress;
            TimeoutNoActivity = timeoutNoActivity;
            ServerPort = serverPort;
            _blockingCollection = new BlockingCollection<MessageWithType>();
        }

        public bool IsConnected => _isConnected;

        /// <summary>
        /// The most recent _logonResponse.
        /// Use this to check Server flags before doing SendRequest()
        /// </summary>
        public LogonResponse LogonResponse { get; protected set; }

        /// <summary>
        /// See http://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#SymbolIDRequestIDRules
        /// This is auto-incrementing
        /// </summary>
        public int NextRequestId => ++_nextRequestId;

        public string ServerAddress { get; }

        public int ServerPort { get; }

        public string ClientName { get; protected set; }

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
                Disconnect(new Error("Too long since Server sent us a heartbeat."));
            }

            // Send a heartbeat to the server
            var heartbeat = new Heartbeat();
            SendRequest(DTCMessageType.Heartbeat, heartbeat);
        }

        #region events

        public event EventHandler Connected;

        private void OnConnected()
        {
            _isConnected = true;
            var temp = Connected;
            temp?.Invoke(this, new EventArgs());
        }

        public event EventHandler<Error> Disconnected;

        protected void OnDisconnected(Error error)
        {
            _isConnected = false;
            var temp = Disconnected;
            temp?.Invoke(this, error);
        }

        public event EventHandler<Heartbeat> HeartbeatEvent;
        public event EventHandler<Logoff> LogoffEvent;

        public event EventHandler<EncodingResponse> EncodingResponseEvent;
        public event EventHandler<LogonResponse> LogonResponseEvent;

        #endregion events

        /// <summary>
        /// Make the connection to server at port. 
        /// Start the listener that will throw events for messages received from the server.
        /// To Disconnect simply Dispose() of this class.
        /// </summary>
        /// <param name="requestedEncoding"></param>
        /// <param name="timeout">The time (in milliseconds) to wait for a response before giving up</param>
        /// <param name="clientName">optional name for this client</param>
        /// <returns><c>true</c> if successful. <c>false</c> means protocol buffers are not supported by server</returns>
        public async Task<EncodingResponse> ConnectAsync(EncodingEnum requestedEncoding, string clientName, int timeout = 1000)
        {
            if (_isDisposed)
            {
                return null;
            }
            ClientName = clientName;
            _tcpClient = new TcpClient {NoDelay = true, ReceiveBufferSize = int.MaxValue, LingerState = new LingerOption(true, 5)};
            if (TimeoutNoActivity != 0)
            {
                _tcpClient.ReceiveTimeout = TimeoutNoActivity;
            }
            try
            {
                await _tcpClient.ConnectAsync(ServerAddress, ServerPort).ConfigureAwait(false); // connect to the server
            }
            catch (SocketException sex)
            {
                OnDisconnected(new Error(sex.Message));
            }
            // Every Codec must write the encoding request as binary
            _networkStream = _tcpClient.GetStream();
            _currentCodec = new CodecProtobuf(_networkStream, ClientOrServer.Client);
            _cts = new CancellationTokenSource();
            _taskConsumer = Task.Factory.StartNew(ConsumerLoop, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current)
                .ConfigureAwait(false);
            _taskProducer = Task.Factory.StartNew(ProducerLoop, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current)
                .ConfigureAwait(false);

            // Set up the handler to capture the event
            EncodingResponse result = null;

            void Handler(object s, EncodingResponse e)
            {
                EncodingResponseEvent -= Handler; // unregister to avoid a potential memory leak
                result = e;
            }

            EncodingResponseEvent += Handler;

            // Request protocol buffers encoding
            var encodingRequest = new EncodingRequest
            {
                Encoding = requestedEncoding,
                ProtocolType = "DTC",
                ProtocolVersion = (int)DTCVersion.CurrentVersion
            };

            // Give the server a bit to be able to respond
            //await Task.Delay(100).ConfigureAwait(true);
            SendRequest(DTCMessageType.EncodingRequest, encodingRequest);

            // Wait until the response is received or until timeout
            var startTime = DateTime.Now;
            while (result == null && (DateTime.Now - startTime).TotalMilliseconds < timeout)
            {
                await Task.Delay(1).ConfigureAwait(false);
            }
            if (result != null)
            {
                OnConnected();
            }
            return result;
        }

    
        /// <summary>
        /// Call this method AFTER calling ConnectAsync() to make the connection
        /// Send a Logon request to the server. 
        /// If not useHeartbeat, won't do a heartbeat. See: http://www.sierrachart.com/index.php?page=doc/DTCServer.php#HistoricalPriceDataServer
        /// </summary>
        /// <param name="heartbeatIntervalInSeconds">The interval in seconds that each side, the Client and the Server, needs to use to send HEARTBEAT messages to the other side. This should be a value from anywhere from 5 to 60 seconds.</param>
        /// <param name="useHeartbeat"><c>true</c>no heartbeat sent to server and none checked from server</param>
        /// <param name="timeout">The time (in milliseconds) to wait for a response before giving up</param>
        /// <param name="userName">Optional user name for the server to authenticate the Client</param>
        /// <param name="password">Optional password for the server to authenticate the Client</param>
        /// <param name="generalTextData">Optional general-purpose text string. For example, this could be used to pass a license key that the Server may require</param>
        /// <param name="integer1">Optional. General-purpose integer</param>
        /// <param name="integer2">Optional. General-purpose integer</param>
        /// <param name="tradeMode">optional to indicate to the Server that the requested trading mode to be one of the following: Demo, Simulated, Live.</param>
        /// <param name="tradeAccount">optional identifier if that is required to login</param>
        /// <param name="hardwareIdentifier">optional computer hardware identifier</param>
        /// <returns>The LogonResponse, or null if not received before timeout</returns>
        public async Task<LogonResponse> LogonAsync(int heartbeatIntervalInSeconds = 1, bool useHeartbeat = true, int timeout = 1000, string userName = "",
            string password = "", string generalTextData = "", int integer1 = 0, int integer2 = 0, TradeModeEnum tradeMode = TradeModeEnum.TradeModeUnset,
            string tradeAccount = "", string hardwareIdentifier = "")
        {
            if (_isDisposed)
            {
                return null;
            }
            _useHeartbeat = useHeartbeat;
            if (_useHeartbeat)
            {
                // start the heartbeat
                _timerHeartbeat = new Timer(heartbeatIntervalInSeconds * 1000);
                _timerHeartbeat.Elapsed += TimerHeartbeatElapsed;
                _lastHeartbeatReceivedTime = DateTime.Now;
                _timerHeartbeat.Start();
            }

            // Set up the handler to capture the event
            LogonResponse result = null;

            void Handler(object s, LogonResponse e)
            {
                LogonResponseEvent -= Handler; // unregister to avoid a potential memory leak
                result = e;
            }

            LogonResponseEvent += Handler;

            // Send the request
            var logonRequest = new LogonRequest
            {
                ClientName = ClientName,
                GeneralTextData = generalTextData,
                HardwareIdentifier = hardwareIdentifier,
                HeartbeatIntervalInSeconds = _useHeartbeat ? heartbeatIntervalInSeconds : 0,
                Integer1 = integer1,
                Integer2 = integer2,
                Username = userName,
                Password = password,
                ProtocolVersion = (int)DTCVersion.CurrentVersion,
                TradeAccount = tradeAccount,
                TradeMode = tradeMode
            };
            SendRequest(DTCMessageType.LogonRequest, logonRequest);

            // Wait until the response is received or until timeout
            var startTime = DateTime.Now;
            while (result == null && (DateTime.Now - startTime).TotalMilliseconds < timeout)
            {
                await Task.Delay(1).ConfigureAwait(false);
            }
            return result;
        }

        /// <summary>
        /// Return symbol|exchange
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="exchange"></param>
        /// <returns></returns>
        public string CombineSymbolExchange(string symbol, string exchange)
        {
            return $"{symbol}|{exchange}";
        }

        /// <summary>
        /// Split symbol|exchange.
        /// </summary>
        /// <param name="combo"></param>
        /// <param name="symbol"></param>
        /// <param name="exchange"></param>
        public void SplitSymbolExchange(string combo, out string symbol, out string exchange)
        {
            if (string.IsNullOrEmpty(combo))
            {
                symbol = null;
                exchange = null;
                return;
            }
            var splits = combo.Split('|');
            symbol = splits[0];
            exchange = splits[1];
        }

        protected void ThrowEventImpl<T>(T message, EventHandler<T> eventForMessage)
        {
            var temp = eventForMessage;
            temp?.Invoke(this, message);
        }

        protected void ThrowEvent<T>(T message, EventHandler<T> eventForMessage) where T : IMessage
        {
            ThrowEventImpl(message, eventForMessage);
        }

        /// <summary>
        /// This message runs in a continuous loop on its own thread, throwing events as messages are received.
        /// </summary>
        private void ResponseReader()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    // if (!_networkStream.DataAvailable)
                    // {
                    //     await Task.Delay(1).ConfigureAwait(false);
                    //     continue;
                    // }
                    //s_logger.Debug($"Waiting in {nameof(Client)}.{nameof(ResponseReader)} to read a message");
                    var (messageType, messageBytes) = _currentCodec.ReadMessage();
                    if (messageType == DTCMessageType.MessageTypeUnset)
                    {
                        // End of zipped historical data. We can't proceed
                        return;
                    }
                    ProcessResponseBytes(messageType, messageBytes);
                    //await Task.Delay(1).ConfigureAwait(false);
                }
                catch (IOException ex)
                {
                    // Ignore this if it results from disconnect (cancellation)
                    if (_cts.Token.IsCancellationRequested)
                    {
                        Disconnect(new Error("Read error.", ex));
                        return;
                    }
                    Logger.Error(ex, ex.Message);
                    throw;
                }
                catch (Exception ex)
                {
                    var typeName = ex.GetType().Name;
                    Disconnect(new Error($"Read error {typeName}.", ex));
                    Logger.Error(ex, ex.Message);
                }
            }
        }
        
        /// <summary>
        /// Block until a message is available
        /// </summary>
        private MessageWithType ReadMessageWithType()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    //s_logger.Debug($"Waiting in {nameof(Client)}.{nameof(ResponseReader)} to read a message");
                    var messageWithType = _currentCodec.ReadIMessage();
                    return messageWithType;
                }
                catch (IOException ex)
                {
                    // Ignore this if it results from disconnect (cancellation)
                    if (_cts.Token.IsCancellationRequested)
                    {
                        Disconnect(new Error("Read error.", ex));
                        return null;
                    }
                    Logger.Error(ex, ex.Message);
                    throw;
                }
                catch (Exception ex)
                {
                    var typeName = ex.GetType().Name;
                    Disconnect(new Error($"Read error {typeName}.", ex));
                    Logger.Error(ex, ex.Message);
                }
            }
        }

        /// <summary>
        /// internal for unit tests
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="message"></param>
        /// <typeparam name="T"></typeparam>
        protected internal void SendRequest<T>(DTCMessageType messageType, T message) where T : IMessage
        {
#if DEBUG
//            if (messageType != DTCMessageType.Heartbeat)
//            {
//#pragma warning disable 219
//                var debug = 1;
//#pragma warning restore 219
//            }
            DebugHelpers.AddRequestSent(messageType, _currentCodec);
            //var port = ((IPEndPoint)_tcpClient?.Client.LocalEndPoint)?.Port;
            //if (port == 49998 && messageType == DTCMessageType.LogonResponse)
            if (messageType == DTCMessageType.LogonRequest)
            {
#pragma warning disable 219
                var debug2 = 1;
#pragma warning restore 219
                var requestsSent = DebugHelpers.RequestsSent;
                var requestsReceived = DebugHelpers.RequestsReceived;
                var responsesReceived = DebugHelpers.ResponsesReceived;
                var responsesSent = DebugHelpers.ResponsesSent;
            }

#endif
            try
            {
                _currentCodec.Write(messageType, message);
            }
            catch (Exception ex)
            {
                var error = new Error("Unable to send request", ex);
                Disconnect(error);
            }
        }

        /// <summary>
        /// Producer 
        /// </summary>
        private void ProducerLoop()
        {
            while (!_cts.IsCancellationRequested)
            {
                var messageWithType = ReadMessageWithType();
                if (messageWithType == null)
                {
                    // Probably an exception
                    break;
                }
                _blockingCollection.Add(messageWithType, _cts.Token);
            }
            _blockingCollection.CompleteAdding();
        }

        /// <summary>
        /// Consumer
        /// </summary>
        private void ConsumerLoop()
        {
            while (!_blockingCollection.IsCompleted)
            {
                MessageWithType data = null;
                try
                {
                    data = _blockingCollection.Take(_cts.Token);
                }
                catch (InvalidOperationException)
                {
                    // The Microsoft example says we can ignore this exception https://docs.microsoft.com/en-us/dotnet/standard/collections/thread-safe/blockingcollection-overview
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                if (data != null)
                {
                    SendMessage(data);
                }
            }
        }

        protected virtual void SendMessage(MessageWithType messageWithType)
        {
            //s_logger.Debug($"{nameof(ProcessMessage)} is processing {messageWithType}");
            switch (messageWithType.MessageType)
            {
                case DTCMessageType.MessageTypeUnset:
                    throw new InvalidDataException("Unset should never happen");
                case DTCMessageType.LogonRequest:
                    throw new InvalidDataException($"{messageWithType} should never be sent.");
                case DTCMessageType.LogonResponse:
                    ThrowEvent(LogonResponse, LogonResponseEvent);
                    break;
                case DTCMessageType.Heartbeat:
                    _lastHeartbeatReceivedTime = DateTime.Now;
                    ThrowEvent(messageWithType.Message, HeartbeatEvent);
                    break;
                case DTCMessageType.Logoff:
                    break;
                case DTCMessageType.EncodingRequest:
                    break;
                case DTCMessageType.EncodingResponse:
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
                case DTCMessageType.MarketDataUpdateTradeWithUnbundledIndicator:
                    break;
                case DTCMessageType.MarketDataUpdateTradeWithUnbundledIndicator2:
                    break;
                case DTCMessageType.MarketDataUpdateTradeNoTimestamp:
                    break;
                case DTCMessageType.MarketDataUpdateBidAsk:
                    break;
                case DTCMessageType.MarketDataUpdateBidAskCompact:
                    break;
                case DTCMessageType.MarketDataUpdateBidAskNoTimestamp:
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
                case DTCMessageType.MarketDepthSnapshotLevelFloat:
                    break;
                case DTCMessageType.MarketDepthUpdateLevel:
                    break;
                case DTCMessageType.MarketDepthUpdateLevelFloatWithMilliseconds:
                    break;
                case DTCMessageType.MarketDepthUpdateLevelNoTimestamp:
                    break;
                case DTCMessageType.MarketDepthUpdateLevelInt:
                    break;
                case DTCMessageType.MarketDataFeedStatus:
                    break;
                case DTCMessageType.MarketDataFeedSymbolStatus:
                    break;
                case DTCMessageType.TradingSymbolStatus:
                    break;
                case DTCMessageType.SubmitNewSingleOrder:
                    break;
                case DTCMessageType.SubmitNewSingleOrderInt:
                    break;
                case DTCMessageType.SubmitNewOcoOrder:
                    break;
                case DTCMessageType.SubmitNewOcoOrderInt:
                    break;
                case DTCMessageType.SubmitFlattenPositionOrder:
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
                case DTCMessageType.HistoricalOrderFillsReject:
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
                case DTCMessageType.AccountBalanceAdjustment:
                    break;
                case DTCMessageType.AccountBalanceAdjustmentReject:
                    break;
                case DTCMessageType.AccountBalanceAdjustmentComplete:
                    break;
                case DTCMessageType.HistoricalAccountBalancesRequest:
                    break;
                case DTCMessageType.HistoricalAccountBalancesReject:
                    break;
                case DTCMessageType.HistoricalAccountBalanceResponse:
                    break;
                case DTCMessageType.UserMessage:
                    break;
                case DTCMessageType.GeneralLogMessage:
                    break;
                case DTCMessageType.AlertMessage:
                    break;
                case DTCMessageType.JournalEntryAdd:
                    break;
                case DTCMessageType.JournalEntriesRequest:
                    break;
                case DTCMessageType.JournalEntriesReject:
                    break;
                case DTCMessageType.JournalEntryResponse:
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
                case DTCMessageType.HistoricalPriceDataResponseTrailer:
                    break;
                case DTCMessageType.HistoricalMarketDepthDataRequest:
                    break;
                case DTCMessageType.HistoricalMarketDepthDataResponseHeader:
                    break;
                case DTCMessageType.HistoricalMarketDepthDataReject:
                    break;
                case DTCMessageType.HistoricalMarketDepthDataRecordResponse:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Process the message represented by bytes.
        /// binaryReader may be changed to use a new DeflateStream if we change to zipped
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="messageBytes"></param>
        protected virtual void ProcessResponseBytes(DTCMessageType messageType, byte[] messageBytes)
        {
            //s_logger.Debug($"{nameof(ProcessResponseBytes)} is processing {messageType}");
            switch (messageType)
            {
                case DTCMessageType.LogonResponse:
                    LogonResponse = _currentCodec.Load<LogonResponse>(messageType, messageBytes);
                    ThrowEvent(LogonResponse, LogonResponseEvent);
                    break;
                case DTCMessageType.Heartbeat:
                    _lastHeartbeatReceivedTime = DateTime.Now;
                    var heartbeat = _currentCodec.Load<Heartbeat>(messageType, messageBytes);
                    ThrowEvent(heartbeat, HeartbeatEvent);
                    break;
                case DTCMessageType.Logoff:
                    var logoff = _currentCodec.Load<Logoff>(messageType, messageBytes);
                    OnDisconnected(new Error("User logoff"));
                    ThrowEvent(logoff, LogoffEvent);
                    break;
                case DTCMessageType.EncodingResponse:
                    // Note that we must use binary encoding here on the first usage after connect, 
                    //    per http://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#EncodingRequest
                    var encodingResponse = _currentCodec.Load<EncodingResponse>(messageType, messageBytes);
                    switch (encodingResponse.Encoding)
                    {
                        case EncodingEnum.BinaryEncoding:
                            _currentCodec = new CodecBinary(_networkStream, ClientOrServer.Client);
                            break;
                        case EncodingEnum.BinaryWithVariableLengthStrings:
                        case EncodingEnum.JsonEncoding:
                        case EncodingEnum.JsonCompactEncoding:
                            throw new NotImplementedException($"Not implemented in {nameof(ProcessResponseBytes)}: {nameof(encodingResponse.Encoding)}");
                        case EncodingEnum.ProtocolBuffers:
                            _currentCodec = new CodecProtobuf(_networkStream, ClientOrServer.Client);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    ThrowEvent(encodingResponse, EncodingResponseEvent);
                    break;
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
                case DTCMessageType.HistoricalMarketDepthDataResponseHeader:
                case DTCMessageType.HistoricalMarketDepthDataReject:
                case DTCMessageType.HistoricalMarketDepthDataRecordResponse:
                    throw new NotSupportedException("These records should be handled in a derived Client");
                case DTCMessageType.MessageTypeUnset:
                case DTCMessageType.LogonRequest:
                case DTCMessageType.EncodingRequest:
                case DTCMessageType.MarketDataRequest:
                case DTCMessageType.MarketDepthRequest:
                case DTCMessageType.SubmitNewSingleOrder:
                case DTCMessageType.SubmitNewSingleOrderInt:
                case DTCMessageType.SubmitNewOcoOrder:
                case DTCMessageType.SubmitNewOcoOrderInt:
                case DTCMessageType.CancelOrder:
                case DTCMessageType.CancelReplaceOrder:
                case DTCMessageType.CancelReplaceOrderInt:
                case DTCMessageType.OpenOrdersRequest:
                case DTCMessageType.HistoricalOrderFillsRequest:
                case DTCMessageType.CurrentPositionsRequest:
                case DTCMessageType.TradeAccountsRequest:
                case DTCMessageType.ExchangeListRequest:
                case DTCMessageType.SymbolsForExchangeRequest:
                case DTCMessageType.UnderlyingSymbolsForExchangeRequest:
                case DTCMessageType.SymbolsForUnderlyingRequest:
                case DTCMessageType.SecurityDefinitionForSymbolRequest:
                case DTCMessageType.SymbolSearchRequest:
                case DTCMessageType.AccountBalanceRequest:
                case DTCMessageType.HistoricalPriceDataRequest:
                    throw new NotImplementedException($"{messageType}");
                default:
#if DEBUG
                    var requestsSent = DebugHelpers.RequestsSent;
                    var requestsReceived = DebugHelpers.RequestsReceived;
                    var responsesReceived = DebugHelpers.ResponsesReceived;
                    var responsesSent = DebugHelpers.ResponsesSent;
#endif
                    throw new ArgumentOutOfRangeException($"Unexpected MessageType {messageType} received by {ClientName} {nameof(ProcessResponseBytes)}.");
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                //s_logger.Debug("Disposing Client");
                _currentCodec.Write(DTCMessageType.Logoff, new Logoff
                {
                    DoNotReconnect = 1u,
                    Reason = "Client Disposed"
                });
                _cts.Cancel();
                _currentCodec.Close();
                Disconnect(new Error("Disposing"));
                _isDisposed = true;
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Throw the Disconnect event and close down the connection (if any) to the server
        /// </summary>
        /// <param name="error"></param>
        protected void Disconnect(Error error)
        {
            OnDisconnected(error);

            _timerHeartbeat?.Dispose();
            _cts?.Cancel();
            _tcpClient?.Close();
            _tcpClient = null;
        }

        public override string ToString()
        {
            return $"{ClientName} {ServerAddress} {ServerPort}";
        }
    }
}