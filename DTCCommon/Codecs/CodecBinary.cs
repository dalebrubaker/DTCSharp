﻿using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DTCCommon.Extensions;
using DTCPB;
using uint8_t = System.Byte;
using int32_t = System.Int32;

// ReSharper disable InconsistentNaming

namespace DTCCommon.Codecs
{
    public class CodecBinary : Codec
    {
        public CodecBinary(Stream stream) : base(stream)
        {
        }

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

        public override EncodingEnum Encoding => EncodingEnum.BinaryEncoding;

        public override async Task WriteAsync<T>(DTCMessageType messageType, T message, CancellationToken cancellationToken)
        {
            //Logger.Debug($"Writing {messageType} when _isZippedStream={_isZippedStream}");
            switch (messageType)
            {
                case DTCMessageType.MessageTypeUnset:
                    throw new ArgumentException(messageType.ToString());
                case DTCMessageType.LogonRequest:
                    var logonRequest = message as LogonRequest;
                    await WriteLogonRequestAsync<T>(messageType, logonRequest, cancellationToken).ConfigureAwait(false);
                    return;
                case DTCMessageType.LogonResponse:
                    var logonResponse = message as LogonResponse;
                    await WriteLogonResponseAsync<T>(messageType, logonResponse, cancellationToken).ConfigureAwait(false);
                    return;
                case DTCMessageType.Heartbeat:
                    if (_disabledHeartbeats)
                    {
                        return;
                    }
                    var heartbeat = message as Heartbeat;
                    await WriteHeartbeatAsync<T>(messageType, heartbeat, cancellationToken).ConfigureAwait(false);
                    return;
                case DTCMessageType.Logoff:
                    var logoff = message as Logoff;
                    await WriteLogoffAsync<T>(messageType, logoff, cancellationToken).ConfigureAwait(false);
                    return;
                case DTCMessageType.EncodingRequest:
                    var encodingRequest = message as EncodingRequest;
                    await WriteEncodingRequestAsync(messageType, encodingRequest, cancellationToken);
                    return;
                case DTCMessageType.EncodingResponse:
                    var encodingResponse = message as EncodingResponse;
                    await WriteEncodingResponseAsync(messageType, encodingResponse, cancellationToken).ConfigureAwait(false);
                    return;
                case DTCMessageType.MarketDataRequest:
                    var marketDataRequest = message as MarketDataRequest;
                    await WriteMarketDataRequestAsync(messageType, marketDataRequest, cancellationToken).ConfigureAwait(false);
                    return;
                case DTCMessageType.MarketDataReject:
                    var marketDataReject = message as MarketDataReject;
                    await WriteMarketDataRejectAsync(messageType, marketDataReject, cancellationToken).ConfigureAwait(false);
                    return;
                case DTCMessageType.MarketDataFeedStatus:
                    var marketDataFeedStatus = message as MarketDataFeedStatus;
                    await WriteMarketDataFeedStatusAsync(messageType, marketDataFeedStatus, cancellationToken).ConfigureAwait(false);
                    return;
                case DTCMessageType.ExchangeListRequest:
                    var exchangeListRequest = message as ExchangeListRequest;
                    await WriteExchangeListRequestAsync(messageType, exchangeListRequest, cancellationToken).ConfigureAwait(false);
                    return;
                case DTCMessageType.ExchangeListResponse:
                    var exchangeListResponse = message as ExchangeListResponse;
                    await WriteExchangeListResponseAsync(messageType, exchangeListResponse, cancellationToken).ConfigureAwait(false);
                    return;
                case DTCMessageType.SecurityDefinitionForSymbolRequest:
                    var securityDefinitionForSymbolRequest = message as SecurityDefinitionForSymbolRequest;
                    await WriteSecurityDefinitionForSymbolRequestAsync(messageType, securityDefinitionForSymbolRequest, cancellationToken)
                        .ConfigureAwait(false);
                    return;
                case DTCMessageType.SecurityDefinitionResponse:
                    var securityDefinitionResponse = message as SecurityDefinitionResponse;
                    await WriteSecurityDefinitionResponseAsync(messageType, securityDefinitionResponse, cancellationToken).ConfigureAwait(false);
                    return;
                case DTCMessageType.SecurityDefinitionReject:
                    var securityDefinitionReject = message as SecurityDefinitionReject;
                    await WriteSecurityDefinitionRejectAsync(messageType, securityDefinitionReject, cancellationToken).ConfigureAwait(false);
                    return;
                case DTCMessageType.HistoricalPriceDataRequest:
                    var historicalPriceDataRequest = message as HistoricalPriceDataRequest;
                    await WriteHistoricalPriceDataRequestAsync(messageType, historicalPriceDataRequest, cancellationToken).ConfigureAwait(false);
                    return;
                case DTCMessageType.HistoricalPriceDataResponseHeader:
                    // Logger.Debug($"{nameof(CodecBinary)} is writing {messageType} {message}");
                    var historicalPriceDataResponseHeader = message as HistoricalPriceDataResponseHeader;
                    await WriteHistoricalPriceDataResponseHeaderAsync(messageType, historicalPriceDataResponseHeader, cancellationToken).ConfigureAwait(false);
                    return;
                case DTCMessageType.HistoricalPriceDataReject:
                    var historicalPriceDataReject = message as HistoricalPriceDataReject;
                    await WriteHistoricalPriceDataRejectAsync(messageType, historicalPriceDataReject, cancellationToken).ConfigureAwait(false);
                    return;
                case DTCMessageType.HistoricalPriceDataRecordResponse:
                    var historicalPriceDataRecordResponse = message as HistoricalPriceDataRecordResponse;
                    await WriteHistoricalPriceDataRecordResponseAsync(messageType, historicalPriceDataRecordResponse, cancellationToken).ConfigureAwait(false);
                    return;
                case DTCMessageType.HistoricalPriceDataTickRecordResponse:
                    // Probably no longer used after version SierraChart version 1150 per https://www.sierrachart.com/index.php?page=doc/IntradayDataFileFormat.html
                    throw new NotSupportedException($"Not implemented in {nameof(CodecBinary)}: {messageType}");
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
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(WriteAsync)}: {messageType}");
                default:
                    throw new ArgumentOutOfRangeException(messageType.ToString(), messageType, null);
            }
        }

