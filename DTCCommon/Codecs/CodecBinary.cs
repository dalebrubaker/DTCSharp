﻿using System;
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
                    var logoff = message as Logoff;
                    sizeExcludingHeader = TEXT_DESCRIPTION_LENGTH + 1;
                    Utility.WriteHeader(binaryWriter, sizeExcludingHeader, messageType);
                    binaryWriter.Write(logoff.Reason.ToFixedBytes(TEXT_DESCRIPTION_LENGTH));
                    binaryWriter.Write((uint8_t)logoff.DoNotReconnect);
                    return;
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
                    var exchangeListRequest = message as ExchangeListRequest;
                    sizeExcludingHeader = 4;
                    Utility.WriteHeader(binaryWriter, sizeExcludingHeader, messageType);
                    binaryWriter.Write(exchangeListRequest.RequestID);
                    return;
                case DTCMessageType.ExchangeListResponse:
                    var exchangeListResponse = message as ExchangeListResponse;
                    sizeExcludingHeader = 4 + EXCHANGE_LENGTH + 1 + EXCHANGE_DESCRIPTION_LENGTH;
                    Utility.WriteHeader(binaryWriter, sizeExcludingHeader, messageType);
                    binaryWriter.Write(exchangeListResponse.RequestID);
                    binaryWriter.Write(exchangeListResponse.Exchange.ToFixedBytes(EXCHANGE_LENGTH));
                    binaryWriter.Write((uint8_t)exchangeListResponse.IsFinalMessage);
                    binaryWriter.Write(exchangeListResponse.Description.ToFixedBytes(EXCHANGE_DESCRIPTION_LENGTH));
                    return;
                case DTCMessageType.SymbolsForExchangeRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.UnderlyingSymbolsForExchangeRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.SymbolsForUnderlyingRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ;;
                case DTCMessageType.SecurityDefinitionForSymbolRequest:
                    var securityDefinitionForSymbolRequest = message as SecurityDefinitionForSymbolRequest;
                    sizeExcludingHeader = 4 + SYMBOL_LENGTH + EXCHANGE_LENGTH;
                    Utility.WriteHeader(binaryWriter, sizeExcludingHeader, messageType);
                    binaryWriter.Write(securityDefinitionForSymbolRequest.RequestID);
                    binaryWriter.Write(securityDefinitionForSymbolRequest.Symbol.ToFixedBytes(SYMBOL_LENGTH));
                    binaryWriter.Write(securityDefinitionForSymbolRequest.Exchange.ToFixedBytes(EXCHANGE_LENGTH));
                    return;
                case DTCMessageType.SecurityDefinitionResponse:
                    var securityDefinitionResponse = message as SecurityDefinitionResponse;
                    sizeExcludingHeader = 4 + SYMBOL_LENGTH + EXCHANGE_LENGTH + 4 + SYMBOL_DESCRIPTION_LENGTH + (3 * 4) + 1 + (2 * 4) + UNDERLYING_SYMBOL_LENGTH 
                        + 1 + 4 + 1 + (9 * 4) + 1 + 4 + SYMBOL_LENGTH;
                    Utility.WriteHeader(binaryWriter, sizeExcludingHeader, messageType);
                    binaryWriter.Write(securityDefinitionResponse.RequestID);
                    binaryWriter.Write(securityDefinitionResponse.Symbol.ToFixedBytes(SYMBOL_LENGTH));
                    binaryWriter.Write(securityDefinitionResponse.Exchange.ToFixedBytes(EXCHANGE_LENGTH));
                    binaryWriter.Write((int)securityDefinitionResponse.SecurityType);
                    binaryWriter.Write(securityDefinitionResponse.Description.ToFixedBytes(SYMBOL_DESCRIPTION_LENGTH));
                    binaryWriter.Write(securityDefinitionResponse.MinPriceIncrement);
                    binaryWriter.Write((int)securityDefinitionResponse.PriceDisplayFormat);
                    binaryWriter.Write(securityDefinitionResponse.CurrencyValuePerIncrement);
                    binaryWriter.Write((byte)securityDefinitionResponse.IsFinalMessage);
                    binaryWriter.Write(securityDefinitionResponse.FloatToIntPriceMultiplier);
                    binaryWriter.Write(securityDefinitionResponse.IntToFloatPriceDivisor);
                    binaryWriter.Write(securityDefinitionResponse.UnderlyingSymbol.ToFixedBytes(UNDERLYING_SYMBOL_LENGTH));
                    binaryWriter.Write((byte)securityDefinitionResponse.UpdatesBidAskOnly);
                    binaryWriter.Write(securityDefinitionResponse.StrikePrice);
                    binaryWriter.Write((uint8_t)securityDefinitionResponse.PutOrCall);
                    binaryWriter.Write(securityDefinitionResponse.ShortInterest);
                    binaryWriter.Write((uint)securityDefinitionResponse.SecurityExpirationDate);
                    binaryWriter.Write(securityDefinitionResponse.BuyRolloverInterest);
                    binaryWriter.Write(securityDefinitionResponse.SellRolloverInterest);
                    binaryWriter.Write(securityDefinitionResponse.EarningsPerShare);
                    binaryWriter.Write(securityDefinitionResponse.SharesOutstanding);
                    binaryWriter.Write(securityDefinitionResponse.IntToFloatQuantityDivisor);
                    binaryWriter.Write((uint8_t)securityDefinitionResponse.HasMarketDepthData);
                    binaryWriter.Write(securityDefinitionResponse.DisplayPriceMultiplier);
                    binaryWriter.Write(securityDefinitionResponse.ExchangeSymbol.ToFixedBytes(SYMBOL_LENGTH));
                    return;
                case DTCMessageType.SymbolSearchRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}"); ; ;
                case DTCMessageType.SecurityDefinitionReject:
                    var securityDefinitionReject = message as SecurityDefinitionReject;
                    sizeExcludingHeader = 4 + TEXT_DESCRIPTION_LENGTH;
                    Utility.WriteHeader(binaryWriter, sizeExcludingHeader, messageType);
                    binaryWriter.Write(securityDefinitionReject.RequestID);
                    binaryWriter.Write(securityDefinitionReject.RejectText.ToFixedBytes(TEXT_DESCRIPTION_LENGTH));
                    return;
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
            int startIndex;
            switch (messageType)
            {
                case DTCMessageType.MessageTypeUnset:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.LogonRequest:
                    var logonRequest = result as LogonRequest;
                    startIndex = 0;
                    logonRequest.ProtocolVersion = BitConverter.ToInt32(bytes, startIndex);
                    startIndex += 4;
                    logonRequest.Username =  bytes.StringFromNullTerminatedBytes(startIndex);
                    startIndex += USERNAME_PASSWORD_LENGTH;
                    logonRequest.Password =bytes.StringFromNullTerminatedBytes(startIndex);
                    startIndex += USERNAME_PASSWORD_LENGTH;
                    logonRequest.GeneralTextData =bytes.StringFromNullTerminatedBytes(startIndex);
                    startIndex += GENERAL_IDENTIFIER_LENGTH;
                    logonRequest.Integer1 = BitConverter.ToInt32(bytes, startIndex);
                    startIndex += 4;
                    logonRequest.Integer2 = BitConverter.ToInt32(bytes, startIndex);
                    startIndex += 4;
                    logonRequest.HeartbeatIntervalInSeconds = BitConverter.ToInt32(bytes, startIndex);
                    startIndex += 4;
                    logonRequest.TradeMode = (TradeModeEnum)BitConverter.ToInt32(bytes, startIndex);
                    startIndex += 4;
                    logonRequest.TradeAccount =bytes.StringFromNullTerminatedBytes(startIndex);
                    startIndex += TRADE_ACCOUNT_LENGTH;
                    logonRequest.HardwareIdentifier =bytes.StringFromNullTerminatedBytes(startIndex);
                    startIndex += GENERAL_IDENTIFIER_LENGTH;
                    logonRequest.ClientName =bytes.StringFromNullTerminatedBytes(startIndex);
                    startIndex += 32;
                    return result;
                case DTCMessageType.LogonResponse:
                    var logonResponse = result as LogonResponse;
                    startIndex = 0;
                    logonResponse.ProtocolVersion = BitConverter.ToInt32(bytes, startIndex);
                    startIndex += 4;
                    logonResponse.Result = (LogonStatusEnum)BitConverter.ToInt32(bytes, startIndex);
                    startIndex += 4;
                    logonResponse.ResultText =bytes.StringFromNullTerminatedBytes(startIndex);
                    startIndex += TEXT_DESCRIPTION_LENGTH;
                    logonResponse.ReconnectAddress =bytes.StringFromNullTerminatedBytes(startIndex);
                    startIndex += 64;
                    logonResponse.Integer1 = BitConverter.ToInt32(bytes, startIndex);
                    startIndex += 4;
                    logonResponse.ServerName =bytes.StringFromNullTerminatedBytes(startIndex);
                    startIndex += 60;
                    logonResponse.MarketDepthUpdatesBestBidAndAsk = bytes[startIndex++];
                    logonResponse.TradingIsSupported = bytes[startIndex++];
                    logonResponse.OCOOrdersSupported = bytes[startIndex++];
                    logonResponse.OrderCancelReplaceSupported = bytes[startIndex++];
                    logonResponse.SymbolExchangeDelimiter = bytes.StringFromNullTerminatedBytes(startIndex);
                    startIndex += SYMBOL_EXCHANGE_DELIMITER_LENGTH;
                    logonResponse.SecurityDefinitionsSupported = bytes[startIndex++];
                    logonResponse.HistoricalPriceDataSupported = bytes[startIndex++];
                    logonResponse.ResubscribeWhenMarketDataFeedAvailable = bytes[startIndex++];
                    logonResponse.MarketDepthIsSupported = bytes[startIndex++];
                    logonResponse.OneHistoricalPriceDataRequestPerConnection = bytes[startIndex++];
                    logonResponse.BracketOrdersSupported = bytes[startIndex++];
                    logonResponse.UseIntegerPriceOrderMessages = bytes[startIndex++];
                    logonResponse.UsesMultiplePositionsPerSymbolAndTradeAccount = bytes[startIndex++];
                    logonResponse.MarketDataSupported = bytes[startIndex++];
                    return result;
                case DTCMessageType.Heartbeat:
                    var heartbeat = result as Heartbeat;
                    heartbeat.NumDroppedMessages = BitConverter.ToUInt32(bytes, 0);
                    heartbeat.CurrentDateTime = BitConverter.ToInt64(bytes, 4);
                    return result;
                case DTCMessageType.Logoff:
                    var logoff = result as Logoff;
                    startIndex = 0;
                    logoff.Reason = bytes.StringFromNullTerminatedBytes(startIndex);
                    startIndex += TEXT_DESCRIPTION_LENGTH;
                    logoff.DoNotReconnect = bytes[startIndex++];
                    return result;
                case DTCMessageType.EncodingRequest:
                    var encodingRequest = result as EncodingRequest;
                    encodingRequest.ProtocolVersion = BitConverter.ToInt32(bytes, 0);
                    encodingRequest.Encoding = (EncodingEnum)BitConverter.ToInt32(bytes, 4);
                    encodingRequest.ProtocolType = bytes.StringFromNullTerminatedBytes(8);
                    return result;
                case DTCMessageType.EncodingResponse:
                    var encodingResponse = result as EncodingResponse;
                    encodingResponse.ProtocolVersion = BitConverter.ToInt32(bytes, 0);
                    encodingResponse.Encoding = (EncodingEnum)BitConverter.ToInt32(bytes, 4);
                    encodingResponse.ProtocolType = bytes.StringFromNullTerminatedBytes(8);
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
                    var exchangeListRequest = result as ExchangeListRequest;
                    exchangeListRequest.RequestID = BitConverter.ToInt32(bytes, 0);
                    return result;
                case DTCMessageType.ExchangeListResponse:
                    var exchangeListResponse = result as ExchangeListResponse;
                    startIndex = 0;
                    exchangeListResponse.RequestID = BitConverter.ToInt32(bytes, startIndex);
                    startIndex += 4;
                    exchangeListResponse.Exchange = bytes.StringFromNullTerminatedBytes(startIndex);
                    startIndex += EXCHANGE_LENGTH;
                    exchangeListResponse.IsFinalMessage = bytes[startIndex++];
                    exchangeListResponse.Description = bytes.StringFromNullTerminatedBytes(startIndex);
                    startIndex += EXCHANGE_DESCRIPTION_LENGTH;
                    return result;
                case DTCMessageType.SymbolsForExchangeRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.UnderlyingSymbolsForExchangeRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.SymbolsForUnderlyingRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.SecurityDefinitionForSymbolRequest:
                    var securityDefinitionForSymbolRequest = result as SecurityDefinitionForSymbolRequest;
                    startIndex = 0;
                    securityDefinitionForSymbolRequest.RequestID = BitConverter.ToInt32(bytes, 0);
                    startIndex += 4;
                    securityDefinitionForSymbolRequest.Symbol = bytes.StringFromNullTerminatedBytes(startIndex);
                    startIndex += SYMBOL_LENGTH;
                    securityDefinitionForSymbolRequest.Exchange = bytes.StringFromNullTerminatedBytes(startIndex);
                    startIndex += EXCHANGE_LENGTH;
                    return result;
                case DTCMessageType.SecurityDefinitionResponse:
                    var securityDefinitionResponse = result as SecurityDefinitionResponse;
                    startIndex = 0;
                    securityDefinitionResponse.RequestID = BitConverter.ToInt32(bytes, startIndex);
                    startIndex += 4;
                    securityDefinitionResponse.Symbol = bytes.StringFromNullTerminatedBytes(startIndex);
                    startIndex += SYMBOL_LENGTH;
                    securityDefinitionResponse.Exchange = bytes.StringFromNullTerminatedBytes(startIndex);
                    startIndex += EXCHANGE_LENGTH;
                    securityDefinitionResponse.SecurityType = (SecurityTypeEnum)BitConverter.ToInt32(bytes, startIndex);
                    startIndex += 4;
                    securityDefinitionResponse.Description = bytes.StringFromNullTerminatedBytes(startIndex);
                    startIndex += SYMBOL_DESCRIPTION_LENGTH;
                    securityDefinitionResponse.MinPriceIncrement = BitConverter.ToSingle(bytes, startIndex);
                    startIndex += 4;
                    securityDefinitionResponse.PriceDisplayFormat = (PriceDisplayFormatEnum)BitConverter.ToInt32(bytes, startIndex);
                    startIndex += 4;
                    securityDefinitionResponse.CurrencyValuePerIncrement = BitConverter.ToSingle(bytes, startIndex);
                    startIndex += 4;
                    securityDefinitionResponse.IsFinalMessage = bytes[startIndex++];
                    securityDefinitionResponse.FloatToIntPriceMultiplier = BitConverter.ToSingle(bytes, startIndex);
                    startIndex += 4;
                    securityDefinitionResponse.IntToFloatPriceDivisor = BitConverter.ToSingle(bytes, startIndex);
                    startIndex += 4;
                    securityDefinitionResponse.UnderlyingSymbol = bytes.StringFromNullTerminatedBytes(startIndex);
                    startIndex += UNDERLYING_SYMBOL_LENGTH;
                    securityDefinitionResponse.UpdatesBidAskOnly = bytes[startIndex++];
                    securityDefinitionResponse.StrikePrice = BitConverter.ToSingle(bytes, startIndex);
                    startIndex += 4;
                    securityDefinitionResponse.PutOrCall = (PutCallEnum)BitConverter.ToInt32(bytes, startIndex);
                    startIndex += 4;
                    securityDefinitionResponse.ShortInterest = BitConverter.ToUInt32(bytes, startIndex);
                    startIndex += 4;
                    securityDefinitionResponse.SecurityExpirationDate = BitConverter.ToInt32(bytes, startIndex);
                    startIndex += 4;
                    securityDefinitionResponse.BuyRolloverInterest = BitConverter.ToSingle(bytes, startIndex);
                    startIndex += 4;
                    securityDefinitionResponse.SellRolloverInterest = BitConverter.ToSingle(bytes, startIndex);
                    startIndex += 4;
                    securityDefinitionResponse.EarningsPerShare = BitConverter.ToSingle(bytes, startIndex);
                    startIndex += 4;
                    securityDefinitionResponse.SharesOutstanding = BitConverter.ToUInt32(bytes, startIndex);
                    startIndex += 4;
                    securityDefinitionResponse.IntToFloatQuantityDivisor = BitConverter.ToSingle(bytes, startIndex);
                    startIndex += 4;
                    securityDefinitionResponse.HasMarketDepthData = bytes[startIndex++];
                    securityDefinitionResponse.DisplayPriceMultiplier = BitConverter.ToSingle(bytes, startIndex);
                    startIndex += 4;
                    securityDefinitionResponse.ExchangeSymbol = bytes.StringFromNullTerminatedBytes(startIndex);
                    startIndex += SYMBOL_LENGTH;
                    return result;
                case DTCMessageType.SymbolSearchRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}"); ;;
                case DTCMessageType.SecurityDefinitionReject:
                    var securityDefinitionReject = result as SecurityDefinitionReject;
                    startIndex = 0;
                    securityDefinitionReject.RequestID = BitConverter.ToInt32(bytes, startIndex);
                    startIndex += 4;
                    securityDefinitionReject.RejectText = bytes.StringFromNullTerminatedBytes(startIndex);
                    startIndex += TEXT_DESCRIPTION_LENGTH;
                    return result;
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
