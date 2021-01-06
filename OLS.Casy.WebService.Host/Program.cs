using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.WindowsServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace OLS.Casy.WebService.Host
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var isService = !(Debugger.IsAttached || args.Contains("--console"));

            
            var builder = new WebHostBuilder()
                .ConfigureAppConfiguration((hostingContext, configBuilder) =>
                {
                    configBuilder.AddJsonFile("appsettings.json", optional: false);
                    configBuilder.AddEnvironmentVariables();
                    configBuilder.AddCommandLine(args);
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                })
                .UseKestrel()
                .UseUrls("http://*:8536");

            if (isService)
            {
                var pathToContentRoot = Directory.GetCurrentDirectory();
                var pathToExe = Process.GetCurrentProcess().MainModule.FileName;
                pathToContentRoot = Path.GetDirectoryName(pathToExe);

                builder.UseContentRoot(pathToContentRoot);
            }

            builder.UseStartup<Startup>();
            
            var host = builder.Build();
            if (isService)
            {   
                host.RunAsService();
                //host.Run();
            }
            else
            {
                host.Run();
            }
        }
    }
}
