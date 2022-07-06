using System;

// ReSharper disable once CheckNamespace
namespace DTCCommon;

public static class IntExtensions
{
    // ReSharper disable once InconsistentNaming
    public static DateTime FromYYMMDD(this int yymmdd)
    {
        var year = yymmdd / 10000;
        var month = yymmdd % 10000 / 100;
        var day = yymmdd % 100;
        return new DateTime(year + 2000, month, day);
    }

    // ReSharper disable once InconsistentNaming
    public static TimeSpan FromHHMMSS(this int hhmmss)
    {
        var hours = hhmmss / 10000;
        var minutes = hhmmss % 10000 / 100;
        var seconds = hhmmss % 100;
        return new TimeSpan(hours, minutes, seconds);
    }
}