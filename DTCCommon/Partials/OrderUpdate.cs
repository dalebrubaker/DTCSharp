using System;
using DTCCommon;
using Google.Protobuf;

// ReSharper disable once CheckNamespace
// ReSharper disable once IdentifierTypo
namespace DTCPB
{
    public partial class OrderUpdate : ICustomDiagnosticMessage
    {
        private DateTime _orderReceivedDateTimeUtc;
        private DateTime _orderReceivedDateTimeLocal;
        private DateTime _lastFillDateTimeUtc;
        private DateTime _lastFillDateTimeLocal;
        private DateTime _goodTillDateTimeUtc;
        private DateTime _goodTillDateTimeLocal;
        private DateTime _latestTransactionDateTimeUtc;
        private DateTime _latestTransactionDateTimeLocal;

        public DateTime OrderReceivedDateTimeUtc
        {
            get
            {
                if (_orderReceivedDateTimeUtc == DateTime.MinValue)
                {
                    _orderReceivedDateTimeUtc = orderReceivedDateTime_.FromUnixSecondsToDateTime();
                }
                return _orderReceivedDateTimeUtc;
            }
        }

        public DateTime OrderReceivedDateTimeLocal
        {
            get
            {
                if (_orderReceivedDateTimeLocal == DateTime.MinValue)
                {
                    _orderReceivedDateTimeLocal = OrderReceivedDateTimeUtc.ToLocalTime();
                }
                return _orderReceivedDateTimeLocal;
            }
            set => orderReceivedDateTime_ = value.ToUniversalTime().ToUnixSeconds();
        }

        public DateTime LastFillDateTimeUtc
        {
            get
            {
                if (_lastFillDateTimeUtc == DateTime.MinValue)
                {
                    _lastFillDateTimeUtc = lastFillDateTime_.FromUnixSecondsToDateTime();
                }
                return _lastFillDateTimeUtc;
            }
        }

        public DateTime LastFillDateTimeLocal
        {
            get
            {
                if (_lastFillDateTimeLocal == DateTime.MinValue)
                {
                    _lastFillDateTimeLocal = LastFillDateTimeUtc.ToLocalTime();
                }
                return _lastFillDateTimeLocal;
            }
            set => lastFillDateTime_ = value.ToUniversalTime().ToUnixSeconds();
        }

        public DateTime GoodTillDateTimeUtc
        {
            get
            {
                if (_goodTillDateTimeUtc == DateTime.MinValue)
                {
                    _goodTillDateTimeUtc = goodTillDateTime_.FromUnixSecondsToDateTime();
                }
                return _goodTillDateTimeUtc;
            }
        }

        public DateTime GoodTillDateTimeLocal
        {
            get
            {
                if (_goodTillDateTimeLocal == DateTime.MinValue)
                {
                    _goodTillDateTimeLocal = GoodTillDateTimeUtc.ToLocalTime();
                }
                return _goodTillDateTimeLocal;
            }
            set => goodTillDateTime_ = value.ToUniversalTime().ToUnixSeconds();
        }

        public DateTime LatestTransactionDateTimeUtc
        {
            get
            {
                if (_latestTransactionDateTimeUtc == DateTime.MinValue)
                {
                    _latestTransactionDateTimeUtc = latestTransactionDateTime_.DtcDateTimeWithMillisecondsToUtc();
                }
                return _latestTransactionDateTimeUtc;
            }
        }

        public DateTime LatestTransactionDateTimeLocal
        {
            get
            {
                if (_latestTransactionDateTimeLocal == DateTime.MinValue)
                {
                    _latestTransactionDateTimeLocal = LatestTransactionDateTimeUtc.ToLocalTime();
                }
                return _latestTransactionDateTimeLocal;
            }
            set => latestTransactionDateTime_ = value.ToUniversalTime().ToUnixSeconds();
        }

        public OrderAction OrderAction => BuySell.ToOrderAction(OpenOrClose);

        public bool IsNoOrders
        {
            get => noOrders_ != 0;
            set => noOrders_ = value ? 1u : 0u;
        }
        
        public string ToDiagnosticString()
        {
            var result = $"'{TradeAccount} {Symbol}";
            if (IsNoOrders)
            {
                result += " No Orders'";
                return result;
            }
            if (!string.IsNullOrEmpty(Exchange))
            {
                result += $"-{Exchange}";
            }
            result += $" ClientOrderID={ClientOrderID}";
            result += $" {OrderReceivedDateTimeLocal:O}(local)";
            if (!string.IsNullOrEmpty(ExchangeOrderID))
            {
                result += $" ExchangeOrderID:{ExchangeOrderID}";
            }
            result += $" {OrderStatus} reason={OrderUpdateReason} {BuySell} {OrderQuantity} {OrderType}";
            if (Price1 != 0)
            {
                result += $" Price1={Price1}";
            }
            if (Price2 != 0)
            {
                result += $" Price2={Price2}";
            }
            result += $" {TimeInForce}";
            if (TimeInForce == TimeInForceEnum.TifGoodTillDateTime)
            {
                result += $" GoodTillDateTime={GoodTillDateTimeLocal}(local)";
            }
            if (!string.IsNullOrEmpty(InfoText))
            {
                result += $" InfoText:{InfoText}";
            }
            if (FilledQuantity != 0)
            {
                result += $" FilledQuantity={FilledQuantity}";
            }
            if (RemainingQuantity != 0)
            {
                result += $" RemainingQuantity={RemainingQuantity}";
            }
            if (AverageFillPrice != 0)
            {
                result += $" AverageFillPrice={AverageFillPrice}";
            }
            if (LastFillPrice != 0)
            {
                result += $" LastFillPrice={LastFillPrice}";
            }
            if (LastFillPrice != 0)
            {
                result += $" LastFillPrice={LastFillPrice}";
            }
            if (LastFillQuantity != 0)
            {
                result += $" LastFillQuantity={LastFillQuantity}";
            }
            if (!string.IsNullOrEmpty(LastFillExecutionID))
            {
                result += $" LastFillExecutionID:{LastFillExecutionID}";
            }
            if (LastFillDateTime > 0)
            {
                result += $" LastFillDateTime={LastFillDateTimeLocal:O}(local)";
            }
            if (!string.IsNullOrEmpty(ParentServerOrderID))
            {
                result += $" ParentServerOrderID:{ParentServerOrderID}";
            }
            if (!string.IsNullOrEmpty(FreeFormText))
            {
                result += $" FreeFormText:{FreeFormText}";
            }
            if (!string.IsNullOrEmpty(PreviousClientOrderID))
            {
                result += $" PreviousClientOrderID:{PreviousClientOrderID}";
            }
            result += $" ServerOrderID={ServerOrderID}";
            if (!string.IsNullOrEmpty(PreviousServerOrderID))
            {
                result += $" PreviousServerOrderID:{PreviousServerOrderID}";
            }
            if (!string.IsNullOrEmpty(OCOLinkedOrderServerOrderID))
            {
                result += $" OCOLinkedOrderServerOrderID:{OCOLinkedOrderServerOrderID}";
            }
            result += "'";
            return result;
        }
    }
}