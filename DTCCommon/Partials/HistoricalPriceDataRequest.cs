using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DTCCommon;
using DTCCommon.Extensions;
using Google.Protobuf;

namespace DTCPB
{
    [DebuggerDisplay("{MyToString}")]
    public partial class HistoricalPriceDataRequest
    {
        public DateTime StartDateTimeUtc
        {
            get { return startDateTime_.DtcDateTimeToUtc(); }
            set { startDateTime_ = value.UtcToDtcDateTime(); }
        }

        public DateTime EndDateTimeUtc
        {
            get { return endDateTime_.DtcDateTimeToUtc(); }
            set { endDateTime_ = value.UtcToDtcDateTime(); }
        }

        public bool IsZipped
        {
            get { return useZLibCompression_ != 0; }
            set { useZLibCompression_ = value ?  1u : 0u; }
        }

        public string MyToString => $"{symbol_} {StartDateTimeUtc} - {EndDateTimeUtc} MaxDaysToReturn:{MaxDaysToReturn} (UTC) Zip:{IsZipped} {RecordInterval}";

     

    }
}
