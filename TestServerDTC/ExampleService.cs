using System;
using System.Collections.Generic;
using DTCCommon.EventArgsF;
using DTCCommon.Extensions;
using DTCPB;
using DTCServer;
using Google.Protobuf;

namespace TestServer
{
    /// <summary>
    /// The service implementation that provides responses to client requests.
    /// </summary>
    public sealed class ExampleService : IServiceDTC
    {
        public ExampleService()
        {
            NumTradesAndBidAsksToSend = 20;
            MarketDataUpdateTradeCompacts = new List<MarketDataUpdateTradeCompact>(NumTradesAndBidAsksToSend);
            MarketDataUpdateBidAskCompacts = new List<MarketDataUpdateBidAskCompact>(NumTradesAndBidAsksToSend);
            MarketDataUpdateBidAskCompacts = new List<MarketDataUpdateBidAskCompact>(NumTradesAndBidAsksToSend);

            NumHistoricalPriceDataRecordsToSend = 10;
            HistoricalPriceDataRecordResponses = new List<HistoricalPriceDataRecordResponse>(NumHistoricalPriceDataRecordsToSend);

            // Define default test data
            for (int i = 0; i < NumTradesAndBidAsksToSend; i++)
            {
                var trade = new MarketDataUpdateTradeCompact
                {
                    AtBidOrAsk = AtBidOrAskEnum.AtAsk,
                    DateTime = DateTime.UtcNow.UtcToDtcDateTime4Byte(),
                    Price = 2000f + i,
                    SymbolID = 1u,
                    Volume = i + 1
                };
                MarketDataUpdateTradeCompacts.Add(trade);
                var bidAsk = new MarketDataUpdateBidAskCompact
                {
                    AskPrice = 2000f + i,
                    BidPrice = 2000f + i - 0.25f,
                    AskQuantity = i,
                    BidQuantity = i + 1,
                    DateTime = DateTime.UtcNow.UtcToDtcDateTime4Byte(),
                    SymbolID = 1u
                };
                MarketDataUpdateBidAskCompacts.Add(bidAsk);
            }
            MarketDataSnapshot = new MarketDataSnapshot
            {
                AskPrice = 1,
                AskQuantity = 2,
                BidAskDateTime = DateTime.UtcNow.UtcToDtcDateTime(),
                BidPrice = 3,
                BidQuantity = 4,
                LastTradeDateTime = DateTime.UtcNow.UtcToDtcDateTime(),
                LastTradePrice = 5,
                LastTradeVolume = 6,
                OpenInterest = 7,
                SessionHighPrice = 8,
                SymbolID = 1
            };

            HistoricalPriceDataResponseHeader = new HistoricalPriceDataResponseHeader
            {
                IntToFloatPriceDivisor = 1,
                NoRecordsToReturn = 0,
                RecordInterval = HistoricalDataIntervalEnum.IntervalTick,
                RequestID = 1,
                UseZLibCompression = 0
            };

            for (int i = 0; i < NumHistoricalPriceDataRecordsToSend; i++)
            {
                var response = new HistoricalPriceDataRecordResponse
                {
                    AskVolume = i + 1,
                    BidVolume = i + 1,
                    HighPrice = 2010,
                    IsFinalRecord = 0,
                    LastPrice = 2008,
                    LowPrice = 2001,
                    NumTrades = (uint)i + 1,
                    OpenPrice = 2002,
                    RequestID = 1,
                    StartDateTime = DateTime.UtcNow.UtcToDtcDateTime(),
                    Volume = i + 1
                };
                HistoricalPriceDataRecordResponses.Add(response);
            }
        }

        public List<HistoricalPriceDataRecordResponse> HistoricalPriceDataRecordResponses { get; set; }

        public int NumTradesAndBidAsksToSend { get; set; }

        public int NumHistoricalPriceDataRecordsToSend { get; set; }

        #region PropertiesForTesting

        /// <summary>
        /// Set this to the trades to be sent as the result of a MarketDataRequest
        /// </summary>
        public List<MarketDataUpdateTradeCompact> MarketDataUpdateTradeCompacts { get; set; }

