using Microsoft.Extensions.PlatformAbstractions;
using PeterKottas.DotNetCore.WindowsService;
using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetEscapades.Extensions.Logging.RollingFile;
using OLS.Casy.ActivationServer.Data;

namespace OLS.Casy.ActivationServer.ReportGenerator
{
    class Program
    {
        public static void Main(string[] args)
        {
#if !DEBUG
            var configuration = new ConfigurationBuilder()
                .SetBasePath(PlatformServices.Default.Application.ApplicationBasePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
#else
            var configuration = new ConfigurationBuilder()
                .SetBasePath(PlatformServices.Default.Application.ApplicationBasePath)
                .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true)
                .Build();
#endif

            var svcProvider = new ServiceCollection()
                .AddLogging(builder =>
                {
                    builder
                        .SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace)
                        .AddFile(opts => configuration.GetSection("FileLoggingOptions").Bind(opts));
                })
                .AddOptions()
                .AddSingleton(new LoggerFactory().AddConsole())
                .AddSingleton<IFileSystemStorageService, FileSystemStorageService>()
                .BuildServiceProvider();

            var logger = svcProvider.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();

            ServiceRunner<ReportGenerationService>.Run(config =>
            {
                var name = config.GetDefaultName();
                config.Service(serviceConfig =>
                {
                    serviceConfig.ServiceFactory((extraArguments, controller) =>
                    {
                        return new ReportGenerationService(controller,
                            svcProvider.GetRequiredService<ILoggerFactory>().CreateLogger<ReportGenerationService>(),
                            svcProvider.GetRequiredService<IFileSystemStorageService>());
                    });

                    serviceConfig.OnStart((service, extraParams) =>
                    {
                        logger.LogTrace("Service {0} started", name);
                        service.Start();
                    });

                    serviceConfig.OnStop(service =>
                    {
                        logger.LogTrace("Service {0} stopped", name);
                        service.Stop();
                    });

                    serviceConfig.OnError(e => { logger.LogError(e, $"Service {name} errored with exception"); });
                });
            });
        }
    }
}