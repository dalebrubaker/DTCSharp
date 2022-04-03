using System;
using System.IO;
using DTCPB;
using Google.Protobuf;
using NLog;

namespace DTCCommon.Codecs
{
    public static class CodecBinaryConverter
    {
        private static readonly ILogger s_logger = LogManager.GetCurrentClassLogger();

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
        private const int CURRENCY_CODE_LENGTH = 8;
        private const int CLIENT_NAME_LENGTH = 32;

        /// <summary>
        /// Load the given empty message with the values from messageEncoded.MessageBytes
        /// </summary>
        /// <param name="messageEncoded"></param>
        /// <param name="message"></param>
        /// <exception cref="NotImplementedException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private static void ConvertToProtobuf(MessageEncoded messageEncoded, IMessage message)
        {
            var messageType = messageEncoded.MessageType;
            var bytes = messageEncoded.MessageBytes;
            var index = 0;
            switch (messageEncoded.MessageType)
            {
                case DTCMessageType.MessageTypeUnset:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinaryConverter)}: {messageType}");
                case DTCMessageType.LogonRequest:
                    var logonRequest = (LogonRequest)message;
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
                    logonRequest.Unused1 = BitConverter.ToInt32(bytes, index);
                    index += 4;
                    logonRequest.TradeAccount = bytes.StringFromNullTerminatedBytes(index);
                    index += TRADE_ACCOUNT_LENGTH;
                    logonRequest.HardwareIdentifier = bytes.StringFromNullTerminatedBytes(index);
                    index += GENERAL_IDENTIFIER_LENGTH;
                    logonRequest.ClientName = bytes.StringFromNullTerminatedBytes(index);
                    index += CLIENT_NAME_LENGTH;
                    logonRequest.MarketDataTransmissionInterval = BitConverter.ToInt32(bytes, index);
                    return;
                case DTCMessageType.LogonResponse:
                    var logonResponse = (LogonResponse)message;
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
                    logonResponse.Unused1 = bytes[index++];
                    logonResponse.UsesMultiplePositionsPerSymbolAndTradeAccount = bytes[index++];
                    logonResponse.MarketDataSupported = BitConverter.ToUInt32(bytes, index);
                    return;
                case DTCMessageType.Heartbeat:
                    var heartbeat = (Heartbeat)message;
                    heartbeat.NumDroppedMessages = BitConverter.ToUInt32(bytes, index);
                    index += 4;
                    heartbeat.CurrentDateTime = BitConverter.ToInt64(bytes, index);
                    return;
                case DTCMessageType.Logoff:
                    var logoff = (Logoff)message;
                    logoff.Reason = bytes.StringFromNullTerminatedBytes(index);
                    index += TEXT_DESCRIPTION_LENGTH;
                    logoff.DoNotReconnect = bytes[index++];
                    return;
                case DTCMessageType.EncodingRequest:
                    // EncodingResponse comes back as binary for all protocol versions
                    var encodingRequest = (EncodingRequest)message;
                    encodingRequest.ProtocolVersion = BitConverter.ToInt32(bytes, index);
                    index += 4;
                    encodingRequest.Encoding = (EncodingEnum)BitConverter.ToInt32(bytes, index);
                    index += 4;
                    encodingRequest.ProtocolType = bytes.StringFromNullTerminatedBytes(index);
                    return;
                case DTCMessageType.EncodingResponse:
                    // EncodingResponse comes back as binary for all protocol versions
                    var encodingResponse = (EncodingResponse)message;
                    encodingResponse.ProtocolVersion = BitConverter.ToInt32(bytes, index);
                    index += 4;
                    encodingResponse.Encoding = (EncodingEnum)BitConverter.ToInt32(bytes, index);
                    index += 4;
                    encodingResponse.ProtocolType = bytes.StringFromNullTerminatedBytes(index);
                    return;
                case DTCMessageType.MarketDataRequest:
                    var marketDataRequest = (MarketDataRequest)message;
                    marketDataRequest.RequestAction = (RequestActionEnum)BitConverter.ToInt32(bytes, index);
                    index += 4;
                    marketDataRequest.SymbolID = BitConverter.ToUInt16(bytes, index);
                    index += 2;
                    marketDataRequest.Symbol = bytes.StringFromNullTerminatedBytes(index);
                    index += SYMBOL_LENGTH;
                    marketDataRequest.Exchange = bytes.StringFromNullTerminatedBytes(index);
                    index += EXCHANGE_LENGTH;
                    marketDataRequest.IntervalForSnapshotUpdatesInMilliseconds = BitConverter.ToUInt32(bytes, index);
                    return;
                case DTCMessageType.MarketDataReject:
                    var marketDataReject = (MarketDataReject)message;
                    marketDataReject.SymbolID = BitConverter.ToUInt16(bytes, index);
                    index += 2;
                    marketDataReject.RejectText = bytes.StringFromNullTerminatedBytes(index);
                    index += TEXT_DESCRIPTION_LENGTH;
                    return;
                case DTCMessageType.MarketDataFeedStatus:
                    var marketDataFeedStatus = (MarketDataFeedStatus)message;
                    marketDataFeedStatus.Status = (MarketDataFeedStatusEnum)BitConverter.ToInt32(bytes, index);
                    index += 4;
                    return;
                case DTCMessageType.MarketDataFeedSymbolStatus:
                case DTCMessageType.SubmitNewSingleOrder:
                case DTCMessageType.SubmitNewSingleOrderInt:
                case DTCMessageType.SubmitNewOcoOrder:
                case DTCMessageType.SubmitNewOcoOrderInt:
                case DTCMessageType.CancelOrder:
                case DTCMessageType.CancelReplaceOrder:
                case DTCMessageType.CancelReplaceOrderInt:
                case DTCMessageType.OpenOrdersRequest:
                case DTCMessageType.OpenOrdersReject:
                case DTCMessageType.OrderUpdate:
                case DTCMessageType.HistoricalOrderFillsRequest:
                case DTCMessageType.HistoricalOrderFillResponse:
                case DTCMessageType.CurrentPositionsRequest:
                case DTCMessageType.CurrentPositionsReject:
                case DTCMessageType.TradeAccountsRequest:
                case DTCMessageType.TradeAccountResponse:
                    throw new NotImplementedException();
                case DTCMessageType.ExchangeListRequest:
                    var exchangeListRequest = (ExchangeListRequest)message;
                    exchangeListRequest.RequestID = BitConverter.ToInt32(bytes, index);
                    index += 4;
                    return;
                case DTCMessageType.ExchangeListResponse:
                    var exchangeListResponse = (ExchangeListResponse)message;
                    exchangeListResponse.RequestID = BitConverter.ToInt32(bytes, index);
                    index += 4;
                    exchangeListResponse.Exchange = bytes.StringFromNullTerminatedBytes(index);
                    index += EXCHANGE_LENGTH;
                    exchangeListResponse.IsFinalMessage = bytes[index++];
                    exchangeListResponse.Description = bytes.StringFromNullTerminatedBytes(index);
                    index += EXCHANGE_DESCRIPTION_LENGTH;
                    return;
                case DTCMessageType.SymbolsForExchangeRequest:
                case DTCMessageType.UnderlyingSymbolsForExchangeRequest:
                case DTCMessageType.SymbolsForUnderlyingRequest:
                    throw new NotImplementedException();
                case DTCMessageType.SecurityDefinitionForSymbolRequest:
                    var securityDefinitionForSymbolRequest = (SecurityDefinitionForSymbolRequest)message;
                    securityDefinitionForSymbolRequest.RequestID = BitConverter.ToInt32(bytes, index);
                    index += 4;
                    securityDefinitionForSymbolRequest.Symbol = bytes.StringFromNullTerminatedBytes(index);
                    index += SYMBOL_LENGTH;
                    securityDefinitionForSymbolRequest.Exchange = bytes.StringFromNullTerminatedBytes(index);
                    index += EXCHANGE_LENGTH;
                    return;
                case DTCMessageType.SecurityDefinitionResponse:
                    var securityDefinitionResponse = (SecurityDefinitionResponse)message;
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
                    securityDefinitionResponse.InitialMarginRequirement = BitConverter.ToSingle(bytes, index);
                    index += 4;
                    securityDefinitionResponse.MaintenanceMarginRequirement = BitConverter.ToSingle(bytes, index);
                    index += 4;
                    securityDefinitionResponse.Currency = bytes.StringFromNullTerminatedBytes(index);
                    index += CURRENCY_CODE_LENGTH;
                    securityDefinitionResponse.ContractSize = BitConverter.ToSingle(bytes, index);
                    index += 4;
                    securityDefinitionResponse.OpenInterest = BitConverter.ToUInt32(bytes, index);
                    index += 4;
                    securityDefinitionResponse.RolloverDate = BitConverter.ToInt32(bytes, index);
                    index += 4;
                    securityDefinitionResponse.IsDelayed = BitConverter.ToUInt32(bytes, index);
                    index += 4;
                    securityDefinitionResponse.SecurityIdentifier = BitConverter.ToInt64(bytes, index);
                    index += 8;
                    securityDefinitionResponse.ProductIdentifier = bytes.StringFromNullTerminatedBytes(index);
                    index += GENERAL_IDENTIFIER_LENGTH;
                    return;
                case DTCMessageType.SymbolSearchRequest:
                    throw new NotImplementedException();
                case DTCMessageType.SecurityDefinitionReject:
                    var securityDefinitionReject = (SecurityDefinitionReject)message;
                    securityDefinitionReject.RequestID = BitConverter.ToInt32(bytes, index);
                    index += 4;
                    securityDefinitionReject.RejectText = bytes.StringFromNullTerminatedBytes(index);
                    index += TEXT_DESCRIPTION_LENGTH;
                    return;
                case DTCMessageType.AccountBalanceRequest:
                    throw new NotImplementedException();
                case DTCMessageType.AccountBalanceReject:
                    throw new NotImplementedException();
                case DTCMessageType.AccountBalanceUpdate:
                case DTCMessageType.PositionUpdate:
                    // Probably being thrown from SC on an open trade while connecting binary for Encoding change
                    s_logger.Warn($"Ignoring binary message {messageEncoded}");
                    break;
                case DTCMessageType.UserMessage:
                    var userMessage = (UserMessage)message;
                    index = 0;
                    userMessage.UserMessage_ = bytes.StringFromNullTerminatedBytes(index);
                    index += TEXT_MESSAGE_LENGTH;
                    return;
                case DTCMessageType.GeneralLogMessage:
                    var generalLogMessage = (GeneralLogMessage)message;
                    generalLogMessage.MessageText = bytes.StringFromNullTerminatedBytes(index);
                    index += 128;
                    return;
                case DTCMessageType.HistoricalPriceDataRequest:
                    var historicalPriceDataRequest = (HistoricalPriceDataRequest)message;
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
                    return;
                case DTCMessageType.HistoricalPriceDataResponseHeader:
                    var historicalPriceDataResponseHeader = (HistoricalPriceDataResponseHeader)message;
                    historicalPriceDataResponseHeader.RequestID = BitConverter.ToInt32(bytes, index);
                    index += 4;
                    historicalPriceDataResponseHeader.RecordInterval = (HistoricalDataIntervalEnum)BitConverter.ToInt32(bytes, index);
                    index += 4;
                    historicalPriceDataResponseHeader.UseZLibCompression = bytes[index++];
                    historicalPriceDataResponseHeader.NoRecordsToReturn = bytes[index++];
                    index += 2; // align for packing
                    historicalPriceDataResponseHeader.IntToFloatPriceDivisor = BitConverter.ToSingle(bytes, index);

