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

        public void Write(BinaryWriter binaryWriter, EncodingEnum encodingEnum)
        {
            
        }
    }
}