        /// <summary>
        /// Set this to the BidAsks to be sent as the result of a MarketDataRequest
        /// </summary>
        public List<MarketDataUpdateBidAskCompact> MarketDataUpdateBidAskCompacts { get; set; }

        /// <summary>
        /// Set this to the snapshot to be sent as the result of a MarketDataRequest
        /// </summary>
        public MarketDataSnapshot MarketDataSnapshot { get; set; }

        /// <summary>
        /// Set this to the header to be sent as the result of a HistoricalPriceDataRequest
        /// </summary>
        public HistoricalPriceDataResponseHeader HistoricalPriceDataResponseHeader { get; set; }

        #endregion PropertiesForTesting

        // These events just show a mechanism for having other parts of your server application hook up to client requests.

        #region events

        public event EventHandler<EventArgs<Heartbeat, DTCMessageType, ClientHandler>> HeartbeatEvent;
        public event EventHandler<EventArgs<Logoff, DTCMessageType, ClientHandler>> LogoffEvent;

        /// <summary>
        /// This event is only thrown for informational purposes.
        /// HandleMessage() takes care of changing the current encoding and responding.
        /// So do NOT respond to this event.
        /// </summary>
        public event EventHandler<EventArgs<EncodingRequest, DTCMessageType, ClientHandler>> EncodingRequestEvent;

        public event EventHandler<EventArgs<LogonRequest, DTCMessageType, ClientHandler>> LogonRequestEvent;
        public event EventHandler<EventArgs<MarketDataRequest, DTCMessageType, ClientHandler>> MarketDataRequestEvent;
        public event EventHandler<EventArgs<MarketDepthRequest, DTCMessageType, ClientHandler>> MarketDepthRequestEvent;
        public event EventHandler<EventArgs<SubmitNewSingleOrder, DTCMessageType, ClientHandler>> SubmitNewSingleOrderEvent;
        public event EventHandler<EventArgs<SubmitNewSingleOrderInt, DTCMessageType, ClientHandler>> SubmitNewSingleOrderIntEvent;
        public event EventHandler<EventArgs<SubmitNewOCOOrder, DTCMessageType, ClientHandler>> SubmitNewOcoOrderEvent;
        public event EventHandler<EventArgs<SubmitNewOCOOrderInt, DTCMessageType, ClientHandler>> SubmitNewOcoOrderIntEvent;
        public event EventHandler<EventArgs<CancelOrder, DTCMessageType, ClientHandler>> CancelOrderEvent;
        public event EventHandler<EventArgs<CancelReplaceOrder, DTCMessageType, ClientHandler>> CancelReplaceOrderEvent;
        public event EventHandler<EventArgs<CancelReplaceOrderInt, DTCMessageType, ClientHandler>> CancelReplaceOrderIntEvent;
        public event EventHandler<EventArgs<OpenOrdersRequest, DTCMessageType, ClientHandler>> OpenOrdersRequestEvent;
        public event EventHandler<EventArgs<HistoricalOrderFillsRequest, DTCMessageType, ClientHandler>> HistoricalOrderFillsRequestEvent;
        public event EventHandler<EventArgs<CurrentPositionsRequest, DTCMessageType, ClientHandler>> CurrentPositionsRequestEvent;
        public event EventHandler<EventArgs<TradeAccountsRequest, DTCMessageType, ClientHandler>> TradeAccountsRequestEvent;
        public event EventHandler<EventArgs<ExchangeListRequest, DTCMessageType, ClientHandler>> ExchangeListRequestEvent;
        public event EventHandler<EventArgs<SymbolsForExchangeRequest, DTCMessageType, ClientHandler>> SymbolsForExchangeRequestEvent;
        public event EventHandler<EventArgs<UnderlyingSymbolsForExchangeRequest, DTCMessageType, ClientHandler>> UnderlyingSymbolsForExchangeRequestEvent;
        public event EventHandler<EventArgs<SymbolsForUnderlyingRequest, DTCMessageType, ClientHandler>> SymbolsForUnderlyingRequestEvent;
        public event EventHandler<EventArgs<SecurityDefinitionForSymbolRequest, DTCMessageType, ClientHandler>> SecurityDefinitionForSymbolRequestEvent;
        public event EventHandler<EventArgs<SymbolSearchRequest, DTCMessageType, ClientHandler>> SymbolSearchRequestEvent;
        public event EventHandler<EventArgs<AccountBalanceRequest, DTCMessageType, ClientHandler>> AccountBalanceRequestEvent;
        public event EventHandler<EventArgs<HistoricalPriceDataRequest, DTCMessageType, ClientHandler>> HistoricalPriceDataRequestEvent;

