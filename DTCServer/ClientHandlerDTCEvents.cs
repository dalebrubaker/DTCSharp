using System;
using System.IO.Compression;
using DTCCommon;
using DTCCommon.Codecs;
using DTCPB;
using Google.Protobuf;

namespace DTCServer
{
    /// <summary>
    /// This partial class focuses on processing requests and sending events in <see cref="ClientHandlerDTC"/>
    /// </summary>
    public partial class ClientHandlerDTC
    {
        /// <summary>
        /// Provides ClientName etc. about the client being handled
        /// </summary>
        private LogonRequest LogonRequest { get; set; }
        
        /// <summary>
        /// Process messageProto if it should be handled immediately instead of adding it to the queue to process later.
        /// We don't want heartbeats to be delayed until all responses on the queue have been processed.
        /// </summary>
        /// <param name="messageProto"></param>
        /// <returns><c>true</c> if handled</returns>
        private bool PreProcessRequest(MessageProto messageProto)
        {
            if (messageProto.IsExtended)
            {
                // We don't preProcess any extended message
                return false;
            }
            if (messageProto.MessageType == DTCMessageType.MessageTypeUnset)
            {
                throw new DTCSharpException("Why? Should be filtered out by the reader.");
            }
            var message = messageProto.Message;
            var messageType = messageProto.MessageType;
            OnEveryRequest(messageProto.Message);

            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (messageProto.MessageType)
            {
                case DTCMessageType.Heartbeat:
                    // We don't process this message. ResponseReader records the last message received,
                    //  whether a heartbeat or any other record, as required while reading historical records without intermingled heartbeats
                    //s_logger.ConditionalDebug($"Heartbeat received from DTC in {this}");
                    SendResponse(messageProto);
                    _callback.Invoke(this, messageProto); // send this to the callback for informational purposes
                    return true;
                case DTCMessageType.EncodingRequest:
                    HandleEncodingRequest(messageProto);
                    return true;
                case DTCMessageType.HistoricalPriceDataRequest:
                    _callback.Invoke(this, messageProto);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Process the request received from a client
        /// </summary>
        /// <param name="messageProto"></param>
        private void ProcessRequest(MessageProto messageProto)
        {
            OnEveryRequest(messageProto.Message);
            if (messageProto.IsExtended)
            {
                ProcessRequestExtended(messageProto);
                return;
            }
            var message = messageProto.Message;
            var messageType = messageProto.MessageType;
            switch (messageType)
            {
                case DTCMessageType.LogonRequest:
                    LogonRequest = (LogonRequest)message;
                    _heartbeatIntervalInSeconds = LogonRequest.HeartbeatIntervalInSeconds;
                    _callback.Invoke(this, messageProto); // send this to the callback for informational purposes

                    // Don't start looking for heartbeats and sending heartbeats until logon is requested
                    _timerHeartbeat.Start();
                    break;
                case DTCMessageType.Heartbeat:
                    throw new InvalidOperationException("This message type should have been handled by PreProcessRequest");
                case DTCMessageType.Logoff:
                    _callback.Invoke(this, messageProto); // send this to the callback for informational purposes
                    Dispose();
                    break;
                case DTCMessageType.MarketDataRequest:
                case DTCMessageType.MarketDepthRequest:
                case DTCMessageType.SubmitNewSingleOrder:
                case DTCMessageType.SubmitNewSingleOrderInt:
                case DTCMessageType.SubmitNewOcoOrder:
                case DTCMessageType.SubmitNewOcoOrderInt:
                case DTCMessageType.CancelOrder:
                case DTCMessageType.CancelReplaceOrder:
                case DTCMessageType.CancelReplaceOrderInt:
                case DTCMessageType.OpenOrdersRequest:
                case DTCMessageType.HistoricalOrderFillsRequest:
                case DTCMessageType.CurrentPositionsRequest:
                case DTCMessageType.TradeAccountsRequest:
                case DTCMessageType.ExchangeListRequest:
                case DTCMessageType.SymbolsForExchangeRequest:
                case DTCMessageType.UnderlyingSymbolsForExchangeRequest:
                case DTCMessageType.SymbolsForUnderlyingRequest:
                case DTCMessageType.SecurityDefinitionForSymbolRequest:
                case DTCMessageType.SymbolSearchRequest:
                case DTCMessageType.AccountBalanceRequest:
                case DTCMessageType.TradingSymbolStatus:
                case DTCMessageType.SubmitFlattenPositionOrder:
                case DTCMessageType.HistoricalOrderFillsReject:
                case DTCMessageType.HistoricalAccountBalancesRequest:
                case DTCMessageType.AlertMessage:
                case DTCMessageType.JournalEntryAdd:
                case DTCMessageType.JournalEntriesRequest:
                case DTCMessageType.JournalEntriesReject:
                case DTCMessageType.HistoricalMarketDepthDataRequest:
                    _callback.Invoke(this, messageProto);
                    break;
                case DTCMessageType.MessageTypeUnset:
                default:
                    throw new ArgumentOutOfRangeException($"Unexpected Message {messageProto} received by {this}.{nameof(ProcessRequest)}.");
            }
        }

        private void HandleEncodingRequest(MessageProto messageProto)
        {
            var encodingRequest = (EncodingRequest)messageProto.Message;
            var encodingResponse = new EncodingResponse
            {
                ProtocolType = encodingRequest.ProtocolType,
                ProtocolVersion = encodingRequest.ProtocolVersion,
                Encoding = _currentEncoding // Default to reject the encoding request (keep the existing one)
            };
            if (encodingRequest.Encoding != _currentEncoding)
            {
                switch (encodingRequest.Encoding)
                {
                    case EncodingEnum.BinaryEncoding:
                    case EncodingEnum.ProtocolBuffers:
                        // Accept the encodingRequest
                        s_logger.ConditionalTrace($"ClientHandler is changing encoding from {_currentEncoding} to {encodingRequest.Encoding} in {this}");
                        encodingResponse.Encoding = encodingRequest.Encoding;
                        break;
                    case EncodingEnum.BinaryWithVariableLengthStrings:
                    case EncodingEnum.JsonEncoding:
                    case EncodingEnum.JsonCompactEncoding:
                        s_logger.ConditionalTrace($"ClientHandler is rejecting the encoding request change to {encodingRequest.Encoding}, encoding remains {_currentEncoding} in {this}");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // BE SURE to change the encoding AFTER sending the response
            SendResponse(DTCMessageType.EncodingResponse, encodingResponse);

            if (encodingResponse.Encoding != _currentEncoding)
            {
                // Now change to the new encoding
                _currentEncoding = encodingRequest.Encoding;
                switch (encodingRequest.Encoding)
                {
                    case EncodingEnum.BinaryEncoding:
                        _encode = CodecBinaryConverter.EncodeBinary;
                        _decode = CodecBinaryConverter.DecodeBinary;
                        s_logger.ConditionalTrace($"Changed codec from {_currentEncoding} to {encodingResponse.Encoding} in {this}");
                        break;
                    case EncodingEnum.BinaryWithVariableLengthStrings:
                    case EncodingEnum.JsonEncoding:
                    case EncodingEnum.JsonCompactEncoding:
                        // not supported.
                        throw new InvalidOperationException("Should have been rejected above.");
                    case EncodingEnum.ProtocolBuffers:
                        _encode = CodecProtobufConverter.EncodeProtobuf;
                        _decode = CodecProtobufConverter.DecodeProtobuf;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                _callback.Invoke(this, messageProto); // send this to the callback for informational purposes
            }
        }

        private void ProcessRequestExtended(MessageProto messageProto)
        {
            switch (messageProto.MessageTypeExtended)
            {
                case DTCSharpMessageType.Unset:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void SwitchStreamToZipped()
        {
            _currentStream.WriteZipHeader();
            try
            {
                _currentStream = new DeflateStream(_currentStream, CompressionMode.Compress, true);
                s_logger.ConditionalDebug($"Switched clientHandler to write zipped in {this}");
            }
            catch (Exception ex)
            {
                s_logger.Error(ex, $"{ex.Message} in {this}");
                throw;
            }
        }

        #region Events

        public event EventHandler<string> Connected;

        internal void OnConnected(string message)
        {
            IsConnected = true;
            var temp = Connected;
            temp?.Invoke(this, message);
        }

        public event EventHandler<Result> Disconnected;

        internal void OnDisconnected(Result result)
        {
            if (!IsConnected)
            {
                return;
            }
            IsConnected = false;
            var temp = Disconnected;
            temp?.Invoke(this, result);
        }

        public event EventHandler<IMessage> EveryMessageFromClient;

        private void OnEveryRequest(IMessage protobuf)
        {
            var tmp = EveryMessageFromClient;
            tmp?.Invoke(this, protobuf);
        }

        #endregion Events
    }
}