        private async Task WriteHistoricalPriceDataRecordResponseAsync(DTCMessageType messageType,
            HistoricalPriceDataRecordResponse historicalPriceDataRecordResponse, CancellationToken cancellationToken)
        {
            const int sizeExcludingHeader = 4 + 9 * 8 + 1;
            const int size = sizeExcludingHeader + 4;
            using var bufferBuilder = new BufferBuilder(size, this);
            bufferBuilder.AddHeader(messageType);

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
            await bufferBuilder.WriteAsync(_stream, cancellationToken).ConfigureAwait(false);
        }

        private async Task WriteHistoricalPriceDataRejectAsync(DTCMessageType messageType, HistoricalPriceDataReject historicalPriceDataReject,
            CancellationToken cancellationToken)
        {
            const int sizeExcludingHeader = 4 + TEXT_DESCRIPTION_LENGTH + 2 + 2;
            const int size = sizeExcludingHeader + 4;
            using var bufferBuilder = new BufferBuilder(size, this);
            bufferBuilder.AddHeader(messageType);

            bufferBuilder.Add(historicalPriceDataReject.RequestID);
            bufferBuilder.Add(historicalPriceDataReject.RejectText.ToFixedBytes(TEXT_DESCRIPTION_LENGTH));
            bufferBuilder.Add((short)historicalPriceDataReject.RejectReasonCode);
            bufferBuilder.Add((ushort)historicalPriceDataReject.RetryTimeInSeconds);
            await bufferBuilder.WriteAsync(_stream, cancellationToken).ConfigureAwait(false);
        }

