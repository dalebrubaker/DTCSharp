using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DTCPB;
using Google.Protobuf;

namespace DTCCommon.Codecs
{
    public class CodecProtobuf
    {
        private readonly BinaryReader _binaryReader;
        private readonly BinaryWriter _binaryWriter;

        public CodecProtobuf(BinaryReader binaryReader, BinaryWriter binaryWriter)
        {
            _binaryReader = binaryReader;
            _binaryWriter = binaryWriter;
        }

        public void Send<T>(T message) where T : IMessage<T>
        {
            var size = message.CalculateSize();
            var type = DTCMessageType.EncodingRequest;
            _binaryWriter.Write((short)size);
            _binaryWriter.Write((short)type);
            _binaryWriter.Write(message.ToByteArray());
        }

        public T Convert<T>(byte[] bytes) where T : IMessage<T>, new()
        {
            var result = new T();
            result.MergeFrom(bytes);
            return result;
        }


    }
}
