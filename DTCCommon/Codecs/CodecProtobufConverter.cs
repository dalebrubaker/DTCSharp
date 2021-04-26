using DTCPB;
using Google.Protobuf;

namespace DTCCommon.Codecs
{
    public class CodecProtobufConverter : ICodecConverter
    {
        public T ConvertToProtobuf<T>(DTCMessageType messageType, byte[] messageBytes) where T : IMessage<T>, new()
        {
            var result = new T();
            result.MergeFrom(messageBytes);
            return result;
        }

        public byte[] ConvertToBuffer<T>(DTCMessageType messageType, T message) where T : IMessage<T>, new()
        {
            return message.ToByteArray();
        }
    }
}