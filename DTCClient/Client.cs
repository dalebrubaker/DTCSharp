using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using DTCCommon;
using DTCCommon.Codecs;
using DTCCommon.Exceptions;
using DTCCommon.Extensions;
using DTCPB;
using Google.Protobuf;
using NLog;
using Timer = System.Timers.Timer;

namespace DTCClient
{
    public class Client : IDisposable
    {
        private static readonly ILogger s_logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Holds messages received from the server
        /// </summary>
        private readonly BlockingCollection<MessageDTC> _serverMessageQueue;

        private readonly int _timeoutNoActivity;
        private readonly SemaphoreSlim _semaphoreStreamReader;
        private Timer _timerHeartbeat;
        private bool _isDisposed;
        private TcpClient _tcpClient;
        private DateTime _lastHeartbeatReceivedTime;
        private NetworkStream _networkStream;
        private Codec _currentCodec;
        private CancellationTokenSource _cts;
        private int _nextRequestId;
        private bool _useHeartbeat;
        private bool _isConnected;
        private Task _taskServerMessageProcessor;
        private Task _taskServerMessageReader;

        /// <summary>
        /// Constructor for a client
        /// </summary>
        /// <param name="serverAddress">the machine name or an IP address for the server to which we want to connect</param>
        /// <param name="serverPort">the port for the server to which we want to connect</param>
        /// <param name="timeoutNoActivity">milliseconds timeout to assume disconnected if no activity. Set to 0 for Infinite</param>
        public Client(string serverAddress, int serverPort, int timeoutNoActivity)
        {
            ServerAddress = serverAddress;
            _timeoutNoActivity = timeoutNoActivity;
            ServerPort = serverPort;
            _serverMessageQueue = new BlockingCollection<MessageDTC>();
            _semaphoreStreamReader = new SemaphoreSlim(0, 1);
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

        private void OnDisconnected(Error error)
        {
            _isConnected = false;
            var temp = Disconnected;
            temp?.Invoke(this, error);
        }

        public event EventHandler<Heartbeat> HeartbeatEvent;
        public event EventHandler<Logoff> LogoffEvent;

        public event EventHandler<EncodingResponse> EncodingResponseEvent;
        public event EventHandler<LogonResponse> LogonResponseEvent;
        public event EventHandler<MarketDataReject> MarketDataRejectEvent;
        public event EventHandler<MarketDataSnapshot> MarketDataSnapshotEvent;
        public event EventHandler<MarketDataSnapshot_Int> MarketDataSnapshotIntEvent;
        public event EventHandler<MarketDataUpdateTrade> MarketDataUpdateTradeEvent;
        public event EventHandler<MarketDataUpdateTradeCompact> MarketDataUpdateTradeCompactEvent;
        public event EventHandler<MarketDataUpdateTrade_Int> MarketDataUpdateTradeIntEvent;
        public event EventHandler<MarketDataUpdateLastTradeSnapshot> MarketDataUpdateLastTradeSnapshotEvent;
        public event EventHandler<MarketDataUpdateBidAsk> MarketDataUpdateBidAskEvent;
        public event EventHandler<MarketDataUpdateBidAskCompact> MarketDataUpdateBidAskCompactEvent;
        public event EventHandler<MarketDataUpdateBidAsk_Int> MarketDataUpdateBidAskIntEvent;
        public event EventHandler<MarketDataUpdateSessionOpen> MarketDataUpdateSessionOpenEvent;
        public event EventHandler<MarketDataUpdateSessionOpen_Int> MarketDataUpdateSessionOpenIntEvent;
        public event EventHandler<MarketDataUpdateSessionHigh> MarketDataUpdateSessionHighEvent;
        public event EventHandler<MarketDataUpdateSessionHigh_Int> MarketDataUpdateSessionHighIntEvent;
        public event EventHandler<MarketDataUpdateSessionLow> MarketDataUpdateSessionLowEvent;
        public event EventHandler<MarketDataUpdateSessionLow_Int> MarketDataUpdateSessionLowIntEvent;
        public event EventHandler<MarketDataUpdateSessionVolume> MarketDataUpdateSessionVolumeEvent;
        public event EventHandler<MarketDataUpdateOpenInterest> MarketDataUpdateOpenInterestEvent;
        public event EventHandler<MarketDataUpdateSessionSettlement> MarketDataUpdateSessionSettlementEvent;
        public event EventHandler<MarketDataUpdateSessionSettlement_Int> MarketDataUpdateSessionSettlementIntEvent;
        public event EventHandler<MarketDataUpdateSessionNumTrades> MarketDataUpdateSessionNumTradesEvent;
        public event EventHandler<MarketDataUpdateTradingSessionDate> MarketDataUpdateTradingSessionDateEvent;
        public event EventHandler<MarketDepthReject> MarketDepthRejectEvent;
        public event EventHandler<MarketDepthSnapshotLevel> MarketDepthSnapshotLevelEvent;
        public event EventHandler<MarketDepthSnapshotLevel_Int> MarketDepthSnapshotLevelIntEvent;
        public event EventHandler<MarketDepthUpdateLevel> MarketDepthUpdateLevelEvent;
        public event EventHandler<MarketDepthUpdateLevel_Int> MarketDepthUpdateLevelIntEvent;
        public event EventHandler<MarketDataFeedStatus> MarketDataFeedStatusEvent;
        public event EventHandler<MarketDataFeedSymbolStatus> MarketDataFeedSymbolStatusEvent;
        public event EventHandler<OpenOrdersReject> OpenOrdersRejectEvent;
        public event EventHandler<OrderUpdate> OrderUpdateEvent;
        public event EventHandler<HistoricalOrderFillResponse> HistoricalOrderFillResponseEvent;
        public event EventHandler<CurrentPositionsReject> CurrentPositionsRejectEvent;
        public event EventHandler<PositionUpdate> PositionUpdateEvent;
        public event EventHandler<TradeAccountResponse> TradeAccountResponseEvent;
        public event EventHandler<ExchangeListResponse> ExchangeListResponseEvent;
        public event EventHandler<SecurityDefinitionResponse> SecurityDefinitionResponseEvent;
        public event EventHandler<SecurityDefinitionReject> SecurityDefinitionRejectEvent;
        public event EventHandler<AccountBalanceReject> AccountBalanceRejectEvent;
        public event EventHandler<AccountBalanceUpdate> AccountBalanceUpdateEvent;
        public event EventHandler<UserMessage> UserMessageEvent;
        public event EventHandler<GeneralLogMessage> GeneralLogMessageEvent;
        public event EventHandler<HistoricalPriceDataResponseHeader> HistoricalPriceDataResponseHeaderEvent;
        public event EventHandler<HistoricalPriceDataReject> HistoricalPriceDataRejectEvent;
        public event EventHandler<HistoricalPriceDataRecordResponse> HistoricalPriceDataRecordResponseEvent;
        public event EventHandler<HistoricalPriceDataTickRecordResponse> HistoricalPriceDataTickRecordResponseEvent;
        public event EventHandler<HistoricalPriceDataRecordResponse_Int> HistoricalPriceDataRecordResponseIntEvent;
        public event EventHandler<HistoricalPriceDataTickRecordResponse_Int> HistoricalPriceDataTickRecordResponseIntEvent;

        private void ThrowEvent<T>(T message, EventHandler<T> eventForMessage) where T : IMessage
        {
            var temp = eventForMessage;
            temp?.Invoke(this, message);
        }

        public event EventHandler<IMessage> EveryMessageFromServer;

        private void OnEveryEventFromServer(IMessage protobuf)
        {
            var tmp = EveryMessageFromServer;
            tmp?.Invoke(this, protobuf);
        }

        #endregion events

        public bool IsConnected => _isConnected;

        /// <summary>
        /// The most recent _logonResponse.
        /// Use this to check Server flags before doing SendRequest()
        /// </summary>
        public LogonResponse LogonResponse { get; private set; }

        /// <summary>
        /// See http://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#SymbolIDRequestIDRules
        /// This is auto-incrementing
        /// </summary>
        public int NextRequestId => ++_nextRequestId;

        public string ServerAddress { get; }

        public int ServerPort { get; }

        public string ClientName { get; private set; }

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
                OnDisconnect(new Error("Too long since Server sent us a heartbeat."));
            }

