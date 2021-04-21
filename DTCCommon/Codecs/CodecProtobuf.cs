using System.IO;
using DTCPB;
using Google.Protobuf;

namespace DTCCommon.Codecs
{
    public class CodecProtobuf : ICodecDTC
    {
        /// <summary>
        /// Write the message using binaryWriter
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="message"></param>
        /// <param name="binaryWriter">It's possible for this to become null because of stream failure and a Dispose()</param>
        public void Write<T>(DTCMessageType messageType, T message, BinaryWriter binaryWriter) where T : IMessage
        {
            if (binaryWriter == null)
            {
                return;
            }
            var bytes = message.ToByteArray();
            Utility.WriteHeader(binaryWriter, bytes.Length, messageType);
            binaryWriter.Write(bytes);
        }

        /// <summary>
        /// Load the bytes into a new protobuf IMessage
        /// </summary>
        /// <param name="messageType">For protobuf we don't need the messageType</param>
        /// <param name="bytes"></param>
        /// <param name="index"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Load<T>(DTCMessageType messageType, byte[] bytes, int index = 0) where T : IMessage<T>, new()
        {
            var result = new T();
            result.MergeFrom(bytes);
            return result;
        }
    }
}