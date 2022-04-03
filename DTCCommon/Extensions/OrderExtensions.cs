using System.Diagnostics;
using DTCPB;

// ReSharper disable once CheckNamespace
namespace DTCCommon
{
    public static class OrderExtensions
    {
        /// <summary>
        /// Return a new OrderUpdate instance corresponding to order
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public static OrderUpdate ToOrderUpdate(this IOrderDTC order)
        {
            var symbolDtc = order.GetSymbolDtc();
            var realtimePriceMultiplier = order.GetRealtimePriceMultiplier();
            Debug.Assert(realtimePriceMultiplier > 0);
            var orderUpdate = new OrderUpdate
            {
                Symbol = symbolDtc,
                Exchange = order.Exchange,
                PreviousServerOrderID = order.PreviousServerOrderId ?? "",
                ServerOrderID = order.ServerOrderId ?? "",
                ClientOrderID = order.ClientOrderId ?? "",
                ExchangeOrderID = order.ExchangeOrderId ?? "",
                OrderStatus = order.Status,
                OrderUpdateReason = order.Reason,
                OrderType = order.OrderType,
                BuySell = order.Side,
                Price1 = order.Price1 / realtimePriceMultiplier,
                Price2 = order.Price2 / realtimePriceMultiplier,
                TimeInForce = order.TimeInForce,
                GoodTillDateTimeLocal = order.Gtd,
                OrderQuantity = order.OrderQuantity,
                FilledQuantity = order.FilledQuantity,
                AverageFillPrice = order.AverageFillPrice / realtimePriceMultiplier,
                LastFillPrice = order.LastFillPrice / realtimePriceMultiplier,
                LastFillDateTimeLocal = order.LastFillDateTime,
                LastFillQuantity = order.LastFillQuantity,
                LastFillExecutionID = order.LastFillExecutionId ?? "",
                TradeAccount = order.TradeAccount ?? "",
                InfoText = order.InfoText ?? "",
                ParentServerOrderID = order.ParentServerOrderId ?? "",
                OCOLinkedOrderServerOrderID = order.Oco ?? "",
                OpenOrClose = order.OpenOrClose,
                PreviousClientOrderID = order.PreviousClientOrderId ?? "",
                OrderReceivedDateTimeLocal = order.OrderReceivedDateTime,
                LatestTransactionDateTimeLocal = order.LatestTransactionDateTime
            };
            return orderUpdate;
        }

        /// <summary>
        /// Update order with the current properties in orderUpdate
        /// </summary>
        /// <param name="orderUpdate"></param>
        /// <param name="order"></param>
        public static void UpdateOrder(this OrderUpdate orderUpdate, IOrderDTC order)
        {
            if (orderUpdate.IsNoOrders)
            {
                // This is an empty update
                return;
            }
            Debug.Assert(orderUpdate.TradeAccount == order.TradeAccount && orderUpdate.Symbol == order.GetSymbolDtc());
            var symbol = order.GetSymbol(orderUpdate.Symbol);
            var realtimePriceMultiplier = order.GetRealtimePriceMultiplier();
            order.Symbol = symbol;
            order.Exchange = orderUpdate.Exchange;
            order.PreviousServerOrderId = orderUpdate.PreviousServerOrderID;
            order.ServerOrderId = orderUpdate.ServerOrderID;
            if (!string.IsNullOrEmpty(orderUpdate.ClientOrderID))
            {
                // Never overwrite our database Id just because the orderUpdate doesn't know it
                order.ClientOrderId = orderUpdate.ClientOrderID;
            }
            order.ExchangeOrderId = orderUpdate.ExchangeOrderID;
            order.Status = orderUpdate.OrderStatus;
            order.Reason = orderUpdate.OrderUpdateReason;
            order.OrderType = orderUpdate.OrderType;
            order.Side = orderUpdate.BuySell;
            order.Price1 = orderUpdate.Price1 * realtimePriceMultiplier;
            order.Price2 = orderUpdate.Price2 * realtimePriceMultiplier;
            order.TimeInForce = orderUpdate.TimeInForce;
            order.Gtd = orderUpdate.GoodTillDateTimeLocal;
            order.OrderQuantity = orderUpdate.OrderQuantity;
            order.FilledQuantity = orderUpdate.FilledQuantity;
            order.AverageFillPrice = orderUpdate.AverageFillPrice * realtimePriceMultiplier;
            order.LastFillPrice = orderUpdate.LastFillPrice * realtimePriceMultiplier;
            order.LastFillDateTime = orderUpdate.LastFillDateTimeLocal;
            order.LastFillQuantity = orderUpdate.LastFillQuantity;
            order.TradeAccount = orderUpdate.TradeAccount;
            order.InfoText = orderUpdate.InfoText;
            order.ParentServerOrderId = orderUpdate.ParentServerOrderID;
            order.Oco = orderUpdate.OCOLinkedOrderServerOrderID;
            order.OpenOrClose = orderUpdate.OpenOrClose;
            order.PreviousClientOrderId = orderUpdate.PreviousClientOrderID;
            order.OrderReceivedDateTime = orderUpdate.OrderReceivedDateTimeLocal;
            order.LatestTransactionDateTime = orderUpdate.LatestTransactionDateTimeLocal;
        }
    }
}