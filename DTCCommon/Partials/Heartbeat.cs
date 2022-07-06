using System.Globalization;
using DTCCommon;
using Google.Protobuf;

// ReSharper disable once CheckNamespace
namespace DTCPB;

public partial class Heartbeat : ICustomDiagnosticMessage
{
    public string ToDiagnosticString()
    {
        return $"{CurrentDateTime.DtcDateTimeToUtc().ToLocalTime().ToString(CultureInfo.InvariantCulture)} Dropped={NumDroppedMessages:N0}";
    }
}