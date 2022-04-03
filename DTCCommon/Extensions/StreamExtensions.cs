using System;
using System.Diagnostics;
using System.IO;
using NLog;

// ReSharper disable once CheckNamespace
namespace DTCCommon
{
    public static class StreamExtensions
    {
        private static readonly ILogger s_logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Read count bytes from stream into destination
        /// Thanks to Mark Gravell at https://stackoverflow.com/questions/37783817/c-sharp-tcpclient-stream-problems
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="destination"></param>
        /// <param name="count"></param>
        /// <returns><c>true</c> if able to fill the destination buffer with count bytes</returns>
        /// <exception cref="IOException">when a read cannot occur</exception>
        private static bool Fill(Stream stream, byte[] destination, int count)
        {
            //s_logger.Debug($"Starting to Fill from stream={stream}");
            int bytesRead, offset = 0;
            while (count > 0
                   && (bytesRead = stream.Read(destination, offset, count)) > 0)
            {
                offset += bytesRead;
                count -= bytesRead;
            }
            return count == 0;
        }

        /// <summary>
        /// Read a DTC message from stream.
        /// Read count bytes from stream into destination
        /// Thanks to Mark Gravell at https://stackoverflow.com/questions/37783817/c-sharp-tcpclient-stream-problems
        /// </summary>
        /// <param name="stream"></param>
        /// <returns>(DTCMessageType messageType, byte[] messageBytes)</returns>
        /// <exception cref="IOException">when a read cannot occur</exception>
        public static unsafe MessageEncoded ReadMessageEncoded(this Stream stream)
        {
            //s_logger.Debug($"Starting to ReadMessageEncoded from stream={stream}");
            var buffer = new byte[4];
            fixed (byte* pbyte = &buffer[0])
            {
                MessageEncoded messageEncoded = null;
                while (true)
                {
                    // Read the message size, including the 4-byte header
                    if (!Fill(stream, buffer, 4))
                    {
                        //s_logger.Debug("Reached end of stream");
                        throw new EndOfStreamException();
                    }
                    var messageSize = *((short*)pbyte);
                    var messageTypeShort = (*(short*)(pbyte + 2));
                    if (messageSize < 4)
                    {
                        throw new DTCSharpException($"Illegal negative messageSize={messageSize:N0}");
                    }

                    // Read the message bytes directly into messageEncoded
                    messageEncoded = new MessageEncoded(messageTypeShort, new byte[messageSize - 4]);
                    if (!Fill(stream, messageEncoded.MessageBytes, messageSize - 4))
                    {
                        //s_logger.Debug("Reached end of stream");
                        throw new EndOfStreamException();
                    }
                    break;
                }
                //s_logger.Trace($"Read {messageEncoded} from stream={stream} messageSize={messageSize} messageTypeShort={messageTypeShort}");
                return messageEncoded;
            }
        }

        /// <summary>
        /// Write messageEncoded to stream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="messageEncoded"></param>
        public static void WriteMessageEncoded(this Stream stream, MessageEncoded messageEncoded)
        {
            //s_logger.Debug($"Starting to WriteMessageEncoded to stream={stream}");
            // try
            // {
                // Write the header
                var header = new byte[4];
                var size = (short)(messageEncoded.MessageBytes.Length + 4);
                header[0] = (byte)size;
                header[1] = (byte)((uint)size >> 8);
                var msgType = messageEncoded.MessageTypeAsShort;
                header[2] = (byte)msgType;
                header[3] = (byte)((uint)msgType >> 8);
                stream.Write(header, 0, 4);

                // Write the message
                stream.Write(messageEncoded.MessageBytes, 0, messageEncoded.MessageBytes.Length);
                //s_logger.Debug($"Wrote {messageEncoded} to stream={stream}");
            // }
            // catch (IOException ex)
            // {
            //     s_logger.Error(ex, $"{ex.Message} {messageEncoded.MessageType}");
            //     throw;
            // }
        }

        /// <summary>
        /// Skip past the 2-byte header. See https://tools.ietf.org/html/rfc1950
        /// </summary>
        /// <param name="stream"></param>
        public static void ReadPastZipHeader(this Stream stream)
        {
            var buffer = new byte[2];
            var numBytes = stream.Read(buffer, 0, 2);
            Debug.Assert(numBytes == 2);
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
        }

        /// <summary>
        /// Write the zlib header before creating a DeflateStream. See https://tools.ietf.org/html/rfc1950
        /// </summary>
        /// <param name="stream"></param>
        public static void WriteZipHeader(this Stream stream)
        {
            // Write the 2-byte header that Sierra Chart has coming from ZLib. See https://tools.ietf.org/html/rfc1950
            var buffer = new byte[2];
            buffer[0] = 120; // zlibCmf 120 = 0111 1000 means Deflate 
            buffer[1] = 156; // zlibFlg 156 = 1001 1100 6 = 1001 1100
            stream.Write(buffer, 0, 2);
        }
    }
}