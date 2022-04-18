using System;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
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
            
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            var seqURL = Environment.GetEnvironmentVariable("SeqURL");
            var apiKey = Environment.GetEnvironmentVariable("DTCSharpSeqApiKey");
            if (string.IsNullOrEmpty(seqURL))
            {
                Log.Logger = new LoggerConfiguration()
                    .ReadFrom
                    .Configuration(configuration)
                    .CreateLogger();
            }
            else
            {
                // Add Seq using environment variables, to keep them out of appsettings.json
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.Seq(seqURL, apiKey:apiKey)
                    .ReadFrom
                    .Configuration(configuration)
                    .CreateLogger();
            }
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