        public event EventHandler<string> MessageEvent;

        public bool SendSymbols { get; set; }

        private void OnMessage(string message)
        {
            var temp = MessageEvent;
            temp?.Invoke(this, message);
        }

        private void ThrowEvent<T>(T message, EventHandler<EventArgs<T, DTCMessageType, ClientHandler>> eventForMessage, DTCMessageType messageType,
            ClientHandler clientHandler) where T : IMessage
        {
            var temp = eventForMessage; // for thread safety
            temp?.Invoke(this, new EventArgs<T, DTCMessageType, ClientHandler>(message, messageType, clientHandler));
        }

        public void HandleRequest<T>(ClientHandler clientHandler, DTCMessageType messageType, T message) where T : IMessage
        {
            switch (messageType)
            {
                case DTCMessageType.Heartbeat:
                case DTCMessageType.Logoff:
                case DTCMessageType.EncodingRequest:
                    // This is informational only. Request already has been handled by the clientHandler
                    break;

                case DTCMessageType.LogonRequest:
                    var logonRequest = message as LogonRequest;
                    var logonResponse = new LogonResponse
                    {
                        HistoricalPriceDataSupported = 1u,
                        ProtocolVersion = logonRequest.ProtocolVersion,
                        MarketDataSupported = 1u
                    };
                    clientHandler.SendResponse(DTCMessageType.LogonResponse, logonResponse);
                    break;

                case DTCMessageType.HistoricalPriceDataRequest:
                    var historicalPriceDataRequest = message as HistoricalPriceDataRequest;
                    HistoricalPriceDataResponseHeader.IsZipped = historicalPriceDataRequest.IsZipped;
                    clientHandler.SendResponse(DTCMessageType.HistoricalPriceDataResponseHeader, HistoricalPriceDataResponseHeader);
                    var numSent = 0;
                    for (int i = 0; i < HistoricalPriceDataRecordResponses.Count; i++)
                    {
                        var historicalPriceDataRecordResponse = HistoricalPriceDataRecordResponses[i];
                        if (historicalPriceDataRecordResponse.StartDateTime >= historicalPriceDataRequest.StartDateTime)
                        {
                            numSent++;
                            clientHandler.SendResponse(DTCMessageType.HistoricalPriceDataRecordResponse, historicalPriceDataRecordResponse);
                        }
                    }
                    var historicalPriceDataRecordResponseFinal = new HistoricalPriceDataRecordResponse();
                    historicalPriceDataRecordResponseFinal.IsFinalRecordBool = true;
                    clientHandler.SendResponse(DTCMessageType.HistoricalPriceDataRecordResponse, historicalPriceDataRecordResponseFinal);
                    numSent++;
                    if (historicalPriceDataRequest.IsZipped)
                    {
                        clientHandler.EndZippedWriting();
                    }
                    break;
                case DTCMessageType.MarketDataRequest:
                    var marketDataRequest = message as MarketDataRequest;
                    var numSentMarketData = 0;
                    for (int i = 0; i < NumTradesAndBidAsksToSend; i++)
                    {
                        var marketDataUpdateTradeCompact = MarketDataUpdateTradeCompacts[i];
                        if (marketDataRequest.SymbolID == marketDataUpdateTradeCompact.SymbolID)
                        {
                            numSentMarketData++;
                            clientHandler.SendResponse(DTCMessageType.MarketDataUpdateTradeCompact, marketDataUpdateTradeCompact);
                        }
                    }
                    
                    break;
                case DTCMessageType.MarketDataReject:
                case DTCMessageType.MarketDataSnapshot:
                case DTCMessageType.MarketDataSnapshotInt:
                case DTCMessageType.MarketDataUpdateTrade:
                case DTCMessageType.MarketDataUpdateTradeCompact:
                case DTCMessageType.MarketDataUpdateTradeInt:
                case DTCMessageType.MarketDataUpdateLastTradeSnapshot:
                case DTCMessageType.MarketDataUpdateTradeWithUnbundledIndicator:
                case DTCMessageType.MarketDataUpdateTradeWithUnbundledIndicator2:
                case DTCMessageType.MarketDataUpdateTradeNoTimestamp:
                case DTCMessageType.MarketDataUpdateBidAsk:
                case DTCMessageType.MarketDataUpdateBidAskCompact:
                case DTCMessageType.MarketDataUpdateBidAskNoTimestamp:
                case DTCMessageType.MarketDataUpdateBidAskInt:
                case DTCMessageType.MarketDataUpdateSessionOpen:
                case DTCMessageType.MarketDataUpdateSessionOpenInt:
                case DTCMessageType.MarketDataUpdateSessionHigh:
                case DTCMessageType.MarketDataUpdateSessionHighInt:
                case DTCMessageType.MarketDataUpdateSessionLow:
                case DTCMessageType.MarketDataUpdateSessionLowInt:
                case DTCMessageType.MarketDataUpdateOpenInterest:
                case DTCMessageType.MarketDataUpdateSessionSettlement:
                case DTCMessageType.MarketDataUpdateSessionSettlementInt:
                case DTCMessageType.MarketDataUpdateSessionNumTrades:
                case DTCMessageType.MarketDataUpdateTradingSessionDate:
                case DTCMessageType.MarketDepthReject:
                case DTCMessageType.MarketDepthSnapshotLevel:
                case DTCMessageType.MarketDepthSnapshotLevelInt:
                case DTCMessageType.MarketDepthSnapshotLevelFloat:
                case DTCMessageType.MarketDepthUpdateLevel:
                case DTCMessageType.MarketDepthUpdateLevelFloatWithMilliseconds:
                case DTCMessageType.MarketDepthUpdateLevelNoTimestamp:
                case DTCMessageType.MarketDepthUpdateLevelInt:
                case DTCMessageType.MarketDataFeedStatus:
                case DTCMessageType.MarketDataFeedSymbolStatus:
                case DTCMessageType.TradingSymbolStatus:
                case DTCMessageType.SubmitNewSingleOrder:
                case DTCMessageType.SubmitNewSingleOrderInt:
                case DTCMessageType.SubmitNewOcoOrder:
                case DTCMessageType.SubmitNewOcoOrderInt:
                case DTCMessageType.SubmitFlattenPositionOrder:
                case DTCMessageType.CancelOrder:
                case DTCMessageType.CancelReplaceOrder:
                case DTCMessageType.CancelReplaceOrderInt:
                case DTCMessageType.OpenOrdersRequest:
                case DTCMessageType.OpenOrdersReject:
                case DTCMessageType.OrderUpdate:
                case DTCMessageType.HistoricalOrderFillsRequest:
                case DTCMessageType.HistoricalOrderFillsReject:
                case DTCMessageType.CurrentPositionsRequest:
                case DTCMessageType.CurrentPositionsReject:
                case DTCMessageType.PositionUpdate:
                case DTCMessageType.TradeAccountsRequest:
                case DTCMessageType.ExchangeListRequest:
                case DTCMessageType.SymbolsForExchangeRequest:
                case DTCMessageType.UnderlyingSymbolsForExchangeRequest:
                case DTCMessageType.SymbolsForUnderlyingRequest:
                case DTCMessageType.SecurityDefinitionForSymbolRequest:
                case DTCMessageType.SymbolSearchRequest:
                case DTCMessageType.SecurityDefinitionReject:
                case DTCMessageType.AccountBalanceRequest:
                case DTCMessageType.AccountBalanceReject:
                case DTCMessageType.AccountBalanceUpdate:
                case DTCMessageType.AccountBalanceAdjustment:
                case DTCMessageType.AccountBalanceAdjustmentComplete:
                case DTCMessageType.HistoricalAccountBalancesRequest:
                case DTCMessageType.UserMessage:
                case DTCMessageType.GeneralLogMessage:
                case DTCMessageType.AlertMessage:
                case DTCMessageType.JournalEntryAdd:
                case DTCMessageType.JournalEntriesRequest:
                case DTCMessageType.HistoricalMarketDepthDataRequest:
                    throw new NotImplementedException($"{messageType}");

                case DTCMessageType.MessageTypeUnset:
                case DTCMessageType.LogonResponse:
                case DTCMessageType.EncodingResponse:
                case DTCMessageType.HistoricalOrderFillResponse:
                case DTCMessageType.TradeAccountResponse:
                case DTCMessageType.ExchangeListResponse:
                case DTCMessageType.SecurityDefinitionResponse:
                case DTCMessageType.AccountBalanceAdjustmentReject:
                case DTCMessageType.HistoricalAccountBalancesReject:
                case DTCMessageType.HistoricalAccountBalanceResponse:
                case DTCMessageType.JournalEntriesReject:
                case DTCMessageType.JournalEntryResponse:
                case DTCMessageType.HistoricalPriceDataResponseHeader:
                case DTCMessageType.HistoricalPriceDataReject:
                case DTCMessageType.HistoricalPriceDataRecordResponse:
                case DTCMessageType.HistoricalPriceDataTickRecordResponse:
                case DTCMessageType.HistoricalPriceDataRecordResponseInt:
                case DTCMessageType.HistoricalPriceDataTickRecordResponseInt:
                case DTCMessageType.HistoricalPriceDataResponseTrailer:
                case DTCMessageType.HistoricalMarketDepthDataResponseHeader:
                case DTCMessageType.HistoricalMarketDepthDataReject:
                case DTCMessageType.HistoricalMarketDepthDataRecordResponse:
                    throw new NotSupportedException($"Unexpected request {messageType} in {GetType().Name}.{nameof(HandleRequest)}");
                default:
                    throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null);
            }
            var msg = $"{messageType}:{message}";
            OnMessage(msg);
        }

