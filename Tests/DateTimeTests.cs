using System;
using DTCCommon;
using Xunit;

namespace Tests;

[Collection("Logging collection")]
public class DateTimeTests : IDisposable
{
    private readonly TestFixture _fixture;

    public DateTimeTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    public void Dispose()
    {
        Console.WriteLine("Disposing");
    }

    [Fact]
    public void ScDateTimeTest()
    {
        var origDtSeconds = new DateTime(2001, 1, 1, 1, 1, 1, DateTimeKind.Utc);
        var scDt = origDtSeconds.UtcToDtcDateTime();
        var dtSeconds = scDt.DtcDateTimeToUtc();
        Assert.Equal(origDtSeconds, dtSeconds);

        origDtSeconds = origDtSeconds.AddMilliseconds(1);
        scDt = origDtSeconds.UtcToDtcDateTime();
        dtSeconds = scDt.DtcDateTimeToUtc();
        Assert.NotEqual(origDtSeconds, dtSeconds);
    }

    [Fact]
    public void ScDateTime4ByteTest()
    {
        var origDtSeconds = new DateTime(2001, 1, 1, 1, 1, 1, DateTimeKind.Utc);
        var scDt = origDtSeconds.UtcToDtcDateTime4Byte();
        var dtSeconds = scDt.DtcDateTime4ByteToUtc();
        Assert.Equal(origDtSeconds, dtSeconds);

        origDtSeconds = origDtSeconds.AddMilliseconds(1);
        scDt = origDtSeconds.UtcToDtcDateTime4Byte();
        dtSeconds = scDt.DtcDateTime4ByteToUtc();
        Assert.NotEqual(origDtSeconds, dtSeconds);
    }

    [Fact]
    public void ScDateTimeWithMillisecondsTest()
    {
        var origDt = new DateTime(2001, 1, 1, 1, 1, 1, DateTimeKind.Utc);
        origDt = origDt.AddMilliseconds(1);
        var scDt = origDt.UtcToDtcDateTimeWithMilliseconds();
        var dt = scDt.DtcDateTimeWithMillisecondsToUtc();
        Assert.Equal(origDt, dt);
    }

    [Fact]
    public void ScIntradayDateTimeWithMillisecondsTest()
    {
        var origDt = new DateTime(2001, 1, 1, 1, 1, 1, DateTimeKind.Utc);
        origDt = origDt.AddMilliseconds(1);
        var scDt = origDt.UtcToDtcIntradayDateTimeWithMilliseconds();
        var dt = scDt.DtcIntradayDateTimeWithMillisecondsToUtc();
        Assert.Equal(origDt, dt);
    }

    [Fact]
    public void ScIntradayDateTimeWithMillisecondsTest2()
    {
        var origDt = new DateTime(2016, 5, 16, 4, 11, 48, DateTimeKind.Utc);
        var scDt = 42506.174861111111; // SC number
        var dt = scDt.DtcIntradayDateTimeWithMillisecondsToUtc();
        Assert.Equal(origDt, dt);
    }
}