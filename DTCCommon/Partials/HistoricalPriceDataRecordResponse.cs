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
    public partial class HistoricalPriceDataRecordResponse
    {

        public DateTime StartDateTimeUtc
        {
            get { return startDateTime_.DtcDateTimeToUtc(); }
            set { startDateTime_ = value.UtcToDtcDateTime(); }
        }

        public bool IsFinalRecordBool
        {
            get { return isFinalRecord_ != 0; }
            set { isFinalRecord_ = value ? 1u : 0u; }
        }

        public string MyToString => $"{StartDateTimeUtc}(UTC) O:{OpenPrice} H:{HighPrice} L:{LowPrice} C:{LastPrice} V:{Volume} BV:{BidVolume} AV:{AskVolume} #T{NumTrades} Final{IsFinalRecordBool}";

     

    }
}
