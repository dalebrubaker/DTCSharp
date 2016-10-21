using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DTCPB;
using Google.Protobuf;

namespace DTCCommon.Codecs
{
    public class CodecBinary : ICodecDTC
    {
        public void Write<T>(DTCMessageType messageType, T message, BinaryWriter binaryWriter) where T : IMessage
        {
            switch (messageType)
            {
                case DTCMessageType.MessageTypeUnset:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.LogonRequest:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.LogonResponse:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.Heartbeat:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.Logoff:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.EncodingRequest:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.EncodingResponse:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataRequest:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataReject:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataSnapshot:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataSnapshotInt:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateTrade:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateTradeCompact:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateTradeInt:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateLastTradeSnapshot:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateBidAsk:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateBidAskCompact:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateBidAskInt:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateSessionOpen:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateSessionOpenInt:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateSessionHigh:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateSessionHighInt:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateSessionLow:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateSessionLowInt:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateSessionVolume:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateOpenInterest:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateSessionSettlement:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateSessionSettlementInt:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateSessionNumTrades:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateTradingSessionDate:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDepthRequest:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDepthReject:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDepthSnapshotLevel:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDepthSnapshotLevelInt:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDepthUpdateLevel:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDepthUpdateLevelCompact:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDepthUpdateLevelInt:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDepthFullUpdate10:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDepthFullUpdate20:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataFeedStatus:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataFeedSymbolStatus:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.SubmitNewSingleOrder:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.SubmitNewSingleOrderInt:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.SubmitNewOcoOrder:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.SubmitNewOcoOrderInt:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.CancelOrder:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.CancelReplaceOrder:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.CancelReplaceOrderInt:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.OpenOrdersRequest:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.OpenOrdersReject:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.OrderUpdate:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.HistoricalOrderFillsRequest:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.HistoricalOrderFillResponse:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.CurrentPositionsRequest:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.CurrentPositionsReject:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.PositionUpdate:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.TradeAccountsRequest:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.TradeAccountResponse:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.ExchangeListRequest:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.ExchangeListResponse:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.SymbolsForExchangeRequest:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.UnderlyingSymbolsForExchangeRequest:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.SymbolsForUnderlyingRequest:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.SecurityDefinitionForSymbolRequest:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.SecurityDefinitionResponse:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.SymbolSearchRequest:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.SecurityDefinitionReject:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.AccountBalanceRequest:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.AccountBalanceReject:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.AccountBalanceUpdate:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.UserMessage:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.GeneralLogMessage:
                    throw new NotImplementedException(nameof(messageType));
                case DTCMessageType.HistoricalPriceDataRequest:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.HistoricalPriceDataResponseHeader:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.HistoricalPriceDataReject:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.HistoricalPriceDataRecordResponse:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.HistoricalPriceDataTickRecordResponse:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.HistoricalPriceDataRecordResponseInt:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.HistoricalPriceDataTickRecordResponseInt:
                    throw new NotImplementedException(nameof(messageType));;
                default:
                    throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null);
            }
        }

        /// <summary>
        /// Write the message using binaryWriter
        /// </summary>
        /// <param name="message"></param>
        /// <param name="binaryWriter"></param>
        public void Write<T>(T message, BinaryWriter binaryWriter) where T : IMessage
        {
            var messageType = MessageTypes.MessageTypeByMessage[typeof(T)];
            Write(messageType, message, binaryWriter);
        }

        public T Load<T>(DTCMessageType messageType, byte[] bytes) where T : IMessage<T>, new()
        {
            switch (messageType)
            {
                case DTCMessageType.MessageTypeUnset:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.LogonRequest:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.LogonResponse:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.Heartbeat:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.Logoff:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.EncodingRequest:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.EncodingResponse:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataRequest:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataReject:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataSnapshot:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataSnapshotInt:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateTrade:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateTradeCompact:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateTradeInt:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateLastTradeSnapshot:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateBidAsk:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateBidAskCompact:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateBidAskInt:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateSessionOpen:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateSessionOpenInt:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateSessionHigh:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateSessionHighInt:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateSessionLow:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateSessionLowInt:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateSessionVolume:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateOpenInterest:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateSessionSettlement:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateSessionSettlementInt:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateSessionNumTrades:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataUpdateTradingSessionDate:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDepthRequest:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDepthReject:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDepthSnapshotLevel:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDepthSnapshotLevelInt:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDepthUpdateLevel:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDepthUpdateLevelCompact:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDepthUpdateLevelInt:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDepthFullUpdate10:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDepthFullUpdate20:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataFeedStatus:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.MarketDataFeedSymbolStatus:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.SubmitNewSingleOrder:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.SubmitNewSingleOrderInt:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.SubmitNewOcoOrder:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.SubmitNewOcoOrderInt:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.CancelOrder:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.CancelReplaceOrder:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.CancelReplaceOrderInt:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.OpenOrdersRequest:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.OpenOrdersReject:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.OrderUpdate:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.HistoricalOrderFillsRequest:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.HistoricalOrderFillResponse:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.CurrentPositionsRequest:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.CurrentPositionsReject:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.PositionUpdate:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.TradeAccountsRequest:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.TradeAccountResponse:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.ExchangeListRequest:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.ExchangeListResponse:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.SymbolsForExchangeRequest:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.UnderlyingSymbolsForExchangeRequest:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.SymbolsForUnderlyingRequest:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.SecurityDefinitionForSymbolRequest:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.SecurityDefinitionResponse:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.SymbolSearchRequest:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.SecurityDefinitionReject:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.AccountBalanceRequest:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.AccountBalanceReject:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.AccountBalanceUpdate:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.UserMessage:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.GeneralLogMessage:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.HistoricalPriceDataRequest:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.HistoricalPriceDataResponseHeader:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.HistoricalPriceDataReject:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.HistoricalPriceDataRecordResponse:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.HistoricalPriceDataTickRecordResponse:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.HistoricalPriceDataRecordResponseInt:
                    throw new NotImplementedException(nameof(messageType));;
                case DTCMessageType.HistoricalPriceDataTickRecordResponseInt:
                    throw new NotImplementedException(nameof(messageType));;
                default:
                    throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null);
            }
        }

        /// <summary>
        /// Load the message represented by bytes into the IMessage
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public T Load<T>(byte[] bytes) where T : IMessage<T>, new()
        {
            var messageType = MessageTypes.MessageTypeByMessage[typeof(T)];
            return Load<T>(messageType, bytes);
        }
    }
}
