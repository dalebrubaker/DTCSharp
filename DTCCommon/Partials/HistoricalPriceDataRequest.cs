using System;
using System.Diagnostics;
using DTCCommon.Extensions;

namespace DTCPB
{
    [DebuggerDisplay("{" + nameof(MyToString) + "}")]
    public partial class HistoricalPriceDataRequest
    {
        public DateTime StartDateTimeUtc
        {
            get => startDateTime_.DtcDateTimeToUtc();
            set => startDateTime_ = value.UtcToDtcDateTime();
        }

        public DateTime EndDateTimeUtc
        {
            get => endDateTime_.DtcDateTimeToUtc();
            set => endDateTime_ = value.UtcToDtcDateTime();
        }

        public bool IsZipped
        {
            get => useZLibCompression_ != 0;
            set => useZLibCompression_ = value ? 1u : 0u;
        }

        public string MyToString => $"{symbol_} {StartDateTimeUtc} - {EndDateTimeUtc} MaxDaysToReturn:{MaxDaysToReturn} (UTC) Zip:{IsZipped} {RecordInterval}";
    }
}