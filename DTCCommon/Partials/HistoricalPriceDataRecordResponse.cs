using System;
using DTCCommon;
using Google.Protobuf;

// ReSharper disable once CheckNamespace
// ReSharper disable once IdentifierTypo
namespace DTCPB;

public partial class HistoricalPriceDataRecordResponse : ICustomDiagnosticMessage
{
    private DateTime _startDateTimeLocal;
    private DateTime _startDateTimeUtc;

    public DateTime StartDateTimeUtc
    {
        get
        {
            if (_startDateTimeUtc == DateTime.MinValue)
            {
                _startDateTimeUtc = startDateTime_.FromUnixSecondsToDateTimeDTC();
            }
            return _startDateTimeUtc;
        }
    }

    public DateTime StartDateTimeLocal
    {
        get
        {
            if (_startDateTimeLocal == DateTime.MinValue)
            {
                _startDateTimeLocal = StartDateTimeUtc.ToLocalTime();
            }
            return _startDateTimeLocal;
        }
    }

    public bool IsFinalRecordBool
    {
        get => isFinalRecord_ != 0;
        set => isFinalRecord_ = value ? 1u : 0u;
    }

    public bool IsOneTick => NumTrades == 1 && OpenPrice == 0;

    public string ToDiagnosticString()
    {
        return $"{StartDateTimeLocal:yyyyMMdd.HHmmss}(Local) O:{OpenPrice} H:{HighPrice} L:{LowPrice} C:{LastPrice} V:{Volume} BV:{BidVolume} AV:{AskVolume} #T:{NumTrades} "
               + $"FinalRecord={IsFinalRecordBool}";
    }
}