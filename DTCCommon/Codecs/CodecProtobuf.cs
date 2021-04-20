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
        /// Write the message using binaryWriter
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        /// <param name="binaryWriter"></param>
        public void Write<T>(T message, BinaryWriter binaryWriter) where T : IMessage
        {
            var messageType = MessageTypes.MessageTypeByMessage[typeof(T)];
            Write(messageType, message, binaryWriter);
        }

        public T Load<T>(DTCMessageType messageType, byte[] bytes, int index = 0) where T : IMessage<T>, new()
        {
            // For protobuf we don't need the messageType
            return Load<T>(bytes);
        }

        /// <summary>
        /// Load the message represented by bytes into the IMessage
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public T Load<T>(byte[] bytes) where T : IMessage<T>, new()
        {
            var result = new T();
            result.MergeFrom(bytes);
            return result;
        }
    }
}