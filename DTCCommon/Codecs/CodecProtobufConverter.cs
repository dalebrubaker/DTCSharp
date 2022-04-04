using System;
using Google.Protobuf;
using NLog;

namespace DTCCommon.Codecs
{
    public static class CodecProtobufConverter
    {
        private static readonly Logger s_logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// This is the Func used when the current encoding is EncodingEnum.ProtocolBuffers
        /// </summary>
        /// <param name="messageProto"></param>
        /// <returns>MessageEncoded with bytes for ProtocolBuffers</returns>
        public static MessageEncoded EncodeProtobuf(MessageProto messageProto)
        {
            var bytes = messageProto.Message.ToByteArray();
            return messageProto.IsExtended ? new MessageEncoded(messageProto.MessageTypeExtended, bytes) : new MessageEncoded(messageProto.MessageType, bytes);
        }

        /// <summary>
        /// This is the Func used when the current encoding is EncodingEnum.ProtocolBuffers
        /// </summary>
        /// <param name="messageEncoded"></param>
        /// <returns></returns>
        public static MessageProto DecodeProtobuf(MessageEncoded messageEncoded)
        {
            if (messageEncoded.IsExtended)
            {
                var message1 = EmptyProtobufs.GetEmptyProtobuf(messageEncoded.MessageTypeExtended);
                message1.MergeFrom(messageEncoded.MessageBytes);
                var result1 = new MessageProto(messageEncoded.MessageTypeExtended, message1);
                return result1;
            }
            try
            {
                var message = EmptyProtobufs.GetEmptyProtobuf(messageEncoded.MessageType);
                message.MergeFrom(messageEncoded.MessageBytes);
                var result = new MessageProto(messageEncoded.MessageType, message);
                return result;
            }
            catch (Exception ex)
            {
                s_logger.Error(ex, $"MessageType={messageEncoded.MessageType} {ex.Message}");
                throw;
            }
        }
    }
}