using System;
using DTCCommon;
using Google.Protobuf;

// ReSharper disable once CheckNamespace
// ReSharper disable once IdentifierTypo
namespace DTCPB
{
    public partial class MarketDataUpdateBidAskCompact : ICustomDiagnosticMessage
    {
        private DateTime _timestampLocal;

        public DateTime TimestampLocal
        {
            get
            {
                var dateTimeUtc = dateTime_.DtcDateTime4ByteToUtc();
                _timestampLocal = dateTimeUtc.ToLocalTime();
                return _timestampLocal;
            }
            set
            {
                var timestampUtc = value.ToUniversalTime();
                DateTime = timestampUtc.UtcToDtcDateTime4Byte();
            }
        }

        public string ToDiagnosticString()
        {
            return $"{TimestampLocal:yyyyMMdd.HHmmss}(Local) B:{BidQuantity:N0}@{BidPrice} A:{AskQuantity:N0}@{AskPrice}";
        }
    }
}