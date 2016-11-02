﻿using System;
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

        /// <summary>
        /// Read a header at the beginning of bytes
        /// </summary>
        /// <param name="bytes">the bytes that hold the header at the beginning</param>
        /// <param name="sizeExcludingHeader">the size EXCLUDING the header</param>
        /// <param name="messageType"></param>
        public static void ReadHeader(byte[] bytes, out int sizeExcludingHeader, out DTCMessageType messageType)
        {
            sizeExcludingHeader = BitConverter.ToUInt16(bytes, 0) - 4;
            messageType = (DTCMessageType)BitConverter.ToUInt16(bytes, 2);
        }
    }
}
