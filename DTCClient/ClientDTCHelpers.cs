using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DTCCommon;
using DTCPB;

namespace DTCClient
{
    /// <summary>
    /// This partial class focuses on helper methods
    /// </summary>
    public partial class ClientDTC
    {
        /// <summary>
        /// Get historical data from DTC, returned in the various callbacks
        /// Does NOT Dispose when complete, because you might be going to a listener instead of a separate SierraChart-style historical client 
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="symbol"></param>
        /// <param name="exchange"></param>
        /// <param name="recordInterval"></param>
        /// <param name="startDateTimeUtc">if DateTime.MinValue, is requested as 0 (earliest value, per DTC)</param>
        /// <param name="endDateTimeUtc">if DateTime.MaxValue, is requested as 0 (latest value, per DTC)</param>
        /// <param name="maxDaysToReturn"></param>
        /// <param name="useZLibCompression"></param>
        /// <param name="requestDividendAdjustedStockData"></param>
        /// <param name="flag1"></param>
        /// <param name="headerCallback"></param>
        /// <param name="dataCallback"></param>
        /// <param name="rejectCallback"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="DTCSharpException"></exception>
        public Result GetHistoricalData(uint requestId, string symbol, string exchange, HistoricalDataIntervalEnum recordInterval, DateTime startDateTimeUtc, DateTime endDateTimeUtc,
            uint maxDaysToReturn, bool useZLibCompression, bool requestDividendAdjustedStockData, bool flag1,
            Action<HistoricalPriceDataResponseHeader> headerCallback, Action<HistoricalPriceDataRecordResponse> dataCallback, Action<HistoricalPriceDataReject> rejectCallback = null,
            CancellationToken cancellationToken = default)
        {
            var countRecordsReceived = 0;
            var error = new Result();
            var signal = new ManualResetEvent(false);

            // Set up handler to capture the reject event
            void OnHistoricalPriceDataRejectEvent(object s, HistoricalPriceDataReject historicalPriceDataReject)
            {
                if (historicalPriceDataReject.RequestID != requestId)
                {
                    // Ignore this one
                    return;
                }
                var message = $"{historicalPriceDataReject.RejectText} {historicalPriceDataReject.RejectReasonCode} for symbol={symbol} exchange={exchange}";
                error = new Result(message, ErrorTypes.NoDataAvailableForSymbol);
                rejectCallback?.Invoke(historicalPriceDataReject);
                signal.Set();
                //s_logger.Debug($"Rejection historicalPriceDataReject={historicalPriceDataReject}");
            }

            HistoricalPriceDataRejectEvent += OnHistoricalPriceDataRejectEvent;

            // Set up handler to capture the header event
            void HandlerHeader(object s, HistoricalPriceDataResponseHeader header)
            {
                if (header.RequestID != requestId)
                {
                    // Ignore this one
                    return;
                }
                headerCallback(header);
                if (header.IsNoRecordsAvailable)
                {
                    error = new Result();
                    signal.Set();
                    //s_logger.Debug($"No records available for {symbol} {recordInterval} header={header}");
                }
            }

            HistoricalPriceDataResponseHeaderEvent += HandlerHeader;

            // Set up the handler to capture the HistoricalPriceDataRecordResponseEvent
            HistoricalPriceDataRecordResponse response;

            void Handler(object s, HistoricalPriceDataRecordResponse e)
            {
                response = e;
                if (response.RequestID != requestId)
                {
                    // Ignore this one
                    return;
                }
                if (e.StartDateTime > 0)
                {
                    // A final record might not have a value, signified by e.StartDateTime == 0
                    //s_logger.Debug($"Sending for {symbol} response={response}");
                    dataCallback(response);
                    countRecordsReceived++;
                }
                if (e.IsFinalRecordBool)
                {
                    //s_logger.Debug($"Received final record for {symbol} e={e}");
                    signal.Set();
                }
            }

            HistoricalPriceDataRecordResponseEvent += Handler;

            // Send the request
            GetHistoricalData(requestId, symbol, exchange, recordInterval, startDateTimeUtc, endDateTimeUtc, maxDaysToReturn, useZLibCompression, requestDividendAdjustedStockData, flag1);
            if (!signal.WaitOne(TimeoutMs))
            {
                throw new TimeoutException();
            }

            s_logger.Verbose("Received {CountRecordsReceived:N0} records for {Symbol} {RecordInterval}", countRecordsReceived, symbol, recordInterval);
            return error;
        }

        /// <summary>
        /// Get historical data from DTC, returned only in events
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="symbol"></param>
        /// <param name="exchange"></param>
        /// <param name="recordInterval"></param>
        /// <param name="startDateTimeUtc">if DateTime.MinValue, is requested as 0 (earliest value, per DTC)</param>
        /// <param name="endDateTimeUtc">if DateTime.MaxValue, is requested as 0 (latest value, per DTC)</param>
        /// <param name="maxDaysToReturn"></param>
        /// <param name="useZLibCompression"></param>
        /// <param name="requestDividendAdjustedStockData"></param>
        /// <param name="flag1"></param>
        public void GetHistoricalData(uint requestId, string symbol, string exchange, HistoricalDataIntervalEnum recordInterval, DateTime startDateTimeUtc, DateTime endDateTimeUtc,
            uint maxDaysToReturn, bool useZLibCompression, bool requestDividendAdjustedStockData, bool flag1)
        {
            var startDateTime = startDateTimeUtc == DateTime.MinValue ? 0 : startDateTimeUtc.UtcToDtcDateTime();
            var endDateTime = (endDateTimeUtc == DateTime.MinValue || endDateTimeUtc == DateTime.MaxValue) ? 0 : endDateTimeUtc.UtcToDtcDateTime();
            var request = new HistoricalPriceDataRequest
            {
                RequestID = (int)requestId,
                Symbol = symbol,
                Exchange = exchange,
                RecordInterval = recordInterval,
                StartDateTime = startDateTime,
                EndDateTime = endDateTime,
                MaxDaysToReturn = maxDaysToReturn,
                UseZLibCompression = useZLibCompression ? 1U : 0,
                RequestDividendAdjustedStockData = requestDividendAdjustedStockData ? 1U : 0,
                Integer1 = flag1 ? 1U : 0,
            };
            SendRequest(DTCMessageType.HistoricalPriceDataRequest, request);
        }

