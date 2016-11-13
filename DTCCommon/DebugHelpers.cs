using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTCCommon
{
    public static class DebugHelpers
    {
#if DEBUG
        // ClientHelper (server-side)
        public static List<string> RequestsReceived = new List<string>();
        public static List<string> ResponsesSent = new List<string>();

        // Client
        public static List<string> ResponsesReceived = new List<string>();
        public static List<string> RequestsSent = new List<string>();


#endif
    }
}
