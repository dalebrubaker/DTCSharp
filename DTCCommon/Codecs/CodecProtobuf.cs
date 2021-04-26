using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DTCPB;
using Google.Protobuf;

namespace DTCCommon.Codecs
{
    public class CodecProtobuf : Codec
    {
        public CodecProtobuf(Stream stream) : base(stream)
        {
        }

        public override EncodingEnum Encoding => EncodingEnum.ProtocolBuffers;

        public override void Write<T>(DTCMessageType messageType, T message)
        {
            if (_disabledHeartbeats && messageType == DTCMessageType.Heartbeat)
            {
                return;
            }
            var bytes = message.ToByteArray();
            using var bufferBuilder = new BufferBuilderOBS(4 + bytes.Length, this);
            bufferBuilder.AddHeader(messageType);
            bufferBuilder.Add(bytes);
            bufferBuilder.Write(CurrentStream);

        }

        public override T Load<T>(DTCMessageType messageType, byte[] bytes)
        {
            try
            {
                var result = new T();
                result.MergeFrom(bytes);
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, ex.Message);
                throw;
            }
        }
    }
}