using System;
using System.Runtime.Serialization;
using DTCPB;

// ReSharper disable once CheckNamespace
namespace DTCCommon
{
    [Serializable]
    public class OrderRejectedException : Exception
    {
        public OrderRejectedException()
        {
        }

        public OrderRejectedException(string message)
            : base(message)
        {
        }

        public OrderRejectedException(string message, Exception inner)
            : base(message, inner)
        {
        }

        public OrderRejectedException(OrderUpdate orderUpdate)
        {
            
        }

        /// <summary>
        /// The order update that caused this exception.
        /// </summary>
        public OrderUpdate OrderUpdate { get; set; }

        /// <summary>
        /// Probably the explanation for this rejection
        /// </summary>
        public string InfoText => OrderUpdate?.InfoText;

        // Ensure Exception is Serializable
        protected OrderRejectedException(SerializationInfo info, StreamingContext ctxt)
            : base(info, ctxt)
        {
        }
    }
}