﻿using System;
using System.IO;
using Google.Protobuf;

namespace DTCPB
{
    public partial class EncodingRequest
    {
        public static short Size => 12;

        public DTCMessageType MessageType => DTCMessageType.EncodingRequest;

        public void Write(BinaryWriter binaryWriter, EncodingEnum currentEncoding)
        {
            switch (currentEncoding)
            {
                case EncodingEnum.BinaryEncoding:
                    binaryWriter.Write(Size);
                    binaryWriter.Write((short)MessageType);
                    binaryWriter.Write(ProtocolVersion);  
                    binaryWriter.Write((int)Encoding); // enum size is 4
                    var protocolType = ProtocolType.ToCharArray();
                    binaryWriter.Write(protocolType); // 3 chars DTC plus null terminator 
                    break;
                case EncodingEnum.BinaryWithVariableLengthStrings:
                case EncodingEnum.JsonEncoding:
                case EncodingEnum.JsonCompactEncoding:
                    throw new NotImplementedException();
                case EncodingEnum.ProtocolBuffers:
                    binaryWriter.Write(this.ToByteArray());
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(currentEncoding), currentEncoding, null);
            }
        }

        public static EncodingRequest Read(BinaryReader binaryReader, EncodingEnum currentEncoding)
        {
            var bytes = binaryReader.ReadBytes(Size);
            switch (currentEncoding)
            {
                case EncodingEnum.BinaryEncoding:
                    var result = new EncodingRequest
                    {
                        ProtocolVersion = binaryReader.ReadInt32(),
                        Encoding = (EncodingEnum)binaryReader.ReadInt32()
                    };
                    var protocolType = binaryReader.ReadChars(4);
                    result.ProtocolType = new string(protocolType);
                    return result;
                case EncodingEnum.BinaryWithVariableLengthStrings:
                case EncodingEnum.JsonEncoding:
                case EncodingEnum.JsonCompactEncoding:
                    throw new NotImplementedException();
                case EncodingEnum.ProtocolBuffers:
                    return Parser.ParseFrom(bytes);
                default:
                    throw new ArgumentOutOfRangeException(nameof(currentEncoding), currentEncoding, null);
            }
        }
    }
}
