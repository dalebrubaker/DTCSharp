using Google.Protobuf;

// ReSharper disable once CheckNamespace
namespace DTCPB
{
    public partial class LogonResponse : ICustomDiagnosticMessage
    {
        public bool IsMarketDepthUpdatesBestBidAndAsk => MarketDepthUpdatesBestBidAndAsk != 0;
        public bool IsTradingSupported => TradingIsSupported != 0;
        public bool IsOcoOrdersSupported => OCOOrdersSupported != 0;
        public bool IsOrderCancelReplaceSupported => OrderCancelReplaceSupported != 0;
        public bool IsSecurityDefinitionsSupported => SecurityDefinitionsSupported != 0;
        public bool IsBracketOrdersSupported => BracketOrdersSupported != 0;
        public bool IsMarketDataSupported => MarketDataSupported != 0;

        public string ToDiagnosticString()
        {
            var result = "";
            if (!string.IsNullOrEmpty(ServerName))
            {
                result += ServerName;
            }
            result += $" {Result}";
            if (!string.IsNullOrEmpty(ResultText))
            {
                result += $" {ResultText}";
            }
            return result;
        }
    }
}