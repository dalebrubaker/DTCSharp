using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DTCCommon.Extensions
{
    public static class DateTimeExtensions
    {
        public static readonly DateTime EpochStart;

        static DateTimeExtensions()
        {
            EpochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        /// <summary>
        /// https://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#t_DateTime
        /// Convert the 8 byte seconds since 1/1/70 to a DateTime (UTC)
        /// </summary>
        /// <param name="unixSeconds"></param>
        /// <returns></returns>
        public static DateTime DtcDateTimeToUtc(this long unixSeconds)
        {
            var result = EpochStart.AddSeconds(unixSeconds);
            return result;
        }

        /// <summary>
        /// https://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#t_DateTime
        /// Convert dateTime (UTC) to DTC t_DateTime (8 bytes, seconds since 1/1/70)
        /// </summary>
        /// <param name="dateTimeUtc"></param>
        /// <returns></returns>
        public static long UtcToDtcDateTime(this DateTime dateTimeUtc)
        {
            var unixTimeInSeconds = (long)(dateTimeUtc - EpochStart).TotalSeconds;
            return unixTimeInSeconds;
        }

        /// <summary>
        /// https://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#t_DateTime4Byte
        /// Convert the 4 byte seconds since 1/1/70 to a DateTime (UTC)
        /// </summary>
        /// <param name="dt4Byte"></param>
        /// <returns></returns>
        public static DateTime DtcDateTime4ByteToUtc(this int dt4Byte)
        {
            return DtcDateTimeToUtc(dt4Byte);
        }

        /// <summary>
        /// https://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#t_DateTime4Byte
        /// Convert dateTime (UTC) to DTC t_DateTime4Byte (4 bytes, seconds since 1/1/70)
        /// </summary>
        /// <param name="dateTimeUtc"></param>
        /// <returns></returns>
        public static int UtcToDtcDateTime4Byte(this DateTime dateTimeUtc)
        {
            return (int)UtcToDtcDateTime(dateTimeUtc);
        }

        /// <summary>
        /// https://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#t_DateTimeWithMilliseconds
        /// Convert the double since 1/1/70 to a DateTime (UTC), which fractional milliseconds
        /// [As of 10/20/2016 the milliseconds are just sequence numbers, but we'll pretend they've done the work they plan for 2017]
        /// </summary>
        /// <param name="dtDouble"></param>
        /// <returns></returns>
        public static DateTime DtcDateTimeWithMillisecondsToUtc(this double dtDouble)
        {
            var seconds = Math.Truncate(dtDouble);
            var msecs = (int)(1000 * (dtDouble - seconds));
            var result = EpochStart.AddSeconds(seconds).AddMilliseconds(msecs);
            return result;
        }

        /// <summary>
        /// https://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#t_DateTimeWithMilliseconds
        /// Convert the double since 1/1/70 to a DateTime (UTC), which fractional milliseconds
        /// Convert dateTime (UTC) to DTC t_DateTimeWithMilliseconds (8 bytes, seconds since 1/1/70, fractional portion is milliseconds)
        /// [As of 10/20/2016 the milliseconds are just sequence numbers, but we'll pretend they've done the work they plan for 2017]
        /// </summary>
        /// <param name="dateTimeUtc"></param>
        /// <returns></returns>
        public static double UtcToDtcDateTimeWithMilliseconds(this DateTime dateTimeUtc)
        {
            var timeSpan = (dateTimeUtc - EpochStart);
            var seconds = Math.Truncate(timeSpan.TotalSeconds);
            var result = seconds + timeSpan.Milliseconds / 1000.0;
            return result;
        }
    }
}
