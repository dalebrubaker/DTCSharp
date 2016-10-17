using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DTCCommon;
using Google.Protobuf;

namespace DTCPB
{
    public partial class Heartbeat : IMessageDTC
    {
        public short Size => 12;
        public DTCMessageType MessageType => DTCMessageType.Heartbeat;

        public void Write(BinaryWriter binaryWriter, EncodingEnum currentEncoding)
        {
            switch (currentEncoding)
            {
                case EncodingEnum.BinaryEncoding:
                    Utility.WriteHeader(binaryWriter, Size, MessageType);
                    binaryWriter.Write(NumDroppedMessages);
                    binaryWriter.Write(CurrentDateTime); 
                    break;
                case EncodingEnum.BinaryWithVariableLengthStrings:
                case EncodingEnum.JsonEncoding:
                case EncodingEnum.JsonCompactEncoding:
                    throw new NotImplementedException();
                case EncodingEnum.ProtocolBuffers:
                    var bytes = this.ToByteArray();
                    Utility.WriteHeader(binaryWriter, bytes.Length, MessageType);
                    binaryWriter.Write(bytes);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(currentEncoding), currentEncoding, null);
            }
        }

        public void Load(byte[] bytes, EncodingEnum currentEncoding)
        {
            switch (currentEncoding)
            {
                case EncodingEnum.BinaryEncoding:
                    NumDroppedMessages = BitConverter.ToUInt32(bytes, 0);
                    CurrentDateTime = BitConverter.ToInt64(bytes, 4);
                    break;
                case EncodingEnum.BinaryWithVariableLengthStrings:
                case EncodingEnum.JsonEncoding:
                case EncodingEnum.JsonCompactEncoding:
                    throw new NotImplementedException();
                case EncodingEnum.ProtocolBuffers:
                    Parser.ParseFrom(bytes);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(currentEncoding), currentEncoding, null);
            }
        }
    }
}
