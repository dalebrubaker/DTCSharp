using System;
using System.Windows.Forms;
using Serilog;

namespace TestClient
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            var seqURL = Environment.GetEnvironmentVariable("SeqURL");
            var apiKey = Environment.GetEnvironmentVariable("DTCSharpSeqApiKey");
            if (seqURL != null)
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .Enrich.FromLogContext()
                    .Enrich.WithProcessId()
                    .Enrich.WithThreadId()
                    .Enrich.WithThreadName()
                    .Enrich
                    .WithProperty("Application", nameof(TestClient))
                    .WriteTo.Seq(seqURL, apiKey: apiKey)
                    .CreateLogger();
            }
            try
            {
                Log.Verbose("Starting");
                Application.Run(new ClientForm());
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Failed to run");
            }
            finally
            {
                Log.Verbose("Exiting");
                Log.CloseAndFlush();
            }
        }
    }
}