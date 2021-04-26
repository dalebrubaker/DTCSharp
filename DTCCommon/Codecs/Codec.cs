using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using DTCCommon.Exceptions;
using DTCCommon.Extensions;
using DTCPB;
using Google.Protobuf;
using NLog;

namespace DTCCommon.Codecs
{
    public abstract class Codec
    {
        protected static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        
        protected string _ownerName;


        protected Stream _stream; // normally a NetworkStream but can be a MemoryStream for a unit test
        private bool _isZippedStream;
        private DeflateStream _deflateStream;
        private readonly byte[] _bufferHeader;

        protected bool _disabledHeartbeats;

        protected Codec(Stream stream)
        {
            _stream = stream;
            _bufferHeader = new byte[4];
        }

        public abstract EncodingEnum Encoding { get; }

        /// <summary>
        /// Write the ProtocolBuffer IMessage as bytes to the network stream.
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        public abstract Task WriteAsync<T>(DTCMessageType messageType, T message, CancellationToken cancellationToken) where T : IMessage;

        /// <summary>
        /// Load the message represented by bytes into a new IMessage. Each codec translates the byte stream to a protobuf message.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageType"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public abstract T Load<T>(DTCMessageType messageType, byte[] bytes) where T : IMessage<T>, new();

        private MessageDTC GetMessageWithType<T>(DTCMessageType messageType, byte[] bytes) where T : IMessage<T>, new()
        {
            var iMessage = Load<T>(messageType, bytes);
            var result = new MessageDTC(messageType, iMessage);
            return result;
        }

        public async Task<MessageDTC> GetMessageDTCAsync(CancellationToken cancellationToken)
        {
            var (messageType, bytes) = await ReadMessageAsync(cancellationToken);
            return ReadMessageDTC(messageType, bytes);
        }

