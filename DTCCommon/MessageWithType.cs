using DTCPB;
using Google.Protobuf;

namespace DTCCommon
{
    public class MessageWithType
    {
        public DTCMessageType MessageType { get; }
        public IMessage Message { get; }

        public MessageWithType(DTCMessageType messageType, IMessage message)
        {
            MessageType = messageType;
            Message = message;
        }

        public override string ToString()
        {
            return $"{MessageType}: {GetType().Name}";
        }
    }
}