        /// <summary>
        /// Get the SecurityDefinitionResponse for symbol, or null if not received before timeout
        /// SierraChart rejects, claiming RequestId is 0, if requests go too fast.
        /// So we lock on a dictionary.
        /// So use one client for all requests if possible.
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="exchange">is in the Request, but is ignored</param>
        /// <returns>the SecurityDefinitionResponse and a Result</returns>
        public (SecurityDefinitionResponse response, Result result) GetSecurityDefinition(string symbol, string exchange)
        {
            lock (s_securityDefinitionsBySymbol)
            {
                if (s_securityDefinitionsBySymbol.TryGetValue(symbol, out var securityDefinitionResponse))
                {
                    return (securityDefinitionResponse, new Result());
                }
                // Set up the handler to capture the event
                var error = new Result();
                var signal = new ManualResetEvent(false);
                var requestId = (int)NextRequestId;

                void HandlerResponse(object s, SecurityDefinitionResponse response)
                {
                    if (response.RequestID != requestId)
                    {
                        return;
                    }
                    if (response.Symbol != symbol)
                    {
                        throw new DTCSharpException("Why?");
                    }
                    securityDefinitionResponse = response;
                    if (response.Symbol != symbol)
                    {
                        // Sierra Chart never does reject, but bad index symbol TICK has no description
                        // But it isn't reliable, fails even realtime with BTCU21-CME
                        var msg = $"Unrecognized symbol={symbol}. Description is empty or null. {response}";
                        error = new Result(msg, ErrorTypes.UnrecognizedSymbol);
                        s_logger.Debug(msg);
                        securityDefinitionResponse = null;
                    }
                    else if (string.IsNullOrEmpty(response.Description) && response.SecurityType != SecurityTypeEnum.SecurityTypeStock)
                    {
                        // Sierra Chart never does reject, but unrecognized symbols seem to have null Description per https://www.sierrachart.com/SupportBoard.php?PostID=161327#P161327
                        // But BarChart stocks do come back with empty Description
                        error = new Result($"Unrecognized symbol={symbol} exchange={exchange}. Description is empty.", ErrorTypes.UnrecognizedSymbol);
                        securityDefinitionResponse = null;
                    }
                    s_securityDefinitionsBySymbol[symbol] = securityDefinitionResponse; // Yes we even cache nulls. DTC won't have it next call, either.
                    signal.Set();
                }

                SecurityDefinitionResponseEvent += HandlerResponse;

                void HandlerReject(object s, SecurityDefinitionReject reject)
                {
                    // Sierra Chart never does reject, but unrecognized symbols seem to have MinPriceIncrement 0
                    if (reject.RequestID != requestId)
                    {
                        throw new DTCSharpException("Why?");
                    }
                    error = new Result(reject.RejectText);
                    s_logger.Debug("SecurityDefinitionReject for {Symbol} ={RequestId} {RejectText}", symbol, requestId, reject.RejectText);
                    signal.Set();
                }

                SecurityDefinitionRejectEvent += HandlerReject;

                // Send the request
                var securityDefinitionForSymbolRequest = new SecurityDefinitionForSymbolRequest
                {
                    RequestID = requestId,
                    Symbol = symbol,
                    Exchange = exchange
                };

                //s_logger.Debug($"Sending SecurityDefinitionForSymbolRequest for {symbol} ={requestId}");
                SendRequest(DTCMessageType.SecurityDefinitionForSymbolRequest, securityDefinitionForSymbolRequest);
                if (!signal.WaitOne(TimeoutMs))
                {
                    throw new TimeoutException();
                }
                return (securityDefinitionResponse, error);
            }
        }

        /// <summary>
        /// Return a list of all the exchanges on the server
        /// </summary>
        /// <returns></returns>
        public List<string> GetExchanges()
        {
            var exchanges = new List<string>();
            var requestId = NextRequestId;
            var signal = new ManualResetEvent(false);

            void HandlerResponse(object s, ExchangeListResponse response)
            {
                if (response.RequestID != requestId)
                {
                    // ignore
                    return;
                }
                exchanges.Add(response.Exchange);
                if (response.IsFinalMessage != 0)
                {
                    signal.Set();
                }
            }

            ExchangeListResponseEvent += HandlerResponse;

            var exchangeListRequest = new ExchangeListRequest
            {
                RequestID = (int)requestId
            };
            SendRequest(DTCMessageType.ExchangeListRequest, exchangeListRequest);
            if (!signal.WaitOne(TimeoutMs))
            {
                throw new TimeoutException();
            }
            return exchanges;
        }

        /// <summary>
        /// Return a list of all the securities for exchange
        /// </summary>
        /// <param name="exchange"></param>
        /// <param name="securityType"></param>
        /// <returns></returns>
        public List<SecurityDefinitionResponse> GetSymbolsForExchange(string exchange, SecurityTypeEnum securityType)
        {
            var securityDefinitionResponses = new List<SecurityDefinitionResponse>();
            var requestId = NextRequestId;
            var signal = new ManualResetEvent(false);

            void HandlerResponse(object s, SecurityDefinitionResponse response)
            {
                if (response.RequestID != requestId)
                {
                    // ignore
                    return;
                }
                securityDefinitionResponses.Add(response);
                if (response.IsFinalMessage != 0)
                {
                    signal.Set();
                }
            }

            SecurityDefinitionResponseEvent += HandlerResponse;

            var request = new SymbolsForExchangeRequest
            {
                RequestID = (int)requestId,
                Exchange = exchange
            };
            if (securityType != SecurityTypeEnum.SecurityTypeUnset)
            {
                request.SecurityType = securityType;
            }
            SendRequest(DTCMessageType.SymbolsForExchangeRequest, request);
            if (!signal.WaitOne(TimeoutMs))
            {
                throw new TimeoutException();
            }
            return securityDefinitionResponses;
        }