        private void OnHeartbeatEvent(EventArgs<Heartbeat, DTCMessageType, ClientHandler> e)
        {
            HeartbeatEvent?.Invoke(this, e);
        }

        private void OnLogoffEvent(EventArgs<Logoff, DTCMessageType, ClientHandler> e)
        {
            LogoffEvent?.Invoke(this, e);
        }

        private void OnEncodingRequestEvent(EventArgs<EncodingRequest, DTCMessageType, ClientHandler> e)
        {
            EncodingRequestEvent?.Invoke(this, e);
        }

        private void OnLogonRequestEvent(EventArgs<LogonRequest, DTCMessageType, ClientHandler> e)
        {
            LogonRequestEvent?.Invoke(this, e);
        }

        private void OnMarketDataRequestEvent(EventArgs<MarketDataRequest, DTCMessageType, ClientHandler> e)
        {
            MarketDataRequestEvent?.Invoke(this, e);
        }

        private void OnMarketDepthRequestEvent(EventArgs<MarketDepthRequest, DTCMessageType, ClientHandler> e)
        {
            MarketDepthRequestEvent?.Invoke(this, e);
        }

        private void OnSubmitNewSingleOrderEvent(EventArgs<SubmitNewSingleOrder, DTCMessageType, ClientHandler> e)
        {
            SubmitNewSingleOrderEvent?.Invoke(this, e);
        }

