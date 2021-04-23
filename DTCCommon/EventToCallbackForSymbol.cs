using System;
using DTCPB;

namespace DTCCommon
{
    public class EventToCallbackForSymbol<T> : IDisposable where T : IMessageSymbolId, new()
    {
        private readonly uint _symbolId;
        private readonly string _symbol;
        private readonly string _exchange;
        private readonly Action<EventHandler<T>> _remove;
        private readonly Action<T> _callback;
        private bool _isDisposed;

        /// <summary>
        /// EventToCallbackForSymbol
        /// </summary>
        /// <param name="symbolId">the symbol Id for events to be sent to callback</param>
        /// <param name="symbol">for debugging</param>
        /// <param name="exchange">for debugging</param>
        /// <param name="add"></param>
        /// <param name="remove"></param>
        /// <param name="callback">the callback for the event, or ignored if null</param>
        public EventToCallbackForSymbol(uint symbolId, string symbol, string exchange, Action<EventHandler<T>> add, Action<EventHandler<T>> remove,
            Action<T> callback)
        {
            _symbolId = symbolId;
            _symbol = symbol;
            _exchange = exchange;
            _remove = remove;
            if (callback != null)
            {
                _callback = callback;
                add(Client_EventT);
            }
        }

        public bool IsDataReceived { get; private set; }

        private void Client_EventT(object sender, T e)
        {
            if (e.SymbolID == _symbolId)
            {
                _callback(e);
                IsDataReceived = true;
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                if (_callback != null)
                {
                    // ReSharper disable once DelegateSubtraction
                    _remove(Client_EventT);
                }
                _isDisposed = true;
            }
            GC.SuppressFinalize(this);
        }

        public override string ToString()
        {
            return $"{_symbolId} {_symbol} {_exchange}";
        }
    }
}