        /// <summary>
        /// Request market data for symbol|exchange
        /// Add a symbolId if not already assigned 
        /// This is done for you within GetMarketDataUpdateTradeCompact()
        /// </summary>
        /// <param name="symbolId">The 1-based unique SymbolId that you have assigned symbol.exchange</param>
        /// <param name="symbol"></param>
        /// <param name="exchange">optional</param>
        /// <returns>The 1-based unique SymbolId that you have assigned symbol.exchange</returns>
        public uint SubscribeMarketData(uint symbolId, string symbol, string exchange)
        {
            lock (_lock)
            {
                if (LogonResponse is not { IsMarketDataSupported: true })
                {
                    throw new DTCSharpException($"MarketData is not supported for {symbol}.");
                }
                var request = new MarketDataRequest
                {
                    RequestAction = RequestActionEnum.Subscribe,
                    SymbolID = symbolId,
                    Symbol = symbol,
                    Exchange = exchange
                };
                s_logger.Information("Subscribing to MarketData: {Request}", request);
                SendRequest(DTCMessageType.MarketDataRequest, request);
                return symbolId;
            }
        }

        /// <summary>
        /// Unsubscribe from market data  symbolId 
        /// </summary>
        /// <param name="symbolId">The 1-based unique SymbolId that you have assigned symbol.exchange</param>
        /// <param name="symbol"></param>
        /// <param name="exchange"></param>
        public void UnsubscribeMarketData(uint symbolId, string symbol, string exchange)
        {
            var request = new MarketDataRequest
            {
                RequestAction = RequestActionEnum.Unsubscribe,
                SymbolID = symbolId,
                Symbol = symbol,
                Exchange = exchange
            };
            s_logger.Information("Unsubscribing from MarketData: {Request}", request);
            SendRequest(DTCMessageType.MarketDataRequest, request);
        }

