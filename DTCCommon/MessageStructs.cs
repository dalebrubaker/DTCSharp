using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using DTCPB;
using uint8_t = System.Byte;
using uint16_t = System.UInt16;
using u_int16 = System.UInt16;
using u_int32 = System.UInt32;
using int32_t = System.Int32;
using t_DateTime4Byte = System.Int32;

// ReSharper disable InconsistentNaming

namespace DTCCommon
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 8)]
    public struct s_MarketDataUpdateTradeCompact
    {
        [MarshalAs(UnmanagedType.U2)] uint16_t Size;
        [MarshalAs(UnmanagedType.U2)] uint16_t Type;
        [MarshalAs(UnmanagedType.R4)] float Price;
        [MarshalAs(UnmanagedType.R4)] float Volume;
        [MarshalAs(UnmanagedType.U4)] t_DateTime4Byte DateTime;
        [MarshalAs(UnmanagedType.U2)] uint16_t SymbolID;
        [MarshalAs(UnmanagedType.U2)] AtBidOrAskEnum AtBidOrAsk;
    }

    /// <summary>
    /// See http://www.sierrachart.com/index.php?page=doc/IntradayDataFileFormat.html#DataStructures
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 8)]
    public struct s_IntradayHeader
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 4)]
        public string FileTypeUniqueHeaderID;  // Set to the text string: "SCID"
        [MarshalAs(UnmanagedType.U4)]
        public u_int32 HeaderSize; // Set to the header size in bytes.
        [MarshalAs(UnmanagedType.U4)]
        public u_int32 RecordSize; // Set to the record size in bytes.
        [MarshalAs(UnmanagedType.U2)]
        public u_int16 Version; // Automatically set to the current version. Currently 1.
        [MarshalAs(UnmanagedType.U2)]
        public u_int16 Unused1; // Not used.
        [MarshalAs(UnmanagedType.U4)]
        public u_int32 UTCStartIndex;  // This should be 0.
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 36)]
        public string Reserve; // Not used.

        public int Size => Marshal.SizeOf(this);

        public void CopyFrom(byte[] bytes)
        {
            int size = Size;
            if (bytes.Length < size)
            {
                size = bytes.Length;
            }
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(bytes, 0, ptr, size);
            this = (s_IntradayHeader)Marshal.PtrToStructure(ptr, typeof(s_IntradayHeader));
            Marshal.FreeHGlobal(ptr);
        }

        public byte[] GetBytes(s_IntradayHeader str)
        {
            int size = Size;
            byte[] bytes = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(str, ptr, true);
            Marshal.Copy(ptr, bytes, 0, size);
            Marshal.FreeHGlobal(ptr);
            return bytes;
        }
    };

    /// <summary>
    /// See http://www.sierrachart.com/index.php?page=doc/IntradayDataFileFormat.html#DataStructuresIn
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 8)]
    public struct s_IntradayRecord
    {
        [MarshalAs(UnmanagedType.R8)]
        public double DateTime;
        [MarshalAs(UnmanagedType.R4)]
        public float Open;
        [MarshalAs(UnmanagedType.R4)]
        public float High;
        [MarshalAs(UnmanagedType.R4)]
        public float Low;
        [MarshalAs(UnmanagedType.R4)]
        public float Close;
        [MarshalAs(UnmanagedType.U4)]
        public u_int32 NumTrades;
        [MarshalAs(UnmanagedType.U4)]
        public u_int32 TotalVolume;
        [MarshalAs(UnmanagedType.U4)]
        public u_int32 BidVolume;
        [MarshalAs(UnmanagedType.U4)]
        public u_int32 AskVolume;

        public int Size => Marshal.SizeOf(this);

        public void CopyFrom(byte[] bytes)
        {
            int size = Size;
            if (bytes.Length < size)
            {
                size = bytes.Length;
            }
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(bytes, 0, ptr, size);
            this = (s_IntradayRecord)Marshal.PtrToStructure(ptr, typeof(s_IntradayRecord));
            Marshal.FreeHGlobal(ptr);
        }

        public byte[] GetBytes(s_IntradayRecord str)
        {
            byte[] bytes = new byte[Size];

            IntPtr ptr = Marshal.AllocHGlobal(Size);
            Marshal.StructureToPtr(str, ptr, true);
            Marshal.Copy(ptr, bytes, 0, Size);
            Marshal.FreeHGlobal(ptr);
            return bytes;
        }
    };

}
