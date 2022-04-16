using System;
using System.Reflection;
using Serilog;

namespace Tests;

public class TestFixture : IDisposable
{
    private static int s_counterStart;
    private static int s_counterExit;

    /// <summary>
    /// must be parameterless
    /// </summary>
    public TestFixture()
    {
        // Allow logging during Tests
        var seqURL = Environment.GetEnvironmentVariable("SeqURL");
        var apiKey = Environment.GetEnvironmentVariable("DTCSharpSeqApiKey");
        if (seqURL != null)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .Enrich.WithThreadId()
                .Enrich.WithThreadName()
                .Enrich
                .WithProperty("Application", nameof(Tests))
                .WriteTo.Seq(seqURL, apiKey: apiKey)
                .CreateLogger();
        }
        var method = (MethodBase.GetCurrentMethod());
        var stackTrace = Environment.StackTrace;
        Log.Verbose("Starting {counter} {classType}", s_counterStart++, GetType().Name);
    }

    public void Dispose()
    {
        Log.Verbose("Exiting {counter} {classType}", s_counterExit++, GetType().Name);
        Log.CloseAndFlush();
    }
}