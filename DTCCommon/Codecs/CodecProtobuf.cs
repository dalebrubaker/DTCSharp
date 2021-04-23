using System.IO;
using DTCCommon.Enums;
using DTCPB;
using Google.Protobuf;

namespace DTCCommon.Codecs
{
    public class CodecProtobuf : Codec
    {
        public CodecProtobuf(Stream stream, ClientOrServer clientOrServer) : base(stream, clientOrServer)
        {

        }

        public override void Write<T>(DTCMessageType messageType, T message)
        {
            if (_binaryWriter == null)
            {
                return;
            }
            if (_disabledHeartbeats && messageType == DTCMessageType.Heartbeat)
            {
                return;
            }
            if (messageType is DTCMessageType.EncodingRequest)
            {
                // EncodingRequest goes as binary for all protocol versions
                WriteEncodingRequest(messageType, message);
                return;
            }
            if (messageType == DTCMessageType.EncodingResponse)
            {
                // EncodingResponse goes as binary for all protocol versions
                WriteEncodingResponse(messageType, message);
                return;
            }
            var bytes = message.ToByteArray();
            Utility.WriteHeader(_binaryWriter, bytes.Length, messageType);
            _binaryWriter.Write(bytes);
        }

        public override T Load<T>(DTCMessageType messageType, byte[] bytes, int index = 0)
        {
            var result = new T();
            if (messageType == DTCMessageType.EncodingRequest)
            {
                // EncodingRequest comes back as binary for all protocol versions
                LoadEncodingRequest<T>(bytes, index, ref result);
                return result;
            }
            if (messageType == DTCMessageType.EncodingResponse)
            {
                // EncodingResponse comes back as binary for all protocol versions
                LoadEncodingResponse<T>(bytes, index, ref result);
                return result;
            }
            result.MergeFrom(bytes);
            return result;
        }
    }
}