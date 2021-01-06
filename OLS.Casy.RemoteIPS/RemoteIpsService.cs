using Devices.Client;
using Devices.Common.Dto.Models;
using Devices.ReadModels.Client;
using Functions.Client;
using Functions.Common;
using Functions.Common.Dtos.Commands;
using Interactors.Client;
using Interactors.Common.Dtos.Commands;
using Interactors.ReadModels.Client;
using Items.Client;
using Items.Common;
using Items.Common.Dtos.Commands;
using Items.Common.Dtos.Models;
using Items.ReadModels.Client;
using MicroServices.Common.MessageBus;
using MicroServices.Common.RabbitMq.MessageBus;
using MicroServices.Common.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OLS.Casy.Core;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Config.Api;
using OLS.Casy.Models;
using OLS.Casy.Models.Enums;
using OLS.Casy.RemoteIPS.Api;
using Polly;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Workbooks.Client;
using Workbooks.ReadModels.Client;

namespace OLS.Casy.RemoteIPS
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IService))]
    [Export(typeof(IRemoteIpsService))]
    public class RemoteIpsService : AbstractService, IRemoteIpsService
    {
        private DevicesView _devicesView;
        private IServiceProvider _serviceProvider;
        private IEnvironmentService _environmentService;

        private DeviceDto _casyDeviceDto;
        private ISensorHandler _sensorHandler;
        private ISensorsView _sensorsView;
        private IWorkbooksView _workbooksView;
        private IWorkbooksHandler _workbooksHandler;

        [ImportingConstructor]
        public RemoteIpsService(IConfigService configService,
            IEnvironmentService environmentService)
            : base(configService)
        {
            var hostBuilder = new HostBuilder();
            hostBuilder.ConfigureAppConfiguration((hostingContext, configBuilder) =>
            {
                configBuilder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("remoteIPS.json", optional: false);
            }).ConfigureServices(ConfigureService);

            var host = hostBuilder.Build();

            _serviceProvider = host.Services;
            _environmentService = environmentService;

            //IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("remoteIPS.json").Build();
            //IMessageBus messageBus = new RabbitMqBus(configuration);
        }

        

        public async override void Prepare(IProgress<string> progress)
        {
            var devicesView = _serviceProvider.GetService<IDevicesView>();
            await devicesView.InitializeAsync("CASY");

            var serialNumber = _environmentService.GetEnvironmentInfo("SerialNumber") as string;

            _sensorHandler = _serviceProvider.GetService<ISensorHandler>();

            _casyDeviceDto = devicesView.GetDevices().FirstOrDefault(x => x.Identifier == $"CASY_{serialNumber}");
            if (_casyDeviceDto == null)
            {
                var deviceHandler = _serviceProvider.GetService<IDeviceHandler>();
                _casyDeviceDto = await deviceHandler.CreateDeviceAsync(new Devices.Common.Dto.Commands.CreateDeviceCommand
                {
                    DeviceType = "CASY", DisplayName = $"CASY_{serialNumber}", Identifier = $"CASY_{serialNumber}"
                });

                var itemsView = _serviceProvider.GetService<IItemsView>();
                await itemsView.InitializeAsync();

                var deviceItem = itemsView.GetByFullName($"{_casyDeviceDto.DeviceType}.{_casyDeviceDto.Identifier}");

                var itemsHandler = _serviceProvider.GetService<IItemsHandler>();
                var functionHandler = _serviceProvider.GetService<IFunctionHandler>();

                await _sensorHandler.CreateSensorAsync(new CreateSensorCommand
                {
                    Identifier = "IsOnline",
                    DeviceId = _casyDeviceDto.Id,
                    ValueType = "Boolean"
                }).ContinueWith(t =>
                {
                    itemsHandler.CreateItemAsync(new CreateItemCommand
                    {
                        Name = "IsOnline",
                        ItemType = ItemTypes.Boolean,
                        ParentItemFullName = deviceItem.FullName,
                        Bindings = new[]
                        {
                            new BindingDto
                            {
                                BindingConnectionId = t.Result.Id,
                                BindingConnectionType = ItemBindingConnectionType.Listen
                            }
                        }
                    }).ContinueWith(t2 =>
                    {
                        functionHandler.CreateDeviceFunctionAsync(new CreateDeviceFunctionCommand
                        {
                            Identifier = "IsOnline",
                            FunctionType = "Is Online",
                            DisplayName = "Is Online",
                            DeviceId = _casyDeviceDto.Id,
                            IsOnlineItemId = t2.Result.Id,
                            DisplayOrder = 0
                        });
                    });
                });

                await _sensorHandler.CreateSensorAsync(new CreateSensorCommand
                {
                    Identifier = "LastMeasurementResult",
                    DeviceId = _casyDeviceDto.Id,
                    ValueType = "Text"
                }).ContinueWith(t =>
                {
                    itemsHandler.CreateItemAsync(new CreateItemCommand
                    {
                        Name = "LastMeasureresult",
                        ItemType = ItemTypes.String,
                        ParentItemFullName = deviceItem.FullName,
                        Bindings = new[]
                        {
                        new BindingDto
                        {
                            BindingConnectionId = t.Result.Id,
                            BindingConnectionType = ItemBindingConnectionType.Listen
                        }
                    }
                    }).ContinueWith(t2 =>
                    {
                        functionHandler.CreateInfoFunctionAsync(new CreateInfoFunctionCommand
                        {
                            DeviceId = _casyDeviceDto.Id,
                            Identifier = "LastMeasurementResult",
                            FunctionType = "CASY LastMeasurementResult",
                            DisplayName = "Last Measurement Result",
                            DisplayItems = new List<DisplayItem>
                            {
                                new DisplayItem
                                {
                                    ItemMappings = new Dictionary<string, Guid>
                                    {
                                        { "MeasurmentResult", t2.Result.Id}
                                    },
                                    DisplayName = "{MeasurmentResult}",
                                    DisplayOrder = 0,
                                }
                            },
                            DisplayOrder = 0
                        });
                    });
                });
            }

            _sensorsView = _serviceProvider.GetService<ISensorsView>();
            await _sensorsView.InitializeAsync();

            var casySensors = _sensorsView.GetSensorsByDeviceId(_casyDeviceDto.Id);
            var isOnlineSensor = casySensors.FirstOrDefault(x => x.Identifier == "IsOnline");

            await _sensorHandler.CreateSensorValueAsync(new CreateSensorValueCommand
            {
                SensorId = isOnlineSensor.Id,
                TimeStamp = DateTime.UtcNow,
                Type = "Boolean",
                Value = true
            });

            _workbooksHandler = _serviceProvider.GetService<IWorkbooksHandler>();
            _workbooksView = _serviceProvider.GetService<IWorkbooksView>();
            await _workbooksView.InitializeAsync(false, false);

            base.Prepare(progress);
        }

        public async override void Deinitialize(IProgress<string> progress)
        {
            var casySensors = _sensorsView.GetSensorsByDeviceId(_casyDeviceDto.Id);
            var isOnlineSensor = casySensors.FirstOrDefault(x => x.Identifier == "IsOnline");

            await _sensorHandler.CreateSensorValueAsync(new CreateSensorValueCommand
            {
                SensorId = isOnlineSensor.Id,
                TimeStamp = DateTime.UtcNow,
                Type = "Boolean",
                Value = false
            });

            base.Deinitialize(progress);
        }

        public IEnumerable<string> GetWorkbookNames()
        {
            return _workbooksView.GetWorkbooks().Select(x => x.Name).AsEnumerable();
        }

        public async Task PostMeasureResult(MeasureResult measureResult, string workbookName)
        {
            var casySensors = _sensorsView.GetSensorsByDeviceId(_casyDeviceDto.Id);
            var lastMeasureResultSensor = casySensors.FirstOrDefault(x => x.Identifier == "LastMeasurementResult");

            var measureResultItem = measureResult.MeasureResultItemsContainers[MeasureResultItemTypes.Counts].MeasureResultItem;
            var measureResultValue = measureResultItem == null
                ? "---"
                : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);

            await _sensorHandler.CreateSensorValueAsync(new CreateSensorValueCommand
            {
                SensorId = lastMeasureResultSensor.Id,
                TimeStamp = DateTime.UtcNow,
                Type = "String",
                Value = $"Counts: {measureResultValue}"
            });

            if(!string.IsNullOrEmpty(workbookName))
            {
                var workbook = _workbooksView.GetWorkbooks().FirstOrDefault(x => x.Name == workbookName);
                await _workbooksHandler.CreateWorkbookMessageAsync(new Workbooks.Common.Dtos.Commands.CreateWorkbookMessageCommand()
                {
                    DeviceId = _casyDeviceDto.Id,
                    WorkbookId = workbook.Id,
                    Text = $"Counts: {measureResultValue}"
                });
            }
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
            services.AddSingleton<IDeviceHandler, DeviceHandler>();
            services.AddSingleton<IItemsView, ItemsView>();
            services.AddSingleton<ISensorsView, SensorsView>();
            services.AddSingleton<ISensorHandler, SensorHandler>();
            services.AddSingleton<IItemsHandler, ItemsHandler>();
            services.AddSingleton<IFunctionHandler, FunctionHandler>();
            services.AddSingleton<IWorkbooksView, WorkbooksView>();
            services.AddSingleton<IWorkbooksHandler, WorkbooksHandler>();
        }
    }
}
