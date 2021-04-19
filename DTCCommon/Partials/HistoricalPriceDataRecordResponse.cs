using System;
using System.Diagnostics;
using DTCCommon.Extensions;

// ReSharper disable once CheckNamespace
// ReSharper disable once IdentifierTypo
namespace DTCPB
{
    [DebuggerDisplay("{" + nameof(MyToString) + "}")]
    public partial class HistoricalPriceDataRecordResponse
    {
        public DateTime StartDateTimeUtc
        {
            get => startDateTime_.DtcDateTimeToUtc();
            set => startDateTime_ = value.UtcToDtcDateTime();
        }

        public bool IsFinalRecordBool
        {
            get => isFinalRecord_ != 0;
            set => isFinalRecord_ = value ? 1u : 0u;
        }

        public string MyToString =>
            $"{StartDateTimeUtc}(UTC) O:{OpenPrice} H:{HighPrice} L:{LowPrice} C:{LastPrice} V:{Volume} BV:{BidVolume} AV:{AskVolume} #T{NumTrades} Final{IsFinalRecordBool}";
    }
}