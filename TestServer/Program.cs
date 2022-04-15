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

            var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();
            try
            {
                Log.Verbose("{AppName} starting", nameof(TestServer));
                Application.Run(new ServerForm());
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "{AppName} failed to run", nameof(TestServer));
            }
            finally
            {
                Log.Verbose("{AppName} exiting", nameof(TestServer));
                Log.CloseAndFlush();
            }
        }
    }
}