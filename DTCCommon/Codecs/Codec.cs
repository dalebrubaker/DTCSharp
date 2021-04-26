using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using DTCCommon.Exceptions;
using DTCPB;
using Google.Protobuf;
using NLog;

namespace DTCCommon.Codecs
{
    public abstract class Codec : IDisposable
    {
        // ReSharper disable once MemberCanBePrivate.Global
        protected static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private readonly string _ownerName;

        private readonly Stream _stream; // normally a NetworkStream but can be a MemoryStream for a unit test
        public bool IsZippedStream => _deflateStream != null;
        private DeflateStream _deflateStream;
        private readonly byte[] _bufferHeader;
        private bool _isDisposed;

        /// <summary>
        /// The converter to and from Protobuf for the current encoding
        /// </summary>
        private readonly ICodecConverter _codecConverter;

        private Stream CurrentStream => _deflateStream ?? _stream;

        protected Codec(Stream stream, ICodecConverter codecConverter)
        {
            var frame = new StackFrame(2);
            var declaringType = frame.GetMethod().DeclaringType;
            if (declaringType != null)
            {
                _ownerName = declaringType.Name;
            }
            _stream = stream;
            _bufferHeader = new byte[4];
            _codecConverter = codecConverter;
        }

        public abstract EncodingEnum Encoding { get; }

        /// <summary>
        /// Write the ProtocolBuffer IMessage as messageBytes to the network stream.
        /// Heartbeats will be skipped if the stream is zipped
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="message"></param>
        public void Write<T>(DTCMessageType messageType, T message) where T : IMessage
        {
            if (messageType == DTCMessageType.Heartbeat && IsZippedStream)
            {
                // Refuse to write a heartbeat while we're zipped
                return;
            }
            var buffer = _codecConverter.ConvertToBuffer(messageType, message);

            // Write the header
            var header = new byte[4];
            var size = (short)(buffer.Length + 4);
            header[0] = (byte)size;
            header[1] = (byte)((uint)size >> 8);
            var msgType = (short)messageType;
            header[2] = (byte)msgType;
            header[3] = (byte)((uint)msgType >> 8);
            CurrentStream.Write(header, 0, 4);

            CurrentStream.Write(buffer, 0, buffer.Length);
        }

        public MessageDTC GetMessageDTC()
        {
            var (messageType, messageBytes) = ReadMessage();
            if (messageType == DTCMessageType.MessageTypeUnset)
            {
                return new MessageDTC(messageType, null);
            }
            var iMessage = _codecConverter.ConvertToProtobuf(messageType, messageBytes);
            var result = new MessageDTC(messageType, iMessage);
            return result;
        }

        public (DTCMessageType messageType, byte[] bytes) ReadMessage()
        {
            if (_isDisposed)
            {
                return (DTCMessageType.MessageTypeUnset, new byte[0]);
            }
            try
            {
                var numBytes = CurrentStream.Read(_bufferHeader, 0, 2);
                if (numBytes < 2)
                {
                    // There is not a complete record available yet
                    return (DTCMessageType.MessageTypeUnset, new byte[0]);
                }
                MyDebug.Assert(numBytes == 2);
                var size = BitConverter.ToInt16(_bufferHeader, 0);
                //Logger.Debug($"{this}.{nameof(ReadMessage)} read size={size} from _stream");
                if (size < 4)
                {
                    var buffer = new byte[10000];
                    var moreBytes = CurrentStream.Read(buffer, 0, 10000);
                    // There is not a complete record available yet
                    return (DTCMessageType.MessageTypeUnset, new byte[0]);
                    // MyDebug.Assert(size > 4, "If only 4, then message length is 0 messageBytes");
                }
                //MyDebug.Assert(size > 4, "If only 4, then message length is 0 messageBytes");
                numBytes = CurrentStream.Read(_bufferHeader, 2, 2);
                if (numBytes < 2)
                {
                    // There is not a complete record available yet
                    return (DTCMessageType.MessageTypeUnset, new byte[0]);
                }
                MyDebug.Assert(numBytes == 2);
                var messageType = (DTCMessageType)BitConverter.ToInt16(_bufferHeader, 2);
                //Logger.Debug($"{this}.{nameof(ReadMessage)} read messageType={messageType} from _stream");
                var messageSize = size - 4; // size includes the header
                var messageBytes = new byte[messageSize];
                numBytes = CurrentStream.Read(messageBytes, 0, messageSize);
                if (numBytes < messageSize)
                {
                    // There is not a complete record available yet
                    return (DTCMessageType.MessageTypeUnset, new byte[0]);
                }
                //Logger.Debug($"{this}.{nameof(ReadMessage)} read {numBytes} messageSize from _stream");
                MyDebug.Assert(numBytes == messageSize);
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

        public IMessage GetProtobuf(DTCMessageType messageType, byte[] messageBytes)
        {
            return _codecConverter.ConvertToProtobuf(messageType, messageBytes);
        }

        /// <summary>
        /// Called by Client when the server tells it to read zipped
        /// </summary>
        /// <exception cref="DTCSharpException"></exception>
        public void ReadSwitchToZipped()
        {
            if (IsZippedStream)
            {
                throw new DTCSharpException("Why?");
            }
            // Skip past the 2-byte header. See https://tools.ietf.org/html/rfc1950
            var buffer = new byte[2];
            CurrentStream.Read(buffer, 0, 2);
            var zlibCmf = buffer[0];
            if (zlibCmf != 120)
            {
                // 120 = 0111 1000 means Deflate
                throw new DTCSharpException($"Unexpected zlibCmf header byte {zlibCmf}, expected 120");
            }
            var zlibFlg = buffer[1];
            if (zlibFlg != 156)
            {
                // 156 = 1001 1100
                throw new DTCSharpException($"Unexpected zlibFlg header byte {zlibFlg}, expected 156");
            }
            try
            {
                // Leave the underlying stream open
                _deflateStream = new DeflateStream(_stream, CompressionMode.Decompress, true);
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
            if (IsZippedStream)
            {
                throw new DTCSharpException("Why?");
            }

            // Write the 2-byte header that Sierra Chart has coming from ZLib. See https://tools.ietf.org/html/rfc1950
            var buffer = new byte[2];
            buffer[0] = 120; // zlibCmf 120 = 0111 1000 means Deflate 
            buffer[1] = 156; // zlibFlg 156 = 1001 1100 6 = 1001 1100
            _stream.Write(buffer, 0, 2);
            try
            {
                _deflateStream = new DeflateStream(_stream, CompressionMode.Compress, true);
                _deflateStream.Flush();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, ex.Message);
                throw;
            }

            Logger.Debug("Switched to zipped in {nameof(WriteSwitchToZipped)} {this}", this, nameof(WriteSwitchToZipped));
        }

        public void EndZippedWriting()
        {
            // Switch _stream from _deflateStream back to the _tcpClient.NetworkStream
            // Do NOT dispose of the underlying NetworkStream, which is owned by the _tcpClient
            _deflateStream.Close(); // also does Dispose
            _deflateStream = null;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            // Do NOT dispose of the underlying NetworkStream, which is owned by the _tcpClient
            //_stream?.Dispose();
            _isDisposed = true;
            _deflateStream?.Dispose();
        }

        public override string ToString()
        {
            var result = $"{Encoding} owned by {_ownerName} zipped={IsZippedStream} ";
            return result;
        }
    }
}