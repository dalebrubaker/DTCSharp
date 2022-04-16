using System;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using DTCClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            Log.Logger = new LoggerConfiguration()
                .ReadFrom
                .Configuration(configuration)
                .CreateLogger();
            
            try
            {
                Log.Verbose("{AppName} starting", nameof(TestClient));
                var host = Host.CreateDefaultBuilder()
                    .ConfigureServices((hostContext, services) =>
                    {
                        services.AddScoped<ClientForm>();
                        services.AddScoped<ClientDTC>();
                    })
                    .UseSerilog()
                    .Build();
                using var serviceScope = host.Services.CreateScope();
                var serviceProvider = serviceScope.ServiceProvider;
                var form1 = serviceProvider.GetRequiredService<ClientForm>();
                Application.Run(form1);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "{AppName} failed to run", nameof(TestClient));
            }
            finally
            {
                Log.Verbose("{AppName} exiting", nameof(TestClient));
                Log.CloseAndFlush();
            }
        }
    }
}