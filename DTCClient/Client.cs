using System;
using System.Threading;
using System.Threading.Tasks;
using DTCCommon;
using DTCCommon.Extensions;
using DTCPB;

namespace DTCClient
{
    public class Client : ClientBase
    {
        /// <summary>
        /// Constructor for a client
        /// </summary>
        /// <param name="serverAddress">the machine name or an IP address for the server to which we want to connect</param>
        /// <param name="serverPort">the port for the server to which we want to connect</param>
        /// <param name="timeoutNoActivity">milliseconds timeout to assume disconnected if no activity. Set to 0 for Infinite</param>
        public Client(string serverAddress, int serverPort, int timeoutNoActivity) : base(serverAddress, serverPort, timeoutNoActivity)
        {
        }

        #region events

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
            var timeout = TimeoutNoActivity;
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
        protected override void ProcessMessage(MessageDTC messageDTC)
        {
            //s_logger.Debug($"{nameof(ProcessResponseBytes)} is processing {messageType}");\
            switch (messageDTC.MessageType)
            {
                case DTCMessageType.LogonResponse:
                    base.ProcessMessage(messageDTC);
                    LogonResponse = messageDTC.Message as LogonResponse;
                    
                    // Note that SierraChart does allow more than OneHistoricalPriceDataRequestPerConnection 
                    break;
                case DTCMessageType.Heartbeat:
                case DTCMessageType.Logoff:
                case DTCMessageType.EncodingResponse:
                    base.ProcessMessage(messageDTC);
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
                case DTCMessageType.SecurityDefinitionResponse:
                    ThrowEvent(messageDTC.Message as SecurityDefinitionResponse, SecurityDefinitionResponseEvent);
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
                case DTCMessageType.HistoricalPriceDataResponseHeader:
                    var historicalPriceDataResponseHeader = messageDTC.Message as HistoricalPriceDataResponseHeader;
                    if (historicalPriceDataResponseHeader.UseZLibCompression == 1)
                    {
                        // Skip past the 2-byte header. See https://tools.ietf.org/html/rfc1950
                        Logger.Debug($"{nameof(Client)}.{nameof(ProcessMessage)} is switching client stream to read zipped.");

                        _currentCodec.ReadSwitchToZipped();
                        _useHeartbeat = false;
                    }
                    ThrowEvent(historicalPriceDataResponseHeader, HistoricalPriceDataResponseHeaderEvent);
                    break;
                case DTCMessageType.HistoricalPriceDataReject:
                    ThrowEvent(messageDTC.Message as HistoricalPriceDataReject, HistoricalPriceDataRejectEvent);
                    break;
                case DTCMessageType.HistoricalPriceDataRecordResponse:
                    var historicalPriceDataRecordResponse = messageDTC.Message as HistoricalPriceDataRecordResponse;
                    ThrowEvent(historicalPriceDataRecordResponse, HistoricalPriceDataRecordResponseEvent);
                    if (historicalPriceDataRecordResponse.IsFinalRecordBool)
                    {}
                    if (historicalPriceDataRecordResponse.IsFinalRecordBool && _currentCodec.IsZippedStream)
                    {
                        // Switch back from reading zlib to regular networkStream
                        _currentCodec.EndZippedWriting();
                    }
                    break;
                case DTCMessageType.HistoricalPriceDataTickRecordResponse:
                    var historicalPriceDataTickRecordResponse = messageDTC.Message as HistoricalPriceDataTickRecordResponse;
                    ThrowEvent(historicalPriceDataTickRecordResponse, HistoricalPriceDataTickRecordResponseEvent);
                    if (historicalPriceDataTickRecordResponse.IsFinalRecordBool && _currentCodec.IsZippedStream)
                    {
                        // Switch back from reading zlib to regular networkStream
                        _currentCodec.EndZippedWriting();
                    }
                    break;
                case DTCMessageType.HistoricalPriceDataRecordResponseInt:
                    ThrowEvent(messageDTC.Message as HistoricalPriceDataRecordResponse_Int, HistoricalPriceDataRecordResponseIntEvent);
                    break;
                case DTCMessageType.HistoricalPriceDataTickRecordResponseInt:
                    ThrowEvent(messageDTC.Message as HistoricalPriceDataTickRecordResponse_Int, HistoricalPriceDataTickRecordResponseIntEvent);
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
                    throw new NotImplementedException($"{messageDTC}");
                default:
                    throw new ArgumentOutOfRangeException($"Unexpected Message {messageDTC} received by {ClientName} {nameof(ProcessMessage)}.");
            }
        }
    }
}