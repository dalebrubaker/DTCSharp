using System;
using DTCPB;

namespace DTCCommon
{
    public interface IOrderDTC
    {
        string Symbol { get; set; }
        string Exchange { get; set; }
        string PreviousServerOrderId { get; set; }
        string ServerOrderId { get; set; }
        string ClientOrderId { get; set; }
        string ExchangeOrderId { get; set; }
        OrderStatusEnum Status { get; set; }
        OrderUpdateReasonEnum Reason { get; set; }
        OrderTypeEnum OrderType { get; set; }
        BuySellEnum Side { get; set; }
        double Price1 { get; set; }
        double Price2 { get; set; }
        TimeInForceEnum TimeInForce { get; set; }

        /// <summary>
        /// Local time
        /// </summary>
        DateTime Gtd { get; set; }

        double OrderQuantity { get; set; }
        double FilledQuantity { get; set; }
        
        /// <summary>
        /// RemainingQuantity is not set because RemainingQuantity == OrderQuantity - FilledQuantity
        /// </summary>
        // double RemainingQuantity { get; set; }
        
        double AverageFillPrice { get; set; }
        double LastFillPrice { get; set; }

        /// <summary>
        /// Local time
        /// </summary>
        DateTime LastFillDateTime { get; set; }

        double LastFillQuantity { get; set; }
        string LastFillExecutionId { get; set; }
        string TradeAccount { get; set; }
        string InfoText { get; set; }
        string ParentServerOrderId { get; set; }
        string Oco { get; set; }
        OpenCloseTradeEnum OpenOrClose { get; set; }
        string PreviousClientOrderId { get; set; }
        string FreeFormText { get; set; }
        bool IsCancelRequested { get; set; }

        /// <summary>
        /// Local time
        /// </summary>
        DateTime OrderReceivedDateTime { get; set; }
        
        /// <summary>
        /// Local time
        /// </summary>
        DateTime LatestTransactionDateTime { get; set; }
        
        bool UseOffsets { get; set; }
        
        bool IsAutomatedOrder { get; }
        bool IsParentOrder { get; }
        double OffsetFromParent { get; }
        string ParentTriggerClientOrderId { get; }
        OrderAction OrderAction { get; }
        
        /// <summary>
        /// Called for each OrderUpdate received from DTC
        /// </summary>
        /// <param name="orderUpdate"></param>
        void OnOrderUpdate(OrderUpdate orderUpdate);

        /// <summary>
        /// Return the Realtime Price Multiplier for the order based on the DTC connection
        /// </summary>
        /// <returns></returns>
        double GetRealtimePriceMultiplier();

        /// <summary>
        /// Return the DTC version of the internal symbol in an order
        /// </summary>
        /// <returns></returns>
        string GetSymbolDtc();
        
        /// <summary>
        /// Return the internal version of the DTC symbol in and orderUpdate
        /// </summary>
        /// <returns></returns>
        string GetSymbol(string symbolDTC);
    }
}