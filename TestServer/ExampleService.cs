using System;
using System.Collections.Generic;
using System.Net;
using DTCCommon.Extensions;
using DTCPB;
using DTCServer;
using Google.Protobuf;
using NLog;

namespace TestServer
{
    /// <summary>
    /// The service implementation that provides responses to client requests.
    /// </summary>
    public sealed class ExampleService : Server
    {
        private static readonly ILogger s_logger = LogManager.GetCurrentClassLogger();

        public ExampleService(IPAddress ipAddress, int port, int timeoutNoActivity, int numTradesAndBidAsksToSend, int numHistoricalPriceDataRecordsToSend) :
            base(ipAddress, port, timeoutNoActivity)
        {
            NumTradesAndBidAsksToSend = numTradesAndBidAsksToSend;
            MarketDataUpdateTradeCompacts = new List<MarketDataUpdateTradeCompact>(NumTradesAndBidAsksToSend);
            MarketDataUpdateBidAskCompacts = new List<MarketDataUpdateBidAskCompact>(NumTradesAndBidAsksToSend);
            MarketDataUpdateBidAskCompacts = new List<MarketDataUpdateBidAskCompact>(NumTradesAndBidAsksToSend);

            NumHistoricalPriceDataRecordsToSend = numHistoricalPriceDataRecordsToSend;
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

        public int NumTradesAndBidAsksToSend { get; }

        public int NumHistoricalPriceDataRecordsToSend { get; }

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

        public event EventHandler<string> MessageEvent;

        private void OnMessage(string message)
        {
            var temp = MessageEvent;
            temp?.Invoke(this, message);
        }

        protected override void HandleRequest(ClientHandler clientHandler, DTCMessageType messageType, IMessage message)
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
                    HistoricalPriceDataResponseHeader.UseZLibCompressionBool = historicalPriceDataRequest.UseZLibCompressionBool;
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
                    if (historicalPriceDataRequest.UseZLibCompressionBool)
                    {
                        clientHandler.EndZippedWriting();
                    }
                    break;
                case DTCMessageType.MarketDataRequest:
                    var marketDataRequest = message as MarketDataRequest;
                    switch (marketDataRequest.RequestAction)
                    {
                        case RequestActionEnum.RequestActionUnset:
                            break;
                        case RequestActionEnum.Subscribe:
                            SendSnapshot(clientHandler);
                            SendMarketData(clientHandler, marketDataRequest);
                            break;
                        case RequestActionEnum.Unsubscribe:
                            // stop sending data
                            break;
                        case RequestActionEnum.Snapshot:
                            SendSnapshot(clientHandler);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
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

        private void SendMarketData(ClientHandler clientHandler, MarketDataRequest marketDataRequest)
        {
            var numSentMarketData = 0;
            var numSentBidAsks = 0;
            for (int i = 0; i < NumTradesAndBidAsksToSend; i++)
            {
                var marketDataUpdateTradeCompact = MarketDataUpdateTradeCompacts[i];
                if (marketDataRequest.SymbolID == marketDataUpdateTradeCompact.SymbolID)
                {
                    numSentMarketData++;
                    clientHandler.SendResponse(DTCMessageType.MarketDataUpdateTradeCompact, marketDataUpdateTradeCompact);
                }
                var marketDataUpdateBidAskCompact = MarketDataUpdateBidAskCompacts[i];
                if (marketDataRequest.SymbolID == marketDataUpdateBidAskCompact.SymbolID)
                {
                    numSentBidAsks++;
                    clientHandler.SendResponse(DTCMessageType.MarketDataUpdateBidAskCompact, marketDataUpdateBidAskCompact);
                }
            }
            s_logger.Debug($"Sent {numSentBidAsks} bid/asks", numSentBidAsks);
        }

        private static void SendSnapshot(ClientHandler clientHandler)
        {
            // First we send a snapshot, then bids and asks
            var marketDataSnapshot = new MarketDataSnapshot
            {
                AskPrice = 1,
                AskQuantity = 1,
                BidAskDateTime = DateTime.UtcNow.UtcToDtcDateTimeWithMilliseconds(),
                BidPrice = 2,
                BidQuantity = 2,
                LastTradeDateTime = DateTime.UtcNow.UtcToDtcDateTimeWithMilliseconds(),
                LastTradePrice = 123,
                LastTradeVolume = 456,
                MarketDepthUpdateDateTime = DateTime.UtcNow.UtcToDtcDateTimeWithMilliseconds(),
                OpenInterest = 789
            };
            clientHandler.SendResponse(DTCMessageType.MarketDataSnapshot, marketDataSnapshot);
        }

        #endregion events
    }
}