                    //Logger.Debug($"{nameof(CodecBinary)} loaded {messageType} {result}");
                    return;
                case DTCMessageType.HistoricalPriceDataReject:
                    var historicalPriceDataReject = (HistoricalPriceDataReject)message;
                    historicalPriceDataReject.RequestID = BitConverter.ToInt32(bytes, index);
                    index += 4;
                    historicalPriceDataReject.RejectText = bytes.StringFromNullTerminatedBytes(index);
                    index += TEXT_DESCRIPTION_LENGTH;
                    historicalPriceDataReject.RejectReasonCode = (HistoricalPriceDataRejectReasonCodeEnum)BitConverter.ToInt16(bytes, index);
                    index += 2;
                    historicalPriceDataReject.RetryTimeInSeconds = BitConverter.ToUInt16(bytes, index);
                    index += 2;
                    return;
                case DTCMessageType.HistoricalPriceDataRecordResponse:
                    var historicalPriceDataRecordResponse = (HistoricalPriceDataRecordResponse)message;
                    historicalPriceDataRecordResponse.RequestID = BitConverter.ToInt32(bytes, index);
                    index += 4;
                    historicalPriceDataRecordResponse.StartDateTime = BitConverter.ToInt64(bytes, index);
                    var _ = $"{historicalPriceDataRecordResponse.StartDateTime.DtcDateTimeToUtc().ToLocalTime():yyyyMMdd.HHmmss.fff} (local).";
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
                    if (historicalPriceDataRecordResponse.Volume == 0)
                    {
                    }
                    historicalPriceDataRecordResponse.IsFinalRecord = bytes[index++];
                    return;
                case DTCMessageType.HistoricalPriceDataTickRecordResponse:
                    // Probably no longer used after version 1150 per https://www.sierrachart.com/index.php?page=doc/IntradayDataFileFormat.html
                    var historicalPriceDataTickRecordResponse = (HistoricalPriceDataTickRecordResponse)message;
                    historicalPriceDataTickRecordResponse.RequestID = BitConverter.ToInt32(bytes, index);
                    index += 4;
                    historicalPriceDataTickRecordResponse.DateTime = BitConverter.ToInt64(bytes, index);
                    index += 8;
                    historicalPriceDataTickRecordResponse.AtBidOrAsk = (AtBidOrAskEnum)BitConverter.ToInt32(bytes, index);
                    // I think this 2-byte enum padded to 4?
                    index += 4;
                    historicalPriceDataTickRecordResponse.Price = BitConverter.ToDouble(bytes, index);
                    index += 8;
                    historicalPriceDataTickRecordResponse.Volume = BitConverter.ToDouble(bytes, index);
                    index += 8;
                    historicalPriceDataTickRecordResponse.IsFinalRecord = bytes[index++];
                    return;
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
                case DTCMessageType.MarketDataUpdateSessionVolume:
                case DTCMessageType.MarketDataUpdateOpenInterest:
                case DTCMessageType.MarketDataUpdateSessionSettlement:
                case DTCMessageType.MarketDataUpdateSessionSettlementInt:
                case DTCMessageType.MarketDataUpdateSessionNumTrades:
                case DTCMessageType.MarketDataUpdateTradingSessionDate:
                case DTCMessageType.MarketDepthRequest:
                case DTCMessageType.MarketDepthReject:
                case DTCMessageType.MarketDepthSnapshotLevel:
                case DTCMessageType.MarketDepthSnapshotLevelInt:
                case DTCMessageType.MarketDepthSnapshotLevelFloat:
                case DTCMessageType.MarketDepthUpdateLevel:
                case DTCMessageType.MarketDepthUpdateLevelFloatWithMilliseconds:
                case DTCMessageType.MarketDepthUpdateLevelNoTimestamp:
                case DTCMessageType.MarketDepthUpdateLevelInt:
                case DTCMessageType.TradingSymbolStatus:
                case DTCMessageType.SubmitFlattenPositionOrder:
                case DTCMessageType.HistoricalOrderFillsReject:
                case DTCMessageType.AccountBalanceAdjustment:
                case DTCMessageType.AccountBalanceAdjustmentReject:
                case DTCMessageType.AccountBalanceAdjustmentComplete:
                case DTCMessageType.HistoricalAccountBalancesRequest:
                case DTCMessageType.HistoricalAccountBalancesReject:
                case DTCMessageType.HistoricalAccountBalanceResponse:
                case DTCMessageType.AlertMessage:
                case DTCMessageType.JournalEntryAdd:
                case DTCMessageType.JournalEntriesRequest:
                case DTCMessageType.JournalEntriesReject:
                case DTCMessageType.JournalEntryResponse:
                case DTCMessageType.HistoricalPriceDataRecordResponseInt:
                case DTCMessageType.HistoricalPriceDataTickRecordResponseInt:
                case DTCMessageType.HistoricalPriceDataResponseTrailer:
                case DTCMessageType.HistoricalMarketDepthDataRequest:
                case DTCMessageType.HistoricalMarketDepthDataResponseHeader:
                case DTCMessageType.HistoricalMarketDepthDataReject:
                    throw new NotImplementedException();
                case DTCMessageType.HistoricalMarketDepthDataRecordResponse:
                    // Probably no longer used after version SierraChart version 1150 per https://www.sierrachart.com/index.php?page=doc/IntradayDataFileFormat.html
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException(messageType.ToString(), messageType, null);
            }
        }

        public static MessageProto DecodeBinary(MessageEncoded messageEncoded)
        {
            if (messageEncoded.IsExtended)
            {
                throw new NotSupportedException("No extended message types currently are supported in Binary Encoding.");
            }
            var message = EmptyProtobufs.GetEmptyProtobuf(messageEncoded.MessageType);
            ConvertToProtobuf(messageEncoded, message);
            var result = new MessageProto(messageEncoded.MessageType, message);
            return result;
        }
        

        public static MessageEncoded EncodeBinary(MessageProto messageProto)
        {
            if (messageProto.IsExtended)
            {
                throw new NotSupportedException("No extended message types currently are supported in Binary Encoding.");
            }
            var bytes = ConvertToBinary(messageProto);
            var result = new MessageEncoded(messageProto.MessageType, bytes);
            return result;
        }

        private static byte[] ConvertToBinary(MessageProto messageProto)
        {
            //Logger.Debug($"Writing {messageType} when _isZippedStream={_isZippedStream}");
            var messageType = messageProto.MessageType;
            var message = messageProto.Message;
            switch (messageType)
            {
                case DTCMessageType.MessageTypeUnset:
                    return Array.Empty<byte>();
                case DTCMessageType.LogonRequest:
                    var logonRequest = (LogonRequest)message;
                    return ConvertToBufferLogonRequest(messageType, logonRequest);
                case DTCMessageType.LogonResponse:
                    var logonResponse = (LogonResponse)message;
                    return ConvertToBufferLogonResponse(messageType, logonResponse);
                case DTCMessageType.Heartbeat:
                    var heartbeat = (Heartbeat)message;
                    return ConvertToBufferHeartbeat(messageType, heartbeat);
                case DTCMessageType.Logoff:
                    var logoff = (Logoff)message;
                    return ConvertToBufferLogoff(messageType, logoff);
                case DTCMessageType.EncodingRequest:
                    var encodingRequest = (EncodingRequest)message;
                    return ConvertToBufferEncodingRequest(messageType, encodingRequest);
                case DTCMessageType.EncodingResponse:
                    var encodingResponse = (EncodingResponse)message;
                    return ConvertToBufferEncodingResponse(messageType, encodingResponse);
                case DTCMessageType.MarketDataRequest:
                    var marketDataRequest = (MarketDataRequest)message;
                    return ConvertToBufferMarketDataRequest(messageType, marketDataRequest);
                case DTCMessageType.MarketDataReject:
                    var marketDataReject = (MarketDataReject)message;
                    return ConvertToBufferMarketDataReject(messageType, marketDataReject);
                case DTCMessageType.MarketDataFeedStatus:
                    var marketDataFeedStatus = (MarketDataFeedStatus)message;
                    return ConvertToBufferMarketDataFeedStatus(messageType, marketDataFeedStatus);
                case DTCMessageType.ExchangeListRequest:
                    var exchangeListRequest = (ExchangeListRequest)message;
                    return ConvertToBufferExchangeListRequest(messageType, exchangeListRequest);
                case DTCMessageType.ExchangeListResponse:
                    var exchangeListResponse = (ExchangeListResponse)message;
                    return ConvertToBufferExchangeListResponse(messageType, exchangeListResponse);
                case DTCMessageType.SecurityDefinitionForSymbolRequest:
                    var securityDefinitionForSymbolRequest = (SecurityDefinitionForSymbolRequest)message;
                    return ConvertToBufferSecurityDefinitionForSymbolRequest(messageType, securityDefinitionForSymbolRequest);
                case DTCMessageType.SecurityDefinitionResponse:
                    var securityDefinitionResponse = (SecurityDefinitionResponse)message;
                    return ConvertToBufferSecurityDefinitionResponse(messageType, securityDefinitionResponse);
                case DTCMessageType.SecurityDefinitionReject:
                    var securityDefinitionReject = (SecurityDefinitionReject)message;
                    return ConvertToBufferSecurityDefinitionReject(messageType, securityDefinitionReject);
                case DTCMessageType.HistoricalPriceDataRequest:
                    var historicalPriceDataRequest = (HistoricalPriceDataRequest)message;
                    return ConvertToBufferHistoricalPriceDataRequest(messageType, historicalPriceDataRequest);
                case DTCMessageType.HistoricalPriceDataResponseHeader:
                    // Logger.Debug($"{nameof(CodecBinary)} is writing {messageType} {message}");
                    var historicalPriceDataResponseHeader = (HistoricalPriceDataResponseHeader)message;
                    return ConvertToBufferHistoricalPriceDataResponseHeader(messageType, historicalPriceDataResponseHeader);
                case DTCMessageType.HistoricalPriceDataReject:
                    var historicalPriceDataReject = (HistoricalPriceDataReject)message;
                    return ConvertToBufferHistoricalPriceDataReject(messageType, historicalPriceDataReject);
                case DTCMessageType.HistoricalPriceDataRecordResponse:
                    var historicalPriceDataRecordResponse = (HistoricalPriceDataRecordResponse)message;
                    return ConvertToBufferHistoricalPriceDataRecordResponse(messageType, historicalPriceDataRecordResponse);
                case DTCMessageType.HistoricalPriceDataTickRecordResponse:
                    // Probably no longer used after version SierraChart version 1150 per https://www.sierrachart.com/index.php?page=doc/IntradayDataFileFormat.html
                    throw new NotSupportedException($"Not implemented in {nameof(CodecBinaryConverter)}: {messageType}");
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
                case DTCMessageType.MarketDataUpdateSessionVolume:
                case DTCMessageType.MarketDataUpdateOpenInterest:
                case DTCMessageType.MarketDataUpdateSessionSettlement:
                case DTCMessageType.MarketDataUpdateSessionSettlementInt:
                case DTCMessageType.MarketDataUpdateSessionNumTrades:
                case DTCMessageType.MarketDataUpdateTradingSessionDate:
                case DTCMessageType.MarketDepthRequest:
                case DTCMessageType.MarketDepthReject:
                case DTCMessageType.MarketDepthSnapshotLevel:
                case DTCMessageType.MarketDepthSnapshotLevelInt:
                case DTCMessageType.MarketDepthSnapshotLevelFloat:
                case DTCMessageType.MarketDepthUpdateLevel:
                case DTCMessageType.MarketDepthUpdateLevelFloatWithMilliseconds:
                case DTCMessageType.MarketDepthUpdateLevelNoTimestamp:
                case DTCMessageType.MarketDepthUpdateLevelInt:
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
                case DTCMessageType.HistoricalOrderFillResponse:
                case DTCMessageType.HistoricalOrderFillsReject:
                case DTCMessageType.CurrentPositionsRequest:
                case DTCMessageType.CurrentPositionsReject:
                case DTCMessageType.PositionUpdate:
                case DTCMessageType.TradeAccountsRequest:
                case DTCMessageType.TradeAccountResponse:
                case DTCMessageType.SymbolsForExchangeRequest:
                case DTCMessageType.UnderlyingSymbolsForExchangeRequest:
                case DTCMessageType.SymbolsForUnderlyingRequest:
                case DTCMessageType.SymbolSearchRequest:
                case DTCMessageType.AccountBalanceRequest:
                case DTCMessageType.AccountBalanceReject:
                case DTCMessageType.AccountBalanceUpdate:
                case DTCMessageType.AccountBalanceAdjustment:
                case DTCMessageType.AccountBalanceAdjustmentReject:
                case DTCMessageType.AccountBalanceAdjustmentComplete:
                case DTCMessageType.HistoricalAccountBalancesRequest:
                case DTCMessageType.HistoricalAccountBalancesReject:
                case DTCMessageType.HistoricalAccountBalanceResponse:
                case DTCMessageType.UserMessage:
                case DTCMessageType.GeneralLogMessage:
                case DTCMessageType.AlertMessage:
                case DTCMessageType.JournalEntryAdd:
                case DTCMessageType.JournalEntriesRequest:
                case DTCMessageType.JournalEntriesReject:
                case DTCMessageType.JournalEntryResponse:
                case DTCMessageType.HistoricalPriceDataRecordResponseInt:
                case DTCMessageType.HistoricalPriceDataTickRecordResponseInt:
                case DTCMessageType.HistoricalPriceDataResponseTrailer:
                case DTCMessageType.HistoricalMarketDepthDataRequest:
                case DTCMessageType.HistoricalMarketDepthDataResponseHeader:
                case DTCMessageType.HistoricalMarketDepthDataReject:
                case DTCMessageType.HistoricalMarketDepthDataRecordResponse:
                case DTCMessageType.MarketOrdersRequest:
                case DTCMessageType.MarketOrdersReject:
                case DTCMessageType.MarketOrdersAdd:
                case DTCMessageType.MarketOrdersModify:
                case DTCMessageType.MarketOrdersRemove:
                case DTCMessageType.MarketOrdersSnapshotMessageBoundary:
                case DTCMessageType.AddCorrectingOrderFill:
                case DTCMessageType.CorrectingOrderFillResponse:
                default:
                    throw new NotSupportedException($"Not implemented in {nameof(CodecBinaryConverter)}: {messageType}");
            }
        }

        private static byte[] ConvertToBufferEncodingRequest(DTCMessageType messageType, EncodingRequest encodingRequest)
        {
            using var bufferBuilder = new BufferBuilder(12);
            bufferBuilder.Add(encodingRequest.ProtocolVersion);
            bufferBuilder.Add((int)encodingRequest.Encoding); // enum size is 4
            var protocolType = encodingRequest.ProtocolType.ToFixedBytes(4);
            bufferBuilder.Add(protocolType); // 3 chars DTC plus null terminator 
            return bufferBuilder.Buffer;
        }

        private static byte[] ConvertToBufferEncodingResponse(DTCMessageType messageType, EncodingResponse encodingResponse)
        {
            using var bufferBuilder = new BufferBuilder(12);
            bufferBuilder.Add(encodingResponse.ProtocolVersion);
            bufferBuilder.Add((int)encodingResponse.Encoding); // enum size is 4
            var protocolType = encodingResponse.ProtocolType.ToFixedBytes(4);
            bufferBuilder.Add(protocolType); // 3 chars DTC plus null terminator 
            return bufferBuilder.Buffer;
        }

        private static byte[] ConvertToBufferHistoricalPriceDataRecordResponse(DTCMessageType messageType,
            HistoricalPriceDataRecordResponse historicalPriceDataRecordResponse)
        {
            const int SizeExcludingHeader = 4 + 9 * 8 + 1;
            using var bufferBuilder = new BufferBuilder(SizeExcludingHeader);
            bufferBuilder.Add(historicalPriceDataRecordResponse.RequestID);
            bufferBuilder.Add(historicalPriceDataRecordResponse.StartDateTime);
            bufferBuilder.Add(historicalPriceDataRecordResponse.OpenPrice);
            bufferBuilder.Add(historicalPriceDataRecordResponse.HighPrice);
            bufferBuilder.Add(historicalPriceDataRecordResponse.LowPrice);
            bufferBuilder.Add(historicalPriceDataRecordResponse.LastPrice);
            bufferBuilder.Add(historicalPriceDataRecordResponse.Volume);
            bufferBuilder.Add(historicalPriceDataRecordResponse.NumTrades);
            bufferBuilder.Add(0); // for 8-byte packing boundary
            bufferBuilder.Add(historicalPriceDataRecordResponse.BidVolume);
            bufferBuilder.Add(historicalPriceDataRecordResponse.AskVolume);
            bufferBuilder.Add((byte)historicalPriceDataRecordResponse.IsFinalRecord);
            return bufferBuilder.Buffer;
        }

        private static byte[] ConvertToBufferHistoricalPriceDataReject(DTCMessageType messageType, HistoricalPriceDataReject historicalPriceDataReject)
        {
            const int SizeExcludingHeader = 4 + TEXT_DESCRIPTION_LENGTH + 2 + 2;
            using var bufferBuilder = new BufferBuilder(SizeExcludingHeader);
            bufferBuilder.Add(historicalPriceDataReject.RequestID);
            bufferBuilder.Add(historicalPriceDataReject.RejectText.ToFixedBytes(TEXT_DESCRIPTION_LENGTH));
            bufferBuilder.Add((short)historicalPriceDataReject.RejectReasonCode);
            bufferBuilder.Add((ushort)historicalPriceDataReject.RetryTimeInSeconds);
            return bufferBuilder.Buffer;
        }

        private static byte[] ConvertToBufferHistoricalPriceDataResponseHeader(DTCMessageType messageType, HistoricalPriceDataResponseHeader historicalPriceDataResponseHeader)
        {
            const int SizeExcludingHeader = 4 + 4 + 2 + 2 + 4;
            using var bufferBuilder = new BufferBuilder(SizeExcludingHeader);
            bufferBuilder.Add(historicalPriceDataResponseHeader.RequestID);
            bufferBuilder.Add((int)historicalPriceDataResponseHeader.RecordInterval);
            bufferBuilder.Add((byte)historicalPriceDataResponseHeader.UseZLibCompression);
            bufferBuilder.Add((byte)historicalPriceDataResponseHeader.NoRecordsToReturn);
            bufferBuilder.Add((short)0); // align for packing
            bufferBuilder.Add(historicalPriceDataResponseHeader.IntToFloatPriceDivisor);
            return bufferBuilder.Buffer;
        }

        private static byte[] ConvertToBufferHistoricalPriceDataRequest(DTCMessageType messageType, HistoricalPriceDataRequest historicalPriceDataRequest)
        {
            const int SizeExcludingHeader = 4 + SYMBOL_LENGTH + EXCHANGE_LENGTH + 4 + 4 + 8 + 8 + 4 + 3 * 1;
            using var bufferBuilder = new BufferBuilder(SizeExcludingHeader);
            bufferBuilder.Add(historicalPriceDataRequest.RequestID);
            bufferBuilder.Add(historicalPriceDataRequest.Symbol.ToFixedBytes(SYMBOL_LENGTH));
            bufferBuilder.Add(historicalPriceDataRequest.Exchange.ToFixedBytes(EXCHANGE_LENGTH));
            bufferBuilder.Add((int)historicalPriceDataRequest.RecordInterval);
            bufferBuilder.Add(0); // 4 bytes for alignment on 8-byte boundary
            bufferBuilder.Add(historicalPriceDataRequest.StartDateTime);
            bufferBuilder.Add(historicalPriceDataRequest.EndDateTime);
            bufferBuilder.Add(historicalPriceDataRequest.MaxDaysToReturn);
            bufferBuilder.Add((byte)historicalPriceDataRequest.UseZLibCompression);
            bufferBuilder.Add((byte)historicalPriceDataRequest.RequestDividendAdjustedStockData);
            bufferBuilder.Add((byte)historicalPriceDataRequest.Integer1);
            return bufferBuilder.Buffer;
        }

        private static byte[] ConvertToBufferSecurityDefinitionReject(DTCMessageType messageType, SecurityDefinitionReject securityDefinitionReject)
        {
            const int SizeExcludingHeader = 4 + TEXT_DESCRIPTION_LENGTH;
            using var bufferBuilder = new BufferBuilder(SizeExcludingHeader);
            bufferBuilder.Add(securityDefinitionReject.RequestID);
            bufferBuilder.Add(securityDefinitionReject.RejectText.ToFixedBytes(TEXT_DESCRIPTION_LENGTH));
            return bufferBuilder.Buffer;
        }

        private static byte[] ConvertToBufferSecurityDefinitionResponse(DTCMessageType messageType, SecurityDefinitionResponse securityDefinitionResponse)
        {
            const int SizeExcludingHeader = 4
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
                                            + SYMBOL_LENGTH // for ExchangeSymbol
                                            + 4
                                            + 4
                                            + CURRENCY_CODE_LENGTH
                                            + 4
                                            + 4
                                            + 4
                                            + 4 // IsDelayed
                                            +8
                                            +GENERAL_IDENTIFIER_LENGTH; // ProductIdentifier uses this length
            using var bufferBuilder = new BufferBuilder(SizeExcludingHeader);
            bufferBuilder.Add(securityDefinitionResponse.RequestID);
            bufferBuilder.Add(securityDefinitionResponse.Symbol.ToFixedBytes(SYMBOL_LENGTH));
            bufferBuilder.Add(securityDefinitionResponse.Exchange.ToFixedBytes(EXCHANGE_LENGTH));
            bufferBuilder.Add((int)securityDefinitionResponse.SecurityType);
            bufferBuilder.Add(securityDefinitionResponse.Description.ToFixedBytes(SYMBOL_DESCRIPTION_LENGTH));
            bufferBuilder.Add(securityDefinitionResponse.MinPriceIncrement);
            bufferBuilder.Add((int)securityDefinitionResponse.PriceDisplayFormat);
            bufferBuilder.Add(securityDefinitionResponse.CurrencyValuePerIncrement);
            bufferBuilder.Add(securityDefinitionResponse.IsFinalMessage);
            bufferBuilder.Add(securityDefinitionResponse.FloatToIntPriceMultiplier);
            bufferBuilder.Add(securityDefinitionResponse.IntToFloatPriceDivisor);
            bufferBuilder.Add(securityDefinitionResponse.UnderlyingSymbol.ToFixedBytes(UNDERLYING_SYMBOL_LENGTH));
            bufferBuilder.Add(securityDefinitionResponse.UpdatesBidAskOnly);
            bufferBuilder.Add(securityDefinitionResponse.StrikePrice);
            bufferBuilder.Add((int)securityDefinitionResponse.PutOrCall);
            bufferBuilder.Add(securityDefinitionResponse.ShortInterest);
            bufferBuilder.Add((uint)securityDefinitionResponse.SecurityExpirationDate);
            bufferBuilder.Add(securityDefinitionResponse.BuyRolloverInterest);
            bufferBuilder.Add(securityDefinitionResponse.SellRolloverInterest);
            bufferBuilder.Add(securityDefinitionResponse.EarningsPerShare);
            bufferBuilder.Add(securityDefinitionResponse.SharesOutstanding);
            bufferBuilder.Add(securityDefinitionResponse.IntToFloatQuantityDivisor);
            bufferBuilder.Add(securityDefinitionResponse.HasMarketDepthData);
            bufferBuilder.Add(securityDefinitionResponse.DisplayPriceMultiplier);
            bufferBuilder.Add(securityDefinitionResponse.ExchangeSymbol.ToFixedBytes(SYMBOL_LENGTH));
            bufferBuilder.Add(securityDefinitionResponse.InitialMarginRequirement);
            bufferBuilder.Add(securityDefinitionResponse.MaintenanceMarginRequirement);
            bufferBuilder.Add(securityDefinitionResponse.Currency.ToFixedBytes(CURRENCY_CODE_LENGTH));
            bufferBuilder.Add(securityDefinitionResponse.ContractSize);
            bufferBuilder.Add(securityDefinitionResponse.OpenInterest);
            bufferBuilder.Add(securityDefinitionResponse.RolloverDate); // DateTime4byte per  https://dtcprotocol.org/index.php?page=doc/DTCMessages_SymbolDiscoverySecurityDefinitionsMessages.php#Messages-SECURITY_DEFINITION_RESPONSE
            bufferBuilder.Add(securityDefinitionResponse.IsDelayed);
            bufferBuilder.Add(securityDefinitionResponse.SecurityIdentifier);
            bufferBuilder.Add(securityDefinitionResponse.ProductIdentifier.ToFixedBytes(GENERAL_IDENTIFIER_LENGTH));
            return bufferBuilder.Buffer;
        }

        private static byte[] ConvertToBufferSecurityDefinitionForSymbolRequest(DTCMessageType messageType, SecurityDefinitionForSymbolRequest securityDefinitionForSymbolRequest)
        {
            const int SizeExcludingHeader = 4 + SYMBOL_LENGTH + EXCHANGE_LENGTH;
            using var bufferBuilder = new BufferBuilder(SizeExcludingHeader);
            bufferBuilder.Add(securityDefinitionForSymbolRequest.RequestID);
            bufferBuilder.Add(securityDefinitionForSymbolRequest.Symbol.ToFixedBytes(SYMBOL_LENGTH));
            bufferBuilder.Add(securityDefinitionForSymbolRequest.Exchange.ToFixedBytes(EXCHANGE_LENGTH));
            return bufferBuilder.Buffer;
        }

        private static byte[] ConvertToBufferExchangeListResponse(DTCMessageType messageType, ExchangeListResponse exchangeListResponse)
        {
            const int SizeExcludingHeader = 4 + EXCHANGE_LENGTH + 1 + EXCHANGE_DESCRIPTION_LENGTH;
            using var bufferBuilder = new BufferBuilder(SizeExcludingHeader);
            bufferBuilder.Add(exchangeListResponse.RequestID);
            bufferBuilder.Add(exchangeListResponse.Exchange.ToFixedBytes(EXCHANGE_LENGTH));
            bufferBuilder.Add((byte)exchangeListResponse.IsFinalMessage);
            bufferBuilder.Add(exchangeListResponse.Description.ToFixedBytes(EXCHANGE_DESCRIPTION_LENGTH));
            return bufferBuilder.Buffer;
        }

        private static byte[] ConvertToBufferExchangeListRequest(DTCMessageType messageType, ExchangeListRequest exchangeListRequest)
        {
            const int SizeExcludingHeader = 4;
            using var bufferBuilder = new BufferBuilder(SizeExcludingHeader);
            bufferBuilder.Add(exchangeListRequest.RequestID);
            return bufferBuilder.Buffer;
        }

        private static byte[] ConvertToBufferMarketDataFeedStatus(DTCMessageType messageType, MarketDataFeedStatus marketDataFeedStatus)
        {
            const int SizeExcludingHeader = 4;
            using var bufferBuilder = new BufferBuilder(SizeExcludingHeader);
            bufferBuilder.Add((int)marketDataFeedStatus.Status);
            return bufferBuilder.Buffer;
        }

        private static byte[] ConvertToBufferMarketDataReject(DTCMessageType messageType, MarketDataReject marketDataReject)
        {
            const int SizeExcludingHeader = 2 + TEXT_DESCRIPTION_LENGTH;
            using var bufferBuilder = new BufferBuilder(SizeExcludingHeader);
            bufferBuilder.Add((ushort)marketDataReject.SymbolID);
            bufferBuilder.Add(marketDataReject.RejectText.ToFixedBytes(TEXT_DESCRIPTION_LENGTH));
            return bufferBuilder.Buffer;
        }

        private static byte[] ConvertToBufferMarketDataRequest(DTCMessageType messageType, MarketDataRequest marketDataRequest)
        {
            const int SizeExcludingHeader = 4 + 2 + SYMBOL_LENGTH + EXCHANGE_LENGTH + 4;
            using var bufferBuilder = new BufferBuilder(SizeExcludingHeader);
            bufferBuilder.Add((int)marketDataRequest.RequestAction);
            bufferBuilder.Add((ushort)marketDataRequest.SymbolID);
            bufferBuilder.Add(marketDataRequest.Symbol.ToFixedBytes(SYMBOL_LENGTH));
            bufferBuilder.Add(marketDataRequest.Exchange.ToFixedBytes(EXCHANGE_LENGTH));
            bufferBuilder.Add(marketDataRequest.IntervalForSnapshotUpdatesInMilliseconds);
            return bufferBuilder.Buffer;
        }

        private static byte[] ConvertToBufferLogoff(DTCMessageType messageType, Logoff logoff)
        {
            try
            {
                const int SizeExcludingHeader = TEXT_DESCRIPTION_LENGTH + 1;
                using var bufferBuilder = new BufferBuilder(SizeExcludingHeader);
                bufferBuilder.Add(logoff.Reason.ToFixedBytes(TEXT_DESCRIPTION_LENGTH));
                bufferBuilder.Add((byte)logoff.DoNotReconnect);
                return bufferBuilder.Buffer;
            }
            catch (IOException)
            {
                // // Ignore this exception, which happens on Dispose() if the stream has already gone away, as when the ClientHandler finishes sending zipped historical records 
                // var _ = ex;
                throw;
            }
        }

        private static byte[] ConvertToBufferHeartbeat(DTCMessageType messageType, Heartbeat heartbeat)
        {
            using var bufferBuilder = new BufferBuilder(12);
            bufferBuilder.Add(heartbeat.NumDroppedMessages);
            bufferBuilder.Add(heartbeat.CurrentDateTime);
            return bufferBuilder.Buffer;
        }

        private static byte[] ConvertToBufferLogonResponse(DTCMessageType messageType, LogonResponse logonResponse)
        {
            const int SizeExcludingHeader = 4 + 4 + TEXT_DESCRIPTION_LENGTH + 64 + 4 + 60 + 4 * 1 + SYMBOL_EXCHANGE_DELIMITER_LENGTH + 8 * 1 + 4;
            using var bufferBuilder = new BufferBuilder(SizeExcludingHeader);
            bufferBuilder.Add(logonResponse.ProtocolVersion);
            bufferBuilder.Add((int)logonResponse.Result);
            bufferBuilder.Add(logonResponse.ResultText.ToFixedBytes(TEXT_DESCRIPTION_LENGTH));
            bufferBuilder.Add(logonResponse.ReconnectAddress.ToFixedBytes(64));
            bufferBuilder.Add(logonResponse.Integer1);
            bufferBuilder.Add(logonResponse.ServerName.ToFixedBytes(60));
            bufferBuilder.Add((byte)logonResponse.MarketDepthUpdatesBestBidAndAsk);
            bufferBuilder.Add((byte)logonResponse.TradingIsSupported);
            bufferBuilder.Add((byte)logonResponse.OCOOrdersSupported);
            bufferBuilder.Add((byte)logonResponse.OrderCancelReplaceSupported);
            bufferBuilder.Add(logonResponse.SymbolExchangeDelimiter.ToFixedBytes(SYMBOL_EXCHANGE_DELIMITER_LENGTH));
            bufferBuilder.Add((byte)logonResponse.SecurityDefinitionsSupported);
            bufferBuilder.Add((byte)logonResponse.HistoricalPriceDataSupported);
            bufferBuilder.Add((byte)logonResponse.ResubscribeWhenMarketDataFeedAvailable);
            bufferBuilder.Add((byte)logonResponse.MarketDepthIsSupported);
            bufferBuilder.Add((byte)logonResponse.OneHistoricalPriceDataRequestPerConnection);
            bufferBuilder.Add((byte)logonResponse.BracketOrdersSupported);
            bufferBuilder.Add((byte)logonResponse.Unused1);
            bufferBuilder.Add((byte)logonResponse.UsesMultiplePositionsPerSymbolAndTradeAccount);
            bufferBuilder.Add(logonResponse.MarketDataSupported);
            return bufferBuilder.Buffer;
        }

        private static byte[] ConvertToBufferLogonRequest(DTCMessageType messageType, LogonRequest logonRequest)
        {
            const int SizeExcludingHeader = 4
                                            + USERNAME_PASSWORD_LENGTH
                                            + USERNAME_PASSWORD_LENGTH
                                            + GENERAL_IDENTIFIER_LENGTH
                                            + 4
                                            + 4
                                            + 4
                                            + 4
                                            + TRADE_ACCOUNT_LENGTH
                                            + GENERAL_IDENTIFIER_LENGTH
                                            + 36;
            using var bufferBuilder = new BufferBuilder(SizeExcludingHeader);
            bufferBuilder.Add(logonRequest.ProtocolVersion);
            bufferBuilder.Add(logonRequest.Username.ToFixedBytes(USERNAME_PASSWORD_LENGTH));
            bufferBuilder.Add(logonRequest.Password.ToFixedBytes(USERNAME_PASSWORD_LENGTH));
            bufferBuilder.Add(logonRequest.GeneralTextData.ToFixedBytes(GENERAL_IDENTIFIER_LENGTH));
            bufferBuilder.Add(logonRequest.Integer1);
            bufferBuilder.Add(logonRequest.Integer2);
            bufferBuilder.Add(logonRequest.HeartbeatIntervalInSeconds);
            bufferBuilder.Add(logonRequest.Unused1);
            bufferBuilder.Add(logonRequest.TradeAccount.ToFixedBytes(TRADE_ACCOUNT_LENGTH));
            bufferBuilder.Add(logonRequest.HardwareIdentifier.ToFixedBytes(GENERAL_IDENTIFIER_LENGTH));
            bufferBuilder.Add(logonRequest.ClientName.ToFixedBytes(CLIENT_NAME_LENGTH));
            bufferBuilder.Add(logonRequest.MarketDataTransmissionInterval);
            return bufferBuilder.Buffer;
        }
    }
}