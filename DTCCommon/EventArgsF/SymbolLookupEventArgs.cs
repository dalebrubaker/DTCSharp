using System;
using DTCPB;

namespace DTCCommon.EventArgsF
{
    public class SymbolLookupEventArgs : EventArgs
    {
        public SecurityDefinitionResponse LookupResult { get; set; }

        public SymbolLookupEventArgs(SecurityDefinitionResponse lookupResult)
        {
            LookupResult = lookupResult;
        }
    }
}