using System;
using DTCCommon.Extensions;
using Google.Protobuf;

// ReSharper disable once CheckNamespace
// ReSharper disable once IdentifierTypo
namespace DTCPB
{
    public partial class HistoricalPriceDataTickRecordResponse : ICustomDiagnosticMessage
    {
        public DateTime StartDateTimeUtc
        {
            get => dateTime_.DtcIntradayDateTimeWithMillisecondsToUtc();
            set => dateTime_ = value.UtcToDtcDateTime();
        }

        public bool IsFinalRecordBool
        {
            get => isFinalRecord_ != 0;
            set => isFinalRecord_ = value ? 1u : 0u;
        }

        public string ToDiagnosticString()
        {
            return
                $"{StartDateTimeUtc}(UTC) O:{Price} V:{Volume} AtBidOrAsk:{AtBidOrAsk} Final{IsFinalRecordBool}";
        }
    }
}