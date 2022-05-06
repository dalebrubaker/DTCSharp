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
            
            try
            {
                CreateSerilogLogger();
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
        
        private static void CreateSerilogLogger()
        {
            var configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .AddUserSecrets<ServerForm>()
                    .Build();
            Log.Logger = new LoggerConfiguration()
                .ReadFrom
                .Configuration(configuration)
                .CreateLogger();
        }


    }
}