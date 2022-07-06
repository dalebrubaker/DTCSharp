namespace DTCCommon.Codecs;
// TODO It might be rather straightforward to convert between protobuf and JSON. Maybe not so for JsonCompact
// https://developers.google.com/protocol-buffers/docs/reference/java/com/google/protobuf/util/JsonFormat
// https://developers.google.com/protocol-buffers/docs/reference/csharp/class/google/protobuf/json-formatter
// https://stackoverflow.com/questions/28545401/java-json-protobuf-back-conversion
// https://stackoverflow.com/questions/55363176/alternatives-to-protobufs-json-format-to-convert-protobuf-message-to-json

public class CodecJsonConverter
{
    // /// <summary>
    // /// This is the Func used when the current encoding is EncodingEnum.JsonEncoding
    // /// </summary>
    // /// <param name="messageProto"></param>
    // /// <returns>MessageEncoded with bytes for Json</returns>
    // public static MessageEncoded EncodeJson(MessageProto messageProto)
    // {
    //     var result = new MessageEncoded(messageProto.MessageType, bytes);
    //     return result;
    // }
    //
    // /// <summary>
    // /// This is the Func used when the current encoding is EncodingEnum.JsonEncoding
    // /// </summary>
    // /// <param name="messageEncoded">MessageEncoded with bytes for Json</param>
    // /// <returns></returns>
    // public static MessageProto DecodeJson(MessageEncoded messageEncoded)
    // {
    //     var message = EmptyProtobufs.GetEmptyProtobuf(messageEncoded.MessageType);
    //     message.MergeFrom(messageEncoded.MessageBytes);
    //     var result = new MessageProto(messageEncoded.MessageType, message);
    //     return result;
    // }
}