using System;
using DTCCommon.EventArgsF;
using DTCPB;
using Google.Protobuf;

namespace DTCServer
{
    public interface IServiceDTC
    {
        bool SendSymbols { get; set; }
        event EventHandler<EventArgs<Heartbeat, DTCMessageType, ClientHandler>> HeartbeatEvent;

        event EventHandler<EventArgs<Logoff, DTCMessageType, ClientHandler>> LogoffEvent;

        /// <summary>
        /// This event is only thrown for informational purposes.
        /// HandleMessage() takes care of changing the current encoding and responding.
        /// So do NOT respond to this event.
        /// </summary>
        event EventHandler<EventArgs<EncodingRequest, DTCMessageType, ClientHandler>> EncodingRequestEvent;

        event EventHandler<EventArgs<LogonRequest, DTCMessageType, ClientHandler>> LogonRequestEvent;
        event EventHandler<EventArgs<MarketDataRequest, DTCMessageType, ClientHandler>> MarketDataRequestEvent;
        event EventHandler<EventArgs<MarketDepthRequest, DTCMessageType, ClientHandler>> MarketDepthRequestEvent;
        event EventHandler<EventArgs<SubmitNewSingleOrder, DTCMessageType, ClientHandler>> SubmitNewSingleOrderEvent;
        event EventHandler<EventArgs<SubmitNewSingleOrderInt, DTCMessageType, ClientHandler>> SubmitNewSingleOrderIntEvent;
        event EventHandler<EventArgs<SubmitNewOCOOrder, DTCMessageType, ClientHandler>> SubmitNewOcoOrderEvent;
        event EventHandler<EventArgs<SubmitNewOCOOrderInt, DTCMessageType, ClientHandler>> SubmitNewOcoOrderIntEvent;
        event EventHandler<EventArgs<CancelOrder, DTCMessageType, ClientHandler>> CancelOrderEvent;
        event EventHandler<EventArgs<CancelReplaceOrder, DTCMessageType, ClientHandler>> CancelReplaceOrderEvent;
        event EventHandler<EventArgs<CancelReplaceOrderInt, DTCMessageType, ClientHandler>> CancelReplaceOrderIntEvent;
        event EventHandler<EventArgs<OpenOrdersRequest, DTCMessageType, ClientHandler>> OpenOrdersRequestEvent;
        event EventHandler<EventArgs<HistoricalOrderFillsRequest, DTCMessageType, ClientHandler>> HistoricalOrderFillsRequestEvent;
        event EventHandler<EventArgs<CurrentPositionsRequest, DTCMessageType, ClientHandler>> CurrentPositionsRequestEvent;
        event EventHandler<EventArgs<TradeAccountsRequest, DTCMessageType, ClientHandler>> TradeAccountsRequestEvent;
        event EventHandler<EventArgs<ExchangeListRequest, DTCMessageType, ClientHandler>> ExchangeListRequestEvent;
        event EventHandler<EventArgs<SymbolsForExchangeRequest, DTCMessageType, ClientHandler>> SymbolsForExchangeRequestEvent;
        event EventHandler<EventArgs<UnderlyingSymbolsForExchangeRequest, DTCMessageType, ClientHandler>> UnderlyingSymbolsForExchangeRequestEvent;
        event EventHandler<EventArgs<SymbolsForUnderlyingRequest, DTCMessageType, ClientHandler>> SymbolsForUnderlyingRequestEvent;
        event EventHandler<EventArgs<SecurityDefinitionForSymbolRequest, DTCMessageType, ClientHandler>> SecurityDefinitionForSymbolRequestEvent;
        event EventHandler<EventArgs<SymbolSearchRequest, DTCMessageType, ClientHandler>> SymbolSearchRequestEvent;
        event EventHandler<EventArgs<AccountBalanceRequest, DTCMessageType, ClientHandler>> AccountBalanceRequestEvent;
        event EventHandler<EventArgs<HistoricalPriceDataRequest, DTCMessageType, ClientHandler>> HistoricalPriceDataRequestEvent;
        event EventHandler<string> MessageEvent;
        event EventHandler<EventArgs<string, ClientHandler>> ConnectedEvent;
        event EventHandler<EventArgs<string, ClientHandler>> DisconnectedEvent;

        /// <summary>
        /// This method is called for every request received by a client connected to this server.
        /// WARNING! You must not block this thread for long, as further requests can't be received until you return from this method.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="clientHandler">The handler for a particular client connected to this server</param>
        /// <param name="messageType">the message type</param>
        /// <param name="message">the message (a Google.Protobuf message)</param>
        /// <returns></returns>
        void HandleRequest<T>(ClientHandler clientHandler, DTCMessageType messageType, T message) where T : IMessage;
    }
}