using System;
using System.IO.Compression;
using DTCCommon;
using DTCCommon.Codecs;
using DTCPB;
using Google.Protobuf;

namespace DTCClient;

/// <summary>
///     This partial class focuses on processing responses and sending events in <see cref="ClientDTC" />
/// </summary>
public partial class ClientDTC
{
    /// <summary>
    ///     Process messageProto if it should be handled immediately instead of adding it to the queue to process later.
    ///     We don't want to use the queue until a successful logon in the requested encoding.
    ///     And we don't want heartbeats to be delayed until all responses on the queue have been processed.
    /// </summary>
    /// <param name="messageProto"></param>
    /// <returns><c>true</c> if handled</returns>
    private bool PreProcessResponse(MessageProto messageProto)
    {
        if (messageProto.IsExtended)
        {
            // We don't preProcess any extended message
            return false;
        }

        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (messageProto.MessageType)
        {
            case DTCMessageType.Heartbeat:
                // We don't process this message. ResponseReader records the last message received,
                //  whether a heartbeat or any other record, as required while reading historical records without intermingled heartbeats
                //s_logger.Debug($"Heartbeat received from DTC in {this}");
                OnEveryResponse(messageProto.Message);
                OnIMessage((Heartbeat)messageProto.Message, HeartbeatEvent);
                return true;
            case DTCMessageType.EncodingResponse:
                // Note that we must use binary encoding here on the FIRST usage after connect, 
                //    per http://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#EncodingRequest
                var encodingResponse = (EncodingResponse)messageProto.Message;
                HandleEncodingResponse(encodingResponse);
                return true;
            case DTCMessageType.HistoricalPriceDataResponseHeader:
                // We must process this here instead of adding it to the queue, otherwise the next message is read from the unzipped stream before we know about it
                var historicalPriceDataResponseHeader = (HistoricalPriceDataResponseHeader)messageProto.Message;
                OnEveryResponse(messageProto.Message);
                OnIMessage(historicalPriceDataResponseHeader, HistoricalPriceDataResponseHeaderEvent);
                if (historicalPriceDataResponseHeader.UseZLibCompressionBool)
                {
                    SwitchCurrentStreamToZipped();
                }
                return true;
            case DTCMessageType.HistoricalPriceDataRecordResponse:
                var historicalPriceDataRecordResponse = (HistoricalPriceDataRecordResponse)messageProto.Message;
                OnEveryResponse(messageProto.Message);
                OnIMessage(historicalPriceDataRecordResponse, HistoricalPriceDataRecordResponseEvent);
                if (historicalPriceDataRecordResponse.IsFinalRecordBool)
                {
                    EndZippedHistorical();
                }
                return true;
            case DTCMessageType.HistoricalPriceDataTickRecordResponse:
                var historicalPriceDataTickRecordResponse = (HistoricalPriceDataTickRecordResponse)messageProto.Message;
                OnEveryResponse(messageProto.Message);
                OnIMessage(historicalPriceDataTickRecordResponse, HistoricalPriceDataTickRecordResponseEvent);
                if (historicalPriceDataTickRecordResponse.IsFinalRecordBool)
                {
                    EndZippedHistorical();
                }
                return true;
            case DTCMessageType.HistoricalPriceDataRecordResponseInt:
                var historicalPriceDataRecordResponseInt = (HistoricalPriceDataRecordResponse_Int)messageProto.Message;
                OnEveryResponse(messageProto.Message);
                OnIMessage(historicalPriceDataRecordResponseInt, HistoricalPriceDataRecordResponseIntEvent);
                if (historicalPriceDataRecordResponseInt.IsFinalRecord != 0)
                {
                    EndZippedHistorical();
                }
                return true;
            case DTCMessageType.HistoricalPriceDataTickRecordResponseInt:
                var historicalPriceDataTickRecordResponseInt = (HistoricalPriceDataTickRecordResponse_Int)messageProto.Message;
                OnEveryResponse(messageProto.Message);
                OnIMessage((HistoricalPriceDataTickRecordResponse_Int)messageProto.Message, HistoricalPriceDataTickRecordResponseIntEvent);
                if (historicalPriceDataTickRecordResponseInt.IsFinalRecord != 0)
                {
                    EndZippedHistorical();
                }
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    ///     Process the message.
    ///     binaryReader may be changed to use a new DeflateStream if we change to zipped
    /// </summary>
    /// <param name="messageProto"></param>
    private void ProcessResponse(MessageProto messageProto)
    {
        OnEveryResponse(messageProto.Message);
        if (messageProto.IsExtended)
        {
            ProcessResponseExtended(messageProto);
            return;
        }
        switch (messageProto.MessageType)
        {
            case DTCMessageType.Heartbeat:
            case DTCMessageType.EncodingRequest:
            case DTCMessageType.HistoricalPriceDataResponseHeader:
            case DTCMessageType.HistoricalPriceDataRecordResponse:
            case DTCMessageType.HistoricalPriceDataTickRecordResponse:
            case DTCMessageType.HistoricalPriceDataRecordResponseInt:
            case DTCMessageType.HistoricalPriceDataTickRecordResponseInt:
                throw new InvalidOperationException("This message type should have been handled by PreProcessResponse");
            case DTCMessageType.LogonResponse:
                OnIMessage((LogonResponse)messageProto.Message, LogonResponseEvent);
                LogonResponse = (LogonResponse)messageProto.Message;
                break;
            case DTCMessageType.HistoricalPriceDataReject:
                var historicalPriceDataReject = (HistoricalPriceDataReject)messageProto.Message;
                OnIMessage(historicalPriceDataReject, HistoricalPriceDataRejectEvent);
                break;
            case DTCMessageType.SecurityDefinitionResponse:
                var securityDefinitionResponse = (SecurityDefinitionResponse)messageProto.Message;
                OnIMessage(securityDefinitionResponse, SecurityDefinitionResponseEvent);

                // SC throws a bunch of these on connection, so save them in case they are requested later
                lock (s_securityDefinitionsBySymbol)
                {
                    if (!s_securityDefinitionsBySymbol.ContainsKey(securityDefinitionResponse.Symbol))
                    {
                        s_securityDefinitionsBySymbol.Add(securityDefinitionResponse.Symbol, securityDefinitionResponse);
                    }
                }
                break;
            case DTCMessageType.Logoff:
                OnIMessage((Logoff)messageProto.Message, LogoffEvent);
                Dispose();
                break;
            case DTCMessageType.MarketDataReject:
                OnIMessage((MarketDataReject)messageProto.Message, MarketDataRejectEvent);
                break;
            case DTCMessageType.MarketDataSnapshot:
                OnIMessage((MarketDataSnapshot)messageProto.Message, MarketDataSnapshotEvent);
                break;
            case DTCMessageType.MarketDataSnapshotInt:
                OnIMessage((MarketDataSnapshot_Int)messageProto.Message, MarketDataSnapshotIntEvent);
                break;
            case DTCMessageType.MarketDataUpdateTrade:
                OnIMessage((MarketDataUpdateTrade)messageProto.Message, MarketDataUpdateTradeEvent);
                break;
            case DTCMessageType.MarketDataUpdateTradeCompact:
                OnIMessage((MarketDataUpdateTradeCompact)messageProto.Message, MarketDataUpdateTradeCompactEvent);
                break;
            case DTCMessageType.MarketDataUpdateTradeInt:
                OnIMessage((MarketDataUpdateTrade_Int)messageProto.Message, MarketDataUpdateTradeIntEvent);
                break;
            case DTCMessageType.MarketDataUpdateLastTradeSnapshot:
                OnIMessage((MarketDataUpdateLastTradeSnapshot)messageProto.Message, MarketDataUpdateLastTradeSnapshotEvent);
                break;
            case DTCMessageType.MarketDataUpdateBidAsk:
                OnIMessage((MarketDataUpdateBidAsk)messageProto.Message, MarketDataUpdateBidAskEvent);
                break;
            case DTCMessageType.MarketDataUpdateBidAskCompact:
                OnIMessage((MarketDataUpdateBidAskCompact)messageProto.Message, MarketDataUpdateBidAskCompactEvent);
                break;
            case DTCMessageType.MarketDataUpdateBidAskInt:
                OnIMessage((MarketDataUpdateBidAsk_Int)messageProto.Message, MarketDataUpdateBidAskIntEvent);
                break;
            case DTCMessageType.MarketDataUpdateSessionOpen:
                OnIMessage((MarketDataUpdateSessionOpen)messageProto.Message, MarketDataUpdateSessionOpenEvent);
                break;
            case DTCMessageType.MarketDataUpdateSessionOpenInt:
                OnIMessage((MarketDataUpdateSessionOpen_Int)messageProto.Message, MarketDataUpdateSessionOpenIntEvent);
                break;
            case DTCMessageType.MarketDataUpdateSessionHigh:
                OnIMessage((MarketDataUpdateSessionHigh)messageProto.Message, MarketDataUpdateSessionHighEvent);
                break;
            case DTCMessageType.MarketDataUpdateSessionHighInt:
                OnIMessage((MarketDataUpdateSessionHigh_Int)messageProto.Message, MarketDataUpdateSessionHighIntEvent);
                break;
            case DTCMessageType.MarketDataUpdateSessionLow:
                OnIMessage((MarketDataUpdateSessionLow)messageProto.Message, MarketDataUpdateSessionLowEvent);
                break;
            case DTCMessageType.MarketDataUpdateSessionLowInt:
                OnIMessage((MarketDataUpdateSessionLow_Int)messageProto.Message, MarketDataUpdateSessionLowIntEvent);
                break;
            case DTCMessageType.MarketDataUpdateSessionVolume:
                OnIMessage((MarketDataUpdateSessionVolume)messageProto.Message, MarketDataUpdateSessionVolumeEvent);
                break;
            case DTCMessageType.MarketDataUpdateOpenInterest:
                OnIMessage((MarketDataUpdateOpenInterest)messageProto.Message, MarketDataUpdateOpenInterestEvent);
                break;
            case DTCMessageType.MarketDataUpdateSessionSettlement:
                OnIMessage((MarketDataUpdateSessionSettlement)messageProto.Message, MarketDataUpdateSessionSettlementEvent);
                break;
            case DTCMessageType.MarketDataUpdateSessionSettlementInt:
                OnIMessage((MarketDataUpdateSessionSettlement_Int)messageProto.Message, MarketDataUpdateSessionSettlementIntEvent);
                break;
            case DTCMessageType.MarketDataUpdateSessionNumTrades:
                OnIMessage((MarketDataUpdateSessionNumTrades)messageProto.Message, MarketDataUpdateSessionNumTradesEvent);
                break;
            case DTCMessageType.MarketDataUpdateTradingSessionDate:
                OnIMessage((MarketDataUpdateTradingSessionDate)messageProto.Message, MarketDataUpdateTradingSessionDateEvent);
                break;
            case DTCMessageType.MarketDepthReject:
                OnIMessage((MarketDepthReject)messageProto.Message, MarketDepthRejectEvent);
                break;
            case DTCMessageType.MarketDepthSnapshotLevel:
                OnIMessage((MarketDepthSnapshotLevel)messageProto.Message, MarketDepthSnapshotLevelEvent);
                break;
            case DTCMessageType.MarketDepthSnapshotLevelInt:
                OnIMessage((MarketDepthSnapshotLevel_Int)messageProto.Message, MarketDepthSnapshotLevelIntEvent);
                break;
            case DTCMessageType.MarketDepthUpdateLevel:
                OnIMessage((MarketDepthUpdateLevel)messageProto.Message, MarketDepthUpdateLevelEvent);
                break;
            case DTCMessageType.MarketDepthUpdateLevelInt:
                OnIMessage((MarketDepthUpdateLevel_Int)messageProto.Message, MarketDepthUpdateLevelIntEvent);
                break;
            case DTCMessageType.MarketDataFeedStatus:
                OnIMessage((MarketDataFeedStatus)messageProto.Message, MarketDataFeedStatusEvent);
                break;
            case DTCMessageType.MarketDataFeedSymbolStatus:
                OnIMessage((MarketDataFeedSymbolStatus)messageProto.Message, MarketDataFeedSymbolStatusEvent);
                break;
            case DTCMessageType.OpenOrdersReject:
                OnIMessage((OpenOrdersReject)messageProto.Message, OpenOrdersRejectEvent);
                break;
            case DTCMessageType.OrderUpdate:
                OnIMessage((OrderUpdate)messageProto.Message, OrderUpdateEvent);
                break;
            case DTCMessageType.HistoricalOrderFillResponse:
                OnIMessage((HistoricalOrderFillResponse)messageProto.Message, HistoricalOrderFillResponseEvent);
                break;
            case DTCMessageType.CurrentPositionsReject:
                OnIMessage((CurrentPositionsReject)messageProto.Message, CurrentPositionsRejectEvent);
                break;
            case DTCMessageType.PositionUpdate:
                OnIMessage((PositionUpdate)messageProto.Message, PositionUpdateEvent);
                break;
            case DTCMessageType.TradeAccountResponse:
                OnIMessage((TradeAccountResponse)messageProto.Message, TradeAccountResponseEvent);
                break;
            case DTCMessageType.ExchangeListResponse:
                OnIMessage((ExchangeListResponse)messageProto.Message, ExchangeListResponseEvent);
                break;
            case DTCMessageType.SecurityDefinitionReject:
                OnIMessage((SecurityDefinitionReject)messageProto.Message, SecurityDefinitionRejectEvent);
                break;
            case DTCMessageType.AccountBalanceReject:
                OnIMessage((AccountBalanceReject)messageProto.Message, AccountBalanceRejectEvent);
                break;
            case DTCMessageType.AccountBalanceUpdate:
                OnIMessage((AccountBalanceUpdate)messageProto.Message, AccountBalanceUpdateEvent);
                break;
            case DTCMessageType.UserMessage:
                OnIMessage((UserMessage)messageProto.Message, UserMessageEvent);
                break;
            case DTCMessageType.GeneralLogMessage:
                OnIMessage((GeneralLogMessage)messageProto.Message, GeneralLogMessageEvent);
                break;
            case DTCMessageType.TradingSymbolStatus:
                OnIMessage((TradingSymbolStatus)messageProto.Message, TradingSymbolStatusEvent);
                break;
            case DTCMessageType.MarketDataUpdateTradeWithUnbundledIndicator:
                OnIMessage((MarketDataUpdateTradeWithUnbundledIndicator)messageProto.Message, MarketDataUpdateTradeWithUnbundledIndicatorEvent);
                break;
            case DTCMessageType.MarketDataUpdateTradeWithUnbundledIndicator2:
                OnIMessage((MarketDataUpdateTradeWithUnbundledIndicator2)messageProto.Message, MarketDataUpdateTradeWithUnbundledIndicator2Event);
                break;
            case DTCMessageType.MarketDataUpdateTradeNoTimestamp:
                OnIMessage((MarketDataUpdateTradeNoTimestamp)messageProto.Message, MarketDataUpdateTradeNoTimestampEvent);
                break;
            case DTCMessageType.MarketDataUpdateBidAskNoTimestamp:
                OnIMessage((MarketDataUpdateBidAskNoTimeStamp)messageProto.Message, MarketDataUpdateBidAskNoTimestampEvent);
                break;
            case DTCMessageType.MarketDepthSnapshotLevelFloat:
                OnIMessage((MarketDepthSnapshotLevelFloat)messageProto.Message, MarketDepthSnapshotLevelFloatEvent);
                break;
            case DTCMessageType.MarketDepthUpdateLevelFloatWithMilliseconds:
                OnIMessage((MarketDepthUpdateLevelFloatWithMilliseconds)messageProto.Message, MarketDepthUpdateLevelFloatWithMillisecondsEvent);
                break;
            case DTCMessageType.MarketDepthUpdateLevelNoTimestamp:
                OnIMessage((MarketDepthUpdateLevelNoTimestamp)messageProto.Message, MarketDepthUpdateLevelNoTimestampEvent);
                break;
            case DTCMessageType.SubmitNewSingleOrder:
                OnIMessage((SubmitNewSingleOrder)messageProto.Message, SubmitNewSingleOrderEvent);
                break;
            case DTCMessageType.SubmitNewSingleOrderInt:
                OnIMessage((SubmitNewSingleOrderInt)messageProto.Message, SubmitNewSingleOrderIntEvent);
                break;
            case DTCMessageType.SubmitNewOcoOrder:
                OnIMessage((SubmitNewOCOOrder)messageProto.Message, SubmitNewOcoOrderEvent);
                break;
            case DTCMessageType.SubmitNewOcoOrderInt:
                OnIMessage((SubmitNewOCOOrderInt)messageProto.Message, SubmitNewOcoOrderIntEvent);
                break;
            case DTCMessageType.SubmitFlattenPositionOrder:
                OnIMessage((SubmitFlattenPositionOrder)messageProto.Message, SubmitFlattenPositionOrderEvent);
                break;
            case DTCMessageType.CancelOrder:
                OnIMessage((CancelOrder)messageProto.Message, CancelOrderEvent);
                break;
            case DTCMessageType.CancelReplaceOrder:
                OnIMessage((CancelReplaceOrder)messageProto.Message, CancelReplaceOrderEvent);
                break;
            case DTCMessageType.CancelReplaceOrderInt:
                OnIMessage((CancelReplaceOrderInt)messageProto.Message, CancelReplaceOrderIntEvent);
                break;
            case DTCMessageType.HistoricalOrderFillsReject:
                OnIMessage((HistoricalOrderFillsReject)messageProto.Message, HistoricalOrderFillsRejectEvent);
                break;
            case DTCMessageType.AccountBalanceAdjustmentReject:
                OnIMessage((AccountBalanceAdjustmentReject)messageProto.Message, AccountBalanceAdjustmentRejectEvent);
                break;
            case DTCMessageType.AccountBalanceAdjustmentComplete:
                OnIMessage((AccountBalanceAdjustmentComplete)messageProto.Message, AccountBalanceAdjustmentCompleteEvent);
                break;
            case DTCMessageType.HistoricalAccountBalancesReject:
                OnIMessage((HistoricalAccountBalancesReject)messageProto.Message, HistoricalAccountBalancesRejectEvent);
                break;
            case DTCMessageType.HistoricalAccountBalanceResponse:
                OnIMessage((HistoricalAccountBalanceResponse)messageProto.Message, HistoricalAccountBalanceResponseEvent);
                break;
            case DTCMessageType.AlertMessage:
                OnIMessage((AlertMessage)messageProto.Message, AlertMessageEvent);
                break;
            case DTCMessageType.JournalEntryAdd:
                OnIMessage((JournalEntryAdd)messageProto.Message, JournalEntryAddEvent);
                break;
            case DTCMessageType.JournalEntriesReject:
                OnIMessage((JournalEntriesReject)messageProto.Message, JournalEntriesRejectEvent);
                break;
            case DTCMessageType.JournalEntryResponse:
                OnIMessage((JournalEntryResponse)messageProto.Message, JournalEntryResponseEvent);
                break;
            case DTCMessageType.HistoricalPriceDataResponseTrailer:
                OnIMessage((HistoricalPriceDataResponseTrailer)messageProto.Message, HistoricalPriceDataResponseTrailerEvent);
                break;
            case DTCMessageType.HistoricalMarketDepthDataResponseHeader:
                OnIMessage((HistoricalMarketDepthDataResponseHeader)messageProto.Message, HistoricalMarketDepthDataResponseHeaderEvent);
                break;
            case DTCMessageType.HistoricalMarketDepthDataReject:
                OnIMessage((HistoricalMarketDepthDataReject)messageProto.Message, HistoricalMarketDepthDataRejectEvent);
                break;
            case DTCMessageType.HistoricalMarketDepthDataRecordResponse:
                OnIMessage((HistoricalMarketDepthDataRecordResponse)messageProto.Message, HistoricalMarketDepthDataRecordResponseEvent);
                break;
            case DTCMessageType.MessageTypeUnset:
            default:
                throw new ArgumentOutOfRangeException($"Unexpected Message {messageProto} received by {this} {nameof(ProcessResponse)}.");
        }
    }

    private void HandleEncodingResponse(EncodingResponse encodingResponse)
    {
        OnIMessage(encodingResponse, EncodingResponseEvent);
        if (encodingResponse.Encoding != _currentEncoding)
        {
            switch (encodingResponse.Encoding)
            {
                case EncodingEnum.BinaryEncoding:
                    _encode = CodecBinaryConverter.EncodeBinary;
                    _decode = CodecBinaryConverter.DecodeBinary;
                    s_logger.Verbose("Changed codec from {_currentEncoding} to {Encoding} in {ClientDTC}", _currentEncoding, encodingResponse.Encoding, this);
                    break;
                case EncodingEnum.BinaryWithVariableLengthStrings:
                case EncodingEnum.JsonEncoding:
                case EncodingEnum.JsonCompactEncoding:
                    throw new NotImplementedException("Not supported yet");
                case EncodingEnum.ProtocolBuffers:
                    _encode = CodecProtobufConverter.EncodeProtobuf;
                    _decode = CodecProtobufConverter.DecodeProtobuf;
                    s_logger.Verbose("Changed codec from {_currentEncoding} to {Encoding} in {ClientDTC}", _currentEncoding, encodingResponse.Encoding, this);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        _currentEncoding = encodingResponse.Encoding;
    }

    private void ProcessResponseExtended(MessageProto messageProto)
    {
        switch (messageProto.MessageTypeExtended)
        {
            case DTCSharpMessageType.Unset:
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void EndZippedHistorical()
    {
        if (_currentStream.GetType() != typeof(DeflateStream))
        {
            return;
        }

        // Close the stream to flush the deflateStream
        _currentStream.Close();

        // Go back to the NetworkStream
        _currentStream = GetStream();
        //s_logger.Debug($"Ended zipped historical {this} ");
    }

    private void SwitchCurrentStreamToZipped()
    {
        _currentStream.ReadPastZipHeader();
        try
        {
            // Leave the network stream open, but wrap it with DeflateStream so future reads and writes are zipped.
            _currentStream = new DeflateStream(_currentStream, CompressionMode.Decompress, true);
            s_logger.Debug("{ClientDTC} switched stream to read zipped.", this);
        }
        catch (Exception ex)
        {
            s_logger.Error(ex, "{Message} in {ClientDTC}", ex.Message, this);
            Dispose();
        }
    }

    public event EventHandler<Heartbeat> HeartbeatEvent;
    public event EventHandler<Logoff> LogoffEvent;
    public event EventHandler<EncodingResponse> EncodingResponseEvent;
    public event EventHandler<LogonResponse> LogonResponseEvent;
    public event EventHandler<MarketDataReject> MarketDataRejectEvent;
    public event EventHandler<MarketDataSnapshot> MarketDataSnapshotEvent;
    public event EventHandler<MarketDataSnapshot_Int> MarketDataSnapshotIntEvent;
    public event EventHandler<MarketDataUpdateTrade> MarketDataUpdateTradeEvent;
    public event EventHandler<MarketDataUpdateTradeCompact> MarketDataUpdateTradeCompactEvent;
    public event EventHandler<MarketDataUpdateTrade_Int> MarketDataUpdateTradeIntEvent;
    public event EventHandler<MarketDataUpdateLastTradeSnapshot> MarketDataUpdateLastTradeSnapshotEvent;
    public event EventHandler<MarketDataUpdateBidAsk> MarketDataUpdateBidAskEvent;
    public event EventHandler<MarketDataUpdateBidAskCompact> MarketDataUpdateBidAskCompactEvent;
    public event EventHandler<MarketDataUpdateBidAsk_Int> MarketDataUpdateBidAskIntEvent;
    public event EventHandler<MarketDataUpdateSessionOpen> MarketDataUpdateSessionOpenEvent;
    public event EventHandler<MarketDataUpdateSessionOpen_Int> MarketDataUpdateSessionOpenIntEvent;
    public event EventHandler<MarketDataUpdateSessionHigh> MarketDataUpdateSessionHighEvent;
    public event EventHandler<MarketDataUpdateSessionHigh_Int> MarketDataUpdateSessionHighIntEvent;
    public event EventHandler<MarketDataUpdateSessionLow> MarketDataUpdateSessionLowEvent;
    public event EventHandler<MarketDataUpdateSessionLow_Int> MarketDataUpdateSessionLowIntEvent;
    public event EventHandler<MarketDataUpdateSessionVolume> MarketDataUpdateSessionVolumeEvent;
    public event EventHandler<MarketDataUpdateOpenInterest> MarketDataUpdateOpenInterestEvent;
    public event EventHandler<MarketDataUpdateSessionSettlement> MarketDataUpdateSessionSettlementEvent;
    public event EventHandler<MarketDataUpdateSessionSettlement_Int> MarketDataUpdateSessionSettlementIntEvent;
    public event EventHandler<MarketDataUpdateSessionNumTrades> MarketDataUpdateSessionNumTradesEvent;
    public event EventHandler<MarketDataUpdateTradingSessionDate> MarketDataUpdateTradingSessionDateEvent;
    public event EventHandler<MarketDepthReject> MarketDepthRejectEvent;
    public event EventHandler<MarketDepthSnapshotLevel> MarketDepthSnapshotLevelEvent;
    public event EventHandler<MarketDepthSnapshotLevel_Int> MarketDepthSnapshotLevelIntEvent;
    public event EventHandler<MarketDepthUpdateLevel> MarketDepthUpdateLevelEvent;
    public event EventHandler<MarketDepthUpdateLevel_Int> MarketDepthUpdateLevelIntEvent;
    public event EventHandler<MarketDataFeedStatus> MarketDataFeedStatusEvent;
    public event EventHandler<MarketDataFeedSymbolStatus> MarketDataFeedSymbolStatusEvent;
    public event EventHandler<OpenOrdersReject> OpenOrdersRejectEvent;
    public event EventHandler<OrderUpdate> OrderUpdateEvent;
    public event EventHandler<HistoricalOrderFillResponse> HistoricalOrderFillResponseEvent;
    public event EventHandler<CurrentPositionsReject> CurrentPositionsRejectEvent;
    public event EventHandler<PositionUpdate> PositionUpdateEvent;
    public event EventHandler<TradeAccountResponse> TradeAccountResponseEvent;
    public event EventHandler<ExchangeListResponse> ExchangeListResponseEvent;
    public event EventHandler<SecurityDefinitionResponse> SecurityDefinitionResponseEvent;
    public event EventHandler<SecurityDefinitionReject> SecurityDefinitionRejectEvent;
    public event EventHandler<AccountBalanceReject> AccountBalanceRejectEvent;
    public event EventHandler<AccountBalanceUpdate> AccountBalanceUpdateEvent;
    public event EventHandler<UserMessage> UserMessageEvent;
    public event EventHandler<GeneralLogMessage> GeneralLogMessageEvent;
    public event EventHandler<TradingSymbolStatus> TradingSymbolStatusEvent;
    public event EventHandler<HistoricalPriceDataResponseHeader> HistoricalPriceDataResponseHeaderEvent;
    public event EventHandler<HistoricalPriceDataReject> HistoricalPriceDataRejectEvent;
    public event EventHandler<HistoricalPriceDataRecordResponse> HistoricalPriceDataRecordResponseEvent;
    public event EventHandler<HistoricalPriceDataTickRecordResponse> HistoricalPriceDataTickRecordResponseEvent;
    public event EventHandler<HistoricalPriceDataRecordResponse_Int> HistoricalPriceDataRecordResponseIntEvent;
    public event EventHandler<HistoricalPriceDataTickRecordResponse_Int> HistoricalPriceDataTickRecordResponseIntEvent;
    public event EventHandler<MarketDataUpdateTradeWithUnbundledIndicator> MarketDataUpdateTradeWithUnbundledIndicatorEvent;
    public event EventHandler<MarketDataUpdateTradeWithUnbundledIndicator2> MarketDataUpdateTradeWithUnbundledIndicator2Event;
    public event EventHandler<MarketDataUpdateTradeNoTimestamp> MarketDataUpdateTradeNoTimestampEvent;
    public event EventHandler<MarketDataUpdateBidAskNoTimeStamp> MarketDataUpdateBidAskNoTimestampEvent;
    public event EventHandler<MarketDepthSnapshotLevelFloat> MarketDepthSnapshotLevelFloatEvent;
    public event EventHandler<MarketDepthUpdateLevelFloatWithMilliseconds> MarketDepthUpdateLevelFloatWithMillisecondsEvent;
    public event EventHandler<MarketDepthUpdateLevelNoTimestamp> MarketDepthUpdateLevelNoTimestampEvent;
    public event EventHandler<SubmitNewSingleOrder> SubmitNewSingleOrderEvent;
    public event EventHandler<SubmitNewSingleOrderInt> SubmitNewSingleOrderIntEvent;
    public event EventHandler<SubmitNewOCOOrder> SubmitNewOcoOrderEvent;
    public event EventHandler<SubmitNewOCOOrderInt> SubmitNewOcoOrderIntEvent;
    public event EventHandler<SubmitFlattenPositionOrder> SubmitFlattenPositionOrderEvent;
    public event EventHandler<CancelOrder> CancelOrderEvent;
    public event EventHandler<CancelReplaceOrder> CancelReplaceOrderEvent;
    public event EventHandler<CancelReplaceOrderInt> CancelReplaceOrderIntEvent;
    public event EventHandler<HistoricalOrderFillsReject> HistoricalOrderFillsRejectEvent;
    public event EventHandler<AccountBalanceAdjustmentReject> AccountBalanceAdjustmentRejectEvent;
    public event EventHandler<AccountBalanceAdjustmentComplete> AccountBalanceAdjustmentCompleteEvent;
    public event EventHandler<HistoricalAccountBalancesReject> HistoricalAccountBalancesRejectEvent;
    public event EventHandler<HistoricalAccountBalanceResponse> HistoricalAccountBalanceResponseEvent;
    public event EventHandler<AlertMessage> AlertMessageEvent;
    public event EventHandler<JournalEntryAdd> JournalEntryAddEvent;
    public event EventHandler<JournalEntriesReject> JournalEntriesRejectEvent;
    public event EventHandler<JournalEntryResponse> JournalEntryResponseEvent;
    public event EventHandler<HistoricalPriceDataResponseTrailer> HistoricalPriceDataResponseTrailerEvent;
    public event EventHandler<HistoricalMarketDepthDataResponseHeader> HistoricalMarketDepthDataResponseHeaderEvent;
    public event EventHandler<HistoricalMarketDepthDataReject> HistoricalMarketDepthDataRejectEvent;
    public event EventHandler<HistoricalMarketDepthDataRecordResponse> HistoricalMarketDepthDataRecordResponseEvent;

    private void OnIMessage<T>(T message, EventHandler<T> eventForMessage) where T : IMessage
    {
        var tmp = eventForMessage;
        tmp?.Invoke(this, message);
    }

    /// <summary>
    ///     Subscribe to this event to get EVERY response from the server
    /// </summary>
    public event EventHandler<IMessage> EveryMessageFromServer;

    private void OnEveryResponse(IMessage protobuf)
    {
        var tmp = EveryMessageFromServer;
        tmp?.Invoke(this, protobuf);
    }

    /// <summary>
    ///     ConnectedEvent happens after a successful Logon
    /// </summary>
    public event EventHandler ConnectedEvent;

    private void OnConnected()
    {
        var temp = ConnectedEvent;
        temp?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    ///     DisconnectedEvent happens just before Close/Dispose
    /// </summary>
    public event EventHandler DisconnectedEvent;

    private void OnDisconnected()
    {
        var temp = DisconnectedEvent;
        temp?.Invoke(this, EventArgs.Empty);
    }
}