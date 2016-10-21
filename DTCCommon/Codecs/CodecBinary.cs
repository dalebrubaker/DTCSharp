using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DTCCommon.Extensions;
using DTCPB;
using Google.Protobuf;
using int32_t = System.Int32;
using uint8_t = System.UInt64;


namespace DTCCommon.Codecs
{
    public class CodecBinary : ICodecDTC
    {

        // Text string lengths. Copied from DTCProtocol.h
        const int32_t USERNAME_PASSWORD_LENGTH = 32;
        const int32_t SYMBOL_EXCHANGE_DELIMITER_LENGTH = 4;
        const int32_t SYMBOL_LENGTH = 64;
        const int32_t EXCHANGE_LENGTH = 16;
        const int32_t UNDERLYING_SYMBOL_LENGTH = 32;
        const int32_t SYMBOL_DESCRIPTION_LENGTH = 64;//Previously 48
        const int32_t EXCHANGE_DESCRIPTION_LENGTH = 48;
        const int32_t ORDER_ID_LENGTH = 32;
        const int32_t TRADE_ACCOUNT_LENGTH = 32;
        const int32_t TEXT_DESCRIPTION_LENGTH = 96;
        const int32_t TEXT_MESSAGE_LENGTH = 256;
        const int32_t ORDER_FREE_FORM_TEXT_LENGTH = 48;
        const int32_t CLIENT_SERVER_NAME_LENGTH = 48;
        const int32_t GENERAL_IDENTIFIER_LENGTH = 64;

