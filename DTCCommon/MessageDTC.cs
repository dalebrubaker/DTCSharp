using DTCPB;
using Google.Protobuf;

namespace DTCCommon
{
    public class MessageDTC
    {
        public DTCMessageType MessageType { get; }
        
        /// <summary>
        /// The message in Protocol Buffer form
        /// </summary>
        public IMessage Message { get; }

        public MessageDTC(DTCMessageType messageType, IMessage message)
        {
            MessageType = messageType;
            Message = message;
        }

        public override string ToString()
        {
            return $"{MessageType}: {GetType().Name}: {Message}";
        }
    }
}