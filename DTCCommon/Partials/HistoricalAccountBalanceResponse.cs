using System;
using DTCCommon;
using Google.Protobuf;

// ReSharper disable once CheckNamespace
// ReSharper disable once IdentifierTypo
namespace DTCPB
{
    public partial class HistoricalAccountBalanceResponse : ICustomDiagnosticMessage
    {
        public DateTime DateTimeUtc => dateTime_.DtcDateTimeWithMillisecondsToUtc();

        public bool IsNoAccountBalances => NoAccountBalances != 0;

        public string ToDiagnosticString()
        {
            var result = $"'{TradeAccount} {CashBalance}";
            return result;
        }
    }
}