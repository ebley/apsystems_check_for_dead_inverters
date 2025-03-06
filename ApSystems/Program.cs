using System.Net.Mail;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApSystems
{
    internal class Program
    {

        public class MailSettings
        {
            public string fromAddress { get; set; }
            public string smtpServer { get; set; }
            public string smtpUser { get; set; }
            public string smtpPs { get; set; }
            public string Password { get; set; }
            public string emailTo { get; set; }
            public int emailPort { get; set; }

        }

        public class SystemSettings
        {
            public string app { get; set; }
            public string s { get; set; }
            public string sid { get; set; }

            public string eid { get; set; }
            public string apiRoot { get; set; }
        }

        static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            var work = host.Services.GetRequiredService<Work>();
            //Work w = new Work();
            await work.Main(args);
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    var builtConfig = config.Build();
                    config.AddJsonFile("mail.json", optional: true, reloadOnChange: true);
                    config.AddJsonFile("system.json", optional: true, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    // Bind configuration sections to classes
                    services.Configure<MailSettings>(context.Configuration.GetSection("MailSettings"));
                    services.Configure<SystemSettings>(context.Configuration.GetSection("SystemSettings"));

                    // Add any other services your app needs
                    services.AddTransient<Work>();
                });
    }
}
