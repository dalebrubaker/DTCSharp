using System;
using DTCCommon;
using Google.Protobuf;

// ReSharper disable once CheckNamespace
// ReSharper disable once IdentifierTypo
namespace DTCPB;

public partial class HistoricalOrderFillResponse : ICustomDiagnosticMessage, IComparable<HistoricalOrderFillResponse>
{
    private DateTime _dateTimeLocal;
    private DateTime _dateTimeUtc;

    public DateTime DateTimeUtc
    {
        get
        {
            if (_dateTimeUtc == System.DateTime.MinValue)
            {
                _dateTimeUtc = dateTime_.FromUnixSecondsToDateTimeDTC();
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

    public bool IsNoOrderFills => NoOrderFills != 0;

    /// <summary>
    ///     A numerical value for the execution direction, +1 for Buy/BuyToCover and -1 for Sell/SellShort
    /// </summary>
    public int Sign => BuySell == BuySellEnum.Buy ? 1 : -1;

    public int CompareTo(HistoricalOrderFillResponse other)
    {
        return DateTime.CompareTo(other.DateTime);
    }

    public string ToDiagnosticString()
    {
        var result = $"'{TradeAccount} {Symbol}";
        if (!string.IsNullOrEmpty(Exchange))
        {
            result += $"-{Exchange}";
        }
        result += $" ExecId={UniqueExecutionID} ServerOrderID:{ServerOrderID} {BuySell} {Quantity} @ {Price} {DateTimeLocal:O}(local)";
        if (!string.IsNullOrEmpty(InfoText))
        {
            result += $" InfoText:{InfoText}";
        }
        return result;
    }
}