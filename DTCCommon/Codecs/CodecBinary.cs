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
using uint8_t = System.Byte;


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
            int sizeExcludingHeader;
            switch (messageType)
            {
                case DTCMessageType.MessageTypeUnset:
                    throw new ArgumentException(messageType.ToString());;
                case DTCMessageType.LogonRequest:
                    var logonRequest = message as LogonRequest;
                    sizeExcludingHeader = 4 + USERNAME_PASSWORD_LENGTH + USERNAME_PASSWORD_LENGTH + GENERAL_IDENTIFIER_LENGTH 
                        + 4 + 4 + 4 + 4 + TRADE_ACCOUNT_LENGTH + GENERAL_IDENTIFIER_LENGTH + 32;
                    Utility.WriteHeader(binaryWriter, sizeExcludingHeader, messageType);
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
                    sizeExcludingHeader = 4 + 4 + TEXT_DESCRIPTION_LENGTH + 64 + 4 + 60 + (4 * 1) + SYMBOL_EXCHANGE_DELIMITER_LENGTH + (9 * 1);
                    Utility.WriteHeader(binaryWriter, sizeExcludingHeader, messageType);
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
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;
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
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;
                case DTCMessageType.MarketDataReject:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.MarketDataSnapshot:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.MarketDataSnapshotInt:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateTrade:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateTradeCompact:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateTradeInt:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateLastTradeSnapshot:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateBidAsk:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateBidAskCompact:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateBidAskInt:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateSessionOpen:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateSessionOpenInt:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateSessionHigh:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateSessionHighInt:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateSessionLow:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateSessionLowInt:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateSessionVolume:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateOpenInterest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateSessionSettlement:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateSessionSettlementInt:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateSessionNumTrades:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateTradingSessionDate:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.MarketDepthRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.MarketDepthReject:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.MarketDepthSnapshotLevel:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.MarketDepthSnapshotLevelInt:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.MarketDepthUpdateLevel:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.MarketDepthUpdateLevelCompact:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.MarketDepthUpdateLevelInt:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.MarketDepthFullUpdate10:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.MarketDepthFullUpdate20:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.MarketDataFeedStatus:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.MarketDataFeedSymbolStatus:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.SubmitNewSingleOrder:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.SubmitNewSingleOrderInt:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.SubmitNewOcoOrder:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.SubmitNewOcoOrderInt:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.CancelOrder:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.CancelReplaceOrder:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.CancelReplaceOrderInt:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.OpenOrdersRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.OpenOrdersReject:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.OrderUpdate:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.HistoricalOrderFillsRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.HistoricalOrderFillResponse:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.CurrentPositionsRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.CurrentPositionsReject:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.PositionUpdate:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.TradeAccountsRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.TradeAccountResponse:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.ExchangeListRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.ExchangeListResponse:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.SymbolsForExchangeRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.UnderlyingSymbolsForExchangeRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.SymbolsForUnderlyingRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.SecurityDefinitionForSymbolRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.SecurityDefinitionResponse:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.SymbolSearchRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.SecurityDefinitionReject:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.AccountBalanceRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.AccountBalanceReject:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.AccountBalanceUpdate:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.UserMessage:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.GeneralLogMessage:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;
                case DTCMessageType.HistoricalPriceDataRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.HistoricalPriceDataResponseHeader:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.HistoricalPriceDataReject:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.HistoricalPriceDataRecordResponse:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.HistoricalPriceDataTickRecordResponse:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.HistoricalPriceDataRecordResponseInt:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.HistoricalPriceDataTickRecordResponseInt:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
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
            int i;
            switch (messageType)
            {
                case DTCMessageType.MessageTypeUnset:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.LogonRequest:
                    var logonRequest = result as LogonRequest;
                    i = 0;
                    logonRequest.ProtocolVersion = BitConverter.ToInt32(bytes, i);
                    i += 4;
                    logonRequest.Username = BitConverter.ToString(bytes, i);
                    i += USERNAME_PASSWORD_LENGTH;
                    logonRequest.Password = BitConverter.ToString(bytes, i);
                    i += USERNAME_PASSWORD_LENGTH;
                    logonRequest.GeneralTextData = BitConverter.ToString(bytes, i);
                    i += GENERAL_IDENTIFIER_LENGTH;
                    logonRequest.Integer1 = BitConverter.ToInt32(bytes, i);
                    i += 4;
                    logonRequest.Integer2 = BitConverter.ToInt32(bytes, i);
                    i += 4;
                    logonRequest.HeartbeatIntervalInSeconds = BitConverter.ToInt32(bytes, i);
                    i += 4;
                    logonRequest.TradeMode = (TradeModeEnum)BitConverter.ToInt32(bytes, i);
                    i += 4;
                    logonRequest.TradeAccount = BitConverter.ToString(bytes, i);
                    i += TRADE_ACCOUNT_LENGTH;
                    logonRequest.HardwareIdentifier = BitConverter.ToString(bytes, i);
                    i += GENERAL_IDENTIFIER_LENGTH;
                    logonRequest.ClientName = BitConverter.ToString(bytes, i);
                    i += 32;
                    return result;
                case DTCMessageType.LogonResponse:
                    var logonResponse = result as LogonResponse;
                    i = 0;
                    logonResponse.ProtocolVersion = BitConverter.ToInt32(bytes, i);
                    i += 4;
                    logonResponse.Result = (LogonStatusEnum)BitConverter.ToInt32(bytes, i);
                    i += 4;
                    logonResponse.ResultText = BitConverter.ToString(bytes, i);
                    i += TEXT_DESCRIPTION_LENGTH;
                    logonResponse.ReconnectAddress = BitConverter.ToString(bytes, i);
                    i += 64;
                    logonResponse.Integer1 = BitConverter.ToInt32(bytes, i);
                    i += 4;
                    logonResponse.ServerName = BitConverter.ToString(bytes, i);
                    i += 60;
                    logonResponse.MarketDepthUpdatesBestBidAndAsk = bytes[i++];
                    logonResponse.TradingIsSupported = bytes[i++];
                    logonResponse.OCOOrdersSupported = bytes[i++];
                    logonResponse.OrderCancelReplaceSupported = bytes[i++];
                    logonResponse.SymbolExchangeDelimiter = BitConverter.ToString(bytes, i);
                    i += SYMBOL_EXCHANGE_DELIMITER_LENGTH;
                    logonResponse.SecurityDefinitionsSupported = bytes[i++];
                    logonResponse.HistoricalPriceDataSupported = bytes[i++];
                    logonResponse.ResubscribeWhenMarketDataFeedAvailable = bytes[i++];
                    logonResponse.MarketDepthIsSupported = bytes[i++];
                    logonResponse.OneHistoricalPriceDataRequestPerConnection = bytes[i++];
                    logonResponse.BracketOrdersSupported = bytes[i++];
                    logonResponse.UseIntegerPriceOrderMessages = bytes[i++];
                    logonResponse.UsesMultiplePositionsPerSymbolAndTradeAccount = bytes[i++];
                    logonResponse.MarketDataSupported = bytes[i++];
                    return result;
                case DTCMessageType.Heartbeat:
                    var heartbeat = result as Heartbeat;
                    heartbeat.NumDroppedMessages = BitConverter.ToUInt32(bytes, 0);
                    heartbeat.CurrentDateTime = BitConverter.ToInt64(bytes, 4);
                    return result;
                case DTCMessageType.Logoff:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
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
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.MarketDataReject:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.MarketDataSnapshot:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.MarketDataSnapshotInt:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateTrade:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateTradeCompact:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateTradeInt:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateLastTradeSnapshot:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateBidAsk:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateBidAskCompact:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateBidAskInt:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateSessionOpen:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateSessionOpenInt:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateSessionHigh:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateSessionHighInt:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateSessionLow:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateSessionLowInt:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateSessionVolume:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateOpenInterest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateSessionSettlement:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateSessionSettlementInt:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateSessionNumTrades:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.MarketDataUpdateTradingSessionDate:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.MarketDepthRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.MarketDepthReject:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.MarketDepthSnapshotLevel:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.MarketDepthSnapshotLevelInt:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.MarketDepthUpdateLevel:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.MarketDepthUpdateLevelCompact:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.MarketDepthUpdateLevelInt:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.MarketDepthFullUpdate10:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.MarketDepthFullUpdate20:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.MarketDataFeedStatus:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.MarketDataFeedSymbolStatus:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.SubmitNewSingleOrder:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.SubmitNewSingleOrderInt:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.SubmitNewOcoOrder:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.SubmitNewOcoOrderInt:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.CancelOrder:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.CancelReplaceOrder:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.CancelReplaceOrderInt:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.OpenOrdersRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.OpenOrdersReject:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.OrderUpdate:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.HistoricalOrderFillsRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.HistoricalOrderFillResponse:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.CurrentPositionsRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.CurrentPositionsReject:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.PositionUpdate:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.TradeAccountsRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.TradeAccountResponse:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.ExchangeListRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.ExchangeListResponse:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.SymbolsForExchangeRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.UnderlyingSymbolsForExchangeRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.SymbolsForUnderlyingRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.SecurityDefinitionForSymbolRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.SecurityDefinitionResponse:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.SymbolSearchRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.SecurityDefinitionReject:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.AccountBalanceRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.AccountBalanceReject:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.AccountBalanceUpdate:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.UserMessage:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.GeneralLogMessage:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.HistoricalPriceDataRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.HistoricalPriceDataResponseHeader:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.HistoricalPriceDataReject:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.HistoricalPriceDataRecordResponse:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.HistoricalPriceDataTickRecordResponse:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.HistoricalPriceDataRecordResponseInt:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.HistoricalPriceDataTickRecordResponseInt:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
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