        private void OnSubmitNewOcoOrderIntEvent(EventArgs<SubmitNewOCOOrderInt, DTCMessageType, ClientHandler> e)
        {
            SubmitNewOcoOrderIntEvent?.Invoke(this, e);
        }

        private void OnCancelOrderEvent(EventArgs<CancelOrder, DTCMessageType, ClientHandler> e)
        {
            CancelOrderEvent?.Invoke(this, e);
        }

        private void OnCancelReplaceOrderIntEvent(EventArgs<CancelReplaceOrderInt, DTCMessageType, ClientHandler> e)
        {
            CancelReplaceOrderIntEvent?.Invoke(this, e);
        }

        private void OnOpenOrdersRequestEvent(EventArgs<OpenOrdersRequest, DTCMessageType, ClientHandler> e)
        {
            OpenOrdersRequestEvent?.Invoke(this, e);
        }

        private void OnHistoricalOrderFillsRequestEvent(EventArgs<HistoricalOrderFillsRequest, DTCMessageType, ClientHandler> e)
        {
            HistoricalOrderFillsRequestEvent?.Invoke(this, e);
        }

        private void OnTradeAccountsRequestEvent(EventArgs<TradeAccountsRequest, DTCMessageType, ClientHandler> e)
        {
            TradeAccountsRequestEvent?.Invoke(this, e);
        }

