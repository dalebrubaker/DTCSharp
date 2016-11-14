using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
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
using Timer = System.Timers.Timer;

namespace DTCClient
{
    public class Client : IDisposable
    {
        private readonly int _timeoutNoActivity;
        private Timer _timerHeartbeat;
        private bool _isDisposed;
        private BinaryWriter _binaryWriter;
        private TcpClient _tcpClient;
        private DateTime _lastHeartbeatReceivedTime;
        private NetworkStream _networkStream;
        private ICodecDTC _currentCodec;
        private readonly CancellationTokenSource _ctsResponseReader;
        private int _nextRequestId;
        private uint _nextSymbolId;
        private bool _useHeartbeat;
        private bool _isBinaryReaderZipped;

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
            _currentCodec = new CodecBinary();
            _ctsResponseReader = new CancellationTokenSource();
        }

        public bool IsConnected => _tcpClient?.Connected ?? false;

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
            var temp = Connected;
            temp?.Invoke(this, new EventArgs());
        }

        public event EventHandler<EventArgs<Error>> Disconnected;

        private void OnDisconnected(Error error)
        {
            var temp = Disconnected;
            temp?.Invoke(this, new EventArgs<Error>(error));
        }

        public event EventHandler<EventArgs<Heartbeat>> HeartbeatEvent;
        public event EventHandler<EventArgs<Logoff>> LogoffEvent;

        public event EventHandler<EventArgs<EncodingResponse>> EncodingResponseEvent;
        public event EventHandler<EventArgs<LogonResponse>> LogonResponseEvent;
        public event EventHandler<EventArgs<MarketDataReject>> MarketDataRejectEvent;
        public event EventHandler<EventArgs<MarketDataSnapshot>> MarketDataSnapshotEvent;
        public event EventHandler<EventArgs<MarketDataSnapshot_Int>> MarketDataSnapshotIntEvent;
        public event EventHandler<EventArgs<MarketDataUpdateTrade>> MarketDataUpdateTradeEvent;
        public event EventHandler<EventArgs<MarketDataUpdateTradeCompact>> MarketDataUpdateTradeCompactEvent;
        public event EventHandler<EventArgs<MarketDataUpdateTrade_Int>> MarketDataUpdateTradeIntEvent;
        public event EventHandler<EventArgs<MarketDataUpdateLastTradeSnapshot>> MarketDataUpdateLastTradeSnapshotEvent;
        public event EventHandler<EventArgs<MarketDataUpdateBidAsk>> MarketDataUpdateBidAskEvent;
        public event EventHandler<EventArgs<MarketDataUpdateBidAskCompact>> MarketDataUpdateBidAskCompactEvent;
        public event EventHandler<EventArgs<MarketDataUpdateBidAsk_Int>> MarketDataUpdateBidAskIntEvent;
        public event EventHandler<EventArgs<MarketDataUpdateSessionOpen>> MarketDataUpdateSessionOpenEvent;
        public event EventHandler<EventArgs<MarketDataUpdateSessionOpen_Int>> MarketDataUpdateSessionOpenIntEvent;
        public event EventHandler<EventArgs<MarketDataUpdateSessionHigh>> MarketDataUpdateSessionHighEvent;
        public event EventHandler<EventArgs<MarketDataUpdateSessionHigh_Int>> MarketDataUpdateSessionHighIntEvent;
        public event EventHandler<EventArgs<MarketDataUpdateSessionLow>> MarketDataUpdateSessionLowEvent;
        public event EventHandler<EventArgs<MarketDataUpdateSessionLow_Int>> MarketDataUpdateSessionLowIntEvent;
        public event EventHandler<EventArgs<MarketDataUpdateSessionVolume>> MarketDataUpdateSessionVolumeEvent;
        public event EventHandler<EventArgs<MarketDataUpdateOpenInterest>> MarketDataUpdateOpenInterestEvent;
        public event EventHandler<EventArgs<MarketDataUpdateSessionSettlement>> MarketDataUpdateSessionSettlementEvent;
        public event EventHandler<EventArgs<MarketDataUpdateSessionSettlement_Int>> MarketDataUpdateSessionSettlementIntEvent;
        public event EventHandler<EventArgs<MarketDataUpdateSessionNumTrades>> MarketDataUpdateSessionNumTradesEvent;
        public event EventHandler<EventArgs<MarketDataUpdateTradingSessionDate>> MarketDataUpdateTradingSessionDateEvent;
        public event EventHandler<EventArgs<MarketDepthReject>> MarketDepthRejectEvent;
        public event EventHandler<EventArgs<MarketDepthSnapshotLevel>> MarketDepthSnapshotLevelEvent;
        public event EventHandler<EventArgs<MarketDepthSnapshotLevel_Int>> MarketDepthSnapshotLevelIntEvent;
        public event EventHandler<EventArgs<MarketDepthUpdateLevel>> MarketDepthUpdateLevelEvent;
        public event EventHandler<EventArgs<MarketDepthUpdateLevelCompact>> MarketDepthUpdateLevelCompactEvent;
        public event EventHandler<EventArgs<MarketDepthUpdateLevel_Int>> MarketDepthUpdateLevelIntEvent;
        public event EventHandler<EventArgs<MarketDepthFullUpdate10>> MarketDepthFullUpdate10Event;
        public event EventHandler<EventArgs<MarketDepthFullUpdate20>> MarketDepthFullUpdate20Event;
        public event EventHandler<EventArgs<MarketDataFeedStatus>> MarketDataFeedStatusEvent;
        public event EventHandler<EventArgs<MarketDataFeedSymbolStatus>> MarketDataFeedSymbolStatusEvent;
        public event EventHandler<EventArgs<OpenOrdersReject>> OpenOrdersRejectEvent;
        public event EventHandler<EventArgs<OrderUpdate>> OrderUpdateEvent;
        public event EventHandler<EventArgs<HistoricalOrderFillResponse>> HistoricalOrderFillResponseEvent;
        public event EventHandler<EventArgs<CurrentPositionsReject>> CurrentPositionsRejectEvent;
        public event EventHandler<EventArgs<PositionUpdate>> PositionUpdateEvent;
        public event EventHandler<EventArgs<TradeAccountResponse>> TradeAccountResponseEvent;
        public event EventHandler<EventArgs<ExchangeListResponse>> ExchangeListResponseEvent;
        public event EventHandler<EventArgs<SecurityDefinitionResponse>> SecurityDefinitionResponseEvent;
        public event EventHandler<EventArgs<SecurityDefinitionReject>> SecurityDefinitionRejectEvent;
        public event EventHandler<EventArgs<AccountBalanceReject>> AccountBalanceRejectEvent;
        public event EventHandler<EventArgs<AccountBalanceUpdate>> AccountBalanceUpdateEvent;
        public event EventHandler<EventArgs<UserMessage>> UserMessageEvent;
        public event EventHandler<EventArgs<GeneralLogMessage>> GeneralLogMessageEvent;
        public event EventHandler<EventArgs<HistoricalPriceDataResponseHeader>> HistoricalPriceDataResponseHeaderEvent;
        public event EventHandler<EventArgs<HistoricalPriceDataReject>> HistoricalPriceDataRejectEvent;
        public event EventHandler<EventArgs<HistoricalPriceDataRecordResponse>> HistoricalPriceDataRecordResponseEvent;
        public event EventHandler<EventArgs<HistoricalPriceDataTickRecordResponse>> HistoricalPriceDataTickRecordResponseEvent;
        public event EventHandler<EventArgs<HistoricalPriceDataRecordResponse_Int>> HistoricalPriceDataRecordResponseIntEvent;
        public event EventHandler<EventArgs<HistoricalPriceDataTickRecordResponse_Int>> HistoricalPriceDataTickRecordResponseIntEvent;


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
            _tcpClient = new TcpClient {NoDelay = true};
            try
            {
                await _tcpClient.ConnectAsync(ServerAddress, ServerPort).ConfigureAwait(false); // connect to the server
            }
            catch (SocketException sex)
            {
                OnDisconnected(new Error(sex.Message));
            }
            catch (Exception)
            {
                throw;
            }
            _networkStream = _tcpClient.GetStream();
            _binaryWriter = new BinaryWriter(_networkStream);
            _currentCodec = new CodecBinary();
            TaskHelper.RunBgLong(async () =>
            {
                await ResponseReaderAsync().ConfigureAwait(false);
            });
            if (_timeoutNoActivity != 0)
            {
                _tcpClient.ReceiveTimeout = _timeoutNoActivity;
            }

            // Set up the handler to capture the event
            EncodingResponse result = null;
            EventHandler<EventArgs<EncodingResponse>> handler = null;
            handler = (s, e) =>
            {
                EncodingResponseEvent -= handler; // unregister to avoid a potential memory leak
                result = e.Data;
            };
            EncodingResponseEvent += handler;

            // Request protocol buffers encoding
            var encodingRequest = new EncodingRequest
            {
                Encoding = requestedEncoding,
                ProtocolType = "DTC",
                ProtocolVersion = (int)DTCVersion.CurrentVersion
            };

            // Give the server a bit to be able to respond
            //await Task.Delay(100).ConfigureAwait(true);
            try
            {
                SendRequest(DTCMessageType.EncodingRequest, encodingRequest);
            }
            catch (Exception ex)
            {
                throw;
            }

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
        public async Task<LogonResponse> LogonAsync(int heartbeatIntervalInSeconds, bool useHeartbeat = true,
            int timeout = 1000, string userName = "", string password = "", string generalTextData = "", int integer1 = 0, int integer2 = 0,
            TradeModeEnum tradeMode = TradeModeEnum.TradeModeUnset, string tradeAccount = "", string hardwareIdentifier = "")
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
            EventHandler<EventArgs<LogonResponse>> handler = null;
            handler = (s, e) =>
            {
                LogonResponseEvent -= handler; // unregister to avoid a potential memory leak
                result = e.Data;
            };
            LogonResponseEvent += handler;

            // Send the request
            var logonRequest = new LogonRequest
            {
                ClientName = ClientName,
                GeneralTextData = generalTextData,
                HardwareIdentifier = hardwareIdentifier,
                HeartbeatIntervalInSeconds = heartbeatIntervalInSeconds,
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
            EventHandler<EventArgs<HistoricalPriceDataReject>> handlerReject = null;
            handlerReject = (s, e) =>
            {
                HistoricalPriceDataRejectEvent -= handlerReject; // unregister to avoid a potential memory leak
                historicalPriceDataReject = e.Data;
                timeout = 0; // force immediate return
            };
            HistoricalPriceDataRejectEvent += handlerReject;

            // Set up handler to capture the header event
            EventHandler<EventArgs<HistoricalPriceDataResponseHeader>> handlerHeader = null;
            handlerHeader = (s, e) =>
            {
                HistoricalPriceDataResponseHeaderEvent -= handlerHeader; // unregister to avoid a potential memory leak
                var header = e.Data;
                headerCallback(header);
                timeout = int.MaxValue; // wait for the last price data response to arrive
            };
            HistoricalPriceDataResponseHeaderEvent += handlerHeader;

            // Set up the handler to capture the HistoricalPriceDataRecordResponseEvent
            HistoricalPriceDataRecordResponse response;
            EventHandler<EventArgs<HistoricalPriceDataRecordResponse>> handler = null;
            handler = (s, e) =>
            {
                response = e.Data;
                dataCallback(response);
                if (e.Data.IsFinalRecord != 0)
                {
                    HistoricalPriceDataRecordResponseEvent -= handler; // unregister to avoid a potential memory leak
                    timeout = 0; // force immediate exit
                }
            };
            HistoricalPriceDataRecordResponseEvent += handler;

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
                Flag1 = flag1 ? 1U : 0,
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
            EventHandler<EventArgs<SecurityDefinitionResponse>> handler = null;
            handler = (s, e) =>
            {
                SecurityDefinitionResponseEvent -= handler; // unregister to avoid a potential memory leak
                result = e.Data;
            };
            SecurityDefinitionResponseEvent += handler;

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
            string combo;
            SymbolExchangeComboBySymbolId.TryGetValue(symbolId, out combo);
            SplitSymbolExchange(combo, out symbol, out exchange);
        }

        /// <summary>
        /// Get the SymbolId for symbol and exchange, adding it if it doesn't already exist
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="exchange"></param>
        /// <returns></returns>
        public uint RequireSymbolId(string symbol, string exchange)
        {
            var combo = CombineSymbolExchange(symbol, exchange);
            uint symbolId;
            if (!SymbolIdBySymbolExchangeCombo.TryGetValue(combo, out symbolId))
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
        public async Task<MarketDataReject> GetMarketDataUpdateTradeCompactAsync(CancellationToken cancellationToken, int timeout, string symbol, string exchange,
            Action<MarketDataSnapshot> snapshotCallback, Action<MarketDataUpdateTradeCompact> tradeCallback,
            Action<MarketDataUpdateBidAskCompact> bidAskCallback = null,
            Action<MarketDataUpdateSessionOpen> sessionOpenCallback = null,
            Action<MarketDataUpdateSessionHigh> sessionHighCallback = null,
            Action<MarketDataUpdateSessionLow> sessionLowCallback = null,
            Action<MarketDataUpdateSessionSettlement> sessionSettlementCallback = null,
            Action<MarketDataUpdateSessionVolume> sessionVolumeCallback = null,
            Action<MarketDataUpdateOpenInterest> openInterestCallback = null
        )
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
            EventHandler<EventArgs<MarketDataReject>> handlerReject = null;
            handlerReject = (s, e) =>
            {
                MarketDataRejectEvent -= handlerReject; // unregister to avoid a potential memory leak
                marketDataReject = e.Data;
                timeout = 0; // force immediate return
            };

            using (var handleSnapshot = new EventToCallbackForSymbol<MarketDataSnapshot>(symbolId, symbol, exchange,
                handler => MarketDataSnapshotEvent += handler, handler => MarketDataSnapshotEvent -= handler, snapshotCallback))
            using (var handleTrade = new EventToCallbackForSymbol<MarketDataUpdateTradeCompact>(symbolId, symbol, exchange,
                handler => MarketDataUpdateTradeCompactEvent += handler, handler => MarketDataUpdateTradeCompactEvent -= handler, tradeCallback))
            using (var handleBidAsk = new EventToCallbackForSymbol<MarketDataUpdateBidAskCompact>(symbolId, symbol, exchange,
                handler => MarketDataUpdateBidAskCompactEvent += handler, handler => MarketDataUpdateBidAskCompactEvent -= handler, bidAskCallback))
            using (var handleSessionOpen = new EventToCallbackForSymbol<MarketDataUpdateSessionOpen>(symbolId, symbol, exchange,
                handler => MarketDataUpdateSessionOpenEvent += handler, handler => MarketDataUpdateSessionOpenEvent -= handler, sessionOpenCallback))
            using (var handleSessionHigh = new EventToCallbackForSymbol<MarketDataUpdateSessionHigh>(symbolId, symbol, exchange,
                handler => MarketDataUpdateSessionHighEvent += handler, handler => MarketDataUpdateSessionHighEvent -= handler, sessionHighCallback))
            using (var handleSessionLow = new EventToCallbackForSymbol<MarketDataUpdateSessionLow>(symbolId, symbol, exchange,
                handler => MarketDataUpdateSessionLowEvent += handler, handler => MarketDataUpdateSessionLowEvent -= handler, sessionLowCallback))
            using (var handleSessionSettlement = new EventToCallbackForSymbol<MarketDataUpdateSessionSettlement>(symbolId, symbol, exchange,
                handler => MarketDataUpdateSessionSettlementEvent += handler, handler => MarketDataUpdateSessionSettlementEvent -= handler, sessionSettlementCallback))
            using (var handleSessionVolume = new EventToCallbackForSymbol<MarketDataUpdateSessionVolume>(symbolId, symbol, exchange,
                handler => MarketDataUpdateSessionVolumeEvent += handler, handler => MarketDataUpdateSessionVolumeEvent -= handler, sessionVolumeCallback))
            using (var handleOpenInterest = new EventToCallbackForSymbol<MarketDataUpdateOpenInterest>(symbolId, symbol, exchange,
                handler => MarketDataUpdateOpenInterestEvent += handler, handler => MarketDataUpdateOpenInterestEvent -= handler, openInterestCallback))
            {
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
                while ((DateTime.Now - startTime).TotalMilliseconds < timeout
                       && !cancellationToken.IsCancellationRequested)
                {
                    if (handleSnapshot.IsDataReceived || handleTrade.IsDataReceived)
                    {
                        // We're receiving data, so never timeout
                        timeout = int.MaxValue;
                    }
                    await Task.Delay(1, cancellationToken).ConfigureAwait(false);
                }
                return marketDataReject;
            }
        }

        public void SendRequest<T>(DTCMessageType messageType, T message) where T : IMessage
        {
#if DEBUG
            if (messageType != DTCMessageType.Heartbeat)
            {
                var debug = 1;
            }
            var messageStr = $"{messageType} {_currentCodec}";
            DebugHelpers.RequestsSent.Add(messageStr);
#endif
            try
            {
                _currentCodec.Write(messageType, message, _binaryWriter);
            }
            catch (Exception ex)
            {
                var error = new Error("Unable to send request", ex);
                Disconnect(error);
            }
        }

        private void ThrowEventImpl<T>(T message, EventHandler<EventArgs<T>> eventForMessage) where T : IMessage
        {
            var temp = eventForMessage; // for thread safety
            temp?.Invoke(this, new EventArgs<T>(message));
        }

        private void ThrowEvent<T>(T message, EventHandler<EventArgs<T>> eventForMessage) where T : IMessage
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
        private Task ResponseReaderAsync()
        {
            var binaryReader = new BinaryReader(_networkStream); // Note that binaryReader may be redefined below in HistoricalPriceDataResponseHeader
            _isBinaryReaderZipped = false;
            while (!_ctsResponseReader.Token.IsCancellationRequested)
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
                    var messageStr = $"{messageType} {_currentCodec} zipped:{_isBinaryReaderZipped} size:{size}";
                    DebugHelpers.ResponsesReceived.Add(messageStr);
#endif
                    var messageBytes = binaryReader.ReadBytes(size - 4); // size included the header size+type
                    ProcessResponse(messageType, messageBytes, ref binaryReader);
                }
                catch (IOException ex)
                {
                    // Ignore this if it results from disconnect (cancellation)
                    if (!_ctsResponseReader.Token.IsCancellationRequested)
                    {
                        Disconnect(new Error("Read error.", ex));
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
        /// binaryReader may be changed to use a new DeflateStream if we change to zipped
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="messageBytes"></param>
        /// <param name="binaryReader">may be changed to use a new DeflateStream if we change to zipped</param>
        private void ProcessResponse(DTCMessageType messageType, byte[] messageBytes, ref BinaryReader binaryReader)
        {
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
                    ThrowEvent(logoff, LogoffEvent);
                    break;
                case DTCMessageType.EncodingResponse:
                    // Note that we must use binary encoding here on the first usage after connect, 
                    //    per http://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#EncodingRequest
                    var encodingResponse = _currentCodec.Load<EncodingResponse>(messageType, messageBytes);
                    switch (encodingResponse.Encoding)
                    {
                        case EncodingEnum.BinaryEncoding:
                            _currentCodec = new CodecBinary();
                            break;
                        case EncodingEnum.BinaryWithVariableLengthStrings:
                        case EncodingEnum.JsonEncoding:
                        case EncodingEnum.JsonCompactEncoding:
                            throw new NotImplementedException($"Not implemented in {nameof(ProcessResponse)}: {nameof(encodingResponse.Encoding)}");
                        case EncodingEnum.ProtocolBuffers:
                            _currentCodec = new CodecProtobuf();
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
                case DTCMessageType.MarketDepthUpdateLevelCompact:
                    var marketDepthUpdateLevelCompact = _currentCodec.Load<MarketDepthUpdateLevelCompact>(messageType, messageBytes);
                    ThrowEvent(marketDepthUpdateLevelCompact, MarketDepthUpdateLevelCompactEvent);
                    break;
                case DTCMessageType.MarketDepthUpdateLevelInt:
                    var marketDepthUpdateLevelInt = _currentCodec.Load<MarketDepthUpdateLevel_Int>(messageType, messageBytes);
                    ThrowEvent(marketDepthUpdateLevelInt, MarketDepthUpdateLevelIntEvent);
                    break;
                case DTCMessageType.MarketDepthFullUpdate10:
                    var marketDepthFullUpdate10 = _currentCodec.Load<MarketDepthFullUpdate10>(messageType, messageBytes);
                    ThrowEvent(marketDepthFullUpdate10, MarketDepthFullUpdate10Event);
                    break;
                case DTCMessageType.MarketDepthFullUpdate20:
                    var marketDepthFullUpdate20 = _currentCodec.Load<MarketDepthFullUpdate20>(messageType, messageBytes);
                    ThrowEvent(marketDepthFullUpdate20, MarketDepthFullUpdate20Event);
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
                        if (_useHeartbeat)
                        {
                            throw new DTCSharpException("Heartbeat cannot co-exist with compression.");
                        }
                        // Skip past the 2-byte header. See https://tools.ietf.org/html/rfc1950
                        var zlibCmf = binaryReader.ReadByte(); // 120 = 0111 1000 means Deflate 
                        if (zlibCmf != 120)
                        {
                            throw new DTCSharpException($"Unexpected zlibCmf header byte {zlibCmf}, expected 120");
                        }
                        var zlibFlg = binaryReader.ReadByte(); // 156 = 1001 1100
                        if (zlibFlg != 156)
                        {
                            throw new DTCSharpException($"Unexpected zlibFlg header byte {zlibFlg}, expected 156");
                        }
                        var deflateStream = new DeflateStream(_networkStream, CompressionMode.Decompress);
                        binaryReader = new BinaryReader(deflateStream);
                        _isBinaryReaderZipped = true;
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
                default:
#if DEBUG
                    var requestsSent = DebugHelpers.RequestsSent;
                    var requestsReceived = DebugHelpers.RequestsReceived;
                    var responsesReceived = DebugHelpers.ResponsesReceived;
                    var responsesSent = DebugHelpers.ResponsesSent;
#endif
                    throw new ArgumentOutOfRangeException($"Unexpected MessageType {messageType} received by {ClientName} {nameof(ProcessResponse)}.");
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
                Disconnect(new Error("Disposing"));
                _isDisposed = true;
            }
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
            _networkStream?.Close();
            _networkStream?.Dispose();
            _networkStream = null;
            _binaryWriter?.Dispose();
            _binaryWriter = null;
            _tcpClient?.Close();
            _tcpClient = null;
        }

        public override string ToString()
        {
            return $"{ClientName} {ServerAddress} {ServerPort}";
        }

    }
}
