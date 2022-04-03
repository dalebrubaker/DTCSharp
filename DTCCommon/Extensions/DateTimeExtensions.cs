using System;

// ReSharper disable once CheckNamespace
namespace DTCCommon
{
    public static class DateTimeExtensions
    {
        public static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
        private static readonly DateTime s_scMicrosecondsEpoch = new DateTime(1899, 12, 30, 0, 0, 0, DateTimeKind.Unspecified);
        private const long TicksPerMicrosecond = 10;
        private static readonly long s_maxSeconds;

        static DateTimeExtensions()
        {
            s_maxSeconds = (long)(DateTime.MaxValue - UnixEpoch).TotalSeconds;
        }

        /// <summary>
        /// https://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#t_DateTime
        /// Convert the 8 byte seconds since 1/1/70 to a DateTime (UTC)
        /// Same as FromUnixSecondsToDateTime()
        /// </summary>
        /// <param name="unixSeconds"></param>
        /// <returns></returns>
        public static DateTime DtcDateTimeToUtc(this long unixSeconds)
        {
            return unixSeconds.FromUnixSecondsToDateTime();
        }

        /// <summary>
        /// https://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#t_DateTime
        /// Convert dateTime (UTC) to DTC t_DateTime (8 bytes, seconds since 1/1/70)
        /// Almost the same as ToUnixSeconds(). Returns 0 if < int.MinValue
        /// </summary>
        /// <param name="dateTimeUtc"></param>
        /// <returns></returns>
        public static long UtcToDtcDateTime(this DateTime dateTimeUtc)
        {
            var result = dateTimeUtc.ToUnixSeconds();
            return result;
        }

        /// <summary>
        /// https://dtcprotocol.org/index.php?page=doc/DTCMessageDocumentation.php#t_DateTime4Byte
        /// Convert the 4 byte seconds since 1/1/70 to a DateTime (UTC)
        /// Same as FromUnixSecondsToDateTime()
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
        /// Save as ToUnixSeconds
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
            if (dtDouble == 0)
            {
                return DateTime.MinValue;
            }
            var seconds = Math.Truncate(dtDouble);
            var msecs = (int)(1000 * (dtDouble - seconds));
            var result = UnixEpoch.AddSeconds(seconds).AddMilliseconds(msecs);
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
            if (dateTimeUtc == DateTime.MinValue)
            {
                return 0;
            }
            var timeSpan = dateTimeUtc - UnixEpoch;
            var seconds = Math.Truncate(timeSpan.TotalSeconds);
            var result = seconds + timeSpan.Milliseconds / 1000.0;
            return result;
        }

        /// <summary>
        /// http://www.sierrachart.com/index.php?page=doc/IntradayDataFileFormat.html#s_IntradayRecord
        /// Convert the double since 1/1/70 to a DateTime (UTC), which fractional milliseconds
        /// [As of 10/20/2016 the milliseconds are just sequence numbers, but we'll pretend they've done the work they plan for 2017]
        /// </summary>
        /// <param name="dtDouble"></param>
        /// <returns></returns>
        public static DateTime DtcIntradayDateTimeWithMillisecondsToUtc(this double dtDouble)
        {
            if (dtDouble == 0)
            {
                return DateTime.MinValue;
            }
            var dateTime = DateTime.FromOADate(dtDouble);
            dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            return dateTime;
        }

        /// <summary>
        /// http://www.sierrachart.com/index.php?page=doc/IntradayDataFileFormat.html#s_IntradayRecord
        /// Convert the double since 1/1/70 to a DateTime (UTC), which fractional milliseconds
        /// Convert dateTime (UTC) to DTC t_DateTimeWithMilliseconds (8 bytes, days since 1/1/70, fractional portion is % of 24 hours)
        /// [As of 10/20/2016 the milliseconds are just sequence numbers, but we'll pretend they've done the work they plan for 2017]
        /// </summary>
        /// <param name="dateTimeUtc"></param>
        /// <returns></returns>
        public static double UtcToDtcIntradayDateTimeWithMilliseconds(this DateTime dateTimeUtc)
        {
            if (dateTimeUtc == DateTime.MinValue)
            {
                return 0;
            }
            var result = dateTimeUtc.ToOADate();
            return result;
        }

        /// <summary>
        /// Convert UNIX DateTime to Windows DateTime
        /// </summary>
        /// <param name="unixSeconds">time in seconds since Epoch</param>
        public static DateTime FromUnixSecondsToDateTime(this long unixSeconds)
        {
            if (unixSeconds == 0)
            {
                return DateTime.MinValue;
            }
            if (unixSeconds >= s_maxSeconds)
            {
                return DateTime.MaxValue;
            }
            var result = UnixEpoch.AddSeconds(unixSeconds);
            return result;
        }

        /// <summary>
        /// Convert Windows DateTime to UNIX seconds.
        /// </summary>
        public static long ToUnixSeconds(this DateTime dateTime)
        {
            if (dateTime == DateTime.MinValue)
            {
                return 0;
            }
            var result = (long)(dateTime - UnixEpoch).TotalSeconds;
            //var check = result.FromUnixSecondsToDateTime();
            return result;
        }

        /// <summary>
        /// Convert SC_DateTimeMS DateTime to Windows DateTime
        /// SC_DateTimeMS is the 8 byte microseconds since  December 30, 1899 to a DateTime (UTC)
        /// https://www.sierrachart.com/index.php?page=doc/IntradayDataFileFormat.html#s_IntradayRecord__DateTime
        /// </summary>
        /// <param name="scMicroSeconds">time in seconds since Epoch</param>
        public static DateTime FromScMicroSecondsToDateTime(this long scMicroSeconds)
        {
            if (scMicroSeconds == 0)
            {
                return DateTime.MinValue;
            }
            var ticks = scMicroSeconds * TicksPerMicrosecond;
            return s_scMicrosecondsEpoch.AddTicks(ticks);
        }

        /// <summary>
        /// Convert Windows DateTime to SC_DateTimeMS which is the 8 byte microseconds since  December 30, 1899 to a DateTime (UTC)
        /// https://www.sierrachart.com/index.php?page=doc/IntradayDataFileFormat.html#s_IntradayRecord__DateTime
        /// </summary>
        public static long ToScMicroSeconds(this DateTime dateTime)
        {
            if (dateTime == DateTime.MinValue)
            {
                return 0;
            }
            var result = (dateTime - s_scMicrosecondsEpoch).Ticks / TicksPerMicrosecond;
            return result;
        }
    }
}