        private async Task WriteHistoricalPriceDataResponseHeaderAsync(DTCMessageType messageType,
            HistoricalPriceDataResponseHeader historicalPriceDataResponseHeader, CancellationToken cancellationToken)
        {
            const int sizeExcludingHeader = 4 + 4 + 2 + 2 + 4;
            const int size = sizeExcludingHeader + 4;
            using var bufferBuilder = new BufferBuilder(size, this);
            bufferBuilder.AddHeader(messageType);

            bufferBuilder.Add(historicalPriceDataResponseHeader.RequestID);
            bufferBuilder.Add((int)historicalPriceDataResponseHeader.RecordInterval);
            bufferBuilder.Add((byte)historicalPriceDataResponseHeader.UseZLibCompression);
            bufferBuilder.Add((byte)historicalPriceDataResponseHeader.NoRecordsToReturn);
            bufferBuilder.Add((short)0); // align for packing
            bufferBuilder.Add(historicalPriceDataResponseHeader.IntToFloatPriceDivisor);
            await bufferBuilder.WriteAsync(_stream, cancellationToken).ConfigureAwait(false);
        }

        private async Task WriteHistoricalPriceDataRequestAsync(DTCMessageType messageType, HistoricalPriceDataRequest historicalPriceDataRequest,
            CancellationToken cancellationToken)
        {
            const int sizeExcludingHeader = 4 + SYMBOL_LENGTH + EXCHANGE_LENGTH + 4 + 4 + 8 + 8 + 4 + 3 * 1;
            const int size = sizeExcludingHeader + 4;
            using var bufferBuilder = new BufferBuilder(size, this);
            bufferBuilder.AddHeader(messageType);

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
            await bufferBuilder.WriteAsync(_stream, cancellationToken).ConfigureAwait(false);
        }

        private async Task WriteSecurityDefinitionRejectAsync(DTCMessageType messageType, SecurityDefinitionReject securityDefinitionReject,
            CancellationToken cancellationToken)
        {
            const int sizeExcludingHeader = 4 + TEXT_DESCRIPTION_LENGTH;
            const int size = sizeExcludingHeader + 4;
            using var bufferBuilder = new BufferBuilder(size, this);
            bufferBuilder.AddHeader(messageType);

            bufferBuilder.Add(securityDefinitionReject.RequestID);
            bufferBuilder.Add(securityDefinitionReject.RejectText.ToFixedBytes(TEXT_DESCRIPTION_LENGTH));
            await bufferBuilder.WriteAsync(_stream, cancellationToken).ConfigureAwait(false);
        }

        private async Task WriteSecurityDefinitionResponseAsync(DTCMessageType messageType, SecurityDefinitionResponse securityDefinitionResponse,
            CancellationToken cancellationToken)
        {
            const int sizeExcludingHeader =
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
            const int size = sizeExcludingHeader + 4;
            using var bufferBuilder = new BufferBuilder(size, this);
            bufferBuilder.AddHeader(messageType);

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
            await bufferBuilder.WriteAsync(_stream, cancellationToken).ConfigureAwait(false);
        }

        private async Task WriteSecurityDefinitionForSymbolRequestAsync(DTCMessageType messageType,
            SecurityDefinitionForSymbolRequest securityDefinitionForSymbolRequest, CancellationToken cancellationToken)
        {
            const int sizeExcludingHeader = 4 + SYMBOL_LENGTH + EXCHANGE_LENGTH;
            const int size = sizeExcludingHeader + 4;
            using var bufferBuilder = new BufferBuilder(size, this);
            bufferBuilder.AddHeader(messageType);

            bufferBuilder.Add(securityDefinitionForSymbolRequest.RequestID);
            bufferBuilder.Add(securityDefinitionForSymbolRequest.Symbol.ToFixedBytes(SYMBOL_LENGTH));
            bufferBuilder.Add(securityDefinitionForSymbolRequest.Exchange.ToFixedBytes(EXCHANGE_LENGTH));
            await bufferBuilder.WriteAsync(_stream, cancellationToken).ConfigureAwait(false);
        }

        private async Task WriteExchangeListResponseAsync(DTCMessageType messageType, ExchangeListResponse exchangeListResponse,
            CancellationToken cancellationToken)
        {
            const int sizeExcludingHeader = 4 + EXCHANGE_LENGTH + 1 + EXCHANGE_DESCRIPTION_LENGTH;
            const int size = sizeExcludingHeader + 4;
            using var bufferBuilder = new BufferBuilder(size, this);
            bufferBuilder.AddHeader(messageType);

            bufferBuilder.Add(exchangeListResponse.RequestID);
            bufferBuilder.Add(exchangeListResponse.Exchange.ToFixedBytes(EXCHANGE_LENGTH));
            bufferBuilder.Add((byte)exchangeListResponse.IsFinalMessage);
            bufferBuilder.Add(exchangeListResponse.Description.ToFixedBytes(EXCHANGE_DESCRIPTION_LENGTH));
            await bufferBuilder.WriteAsync(_stream, cancellationToken).ConfigureAwait(false);
        }

