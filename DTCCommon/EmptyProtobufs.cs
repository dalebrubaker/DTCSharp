// unset
using System;
using DTCPB;
using Google.Protobuf;

namespace DTCCommon
{
    public static class EmptyProtobufs
    {
        public static IMessage GetEmptyProtobuf(DTCMessageType messageType)
        {
            switch (messageType)
            {
                case DTCMessageType.MessageTypeUnset:
                    return null;
                case DTCMessageType.LogonRequest:
                    return new LogonRequest();
                case DTCMessageType.LogonResponse:
                    return new LogonResponse();
                case DTCMessageType.Heartbeat:
                    return new Heartbeat();
                case DTCMessageType.Logoff:
                    return new Logoff();
                case DTCMessageType.EncodingRequest:
                    return new EncodingRequest();
                case DTCMessageType.EncodingResponse:
                    return new EncodingResponse();
                case DTCMessageType.MarketDataRequest:
                    return new MarketDataRequest();
                case DTCMessageType.MarketDataReject:
                    return new MarketDataReject();
                case DTCMessageType.MarketDataSnapshot:
                    return new MarketDataSnapshot();
                case DTCMessageType.MarketDataSnapshotInt:
                    return new MarketDataSnapshot_Int();
                case DTCMessageType.MarketDataUpdateTrade:
                    return new MarketDataUpdateTrade();
                case DTCMessageType.MarketDataUpdateTradeCompact:
                    return new MarketDataUpdateTradeCompact();
                case DTCMessageType.MarketDataUpdateTradeInt:
                    return new MarketDataUpdateTrade_Int();
                case DTCMessageType.MarketDataUpdateLastTradeSnapshot:
                    return new MarketDataUpdateLastTradeSnapshot();
                case DTCMessageType.MarketDataUpdateTradeWithUnbundledIndicator:
                    return new MarketDataUpdateTradeWithUnbundledIndicator();
                case DTCMessageType.MarketDataUpdateTradeWithUnbundledIndicator2:
                    return new MarketDataUpdateTradeWithUnbundledIndicator2();
                case DTCMessageType.MarketDataUpdateTradeNoTimestamp:
                    return new MarketDataUpdateTradeNoTimestamp();
                case DTCMessageType.MarketDataUpdateBidAsk:
                    return new MarketDataUpdateBidAsk();
                case DTCMessageType.MarketDataUpdateBidAskCompact:
                    return new MarketDataUpdateBidAskCompact();
                case DTCMessageType.MarketDataUpdateBidAskNoTimestamp:
                    return new MarketDataUpdateBidAskNoTimeStamp();
                case DTCMessageType.MarketDataUpdateBidAskInt:
                    return new MarketDataUpdateBidAsk_Int();
                case DTCMessageType.MarketDataUpdateSessionOpen:
                    return new MarketDataUpdateSessionOpen();
                case DTCMessageType.MarketDataUpdateSessionOpenInt:
                    return new MarketDataUpdateSessionOpen_Int();
                case DTCMessageType.MarketDataUpdateSessionHigh:
                    return new MarketDataUpdateSessionHigh();
                case DTCMessageType.MarketDataUpdateSessionHighInt:
                    return new MarketDataUpdateSessionHigh_Int();
                case DTCMessageType.MarketDataUpdateSessionLow:
                    return new MarketDataUpdateSessionLow();
                case DTCMessageType.MarketDataUpdateSessionLowInt:
                    return new MarketDataUpdateSessionLow_Int();
                case DTCMessageType.MarketDataUpdateSessionVolume:
                    return new MarketDataUpdateSessionVolume();
                case DTCMessageType.MarketDataUpdateOpenInterest:
                    return new MarketDataUpdateOpenInterest();
                case DTCMessageType.MarketDataUpdateSessionSettlement:
                    return new MarketDataUpdateSessionSettlement();
                case DTCMessageType.MarketDataUpdateSessionSettlementInt:
                    return new MarketDataUpdateSessionSettlement_Int();
                case DTCMessageType.MarketDataUpdateSessionNumTrades:
                    return new MarketDataUpdateSessionNumTrades();
                case DTCMessageType.MarketDataUpdateTradingSessionDate:
                    return new MarketDataUpdateTradingSessionDate();
                case DTCMessageType.MarketDepthRequest:
                    return new MarketDepthRequest();
                case DTCMessageType.MarketDepthReject:
                    return new MarketDepthReject();
                case DTCMessageType.MarketDepthSnapshotLevel:
                    return new MarketDepthSnapshotLevel();
                case DTCMessageType.MarketDepthSnapshotLevelInt:
                    return new MarketDepthSnapshotLevel_Int();
                case DTCMessageType.MarketDepthSnapshotLevelFloat:
                    return new MarketDepthSnapshotLevelFloat();
                case DTCMessageType.MarketDepthUpdateLevel:
                    return new MarketDepthUpdateLevel();
                case DTCMessageType.MarketDepthUpdateLevelFloatWithMilliseconds:
                    return new MarketDepthUpdateLevelFloatWithMilliseconds();
                case DTCMessageType.MarketDepthUpdateLevelNoTimestamp:
                    return new MarketDepthUpdateLevelNoTimestamp();
                case DTCMessageType.MarketDepthUpdateLevelInt:
                    return new MarketDepthUpdateLevel_Int();
                case DTCMessageType.MarketDataFeedStatus:
                    return new MarketDataFeedStatus();
                case DTCMessageType.MarketDataFeedSymbolStatus:
                    return new MarketDataFeedSymbolStatus();
                case DTCMessageType.TradingSymbolStatus:
                    return new TradingSymbolStatus();
                case DTCMessageType.SubmitNewSingleOrder:
                    return new SubmitNewSingleOrder();
                case DTCMessageType.SubmitNewSingleOrderInt:
                    return new SubmitNewSingleOrderInt();
                case DTCMessageType.SubmitNewOcoOrder:
                    return new SubmitNewOCOOrder();
                case DTCMessageType.SubmitNewOcoOrderInt:
                    return new SubmitNewOCOOrderInt();
                case DTCMessageType.SubmitFlattenPositionOrder:
                    return new SubmitFlattenPositionOrder();
                case DTCMessageType.CancelOrder:
                    return new CancelOrder();
                case DTCMessageType.CancelReplaceOrder:
                    return new CancelReplaceOrder();
                case DTCMessageType.CancelReplaceOrderInt:
                    return new CancelReplaceOrderInt();
                case DTCMessageType.OpenOrdersRequest:
                    return new OpenOrdersRequest();
                case DTCMessageType.OpenOrdersReject:
                    return new OpenOrdersReject();
                case DTCMessageType.OrderUpdate:
                    return new OrderUpdate();
                case DTCMessageType.HistoricalOrderFillsRequest:
                    return new HistoricalOrderFillsRequest();
                case DTCMessageType.HistoricalOrderFillResponse:
                    return new HistoricalOrderFillResponse();
                case DTCMessageType.HistoricalOrderFillsReject:
                    return new HistoricalOrderFillsReject();
                case DTCMessageType.CurrentPositionsRequest:
                    return new CurrentPositionsRequest();
                case DTCMessageType.CurrentPositionsReject:
                    return new CurrentPositionsReject();
                case DTCMessageType.PositionUpdate:
                    return new PositionUpdate();
                case DTCMessageType.TradeAccountsRequest:
                    return new TradeAccountsRequest();
                case DTCMessageType.TradeAccountResponse:
                    return new TradeAccountResponse();
                case DTCMessageType.ExchangeListRequest:
                    return new ExchangeListRequest();
                case DTCMessageType.ExchangeListResponse:
                    return new ExchangeListResponse();
                case DTCMessageType.SymbolsForExchangeRequest:
                    return new SymbolsForExchangeRequest();
                case DTCMessageType.UnderlyingSymbolsForExchangeRequest:
                    return new UnderlyingSymbolsForExchangeRequest();
                case DTCMessageType.SymbolsForUnderlyingRequest:
                    return new SymbolsForUnderlyingRequest();
                case DTCMessageType.SecurityDefinitionForSymbolRequest:
                    return new SecurityDefinitionForSymbolRequest();
                case DTCMessageType.SecurityDefinitionResponse:
                    return new SecurityDefinitionResponse();
                case DTCMessageType.SymbolSearchRequest:
                    return new SymbolSearchRequest();
                case DTCMessageType.SecurityDefinitionReject:
                    return new SecurityDefinitionReject();
                case DTCMessageType.AccountBalanceRequest:
                    return new AccountBalanceRequest();
                case DTCMessageType.AccountBalanceReject:
                    return new AccountBalanceReject();
                case DTCMessageType.AccountBalanceUpdate:
                    return new AccountBalanceUpdate();
                case DTCMessageType.AccountBalanceAdjustment:
                    return new AccountBalanceAdjustment();
                case DTCMessageType.AccountBalanceAdjustmentReject:
                    return new AccountBalanceAdjustmentReject();
                case DTCMessageType.AccountBalanceAdjustmentComplete:
                    return new AccountBalanceAdjustmentComplete();
                case DTCMessageType.HistoricalAccountBalancesRequest:
                    return new HistoricalAccountBalancesRequest();
                case DTCMessageType.HistoricalAccountBalancesReject:
                    return new HistoricalAccountBalancesReject();
                case DTCMessageType.HistoricalAccountBalanceResponse:
                    return new HistoricalAccountBalanceResponse();
                case DTCMessageType.UserMessage:
                    return new UserMessage();
                case DTCMessageType.GeneralLogMessage:
                    return new GeneralLogMessage();
                case DTCMessageType.AlertMessage:
                    return new AlertMessage();
                case DTCMessageType.JournalEntryAdd:
                    return new JournalEntryAdd();
                case DTCMessageType.JournalEntriesRequest:
                    return new JournalEntriesRequest();
                case DTCMessageType.JournalEntriesReject:
                    return new JournalEntriesReject();
                case DTCMessageType.JournalEntryResponse:
                    return new JournalEntryResponse();
                case DTCMessageType.HistoricalPriceDataRequest:
                    return new HistoricalPriceDataRequest();
                case DTCMessageType.HistoricalPriceDataResponseHeader:
                    return new HistoricalPriceDataResponseHeader();
                case DTCMessageType.HistoricalPriceDataReject:
                    return new HistoricalPriceDataReject();
                case DTCMessageType.HistoricalPriceDataRecordResponse:
                    return new HistoricalPriceDataRecordResponse();
                case DTCMessageType.HistoricalPriceDataTickRecordResponse:
                    return new HistoricalPriceDataTickRecordResponse();
                case DTCMessageType.HistoricalPriceDataRecordResponseInt:
                    return new HistoricalPriceDataRecordResponse_Int();
                case DTCMessageType.HistoricalPriceDataTickRecordResponseInt:
                    return new HistoricalPriceDataTickRecordResponse_Int();
                case DTCMessageType.HistoricalPriceDataResponseTrailer:
                    return new HistoricalPriceDataResponseTrailer();
                case DTCMessageType.HistoricalMarketDepthDataRequest:
                    return new HistoricalMarketDepthDataRequest();
                case DTCMessageType.HistoricalMarketDepthDataResponseHeader:
                    return new HistoricalMarketDepthDataResponseHeader();
                case DTCMessageType.HistoricalMarketDepthDataReject:
                    return new HistoricalMarketDepthDataReject();
                case DTCMessageType.HistoricalMarketDepthDataRecordResponse:
                    return new HistoricalMarketDepthDataRecordResponse();
                default:
                    throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null);
            }
        }
    }
}