        /// <summary>
        /// For details see: https://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#MarketData
        /// Also https://dtcprotocol.org/index.php?page=doc/DTCMessages_MarketDataMessages.php#Messages-MARKET_DATA_REQUEST
        /// This method subscribes to market data updates. A snapshot response is immediately returned to snapshotCallback. 
        /// Then MarketDataUpdateTradeCompact responses are sent to dataCallback. Optionally, other responses are to the other callbacks.
        /// To stop the callbacks, use MarketDataUnsubscribe() and cancel this method using cancellationToken (CancellationTokenSource.Cancel())
        /// </summary>
        /// <param name="symbolId">The 1-based unique SymbolId that you have assigned symbol.exchange</param>
        /// <param name="timeout">The time (in milliseconds) to wait for a response before giving up</param>
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
        /// <returns>null if error</returns>
        public MarketDataReject GetMarketDataUpdateTradeCompact(uint symbolId, int timeout, string symbol,
            string exchange, Action<MarketDataSnapshot> snapshotCallback, Action<MarketDataUpdateTradeCompact> tradeCallback,
            Action<MarketDataUpdateBidAskCompact> bidAskCallback = null, Action<MarketDataUpdateSessionOpen> sessionOpenCallback = null,
            Action<MarketDataUpdateSessionHigh> sessionHighCallback = null, Action<MarketDataUpdateSessionLow> sessionLowCallback = null,
            Action<MarketDataUpdateSessionSettlement> sessionSettlementCallback = null, Action<MarketDataUpdateSessionVolume> sessionVolumeCallback = null,
            Action<MarketDataUpdateOpenInterest> openInterestCallback = null)
        {
            MarketDataReject marketDataReject = null;
            if (LogonResponse == null)
            {
                return new MarketDataReject { RejectText = "Not logged on." };
            }
            if (!LogonResponse.IsMarketDataSupported)
            {
                return new MarketDataReject { RejectText = "Market data is not supported." };
            }
            var signal = new ManualResetEvent(false);

            // Set up handler to capture the reject event
            void HandlerReject(object s, MarketDataReject e)
            {
                if (e.SymbolID != symbolId)
                {
                    // ignore
                    return;
                }
                marketDataReject = e;
                signal.Set(); // force immediate return
            }

            MarketDataRejectEvent += HandlerReject;

            void MarketDataSnapshotEvent(object sender, MarketDataSnapshot e)
            {
                if (e.SymbolID != symbolId)
                {
                    // ignore
                    return;
                }
                snapshotCallback(e);
                signal.Set();
            }

            this.MarketDataSnapshotEvent += MarketDataSnapshotEvent;

            void MarketDataUpdateTradeCompactEvent(object sender, MarketDataUpdateTradeCompact e)
            {
                if (e.SymbolID != symbolId)
                {
                    // ignore
                    return;
                }
                tradeCallback(e);
                signal.Set();
            }

            this.MarketDataUpdateTradeCompactEvent += MarketDataUpdateTradeCompactEvent;

            void MarketDataUpdateBidAskCompactEvent(object sender, MarketDataUpdateBidAskCompact e)
            {
                if (e.SymbolID != symbolId)
                {
                    // ignore
                    return;
                }
                bidAskCallback?.Invoke(e);
                signal.Set();
            }

            this.MarketDataUpdateBidAskCompactEvent += MarketDataUpdateBidAskCompactEvent;

            void MarketDataUpdateSessionOpenEvent(object sender, MarketDataUpdateSessionOpen e)
            {
                if (e.SymbolID != symbolId)
                {
                    // ignore
                    return;
                }
                sessionOpenCallback?.Invoke(e);
                signal.Set();
            }

            this.MarketDataUpdateSessionOpenEvent += MarketDataUpdateSessionOpenEvent;

            void MarketDataUpdateSessionHighEvent(object sender, MarketDataUpdateSessionHigh e)
            {
                if (e.SymbolID != symbolId)
                {
                    // ignore
                    return;
                }
                sessionHighCallback?.Invoke(e);
                signal.Set();
            }

            this.MarketDataUpdateSessionHighEvent += MarketDataUpdateSessionHighEvent;

            void MarketDataUpdateSessionLowEvent(object sender, MarketDataUpdateSessionLow e)
            {
                if (e.SymbolID != symbolId)
                {
                    // ignore
                    return;
                }
                sessionLowCallback?.Invoke(e);
                signal.Set();
            }

            this.MarketDataUpdateSessionLowEvent += MarketDataUpdateSessionLowEvent;

            void MarketDataUpdateSessionSettlementEvent(object sender, MarketDataUpdateSessionSettlement e)
            {
                if (e.SymbolID != symbolId)
                {
                    // ignore
                    return;
                }
                sessionSettlementCallback?.Invoke(e);
                signal.Set();
            }

            this.MarketDataUpdateSessionSettlementEvent += MarketDataUpdateSessionSettlementEvent;

            void MarketDataUpdateSessionVolumeEvent(object sender, MarketDataUpdateSessionVolume e)
            {
                if (e.SymbolID != symbolId)
                {
                    // ignore
                    return;
                }
                sessionVolumeCallback?.Invoke(e);
                signal.Set();
            }

            this.MarketDataUpdateSessionVolumeEvent += MarketDataUpdateSessionVolumeEvent;

            void MarketDataUpdateOpenInterestEvent(object sender, MarketDataUpdateOpenInterest e)
            {
                if (e.SymbolID != symbolId)
                {
                    // ignore
                    return;
                }
                if (openInterestCallback != null)
                {
                    openInterestCallback(e);
                }
                signal.Set();
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
            if (!signal.WaitOne(TimeoutMs))
            {
                throw new TimeoutException();
            }
            return marketDataReject;
        }

        /// <summary>
        /// Return a list of all the trade accounts for exchange
        /// </summary>
        /// <returns></returns>
        public List<string> GetTradeAccounts()
        {
            var responses = new List<TradeAccountResponse>();
            var requestId = NextRequestId;
            var error = new Result();
            var signal = new ManualResetEvent(false);

            void HandlerResponse(object s, TradeAccountResponse response)
            {
                if (response.RequestID != requestId)
                {
                    // ignore this one
                    return;
                }
                responses.Add(response);
                if (response.MessageNumber == response.TotalNumberMessages)
                {
                    signal.Set();
                }
            }

            TradeAccountResponseEvent += HandlerResponse;

            var request = new TradeAccountsRequest
            {
                RequestID = (int)requestId
            };
            SendRequest(DTCMessageType.TradeAccountsRequest, request);
            if (!signal.WaitOne(TimeoutMs))
            {
                throw new TimeoutException();
            }
            var tradeAccounts = responses.Select(x => x.TradeAccount).ToList();
            return tradeAccounts;
        }

        /// <summary>
        /// Return an account balance update for account
        /// </summary>
        /// <returns></returns>
        public (List<AccountBalanceUpdate>, Result) GetAccountsBalanceUpdates(string accountName)
        {
            var responses = new List<AccountBalanceUpdate>();
            var error = new Result();
            var requestId = NextRequestId;
            var signal = new ManualResetEvent(false);

            void HandlerReject(object s, AccountBalanceReject response)
            {
                if (response.RequestID != requestId)
                {
                    // ignore this one
                    return;
                }
                error = new Result(response.RejectText);
                signal.Set();
            }

            AccountBalanceRejectEvent += HandlerReject;

            void HandlerResponse(object s, AccountBalanceUpdate response)
            {
                // if (string.IsNullOrEmpty(response.TradeAccount))
                // { }
                if (response.RequestID != requestId)
                {
                    // ignore this one
                    //s_logger.Debug($"Ignoring AccountBalanceUpdate: {response}");
                    return;
                }
                if (response.IsNoAccountBalances)
                {
                    signal.Set();
                    return;
                }
                //s_logger.Debug($"Accepted AccountBalanceUpdate: {response}");
                responses.Add(response);
                if (response.MessageNumber == response.TotalNumberMessages)
                {
                    signal.Set();
                }
            }

            AccountBalanceUpdateEvent += HandlerResponse;

            var request = new AccountBalanceRequest
            {
                RequestID = (int)requestId,
                TradeAccount = accountName // 1/1/2022 seems to be a bug in SierraChart that accountName is ignored. AccountBalanceUpdates ll accounts come back.
            };
            //s_logger.Debug($"Sending request forAccountBalanceUpdates: {request}");
            SendRequest(DTCMessageType.AccountBalanceRequest, request);
            if (!signal.WaitOne(TimeoutMs))
            {
                throw new TimeoutException();
            }
            return (responses, error);
        }

        /// <summary>
        /// Return historical account balance updates for account
        /// </summary>
        /// <param name="accountName"></param>
        /// <param name="startDateTimeeUtc">DateTime.MinValue means all</param>
        /// <returns></returns>
        public (List<HistoricalAccountBalanceResponse>, Result) GetHistoricalAccountsBalances(string accountName, DateTime startDateTimeeUtc)
        {
            var responses = new List<HistoricalAccountBalanceResponse>();
            var error = new Result();
            var signal = new ManualResetEvent(false);
            var requestId = NextRequestId;

            void HandlerReject(object s, HistoricalAccountBalancesReject response)
            {
                if (response.RequestID != requestId)
                {
                    // ignore this one
                    return;
                }
                error = new Result(response.RejectText);
                signal.Set();
            }

            HistoricalAccountBalancesRejectEvent += HandlerReject;

            void HandlerResponse(object s, HistoricalAccountBalanceResponse response)
            {
                if (response.RequestID != requestId)
                {
                    // ignore this one
                    return;
                }
                if (response.IsNoAccountBalances)
                {
                    signal.Set();
                    return;
                }
                responses.Add(response);
                if (response.IsFinalResponse > 0)
                {
                    signal.Set();
                }
            }

            HistoricalAccountBalanceResponseEvent += HandlerResponse;

            var request = new HistoricalAccountBalancesRequest
            {
                RequestID = (int)requestId,
                TradeAccount = accountName,
                StartDateTime = startDateTimeeUtc.UtcToDtcDateTime()
            };
            SendRequest(DTCMessageType.HistoricalAccountBalancesRequest, request);
            if (!signal.WaitOne(TimeoutMs))
            {
                throw new TimeoutException();
            }
            return (responses, error);
        }

        /// <summary>
        /// Return position updates for account
        /// </summary>
        /// <returns></returns>
        public (List<PositionUpdate>, Result) GetPositionUpdates(string accountName)
        {
            if (string.IsNullOrEmpty(accountName))
            {
                throw new ArgumentException($"'{nameof(accountName)}' cannot be null or empty.", nameof(accountName));
            }
            var responses = new List<PositionUpdate>();
            var error = new Result();
            var signal = new ManualResetEvent(false);
            var requestId = NextRequestId;

            void HandlerReject(object s, CurrentPositionsReject response)
            {
                if (response.RequestID != requestId)
                {
                    // ignore this one
                    return;
                }
                error = new Result(response.RejectText);
                signal.Set();
            }

            CurrentPositionsRejectEvent += HandlerReject;

            void HandlerResponse(object s, PositionUpdate response)
            {
                if (response.RequestID != requestId)
                {
                    // ignore this one
                    return;
                }
                if (response.IsNoPositions)
                {
                    signal.Set();
                    return;
                }
                responses.Add(response);
                if (response.MessageNumber == response.TotalNumberMessages)
                {
                    signal.Set();
                }
            }

            PositionUpdateEvent += HandlerResponse;

            var request = new AccountBalanceRequest
            {
                RequestID = (int)requestId,
                TradeAccount = accountName
            };
            SendRequest(DTCMessageType.CurrentPositionsRequest, request);
            if (!signal.WaitOne(TimeoutMs))
            {
                throw new TimeoutException();
            }
            return (responses, error);
        }

        /// <summary>
        /// Return historical open orders (orderUpdates) for account
        /// </summary>
        /// <param name="accountName">Ignored if requestAllOrders is true</param>
        /// <param name="serverOrderId">Ignored if requestAllOrders is true</param>
        /// <param name="requestAllOrders"></param>
        /// <returns></returns>
        public (List<OrderUpdate>, Result) GetOpenOrderUpdates(string accountName, string serverOrderId = "", bool requestAllOrders = false)
        {
            var responses = new List<OrderUpdate>();
            var error = new Result();
            var signal = new ManualResetEvent(false);
            var requestId = NextRequestId;

            void HandlerReject(object s, OpenOrdersReject response)
            {
                if (response.RequestID != requestId)
                {
                    // 0 means this is unsolicited
                    // ignore this one
                    return;
                }
                error = new Result(response.RejectText);
                signal.Set();
            }

            OpenOrdersRejectEvent += HandlerReject;

            void HandlerResponse(object s, OrderUpdate response)
            {
                if (response.RequestID != requestId)
                {
                    // 0 means this is unsolicited
                    // ignore this one
                    return;
                }
                if (response.IsNoOrders)
                {
                    signal.Set();
                    return;
                }
                responses.Add(response);
                if (response.TotalNumMessages == response.MessageNumber)
                {
                    signal.Set();
                }
            }

            OrderUpdateEvent += HandlerResponse;

            var request = new OpenOrdersRequest
            {
                RequestID = (int)requestId,
                ServerOrderID = requestAllOrders ? "" : serverOrderId,
                TradeAccount = requestAllOrders ? "" : accountName,
                RequestAllOrders = requestAllOrders ? 1 : 0
            };
            SendRequest(DTCMessageType.OpenOrdersRequest, request);
            if (!signal.WaitOne(TimeoutMs))
            {
                throw new TimeoutException();
            }
            return (responses, error);
        }

        /// <summary>
        /// Return historical order fills for account
        /// </summary>
        /// <param name="accountName">Ignored if requestAllOrders is true</param>
        /// <param name="serverOrderId">Ignored if requestAllOrders is true</param>
        /// <param name="startDateTimeUtc"></param>
        /// <param name="numberOfDays">Number of days of fills to return. 0 means all. Alternative to startDateTimeeUtc</param>
        /// <returns></returns>
        public (List<HistoricalOrderFillResponse>, Result) GetHistoricalOrderFills(string accountName, string serverOrderId = "",
            DateTime? startDateTimeUtc = null, int numberOfDays = 0)
        {
            serverOrderId ??= "";
            var responses = new List<HistoricalOrderFillResponse>();
            var error = new Result();
            var requestId = NextRequestId;
            var signal = new ManualResetEvent(false);

            void HandlerReject(object s, HistoricalOrderFillsReject response)
            {
                if (response.RequestID != requestId)
                {
                    // 0 means this is unsolicited
                    // ignore this one
                    return;
                }
                error = new Result(response.RejectText);
                signal.Set();
            }

            HistoricalOrderFillsRejectEvent += HandlerReject;

            void HandlerResponse(object s, HistoricalOrderFillResponse response)
            {
                if (response.RequestID != requestId)
                {
                    // 0 means this is unsolicited
                    // ignore this one
                    return;
                }
                if (response.IsNoOrderFills)
                {
                    signal.Set();
                    return;
                }
                if (response.UniqueExecutionID.ToLower() == "sod")
                {
                    // E.g. Rithmic is giving this with UniqueExecutionID == "sod" and empty ServerOrderID etc.
                    signal.Set();
                    return;
                }

                responses.Add(response);
                if (response.TotalNumberMessages == response.MessageNumber || response.NoOrderFills > 0)
                {
                    signal.Set();
                }
            }

            HistoricalOrderFillResponseEvent += HandlerResponse;

            var request = new HistoricalOrderFillsRequest
            {
                RequestID = (int)requestId,
                ServerOrderID = serverOrderId,
                TradeAccount = accountName,
                // NumberOfDays = numberOfDays,
                // StartDateTime = startDateTimeeUtc.UtcToDtcDateTime()
            };
            if (startDateTimeUtc != null && startDateTimeUtc != DateTime.MinValue)
            {
                request.StartDateTime = startDateTimeUtc.Value.UtcToDtcDateTime();
            }
            else if (numberOfDays > 0)
            {
                request.NumberOfDays = numberOfDays;
            }
            else
            {
                // Must have either NumberOfDays or StartDateTime Non-Zero per RejectText
                request.NumberOfDays = 10000;
            }
            SendRequest(DTCMessageType.HistoricalOrderFillsRequest, request);
            //s_logger.Verbose("Starting to wait for historical fills {request}");
            if (!signal.WaitOne(TimeoutMs))
            {
                throw new TimeoutException();
            }
            //s_logger.Verbose($" for historical fills {request} Done waiting, error={error}");
            return (responses, error);
        }

        /// <summary>
        /// Submit a single order. Returns immediately
        /// See https://dtcprotocol.org/index.php?page=doc/DTCMessages_OrderEntryModificationMessages.php#Messages-SUBMIT_NEW_SINGLE_ORDER
        /// </summary>
        /// <param name="accountName"></param>
        /// <param name="symbol"></param>
        /// <param name="clientOrderId">Must never have been used before.</param>
        /// <param name="orderType"></param>
        /// <param name="orderAction"></param>
        /// <param name="quantity"></param>
        /// <param name="price1">This is the limit price for a Limit order, the stop price for a Stop order, or the trigger price for a Market if Touched order.</param>
        /// <param name="price2">For a Stop-Limit order, this is the limit price. This only applies to Stop-Limit orders.</param>
        /// <param name="exchange">optional</param>
        /// <param name="tif">Defaults to TifDay</param>
        /// <param name="gtd">In the case of when the TimeInForce is TIF_GOOD_TILL_DATE_TIME, this specifies the expiration Date-Time of the order.</param>
        /// <param name="isAutomatedOrder">Set to 1 to signify the order has been submitted by an automated trading process</param>
        /// <param name="isParentOrder">The Client will set this to 1 when the order is part of a bracket order.
        ///     This indicates that this is the parent order. A bracket order will consist of a SUBMIT_NEW_SINGLE_ORDER message followed by a SUBMIT_NEW_OCO_ORDER message. The Server will use IsParentOrder as a flag to know that this message is a parent order.
        ///     The Server will hold onto this order until it receives the subsequent SUBMIT_NEW_OCO_ORDER message and then process all of the orders as one complete set.</param>
        /// <param name="freeFormText"></param>
        public void SubmitOrder(string accountName, string symbol, string clientOrderId,
            OrderTypeEnum orderType, OrderAction orderAction, double quantity, double price1 = 0, double price2 = 0, string exchange = "",
            TimeInForceEnum tif = TimeInForceEnum.TifDay, DateTime? gtd = null, bool isAutomatedOrder = false, bool isParentOrder = false, string freeFormText = "")
        {
            var (buySell, openOrClose) = orderAction.FromOrderAction();
            var request = new SubmitNewSingleOrder
            {
                TradeAccount = accountName,
                Symbol = symbol,
                ClientOrderID = clientOrderId,
                OpenOrClose = openOrClose,
                OrderType = orderType,
                BuySell = buySell,
                Quantity = quantity,
                TimeInForce = tif,
                IsAutomatedOrder = isAutomatedOrder ? 1u : 0u,
                IsParentOrder = isParentOrder ? 1u : 0u,
                FreeFormText = freeFormText
            };
            if (!string.IsNullOrEmpty(exchange))
            {
                request.Exchange = exchange;
            }
            if (price1 != 0)
            {
                request.Price1 = price1;
            }
            if (price2 != 0)
            {
                request.Price2 = price2;
            }
            if (gtd != null)
            {
                request.GoodTillDateTime = gtd.Value.UtcToDtcDateTime();
            }
            if (!string.IsNullOrEmpty(freeFormText))
            {
                request.FreeFormText = freeFormText;
            }

            //s_logger.Debug($"SendRequest() {request}");
            SendRequest(DTCMessageType.SubmitNewSingleOrder, request);
            s_logger.Information("{TradeMessageLogging}Sent SubmitNewSingleOrder Request={Request}", TradeMessageLogging, request);
        }

        /// <summary>
        /// Submit a single order. Return immediately
        /// See https://dtcprotocol.org/index.php?page=doc/DTCMessages_OrderEntryModificationMessages.php#Messages-SUBMIT_NEW_OCO_ORDER
        /// </summary>
        /// <param name="accountName"></param>
        /// <param name="symbol"></param>
        /// <param name="clientOrderId1">Must never have been used before.</param>
        /// <param name="clientOrderId2">Must never have been used before.</param>
        /// <param name="orderType1"></param>
        /// <param name="orderAction1"></param>
        /// <param name="quantity1"></param>
        /// <param name="orderType2"></param>
        /// <param name="orderAction2"></param>
        /// <param name="quantity2"></param>
        /// <param name="price1_1">This is the limit price for a Limit order, the stop price for a Stop order, or the trigger price for a Market if Touched order.</param>
        /// <param name="price2_1">For a Stop-Limit order, this is the limit price. This only applies to Stop-Limit orders.</param>
        /// <param name="price1_2">This is the limit price for a Limit order, the stop price for a Stop order, or the trigger price for a Market if Touched order.</param>
        /// <param name="price2_2">For a Stop-Limit order, this is the limit price. This only applies to Stop-Limit orders.</param>
        /// <param name="partialFillHandlingEnum"></param>
        /// <param name="useOffsets"></param>
        /// <param name="offsetFromParent1"></param>
        /// <param name="offsetFromParent2"></param>
        /// <param name="exchange">optional</param>
        /// <param name="tif">Defaults to TifDay</param>
        /// <param name="gtd">In the case of when the TimeInForce is TIF_GOOD_TILL_DATE_TIME, this specifies the expiration Date-Time of the order.</param>
        /// <param name="isAutomatedOrder">Set to 1 to signify the order has been submitted by an automated trading process</param>
        /// <param name="freeFormText"></param>
        /// <param name="parentTriggerClientOrderId"></param>
        /// <returns>orderUpdate1 for clientOrderId1 and orderUpdate2 for clientOrder2, plus result holds an error if it occurs.</returns>
        /// <exception cref="DTCSharpException"></exception>
        // ReSharper disable InconsistentNaming
        public void SubmitOcoOrders(string accountName, string symbol, string clientOrderId1, string clientOrderId2,
            OrderTypeEnum orderType1, OrderAction orderAction1, double quantity1, OrderTypeEnum orderType2, OrderAction orderAction2, double quantity2,
            double price1_1 = 0, double price2_1 = 0, double price1_2 = 0, double price2_2 = 0,
            PartialFillHandlingEnum partialFillHandlingEnum = PartialFillHandlingEnum.PartialFillHandlingReduceQuantity,
            bool useOffsets = false, double offsetFromParent1 = 0, double offsetFromParent2 = 0,
            string exchange = "", TimeInForceEnum tif = TimeInForceEnum.TifDay, DateTime? gtd = null, bool isAutomatedOrder = false,
            string freeFormText = "", string parentTriggerClientOrderId = "")
        {
            var (buySell1, openOrClose1) = orderAction1.FromOrderAction();
            var (buySell2, openOrClose2) = orderAction2.FromOrderAction();
            if (openOrClose1 != openOrClose2)
            {
                throw new DTCSharpException("Open and close must be in the same direction on an OCO order.");
            }
            var request = new SubmitNewOCOOrder
            {
                TradeAccount = accountName,
                Symbol = symbol,
                OpenOrClose = openOrClose1,
                ClientOrderID1 = clientOrderId1,
                OrderType1 = orderType1,
                BuySell1 = buySell1,
                Quantity1 = quantity1,
                ClientOrderID2 = clientOrderId2,
                OrderType2 = orderType2,
                BuySell2 = buySell2,
                Quantity2 = quantity2,
                TimeInForce = tif,
                PartialFillHandling = partialFillHandlingEnum,
                UseOffsets = useOffsets ? 1u : 0u,
                OffsetFromParent1 = offsetFromParent1,
                OffsetFromParent2 = offsetFromParent2,
                IsAutomatedOrder = isAutomatedOrder ? 1u : 0u
            };
            if (!string.IsNullOrEmpty(exchange))
            {
                request.Exchange = exchange;
            }
            if (price1_1 != 0)
            {
                request.Price11 = price1_1;
                //request.Price11AsString = request.Price11.ToString();
            }
            if (price2_1 != 0)
            {
                request.Price21 = price2_1;
                //request.Price21AsString = request.Price21.ToString();
            }
            if (price1_2 != 0)
            {
                request.Price12 = price1_2;
                //request.Price12AsString = request.Price12.ToString();
            }
            if (price2_2 != 0)
            {
                request.Price22 = price2_2;
                //request.Price22AsString = request.Price22.ToString();
            }
            if (gtd != null)
            {
                request.GoodTillDateTime = gtd.Value.UtcToDtcDateTime();
            }
            if (!string.IsNullOrEmpty(parentTriggerClientOrderId))
            {
                request.ParentTriggerClientOrderID = parentTriggerClientOrderId;
            }
            if (!string.IsNullOrEmpty(freeFormText))
            {
                request.FreeFormText = freeFormText;
            }

            SendRequest(DTCMessageType.SubmitNewOcoOrder, request);
            s_logger.Information("{TradeMessageLogging}Sent SubmitOcoOrder Request={Request}", TradeMessageLogging, request);
        }
        // ReSharper restore InconsistentNaming

        public void SubmitFlattenPositionOrder(string accountName, string clientOrderId, string symbol, string exchange = "", string freeFormText = "", bool isAutomatedOrder = false)
        {
            var request = new SubmitFlattenPositionOrder
            {
                Symbol = symbol,
                Exchange = exchange,
                TradeAccount = accountName,
                ClientOrderID = clientOrderId,
                FreeFormText = freeFormText,
                IsAutomatedOrder = isAutomatedOrder ? 1u : 0
            };
            SendRequest(DTCMessageType.SubmitFlattenPositionOrder, request);
            s_logger.Information("{TradeMessageLogging}Sent SubmitFlattenPositionOrder Request={Request}", TradeMessageLogging, request);
        }

        public void CancelOrder(string accountName, string clientOrderId, string serverOrderId)
        {
            if (string.IsNullOrEmpty(serverOrderId))
            {
                // Ignore this problem order
                return;
                //throw new ArgumentNullException(nameof(serverOrderId));
            }

            void HandlerResponse(object s, OrderUpdate orderUpdate)
            {
                if (orderUpdate.ServerOrderID != serverOrderId && !string.IsNullOrEmpty(orderUpdate.ServerOrderID) && !string.IsNullOrEmpty(serverOrderId))
                {
                    // Ignore this one. May be cancelling an OCO order, and the other may be coming through first
                    return;
                }
                if (orderUpdate.ClientOrderID != clientOrderId && !string.IsNullOrEmpty(orderUpdate.ClientOrderID) && !string.IsNullOrEmpty(clientOrderId))
                {
                    // Ignore this one. May be cancelling an OCO order, and the other may be coming through first
                    return;
                }
                switch (orderUpdate.OrderStatus)
                {
                    case OrderStatusEnum.OrderStatusPendingCancel:
                    case OrderStatusEnum.OrderStatusCanceled:
                    case OrderStatusEnum.OrderStatusOpen:
                        break;
                    case OrderStatusEnum.OrderStatusRejected:
                        throw new OrderRejectedException(orderUpdate);
                    case OrderStatusEnum.OrderStatusPendingOpen:
                    case OrderStatusEnum.OrderStatusUnspecified:
                    case OrderStatusEnum.OrderStatusOrderSent:
                    case OrderStatusEnum.OrderStatusPendingChild:
                    case OrderStatusEnum.OrderStatusPendingCancelReplace:
                    case OrderStatusEnum.OrderStatusFilled:
                    case OrderStatusEnum.OrderStatusPartiallyFilled:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            OrderUpdateEvent += HandlerResponse;

            var request = new CancelOrder
            {
                TradeAccount = accountName,
                ClientOrderID = clientOrderId,
                ServerOrderID = serverOrderId
            };

            //s_logger.Debug($"SendRequest() {request}");
            SendRequest(DTCMessageType.CancelOrder, request);
            s_logger.Information("{TradeMessageLogging}Sent CancelOrder Request={Request}", TradeMessageLogging, request);
        }

        /// <summary>
        /// See https://dtcprotocol.org/index.php?page=doc/DTCMessages_OrderEntryModificationMessages.php#Messages-CANCEL_REPLACE_ORDER
        /// </summary>
        /// <param name="accountName"></param>
        /// <param name="clientOrderId"></param>
        /// <param name="serverOrderId"></param>
        /// <param name="changePrice1"></param>
        /// <param name="price1"></param>
        /// <param name="changePrice2"></param>
        /// <param name="price2"></param>
        /// <param name="quantity"></param>
        /// <param name="updatePrice1OffsetToParent"></param>
        /// <param name="tif"></param>
        /// <param name="gtd"></param>
        /// <exception cref="DTCSharpException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="OrderRejectedException"></exception>
        public void CancelReplaceOrder(string accountName, string clientOrderId, string serverOrderId, bool changePrice1 = false, double price1 = 0,
            bool changePrice2 = false, double price2 = 0, double quantity = 0, bool updatePrice1OffsetToParent = false,
            TimeInForceEnum tif = TimeInForceEnum.TifDay, DateTime? gtd = null)
        {
            if (string.IsNullOrEmpty(serverOrderId))
            {
                throw new ArgumentNullException(nameof(serverOrderId));
            }

            void HandlerResponse(object s, OrderUpdate orderUpdate)
            {
                if (orderUpdate.ServerOrderID != serverOrderId && !string.IsNullOrEmpty(orderUpdate.ServerOrderID) && !string.IsNullOrEmpty(serverOrderId))
                {
                    // Ignore this one. May be cancelling an OCO order, and the other may be coming through first
                    return;
                }
                if (orderUpdate.ClientOrderID != clientOrderId && !string.IsNullOrEmpty(orderUpdate.ClientOrderID) && !string.IsNullOrEmpty(clientOrderId))
                {
                    // Ignore this one. May be cancelling an OCO order, and the other may be coming through first
                    return;
                }
                switch (orderUpdate.OrderStatus)
                {
                    case OrderStatusEnum.OrderStatusPendingCancelReplace:
                    case OrderStatusEnum.OrderStatusPendingCancel:
                    case OrderStatusEnum.OrderStatusCanceled:
                    case OrderStatusEnum.OrderStatusOpen:
                        break;
                    case OrderStatusEnum.OrderStatusRejected:
                        throw new OrderRejectedException(orderUpdate);
                    case OrderStatusEnum.OrderStatusPendingOpen:
                        // This might be like Submitted but not yet Accepted
                        // error = new Result();
                        // signal.Set();
                        break;
                    case OrderStatusEnum.OrderStatusUnspecified:
                        break;
                    case OrderStatusEnum.OrderStatusOrderSent:
                        break;
                    case OrderStatusEnum.OrderStatusPendingChild:
                        break;
                    case OrderStatusEnum.OrderStatusFilled:
                        break;
                    case OrderStatusEnum.OrderStatusPartiallyFilled:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            OrderUpdateEvent += HandlerResponse;

            var request = new CancelReplaceOrder
            {
                TradeAccount = accountName,
                ClientOrderID = clientOrderId,
                ServerOrderID = serverOrderId,
                TimeInForce = tif,
                GoodTillDateTime = gtd.HasValue ? (ulong)gtd.Value.ToUnixSeconds() : 0,
                UpdatePrice1OffsetToParent = updatePrice1OffsetToParent ? 1u : 0
            };
            if (quantity > 0)
            {
                // User wants to change the quantity
                request.Quantity = quantity;
            }
            if (changePrice1)
            {
                request.Price1IsSet = 1u;
                request.Price1 = price1;
            }
            else
            {
                request.Price1IsSet = 0;
            }
            if (changePrice2)
            {
                request.Price2IsSet = 1u;
                request.Price2 = price2;
            }
            else
            {
                request.Price2IsSet = 0;
            }
            if (gtd != null)
            {
                request.GoodTillDateTime = (ulong)gtd.Value.UtcToDtcDateTime();
            }

            //s_logger.Debug($"SendRequest() {request}");
            SendRequest(DTCMessageType.CancelReplaceOrder, request);
            s_logger.Information("{TradeMessageLogging} sent CancelReplaceOrder Request={Request}", TradeMessageLogging, request);
        }
    }
}