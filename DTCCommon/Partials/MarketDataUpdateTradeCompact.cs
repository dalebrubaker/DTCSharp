using System;
using DTCCommon;
using Google.Protobuf;

// ReSharper disable once CheckNamespace
// ReSharper disable once IdentifierTypo
namespace DTCPB;

public partial class MarketDataUpdateTradeCompact : ICustomDiagnosticMessage
{
    private DateTime? _timestampLocal;

    public DateTime TimestampLocal
    {
        get
        {
            var dateTimeUtc = dateTime_.DtcDateTime4ByteToUtc();
            _timestampLocal = dateTimeUtc.ToLocalTime();
            return _timestampLocal.Value;
        }
        set
        {
            var timestampUtc = value.ToUniversalTime();
            dateTime_ = timestampUtc.UtcToDtcDateTime4Byte();
        }
    }

    /// <summary>
    ///     Used for communication from Instrument through the MarketDataUpdate event
    ///     This is Price divided by TickSize
    /// </summary>
    public long PriceT { get; set; }

    public long BestBidT { get; set; }

    public long BestAskT { get; set; }

    public float BestBid { get; set; }

    public float BestAsk { get; set; }

    public string ToDiagnosticString()
    {
        return $"{TimestampLocal:yyyyMMdd.HHmmss}(Local) L:{Price} V:{Volume} Aggressor:{AtBidOrAsk}";
    }
}