            // Send a heartbeat to the server
            var heartbeat = new Heartbeat();
            SendRequest(DTCMessageType.Heartbeat, heartbeat);
        }

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
            _tcpClient = new TcpClient
            {
                NoDelay = true,
                ReceiveBufferSize = int.MaxValue
            };
            var tmp = _tcpClient.SendBufferSize;
            if (_timeoutNoActivity != 0)
            {
                _tcpClient.ReceiveTimeout = _timeoutNoActivity;
            }
            try
            {
                await _tcpClient.ConnectAsync(ServerAddress, ServerPort).ConfigureAwait(false); // connect to the server
            }
            catch (SocketException sex)
            {
                OnDisconnected(new Error(sex.Message));
            }
            _networkStream = _tcpClient.GetStream();

            // The initial protocol must be Binary, and can switch only when receiving and EncodingResponse
            SetCurrentCodec(EncodingEnum.BinaryEncoding);
            s_logger.Debug("Initial setting of _currentCodec is Binary");
            _cts = new CancellationTokenSource();
            _taskServerMessageProcessor = Task.Factory.StartNew(ServerMessageProcessor, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
            _taskServerMessageReader = Task.Factory.StartNew(ServerMessageReader, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);

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
        /// internal for unit tests
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="message"></param>
        /// <typeparam name="T"></typeparam>
        internal void SendRequest<T>(DTCMessageType messageType, T message) where T : IMessage
        {
            try
            {
                _currentCodec.Write(messageType, message);
            }
            catch (Exception ex)
            {
                var error = new Error("Unable to send request", ex);
                OnDisconnect(error);
            }
        }

        /// <summary>
        /// Producer, loads messages from the server into the _serverMessageQueue
        /// </summary>
        private void ServerMessageReader()
        {
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    //s_logger.Debug($"Waiting to read a message in {nameof(Client)}.{nameof(ServerMessageReader)} using {_currentCodec}");
                    var message = ReadMessageDTC();
                    //s_logger.Debug($"Did read message={message} in {nameof(Client)}.{nameof(ServerMessageReader)} using {_currentCodec}");
                    if (_cts.IsCancellationRequested)
                    {
                        return;
                    }
                    if (message == null)
                    {
                        // Probably an exception
                        throw new DTCSharpException("Why?");
                    }
                    //Logger.Debug($"Waiting in {nameof(Client)}.{nameof(ServerMessageReader)} to add to messageQueue: {message}");
                    _serverMessageQueue.Add(message, _cts.Token);
                    //Logger.Debug($"{nameof(Client)}.{nameof(ServerMessageReader)} added to messageQueue: {message}");

                    BlockReadingStream(message);
                }
                _serverMessageQueue.CompleteAdding();
            }
            catch (Exception ex)
            {
                if (_cts.IsCancellationRequested)
                {
                    return;
                }
                s_logger.Error(ex, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// If we have received a record that will cause us to switch encodings or switch to or from a zipped stream, block until ProcessMessage() releases.
        /// </summary>
        /// <param name="message"></param>
        private void BlockReadingStream(MessageDTC message)
        {
            if (message.MessageType == DTCMessageType.EncodingResponse)
            {
                var encodingResponse = message.Message as EncodingResponse;
                if (encodingResponse.Encoding == _currentCodec.Encoding)
                {
                    // No change
                    return;
                }

                // server has accepted a new encoding
                // Block reading until ProcessMessage() has changed the encoding
                //s_logger.Debug($"Blocking reader, _currentCodec={_currentCodec}");
                _semaphoreStreamReader.Wait();
                //s_logger.Debug($"Done blocking reader, _currentCodec={_currentCodec}");
            }
            else if (message.MessageType == DTCMessageType.HistoricalPriceDataResponseHeader)
            {
                var historicalPriceDataResponseHeader = message.Message as HistoricalPriceDataResponseHeader;
                if (historicalPriceDataResponseHeader.UseZLibCompressionBool && !_currentCodec.IsZippedStream)
                {
                    // Block until ProcessMessage() changes to zipped stream
                    _semaphoreStreamReader.Wait();
                }
            }
            else if (_currentCodec.IsZippedStream && message.MessageType == DTCMessageType.HistoricalPriceDataRecordResponse)
            {
                var historicalPriceDataRecordResponse = message.Message as HistoricalPriceDataRecordResponse;
                if (historicalPriceDataRecordResponse.IsFinalRecordBool)
                {
                    if (_currentCodec.IsZippedStream)
                    {
                        // Block until ProcessMessage() changes to not-zipped stream
                        _semaphoreStreamReader.Wait();
                    }
                }
            }
            else if (_currentCodec.IsZippedStream && message.MessageType == DTCMessageType.HistoricalPriceDataTickRecordResponse)
            {
                var historicalPriceDataTickRecordResponse = message.Message as HistoricalPriceDataTickRecordResponse;
                if (historicalPriceDataTickRecordResponse.IsFinalRecordBool)
                {
                    if (_currentCodec.IsZippedStream)
                    {
                        // Block until ProcessMessage() changes to not-zipped stream
                        _semaphoreStreamReader.Wait();
                    }
                }
            }
        }

        private MessageDTC ReadMessageDTC()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    //s_logger.Debug($"Waiting in {nameof(Client)}.{nameof(ResponseReader)} to read a message");
                    var message = _currentCodec.GetMessageDTC();
                    return message;
                }
                catch (IOException ex)
                {
                    // Ignore this if it results from disconnect (cancellation)
                    if (_cts.IsCancellationRequested)
                    {
                        OnDisconnect(new Error("Read error.", ex));
                        return null;
                    }
                    s_logger.Error(ex, ex.Message);
                    throw;
                }
                catch (Exception ex)
                {
                    var typeName = ex.GetType().Name;
                    OnDisconnect(new Error($"Read error {typeName}.", ex));
                    s_logger.Error(ex, ex.Message);
                    throw;
                }
            }
            return null;
        }

        /// <summary>
        /// Consumer, processes the messages from the _serverMessageQueue
        /// </summary>
        private void ServerMessageProcessor()
        {
            while (!_serverMessageQueue.IsCompleted)
            {
                MessageDTC message = null;
                try
                {
                    message = _serverMessageQueue.Take(_cts.Token);
                    //Logger.Debug($"{nameof(Client)}.{nameof(ServerMessageProcessor)} took from messageQueue: {message}");
                }
                catch (InvalidOperationException)
                {
                    // The Microsoft example says we can ignore this exception https://docs.microsoft.com/en-us/dotnet/standard/collections/thread-safe/blockingcollection-overview
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    s_logger.Error(ex, ex.Message);
                    throw;
                }
                if (message != null)
                {
                    ProcessMessage(message);
                }
            }
        }

        /// <summary>
        /// See http://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#HistoricalPriceData for details
        /// If the request is rejected, this method will return null immediately.
        /// Otherwise the HistoricalPriceDataResponseHeader will be sent to headerCallback followed by HistoricalPriceDataRecordResponse to dataCallback.
        /// Probably this will work only for one symbol per client. Make a new client for each request.
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="exchange"></param>
        /// <param name="recordInterval"></param>
        /// <param name="startDateTimeUtc">Use DateTime.MinValue for 0</param>
        /// <param name="endDateTimeUtc">Use DateTime.MinValue for 0</param>
        /// <param name="maxDaysToReturn"></param>
        /// <param name="useZLibCompression"></param>
        /// <param name="flag1"></param>
        /// <param name="requestDividendAdjustedStockData"></param>
        /// <param name="headerCallback">callback for header</param>
        /// <param name="dataCallback">callback for HistoricalPriceDataRecordResponses</param>
        /// <param name="cancellationToken"></param>`
        /// <returns>rejection, or null if not rejected</returns>
        public async Task<HistoricalPriceDataReject> GetHistoricalPriceDataRecordResponsesAsync(string symbol, string exchange,
            HistoricalDataIntervalEnum recordInterval, DateTime startDateTimeUtc, DateTime endDateTimeUtc, uint maxDaysToReturn, bool useZLibCompression,
            bool requestDividendAdjustedStockData, bool flag1, Action<HistoricalPriceDataResponseHeader> headerCallback,
            Action<HistoricalPriceDataRecordResponse> dataCallback, CancellationToken cancellationToken = default)
        {
            var timeout = _timeoutNoActivity;
            HistoricalPriceDataReject historicalPriceDataReject = null;

            // Set up handler to capture the reject event
            void OnHistoricalPriceDataRejectEvent(object s, HistoricalPriceDataReject e)
            {
                HistoricalPriceDataRejectEvent -= OnHistoricalPriceDataRejectEvent; // unregister to avoid a potential memory leak
                historicalPriceDataReject = e;
                timeout = 0; // force immediate return
            }

            HistoricalPriceDataRejectEvent += OnHistoricalPriceDataRejectEvent;

            // Set up handler to capture the header event
            void HandlerHeader(object s, HistoricalPriceDataResponseHeader e)
            {
                HistoricalPriceDataResponseHeaderEvent -= HandlerHeader; // unregister to avoid a potential memory leak
                var header = e;
                headerCallback(header);
                timeout = int.MaxValue; // wait for the last price data response to arrive
            }

            HistoricalPriceDataResponseHeaderEvent += HandlerHeader;

            // Set up the handler to capture the HistoricalPriceDataRecordResponseEvent
            HistoricalPriceDataRecordResponse response;

            void Handler(object s, HistoricalPriceDataRecordResponse e)
            {
                response = e;
                dataCallback(response);
                if (e.IsFinalRecordBool)
                {
                    HistoricalPriceDataRecordResponseEvent -= Handler; // unregister to avoid a potential memory leak
                    timeout = 0; // force immediate exit
                }
            }

            HistoricalPriceDataRecordResponseEvent += Handler;

            // Send the request
            var request = new HistoricalPriceDataRequest
            {
                RequestID = NextRequestId,
                Symbol = symbol,
                Exchange = exchange,
                RecordInterval = recordInterval,
                StartDateTime = startDateTimeUtc == DateTime.MinValue ? 0 : startDateTimeUtc.UtcToDtcDateTime(),
                EndDateTime = endDateTimeUtc == DateTime.MinValue ? 0 : endDateTimeUtc.UtcToDtcDateTime(),
                MaxDaysToReturn = maxDaysToReturn,
                UseZLibCompression = useZLibCompression ? 1U : 0,
                RequestDividendAdjustedStockData = requestDividendAdjustedStockData ? 1U : 0,
                Integer1 = flag1 ? 1U : 0,
            };
            SendRequest(DTCMessageType.HistoricalPriceDataRequest, request);

            // Wait until timeout or reject or response is received
            var startTime = DateTime.Now; // for checking timeout
            while ((DateTime.Now - startTime).TotalMilliseconds < timeout && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            }
            return historicalPriceDataReject;
        }

        /// <summary>
        /// Get the SecurityDefinitionResponse for symbol, or null if not received before timeout
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="timeout">The time (in milliseconds) to wait for a response before giving up</param>
        /// <returns>the SecurityDefinitionResponse, or null if not received before timeout</returns>
        public async Task<SecurityDefinitionResponse> GetSecurityDefinitionAsync(string symbol, int timeout = 1000)
        {
            // Set up the handler to capture the event
            var startTime = DateTime.Now;
            SecurityDefinitionResponse result = null;

            void Handler(object s, SecurityDefinitionResponse e)
            {
                SecurityDefinitionResponseEvent -= Handler; // unregister to avoid a potential memory leak
                result = e;
            }

            SecurityDefinitionResponseEvent += Handler;

            // Send the request
            var securityDefinitionForSymbolRequest = new SecurityDefinitionForSymbolRequest
            {
                RequestID = NextRequestId,
                Symbol = symbol
            };
            SendRequest(DTCMessageType.SecurityDefinitionForSymbolRequest, securityDefinitionForSymbolRequest);

            // Wait until the response is received or until timeout
            while (result == null && (DateTime.Now - startTime).TotalMilliseconds < timeout)
            {
                await Task.Delay(1).ConfigureAwait(false);
            }
            return result;
        }

        /// <summary>
        /// Request market data for symbol|exchange
        /// Add a symbolId if not already assigned 
        /// This is done for you within GetMarketDataUpdateTradeCompact()
        /// </summary>
        /// <param name="symbolId">The 1-based unique SymbolId that you have assigned symbol.exchange</param>
        /// <param name="symbol"></param>
        /// <param name="exchange">optional</param>
        public uint SubscribeMarketData(uint symbolId, string symbol, string exchange)
        {
            if (LogonResponse == null || LogonResponse.MarketDataSupported == 0)
            {
                return 0;
            }
            var request = new MarketDataRequest
            {
                RequestAction = RequestActionEnum.Subscribe,
                SymbolID = symbolId,
                Symbol = symbol,
                Exchange = exchange
            };
            SendRequest(DTCMessageType.MarketDataRequest, request);
            return symbolId;
        }

        /// <summary>
        /// Unsubscribe from market data  symbolId 
        /// </summary>
        /// <param name="symbolId">The 1-based unique SymbolId that you have assigned symbol.exchange</param>
        public void UnsubscribeMarketData(uint symbolId)
        {
            var request = new MarketDataRequest
            {
                RequestAction = RequestActionEnum.Unsubscribe,
                SymbolID = symbolId,
            };
            SendRequest(DTCMessageType.MarketDataRequest, request);
        }

        /// <summary>
        /// For details see: https://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#MarketData
        /// Also https://dtcprotocol.org/index.php?page=doc/DTCMessages_MarketDataMessages.php#Messages-MARKET_DATA_REQUEST
        /// This method subscribes to market data updates. A snapshot response is immediately returned to snapshotCallback. 
        /// Then MarketDataUpdateTradeCompact responses are sent to dataCallback. Optionally, other responses are to the other callbacks.
        /// To stop the callbacks, use MarketDataUnsubscribe() and cancel this method using cancellationToken (CancellationTokenSource.Cancel())
        /// </summary>
        /// <param name="timeout">The time (in milliseconds) to wait for a response before giving up</param>
        /// <param name="symbolId">The 1-based unique SymbolId that you have assigned symbol.exchange</param>
        /// <param name="cancellationToken">To stop the callbacks, use MarketDataUnsubscribe() and cancel this method using cancellationToken (CancellationTokenSource.Cancel())</param>
        /// <param name="symbol"></param>
        /// <param name="exchange"></param>
        /// <param name="snapshotCallback">Must not be null</param>
        /// <param name="tradeCallback">Must not be null</param>
        /// <param name="bidAskCallback">Won't be used if null</param>
        /// <param name="sessionOpenCallback">Won't be used if null</param>
        /// <param name="sessionHighCallback">Won't be used if null</param>
        /// <param name="sessionLowCallback">Won't be used if null</param>
        /// <param name="sessionSettlementCallback">Won't be used if null</param>
        /// <param name="sessionVolumeCallback">Won't be used if null</param>
        /// <param name="openInterestCallback">Won't be used if null</param>
        public async Task<MarketDataReject> GetMarketDataUpdateTradeCompactAsync(uint symbolId, CancellationToken cancellationToken, int timeout, string symbol,
            string exchange, Action<MarketDataSnapshot> snapshotCallback, Action<MarketDataUpdateTradeCompact> tradeCallback,
            Action<MarketDataUpdateBidAskCompact> bidAskCallback = null, Action<MarketDataUpdateSessionOpen> sessionOpenCallback = null,
            Action<MarketDataUpdateSessionHigh> sessionHighCallback = null, Action<MarketDataUpdateSessionLow> sessionLowCallback = null,
            Action<MarketDataUpdateSessionSettlement> sessionSettlementCallback = null, Action<MarketDataUpdateSessionVolume> sessionVolumeCallback = null,
            Action<MarketDataUpdateOpenInterest> openInterestCallback = null)
        {
            MarketDataReject marketDataReject = null;
            if (LogonResponse == null)
            {
                return new MarketDataReject {RejectText = "Not logged on."};
            }
            if (LogonResponse.MarketDataSupported == 0)
            {
                return new MarketDataReject {RejectText = "Market data is not supported."};
            }

            // Set up handler to capture the reject event
            void HandlerReject(object s, MarketDataReject e)
            {
                MarketDataRejectEvent -= HandlerReject; // unregister to avoid a potential memory leak
                marketDataReject = e;
                timeout = 0; // force immediate return
            }

            MarketDataRejectEvent += HandlerReject;

            var isDataReceived = false;

            void MarketDataSnapshotEvent(object sender, MarketDataSnapshot e)
            {
                isDataReceived = true;
                snapshotCallback(e);
            }

            this.MarketDataSnapshotEvent += MarketDataSnapshotEvent;

            void MarketDataUpdateTradeCompactEvent(object sender, MarketDataUpdateTradeCompact e)
            {
                isDataReceived = true;
                tradeCallback(e);
            }

            this.MarketDataUpdateTradeCompactEvent += MarketDataUpdateTradeCompactEvent;

            void MarketDataUpdateBidAskCompactEvent(object sender, MarketDataUpdateBidAskCompact e)
            {
                isDataReceived = true;
                bidAskCallback?.Invoke(e);
            }

            this.MarketDataUpdateBidAskCompactEvent += MarketDataUpdateBidAskCompactEvent;

            void MarketDataUpdateSessionOpenEvent(object sender, MarketDataUpdateSessionOpen e)
            {
                isDataReceived = true;
                sessionOpenCallback?.Invoke(e);
            }

            this.MarketDataUpdateSessionOpenEvent += MarketDataUpdateSessionOpenEvent;

            void MarketDataUpdateSessionHighEvent(object sender, MarketDataUpdateSessionHigh e)
            {
                isDataReceived = true;
                sessionHighCallback?.Invoke(e);
            }

            this.MarketDataUpdateSessionHighEvent += MarketDataUpdateSessionHighEvent;

            void MarketDataUpdateSessionLowEvent(object sender, MarketDataUpdateSessionLow e)
            {
                isDataReceived = true;
                sessionLowCallback?.Invoke(e);
            }

            this.MarketDataUpdateSessionLowEvent += MarketDataUpdateSessionLowEvent;

            void MarketDataUpdateSessionSettlementEvent(object sender, MarketDataUpdateSessionSettlement e)
            {
                isDataReceived = true;
                sessionSettlementCallback?.Invoke(e);
            }

            this.MarketDataUpdateSessionSettlementEvent += MarketDataUpdateSessionSettlementEvent;

            void MarketDataUpdateSessionVolumeEvent(object sender, MarketDataUpdateSessionVolume e)
            {
                isDataReceived = true;
                sessionVolumeCallback?.Invoke(e);
            }

            this.MarketDataUpdateSessionVolumeEvent += MarketDataUpdateSessionVolumeEvent;

            void MarketDataUpdateOpenInterestEvent(object sender, MarketDataUpdateOpenInterest e)
            {
                isDataReceived = true;
                if (openInterestCallback != null)
                {
                    openInterestCallback(e);
                }
            }

            this.MarketDataUpdateOpenInterestEvent += MarketDataUpdateOpenInterestEvent;

            // Send the request
            var request = new MarketDataRequest
            {
                RequestAction = RequestActionEnum.Subscribe,
                SymbolID = symbolId,
                Symbol = symbol,
                Exchange = exchange
            };
            SendRequest(DTCMessageType.MarketDataRequest, request);

            // Wait until timeout or cancellation
            var startTime = DateTime.Now; // for checking timeout
            while ((DateTime.Now - startTime).TotalMilliseconds < timeout && !cancellationToken.IsCancellationRequested)
            {
                if (isDataReceived)
                {
                    // We're receiving data, so never timeout
                    timeout = int.MaxValue;
                }
                await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            }
            this.MarketDataSnapshotEvent -= MarketDataSnapshotEvent;
            this.MarketDataUpdateTradeCompactEvent -= MarketDataUpdateTradeCompactEvent;
            this.MarketDataUpdateBidAskCompactEvent -= MarketDataUpdateBidAskCompactEvent;
            this.MarketDataUpdateSessionOpenEvent -= MarketDataUpdateSessionOpenEvent;
            this.MarketDataUpdateSessionHighEvent -= MarketDataUpdateSessionHighEvent;
            this.MarketDataUpdateSessionLowEvent -= MarketDataUpdateSessionLowEvent;
            this.MarketDataUpdateSessionSettlementEvent -= MarketDataUpdateSessionSettlementEvent;
            this.MarketDataUpdateSessionVolumeEvent -= MarketDataUpdateSessionVolumeEvent;
            this.MarketDataUpdateOpenInterestEvent -= MarketDataUpdateOpenInterestEvent;

            return marketDataReject;
        }

        /// <summary>
        /// Process the message represented by bytes.
        /// binaryReader may be changed to use a new DeflateStream if we change to zipped
        /// </summary>
        /// <param name="messageDTC"></param>
        private void ProcessMessage(MessageDTC messageDTC)
        {
            OnEveryEventFromServer(messageDTC.Message);
            switch (messageDTC.MessageType)
            {
                case DTCMessageType.LogonResponse:
                    ThrowEvent(messageDTC.Message as LogonResponse, LogonResponseEvent);
                    LogonResponse = messageDTC.Message as LogonResponse;

                    // Note that SierraChart does allow more than OneHistoricalPriceDataRequestPerConnection 
                    break;
                case DTCMessageType.Heartbeat:
                    _lastHeartbeatReceivedTime = DateTime.Now;
                    ThrowEvent(messageDTC.Message as Heartbeat, HeartbeatEvent);
                    break;
                case DTCMessageType.Logoff:
                    OnDisconnected(new Error("User logoff"));
                    ThrowEvent(messageDTC.Message as Logoff, LogoffEvent);
                    break;
                case DTCMessageType.EncodingResponse:
                    // Note that we must use binary encoding here on the FIRST usage after connect, 
                    //    per http://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#EncodingRequest
                    var encodingResponse = messageDTC.Message as EncodingResponse;
                    if (encodingResponse.Encoding != _currentCodec.Encoding)
                    {
                        SetCurrentCodec(encodingResponse.Encoding);

                        // Release the reader now that the encoding has changed
                        _semaphoreStreamReader.Release();
                    }
                    ThrowEvent(encodingResponse, EncodingResponseEvent);
                    break;
                case DTCMessageType.HistoricalPriceDataResponseHeader:
                    var historicalPriceDataResponseHeader = messageDTC.Message as HistoricalPriceDataResponseHeader;
                    if (historicalPriceDataResponseHeader.UseZLibCompression == 1)
                    {
                        // Skip past the 2-byte header. See https://tools.ietf.org/html/rfc1950
                        s_logger.Debug($"{nameof(Client)}.{nameof(ProcessMessage)} is switching client stream to read zipped.");

                        _currentCodec.ReadSwitchToZipped();
                        _useHeartbeat = false;

                        // Release the reader now that the stream has changed to zipped
                        //_logger.Debug($"Releasing reader in {nameof(Client)} after encoding change, _currentCodec={_currentCodec}");
                        _semaphoreStreamReader.Release();
                    }
                    ThrowEvent(historicalPriceDataResponseHeader, HistoricalPriceDataResponseHeaderEvent);
                    break;
                case DTCMessageType.HistoricalPriceDataRecordResponse:
                    var historicalPriceDataRecordResponse = messageDTC.Message as HistoricalPriceDataRecordResponse;
                    if (historicalPriceDataRecordResponse.IsFinalRecordBool && _currentCodec.IsZippedStream)
                    {
                        // Switch back from reading zlib to regular networkStream
                        _currentCodec.EndZippedWriting();

                        // Release the reader now that the stream has changed to not-zipped
                        _semaphoreStreamReader.Release();
                    }
                    ThrowEvent(historicalPriceDataRecordResponse, HistoricalPriceDataRecordResponseEvent);
                    break;
                case DTCMessageType.HistoricalPriceDataTickRecordResponse:
                    var historicalPriceDataTickRecordResponse = messageDTC.Message as HistoricalPriceDataTickRecordResponse;
                    if (historicalPriceDataTickRecordResponse.IsFinalRecordBool && _currentCodec.IsZippedStream)
                    {
                        // Switch back from reading zlib to regular networkStream
                        _currentCodec.EndZippedWriting();

                        // Release the reader now that the stream has changed to not-zipped
                        _semaphoreStreamReader.Release();
                    }
                    ThrowEvent(historicalPriceDataTickRecordResponse, HistoricalPriceDataTickRecordResponseEvent);
                    break;
                case DTCMessageType.HistoricalPriceDataReject:
                    var historicalPriceDataReject = messageDTC.Message as HistoricalPriceDataReject;
                    ThrowEvent(historicalPriceDataReject, HistoricalPriceDataRejectEvent);
                    break;
                case DTCMessageType.HistoricalPriceDataRecordResponseInt:
                    ThrowEvent(messageDTC.Message as HistoricalPriceDataRecordResponse_Int, HistoricalPriceDataRecordResponseIntEvent);
                    break;
                case DTCMessageType.HistoricalPriceDataTickRecordResponseInt:
                    ThrowEvent(messageDTC.Message as HistoricalPriceDataTickRecordResponse_Int, HistoricalPriceDataTickRecordResponseIntEvent);
                    break;
                case DTCMessageType.SecurityDefinitionResponse:
                    var securityDefinitionResponse = messageDTC.Message as SecurityDefinitionResponse;
                    ThrowEvent(securityDefinitionResponse, SecurityDefinitionResponseEvent);
                    break;
                case DTCMessageType.MarketDataReject:
                    ThrowEvent(messageDTC.Message as MarketDataReject, MarketDataRejectEvent);
                    break;
                case DTCMessageType.MarketDataSnapshot:
                    ThrowEvent(messageDTC.Message as MarketDataSnapshot, MarketDataSnapshotEvent);
                    break;
                case DTCMessageType.MarketDataSnapshotInt:
                    ThrowEvent(messageDTC.Message as MarketDataSnapshot_Int, MarketDataSnapshotIntEvent);
                    break;
                case DTCMessageType.MarketDataUpdateTrade:
                    ThrowEvent(messageDTC.Message as MarketDataUpdateTrade, MarketDataUpdateTradeEvent);
                    break;
                case DTCMessageType.MarketDataUpdateTradeCompact:
                    ThrowEvent(messageDTC.Message as MarketDataUpdateTradeCompact, MarketDataUpdateTradeCompactEvent);
                    break;
                case DTCMessageType.MarketDataUpdateTradeInt:
                    ThrowEvent(messageDTC.Message as MarketDataUpdateTrade_Int, MarketDataUpdateTradeIntEvent);
                    break;
                case DTCMessageType.MarketDataUpdateLastTradeSnapshot:
                    ThrowEvent(messageDTC.Message as MarketDataUpdateLastTradeSnapshot, MarketDataUpdateLastTradeSnapshotEvent);
                    break;
                case DTCMessageType.MarketDataUpdateBidAsk:
                    ThrowEvent(messageDTC.Message as MarketDataUpdateBidAsk, MarketDataUpdateBidAskEvent);
                    break;
                case DTCMessageType.MarketDataUpdateBidAskCompact:
                    ThrowEvent(messageDTC.Message as MarketDataUpdateBidAskCompact, MarketDataUpdateBidAskCompactEvent);
                    break;
                case DTCMessageType.MarketDataUpdateBidAskInt:
                    ThrowEvent(messageDTC.Message as MarketDataUpdateBidAsk_Int, MarketDataUpdateBidAskIntEvent);
                    break;
                case DTCMessageType.MarketDataUpdateSessionOpen:
                    ThrowEvent(messageDTC.Message as MarketDataUpdateSessionOpen, MarketDataUpdateSessionOpenEvent);
                    break;
                case DTCMessageType.MarketDataUpdateSessionOpenInt:
                    ThrowEvent(messageDTC.Message as MarketDataUpdateSessionOpen_Int, MarketDataUpdateSessionOpenIntEvent);
                    break;
                case DTCMessageType.MarketDataUpdateSessionHigh:
                    ThrowEvent(messageDTC.Message as MarketDataUpdateSessionHigh, MarketDataUpdateSessionHighEvent);
                    break;
                case DTCMessageType.MarketDataUpdateSessionHighInt:
                    ThrowEvent(messageDTC.Message as MarketDataUpdateSessionHigh_Int, MarketDataUpdateSessionHighIntEvent);
                    break;
                case DTCMessageType.MarketDataUpdateSessionLow:
                    ThrowEvent(messageDTC.Message as MarketDataUpdateSessionLow, MarketDataUpdateSessionLowEvent);
                    break;
                case DTCMessageType.MarketDataUpdateSessionLowInt:
                    ThrowEvent(messageDTC.Message as MarketDataUpdateSessionLow_Int, MarketDataUpdateSessionLowIntEvent);
                    break;
                case DTCMessageType.MarketDataUpdateSessionVolume:
                    ThrowEvent(messageDTC.Message as MarketDataUpdateSessionVolume, MarketDataUpdateSessionVolumeEvent);
                    break;
                case DTCMessageType.MarketDataUpdateOpenInterest:
                    ThrowEvent(messageDTC.Message as MarketDataUpdateOpenInterest, MarketDataUpdateOpenInterestEvent);
                    break;
                case DTCMessageType.MarketDataUpdateSessionSettlement:
                    ThrowEvent(messageDTC.Message as MarketDataUpdateSessionSettlement, MarketDataUpdateSessionSettlementEvent);
                    break;
                case DTCMessageType.MarketDataUpdateSessionSettlementInt:
                    ThrowEvent(messageDTC.Message as MarketDataUpdateSessionSettlement_Int, MarketDataUpdateSessionSettlementIntEvent);
                    break;
                case DTCMessageType.MarketDataUpdateSessionNumTrades:
                    ThrowEvent(messageDTC.Message as MarketDataUpdateSessionNumTrades, MarketDataUpdateSessionNumTradesEvent);
                    break;
                case DTCMessageType.MarketDataUpdateTradingSessionDate:
                    ThrowEvent(messageDTC.Message as MarketDataUpdateTradingSessionDate, MarketDataUpdateTradingSessionDateEvent);
                    break;
                case DTCMessageType.MarketDepthReject:
                    ThrowEvent(messageDTC.Message as MarketDepthReject, MarketDepthRejectEvent);
                    break;
                case DTCMessageType.MarketDepthSnapshotLevel:
                    ThrowEvent(messageDTC.Message as MarketDepthSnapshotLevel, MarketDepthSnapshotLevelEvent);
                    break;
                case DTCMessageType.MarketDepthSnapshotLevelInt:
                    ThrowEvent(messageDTC.Message as MarketDepthSnapshotLevel_Int, MarketDepthSnapshotLevelIntEvent);
                    break;
                case DTCMessageType.MarketDepthUpdateLevel:
                    ThrowEvent(messageDTC.Message as MarketDepthUpdateLevel, MarketDepthUpdateLevelEvent);
                    break;
                case DTCMessageType.MarketDepthUpdateLevelInt:
                    ThrowEvent(messageDTC.Message as MarketDepthUpdateLevel_Int, MarketDepthUpdateLevelIntEvent);
                    break;
                case DTCMessageType.MarketDataFeedStatus:
                    ThrowEvent(messageDTC.Message as MarketDataFeedStatus, MarketDataFeedStatusEvent);
                    break;
                case DTCMessageType.MarketDataFeedSymbolStatus:
                    ThrowEvent(messageDTC.Message as MarketDataFeedSymbolStatus, MarketDataFeedSymbolStatusEvent);
                    break;
                case DTCMessageType.OpenOrdersReject:
                    ThrowEvent(messageDTC.Message as OpenOrdersReject, OpenOrdersRejectEvent);
                    break;
                case DTCMessageType.OrderUpdate:
                    ThrowEvent(messageDTC.Message as OrderUpdate, OrderUpdateEvent);
                    break;
                case DTCMessageType.HistoricalOrderFillResponse:
                    ThrowEvent(messageDTC.Message as HistoricalOrderFillResponse, HistoricalOrderFillResponseEvent);
                    break;
                case DTCMessageType.CurrentPositionsReject:
                    ThrowEvent(messageDTC.Message as CurrentPositionsReject, CurrentPositionsRejectEvent);
                    break;
                case DTCMessageType.PositionUpdate:
                    ThrowEvent(messageDTC.Message as PositionUpdate, PositionUpdateEvent);
                    break;
                case DTCMessageType.TradeAccountResponse:
                    ThrowEvent(messageDTC.Message as TradeAccountResponse, TradeAccountResponseEvent);
                    break;
                case DTCMessageType.ExchangeListResponse:
                    ThrowEvent(messageDTC.Message as ExchangeListResponse, ExchangeListResponseEvent);
                    break;
                case DTCMessageType.SecurityDefinitionReject:
                    ThrowEvent(messageDTC.Message as SecurityDefinitionReject, SecurityDefinitionRejectEvent);
                    break;
                case DTCMessageType.AccountBalanceReject:
                    ThrowEvent(messageDTC.Message as AccountBalanceReject, AccountBalanceRejectEvent);
                    break;
                case DTCMessageType.AccountBalanceUpdate:
                    ThrowEvent(messageDTC.Message as AccountBalanceUpdate, AccountBalanceUpdateEvent);
                    break;
                case DTCMessageType.UserMessage:
                    ThrowEvent(messageDTC.Message as UserMessage, UserMessageEvent);
                    break;
                case DTCMessageType.GeneralLogMessage:
                    ThrowEvent(messageDTC.Message as GeneralLogMessage, GeneralLogMessageEvent);
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Unexpected Message {messageDTC} received by {ClientName} {nameof(ProcessMessage)}.");
            }
        }

        /// <summary>
        /// This must be called whenever the encoding changes, immediately AFTER the EncodingResponse is sent to the client
        /// </summary>
        /// <param name="encoding"></param>
        private void SetCurrentCodec(EncodingEnum encoding)
        {
            if (encoding == _currentCodec?.Encoding)
            {
                return;
            }
            var oldCodec = _currentCodec;
            switch (encoding)
            {
                case EncodingEnum.BinaryEncoding:
                    _currentCodec = new CodecBinary(_networkStream);
                    break;
                case EncodingEnum.BinaryWithVariableLengthStrings:
                case EncodingEnum.JsonEncoding:
                case EncodingEnum.JsonCompactEncoding:
                    throw new NotImplementedException($"Not implemented in {nameof(Client)}: {nameof(encoding)}");
                case EncodingEnum.ProtocolBuffers:
                    _currentCodec = new CodecProtobuf(_networkStream);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(encoding), encoding, null);
            }
            if (oldCodec != null)
            {
                s_logger.Debug($"Changed codec from {oldCodec} to {_currentCodec}");
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                //s_logger.Debug("Disposing Client");
                _isDisposed = true;
                _currentCodec.Write(DTCMessageType.Logoff, new Logoff
                {
                    DoNotReconnect = 1u,
                    Reason = "Client Disposed"
                });
                _currentCodec.Dispose();
                _cts.Cancel(true);
                OnDisconnect(new Error("Disposing"));
                _semaphoreStreamReader.Dispose();
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Throw the OnDisconnect event and close down the connection (if any) to the server
        /// </summary>
        /// <param name="error"></param>
        private void OnDisconnect(Error error)
        {
            OnDisconnected(error);
            //Dispose();
        }

        public override string ToString()
        {
            return $"{ClientName} {ServerAddress} {ServerPort}";
        }
    }
}