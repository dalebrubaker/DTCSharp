using System;
using System.Threading.Tasks;
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
        public event EventHandler<string> MessageEvent;

        private void OnMessage(string message)
        {
            var temp = MessageEvent;
            temp?.Invoke(this, message);
        }

        /// <summary>
        /// This method is called for every request received by a client connected to this server.
        /// You must not block this thread for long, as further requests can't be received until you return from this method.
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
                    break;
                case DTCMessageType.Heartbeat:
                    // do nothing
                    break;
                case DTCMessageType.Logoff:
                    // do nothing
                    break;
                case DTCMessageType.EncodingRequest:
                    // In this SPECIAL CASE the response has already been sent to the client.
                    // The request is then sent here for informational purposes
                    var encodingRequest = message as EncodingRequest;
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
                case DTCMessageType.MarketDataUpdateBidAsk:
                    break;
                case DTCMessageType.MarketDataUpdateBidAskCompact:
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
                case DTCMessageType.MarketDepthUpdateLevel:
                    break;
                case DTCMessageType.MarketDepthUpdateLevelCompact:
                    break;
                case DTCMessageType.MarketDepthUpdateLevelInt:
                    break;
                case DTCMessageType.MarketDepthFullUpdate10:
                    break;
                case DTCMessageType.MarketDepthFullUpdate20:
                    break;
                case DTCMessageType.MarketDataFeedStatus:
                    break;
                case DTCMessageType.MarketDataFeedSymbolStatus:
                    break;
                case DTCMessageType.SubmitNewSingleOrder:
                    break;
                case DTCMessageType.SubmitNewSingleOrderInt:
                    break;
                case DTCMessageType.SubmitNewOcoOrder:
                    break;
                case DTCMessageType.SubmitNewOcoOrderInt:
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
                case DTCMessageType.UserMessage:
                    break;
                case DTCMessageType.GeneralLogMessage:
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
                case DTCMessageType.MessageTypeUnset:
                case DTCMessageType.LogonResponse:
                case DTCMessageType.EncodingResponse:
                default:
                    throw new ArgumentOutOfRangeException($"Unexpected MessageType {messageType} received by {this} {nameof(HandleRequest)}.");
            }
        }

    }
}