        private void OnCurrentPositionsRequestEvent(EventArgs<CurrentPositionsRequest, DTCMessageType, ClientHandler> e)
        {
            CurrentPositionsRequestEvent?.Invoke(this, e);
        }

        private void OnSubmitNewSingleOrderIntEvent(EventArgs<SubmitNewSingleOrderInt, DTCMessageType, ClientHandler> e)
        {
            SubmitNewSingleOrderIntEvent?.Invoke(this, e);
        }

        private void OnSubmitNewOcoOrderEvent(EventArgs<SubmitNewOCOOrder, DTCMessageType, ClientHandler> e)
        {
            SubmitNewOcoOrderEvent?.Invoke(this, e);
        }

        private void OnCancelReplaceOrderEvent(EventArgs<CancelReplaceOrder, DTCMessageType, ClientHandler> e)
        {
            CancelReplaceOrderEvent?.Invoke(this, e);
        }

        private void OnExchangeListRequestEvent(EventArgs<ExchangeListRequest, DTCMessageType, ClientHandler> e)
        {
            ExchangeListRequestEvent?.Invoke(this, e);
        }

        private void OnSymbolsForExchangeRequestEvent(EventArgs<SymbolsForExchangeRequest, DTCMessageType, ClientHandler> e)
        {
            SymbolsForExchangeRequestEvent?.Invoke(this, e);
        }

        private void OnUnderlyingSymbolsForExchangeRequestEvent(EventArgs<UnderlyingSymbolsForExchangeRequest, DTCMessageType, ClientHandler> e)
        {
            UnderlyingSymbolsForExchangeRequestEvent?.Invoke(this, e);
        }

        private void OnSymbolsForUnderlyingRequestEvent(EventArgs<SymbolsForUnderlyingRequest, DTCMessageType, ClientHandler> e)
        {
            SymbolsForUnderlyingRequestEvent?.Invoke(this, e);
        }

        private void OnSymbolSearchRequestEvent(EventArgs<SymbolSearchRequest, DTCMessageType, ClientHandler> e)
        {
            SymbolSearchRequestEvent?.Invoke(this, e);
        }

        private void OnHistoricalPriceDataRequestEvent(EventArgs<HistoricalPriceDataRequest, DTCMessageType, ClientHandler> e)
        {
            HistoricalPriceDataRequestEvent?.Invoke(this, e);
        }

        private void OnAccountBalanceRequestEvent(EventArgs<AccountBalanceRequest, DTCMessageType, ClientHandler> e)
        {
            AccountBalanceRequestEvent?.Invoke(this, e);
        }

        private void OnSecurityDefinitionForSymbolRequestEvent(EventArgs<SecurityDefinitionForSymbolRequest, DTCMessageType, ClientHandler> e)
        {
            SecurityDefinitionForSymbolRequestEvent?.Invoke(this, e);
        }

        #endregion events
    }
}