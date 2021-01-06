using Devices.ReadModels.Client;
using MicroServices.Common.MessageBus;
using MicroServices.Common.RabbitMq.MessageBus;
using MicroServices.Common.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OLS.Casy.Core;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Config.Api;
using Polly;
using System;
using System.ComponentModel.Composition;
using System.Net.Http;

namespace OLS.Casy.RemoteIPS
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IService))]
    public class RemoteIpsService : AbstractService
    {
        private DevicesView _devicesView;
        private IServiceProvider _serviceProvider;

        [ImportingConstructor]
        public RemoteIpsService(IConfigService configService)
            : base(configService)
        {
            var hostBuilder = new HostBuilder();
            hostBuilder.ConfigureAppConfiguration((hostingContext, configBuilder) =>
            {
                configBuilder.AddJsonFile("remoteIPS.json", optional: true);
            }).ConfigureServices(ConfigureService);

            var host = hostBuilder.Build();

            _serviceProvider = host.Services;

            //IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("remoteIPS.json").Build();
            //IMessageBus messageBus = new RabbitMqBus(configuration);
        }

        

        public async override void Prepare(IProgress<string> progress)
        {
            var devicesView = _serviceProvider.GetService<IDevicesView>();
            await devicesView.InitializeAsync("CASY");

            base.Prepare(progress);
        }

        public override void Deinitialize(IProgress<string> progress)
        {
            base.Deinitialize(progress);
        }

        private void ConfigureService(IServiceCollection services)
        {
            var retryPolicy = Policy.Handle<HttpRequestException>()
                .OrResult<HttpResponseMessage>(message => !message.IsSuccessStatusCode)
                .WaitAndRetryAsync(new[]
                    {
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(10),
                        TimeSpan.FromSeconds(20),
                        TimeSpan.FromSeconds(10),
                        TimeSpan.FromSeconds(20),
                        TimeSpan.FromSeconds(10),
                        TimeSpan.FromSeconds(20),
                    },
                    (result, timeSpan, retryCount, context) =>
                    {
                        Console.WriteLine(
                            $"Request failed with {result.Result?.StatusCode}. Waiting {timeSpan} before next retry. Retry attempt {retryCount}");
                    });

            services.AddHttpClient("Default", client =>
            {
            }).AddPolicyHandler(retryPolicy);

            services.AddSingleton<IApiTokenClient, ApiTokenCacheClient>();
            services.AddDistributedMemoryCache();
            services.AddSingleton<ApiClient>();

            services.AddSingleton<IMessageBus, RabbitMqBus>();

            services.AddSingleton<IDevicesView, DevicesView>();
        }

        private void Configure(HostBuilderContext arg1, object arg2)
        {
            throw new NotImplementedException();
        }
    }
}
