using System;
using System.IO;
using DTCCommon.Extensions;
using DTCPB;
using Google.Protobuf;
using uint8_t = System.Byte;
using int32_t = System.Int32;

namespace DTCCommon.Codecs
{
    public class CodecBinary : ICodecDTC
    {
        // Text string lengths. Copied from DTCProtocol.h
        private const int USERNAME_PASSWORD_LENGTH = 32;
        private const int SYMBOL_EXCHANGE_DELIMITER_LENGTH = 4;
        private const int SYMBOL_LENGTH = 64;
        private const int EXCHANGE_LENGTH = 16;
        private const int UNDERLYING_SYMBOL_LENGTH = 32;
        private const int SYMBOL_DESCRIPTION_LENGTH = 64; //Previously 48
        private const int EXCHANGE_DESCRIPTION_LENGTH = 48;
        private const int ORDER_ID_LENGTH = 32;
        private const int TRADE_ACCOUNT_LENGTH = 32;
        private const int TEXT_DESCRIPTION_LENGTH = 96;
        private const int TEXT_MESSAGE_LENGTH = 256;
        private const int ORDER_FREE_FORM_TEXT_LENGTH = 48;
        private const int CLIENT_SERVER_NAME_LENGTH = 48;
        private const int GENERAL_IDENTIFIER_LENGTH = 64;

