using System.Collections.Generic;

namespace DTCCommon
{
    /// <summary>
    /// Bidirectional map of internal symbol to DTC symbol.
    /// Thanks to Enigmativity at https://stackoverflow.com/questions/10966331/two-way-bidirectional-dictionary-in-c/10966684#10966684
    /// </summary>
    public class SymbolMap
    {
        private readonly object _lock = new object();
        private readonly Dictionary<string, string> _symbolDTCBySymbol = new();
        private readonly Dictionary<string, string> _symbolBySymbolDTC = new();
        private readonly string _name;

        // public Indexer<string, string> SymbolDTCBySymbol { get; }
        // public Indexer<string, string> SymbolBySymbolDTC { get; }
        public int Count => _symbolDTCBySymbol.Count;

        public SymbolMap(string name)
        {
            _name = name;
            // SymbolDTCBySymbol = new Indexer<string, string>(_symbolDTCBySymbol);
            // SymbolBySymbolDTC = new Indexer<string, string>(_symbolBySymbolDTC);
        }

        public class Indexer<T3, T4>
        {
            private readonly Dictionary<T3, T4> _dictionary;

            public Indexer(Dictionary<T3, T4> dictionary)
            {
                _dictionary = dictionary;
            }

            public T4 this[T3 index]
            {
                get => _dictionary[index];
                set => _dictionary[index] = value;
            }
        }

        /// <summary>
        /// Add the symbol pair
        /// </summary>
        /// <param name="symbol">some symbol to pair with symbolDTC, e.g. internal to the client or server application</param>
        /// <param name="symbolDTC">the symbol to be sent/received via DTC</param>
        public void Add(string symbol, string symbolDTC)
        {

            lock (_lock)
            { 
                _symbolDTCBySymbol.Add(symbol, symbolDTC);
                _symbolBySymbolDTC.Add(symbolDTC, symbol);
            }
        }

        /// <summary>
        /// Try to get the value of symbolDTC for the given symbol
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="symbolDTC"></param>
        /// <returns></returns>
        public bool TryGetValueSymbolDTC(string symbol, out string symbolDTC)
        {
            lock (_lock)
            {
                var result = _symbolDTCBySymbol.TryGetValue(symbol, out symbolDTC);
                return result;
            }
        }

        /// <summary>
        /// Try to get the value of symbol for the given symbolDTC
        /// </summary>
        /// <param name="symbolDTC"></param>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public bool TryGetValueSymbol(string symbolDTC, out string symbol)
        {
            lock (_lock)
            {
                var result = _symbolBySymbolDTC.TryGetValue(symbolDTC, out symbol);
                return result;
            }
        }

        public override string ToString()
        {
            return $"{_name} Count={Count:N0}";
        }
    }
}