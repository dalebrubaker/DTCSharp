using DTCPB;

namespace DTCCommon
{
    /// <summary>
    /// Pairs messageType with the message bytes as they are encoded over the wire, according to the current protocol (e.g. BinaryEncoding, ProtocolBuffers... 
    /// </summary>
    public class MessageEncoded
    {
        public bool IsExtended { get; } // using MessageTypeExtended instead of MessageType

        /// <summary>
        /// Pairs messageType with the corresponding Protobuf IMessage
        /// </summary>
        public DTCSharpMessageType MessageTypeExtended { get; private set; }

        public DTCMessageType MessageType { get; private set; }

        /// <summary>
        /// The message bytes as they are encoded over the wire, according to the current protocol (e.g. BinaryEncoding, ProtocolBuffers...
        /// These do NOT include the header size/type
        /// </summary>
        public byte[] MessageBytes { get; }

        public MessageEncoded(DTCSharpMessageType messageType, byte[] messageBytes)
        {
            MessageTypeExtended = messageType;
            MessageBytes = messageBytes;
            IsExtended = true;
        }

        public MessageEncoded(DTCMessageType messageType, byte[] messageBytes)
        {
            MessageType = messageType;
            MessageBytes = messageBytes;
        }

        public MessageEncoded(short messageTypeShort, byte[] messageBytes)
        {
            if (messageTypeShort >= 10000)
            {
                MessageTypeExtended = (DTCSharpMessageType)messageTypeShort;
                IsExtended = true;
            }
            else
            {
                MessageType = (DTCMessageType)messageTypeShort;
            }
            MessageBytes = messageBytes;
        }

        internal short MessageTypeAsShort => IsExtended ? (short)MessageTypeExtended : (short)MessageType;

        public override string ToString()
        {
            return IsExtended ? $"{MessageTypeExtended}: count={MessageBytes.Length:N0} bytes" : $"{MessageType}: count={MessageBytes.Length:N0} bytes";
        }
    }
}