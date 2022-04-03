using System;
using System.Runtime.InteropServices;
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
        [MarshalAs(UnmanagedType.U2)] private readonly ushort Size;
        [MarshalAs(UnmanagedType.U2)] private readonly ushort Type;
        [MarshalAs(UnmanagedType.R4)] private readonly float Price;
        [MarshalAs(UnmanagedType.R4)] private readonly float Volume;
        [MarshalAs(UnmanagedType.U4)] private readonly int DateTime;
        [MarshalAs(UnmanagedType.U2)] private readonly ushort SymbolID;
        [MarshalAs(UnmanagedType.U2)] private readonly AtBidOrAskEnum AtBidOrAsk;
    }

    /// <summary>
    /// See http://www.sierrachart.com/index.php?page=doc/IntradayDataFileFormat.html#DataStructures
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 8)]
    public struct s_IntradayHeader
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] // can't use ByValTStr because it is not null-terminated
        public char[] FileTypeUniqueHeaderID; // Set to the text string: "SCID"

        [MarshalAs(UnmanagedType.U4)]
        public uint HeaderSize; // Set to the header size in bytes.

        [MarshalAs(UnmanagedType.U4)]
        public uint RecordSize; // Set to the record size in bytes.

        [MarshalAs(UnmanagedType.U2)]
        public ushort Version; // Automatically set to the current version. Currently 1.

        [MarshalAs(UnmanagedType.U2)]
        public ushort Unused1; // Not used.

        [MarshalAs(UnmanagedType.U4)]
        public uint UTCStartIndex; // This should be 0.

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 36)]
        public char[] Reserve; // Not used.

        public static int Size => Marshal.SizeOf(new s_IntradayHeader());

        // public static s_IntradayHeader CopyFrom(byte[] bytes)
        // {
        //     var size = Size;
        //     if (bytes.Length < size)
        //     {
        //         size = bytes.Length;
        //     }
        //     IntPtr ptr = Marshal.AllocHGlobal(size);
        //     Marshal.Copy(bytes, 0, ptr, size);
        //     var result = (s_IntradayHeader)Marshal.PtrToStructure(ptr, typeof(s_IntradayHeader));
        //     Marshal.FreeHGlobal(ptr);
        //     return result;
        // }
        //
        // public byte[] GetBytes()
        // {
        //     int size = Size;
        //     byte[] bytes = new byte[size];
        //
        //     IntPtr ptr = Marshal.AllocHGlobal(size);
        //     Marshal.StructureToPtr(this, ptr, true);
        //     Marshal.Copy(ptr, bytes, 0, size);
        //     Marshal.FreeHGlobal(ptr);
        //     return bytes;
        // }
    }

    /// <summary>
    /// See http://www.sierrachart.com/index.php?page=doc/IntradayDataFileFormat.html#DataStructuresIn
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 8)]
    public struct s_IntradayRecord
    {
        /// <summary>
        /// SCDateTimeMS per https://www.sierrachart.com/index.php?page=doc/SCDateTime.html#SCDateTimeMillisecondVariables
        /// </summary>
        [MarshalAs(UnmanagedType.U8)]
        public long StartDateTime;

        [MarshalAs(UnmanagedType.R4)]
        public float OpenPrice;

        [MarshalAs(UnmanagedType.R4)]
        public float HighPrice;

        [MarshalAs(UnmanagedType.R4)]
        public float LowPrice;

        [MarshalAs(UnmanagedType.R4)]
        public float LastPrice;

        [MarshalAs(UnmanagedType.U4)]
        public uint NumTrades;

        [MarshalAs(UnmanagedType.U4)]
        public uint Volume;

        [MarshalAs(UnmanagedType.U4)]
        public uint BidVolume;

        [MarshalAs(UnmanagedType.U4)]
        public uint AskVolume;

        // public s_IntradayRecord(HistoricalPriceDataRecordResponse record)
        // {
        //                     
        //     // Convert the HistoricalPriceDataRecordResponse StartDateTime (unix seconds) to SCID StartDateTime (microseconds since 12/30/1899) to 
        //     // https://www.sierrachart.com/index.php?page=doc/IntradayDataFileFormat.html#s_IntradayRecord__DateTime
        //     // https://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#t_DateTime
        //     var startDateTimeScMicroseconds = record.StartDateTimeUtc.ToScMicroSeconds();
        //
        //     StartDateTime = startDateTimeScMicroseconds;
        //     OpenPrice = (float)record.OpenPrice;
        //     HighPrice = (float)record.HighPrice;
        //     LowPrice = (float)record.LowPrice;
        //     LastPrice = (float)record.LastPrice;
        //     Volume = (uint)record.Volume;
        //     BidVolume = (uint)record.BidVolume;
        //     AskVolume = (uint)record.AskVolume;
        //     NumTrades = record.NumTrades;
        // }

        public static int Size => Marshal.SizeOf(new s_IntradayRecord());

        public DateTime StartDateTimeUtc => StartDateTime.FromScMicroSecondsToDateTime();

        // public static s_IntradayRecord CopyFrom(byte[] bytes)
        // {
        //     int size = Size;
        //     if (bytes.Length < size)
        //     {
        //         size = bytes.Length;
        //     }
        //     IntPtr ptr = Marshal.AllocHGlobal(size);
        //     Marshal.Copy(bytes, 0, ptr, size);
        //     var result = (s_IntradayRecord)Marshal.PtrToStructure(ptr, typeof(s_IntradayRecord));
        //     Marshal.FreeHGlobal(ptr);
        //     return result;
        // }

        // public byte[] GetBytes()
        // {
        //     byte[] bytes = new byte[Size];
        //
        //     IntPtr ptr = Marshal.AllocHGlobal(Size);
        //     Marshal.StructureToPtr(this, ptr, true);
        //     Marshal.Copy(ptr, bytes, 0, Size);
        //     Marshal.FreeHGlobal(ptr);
        //     return bytes;
        // }
    }
}