// unset
using System;
using System.Collections.Generic;
using System.Linq;
using DTCPB;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Enum = System.Enum;

namespace DTCCommon
{
    public static class EmptyProtobufs
    {
        public static IList<int> GetAllMessageTypes()
        {
            var messageTypes = Enum.GetValues(typeof(DTCMessageType));
            var messageTypesExtended = Enum.GetValues(typeof(DTCSharpMessageType));
            var messageTypesList = messageTypes.OfType<int>().ToList();
            var messageTypesExtendedList = messageTypesExtended.OfType<int>().ToList();
            messageTypesList.AddRange(messageTypesExtendedList);
            return messageTypesList;
        }
        
        public static IMessage GetEmptyProtobuf(DTCSharpMessageType messageType)
        {
            var dtcSharpMessageType = (DTCSharpMessageType)messageType;
            switch (dtcSharpMessageType)
            {
                case DTCSharpMessageType.Unset:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static IMessage GetEmptyProtobuf(DTCMessageType messageType)
        {
            return messageType switch
            {
                DTCMessageType.MessageTypeUnset => new Empty(),
                DTCMessageType.LogonRequest => new LogonRequest(),
                DTCMessageType.LogonResponse => new LogonResponse(),
                DTCMessageType.Heartbeat => new Heartbeat(),
                DTCMessageType.Logoff => new Logoff(),
                DTCMessageType.EncodingRequest => new EncodingRequest(),
                DTCMessageType.EncodingResponse => new EncodingResponse(),
                DTCMessageType.MarketDataRequest => new MarketDataRequest(),
                DTCMessageType.MarketDataReject => new MarketDataReject(),
                DTCMessageType.MarketDataSnapshot => new MarketDataSnapshot(),
                DTCMessageType.MarketDataSnapshotInt => new MarketDataSnapshot_Int(),
                DTCMessageType.MarketDataUpdateTrade => new MarketDataUpdateTrade(),
                DTCMessageType.MarketDataUpdateTradeCompact => new MarketDataUpdateTradeCompact(),
                DTCMessageType.MarketDataUpdateTradeInt => new MarketDataUpdateTrade_Int(),
                DTCMessageType.MarketDataUpdateLastTradeSnapshot => new MarketDataUpdateLastTradeSnapshot(),
                DTCMessageType.MarketDataUpdateTradeWithUnbundledIndicator => new MarketDataUpdateTradeWithUnbundledIndicator(),
                DTCMessageType.MarketDataUpdateTradeWithUnbundledIndicator2 => new MarketDataUpdateTradeWithUnbundledIndicator2(),
                DTCMessageType.MarketDataUpdateTradeNoTimestamp => new MarketDataUpdateTradeNoTimestamp(),
                DTCMessageType.MarketDataUpdateBidAsk => new MarketDataUpdateBidAsk(),
                DTCMessageType.MarketDataUpdateBidAskCompact => new MarketDataUpdateBidAskCompact(),
                DTCMessageType.MarketDataUpdateBidAskNoTimestamp => new MarketDataUpdateBidAskNoTimeStamp(),
                DTCMessageType.MarketDataUpdateBidAskInt => new MarketDataUpdateBidAsk_Int(),
                DTCMessageType.MarketDataUpdateSessionOpen => new MarketDataUpdateSessionOpen(),
                DTCMessageType.MarketDataUpdateSessionOpenInt => new MarketDataUpdateSessionOpen_Int(),
                DTCMessageType.MarketDataUpdateSessionHigh => new MarketDataUpdateSessionHigh(),
                DTCMessageType.MarketDataUpdateSessionHighInt => new MarketDataUpdateSessionHigh_Int(),
                DTCMessageType.MarketDataUpdateSessionLow => new MarketDataUpdateSessionLow(),
                DTCMessageType.MarketDataUpdateSessionLowInt => new MarketDataUpdateSessionLow_Int(),
                DTCMessageType.MarketDataUpdateSessionVolume => new MarketDataUpdateSessionVolume(),
                DTCMessageType.MarketDataUpdateOpenInterest => new MarketDataUpdateOpenInterest(),
                DTCMessageType.MarketDataUpdateSessionSettlement => new MarketDataUpdateSessionSettlement(),
                DTCMessageType.MarketDataUpdateSessionSettlementInt => new MarketDataUpdateSessionSettlement_Int(),
                DTCMessageType.MarketDataUpdateSessionNumTrades => new MarketDataUpdateSessionNumTrades(),
                DTCMessageType.MarketDataUpdateTradingSessionDate => new MarketDataUpdateTradingSessionDate(),
                DTCMessageType.MarketDepthRequest => new MarketDepthRequest(),
                DTCMessageType.MarketDepthReject => new MarketDepthReject(),
                DTCMessageType.MarketDepthSnapshotLevel => new MarketDepthSnapshotLevel(),
                DTCMessageType.MarketDepthSnapshotLevelInt => new MarketDepthSnapshotLevel_Int(),
                DTCMessageType.MarketDepthSnapshotLevelFloat => new MarketDepthSnapshotLevelFloat(),
                DTCMessageType.MarketDepthUpdateLevel => new MarketDepthUpdateLevel(),
                DTCMessageType.MarketDepthUpdateLevelFloatWithMilliseconds => new MarketDepthUpdateLevelFloatWithMilliseconds(),
                DTCMessageType.MarketDepthUpdateLevelNoTimestamp => new MarketDepthUpdateLevelNoTimestamp(),
                DTCMessageType.MarketDepthUpdateLevelInt => new MarketDepthUpdateLevel_Int(),
                DTCMessageType.MarketDataFeedStatus => new MarketDataFeedStatus(),
                DTCMessageType.MarketDataFeedSymbolStatus => new MarketDataFeedSymbolStatus(),
                DTCMessageType.TradingSymbolStatus => new TradingSymbolStatus(),
                DTCMessageType.SubmitNewSingleOrder => new SubmitNewSingleOrder(),
                DTCMessageType.SubmitNewSingleOrderInt => new SubmitNewSingleOrderInt(),
                DTCMessageType.SubmitNewOcoOrder => new SubmitNewOCOOrder(),
                DTCMessageType.SubmitNewOcoOrderInt => new SubmitNewOCOOrderInt(),
                DTCMessageType.SubmitFlattenPositionOrder => new SubmitFlattenPositionOrder(),
                DTCMessageType.CancelOrder => new CancelOrder(),
                DTCMessageType.CancelReplaceOrder => new CancelReplaceOrder(),
                DTCMessageType.CancelReplaceOrderInt => new CancelReplaceOrderInt(),
                DTCMessageType.OpenOrdersRequest => new OpenOrdersRequest(),
                DTCMessageType.OpenOrdersReject => new OpenOrdersReject(),
                DTCMessageType.OrderUpdate => new OrderUpdate(),
                DTCMessageType.HistoricalOrderFillsRequest => new HistoricalOrderFillsRequest(),
                DTCMessageType.HistoricalOrderFillResponse => new HistoricalOrderFillResponse(),
                DTCMessageType.HistoricalOrderFillsReject => new HistoricalOrderFillsReject(),
                DTCMessageType.CurrentPositionsRequest => new CurrentPositionsRequest(),
                DTCMessageType.CurrentPositionsReject => new CurrentPositionsReject(),
                DTCMessageType.PositionUpdate => new PositionUpdate(),
                DTCMessageType.TradeAccountsRequest => new TradeAccountsRequest(),
                DTCMessageType.TradeAccountResponse => new TradeAccountResponse(),
                DTCMessageType.ExchangeListRequest => new ExchangeListRequest(),
                DTCMessageType.ExchangeListResponse => new ExchangeListResponse(),
                DTCMessageType.SymbolsForExchangeRequest => new SymbolsForExchangeRequest(),
                DTCMessageType.UnderlyingSymbolsForExchangeRequest => new UnderlyingSymbolsForExchangeRequest(),
                DTCMessageType.SymbolsForUnderlyingRequest => new SymbolsForUnderlyingRequest(),
                DTCMessageType.SecurityDefinitionForSymbolRequest => new SecurityDefinitionForSymbolRequest(),
                DTCMessageType.SecurityDefinitionResponse => new SecurityDefinitionResponse(),
                DTCMessageType.SymbolSearchRequest => new SymbolSearchRequest(),
                DTCMessageType.SecurityDefinitionReject => new SecurityDefinitionReject(),
                DTCMessageType.AccountBalanceRequest => new AccountBalanceRequest(),
                DTCMessageType.AccountBalanceReject => new AccountBalanceReject(),
                DTCMessageType.AccountBalanceUpdate => new AccountBalanceUpdate(),
                DTCMessageType.AccountBalanceAdjustment => new AccountBalanceAdjustment(),
                DTCMessageType.AccountBalanceAdjustmentReject => new AccountBalanceAdjustmentReject(),
                DTCMessageType.AccountBalanceAdjustmentComplete => new AccountBalanceAdjustmentComplete(),
                DTCMessageType.HistoricalAccountBalancesRequest => new HistoricalAccountBalancesRequest(),
                DTCMessageType.HistoricalAccountBalancesReject => new HistoricalAccountBalancesReject(),
                DTCMessageType.HistoricalAccountBalanceResponse => new HistoricalAccountBalanceResponse(),
                DTCMessageType.UserMessage => new UserMessage(),
                DTCMessageType.GeneralLogMessage => new GeneralLogMessage(),
                DTCMessageType.AlertMessage => new AlertMessage(),
                DTCMessageType.JournalEntryAdd => new JournalEntryAdd(),
                DTCMessageType.JournalEntriesRequest => new JournalEntriesRequest(),
                DTCMessageType.JournalEntriesReject => new JournalEntriesReject(),
                DTCMessageType.JournalEntryResponse => new JournalEntryResponse(),
                DTCMessageType.HistoricalPriceDataRequest => new HistoricalPriceDataRequest(),
                DTCMessageType.HistoricalPriceDataResponseHeader => new HistoricalPriceDataResponseHeader(),
                DTCMessageType.HistoricalPriceDataReject => new HistoricalPriceDataReject(),
                DTCMessageType.HistoricalPriceDataRecordResponse => new HistoricalPriceDataRecordResponse(),
                DTCMessageType.HistoricalPriceDataTickRecordResponse => new HistoricalPriceDataTickRecordResponse(),
                DTCMessageType.HistoricalPriceDataRecordResponseInt => new HistoricalPriceDataRecordResponse_Int(),
                DTCMessageType.HistoricalPriceDataTickRecordResponseInt => new HistoricalPriceDataTickRecordResponse_Int(),
                DTCMessageType.HistoricalPriceDataResponseTrailer => new HistoricalPriceDataResponseTrailer(),
                DTCMessageType.HistoricalMarketDepthDataRequest => new HistoricalMarketDepthDataRequest(),
                DTCMessageType.HistoricalMarketDepthDataResponseHeader => new HistoricalMarketDepthDataResponseHeader(),
                DTCMessageType.HistoricalMarketDepthDataReject => new HistoricalMarketDepthDataReject(),
                DTCMessageType.HistoricalMarketDepthDataRecordResponse => new HistoricalMarketDepthDataRecordResponse(),
                DTCMessageType.MarketOrdersRequest => new MarketOrdersRequest(),
                DTCMessageType.MarketOrdersReject => new MarketOrdersReject(),
                DTCMessageType.MarketOrdersAdd => new MarketOrdersAdd(),
                DTCMessageType.MarketOrdersModify => new MarketOrdersModify(),
                DTCMessageType.MarketOrdersRemove => new MarketOrdersRemove(),
                DTCMessageType.MarketOrdersSnapshotMessageBoundary => new MarketOrdersSnapshotMessageBoundary(),
                DTCMessageType.AddCorrectingOrderFill => new AddCorrectingOrderFill(),
                DTCMessageType.CorrectingOrderFillResponse => new CorrectingOrderFillResponse(),
                DTCMessageType.MarketDataUpdateBidAskFloatWithMicroseconds => new MarketDataUpdateBidAskFloatWithMicroseconds(),
                _ => throw new ArgumentOutOfRangeException($"messageType={messageType}")
            };
        }
    }
}