        /// <summary>
        ///     Write the message using binaryWriter
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="message"></param>
        /// <param name="binaryWriter">It's possible for this to become null because of stream failure and a Dispose()</param>
        public void Write<T>(DTCMessageType messageType, T message, BinaryWriter binaryWriter) where T : IMessage
        {
            if (binaryWriter == null) return;
            int sizeExcludingHeader;
            switch (messageType)
            {
                case DTCMessageType.MessageTypeUnset:
                    throw new ArgumentException(messageType.ToString());
                case DTCMessageType.LogonRequest:
                    var logonRequest = message as LogonRequest;
                    sizeExcludingHeader =
                        4
                        + USERNAME_PASSWORD_LENGTH
                        + USERNAME_PASSWORD_LENGTH
                        + GENERAL_IDENTIFIER_LENGTH
                        + 4
                        + 4
                        + 4
                        + 4
                        + TRADE_ACCOUNT_LENGTH
                        + GENERAL_IDENTIFIER_LENGTH
                        + 32;
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
                    sizeExcludingHeader = 4 + 4 + TEXT_DESCRIPTION_LENGTH + 64 + 4 + 60 + 4 * 1 + SYMBOL_EXCHANGE_DELIMITER_LENGTH + 8 * 1 + 4;
                    Utility.WriteHeader(binaryWriter, sizeExcludingHeader, messageType);
                    binaryWriter.Write(logonResponse.ProtocolVersion);
                    binaryWriter.Write((int)logonResponse.Result);
                    binaryWriter.Write(logonResponse.ResultText.ToFixedBytes(TEXT_DESCRIPTION_LENGTH));
                    binaryWriter.Write(logonResponse.ReconnectAddress.ToFixedBytes(64));
                    binaryWriter.Write(logonResponse.Integer1);
                    binaryWriter.Write(logonResponse.ServerName.ToFixedBytes(60));
                    binaryWriter.Write((byte)logonResponse.MarketDepthUpdatesBestBidAndAsk);
                    binaryWriter.Write((byte)logonResponse.TradingIsSupported);
                    binaryWriter.Write((byte)logonResponse.OCOOrdersSupported);
                    binaryWriter.Write((byte)logonResponse.OrderCancelReplaceSupported);
                    binaryWriter.Write(logonResponse.SymbolExchangeDelimiter.ToFixedBytes(SYMBOL_EXCHANGE_DELIMITER_LENGTH));
                    binaryWriter.Write((byte)logonResponse.SecurityDefinitionsSupported);
                    binaryWriter.Write((byte)logonResponse.HistoricalPriceDataSupported);
                    binaryWriter.Write((byte)logonResponse.ResubscribeWhenMarketDataFeedAvailable);
                    binaryWriter.Write((byte)logonResponse.MarketDepthIsSupported);
                    binaryWriter.Write((byte)logonResponse.OneHistoricalPriceDataRequestPerConnection);
                    binaryWriter.Write((byte)logonResponse.BracketOrdersSupported);
                    binaryWriter.Write((byte)logonResponse.UseIntegerPriceOrderMessages);
                    binaryWriter.Write((byte)logonResponse.UsesMultiplePositionsPerSymbolAndTradeAccount);
                    binaryWriter.Write(logonResponse.MarketDataSupported);
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
                    binaryWriter.Write((byte)logoff.DoNotReconnect);
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
                    var marketDataRequest = message as MarketDataRequest;
                    sizeExcludingHeader = 4 + 2 + SYMBOL_LENGTH + EXCHANGE_LENGTH;
                    Utility.WriteHeader(binaryWriter, sizeExcludingHeader, messageType);
                    binaryWriter.Write((int)marketDataRequest.RequestAction);
                    binaryWriter.Write((ushort)marketDataRequest.SymbolID);
                    binaryWriter.Write(marketDataRequest.Symbol.ToFixedBytes(SYMBOL_LENGTH));
                    binaryWriter.Write(marketDataRequest.Exchange.ToFixedBytes(EXCHANGE_LENGTH));
                    return;
                case DTCMessageType.MarketDataReject:
                    var marketDataReject = message as MarketDataReject;
                    sizeExcludingHeader = 2 + TEXT_DESCRIPTION_LENGTH;
                    Utility.WriteHeader(binaryWriter, sizeExcludingHeader, messageType);
                    binaryWriter.Write((ushort)marketDataReject.SymbolID);
                    binaryWriter.Write(marketDataReject.RejectText.ToFixedBytes(TEXT_DESCRIPTION_LENGTH));
                    return;
                case DTCMessageType.MarketDataFeedStatus:
                    var marketDataFeedStatus = message as MarketDataFeedStatus;
                    sizeExcludingHeader = 4;
                    Utility.WriteHeader(binaryWriter, sizeExcludingHeader, messageType);
                    binaryWriter.Write((int)marketDataFeedStatus.Status);
                    return;
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
                    binaryWriter.Write((byte)exchangeListResponse.IsFinalMessage);
                    binaryWriter.Write(exchangeListResponse.Description.ToFixedBytes(EXCHANGE_DESCRIPTION_LENGTH));
                    return;
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
                    sizeExcludingHeader =
                        4
                        + SYMBOL_LENGTH
                        + EXCHANGE_LENGTH
                        + 4
                        + SYMBOL_DESCRIPTION_LENGTH
                        + 3 * 4
                        + 4
                        + 2 * 4
                        + UNDERLYING_SYMBOL_LENGTH
                        + 4
                        + 4
                        + 4
                        + 7 * 4
                        + 4
                        + 4
                        + SYMBOL_LENGTH;
                    Utility.WriteHeader(binaryWriter, sizeExcludingHeader, messageType);
                    binaryWriter.Write(securityDefinitionResponse.RequestID);
                    binaryWriter.Write(securityDefinitionResponse.Symbol.ToFixedBytes(SYMBOL_LENGTH));
                    binaryWriter.Write(securityDefinitionResponse.Exchange.ToFixedBytes(EXCHANGE_LENGTH));
                    binaryWriter.Write((int)securityDefinitionResponse.SecurityType);
                    binaryWriter.Write(securityDefinitionResponse.Description.ToFixedBytes(SYMBOL_DESCRIPTION_LENGTH));
                    binaryWriter.Write(securityDefinitionResponse.MinPriceIncrement);
                    binaryWriter.Write((int)securityDefinitionResponse.PriceDisplayFormat);
                    binaryWriter.Write(securityDefinitionResponse.CurrencyValuePerIncrement);
                    binaryWriter.Write(securityDefinitionResponse.IsFinalMessage);
                    binaryWriter.Write(securityDefinitionResponse.FloatToIntPriceMultiplier);
                    binaryWriter.Write(securityDefinitionResponse.IntToFloatPriceDivisor);
                    binaryWriter.Write(securityDefinitionResponse.UnderlyingSymbol.ToFixedBytes(UNDERLYING_SYMBOL_LENGTH));
                    binaryWriter.Write(securityDefinitionResponse.UpdatesBidAskOnly);
                    binaryWriter.Write(securityDefinitionResponse.StrikePrice);
                    binaryWriter.Write((int)securityDefinitionResponse.PutOrCall);
                    binaryWriter.Write(securityDefinitionResponse.ShortInterest);
                    binaryWriter.Write((uint)securityDefinitionResponse.SecurityExpirationDate);
                    binaryWriter.Write(securityDefinitionResponse.BuyRolloverInterest);
                    binaryWriter.Write(securityDefinitionResponse.SellRolloverInterest);
                    binaryWriter.Write(securityDefinitionResponse.EarningsPerShare);
                    binaryWriter.Write(securityDefinitionResponse.SharesOutstanding);
                    binaryWriter.Write(securityDefinitionResponse.IntToFloatQuantityDivisor);
                    binaryWriter.Write(securityDefinitionResponse.HasMarketDepthData);
                    binaryWriter.Write(securityDefinitionResponse.DisplayPriceMultiplier);
                    binaryWriter.Write(securityDefinitionResponse.ExchangeSymbol.ToFixedBytes(SYMBOL_LENGTH));
                    return;
                case DTCMessageType.SecurityDefinitionReject:
                    var securityDefinitionReject = message as SecurityDefinitionReject;
                    sizeExcludingHeader = 4 + TEXT_DESCRIPTION_LENGTH;
                    Utility.WriteHeader(binaryWriter, sizeExcludingHeader, messageType);
                    binaryWriter.Write(securityDefinitionReject.RequestID);
                    binaryWriter.Write(securityDefinitionReject.RejectText.ToFixedBytes(TEXT_DESCRIPTION_LENGTH));
                    return;
                case DTCMessageType.HistoricalPriceDataRequest:
                    var historicalPriceDataRequest = message as HistoricalPriceDataRequest;
                    sizeExcludingHeader = 4 + SYMBOL_LENGTH + EXCHANGE_LENGTH + 4 + 4 + 8 + 8 + 4 + 3 * 1;
                    Utility.WriteHeader(binaryWriter, sizeExcludingHeader, messageType);
                    binaryWriter.Write(historicalPriceDataRequest.RequestID);
                    binaryWriter.Write(historicalPriceDataRequest.Symbol.ToFixedBytes(SYMBOL_LENGTH));
                    binaryWriter.Write(historicalPriceDataRequest.Exchange.ToFixedBytes(EXCHANGE_LENGTH));
                    binaryWriter.Write((int)historicalPriceDataRequest.RecordInterval);
                    binaryWriter.Write(0); // 4 bytes for alignment on 8-byte boundary
                    binaryWriter.Write(historicalPriceDataRequest.StartDateTime);
                    binaryWriter.Write(historicalPriceDataRequest.EndDateTime);
                    binaryWriter.Write(historicalPriceDataRequest.MaxDaysToReturn);
                    binaryWriter.Write((byte)historicalPriceDataRequest.UseZLibCompression);
                    binaryWriter.Write((byte)historicalPriceDataRequest.RequestDividendAdjustedStockData);
                    binaryWriter.Write((byte)historicalPriceDataRequest.Integer1);
                    return;
                case DTCMessageType.HistoricalPriceDataResponseHeader:
                    var historicalPriceDataResponseHeader = message as HistoricalPriceDataResponseHeader;
                    sizeExcludingHeader = 4 + 4 + 2 + 2 + 4;
                    Utility.WriteHeader(binaryWriter, sizeExcludingHeader, messageType);
                    binaryWriter.Write(historicalPriceDataResponseHeader.RequestID);
                    binaryWriter.Write((int)historicalPriceDataResponseHeader.RecordInterval);
                    binaryWriter.Write((byte)historicalPriceDataResponseHeader.UseZLibCompression);
                    binaryWriter.Write((byte)historicalPriceDataResponseHeader.NoRecordsToReturn);
                    binaryWriter.Write((short)0); // align for packing
                    binaryWriter.Write(historicalPriceDataResponseHeader.IntToFloatPriceDivisor);
                    return;
                case DTCMessageType.HistoricalPriceDataReject:
                    var historicalPriceDataReject = message as HistoricalPriceDataReject;
                    sizeExcludingHeader = 4 + TEXT_DESCRIPTION_LENGTH + 2 + 2;
                    Utility.WriteHeader(binaryWriter, sizeExcludingHeader, messageType);
                    binaryWriter.Write(historicalPriceDataReject.RequestID);
                    binaryWriter.Write(historicalPriceDataReject.RejectText.ToFixedBytes(TEXT_DESCRIPTION_LENGTH));
                    binaryWriter.Write((short)historicalPriceDataReject.RejectReasonCode);
                    binaryWriter.Write((ushort)historicalPriceDataReject.RetryTimeInSeconds);
                    return;
                case DTCMessageType.HistoricalPriceDataRecordResponse:
                    var historicalPriceDataRecordResponse = message as HistoricalPriceDataRecordResponse;
                    sizeExcludingHeader = 4 + 9 * 8 + 1;
                    Utility.WriteHeader(binaryWriter, sizeExcludingHeader, messageType);
                    binaryWriter.Write(historicalPriceDataRecordResponse.RequestID);
                    binaryWriter.Write(historicalPriceDataRecordResponse.StartDateTime);
                    binaryWriter.Write(historicalPriceDataRecordResponse.OpenPrice);
                    binaryWriter.Write(historicalPriceDataRecordResponse.HighPrice);
                    binaryWriter.Write(historicalPriceDataRecordResponse.LowPrice);
                    binaryWriter.Write(historicalPriceDataRecordResponse.LastPrice);
                    binaryWriter.Write(historicalPriceDataRecordResponse.Volume);
                    binaryWriter.Write(historicalPriceDataRecordResponse.NumTrades);
                    binaryWriter.Write(0); // for 8-byte packing boundary
                    binaryWriter.Write(historicalPriceDataRecordResponse.BidVolume);
                    binaryWriter.Write(historicalPriceDataRecordResponse.AskVolume);
                    binaryWriter.Write((byte)historicalPriceDataRecordResponse.IsFinalRecord);
                    return;
                case DTCMessageType.HistoricalPriceDataTickRecordResponse:
                    // Probably no longer used after version 1150 per https://www.sierrachart.com/index.php?page=doc/IntradayDataFileFormat.html
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Write)}: {messageType}");
                default:
                    throw new ArgumentOutOfRangeException(messageType.ToString(), messageType, null);
            }
        }

        /// <summary>
        ///     Write the message using binaryWriter
        /// </summary>
        /// <param name="message"></param>
        /// <param name="binaryWriter"></param>
        public void Write<T>(T message, BinaryWriter binaryWriter) where T : IMessage
        {
            var messageType = MessageTypes.MessageTypeByMessage[typeof(T)];
            Write(messageType, message, binaryWriter);
        }

        public T Load<T>(DTCMessageType messageType, byte[] bytes, int index = 0) where T : IMessage<T>, new()
        {
#if DEBUG
            var startIndex = index;
#endif
            var result = new T();
            switch (messageType)
            {
                case DTCMessageType.MessageTypeUnset:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}");
                case DTCMessageType.LogonRequest:
                    var logonRequest = result as LogonRequest;
                    logonRequest.ProtocolVersion = BitConverter.ToInt32(bytes, index);
                    index += 4;
                    logonRequest.Username = bytes.StringFromNullTerminatedBytes(index);
                    index += USERNAME_PASSWORD_LENGTH;
                    logonRequest.Password = bytes.StringFromNullTerminatedBytes(index);
                    index += USERNAME_PASSWORD_LENGTH;
                    logonRequest.GeneralTextData = bytes.StringFromNullTerminatedBytes(index);
                    index += GENERAL_IDENTIFIER_LENGTH;
                    logonRequest.Integer1 = BitConverter.ToInt32(bytes, index);
                    index += 4;
                    logonRequest.Integer2 = BitConverter.ToInt32(bytes, index);
                    index += 4;
                    logonRequest.HeartbeatIntervalInSeconds = BitConverter.ToInt32(bytes, index);
                    index += 4;
                    logonRequest.TradeMode = (TradeModeEnum)BitConverter.ToInt32(bytes, index);
                    index += 4;
                    logonRequest.TradeAccount = bytes.StringFromNullTerminatedBytes(index);
                    index += TRADE_ACCOUNT_LENGTH;
                    logonRequest.HardwareIdentifier = bytes.StringFromNullTerminatedBytes(index);
                    index += GENERAL_IDENTIFIER_LENGTH;
                    logonRequest.ClientName = bytes.StringFromNullTerminatedBytes(index);
                    index += 32;
                    return result;
                case DTCMessageType.LogonResponse:
                    var logonResponse = result as LogonResponse;
                    logonResponse.ProtocolVersion = BitConverter.ToInt32(bytes, index);
                    index += 4;
                    logonResponse.Result = (LogonStatusEnum)BitConverter.ToInt32(bytes, index);
                    index += 4;
                    logonResponse.ResultText = bytes.StringFromNullTerminatedBytes(index);
                    index += TEXT_DESCRIPTION_LENGTH;
                    logonResponse.ReconnectAddress = bytes.StringFromNullTerminatedBytes(index);
                    index += 64;
                    logonResponse.Integer1 = BitConverter.ToInt32(bytes, index);
                    index += 4;
                    logonResponse.ServerName = bytes.StringFromNullTerminatedBytes(index);
                    index += 60;
                    logonResponse.MarketDepthUpdatesBestBidAndAsk = bytes[index++];
                    logonResponse.TradingIsSupported = bytes[index++];
                    logonResponse.OCOOrdersSupported = bytes[index++];
                    logonResponse.OrderCancelReplaceSupported = bytes[index++];
                    logonResponse.SymbolExchangeDelimiter = bytes.StringFromNullTerminatedBytes(index);
                    index += SYMBOL_EXCHANGE_DELIMITER_LENGTH;
                    logonResponse.SecurityDefinitionsSupported = bytes[index++];
                    logonResponse.HistoricalPriceDataSupported = bytes[index++];
                    logonResponse.ResubscribeWhenMarketDataFeedAvailable = bytes[index++];
                    logonResponse.MarketDepthIsSupported = bytes[index++];
                    logonResponse.OneHistoricalPriceDataRequestPerConnection = bytes[index++];
                    logonResponse.BracketOrdersSupported = bytes[index++];
                    logonResponse.UseIntegerPriceOrderMessages = bytes[index++];
                    logonResponse.UsesMultiplePositionsPerSymbolAndTradeAccount = bytes[index++];
                    logonResponse.MarketDataSupported = BitConverter.ToUInt32(bytes, index);
                    index += 4;
                    return result;
                case DTCMessageType.Heartbeat:
                    var heartbeat = result as Heartbeat;
                    heartbeat.NumDroppedMessages = BitConverter.ToUInt32(bytes, index);
                    index += 4;
                    heartbeat.CurrentDateTime = BitConverter.ToInt64(bytes, index);
                    return result;
                case DTCMessageType.Logoff:
                    var logoff = result as Logoff;
                    logoff.Reason = bytes.StringFromNullTerminatedBytes(index);
                    index += TEXT_DESCRIPTION_LENGTH;
                    logoff.DoNotReconnect = bytes[index++];
                    return result;
                case DTCMessageType.EncodingRequest:
                    var encodingRequest = result as EncodingRequest;
                    encodingRequest.ProtocolVersion = BitConverter.ToInt32(bytes, index);
                    index += 4;
                    encodingRequest.Encoding = (EncodingEnum)BitConverter.ToInt32(bytes, index);
                    index += 4;
                    encodingRequest.ProtocolType = bytes.StringFromNullTerminatedBytes(index);
                    index += 4;
                    return result;
                case DTCMessageType.EncodingResponse:
                    var encodingResponse = result as EncodingResponse;
                    encodingResponse.ProtocolVersion = BitConverter.ToInt32(bytes, index);
                    index += 4;
                    encodingResponse.Encoding = (EncodingEnum)BitConverter.ToInt32(bytes, index);
                    index += 4;
                    encodingResponse.ProtocolType = bytes.StringFromNullTerminatedBytes(index);
                    return result;
                case DTCMessageType.MarketDataRequest:
                    var marketDataRequest = result as MarketDataRequest;
                    marketDataRequest.RequestAction = (RequestActionEnum)BitConverter.ToInt32(bytes, index);
                    index += 4;
                    marketDataRequest.SymbolID = BitConverter.ToUInt16(bytes, index);
                    index += 2;
                    marketDataRequest.Symbol = bytes.StringFromNullTerminatedBytes(index);
                    index += SYMBOL_LENGTH;
                    marketDataRequest.Exchange = bytes.StringFromNullTerminatedBytes(index);
                    index += EXCHANGE_LENGTH;
                    return result;
                case DTCMessageType.MarketDataReject:
                    var marketDataReject = result as MarketDataReject;
                    marketDataReject.SymbolID = BitConverter.ToUInt16(bytes, index);
                    index += 2;
                    marketDataReject.RejectText = bytes.StringFromNullTerminatedBytes(index);
                    index += TEXT_DESCRIPTION_LENGTH;
                    return result;
                case DTCMessageType.MarketDataFeedStatus:
                    var marketDataFeedStatus = result as MarketDataFeedStatus;
                    marketDataFeedStatus.Status = (MarketDataFeedStatusEnum)BitConverter.ToInt32(bytes, index);
                    index += 4;
                    return result;
                case DTCMessageType.MarketDataFeedSymbolStatus:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}");
                case DTCMessageType.SubmitNewSingleOrder:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}");
                case DTCMessageType.SubmitNewSingleOrderInt:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}");
                case DTCMessageType.SubmitNewOcoOrder:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}");
                case DTCMessageType.SubmitNewOcoOrderInt:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}");
                case DTCMessageType.CancelOrder:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}");
                case DTCMessageType.CancelReplaceOrder:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}");
                case DTCMessageType.CancelReplaceOrderInt:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}");
                case DTCMessageType.OpenOrdersRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}");
                case DTCMessageType.OpenOrdersReject:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}");
                case DTCMessageType.OrderUpdate:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}");
                case DTCMessageType.HistoricalOrderFillsRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}");
                case DTCMessageType.HistoricalOrderFillResponse:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}");
                case DTCMessageType.CurrentPositionsRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}");
                case DTCMessageType.CurrentPositionsReject:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}");
                case DTCMessageType.PositionUpdate:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}");
                case DTCMessageType.TradeAccountsRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}");
                case DTCMessageType.TradeAccountResponse:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}");
                case DTCMessageType.ExchangeListRequest:
                    var exchangeListRequest = result as ExchangeListRequest;
                    exchangeListRequest.RequestID = BitConverter.ToInt32(bytes, index);
                    index += 4;
                    return result;
                case DTCMessageType.ExchangeListResponse:
                    var exchangeListResponse = result as ExchangeListResponse;
                    exchangeListResponse.RequestID = BitConverter.ToInt32(bytes, index);
                    index += 4;
                    exchangeListResponse.Exchange = bytes.StringFromNullTerminatedBytes(index);
                    index += EXCHANGE_LENGTH;
                    exchangeListResponse.IsFinalMessage = bytes[index++];
                    exchangeListResponse.Description = bytes.StringFromNullTerminatedBytes(index);
                    index += EXCHANGE_DESCRIPTION_LENGTH;
                    return result;
                case DTCMessageType.SymbolsForExchangeRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}");
                case DTCMessageType.UnderlyingSymbolsForExchangeRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}");
                case DTCMessageType.SymbolsForUnderlyingRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}");
                case DTCMessageType.SecurityDefinitionForSymbolRequest:
                    var securityDefinitionForSymbolRequest = result as SecurityDefinitionForSymbolRequest;
                    securityDefinitionForSymbolRequest.RequestID = BitConverter.ToInt32(bytes, index);
                    index += 4;
                    securityDefinitionForSymbolRequest.Symbol = bytes.StringFromNullTerminatedBytes(index);
                    index += SYMBOL_LENGTH;
                    securityDefinitionForSymbolRequest.Exchange = bytes.StringFromNullTerminatedBytes(index);
                    index += EXCHANGE_LENGTH;
                    return result;
                case DTCMessageType.SecurityDefinitionResponse:
                    var securityDefinitionResponse = result as SecurityDefinitionResponse;
                    securityDefinitionResponse.RequestID = BitConverter.ToInt32(bytes, index);
                    index += 4;
                    securityDefinitionResponse.Symbol = bytes.StringFromNullTerminatedBytes(index);
                    index += SYMBOL_LENGTH;
                    securityDefinitionResponse.Exchange = bytes.StringFromNullTerminatedBytes(index);
                    index += EXCHANGE_LENGTH;
                    securityDefinitionResponse.SecurityType = (SecurityTypeEnum)BitConverter.ToInt32(bytes, index);
                    index += 4;
                    securityDefinitionResponse.Description = bytes.StringFromNullTerminatedBytes(index);
                    index += SYMBOL_DESCRIPTION_LENGTH;
                    securityDefinitionResponse.MinPriceIncrement = BitConverter.ToSingle(bytes, index);
                    index += 4;
                    securityDefinitionResponse.PriceDisplayFormat = (PriceDisplayFormatEnum)BitConverter.ToInt32(bytes, index);
                    index += 4;
                    securityDefinitionResponse.CurrencyValuePerIncrement = BitConverter.ToSingle(bytes, index);
                    index += 4;
                    securityDefinitionResponse.IsFinalMessage = BitConverter.ToUInt32(bytes, index); // aligned on 8-byte boundaries
                    index += 4;
                    securityDefinitionResponse.FloatToIntPriceMultiplier = BitConverter.ToSingle(bytes, index);
                    index += 4;
                    securityDefinitionResponse.IntToFloatPriceDivisor = BitConverter.ToSingle(bytes, index);
                    index += 4;
                    securityDefinitionResponse.UnderlyingSymbol = bytes.StringFromNullTerminatedBytes(index);
                    index += UNDERLYING_SYMBOL_LENGTH;
                    securityDefinitionResponse.UpdatesBidAskOnly = BitConverter.ToUInt32(bytes, index); // aligned on 8-byte boundaries
                    index += 4;
                    securityDefinitionResponse.StrikePrice = BitConverter.ToSingle(bytes, index);
                    index += 4;
                    securityDefinitionResponse.PutOrCall = (PutCallEnum)BitConverter.ToUInt32(bytes, index); // aligned on 8-byte boundaries
                    index += 4;
                    securityDefinitionResponse.ShortInterest = BitConverter.ToUInt32(bytes, index);
                    index += 4;
                    securityDefinitionResponse.SecurityExpirationDate = BitConverter.ToInt32(bytes, index);
                    index += 4;
                    securityDefinitionResponse.BuyRolloverInterest = BitConverter.ToSingle(bytes, index);
                    index += 4;
                    securityDefinitionResponse.SellRolloverInterest = BitConverter.ToSingle(bytes, index);
                    index += 4;
                    securityDefinitionResponse.EarningsPerShare = BitConverter.ToSingle(bytes, index);
                    index += 4;
                    securityDefinitionResponse.SharesOutstanding = BitConverter.ToUInt32(bytes, index);
                    index += 4;
                    securityDefinitionResponse.IntToFloatQuantityDivisor = BitConverter.ToSingle(bytes, index);
                    index += 4;
                    securityDefinitionResponse.HasMarketDepthData = BitConverter.ToUInt32(bytes, index); // aligned on 8-byte boundaries
                    index += 4;
                    securityDefinitionResponse.DisplayPriceMultiplier = BitConverter.ToSingle(bytes, index);
                    index += 4;
                    securityDefinitionResponse.ExchangeSymbol = bytes.StringFromNullTerminatedBytes(index);
                    index += SYMBOL_LENGTH;
                    return result;
                case DTCMessageType.SymbolSearchRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}");
                case DTCMessageType.SecurityDefinitionReject:
                    var securityDefinitionReject = result as SecurityDefinitionReject;
                    securityDefinitionReject.RequestID = BitConverter.ToInt32(bytes, index);
                    index += 4;
                    securityDefinitionReject.RejectText = bytes.StringFromNullTerminatedBytes(index);
                    index += TEXT_DESCRIPTION_LENGTH;
                    return result;
                case DTCMessageType.AccountBalanceRequest:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}");
                case DTCMessageType.AccountBalanceReject:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}");
                case DTCMessageType.AccountBalanceUpdate:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}");
                case DTCMessageType.UserMessage:
                    var userMessage = result as UserMessage;
                    index = 0;
                    userMessage.UserMessage_ = bytes.StringFromNullTerminatedBytes(index);
                    index += TEXT_MESSAGE_LENGTH;
                    return result;
                case DTCMessageType.GeneralLogMessage:
                    var generalLogMessage = result as GeneralLogMessage;
                    generalLogMessage.MessageText = bytes.StringFromNullTerminatedBytes(index);
                    index += 128;
                    return result;
                case DTCMessageType.HistoricalPriceDataRequest:
                    var historicalPriceDataRequest = result as HistoricalPriceDataRequest;
                    historicalPriceDataRequest.RequestID = BitConverter.ToInt32(bytes, index);
                    index += 4;
                    historicalPriceDataRequest.Symbol = bytes.StringFromNullTerminatedBytes(index);
                    index += SYMBOL_LENGTH;
                    historicalPriceDataRequest.Exchange = bytes.StringFromNullTerminatedBytes(index);
                    index += EXCHANGE_LENGTH;
                    historicalPriceDataRequest.RecordInterval = (HistoricalDataIntervalEnum)BitConverter.ToInt32(bytes, index);
                    index += 4;
                    index += 4; // 4 bytes for alignment on 8-byte boundary
                    historicalPriceDataRequest.StartDateTime = BitConverter.ToInt64(bytes, index);
                    index += 8;
                    historicalPriceDataRequest.EndDateTime = BitConverter.ToInt64(bytes, index);
                    index += 8;
                    historicalPriceDataRequest.MaxDaysToReturn = BitConverter.ToUInt32(bytes, index);
                    index += 4;
                    historicalPriceDataRequest.UseZLibCompression = bytes[index++];
                    historicalPriceDataRequest.RequestDividendAdjustedStockData = bytes[index++];
                    historicalPriceDataRequest.Integer1 = bytes[index++];
                    return result;
                case DTCMessageType.HistoricalPriceDataResponseHeader:
                    var historicalPriceDataResponseHeader = result as HistoricalPriceDataResponseHeader;
                    historicalPriceDataResponseHeader.RequestID = BitConverter.ToInt32(bytes, index);
                    index += 4;
                    historicalPriceDataResponseHeader.RecordInterval = (HistoricalDataIntervalEnum)BitConverter.ToInt32(bytes, index);
                    index += 4;
                    historicalPriceDataResponseHeader.UseZLibCompression = bytes[index++];
                    historicalPriceDataResponseHeader.NoRecordsToReturn = bytes[index++];
                    index += 2; // align for packing
                    historicalPriceDataResponseHeader.IntToFloatPriceDivisor = BitConverter.ToSingle(bytes, index);
                    index += 4;
                    return result;
                case DTCMessageType.HistoricalPriceDataReject:
                    var historicalPriceDataReject = result as HistoricalPriceDataReject;
                    historicalPriceDataReject.RequestID = BitConverter.ToInt32(bytes, index);
                    index += 4;
                    historicalPriceDataReject.RejectText = bytes.StringFromNullTerminatedBytes(index);
                    index += TEXT_DESCRIPTION_LENGTH;
                    historicalPriceDataReject.RejectReasonCode = (HistoricalPriceDataRejectReasonCodeEnum)BitConverter.ToInt16(bytes, index);
                    index += 2;
                    historicalPriceDataReject.RetryTimeInSeconds = BitConverter.ToUInt16(bytes, index);
                    index += 2;
                    return result;
                case DTCMessageType.HistoricalPriceDataRecordResponse:
                    var historicalPriceDataRecordResponse = result as HistoricalPriceDataRecordResponse;
                    historicalPriceDataRecordResponse.RequestID = BitConverter.ToInt32(bytes, index);
                    index += 4;
                    historicalPriceDataRecordResponse.StartDateTime = BitConverter.ToInt64(bytes, index);
                    var debug = $"{historicalPriceDataRecordResponse.StartDateTime.DtcDateTimeToUtc().ToLocalTime():yyyyMMdd.HHmmss.fff} (local).";
                    index += 8;
                    historicalPriceDataRecordResponse.OpenPrice = BitConverter.ToDouble(bytes, index);
                    index += 8;
                    historicalPriceDataRecordResponse.HighPrice = BitConverter.ToDouble(bytes, index);
                    index += 8;
                    historicalPriceDataRecordResponse.LowPrice = BitConverter.ToDouble(bytes, index);
                    index += 8;
                    historicalPriceDataRecordResponse.LastPrice = BitConverter.ToDouble(bytes, index);
                    index += 8;
                    historicalPriceDataRecordResponse.Volume = BitConverter.ToDouble(bytes, index);
                    index += 8;
                    historicalPriceDataRecordResponse.NumTrades = BitConverter.ToUInt32(bytes, index); // union, also could be OpenInterest
                    index += 4;
                    index += 4; // for 8-byte packing boundary
                    historicalPriceDataRecordResponse.BidVolume = BitConverter.ToDouble(bytes, index);
                    index += 8;
                    historicalPriceDataRecordResponse.AskVolume = BitConverter.ToDouble(bytes, index);
                    index += 8;
                    historicalPriceDataRecordResponse.IsFinalRecord = bytes[index++];
                    return result;
                case DTCMessageType.HistoricalPriceDataTickRecordResponse:
                    // Probably no longer used after version 1150 per https://www.sierrachart.com/index.php?page=doc/IntradayDataFileFormat.html
                    var historicalPriceDataTickRecordResponse = result as HistoricalPriceDataTickRecordResponse;
                    historicalPriceDataTickRecordResponse.RequestID = BitConverter.ToInt32(bytes, index);
                    index += 4;
                    historicalPriceDataTickRecordResponse.DateTime = BitConverter.ToInt64(bytes, index);
                    index += 8;
                    historicalPriceDataTickRecordResponse.AtBidOrAsk = (AtBidOrAskEnum)BitConverter.ToInt32(bytes, index);
                    // TODO is this 2-byte enum padded to 4?
                    index += 4;
                    historicalPriceDataTickRecordResponse.Price = BitConverter.ToDouble(bytes, index);
                    index += 8;
                    historicalPriceDataTickRecordResponse.Volume = BitConverter.ToDouble(bytes, index);
                    index += 8;
                    historicalPriceDataTickRecordResponse.IsFinalRecord = bytes[index++];
                    return result;
                default:
                    throw new ArgumentOutOfRangeException(messageType.ToString(), messageType, null);
            }
        }

        /// <summary>
        ///     Load the message represented by bytes into the IMessage
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