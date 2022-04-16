using System;
using Serilog;

namespace TestsDTC
{
    public class TestFixture : IDisposable
    {
        private static int s_counter;

        public TestFixture()
        {
            // must be parameterless

            // Allow logging during Tests
            var seqURL = Environment.GetEnvironmentVariable("SeqURL");
            var apiKey = Environment.GetEnvironmentVariable("DTCSharpSeqApiKey");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .Enrich.WithThreadId()
                .Enrich.WithThreadName()
                .Enrich
                .WithProperty("Application", nameof(TestsDTC))
                .WriteTo.Seq(seqURL, apiKey: apiKey)
                .CreateLogger();
            Log.Verbose("Starting {counter} {classType}", s_counter++, GetType().Name);
        }

        public void Dispose()
        {
            Log.Verbose("Exiting {counter} {classType}", s_counter++, GetType().Name);
            Log.CloseAndFlush();
        }
    }
}