        private async Task WriteExchangeListRequestAsync(DTCMessageType messageType, ExchangeListRequest exchangeListRequest,
            CancellationToken cancellationToken)
        {
            const int sizeExcludingHeader = 4;
            const int size = sizeExcludingHeader + 4;
            using var bufferBuilder = new BufferBuilder(size, this);
            bufferBuilder.AddHeader(messageType);

            bufferBuilder.Add(exchangeListRequest.RequestID);
            await bufferBuilder.WriteAsync(_stream, cancellationToken).ConfigureAwait(false);
        }

        private async Task WriteMarketDataFeedStatusAsync(DTCMessageType messageType, MarketDataFeedStatus marketDataFeedStatus,
            CancellationToken cancellationToken)
        {
            const int sizeExcludingHeader = 4;
            const int size = sizeExcludingHeader + 4;
            using var bufferBuilder = new BufferBuilder(size, this);
            bufferBuilder.AddHeader(messageType);

            bufferBuilder.Add((int)marketDataFeedStatus.Status);
            await bufferBuilder.WriteAsync(_stream, cancellationToken).ConfigureAwait(false);
        }

        private async Task WriteMarketDataRejectAsync(DTCMessageType messageType, MarketDataReject marketDataReject, CancellationToken cancellationToken)
        {
            const int sizeExcludingHeader = 2 + TEXT_DESCRIPTION_LENGTH;
            const int size = sizeExcludingHeader + 4;
            using var bufferBuilder = new BufferBuilder(size, this);
            bufferBuilder.AddHeader(messageType);

            bufferBuilder.Add((ushort)marketDataReject.SymbolID);
            bufferBuilder.Add(marketDataReject.RejectText.ToFixedBytes(TEXT_DESCRIPTION_LENGTH));
            await bufferBuilder.WriteAsync(_stream, cancellationToken).ConfigureAwait(false);
        }

        private async Task WriteMarketDataRequestAsync(DTCMessageType messageType, MarketDataRequest marketDataRequest, CancellationToken cancellationToken)
        {
            const int sizeExcludingHeader = 4 + 2 + SYMBOL_LENGTH + EXCHANGE_LENGTH;
            const int size = sizeExcludingHeader + 4;
            using var bufferBuilder = new BufferBuilder(size, this);
            bufferBuilder.AddHeader(messageType);

            bufferBuilder.Add((int)marketDataRequest.RequestAction);
            bufferBuilder.Add((ushort)marketDataRequest.SymbolID);
            bufferBuilder.Add(marketDataRequest.Symbol.ToFixedBytes(SYMBOL_LENGTH));
            bufferBuilder.Add(marketDataRequest.Exchange.ToFixedBytes(EXCHANGE_LENGTH));
            await bufferBuilder.WriteAsync(_stream, cancellationToken).ConfigureAwait(false);
        }

        private async Task WriteLogoffAsync<T>(DTCMessageType messageType, Logoff logoff, CancellationToken cancellationToken)
        {
            try
            {
                const int sizeExcludingHeader = TEXT_DESCRIPTION_LENGTH + 1;
                const int size = sizeExcludingHeader + 4;
                using var bufferBuilder = new BufferBuilder(size, this);
                bufferBuilder.AddHeader(messageType);

                bufferBuilder.Add(logoff.Reason.ToFixedBytes(TEXT_DESCRIPTION_LENGTH));
                bufferBuilder.Add((byte)logoff.DoNotReconnect);
                await bufferBuilder.WriteAsync(_stream, cancellationToken).ConfigureAwait(false);
            }
            catch (IOException ex)
            {
                // Ignore this exception, which happens on Dispose() if the stream has already gone away, as when the ClientHandler finishes sending zipped historical records 
                var tmp = ex;
            }
        }

