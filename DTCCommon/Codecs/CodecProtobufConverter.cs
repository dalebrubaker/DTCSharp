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

        public IMessage ConvertToProtobuf(DTCMessageType messageType, byte[] messageBytes)
        {
            var result = EmptyProtobufs.GetEmptyProtobuf(messageType);
            result.MergeFrom(messageBytes);
            return result;
        }

        public byte[] ConvertToBuffer(DTCMessageType messageType, IMessage message)
        {
            return message.ToByteArray();
        }
    }
}