        private MessageDTC ReadMessageDTC(DTCMessageType messageType, byte[] bytes)
        {
            switch (messageType)
            {
                case DTCMessageType.MessageTypeUnset:
                    // Perhaps an exception in ReadMessage()
                    return null;
                case DTCMessageType.LogonRequest:
                    return GetMessageWithType<LogonRequest>(messageType, bytes);
                case DTCMessageType.LogonResponse:
                    return GetMessageWithType<LogonResponse>(messageType, bytes);
                case DTCMessageType.Heartbeat:
                    return GetMessageWithType<Heartbeat>(messageType, bytes);
                case DTCMessageType.Logoff:
                    return GetMessageWithType<Logoff>(messageType, bytes);
                case DTCMessageType.EncodingRequest:
                    return GetMessageWithType<EncodingRequest>(messageType, bytes);
                case DTCMessageType.EncodingResponse:
                    return GetMessageWithType<EncodingResponse>(messageType, bytes);
                case DTCMessageType.MarketDataRequest:
                    return GetMessageWithType<MarketDataRequest>(messageType, bytes);
                case DTCMessageType.MarketDataReject:
                    return GetMessageWithType<MarketDataReject>(messageType, bytes);
                case DTCMessageType.MarketDataSnapshot:
                    return GetMessageWithType<MarketDataSnapshot>(messageType, bytes);
                case DTCMessageType.MarketDataSnapshotInt:
                    return GetMessageWithType<MarketDataSnapshot_Int>(messageType, bytes);
                case DTCMessageType.MarketDataUpdateTrade:
                    return GetMessageWithType<MarketDataUpdateTrade>(messageType, bytes);
                case DTCMessageType.MarketDataUpdateTradeCompact:
                    return GetMessageWithType<MarketDataUpdateTradeCompact>(messageType, bytes);
                case DTCMessageType.MarketDataUpdateTradeInt:
                    return GetMessageWithType<MarketDataUpdateTrade_Int>(messageType, bytes);
                case DTCMessageType.MarketDataUpdateLastTradeSnapshot:
                    return GetMessageWithType<MarketDataUpdateLastTradeSnapshot>(messageType, bytes);
                case DTCMessageType.MarketDataUpdateTradeWithUnbundledIndicator:
                    return GetMessageWithType<MarketDataUpdateTradeWithUnbundledIndicator>(messageType, bytes);
                case DTCMessageType.MarketDataUpdateTradeWithUnbundledIndicator2:
                    return GetMessageWithType<MarketDataUpdateTradeWithUnbundledIndicator2>(messageType, bytes);
                case DTCMessageType.MarketDataUpdateTradeNoTimestamp:
                    return GetMessageWithType<MarketDataUpdateTradeNoTimestamp>(messageType, bytes);
                case DTCMessageType.MarketDataUpdateBidAsk:
                    return GetMessageWithType<MarketDataUpdateBidAsk>(messageType, bytes);
                case DTCMessageType.MarketDataUpdateBidAskCompact:
                    return GetMessageWithType<MarketDataUpdateBidAskCompact>(messageType, bytes);
                case DTCMessageType.MarketDataUpdateBidAskNoTimestamp:
                    return GetMessageWithType<MarketDataUpdateBidAskNoTimeStamp>(messageType, bytes);
                case DTCMessageType.MarketDataUpdateBidAskInt:
                    return GetMessageWithType<MarketDataUpdateBidAsk_Int>(messageType, bytes);
                case DTCMessageType.MarketDataUpdateSessionOpen:
                    return GetMessageWithType<MarketDataUpdateSessionOpen>(messageType, bytes);
                case DTCMessageType.MarketDataUpdateSessionOpenInt:
                    return GetMessageWithType<MarketDataUpdateSessionOpen_Int>(messageType, bytes);
                case DTCMessageType.MarketDataUpdateSessionHigh:
                    return GetMessageWithType<MarketDataUpdateSessionHigh>(messageType, bytes);
                case DTCMessageType.MarketDataUpdateSessionHighInt:
                    return GetMessageWithType<MarketDataUpdateSessionHigh_Int>(messageType, bytes);
                case DTCMessageType.MarketDataUpdateSessionLow:
                    return GetMessageWithType<MarketDataUpdateSessionLow>(messageType, bytes);
                case DTCMessageType.MarketDataUpdateSessionLowInt:
                    return GetMessageWithType<MarketDataUpdateSessionLow_Int>(messageType, bytes);
                case DTCMessageType.MarketDataUpdateSessionVolume:
                    return GetMessageWithType<MarketDataUpdateSessionVolume>(messageType, bytes);
                case DTCMessageType.MarketDataUpdateOpenInterest:
                    return GetMessageWithType<MarketDataUpdateOpenInterest>(messageType, bytes);
                case DTCMessageType.MarketDataUpdateSessionSettlement:
                    return GetMessageWithType<MarketDataUpdateSessionSettlement>(messageType, bytes);
                case DTCMessageType.MarketDataUpdateSessionSettlementInt:
                    return GetMessageWithType<MarketDataUpdateSessionSettlement_Int>(messageType, bytes);
                case DTCMessageType.MarketDataUpdateSessionNumTrades:
                    return GetMessageWithType<MarketDataUpdateSessionNumTrades>(messageType, bytes);
                case DTCMessageType.MarketDataUpdateTradingSessionDate:
                    return GetMessageWithType<MarketDataUpdateTradingSessionDate>(messageType, bytes);
                case DTCMessageType.MarketDepthRequest:
                    return GetMessageWithType<MarketDepthRequest>(messageType, bytes);
                case DTCMessageType.MarketDepthReject:
                    return GetMessageWithType<MarketDepthReject>(messageType, bytes);
                case DTCMessageType.MarketDepthSnapshotLevel:
                    return GetMessageWithType<MarketDepthSnapshotLevel>(messageType, bytes);
                case DTCMessageType.MarketDepthSnapshotLevelInt:
                    return GetMessageWithType<MarketDepthSnapshotLevel_Int>(messageType, bytes);
                case DTCMessageType.MarketDepthSnapshotLevelFloat:
                    return GetMessageWithType<MarketDepthSnapshotLevelFloat>(messageType, bytes);
                case DTCMessageType.MarketDepthUpdateLevel:
                    return GetMessageWithType<MarketDepthUpdateLevel>(messageType, bytes);
                case DTCMessageType.MarketDepthUpdateLevelFloatWithMilliseconds:
                    return GetMessageWithType<MarketDepthUpdateLevelFloatWithMilliseconds>(messageType, bytes);
                case DTCMessageType.MarketDepthUpdateLevelNoTimestamp:
                    return GetMessageWithType<MarketDepthUpdateLevelNoTimestamp>(messageType, bytes);
                case DTCMessageType.MarketDepthUpdateLevelInt:
                    return GetMessageWithType<MarketDepthUpdateLevel_Int>(messageType, bytes);
                case DTCMessageType.MarketDataFeedStatus:
                    return GetMessageWithType<MarketDataFeedStatus>(messageType, bytes);
                case DTCMessageType.MarketDataFeedSymbolStatus:
                    return GetMessageWithType<MarketDataFeedSymbolStatus>(messageType, bytes);
                case DTCMessageType.TradingSymbolStatus:
                    return GetMessageWithType<TradingSymbolStatus>(messageType, bytes);
                case DTCMessageType.SubmitNewSingleOrder:
                    return GetMessageWithType<SubmitNewSingleOrder>(messageType, bytes);
                case DTCMessageType.SubmitNewSingleOrderInt:
                    return GetMessageWithType<SubmitNewSingleOrderInt>(messageType, bytes);
                case DTCMessageType.SubmitNewOcoOrder:
                    return GetMessageWithType<SubmitNewOCOOrder>(messageType, bytes);
                case DTCMessageType.SubmitNewOcoOrderInt:
                    return GetMessageWithType<SubmitNewOCOOrderInt>(messageType, bytes);
                case DTCMessageType.SubmitFlattenPositionOrder:
                    return GetMessageWithType<SubmitFlattenPositionOrder>(messageType, bytes);
                case DTCMessageType.CancelOrder:
                    return GetMessageWithType<CancelOrder>(messageType, bytes);
                case DTCMessageType.CancelReplaceOrder:
                    return GetMessageWithType<CancelReplaceOrder>(messageType, bytes);
                case DTCMessageType.CancelReplaceOrderInt:
                    return GetMessageWithType<CancelReplaceOrderInt>(messageType, bytes);
                case DTCMessageType.OpenOrdersRequest:
                    return GetMessageWithType<OpenOrdersRequest>(messageType, bytes);
                case DTCMessageType.OpenOrdersReject:
                    return GetMessageWithType<OpenOrdersReject>(messageType, bytes);
                case DTCMessageType.OrderUpdate:
                    return GetMessageWithType<OrderUpdate>(messageType, bytes);
                case DTCMessageType.HistoricalOrderFillsRequest:
                    return GetMessageWithType<HistoricalOrderFillsRequest>(messageType, bytes);
                case DTCMessageType.HistoricalOrderFillResponse:
                    return GetMessageWithType<HistoricalOrderFillResponse>(messageType, bytes);
                case DTCMessageType.HistoricalOrderFillsReject:
                    return GetMessageWithType<HistoricalOrderFillsReject>(messageType, bytes);
                case DTCMessageType.CurrentPositionsRequest:
                    return GetMessageWithType<CurrentPositionsRequest>(messageType, bytes);
                case DTCMessageType.CurrentPositionsReject:
                    return GetMessageWithType<CurrentPositionsReject>(messageType, bytes);
                case DTCMessageType.PositionUpdate:
                    return GetMessageWithType<PositionUpdate>(messageType, bytes);
                case DTCMessageType.TradeAccountsRequest:
                    return GetMessageWithType<TradeAccountsRequest>(messageType, bytes);
                case DTCMessageType.TradeAccountResponse:
                    return GetMessageWithType<TradeAccountResponse>(messageType, bytes);
                case DTCMessageType.ExchangeListRequest:
                    return GetMessageWithType<ExchangeListRequest>(messageType, bytes);
                case DTCMessageType.ExchangeListResponse:
                    return GetMessageWithType<ExchangeListResponse>(messageType, bytes);
                case DTCMessageType.SymbolsForExchangeRequest:
                    return GetMessageWithType<SymbolsForExchangeRequest>(messageType, bytes);
                case DTCMessageType.UnderlyingSymbolsForExchangeRequest:
                    return GetMessageWithType<UnderlyingSymbolsForExchangeRequest>(messageType, bytes);
                case DTCMessageType.SymbolsForUnderlyingRequest:
                    return GetMessageWithType<SymbolsForUnderlyingRequest>(messageType, bytes);
                case DTCMessageType.SecurityDefinitionForSymbolRequest:
                    return GetMessageWithType<SecurityDefinitionForSymbolRequest>(messageType, bytes);
                case DTCMessageType.SecurityDefinitionResponse:
                    return GetMessageWithType<SecurityDefinitionResponse>(messageType, bytes);
                case DTCMessageType.SymbolSearchRequest:
                    return GetMessageWithType<SymbolSearchRequest>(messageType, bytes);
                case DTCMessageType.SecurityDefinitionReject:
                    return GetMessageWithType<SecurityDefinitionReject>(messageType, bytes);
                case DTCMessageType.AccountBalanceRequest:
                    return GetMessageWithType<AccountBalanceRequest>(messageType, bytes);
                case DTCMessageType.AccountBalanceReject:
                    return GetMessageWithType<AccountBalanceReject>(messageType, bytes);
                case DTCMessageType.AccountBalanceUpdate:
                    return GetMessageWithType<AccountBalanceUpdate>(messageType, bytes);
                case DTCMessageType.AccountBalanceAdjustment:
                    return GetMessageWithType<AccountBalanceAdjustment>(messageType, bytes);
                case DTCMessageType.AccountBalanceAdjustmentReject:
                    return GetMessageWithType<AccountBalanceAdjustmentReject>(messageType, bytes);
                case DTCMessageType.AccountBalanceAdjustmentComplete:
                    return GetMessageWithType<AccountBalanceAdjustmentComplete>(messageType, bytes);
                case DTCMessageType.HistoricalAccountBalancesRequest:
                    return GetMessageWithType<HistoricalAccountBalancesRequest>(messageType, bytes);
                case DTCMessageType.HistoricalAccountBalancesReject:
                    return GetMessageWithType<HistoricalAccountBalancesReject>(messageType, bytes);
                case DTCMessageType.HistoricalAccountBalanceResponse:
                    return GetMessageWithType<HistoricalAccountBalanceResponse>(messageType, bytes);
                case DTCMessageType.UserMessage:
                    return GetMessageWithType<UserMessage>(messageType, bytes);
                case DTCMessageType.GeneralLogMessage:
                    return GetMessageWithType<GeneralLogMessage>(messageType, bytes);
                case DTCMessageType.AlertMessage:
                    return GetMessageWithType<AlertMessage>(messageType, bytes);
                case DTCMessageType.JournalEntryAdd:
                    return GetMessageWithType<JournalEntryAdd>(messageType, bytes);
                case DTCMessageType.JournalEntriesRequest:
                    return GetMessageWithType<JournalEntriesRequest>(messageType, bytes);
                case DTCMessageType.JournalEntriesReject:
                    return GetMessageWithType<JournalEntriesReject>(messageType, bytes);
                case DTCMessageType.JournalEntryResponse:
                    return GetMessageWithType<JournalEntryResponse>(messageType, bytes);
                case DTCMessageType.HistoricalPriceDataRequest:
                    return GetMessageWithType<HistoricalPriceDataRequest>(messageType, bytes);
                case DTCMessageType.HistoricalPriceDataResponseHeader:
                    return GetMessageWithType<HistoricalPriceDataResponseHeader>(messageType, bytes);
                case DTCMessageType.HistoricalPriceDataReject:
                    return GetMessageWithType<HistoricalPriceDataReject>(messageType, bytes);
                case DTCMessageType.HistoricalPriceDataRecordResponse:
                    return GetMessageWithType<HistoricalPriceDataRecordResponse>(messageType, bytes);
                case DTCMessageType.HistoricalPriceDataTickRecordResponse:
                    return GetMessageWithType<HistoricalPriceDataTickRecordResponse>(messageType, bytes);
                case DTCMessageType.HistoricalPriceDataRecordResponseInt:
                    return GetMessageWithType<HistoricalPriceDataRecordResponse_Int>(messageType, bytes);
                case DTCMessageType.HistoricalPriceDataTickRecordResponseInt:
                    return GetMessageWithType<HistoricalPriceDataTickRecordResponse_Int>(messageType, bytes);
                case DTCMessageType.HistoricalPriceDataResponseTrailer:
                    return GetMessageWithType<HistoricalPriceDataResponseTrailer>(messageType, bytes);
                case DTCMessageType.HistoricalMarketDepthDataRequest:
                    return GetMessageWithType<HistoricalMarketDepthDataRequest>(messageType, bytes);
                case DTCMessageType.HistoricalMarketDepthDataResponseHeader:
                    return GetMessageWithType<HistoricalMarketDepthDataResponseHeader>(messageType, bytes);
                case DTCMessageType.HistoricalMarketDepthDataReject:
                    return GetMessageWithType<HistoricalMarketDepthDataReject>(messageType, bytes);
                case DTCMessageType.HistoricalMarketDepthDataRecordResponse:
                    return GetMessageWithType<HistoricalMarketDepthDataRecordResponse>(messageType, bytes);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public async Task<(DTCMessageType messageType, byte[] bytes)> ReadMessageAsync(CancellationToken cancellationToken)
        {
            try
            {
                var numBytes = await _stream.ReadAsync(_bufferHeader, 0, 2, cancellationToken).ConfigureAwait(false);
                if (numBytes < 2)
                {
                    // There is not a complete record available yet
                    return (DTCMessageType.MessageTypeUnset, new byte[0]);
                }
                Debug.Assert(numBytes == 2);
                var size = BitConverter.ToInt16(_bufferHeader, 0);
                Logger.Debug($"{this}.{nameof(ReadMessageAsync)} read size={size} from _stream");
                if (size < 4)
                {
                    await Task.Delay(100);
                    var buffer = new byte[10000];
                    var moreBytes = await _stream.ReadAsync(buffer, 0, 10000, cancellationToken).ConfigureAwait(false);
                    // There is not a complete record available yet
                    return (DTCMessageType.MessageTypeUnset, new byte[0]);
                    // Debug.Assert(size > 4, "If only 4, then message length is 0 bytes");
                }
                //Debug.Assert(size > 4, "If only 4, then message length is 0 bytes");
                numBytes = await _stream.ReadAsync(_bufferHeader, 2, 2, cancellationToken).ConfigureAwait(false);
                if (numBytes < 2)
                {
                    // There is not a complete record available yet
                    return (DTCMessageType.MessageTypeUnset, new byte[0]);
                }
                Debug.Assert(numBytes == 2);
                var messageType = (DTCMessageType)BitConverter.ToInt16(_bufferHeader, 2);
                //Logger.Debug($"{this}.{nameof(ReadMessageAsync)} read messageType={messageType} from _stream");
                var messageSize = size - 4; // size includes the header
                var messageBytes = new byte[messageSize];
                numBytes = await _stream.ReadAsync(messageBytes, 0, messageSize, cancellationToken).ConfigureAwait(false);
                if (numBytes < messageSize)
                {
                    // There is not a complete record available yet
                    return (DTCMessageType.MessageTypeUnset, new byte[0]);
                }
                //Logger.Debug($"{this}.{nameof(ReadMessageAsync)} read {numBytes} messageSize from _stream");
                Debug.Assert(numBytes == messageSize);
                return (messageType, messageBytes);
            }
            catch (TaskCanceledException)
            {
                return (DTCMessageType.MessageTypeUnset, new byte[0]);
            }
            catch (EndOfStreamException)
            {
                // STILL HAPPENS?
                // This happens when zipped historical records are done. We can no longer read from this stream, which was closed by ClientHandler
                return (DTCMessageType.MessageTypeUnset, new byte[0]);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, ex.Message);
                throw;
            }
        }


        protected async Task WriteEncodingRequestAsync(DTCMessageType messageType, EncodingRequest encodingRequest, CancellationToken cancellationToken)
        {
            // EncodingRequest goes as binary for all protocol versions
            var size = 16;
            using var bufferBuilder = new BufferBuilder(size, this);
            bufferBuilder.AddHeader(messageType);
            bufferBuilder.Add(encodingRequest.ProtocolVersion);
            bufferBuilder.Add((int)encodingRequest.Encoding); // enum size is 4
            var protocolType = encodingRequest.ProtocolType.ToFixedBytes(4);
            bufferBuilder.Add(protocolType); // 3 chars DTC plus null terminator 
            await bufferBuilder.WriteAsync(_stream, cancellationToken).ConfigureAwait(false);
        }

            protected static void LoadEncodingResponse<T>(byte[] bytes, int index, ref T result) where T : IMessage, new()
        {
            // EncodingResponse comes back as binary for all protocol versions
            var encodingResponse = result as EncodingResponse;
            encodingResponse.ProtocolVersion = BitConverter.ToInt32(bytes, index);
            index += 4;
            encodingResponse.Encoding = (EncodingEnum)BitConverter.ToInt32(bytes, index);
            index += 4;
            encodingResponse.ProtocolType = bytes.StringFromNullTerminatedBytes(index);
        }

        protected static void LoadEncodingRequest<T>(byte[] bytes, int index, ref T result) where T : IMessage<T>, new()
        {
            var encodingRequest = result as EncodingRequest;
            encodingRequest.ProtocolVersion = BitConverter.ToInt32(bytes, index);
            index += 4;
            encodingRequest.Encoding = (EncodingEnum)BitConverter.ToInt32(bytes, index);
            index += 4;
            encodingRequest.ProtocolType = bytes.StringFromNullTerminatedBytes(index);
        }

        protected async Task WriteEncodingResponseAsync(DTCMessageType messageType, EncodingResponse encodingResponse, CancellationToken cancellationToken)
        {
            var size = (short)16;
            using var bufferBuilder = new BufferBuilder(size, this);
            bufferBuilder.Add(size);
            bufferBuilder.Add((short)messageType);
            bufferBuilder.Add(encodingResponse.ProtocolVersion);
            bufferBuilder.Add((int)encodingResponse.Encoding); // enum size is 4
            var protocolType = encodingResponse.ProtocolType.ToFixedBytes(4);
            bufferBuilder.Add(protocolType); // 3 chars DTC plus null terminator 
            await bufferBuilder.WriteAsync(_stream, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Called by Client when the server tells it to read zipped
        /// </summary>
        /// <exception cref="DTCSharpException"></exception>
        public void ReadSwitchToZipped()
        {
            if (_isZippedStream)
            {
                throw new DTCSharpException("Why?");
            }
            // Skip past the 2-byte header. See https://tools.ietf.org/html/rfc1950
            var binaryReader = new BinaryReader(_stream);
            var zlibCmf = binaryReader.ReadByte(); // 120 = 0111 1000 means Deflate 
            if (zlibCmf != 120)
            {
                throw new DTCSharpException($"Unexpected zlibCmf header byte {zlibCmf}, expected 120");
            }
            var zlibFlg = binaryReader.ReadByte(); // 156 = 1001 1100
            if (zlibFlg != 156)
            {
                throw new DTCSharpException($"Unexpected zlibFlg header byte {zlibFlg}, expected 156");
            }
            try
            {
                _isZippedStream = true;
                _disabledHeartbeats = true;
                var deflateStream = new DeflateStream(_stream, CompressionMode.Decompress);
                
                // Does redefining _stream also corrupt deflateStream?  We want to read deflateStream but write _stream
                _stream = deflateStream;
                
                
                //_binaryReader = new BinaryReader(deflateStream);
                // Can't write this stream_binaryWriter = new BinaryWriter(deflateStream);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, ex.Message);
                throw;
            }
            Logger.Debug("Switched to zipped in {nameof(ReadSwitchToZipped)} {this}", this, nameof(ReadSwitchToZipped));
        }

        /// <summary>
        /// Called by ClientHandler when the server switches to write zipped
        /// </summary>
        /// <exception cref="DTCSharpException"></exception>
        public void WriteSwitchToZipped()
        {
            if (_isZippedStream)
            {
                throw new DTCSharpException("Why?");
            }

            // Write the 2-byte header that Sierra Chart has coming from ZLib. See https://tools.ietf.org/html/rfc1950
            using var binaryWriter = new BinaryWriter(_stream);
            binaryWriter.Write((byte)120); // zlibCmf 120 = 0111 1000 means Deflate 
            binaryWriter.Write((byte)156); // zlibFlg 156 = 1001 1100
            try
            {
                _isZippedStream = true;
                _disabledHeartbeats = true;
                _deflateStream = new DeflateStream(_stream, CompressionMode.Compress, true);
                _deflateStream.Flush();
                // can't read this stream _binaryReader = new BinaryReader(deflateStream);
                //_binaryWriter = new BinaryWriter(_deflateStream);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, ex.Message);
                throw;
            }

            Logger.Debug("Switched to zipped in {nameof(WriteSwitchToZipped)} {this}", this, nameof(WriteSwitchToZipped));
        }

        public void Close()
        {
            // _binaryReader?.Close();
            // _binaryWriter?.Close();
        }

        public void EndZippedWriting()
        {
            _deflateStream.Close();
            _deflateStream?.Dispose();
            _deflateStream = null;
            Close();
            // _binaryWriter = null;
            // _binaryReader = null;
        }

        public override string ToString()
        {
            return $"{GetType().Name} owned by {_ownerName} {Encoding}";
        }
    }
}