        public void Write<T>(DTCMessageType messageType, T message, BinaryWriter binaryWriter) where T : IMessage
        {
            int size = 0;
            switch (messageType)
            {
                case DTCMessageType.MessageTypeUnset:
                    throw new ArgumentException(messageType.ToString());;
                case DTCMessageType.LogonRequest:
                    var logonRequest = message as LogonRequest;
                    size = 2 + 2 + 4 + USERNAME_PASSWORD_LENGTH + USERNAME_PASSWORD_LENGTH + GENERAL_IDENTIFIER_LENGTH 
                        + 4 + 4 + 4 + 4 + TRADE_ACCOUNT_LENGTH + GENERAL_IDENTIFIER_LENGTH + 32;
                    Utility.WriteHeader(binaryWriter, size, messageType);
                    binaryWriter.Write(logonRequest.ProtocolVersion);
                    binaryWriter.Write(logonRequest.Username.ToFixedBytes(USERNAME_PASSWORD_LENGTH));
                    binaryWriter.Write(logonRequest.Password.ToFixedBytes(USERNAME_PASSWORD_LENGTH));
                    binaryWriter.Write(logonRequest.GeneralTextData.ToFixedBytes(GENERAL_IDENTIFIER_LENGTH));
                    binaryWriter.Write(logonRequest.Integer1);
                    binaryWriter.Write(logonRequest.Integer2);
                    binaryWriter.Write(logonRequest.HeartbeatIntervalInSeconds);
                    binaryWriter.Write((int)logonRequest.TradeMode);
                    binaryWriter.Write(logonRequest.TradeAccount.ToFixedBytes(TRADE_ACCOUNT_LENGTH));
                    binaryWriter.Write(logonRequest.HardwareIdentifier.ToFixedBytes(GENERAL_IDENTIFIER_LENGTH));
                    binaryWriter.Write(logonRequest.ClientName.ToFixedBytes(32));
                    return;
                case DTCMessageType.LogonResponse:
                    var logonResponse = message as LogonResponse;
                    size = 2 + 2 + 4 + 4 + TEXT_DESCRIPTION_LENGTH + 64 + 4 + 60 + 8 * 4
                           + SYMBOL_EXCHANGE_DELIMITER_LENGTH + 8 * 9;
                    Utility.WriteHeader(binaryWriter, size, messageType);
                    binaryWriter.Write(logonResponse.ProtocolVersion);
                    binaryWriter.Write((int)logonResponse.Result);
                    binaryWriter.Write(logonResponse.ResultText.ToFixedBytes(TEXT_DESCRIPTION_LENGTH));
                    binaryWriter.Write(logonResponse.ReconnectAddress.ToFixedBytes(64));
                    binaryWriter.Write(logonResponse.Integer1);
                    binaryWriter.Write(logonResponse.ServerName.ToFixedBytes(60));
                    binaryWriter.Write((uint8_t)logonResponse.MarketDepthUpdatesBestBidAndAsk);
                    binaryWriter.Write((uint8_t)logonResponse.TradingIsSupported);
                    binaryWriter.Write((uint8_t)logonResponse.OCOOrdersSupported);
                    binaryWriter.Write((uint8_t)logonResponse.OrderCancelReplaceSupported);
                    binaryWriter.Write(logonResponse.SymbolExchangeDelimiter.ToFixedBytes(SYMBOL_EXCHANGE_DELIMITER_LENGTH));
                    binaryWriter.Write((uint8_t)logonResponse.SecurityDefinitionsSupported);
                    binaryWriter.Write((uint8_t)logonResponse.HistoricalPriceDataSupported);
                    binaryWriter.Write((uint8_t)logonResponse.ResubscribeWhenMarketDataFeedAvailable);
                    binaryWriter.Write((uint8_t)logonResponse.MarketDepthIsSupported);
                    binaryWriter.Write((uint8_t)logonResponse.OneHistoricalPriceDataRequestPerConnection);
                    binaryWriter.Write((uint8_t)logonResponse.BracketOrdersSupported);
                    binaryWriter.Write((uint8_t)logonResponse.UseIntegerPriceOrderMessages);
                    binaryWriter.Write((uint8_t)logonResponse.UsesMultiplePositionsPerSymbolAndTradeAccount);
                    binaryWriter.Write((uint8_t)logonResponse.MarketDataSupported);
                    return;
                case DTCMessageType.Heartbeat:
                    var heartbeat = message as Heartbeat;
                    Utility.WriteHeader(binaryWriter, 12, messageType);
                    binaryWriter.Write(heartbeat.NumDroppedMessages);
                    binaryWriter.Write(heartbeat.CurrentDateTime); 
                    return;
                case DTCMessageType.Logoff:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.EncodingRequest:
                    var encodingRequest = message as EncodingRequest;
                    Utility.WriteHeader(binaryWriter, 12, messageType);
                    binaryWriter.Write(encodingRequest.ProtocolVersion);
                    binaryWriter.Write((int)encodingRequest.Encoding); // enum size is 4
                    var protocolType = encodingRequest.ProtocolType.ToFixedBytes(4);
                    binaryWriter.Write(protocolType); // 3 chars DTC plus null terminator 
                    return;
                case DTCMessageType.EncodingResponse:
                    var encodingResponse = message as EncodingResponse;
                    Utility.WriteHeader(binaryWriter, 12, messageType);
                    binaryWriter.Write(encodingResponse.ProtocolVersion);
                    binaryWriter.Write((int)encodingResponse.Encoding); // enum size is 4
                    var protocolType2 = encodingResponse.ProtocolType.ToFixedBytes(4);
                    binaryWriter.Write(protocolType2); // 3 chars DTC plus null terminator 
                    return;
                case DTCMessageType.MarketDataRequest:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataReject:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataSnapshot:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataSnapshotInt:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateTrade:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateTradeCompact:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateTradeInt:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateLastTradeSnapshot:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateBidAsk:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateBidAskCompact:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateBidAskInt:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateSessionOpen:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateSessionOpenInt:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateSessionHigh:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateSessionHighInt:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateSessionLow:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateSessionLowInt:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateSessionVolume:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateOpenInterest:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateSessionSettlement:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateSessionSettlementInt:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateSessionNumTrades:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateTradingSessionDate:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDepthRequest:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDepthReject:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDepthSnapshotLevel:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDepthSnapshotLevelInt:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDepthUpdateLevel:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDepthUpdateLevelCompact:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDepthUpdateLevelInt:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDepthFullUpdate10:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDepthFullUpdate20:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataFeedStatus:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataFeedSymbolStatus:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.SubmitNewSingleOrder:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.SubmitNewSingleOrderInt:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.SubmitNewOcoOrder:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.SubmitNewOcoOrderInt:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.CancelOrder:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.CancelReplaceOrder:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.CancelReplaceOrderInt:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.OpenOrdersRequest:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.OpenOrdersReject:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.OrderUpdate:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.HistoricalOrderFillsRequest:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.HistoricalOrderFillResponse:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.CurrentPositionsRequest:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.CurrentPositionsReject:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.PositionUpdate:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.TradeAccountsRequest:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.TradeAccountResponse:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.ExchangeListRequest:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.ExchangeListResponse:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.SymbolsForExchangeRequest:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.UnderlyingSymbolsForExchangeRequest:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.SymbolsForUnderlyingRequest:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.SecurityDefinitionForSymbolRequest:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.SecurityDefinitionResponse:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.SymbolSearchRequest:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.SecurityDefinitionReject:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.AccountBalanceRequest:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.AccountBalanceReject:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.AccountBalanceUpdate:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.UserMessage:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.GeneralLogMessage:
                    throw new NotImplementedException(messageType.ToString());
                case DTCMessageType.HistoricalPriceDataRequest:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.HistoricalPriceDataResponseHeader:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.HistoricalPriceDataReject:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.HistoricalPriceDataRecordResponse:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.HistoricalPriceDataTickRecordResponse:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.HistoricalPriceDataRecordResponseInt:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.HistoricalPriceDataTickRecordResponseInt:
                    throw new NotImplementedException(messageType.ToString());;
                default:
                    throw new ArgumentOutOfRangeException(messageType.ToString(), messageType, null);
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
            var result = new T();
            switch (messageType)
            {
                case DTCMessageType.MessageTypeUnset:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.LogonRequest:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.LogonResponse:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.Heartbeat:
                    var heartbeat = result as Heartbeat;
                    heartbeat.NumDroppedMessages = BitConverter.ToUInt32(bytes, 0);
                    heartbeat.CurrentDateTime = BitConverter.ToInt64(bytes, 4);
                    return result;
                case DTCMessageType.Logoff:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.EncodingRequest:
                    var encodingRequest = result as EncodingRequest;
                    encodingRequest.ProtocolVersion = BitConverter.ToInt32(bytes, 0);
                    encodingRequest.Encoding = (EncodingEnum)BitConverter.ToInt32(bytes, 4);
                    encodingRequest.ProtocolType = BitConverter.ToString(bytes, 8);
                    return result;
                case DTCMessageType.EncodingResponse:
                    var encodingResponse = result as EncodingResponse;
                    encodingResponse.ProtocolVersion = BitConverter.ToInt32(bytes, 0);
                    encodingResponse.Encoding = (EncodingEnum)BitConverter.ToInt32(bytes, 4);
                    encodingResponse.ProtocolType = BitConverter.ToString(bytes, 8);
                    return result;
                case DTCMessageType.MarketDataRequest:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataReject:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataSnapshot:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataSnapshotInt:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateTrade:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateTradeCompact:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateTradeInt:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateLastTradeSnapshot:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateBidAsk:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateBidAskCompact:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateBidAskInt:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateSessionOpen:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateSessionOpenInt:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateSessionHigh:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateSessionHighInt:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateSessionLow:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateSessionLowInt:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateSessionVolume:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateOpenInterest:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateSessionSettlement:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateSessionSettlementInt:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateSessionNumTrades:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataUpdateTradingSessionDate:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDepthRequest:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDepthReject:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDepthSnapshotLevel:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDepthSnapshotLevelInt:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDepthUpdateLevel:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDepthUpdateLevelCompact:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDepthUpdateLevelInt:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDepthFullUpdate10:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDepthFullUpdate20:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataFeedStatus:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.MarketDataFeedSymbolStatus:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.SubmitNewSingleOrder:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.SubmitNewSingleOrderInt:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.SubmitNewOcoOrder:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.SubmitNewOcoOrderInt:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.CancelOrder:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.CancelReplaceOrder:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.CancelReplaceOrderInt:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.OpenOrdersRequest:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.OpenOrdersReject:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.OrderUpdate:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.HistoricalOrderFillsRequest:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.HistoricalOrderFillResponse:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.CurrentPositionsRequest:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.CurrentPositionsReject:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.PositionUpdate:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.TradeAccountsRequest:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.TradeAccountResponse:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.ExchangeListRequest:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.ExchangeListResponse:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.SymbolsForExchangeRequest:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.UnderlyingSymbolsForExchangeRequest:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.SymbolsForUnderlyingRequest:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.SecurityDefinitionForSymbolRequest:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.SecurityDefinitionResponse:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.SymbolSearchRequest:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.SecurityDefinitionReject:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.AccountBalanceRequest:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.AccountBalanceReject:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.AccountBalanceUpdate:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.UserMessage:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.GeneralLogMessage:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.HistoricalPriceDataRequest:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.HistoricalPriceDataResponseHeader:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.HistoricalPriceDataReject:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.HistoricalPriceDataRecordResponse:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.HistoricalPriceDataTickRecordResponse:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.HistoricalPriceDataRecordResponseInt:
                    throw new NotImplementedException(messageType.ToString());;
                case DTCMessageType.HistoricalPriceDataTickRecordResponseInt:
                    throw new NotImplementedException(messageType.ToString());;
                default:
                    throw new ArgumentOutOfRangeException(messageType.ToString(), messageType, null);
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
