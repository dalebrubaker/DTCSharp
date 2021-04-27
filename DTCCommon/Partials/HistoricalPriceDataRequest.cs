using System;
using DTCCommon.Extensions;
using Google.Protobuf;

namespace DTCPB
{
    public partial class HistoricalPriceDataRequest : ICustomDiagnosticMessage
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

        public bool UseZLibCompressionBool
        {
            get => useZLibCompression_ != 0;
            set => useZLibCompression_ = value ? 1u : 0u;
        }

        public string ToDiagnosticString()
        {
            return $"{symbol_} {StartDateTimeUtc} - {EndDateTimeUtc} MaxDaysToReturn:{MaxDaysToReturn} (UTC) Zip:{UseZLibCompressionBool} {RecordInterval}";
        }
    }
}