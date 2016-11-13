using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DTCCommon;
using DTCCommon.Extensions;
using DTCPB;
using DTCServer;
using Google.Protobuf;

namespace TestServer
{
    /// <summary>
    /// The service implementation that provides responses to client requests.
    /// </summary>
    public class ExampleService
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
                    Volume = i + 1,
                };
                MarketDataUpdateTradeCompacts.Add(trade);
                var bidAsk = new MarketDataUpdateBidAskCompact
                {
                    AskPrice = 2000f + i,
                    BidPrice = 2000f + i - 0.25f,
                    AskQuantity = i,
                    BidQuantity = i + 1,
                    DateTime = DateTime.UtcNow.UtcToDtcDateTime4Byte(),
                    SymbolID = 1u,
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
                SymbolID = 1,
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
                    Volume = i + 1,
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
        #region  events

        public event EventHandler<EventArgs<Heartbeat, DTCMessageType, ClientHandler>> HeartbeatEvent;
        public event EventHandler<EventArgs<Logoff, DTCMessageType, ClientHandler>> LogoffEvent;

        /// <summary>
        /// This event is only thrown for informational purposes. 
        /// HandleMessage() takes care of changing the current encoding and responding.
        /// So do NOT respond to this event.
        /// </summary>
        public event EventHandler<EventArgs<EncodingRequest>> EncodingRequestEvent;


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

        private void OnMessage(string message)
        {
            var temp = MessageEvent;
            temp?.Invoke(this, message);
        }

        private void ThrowEvent<T>(T message, EventHandler<EventArgs<T, DTCMessageType, ClientHandler>> eventForMessage, DTCMessageType messageType, ClientHandler clientHandler) where T : IMessage
        {
            var temp = eventForMessage; // for thread safety
            temp?.Invoke(this, new EventArgs<T, DTCMessageType, ClientHandler>(message, messageType, clientHandler));
        }

        #endregion events

        /// <summary>
        /// This method is called for every request received by a client connected to this server.
        /// WARNING! You must not block this thread for long, as further requests can't be received until you return from this method.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="clientHandler">The handler for a particular client connected to this server</param>
        /// <param name="messageType">the message type</param>
        /// <param name="message">the message (a Google.Protobuf message)</param>
        /// <returns></returns>
        public void HandleRequest<T>(ClientHandler clientHandler, DTCMessageType messageType, T message) where T : IMessage
        {
            var clientHandlerId = clientHandler.RemoteEndPoint;
            OnMessage($"Received {messageType} from client {clientHandlerId}");
            switch (messageType)
            {
                case DTCMessageType.LogonRequest:
                    var logonRequest = message as LogonRequest;
                    var logonResponse = new LogonResponse
                    {
                        ProtocolVersion = logonRequest.ProtocolVersion,
                        Result = LogonStatusEnum.LogonSuccess,
                        ResultText = "Logon Successful",
                        ServerName = Environment.MachineName,
                        SecurityDefinitionsSupported = 1,
                        HistoricalPriceDataSupported = 1,
                        MarketDataSupported = 1,
                    };
                    clientHandler.SendResponse(DTCMessageType.LogonResponse, logonResponse);
                    ThrowEvent(logonRequest, LogonRequestEvent, messageType, clientHandler);
                    break;
                case DTCMessageType.Heartbeat:
                    // Don't respond. ClientHandler takes care of it.
                    var heartbeat = message as Heartbeat;
                    ThrowEvent(heartbeat, HeartbeatEvent, messageType, clientHandler);
                    break;
                case DTCMessageType.Logoff:
                    // no response required
                    var logoff = message as Logoff;
                    ThrowEvent(logoff, LogoffEvent, messageType, clientHandler);
                    break;
                case DTCMessageType.EncodingRequest:
                    // Don't respond. ClientHandler takes care of it.
                    // In this SPECIAL CASE the response has already been sent to the client.
                    // The request is then sent here for informational purposes
                    var encodingRequest = message as EncodingRequest;
                    break;
                case DTCMessageType.MarketDataRequest:
                    var marketDataRequest = message as MarketDataRequest;
                    ThrowEvent(marketDataRequest, MarketDataRequestEvent, messageType, clientHandler);
                    clientHandler.SendResponse(DTCMessageType.MarketDataSnapshot, MarketDataSnapshot);
                    foreach (var marketDataUpdateTradeCompact in MarketDataUpdateTradeCompacts)
                    {
                        clientHandler.SendResponse(DTCMessageType.MarketDataUpdateTradeCompact,  marketDataUpdateTradeCompact);
                    }
                    foreach (var marketDataUpdateBidAskCompact in MarketDataUpdateBidAskCompacts)
                    {
                        clientHandler.SendResponse(DTCMessageType.MarketDataUpdateBidAskCompact, marketDataUpdateBidAskCompact);
                    }
                    break;
                case DTCMessageType.MarketDepthRequest:
                    var marketDepthRequest = message as MarketDepthRequest;
                    ThrowEvent(marketDepthRequest, MarketDepthRequestEvent, messageType, clientHandler);
                    throw new NotImplementedException($"{messageType} in {nameof(HandleRequest)}.");
                    break;
                case DTCMessageType.SubmitNewSingleOrder:
                    var submitNewSingleOrder = message as SubmitNewSingleOrder;
                    ThrowEvent(submitNewSingleOrder, SubmitNewSingleOrderEvent, messageType, clientHandler);
                    throw new NotImplementedException($"{messageType} in {nameof(HandleRequest)}.");
                    break;
                case DTCMessageType.SubmitNewSingleOrderInt:
                    var submitNewSingleOrderInt = message as SubmitNewSingleOrderInt;
                    ThrowEvent(submitNewSingleOrderInt, SubmitNewSingleOrderIntEvent, messageType, clientHandler);
                    throw new NotImplementedException($"{messageType} in {nameof(HandleRequest)}.");
                    break;
                case DTCMessageType.SubmitNewOcoOrder:
                    var submitNewOcoOrder = message as SubmitNewOCOOrder;
                    ThrowEvent(submitNewOcoOrder, SubmitNewOcoOrderEvent, messageType, clientHandler);
                    throw new NotImplementedException($"{messageType} in {nameof(HandleRequest)}.");
                    break;
                case DTCMessageType.SubmitNewOcoOrderInt:
                    var submitNewOcoOrderInt = message as SubmitNewOCOOrderInt;
                    ThrowEvent(submitNewOcoOrderInt, SubmitNewOcoOrderIntEvent, messageType, clientHandler);
                    throw new NotImplementedException($"{messageType} in {nameof(HandleRequest)}.");
                    break;
                case DTCMessageType.CancelOrder:
                    var cancelOrder = message as CancelOrder;
                    ThrowEvent(cancelOrder, CancelOrderEvent, messageType, clientHandler);
                    throw new NotImplementedException($"{messageType} in {nameof(HandleRequest)}.");
                    break;
                case DTCMessageType.CancelReplaceOrder:
                    var cancelReplaceOrder = message as CancelReplaceOrder;
                    ThrowEvent(cancelReplaceOrder, CancelReplaceOrderEvent, messageType, clientHandler);
                    throw new NotImplementedException($"{messageType} in {nameof(HandleRequest)}.");
                    break;
                case DTCMessageType.CancelReplaceOrderInt:
                    var cancelReplaceOrderInt = message as CancelReplaceOrderInt;
                    ThrowEvent(cancelReplaceOrderInt, CancelReplaceOrderIntEvent, messageType, clientHandler);
                    throw new NotImplementedException($"{messageType} in {nameof(HandleRequest)}.");
                    break;
                case DTCMessageType.OpenOrdersRequest:
                    var openOrdersRequest = message as OpenOrdersRequest;
                    ThrowEvent(openOrdersRequest, OpenOrdersRequestEvent, messageType, clientHandler);
                    throw new NotImplementedException($"{messageType} in {nameof(HandleRequest)}.");
                    break;
                case DTCMessageType.HistoricalOrderFillsRequest:
                    var historicalOrderFillsRequest = message as HistoricalOrderFillsRequest;
                    ThrowEvent(historicalOrderFillsRequest, HistoricalOrderFillsRequestEvent, messageType, clientHandler);
                    throw new NotImplementedException($"{messageType} in {nameof(HandleRequest)}.");
                    break;
                case DTCMessageType.CurrentPositionsRequest:
                    var currentPositionsRequest = message as CurrentPositionsRequest;
                    ThrowEvent(currentPositionsRequest, CurrentPositionsRequestEvent, messageType, clientHandler);
                    throw new NotImplementedException($"{messageType} in {nameof(HandleRequest)}.");
                    break;
                case DTCMessageType.TradeAccountsRequest:
                    var tradeAccountsRequest = message as TradeAccountsRequest;
                    ThrowEvent(tradeAccountsRequest, TradeAccountsRequestEvent, messageType, clientHandler);
                    throw new NotImplementedException($"{messageType} in {nameof(HandleRequest)}.");
                    break;
                case DTCMessageType.ExchangeListRequest:
                    var exchangeListRequest = message as ExchangeListRequest;
                    ThrowEvent(exchangeListRequest, ExchangeListRequestEvent, messageType, clientHandler);
                    throw new NotImplementedException($"{messageType} in {nameof(HandleRequest)}.");
                    break;
                case DTCMessageType.SymbolsForExchangeRequest:
                    var symbolsForExchangeRequest = message as SymbolsForExchangeRequest;
                    ThrowEvent(symbolsForExchangeRequest, SymbolsForExchangeRequestEvent, messageType, clientHandler);
                    throw new NotImplementedException($"{messageType} in {nameof(HandleRequest)}.");
                    break;
                case DTCMessageType.UnderlyingSymbolsForExchangeRequest:
                    var underlyingSymbolsForExchangeRequest = message as UnderlyingSymbolsForExchangeRequest;
                    ThrowEvent(underlyingSymbolsForExchangeRequest, UnderlyingSymbolsForExchangeRequestEvent, messageType, clientHandler);
                    throw new NotImplementedException($"{messageType} in {nameof(HandleRequest)}.");
                    break;
                case DTCMessageType.SymbolsForUnderlyingRequest:
                    var symbolsForUnderlyingRequest = message as SymbolsForUnderlyingRequest;
                    ThrowEvent(symbolsForUnderlyingRequest, SymbolsForUnderlyingRequestEvent, messageType, clientHandler);
                    throw new NotImplementedException($"{messageType} in {nameof(HandleRequest)}.");
                    break;
                case DTCMessageType.SecurityDefinitionForSymbolRequest:
                    var securityDefinitionForSymbolRequest = message as SecurityDefinitionForSymbolRequest;
                    ThrowEvent(securityDefinitionForSymbolRequest, SecurityDefinitionForSymbolRequestEvent, messageType, clientHandler);
                    throw new NotImplementedException($"{messageType} in {nameof(HandleRequest)}.");
                    break;
                case DTCMessageType.SymbolSearchRequest:
                    var symbolSearchRequest = message as SymbolSearchRequest;
                    ThrowEvent(symbolSearchRequest, SymbolSearchRequestEvent, messageType, clientHandler);
                    throw new NotImplementedException($"{messageType} in {nameof(HandleRequest)}.");
                    break;
                case DTCMessageType.AccountBalanceRequest:
                    var accountBalanceRequest = message as AccountBalanceRequest;
                    ThrowEvent(accountBalanceRequest, AccountBalanceRequestEvent, messageType, clientHandler);
                    throw new NotImplementedException($"{messageType} in {nameof(HandleRequest)}.");
                    break;
                case DTCMessageType.HistoricalPriceDataRequest:
                    var historicalPriceDataRequest = message as HistoricalPriceDataRequest;
                    ThrowEvent(historicalPriceDataRequest, HistoricalPriceDataRequestEvent, messageType, clientHandler);

                    // DO NOT send the HistoricalPriceDataResponseHeader, which was already done for you in clientHandler.
                    // This is because the HistoricalPriceDataResponseHeader must be sent without compression 
                    // and ONLY THEN does the clientHandler switches to compression mode (if requested)

                    HistoricalPriceDataResponseHeader.RequestID = historicalPriceDataRequest.RequestID;
                    HistoricalPriceDataResponseHeader.UseZLibCompression = historicalPriceDataRequest.UseZLibCompression;
                    var zip = historicalPriceDataRequest.UseZLibCompression != 0;
                    clientHandler.SendResponse(DTCMessageType.HistoricalPriceDataResponseHeader, HistoricalPriceDataResponseHeader, thenSwitchToZipped: zip);
                    for (int i = 0; i < NumHistoricalPriceDataRecordsToSend; i++)
                    {
                        var response = HistoricalPriceDataRecordResponses[i];
                        //if ((i + 1) % ushort.MaxValue == 0)
                        //{
                        //    // I think Sierra Chart sends 16,384 records max in a batch TODO check this
                        //    response.IsFinalRecord = 1;
                        //}
                        if (i == NumHistoricalPriceDataRecordsToSend - 1)
                        {
                            response.IsFinalRecord = 1;
                        }
                        response.RequestID = historicalPriceDataRequest.RequestID;
                        clientHandler.SendResponse(DTCMessageType.HistoricalPriceDataRecordResponse, response);
                    }
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
                    throw new ArgumentOutOfRangeException($"Unexpected MessageType {messageType} received by {this} {nameof(HandleRequest)}.");
            }
        }

    }
}
