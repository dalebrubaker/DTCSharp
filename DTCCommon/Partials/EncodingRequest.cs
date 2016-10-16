using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTCPB
{
    public partial class EncodingRequest
    {
        public short Size => 12;

        public DTCMessageType MessageType => DTCMessageType.EncodingRequest;

        public void WriteBinary(BinaryWriter binaryWriter)
        {
            binaryWriter.Write(Size);
            binaryWriter.Write((short)MessageType);
            binaryWriter.Write(ProtocolVersion);
            binaryWriter.Write((int)Encoding);
            binaryWriter.Write(ProtocolType);
        }
    }
}
