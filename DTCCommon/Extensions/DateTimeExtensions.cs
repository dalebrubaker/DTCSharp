using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTCCommon.Extensions
{
    public static class DateTimeExtensions
    {
        /// <summary>
        /// https://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#t_DateTime
        /// Convert the 8 byte seconds since 1/1/70 to a DateTime (UTC)
        /// </summary>
        /// <param name="dt8Byte"></param>
        /// <returns></returns>
        public static DateTime DateTime8ByteToUtc(this long dt8Byte)
        {
            var ticks = dt8Byte * TimeSpan.TicksPerSecond;
            var result = new DateTime(ticks, DateTimeKind.Utc);
            return result;
        }

        /// <summary>
        /// https://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#t_DateTime4Byte
        /// Convert the 4 byte seconds since 1/1/70 to a DateTime (UTC)
        /// </summary>
        /// <param name="dt4Byte"></param>
        /// <returns></returns>
        public static DateTime DateTime4ByteToUtc(this int dt4Byte)
        {
            var ticks = dt4Byte * TimeSpan.TicksPerSecond;
            var result = new DateTime(ticks, DateTimeKind.Utc);
            return result;
        }

        /// <summary>
        /// https://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#t_DateTimeWithMilliseconds
        /// Convert the double since 1/1/70 to a DateTime (UTC), which fractional milliseconds
        /// [As of 10/20/2016 the milliseconds are just sequence numbers, but we'll pretend they've done the work they plan for 2017]
        /// </summary>
        /// <param name="dtDouble"></param>
        /// <returns></returns>
        public static DateTime DateTimeDoubleToUtc(this double dtDouble)
        {
            var days = Math.Truncate(dtDouble);
            var msecs = (int)(1000 * (dtDouble - days));
            var result = DateTime.FromOADate(days).AddMilliseconds(msecs);
            return result;
        }
    }
}
