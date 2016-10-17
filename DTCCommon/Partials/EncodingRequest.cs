using System;
using System.IO;
using Google.Protobuf;

namespace DTCPB
{
    public partial class EncodingRequest : IMessageDTC
    {
        public short Size => 12;

        public DTCMessageType MessageType => DTCMessageType.EncodingRequest;

        public void Write(BinaryWriter binaryWriter, EncodingEnum currentEncoding)
        {
            switch (currentEncoding)
            {
                case EncodingEnum.BinaryEncoding:
                    binaryWriter.Write((short)(Size + 4));
                    binaryWriter.Write((short)MessageType);
                    binaryWriter.Write(ProtocolVersion);  
                    binaryWriter.Write((int)Encoding); // enum size is 4
                    char[] protocolType = new char[4];
                    for (int i = 0; i < 3 && i < ProtocolType.Length; i++)
                    {
                        protocolType[i] = ProtocolType[i];  
                    }
                    binaryWriter.Write(protocolType); // 3 chars DTC plus null terminator 
                    break;
                case EncodingEnum.BinaryWithVariableLengthStrings:
                case EncodingEnum.JsonEncoding:
                case EncodingEnum.JsonCompactEncoding:
                    throw new NotImplementedException();
                case EncodingEnum.ProtocolBuffers:
                    var bytes = this.ToByteArray();
                    binaryWriter.Write((short)(bytes.Length + 4));
                    binaryWriter.Write((short)MessageType);
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
                    ProtocolVersion = BitConverter.ToInt32(bytes, 0);
                    Encoding = (EncodingEnum)BitConverter.ToInt32(bytes, 4);
                    ProtocolType = BitConverter.ToString(bytes, 8);
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
