using System;
using System.Reflection;
using Microsoft.Extensions.Configuration;
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
        SetLogging();
        var method = (MethodBase.GetCurrentMethod());
        var stackTrace = Environment.StackTrace;
        Log.Verbose("Starting {Counter} {ClassType}", s_counterStart++, GetType().Name);
    }

     public static void SetLogging()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddUserSecrets<TestFixture>()
            .Build();
        Log.Logger = new LoggerConfiguration()
            .ReadFrom
            .Configuration(configuration)
            .CreateLogger();
    }

    public void Dispose()
    {
        Log.Verbose("Exiting {counter} {classType}", s_counterExit++, GetType().Name);
        Log.CloseAndFlush();
    }
}