// ReSharper disable once CheckNamespace
using System;
using DTCPB;

// ReSharper disable once CheckNamespace
namespace DTCCommon
{
    public static class EnumExtensions
    {
        public static OrderAction ToOrderAction(this BuySellEnum buySell, OpenCloseTradeEnum openClose)
        {
            switch (buySell)
            {
                case BuySellEnum.Buy:
                    switch (openClose)
                    {
                        case OpenCloseTradeEnum.TradeOpen:
                            return OrderAction.Buy;
                        case OpenCloseTradeEnum.TradeClose:
                            return OrderAction.BuyToCover;
                        case OpenCloseTradeEnum.TradeUnset:
                            return OrderAction.Unset;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(openClose), openClose, null);
                    }
                case BuySellEnum.Sell:
                    switch (openClose)
                    {
                        case OpenCloseTradeEnum.TradeOpen:
                            return OrderAction.SellShort;
                        case OpenCloseTradeEnum.TradeClose:
                            return OrderAction.Sell;
                        case OpenCloseTradeEnum.TradeUnset:
                            return OrderAction.Unset;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(openClose), openClose, null);
                    }
                case BuySellEnum.BuySellUnset:
                    return OrderAction.Unset;
                default:
                    throw new ArgumentOutOfRangeException(nameof(buySell), buySell, null);
            }
        }

        public static (BuySellEnum, OpenCloseTradeEnum) FromOrderAction(this OrderAction orderAction)
        {
            switch (orderAction)
            {
                case OrderAction.Buy:
                    return (BuySellEnum.Buy, OpenCloseTradeEnum.TradeOpen);
                case OrderAction.BuyToCover:
                    return (BuySellEnum.Buy, OpenCloseTradeEnum.TradeClose);
                case OrderAction.Sell:
                    return (BuySellEnum.Sell, OpenCloseTradeEnum.TradeClose);
                case OrderAction.SellShort:
                    return (BuySellEnum.Sell, OpenCloseTradeEnum.TradeOpen);
                case OrderAction.Unset:
                    return (BuySellEnum.BuySellUnset, OpenCloseTradeEnum.TradeUnset);
                default:
                    throw new ArgumentOutOfRangeException(nameof(orderAction), orderAction, null);
            }
        }

        public static BuySellEnum ToSide(this OrderAction orderAction)
        {
            return orderAction switch
            {
                OrderAction.Buy => BuySellEnum.Buy,
                OrderAction.BuyToCover => BuySellEnum.Buy,
                OrderAction.Sell => BuySellEnum.Sell,
                OrderAction.SellShort => BuySellEnum.Sell,
                OrderAction.Unset => BuySellEnum.BuySellUnset,
                _ => throw new ArgumentOutOfRangeException(nameof(orderAction), orderAction, null)
            };
        }

        /// <summary>
        /// Convert from Buy to SellShort and vice versa, going from Long to Short or vice versa
        /// </summary>
        /// <param name="orderAction"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static OrderAction Reverse(this OrderAction orderAction)
        {
            switch (orderAction)
            {
                case OrderAction.Buy:
                    return OrderAction.SellShort;
                case OrderAction.BuyToCover:
                    return OrderAction.Sell;
                case OrderAction.Sell:
                    return OrderAction.BuyToCover;
                case OrderAction.SellShort:
                    return OrderAction.Buy;
                case OrderAction.Unset:
                default:
                    throw new ArgumentOutOfRangeException(nameof(orderAction), orderAction, null);
            }
        }
        
        /// <summary>
        /// Return the exit order action corresponding to an entry order action
        /// </summary>
        /// <param name="orderAction"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="DTCSharpException"></exception>
        public static OrderAction Exit(this OrderAction orderAction)
        {
            switch (orderAction)
            {
                case OrderAction.Buy:
                    return OrderAction.Sell;
                case OrderAction.SellShort:
                    return OrderAction.BuyToCover;
                case OrderAction.BuyToCover:
                case OrderAction.Sell:
                    throw new DTCSharpException("This is for exiting an entry, not for reversing;");
                case OrderAction.Unset:
                default:
                    throw new ArgumentOutOfRangeException(nameof(orderAction), orderAction, null);
            }
        }
        public static string Abbrev(this TimeInForceEnum tif)
        {
            switch (tif)
            {
                case TimeInForceEnum.TifUnset:
                    return ""; // the default we leave blank
                case TimeInForceEnum.TifDay:
                    return "Day";
                case TimeInForceEnum.TifGoodTillCanceled:
                    return "GTC";
                case TimeInForceEnum.TifGoodTillDateTime:
                    return "GTD";
                case TimeInForceEnum.TifImmediateOrCancel:
                    return "IoC";
                case TimeInForceEnum.TifAllOrNone:
                    return "AoN";
                case TimeInForceEnum.TifFillOrKill:
                    return "FoK";
                default:
                    throw new ArgumentOutOfRangeException(nameof(tif), tif, null);
            }
        }
        
        public static TimeInForceEnum ToTimeInForceEnumFromAbbrev(this string str)
        {
            switch (str)
            {
                case "Day":
                    return TimeInForceEnum.TifDay;
                case "GTC":
                    return TimeInForceEnum.TifGoodTillCanceled;
                case "GTD":
                    return TimeInForceEnum.TifGoodTillDateTime;
                case "IoC":
                    return TimeInForceEnum.TifImmediateOrCancel;
                case "AoN":
                    return TimeInForceEnum.TifAllOrNone;
                case "FoK":
                    return TimeInForceEnum.TifFillOrKill;
                default:
                    return TimeInForceEnum.TifUnset;
            }
        }

        public static string Abbrev(this OrderAction orderAction)
        {
            switch (orderAction)
            {
                case OrderAction.Unset:
                    return "";
                case OrderAction.Buy:
                    return "Buy";
                case OrderAction.BuyToCover:
                    return "Cover";
                case OrderAction.Sell:
                    return "Sell";
                case OrderAction.SellShort:
                    return "Short";
                default:
                    throw new ArgumentOutOfRangeException(nameof(orderAction), orderAction, null);
            }
        }

        public static OrderAction ToOrderActionFromAbbrev(this string str)
        {
            switch (str)
            {
                case "Buy":
                    return OrderAction.Buy;
                case "Sell":
                    return OrderAction.Sell;
                case "Cover":
                    return OrderAction.BuyToCover;
                case "Short":
                    return OrderAction.SellShort;
                default:
                    return OrderAction.Unset;
            }
        }
        
        public static string Abbrev(this OrderTypeEnum orderType)
        {
            return orderType.ToString().Substring(9);
        }
        
        public static string Abbrev(this OrderStatusEnum orderStatus)
        {
            return orderStatus.ToString().Replace("OrderStatus", "");
        }

        public static OrderStatusEnum ToOrderStatusEnumFromAbbrev(this string str)
        {
            return str switch
            {
                "OrderSent" => OrderStatusEnum.OrderStatusOrderSent,
                "PendingOpen" => OrderStatusEnum.OrderStatusPendingOpen,
                "PendingChild" => OrderStatusEnum.OrderStatusPendingChild,
                "Open" => OrderStatusEnum.OrderStatusOpen,
                "PendingCancelReplace" => OrderStatusEnum.OrderStatusPendingCancelReplace,
                "PendingCancel" => OrderStatusEnum.OrderStatusPendingCancel,
                "Filled" => OrderStatusEnum.OrderStatusFilled,
                "Canceled" => OrderStatusEnum.OrderStatusCanceled,
                "Rejected" => OrderStatusEnum.OrderStatusRejected,
                "PartiallyFilled" => OrderStatusEnum.OrderStatusPartiallyFilled,
                _ => OrderStatusEnum.OrderStatusUnspecified
            };
        }
        
        public static bool IsClosed(this OrderStatusEnum orderStatus)
        {
            switch (orderStatus)
            {
                case OrderStatusEnum.OrderStatusCanceled:
                case OrderStatusEnum.OrderStatusFilled:
                case OrderStatusEnum.OrderStatusRejected:
                    return true;
                case OrderStatusEnum.OrderStatusPendingCancel:
                case OrderStatusEnum.OrderStatusPendingOpen:
                case OrderStatusEnum.OrderStatusPartiallyFilled:
                case OrderStatusEnum.OrderStatusPendingCancelReplace:
                case OrderStatusEnum.OrderStatusOrderSent:
                case OrderStatusEnum.OrderStatusPendingChild:
                case OrderStatusEnum.OrderStatusOpen:
                case OrderStatusEnum.OrderStatusUnspecified: // is used by SC for attached child orders on a bracket
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(nameof(orderStatus));
            }
        }

        public static string Abbrev(this OrderUpdateReasonEnum orderUpdateReason)
        {
            return orderUpdateReason.ToString();
        }

        public static OrderUpdateReasonEnum ToOrderUpdateReasonEnumFromAbbrev(this string str)
        {
            Enum.TryParse<OrderUpdateReasonEnum>(str, out var reason);
            return reason;
        }
    }
}