        private async Task WriteHeartbeatAsync<T>(DTCMessageType messageType, Heartbeat heartbeat, CancellationToken cancellationToken)
        {
            const int size = 16;
            using var bufferBuilder = new BufferBuilder(size, this);
            bufferBuilder.AddHeader(messageType);

            bufferBuilder.Add(heartbeat.NumDroppedMessages);
            bufferBuilder.Add(heartbeat.CurrentDateTime);
            await bufferBuilder.WriteAsync(_stream, cancellationToken).ConfigureAwait(false);
        }

        private async Task WriteLogonResponseAsync<T>(DTCMessageType messageType, LogonResponse logonResponse, CancellationToken cancellationToken)
        {
            const int sizeExcludingHeader = 4 + 4 + TEXT_DESCRIPTION_LENGTH + 64 + 4 + 60 + 4 * 1 + SYMBOL_EXCHANGE_DELIMITER_LENGTH + 8 * 1 + 4;
            const int size = sizeExcludingHeader + 4;
            using var bufferBuilder = new BufferBuilder(size, this);
            bufferBuilder.AddHeader(messageType);

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
            bufferBuilder.Add((byte)logonResponse.UseIntegerPriceOrderMessages);
            bufferBuilder.Add((byte)logonResponse.UsesMultiplePositionsPerSymbolAndTradeAccount);
            bufferBuilder.Add(logonResponse.MarketDataSupported);
            await bufferBuilder.WriteAsync(_stream, cancellationToken).ConfigureAwait(false);
        }

        private async Task WriteLogonRequestAsync<T>(DTCMessageType messageType, LogonRequest logonRequest, CancellationToken cancellationToken)
        {
            const int sizeExcludingHeader = 4
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
            const int size = sizeExcludingHeader + 4;
            using var bufferBuilder = new BufferBuilder(size, this);
            bufferBuilder.AddHeader(messageType);

            bufferBuilder.Add(logonRequest.ProtocolVersion);
            bufferBuilder.Add(logonRequest.Username.ToFixedBytes(USERNAME_PASSWORD_LENGTH));
            bufferBuilder.Add(logonRequest.Password.ToFixedBytes(USERNAME_PASSWORD_LENGTH));
            bufferBuilder.Add(logonRequest.GeneralTextData.ToFixedBytes(GENERAL_IDENTIFIER_LENGTH));
            bufferBuilder.Add(logonRequest.Integer1);
            bufferBuilder.Add(logonRequest.Integer2);
            bufferBuilder.Add(logonRequest.HeartbeatIntervalInSeconds);
            bufferBuilder.Add((int)logonRequest.TradeMode);
            bufferBuilder.Add(logonRequest.TradeAccount.ToFixedBytes(TRADE_ACCOUNT_LENGTH));
            bufferBuilder.Add(logonRequest.HardwareIdentifier.ToFixedBytes(GENERAL_IDENTIFIER_LENGTH));
            bufferBuilder.Add(logonRequest.ClientName.ToFixedBytes(32));
            await bufferBuilder.WriteAsync(_stream, cancellationToken).ConfigureAwait(false);
        }

        public override T Load<T>(DTCMessageType messageType, byte[] bytes)
        {
            var index = 0;
            var result = new T();
            switch (messageType)
            {
                case DTCMessageType.MessageTypeUnset:
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}: {messageType}");
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
                    // EncodingResponse comes back as binary for all protocol versions
                    LoadEncodingRequest(bytes, index, ref result);
                    return result;
                case DTCMessageType.EncodingResponse:
                    // EncodingResponse comes back as binary for all protocol versions
                    LoadEncodingResponse(bytes, index, ref result);
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

                    //Logger.Debug($"{nameof(CodecBinary)} loaded {messageType} {result}");
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
                    if (historicalPriceDataRecordResponse.Volume == 0)
                    {
                    }
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
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}");
                case DTCMessageType.HistoricalMarketDepthDataRecordResponse:
                    // Probably no longer used after version SierraChart version 1150 per https://www.sierrachart.com/index.php?page=doc/IntradayDataFileFormat.html
                    throw new NotImplementedException($"Not implemented in {nameof(CodecBinary)}.{nameof(Load)}: {messageType}");
                default:
                    throw new ArgumentOutOfRangeException(messageType.ToString(), messageType, null);
            }
        }
    }
}