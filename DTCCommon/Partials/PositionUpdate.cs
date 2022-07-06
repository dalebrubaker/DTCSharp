using System;
using DTCCommon;
using Google.Protobuf;

// ReSharper disable once CheckNamespace
// ReSharper disable once IdentifierTypo
namespace DTCPB;

public partial class PositionUpdate : ICustomDiagnosticMessage
{
    public DateTime EntryDateTimeUtc => ((long)entryDateTime_).FromUnixSecondsToDateTimeDTC();

    public bool IsNoPositions => NoPositions != 0;

    public string ToDiagnosticString()
    {
        var result = $"'{TradeAccount} {Symbol}";
        if (!string.IsNullOrEmpty(Exchange))
        {
            result += $"-{Exchange}";
        }
        result +=
            $" {Quantity} @ {AveragePrice} {EntryDateTimeUtc.ToLocalTime():O}(local) {MarginRequirement} QuantityLimit={QuantityLimit} OpenProfitLoss={OpenProfitLoss} HighPriceDuringPosition={HighPriceDuringPosition} LowPriceDuringPosition={LowPriceDuringPosition}";
        return result;
    }
}