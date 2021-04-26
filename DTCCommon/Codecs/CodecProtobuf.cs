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

        public override async Task WriteAsync<T>(DTCMessageType messageType, T message, CancellationToken cancellationToken)
        {
            if (_disabledHeartbeats && messageType == DTCMessageType.Heartbeat)
            {
                return;
            }
            var bytes = message.ToByteArray();
            using var bufferBuilder = new BufferBuilder(4 + bytes.Length, this);
            bufferBuilder.AddHeader(messageType);
            bufferBuilder.Add(bytes);
            await bufferBuilder.WriteAsync(_stream, cancellationToken).ConfigureAwait(false);
        }

        public override T Load<T>(DTCMessageType messageType, byte[] bytes)
        {
            var index = 0;
            try
            {
                var result = new T();
                if (messageType == DTCMessageType.EncodingRequest)
                {
                    // EncodingRequest comes back as binary for all protocol versions
                    LoadEncodingRequest(bytes, index, ref result);
                    return result;
                }
                if (messageType == DTCMessageType.EncodingResponse)
                {
                    // EncodingResponse comes back as binary for all protocol versions
                    LoadEncodingResponse(bytes, index, ref result);
                    return result;
                }
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