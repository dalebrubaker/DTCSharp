// unset
using System;
using System.IO;
using System.IO.Compression;
using DTCCommon.Enums;
using DTCCommon.Exceptions;
using DTCCommon.Extensions;
using DTCPB;
using Google.Protobuf;
using NLog;

namespace DTCCommon.Codecs
{
    public abstract class Codec
    {
        private readonly Stream _stream;
        private readonly ClientOrServer _clientOrServer;
        protected static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        protected BinaryWriter _binaryWriter;
        protected bool _disabledHeartbeats;
        protected bool _isZippedStream;
        private BinaryReader _binaryReader;
        private DeflateStream _deflateStream;

        protected Codec(Stream stream, ClientOrServer clientOrServer)
        {
            _stream = stream;
            _clientOrServer = clientOrServer;
            _binaryWriter = new BinaryWriter(stream);
            _binaryReader = new BinaryReader(stream);
        }

        /// <summary>
        /// Write the ProtocolBuffer IMessage as bytes to the network stream.
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="message"></param>
        public abstract void Write<T>(DTCMessageType messageType, T message) where T : IMessage;

        /// <summary>
        /// Load the message represented by bytes into a new IMessage. Each codec translates the byte stream to a protobuf message.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageType"></param>
        /// <param name="bytes"></param>
        /// <param name="index">the starting index in bytes</param>
        /// <returns></returns>
        public abstract T Load<T>(DTCMessageType messageType, byte[] bytes, int index = 0) where T : IMessage<T>, new();

        public (DTCMessageType messageType, byte[] bytes) ReadMessage()
        {
            try
            {
                var size = _binaryReader.ReadUInt16();
                var messageType = (DTCMessageType)_binaryReader.ReadUInt16();
                //Logger.Debug($"{nameof(Client)}.{nameof(ResponseReaderAsync)} is about to process {messageType}");
#if DEBUG
                if (messageType == DTCMessageType.EncodingResponse)
                {
                }
                DebugHelpers.AddResponseReceived(messageType, this, size);
                var requestsSent = DebugHelpers.RequestsSent;
                var requestsReceived = DebugHelpers.RequestsReceived;
                var responsesReceived = DebugHelpers.ResponsesReceived;
                var responsesSent = DebugHelpers.ResponsesSent;
#endif
                var messageBytes = _binaryReader.ReadBytes(size - 4); // size includes the header size+type
                return (messageType, messageBytes);
            }
            catch (EndOfStreamException)
            {
                // This happens when zipped historical records are done. We can no longer read from this stream, which was closed by ClientHandler
                return (DTCMessageType.MessageTypeUnset, new byte[0]);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, ex.Message);
                throw;
            }
        }

        protected void WriteEncodingRequest<T>(DTCMessageType messageType, T message) where T : IMessage
        {
            // EncodingRequest goes as binary for all protocol versions
            var encodingRequest = message as EncodingRequest;
            Utility.WriteHeader(_binaryWriter, 12, messageType);
            _binaryWriter.Write(encodingRequest.ProtocolVersion);
            _binaryWriter.Write((int)encodingRequest.Encoding); // enum size is 4
            var protocolType = encodingRequest.ProtocolType.ToFixedBytes(4);
            _binaryWriter.Write(protocolType); // 3 chars DTC plus null terminator 
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

        protected void WriteEncodingResponse<T>(DTCMessageType messageType, T message)
        {
            var encodingResponse = message as EncodingResponse;
            Utility.WriteHeader(_binaryWriter, 12, messageType);
            _binaryWriter.Write(encodingResponse.ProtocolVersion);
            _binaryWriter.Write((int)encodingResponse.Encoding); // enum size is 4
            var protocolType2 = encodingResponse.ProtocolType.ToFixedBytes(4);
            _binaryWriter.Write(protocolType2); // 3 chars DTC plus null terminator 
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
            var zlibCmf = _binaryReader.ReadByte(); // 120 = 0111 1000 means Deflate 
            if (zlibCmf != 120)
            {
                throw new DTCSharpException($"Unexpected zlibCmf header byte {zlibCmf}, expected 120");
            }
            var zlibFlg = _binaryReader.ReadByte(); // 156 = 1001 1100
            if (zlibFlg != 156)
            {
                throw new DTCSharpException($"Unexpected zlibFlg header byte {zlibFlg}, expected 156");
            }
            try
            {
                _isZippedStream = true;
                _disabledHeartbeats = true;
                var deflateStream = new DeflateStream(_stream, CompressionMode.Decompress);
                _binaryReader = new BinaryReader(deflateStream);
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
            _binaryWriter.Write((byte)120); // zlibCmf 120 = 0111 1000 means Deflate 
            _binaryWriter.Write((byte)156); // zlibFlg 156 = 1001 1100
            try
            {
                _isZippedStream = true;
                _disabledHeartbeats = true;
                _deflateStream = new DeflateStream(_stream, CompressionMode.Compress, true);
                _deflateStream.Flush();
                // can't read this stream _binaryReader = new BinaryReader(deflateStream);
                _binaryWriter = new BinaryWriter(_deflateStream);
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
            _binaryReader?.Close();
            _binaryWriter?.Close();
        }

        public void EndZippedWriting()
        {
            _deflateStream.Close();
            _deflateStream?.Dispose();
            _deflateStream = null;
            Close();
            _binaryWriter = null;
            _binaryReader = null;
        }

        public override string ToString()
        {
            return $"{_clientOrServer} {GetType().Name}";
        }
    }
}