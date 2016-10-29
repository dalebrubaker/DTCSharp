using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DTCCommon;
using DTCPB;
using Google.Protobuf;

namespace DTCServer
{
    public class Server
    {
        private int _port;
        private readonly int _heartbeatIntervalInSeconds;
        private readonly bool _useHeartbeat;
        private IPAddress _ipAddress;

        /// <summary>
        /// Start a TCP Listener on port at ipAddress
        /// If not useHeartbeat, won't do a heartbeat. See: http://www.sierrachart.com/index.php?page=doc/DTCServer.php#HistoricalPriceDataServer
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="port"></param>
        /// <param name="heartbeatIntervalInSeconds">The initial interval in seconds that each side, the Client and the Server, needs to use to send HEARTBEAT messages to the other side. This should be a value from anywhere from 5 to 60 seconds.</param>
        /// <param name="useHeartbeat"><c>true</c>no heartbeat sent to server and none checked from server</param>
        public Server(IPAddress ipAddress, int port, int heartbeatIntervalInSeconds, bool useHeartbeat)
        {
            _ipAddress = ipAddress;
            _port = port;
            _heartbeatIntervalInSeconds = heartbeatIntervalInSeconds;
            _useHeartbeat = useHeartbeat;
        }


        #region events

        public event EventHandler<EventArgs<EncodingRequest>> EncodingRequestEvent;
        public event EventHandler<EventArgs<Heartbeat>> HeartbeatEvent;
        public event EventHandler<EventArgs<Logoff>> LogoffEvent;
        public event EventHandler<EventArgs<LogonRequest>> LogonRequestEvent;
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
        /// Run until cancelled  by CancellationTokenSource.Cancel()
        /// </summary>
        /// <param name="cancellationToken"></param>
        public async void Run(CancellationToken cancellationToken)
        {
            var listener = new TcpListener(_ipAddress, _port);
            listener.Start();
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using (var tcpClient = await listener.AcceptTcpClientAsync())
                    {
                        var temp = tcpClient;
#pragma warning disable 4014
                        Task.Run(() => Handle(temp, cancellationToken), cancellationToken);
#pragma warning restore 4014
                    }
                }
                catch (Exception ex)
                {
                    
                    throw;
                }
            }
            
        }

        private async Task Handle(TcpClient tcpClient, CancellationToken cancellationToken)
        {
            var clientHandler = new ClientHandler(tcpClient, _useHeartbeat);
            await clientHandler.Run(cancellationToken, MessageCallback);
        }

        private void ThrowEvent<T>(T message, EventHandler<EventArgs<T>> eventForMessage) where T : IMessage
        {
            var temp = eventForMessage; // for thread safety
            temp?.Invoke(this, new EventArgs<T>(message));
        }

        private void MessageCallback<T>(DTCMessageType messageType, T message) where T : IMessage
        {
            switch (messageType)
            {
                case DTCMessageType.LogonRequest:
                    ThrowEvent(message as LogonRequest, LogonRequestEvent);
                    break;
                case DTCMessageType.Heartbeat:
                    ThrowEvent(message as Heartbeat, HeartbeatEvent);
                    break;
                case DTCMessageType.Logoff:
                    break;
                case DTCMessageType.EncodingRequest:
                    break;
                case DTCMessageType.EncodingResponse:
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
                default:
                    throw new ArgumentOutOfRangeException($"Unexpected MessageType {messageType} received by {this} {nameof(MessageCallback)}.");
            }
        }

        public override string ToString()
        {
            return $"{_ipAddress}:{_port}";
        }

    }
}
