using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Serilog;

namespace DTCCommon
{
    /// <summary>
    /// Cache SierraChart symbol settings file info
    /// </summary>
    public class SymbolSettings
    {
        private static readonly ILogger s_logger = Log.ForContext(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType!);

        private readonly string _path;

        private readonly Dictionary<string, XmlNode> _nodesBySymbolPattern = new Dictionary<string, XmlNode>();
        private readonly Dictionary<string, XmlNode> _nodesBySymbol = new Dictionary<string, XmlNode>();

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="path">fully-qualified path to the SierraChart symbol settings file</param>
        public SymbolSettings(string path)
        {
            _path = path;
            using var sr = File.OpenText(path);
            lock (_nodesBySymbolPattern)
            {
                var doc = new XmlDocument();
                doc.Load(sr);
                var settings = doc.GetElementsByTagName("settings");
                foreach (XmlNode setting in settings)
                {
                    var symbolPattern = setting.FirstChild.InnerText;
                    if (!_nodesBySymbolPattern.ContainsKey(symbolPattern))
                    {
                        _nodesBySymbolPattern.Add(symbolPattern, setting);
                    }
                }
            }
        }

        public string GetInnerText(string symbol, string elementName)
        {
            // s_logger.Debug($"Getting tickSizeStr from symbolSettings for {symbol}");
            var node = GetNode(symbol);
            if (node == null)
            {
                // unrecognized symbol
                return null;
            }
            foreach (XmlNode element in node.ChildNodes)
            {
                if (element.Name == elementName)
                {
                    return element.InnerText;
                }
            }
            return null;
        }

        public List<(string, string)> GetAllElementNamesWithInnerText(string symbol)
        {
            var node = GetNode(symbol);
            var result = new List<(string, string)>();
            foreach (XmlNode element in node.ChildNodes)
            {
                result.Add((element.Name, element.InnerText));
            }
            return result;
        }

        private XmlNode GetNode(string symbol)
        {
            lock (_nodesBySymbol)
            {
                if (_nodesBySymbol.TryGetValue(symbol, out var node))
                {
                    return node;
                }
                lock (_nodesBySymbolPattern)
                {
                    foreach (var symbolPattern in _nodesBySymbolPattern.Keys)
                    {
                        if (IsMatch(symbolPattern, symbol))
                        {
                            node = _nodesBySymbolPattern[symbolPattern];
                            _nodesBySymbol.Add(symbol, node);
                            return node;
                        }
                    }
                    return null;
                }
            }
        }

        /// <summary>
        /// Return <c>true</c> if symbol matches the Sierra Chart symbol pattern
        /// ? means any character, ignore case
        /// # means any digit
        /// </summary>
        /// <param name="symbolPattern"></param>
        /// <param name="symbol"></param>
        /// <returns></returns>
        private static bool IsMatch(string symbolPattern, string symbol)
        {
            if (symbol.Length != symbolPattern.Length)
            {
                return false;
            }
            for (int i = 0; i < symbolPattern.Length; i++)
            {
                var pCh = symbolPattern[i];
                var sCh = symbol[i];
                switch (pCh)
                {
                    case '#':
                        if (!char.IsDigit(sCh))
                        {
                            return false;
                        }
                        break;
                    case '?':
                        if (!char.IsLetter(sCh))
                        {
                            return false;
                        }
                        break;
                    default:
                        if (pCh != sCh)
                        {
                            return false;
                        }
                        break;
                }
            }
            return true;
        }

        /// <summary>
        /// Return the futures symbol matching symbolPrefix
        /// </summary>
        /// <param name="symbolPrefix"></param>
        /// <returns></returns>
        public string GetFuturesSymbolPattern(string symbolPrefix)
        {
            var result = new List<string>();
            lock (_nodesBySymbolPattern)
            {
                foreach (var symbolPattern in _nodesBySymbolPattern.Keys)
                {
                    if (symbolPattern.StartsWith(symbolPrefix, StringComparison.OrdinalIgnoreCase) && !(symbolPattern.IndexOf("SPREAD", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        result.Add(symbolPattern);
                    }
                }
                // not true DebugDTC.Assert(result.Count == 1, "We assume only 1 futures symbol of a given prefix.");
                if (result.Count == 0)
                {
                    return null;
                }
                return  result[0];
            }
        }

        public override string ToString()
        {
            return _path;
        }
    }
}