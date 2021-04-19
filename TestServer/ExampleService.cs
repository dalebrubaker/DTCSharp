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
    public class ExampleService : IServiceDTC
    {
        public ExampleService()
        {
            NumTradesAndBidAsksToSend = 1000;
            MarketDataUpdateTradeCompacts = new List<MarketDataUpdateTradeCompact>(NumTradesAndBidAsksToSend);
            MarketDataUpdateBidAskCompacts = new List<MarketDataUpdateBidAskCompact>(NumTradesAndBidAsksToSend);
            MarketDataUpdateBidAskCompacts = new List<MarketDataUpdateBidAskCompact>(NumTradesAndBidAsksToSend);

            NumHistoricalPriceDataRecordsToSend = 20000;
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
        public event EventHandler<EventArgs<string, ClientHandler>> ConnectedEvent;
        public event EventHandler<EventArgs<string, ClientHandler>> DisconnectedEvent;
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
            throw new NotImplementedException();
        }

        #endregion events
    }
}