using System.IO;
using DTCPB;
using Google.Protobuf;

namespace DTCCommon.Codecs
{
    public interface ICodecDTC
    {
        /// <summary>
        /// Write the message using binaryWriter.
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="message"></param>
        /// <param name="binaryWriter">It's possible for this to become null because of stream failure and a Dispose()</param>
        void Write<T>(DTCMessageType messageType, T message, BinaryWriter binaryWriter) where T : IMessage;

        /// <summary>
        /// Load the message represented by bytes into a new IMessage. Each codec translates the byte stream to a protobuf message.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageType"></param>
        /// <param name="bytes"></param>
        /// <param name="index">the starting index in bytes</param>
        /// <returns></returns>
        T Load<T>(DTCMessageType messageType, byte[] bytes, int index = 0) where T : IMessage<T>, new();
    }
}