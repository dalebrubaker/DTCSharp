using System;
using DTCCommon.Extensions;
using Google.Protobuf;

// ReSharper disable once CheckNamespace
// ReSharper disable once IdentifierTypo
namespace DTCPB
{
    public partial class HistoricalPriceDataRecordResponse : ICustomDiagnosticMessage
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

        public string ToDiagnosticString()
        {
            return
                $"{StartDateTimeUtc}(UTC) O:{OpenPrice} H:{HighPrice} L:{LowPrice} C:{LastPrice} V:{Volume} BV:{BidVolume} AV:{AskVolume} #T{NumTrades} Final{IsFinalRecordBool}";
        }
    }
}