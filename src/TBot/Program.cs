using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;

namespace TBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
            .UseShutdownTimeout(TimeSpan.FromSeconds(10))
            .ConfigureAppConfiguration((builderContext, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .UseStartup<Startup>()
            .ConfigureLogging(logging => {
                logging.AddApplicationInsights("[YOUR_APPLICATION_INSIGHT_KEY]");
                logging.AddFilter<ApplicationInsightsLoggerProvider>("", LogLevel.Trace);
            });
    }
}
