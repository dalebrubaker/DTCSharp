using System;

// ReSharper disable once CheckNamespace
namespace DTCCommon
{
    public static class IntExtensions
    {
        // ReSharper disable once InconsistentNaming
        public static DateTime FromYYMMDD(this int yymmdd)
        {
            int year = yymmdd / 10000;
            int month = yymmdd % 10000 / 100;
            int day = yymmdd % 100;
            return new DateTime(year + 2000, month, day);
        }

        // ReSharper disable once InconsistentNaming
        public static TimeSpan FromHHMMSS(this int hhmmss)
        {
            int hours = hhmmss / 10000;
            int minutes = hhmmss % 10000 / 100;
            int seconds = hhmmss % 100;
            return new TimeSpan(hours, minutes, seconds);
        }
    }
}