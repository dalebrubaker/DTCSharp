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

        private readonly int _timeoutNoActivity;
        private Timer _timerHeartbeat;
        private bool _isDisposed;
        private TcpClient _tcpClient;
        private DateTime _lastHeartbeatReceivedTime;
        private NetworkStream _networkStream;
        private Codec _currentCodec;
        private CancellationTokenSource _ctsResponseReader;
        private int _nextRequestId;
        private uint _nextSymbolId;
        private bool _useHeartbeat;
        private bool _isConnected;
        private ConfiguredTaskAwaitable _tasKReceiveLoop;

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
            SymbolIdBySymbolExchangeCombo = new ConcurrentDictionary<string, uint>();
            SymbolExchangeComboBySymbolId = new ConcurrentDictionary<uint, string>();
        }

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

        /// <summary>
        /// See http://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#SymbolIDRequestIDRules
        /// This is auto-incrementing
        /// </summary>
        public uint NextSymbolId => ++_nextSymbolId;

        /// <summary>
        /// Key is "Symbol|Exchange built by Get
        /// </summary>
        public ConcurrentDictionary<string, uint> SymbolIdBySymbolExchangeCombo { get; set; }

        public ConcurrentDictionary<uint, string> SymbolExchangeComboBySymbolId { get; set; }

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
            // Every Codec must write the encoding request as binary
            _networkStream = _tcpClient.GetStream();
            _currentCodec = new CodecProtobuf(_networkStream, ClientOrServer.Client);
            _ctsResponseReader = new CancellationTokenSource();
            _tasKReceiveLoop = Task.Factory.StartNew(ResponseReader, _ctsResponseReader.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current)
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
        /// <param name="cancellationToken"></param>
        /// <returns>rejection, or null if not rejected</returns>
        public async Task<HistoricalPriceDataReject> GetHistoricalPriceDataRecordResponsesAsync(string symbol, string exchange,
            HistoricalDataIntervalEnum recordInterval, DateTime startDateTimeUtc, DateTime endDateTimeUtc, uint maxDaysToReturn, bool useZLibCompression,
            bool requestDividendAdjustedStockData, bool flag1, Action<HistoricalPriceDataResponseHeader> headerCallback,
            Action<HistoricalPriceDataRecordResponse> dataCallback, CancellationToken cancellationToken = default(CancellationToken))
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

        /// <summary>
        /// Get the symbol and exchange for symbolId, or null if not found
        /// </summary>
        /// <param name="symbolId"></param>
        /// <param name="symbol"></param>
        /// <param name="exchange"></param>
        public void GetSymbolExchangeForSymbolId(uint symbolId, out string symbol, out string exchange)
        {
            SymbolExchangeComboBySymbolId.TryGetValue(symbolId, out var combo);
            SplitSymbolExchange(combo, out symbol, out exchange);
        }

        /// <summary>
        /// Get the SymbolId for symbol and exchange, adding it if it doesn't already exist
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="exchange"></param>
        /// <returns></returns>
        private uint RequireSymbolId(string symbol, string exchange)
        {
            var combo = CombineSymbolExchange(symbol, exchange);
            if (!SymbolIdBySymbolExchangeCombo.TryGetValue(combo, out var symbolId))
            {
                symbolId = NextSymbolId;
                SymbolIdBySymbolExchangeCombo[combo] = symbolId;
                SymbolExchangeComboBySymbolId[symbolId] = combo;
            }
            return symbolId;
        }

        /// <summary>
        /// Request market data for symbol|exchange
        /// Add a symbolId if not already assigned 
        /// This is done for you within GetMarketDataUpdateTradeCompact()
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="exchange"></param>
        /// <returns>the symbol ID for symbol|exchange, or 0 if not logged on or market data is not supported</returns>
        public uint SubscribeMarketData(string symbol, string exchange)
        {
            if (LogonResponse == null || LogonResponse.MarketDataSupported == 0)
            {
                return 0;
            }
            uint symbolId = RequireSymbolId(symbol, exchange);
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
        /// <returns>rejection, or null if not rejected</returns>
        public async Task<MarketDataReject> GetMarketDataUpdateTradeCompactAsync(CancellationToken cancellationToken, int timeout, string symbol,
            string exchange, Action<MarketDataSnapshot> snapshotCallback, Action<MarketDataUpdateTradeCompact> tradeCallback,
            Action<MarketDataUpdateBidAskCompact> bidAskCallback = null, Action<MarketDataUpdateSessionOpen> sessionOpenCallback = null,
            Action<MarketDataUpdateSessionHigh> sessionHighCallback = null, Action<MarketDataUpdateSessionLow> sessionLowCallback = null,
            Action<MarketDataUpdateSessionSettlement> sessionSettlementCallback = null, Action<MarketDataUpdateSessionVolume> sessionVolumeCallback = null,
            Action<MarketDataUpdateOpenInterest> openInterestCallback = null)
        {
            var symbolId = RequireSymbolId(symbol, exchange);
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
                bidAskCallback(e);
            }

            this.MarketDataUpdateBidAskCompactEvent += MarketDataUpdateBidAskCompactEvent;

            void MarketDataUpdateSessionOpenEvent(object sender, MarketDataUpdateSessionOpen e)
            {
                isDataReceived = true;
                sessionOpenCallback(e);
            }

            this.MarketDataUpdateSessionOpenEvent += MarketDataUpdateSessionOpenEvent;

            void MarketDataUpdateSessionHighEvent(object sender, MarketDataUpdateSessionHigh e)
            {
                isDataReceived = true;
                sessionHighCallback(e);
            }

            this.MarketDataUpdateSessionHighEvent += MarketDataUpdateSessionHighEvent;

            void MarketDataUpdateSessionLowEvent(object sender, MarketDataUpdateSessionLow e)
            {
                isDataReceived = true;
                sessionLowCallback(e);
            }

            this.MarketDataUpdateSessionLowEvent += MarketDataUpdateSessionLowEvent;

            void MarketDataUpdateSessionSettlementEvent(object sender, MarketDataUpdateSessionSettlement e)
            {
                isDataReceived = true;
                sessionSettlementCallback(e);
            }

            this.MarketDataUpdateSessionSettlementEvent += MarketDataUpdateSessionSettlementEvent;

            void MarketDataUpdateSessionVolumeEvent(object sender, MarketDataUpdateSessionVolume e)
            {
                isDataReceived = true;
                sessionVolumeCallback(e);
            }

            this.MarketDataUpdateSessionVolumeEvent += MarketDataUpdateSessionVolumeEvent;

            void MarketDataUpdateOpenInterestEvent(object sender, MarketDataUpdateOpenInterest e)
            {
                isDataReceived = true;
                openInterestCallback(e);
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

        public void SendRequest<T>(DTCMessageType messageType, T message) where T : IMessage
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

        private void ThrowEventImpl<T>(T message, EventHandler<T> eventForMessage)
        {
            var temp = eventForMessage;
            temp?.Invoke(this, message);
        }

        private void ThrowEvent<T>(T message, EventHandler<T> eventForMessage) where T : IMessage
        {
            //if (_stayOnCallingThread)
            //{
            //    var task = new Task(() => ThrowEventImpl(message, eventForMessage));
            //    task.RunSynchronously(_taskSchedulerCurrContext);
            //}
            //else
            //{
            ThrowEventImpl(message, eventForMessage);
            //}
        }

        /// <summary>
        /// This message runs in a continuous loop on its own thread, throwing events as messages are received.
        /// </summary>
        private void ResponseReader()
        {
            while (!_ctsResponseReader.Token.IsCancellationRequested)
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
                    if (_ctsResponseReader.Token.IsCancellationRequested)
                    {
                        Disconnect(new Error("Read error.", ex));
                        return;
                    }
                    s_logger.Error(ex, ex.Message);
                    throw;
                }
                catch (Exception ex)
                {
                    var typeName = ex.GetType().Name;
                    Disconnect(new Error($"Read error {typeName}.", ex));
                    s_logger.Error(ex, ex.Message);
                }
            }
        }

        /// <summary>
        /// Process the message represented by bytes.
        /// binaryReader may be changed to use a new DeflateStream if we change to zipped
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="messageBytes"></param>
        private void ProcessResponseBytes(DTCMessageType messageType, byte[] messageBytes)
        {
            if (messageType == DTCMessageType.MarketDataUpdateBidAsk)
            {
            }

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
                    var marketDataReject = _currentCodec.Load<MarketDataReject>(messageType, messageBytes);
                    ThrowEvent(marketDataReject, MarketDataRejectEvent);
                    break;
                case DTCMessageType.MarketDataSnapshot:
                    var marketDataSnapshot = _currentCodec.Load<MarketDataSnapshot>(messageType, messageBytes);
                    ThrowEvent(marketDataSnapshot, MarketDataSnapshotEvent);
                    break;
                case DTCMessageType.MarketDataSnapshotInt:
                    var marketDataSnapshotInt = _currentCodec.Load<MarketDataSnapshot_Int>(messageType, messageBytes);
                    ThrowEvent(marketDataSnapshotInt, MarketDataSnapshotIntEvent);
                    break;
                case DTCMessageType.MarketDataUpdateTrade:
                    var marketDataUpdateTrade = _currentCodec.Load<MarketDataUpdateTrade>(messageType, messageBytes);
                    ThrowEvent(marketDataUpdateTrade, MarketDataUpdateTradeEvent);
                    break;
                case DTCMessageType.MarketDataUpdateTradeCompact:
                    var marketDataUpdateTradeCompact = _currentCodec.Load<MarketDataUpdateTradeCompact>(messageType, messageBytes);
                    ThrowEvent(marketDataUpdateTradeCompact, MarketDataUpdateTradeCompactEvent);
                    break;
                case DTCMessageType.MarketDataUpdateTradeInt:
                    var marketDataUpdateTradeInt = _currentCodec.Load<MarketDataUpdateTrade_Int>(messageType, messageBytes);
                    ThrowEvent(marketDataUpdateTradeInt, MarketDataUpdateTradeIntEvent);
                    break;
                case DTCMessageType.MarketDataUpdateLastTradeSnapshot:
                    var marketDataUpdateLastTradeSnapshot = _currentCodec.Load<MarketDataUpdateLastTradeSnapshot>(messageType, messageBytes);
                    ThrowEvent(marketDataUpdateLastTradeSnapshot, MarketDataUpdateLastTradeSnapshotEvent);
                    break;
                case DTCMessageType.MarketDataUpdateBidAsk:
                    var marketDataUpdateBidAsk = _currentCodec.Load<MarketDataUpdateBidAsk>(messageType, messageBytes);
                    ThrowEvent(marketDataUpdateBidAsk, MarketDataUpdateBidAskEvent);
                    break;
                case DTCMessageType.MarketDataUpdateBidAskCompact:
                    var marketDataUpdateBidAskCompact = _currentCodec.Load<MarketDataUpdateBidAskCompact>(messageType, messageBytes);
                    //s_logger.Debug("Throwing a MarketDataUpdateBidAskCompactEvent");
                    ThrowEvent(marketDataUpdateBidAskCompact, MarketDataUpdateBidAskCompactEvent);
                    break;
                case DTCMessageType.MarketDataUpdateBidAskInt:
                    var marketDataUpdateBidAskInt = _currentCodec.Load<MarketDataUpdateBidAsk_Int>(messageType, messageBytes);
                    ThrowEvent(marketDataUpdateBidAskInt, MarketDataUpdateBidAskIntEvent);
                    break;
                case DTCMessageType.MarketDataUpdateSessionOpen:
                    var marketDataUpdateSessionOpen = _currentCodec.Load<MarketDataUpdateSessionOpen>(messageType, messageBytes);
                    ThrowEvent(marketDataUpdateSessionOpen, MarketDataUpdateSessionOpenEvent);
                    break;
                case DTCMessageType.MarketDataUpdateSessionOpenInt:
                    var marketDataUpdateSessionOpenInt = _currentCodec.Load<MarketDataUpdateSessionOpen_Int>(messageType, messageBytes);
                    ThrowEvent(marketDataUpdateSessionOpenInt, MarketDataUpdateSessionOpenIntEvent);
                    break;
                case DTCMessageType.MarketDataUpdateSessionHigh:
                    var marketDataUpdateSessionHigh = _currentCodec.Load<MarketDataUpdateSessionHigh>(messageType, messageBytes);
                    ThrowEvent(marketDataUpdateSessionHigh, MarketDataUpdateSessionHighEvent);
                    break;
                case DTCMessageType.MarketDataUpdateSessionHighInt:
                    var marketDataUpdateSessionHighInt = _currentCodec.Load<MarketDataUpdateSessionHigh_Int>(messageType, messageBytes);
                    ThrowEvent(marketDataUpdateSessionHighInt, MarketDataUpdateSessionHighIntEvent);
                    break;
                case DTCMessageType.MarketDataUpdateSessionLow:
                    var marketDataUpdateSessionLow = _currentCodec.Load<MarketDataUpdateSessionLow>(messageType, messageBytes);
                    ThrowEvent(marketDataUpdateSessionLow, MarketDataUpdateSessionLowEvent);
                    break;
                case DTCMessageType.MarketDataUpdateSessionLowInt:
                    var marketDataUpdateSessionLowInt = _currentCodec.Load<MarketDataUpdateSessionLow_Int>(messageType, messageBytes);
                    ThrowEvent(marketDataUpdateSessionLowInt, MarketDataUpdateSessionLowIntEvent);
                    break;
                case DTCMessageType.MarketDataUpdateSessionVolume:
                    var marketDataUpdateSessionVolume = _currentCodec.Load<MarketDataUpdateSessionVolume>(messageType, messageBytes);
                    ThrowEvent(marketDataUpdateSessionVolume, MarketDataUpdateSessionVolumeEvent);
                    break;
                case DTCMessageType.MarketDataUpdateOpenInterest:
                    var marketDataUpdateOpenInterest = _currentCodec.Load<MarketDataUpdateOpenInterest>(messageType, messageBytes);
                    ThrowEvent(marketDataUpdateOpenInterest, MarketDataUpdateOpenInterestEvent);
                    break;
                case DTCMessageType.MarketDataUpdateSessionSettlement:
                    var marketDataUpdateSessionSettlement = _currentCodec.Load<MarketDataUpdateSessionSettlement>(messageType, messageBytes);
                    ThrowEvent(marketDataUpdateSessionSettlement, MarketDataUpdateSessionSettlementEvent);
                    break;
                case DTCMessageType.MarketDataUpdateSessionSettlementInt:
                    var marketDataUpdateSessionSettlementInt = _currentCodec.Load<MarketDataUpdateSessionSettlement_Int>(messageType, messageBytes);
                    ThrowEvent(marketDataUpdateSessionSettlementInt, MarketDataUpdateSessionSettlementIntEvent);
                    break;
                case DTCMessageType.MarketDataUpdateSessionNumTrades:
                    var marketDataUpdateSessionNumTrades = _currentCodec.Load<MarketDataUpdateSessionNumTrades>(messageType, messageBytes);
                    ThrowEvent(marketDataUpdateSessionNumTrades, MarketDataUpdateSessionNumTradesEvent);
                    break;
                case DTCMessageType.MarketDataUpdateTradingSessionDate:
                    var marketDataUpdateTradingSessionDate = _currentCodec.Load<MarketDataUpdateTradingSessionDate>(messageType, messageBytes);
                    ThrowEvent(marketDataUpdateTradingSessionDate, MarketDataUpdateTradingSessionDateEvent);
                    break;
                case DTCMessageType.MarketDepthReject:
                    var marketDepthReject = _currentCodec.Load<MarketDepthReject>(messageType, messageBytes);
                    ThrowEvent(marketDepthReject, MarketDepthRejectEvent);
                    break;
                case DTCMessageType.MarketDepthSnapshotLevel:
                    var marketDepthSnapshotLevel = _currentCodec.Load<MarketDepthSnapshotLevel>(messageType, messageBytes);
                    ThrowEvent(marketDepthSnapshotLevel, MarketDepthSnapshotLevelEvent);
                    break;
                case DTCMessageType.MarketDepthSnapshotLevelInt:
                    var marketDepthSnapshotLevelInt = _currentCodec.Load<MarketDepthSnapshotLevel_Int>(messageType, messageBytes);
                    ThrowEvent(marketDepthSnapshotLevelInt, MarketDepthSnapshotLevelIntEvent);
                    break;
                case DTCMessageType.MarketDepthUpdateLevel:
                    var marketDepthUpdateLevel = _currentCodec.Load<MarketDepthUpdateLevel>(messageType, messageBytes);
                    ThrowEvent(marketDepthUpdateLevel, MarketDepthUpdateLevelEvent);
                    break;
                case DTCMessageType.MarketDepthUpdateLevelInt:
                    var marketDepthUpdateLevelInt = _currentCodec.Load<MarketDepthUpdateLevel_Int>(messageType, messageBytes);
                    ThrowEvent(marketDepthUpdateLevelInt, MarketDepthUpdateLevelIntEvent);
                    break;
                case DTCMessageType.MarketDataFeedStatus:
                    var marketDataFeedStatus = _currentCodec.Load<MarketDataFeedStatus>(messageType, messageBytes);
                    ThrowEvent(marketDataFeedStatus, MarketDataFeedStatusEvent);
                    break;
                case DTCMessageType.MarketDataFeedSymbolStatus:
                    var marketDataFeedSymbolStatus = _currentCodec.Load<MarketDataFeedSymbolStatus>(messageType, messageBytes);
                    ThrowEvent(marketDataFeedSymbolStatus, MarketDataFeedSymbolStatusEvent);
                    break;
                case DTCMessageType.OpenOrdersReject:
                    var openOrdersReject = _currentCodec.Load<OpenOrdersReject>(messageType, messageBytes);
                    ThrowEvent(openOrdersReject, OpenOrdersRejectEvent);
                    break;
                case DTCMessageType.OrderUpdate:
                    var orderUpdate = _currentCodec.Load<OrderUpdate>(messageType, messageBytes);
                    ThrowEvent(orderUpdate, OrderUpdateEvent);
                    break;
                case DTCMessageType.HistoricalOrderFillResponse:
                    var historicalOrderFillResponse = _currentCodec.Load<HistoricalOrderFillResponse>(messageType, messageBytes);
                    ThrowEvent(historicalOrderFillResponse, HistoricalOrderFillResponseEvent);
                    break;
                case DTCMessageType.CurrentPositionsReject:
                    var currentPositionsReject = _currentCodec.Load<CurrentPositionsReject>(messageType, messageBytes);
                    ThrowEvent(currentPositionsReject, CurrentPositionsRejectEvent);
                    break;
                case DTCMessageType.PositionUpdate:
                    var positionUpdate = _currentCodec.Load<PositionUpdate>(messageType, messageBytes);
                    ThrowEvent(positionUpdate, PositionUpdateEvent);
                    break;
                case DTCMessageType.TradeAccountResponse:
                    var tradeAccountResponse = _currentCodec.Load<TradeAccountResponse>(messageType, messageBytes);
                    ThrowEvent(tradeAccountResponse, TradeAccountResponseEvent);
                    break;
                case DTCMessageType.ExchangeListResponse:
                    var exchangeListResponse = _currentCodec.Load<ExchangeListResponse>(messageType, messageBytes);
                    ThrowEvent(exchangeListResponse, ExchangeListResponseEvent);
                    break;
                case DTCMessageType.SecurityDefinitionResponse:
                    var securityDefinitionResponse = _currentCodec.Load<SecurityDefinitionResponse>(messageType, messageBytes);
                    ThrowEvent(securityDefinitionResponse, SecurityDefinitionResponseEvent);
                    break;
                case DTCMessageType.SecurityDefinitionReject:
                    var securityDefinitionReject = _currentCodec.Load<SecurityDefinitionReject>(messageType, messageBytes);
                    ThrowEvent(securityDefinitionReject, SecurityDefinitionRejectEvent);
                    break;
                case DTCMessageType.AccountBalanceReject:
                    var accountBalanceReject = _currentCodec.Load<AccountBalanceReject>(messageType, messageBytes);
                    ThrowEvent(accountBalanceReject, AccountBalanceRejectEvent);
                    break;
                case DTCMessageType.AccountBalanceUpdate:
                    var accountBalanceUpdate = _currentCodec.Load<AccountBalanceUpdate>(messageType, messageBytes);
                    ThrowEvent(accountBalanceUpdate, AccountBalanceUpdateEvent);
                    break;
                case DTCMessageType.UserMessage:
                    var userMessage = _currentCodec.Load<UserMessage>(messageType, messageBytes);
                    ThrowEvent(userMessage, UserMessageEvent);
                    break;
                case DTCMessageType.GeneralLogMessage:
                    var generalLogMessage = _currentCodec.Load<GeneralLogMessage>(messageType, messageBytes);
                    ThrowEvent(generalLogMessage, GeneralLogMessageEvent);
                    break;
                case DTCMessageType.HistoricalPriceDataResponseHeader:
                    var historicalPriceDataResponseHeader = _currentCodec.Load<HistoricalPriceDataResponseHeader>(messageType, messageBytes);
                    if (historicalPriceDataResponseHeader.UseZLibCompression == 1)
                    {
                        // Skip past the 2-byte header. See https://tools.ietf.org/html/rfc1950
                        s_logger.Debug($"{nameof(Client)}.{nameof(ProcessResponseBytes)} is switching client stream to read zipped.");

                        _currentCodec.ReadSwitchToZipped();
                        _useHeartbeat = false;
                    }
                    ThrowEvent(historicalPriceDataResponseHeader, HistoricalPriceDataResponseHeaderEvent);
                    break;
                case DTCMessageType.HistoricalPriceDataReject:
                    var historicalPriceDataReject = _currentCodec.Load<HistoricalPriceDataReject>(messageType, messageBytes);
                    ThrowEvent(historicalPriceDataReject, HistoricalPriceDataRejectEvent);
                    break;
                case DTCMessageType.HistoricalPriceDataRecordResponse:
                    var historicalPriceDataRecordResponse = _currentCodec.Load<HistoricalPriceDataRecordResponse>(messageType, messageBytes);
                    ThrowEvent(historicalPriceDataRecordResponse, HistoricalPriceDataRecordResponseEvent);
#if DEBUG
                    var requestsSent2 = DebugHelpers.RequestsSent;
                    var requestsReceived2 = DebugHelpers.RequestsReceived;
                    var responsesReceived2 = DebugHelpers.ResponsesReceived;
                    var responsesSent2 = DebugHelpers.ResponsesSent;
#endif
                    break;
                case DTCMessageType.HistoricalPriceDataTickRecordResponse:
                    var historicalPriceDataTickRecordResponse = _currentCodec.Load<HistoricalPriceDataTickRecordResponse>(messageType, messageBytes);
                    ThrowEvent(historicalPriceDataTickRecordResponse, HistoricalPriceDataTickRecordResponseEvent);
                    break;
                case DTCMessageType.HistoricalPriceDataRecordResponseInt:
                    var historicalPriceDataRecordResponseInt = _currentCodec.Load<HistoricalPriceDataRecordResponse_Int>(messageType, messageBytes);
                    ThrowEvent(historicalPriceDataRecordResponseInt, HistoricalPriceDataRecordResponseIntEvent);
                    break;
                case DTCMessageType.HistoricalPriceDataTickRecordResponseInt:
                    var historicalPriceDataTickRecordResponseInt = _currentCodec.Load<HistoricalPriceDataTickRecordResponse_Int>(messageType, messageBytes);
                    ThrowEvent(historicalPriceDataTickRecordResponseInt, HistoricalPriceDataTickRecordResponseIntEvent);
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
                case DTCMessageType.HistoricalMarketDepthDataResponseHeader:
                case DTCMessageType.HistoricalMarketDepthDataReject:
                case DTCMessageType.HistoricalMarketDepthDataRecordResponse:
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
                _ctsResponseReader.Cancel();
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
        private void Disconnect(Error error)
        {
            OnDisconnected(error);

            _timerHeartbeat?.Dispose();
            _ctsResponseReader?.Cancel();
            _tcpClient?.Close();
            _tcpClient = null;
        }

        public override string ToString()
        {
            return $"{ClientName} {ServerAddress} {ServerPort}";
        }
    }
}