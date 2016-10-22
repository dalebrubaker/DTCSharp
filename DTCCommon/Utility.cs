using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DTCPB;

namespace DTCCommon
{
    public static class Utility
    {
        /// <summary>
        /// Write a header
        /// </summary>
        /// <param name="binaryWriter"></param>
        /// <param name="sizeExcludingHeader">the size EXCLUDING the header</param>
        /// <param name="messageType"></param>
        public static void WriteHeader(BinaryWriter binaryWriter, int sizeExcludingHeader, DTCMessageType messageType)
        {
            binaryWriter.Write((ushort)(sizeExcludingHeader + 4));
            binaryWriter.Write((ushort)messageType);
        }
    }
}
