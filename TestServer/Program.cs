using System;
using System.Windows.Forms;
using Serilog;

namespace TestServer
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
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .Enrich.WithThreadId()
                .Enrich.WithThreadName()
                .Enrich
                .WithProperty("Application", nameof(TestServer))
                .WriteTo.Seq(seqURL, apiKey: apiKey)
                .CreateLogger();
            try
            {
                Log.Verbose("Starting");
                Application.Run(new ServerForm());
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