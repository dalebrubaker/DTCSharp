using System;
using DTCCommon;
using Google.Protobuf;

// ReSharper disable once CheckNamespace
// ReSharper disable once IdentifierTypo
namespace DTCPB;

public partial class HistoricalPriceDataTickRecordResponse : ICustomDiagnosticMessage
{
    private DateTime _dateTimeLocal;
    private DateTime _dateTimeUtc;

    public DateTime DateTimeUtc
    {
        get
        {
            if (_dateTimeUtc == System.DateTime.MinValue)
            {
                _dateTimeUtc = dateTime_.DtcDateTimeWithMillisecondsToUtc();
            }
            return _dateTimeUtc;
        }
    }

    public DateTime DateTimeLocal
    {
        get
        {
            if (_dateTimeLocal == System.DateTime.MinValue)
            {
                _dateTimeLocal = DateTimeUtc.ToLocalTime();
            }
            return _dateTimeLocal;
        }
    }

    public bool IsFinalRecordBool
    {
        get => isFinalRecord_ != 0;
        set => isFinalRecord_ = value ? 1u : 0u;
    }

    public string ToDiagnosticString()
    {
        return $"{DateTimeLocal}(Local) P:{Price} V:{Volume} AtBidOrAskEnum:{AtBidOrAsk} Final{IsFinalRecordBool}";
    }
}