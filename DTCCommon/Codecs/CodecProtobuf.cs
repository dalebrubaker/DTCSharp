using System.IO;
using DTCPB;

namespace DTCCommon.Codecs
{
    public class CodecProtobuf : Codec
    {
        public CodecProtobuf(Stream stream) : base(stream, new CodecProtobufConverter())
        {
        }

        public override EncodingEnum Encoding => EncodingEnum.ProtocolBuffers;
    }
}