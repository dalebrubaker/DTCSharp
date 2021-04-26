// unset
using DTCPB;
using Google.Protobuf;

namespace DTCCommon.Codecs
{
    public interface ICodecConverter
    {
        /// <summary>
        /// Convert the messageBytes into the correct protocol message type
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="messageBytes">do NOT include header (size and messageType)</param>
        /// <returns></returns>
        T ConvertToProtobuf<T>(DTCMessageType messageType, byte[] messageBytes) where T : IMessage<T>, new();

        /// <summary>
        /// Convert the protocol message into a byte array which does NOT include the header (size and messageType)
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        byte[] ConvertToBuffer<T>(DTCMessageType messageType, T message) where T : IMessage<T>, new();
    }
}