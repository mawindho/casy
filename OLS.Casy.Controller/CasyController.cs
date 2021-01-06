using OLS.Casy.Com.Api;
using OLS.Casy.Controller.Api;
using OLS.Casy.Core;
using OLS.Casy.Core.Activation;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Config.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Core.Logging.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Models;
using OLS.Casy.Models.Enums;
using OLS.Casy.Monitoring.Api;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace OLS.Casy.Controller
{
    /// <summary>
    /// Implementation of <see cref="ICasyController"/>.
    /// Uses serial port communication with casy device.
    /// </summary>
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(ICasyController))]
    [Export(typeof(IService))]
    public class CasyController : BaseCasyController, ICasyController, IPartImportsSatisfiedNotification
    {
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly IErrorContoller _errorController;
        private readonly ILocalizationService _localizationService;
        private readonly IEnvironmentService _environmentService;
        private readonly ITTSwitchService _ttSwitchService;
        private readonly IMonitoringService _monitoringService;
        private readonly IFileSystemStorageService _fileSystemStorageService;
        private readonly IEncryptionProvider _encryptionProvider;
        private readonly ILogger _logger;

        private bool _initialSelfTestDone;

        /// <summary>
        /// MEF importing constructor
        /// </summary>
        /// <param name="configService">Implementation of <see cref="IConfigService"/> </param>
        /// <param name="casySerialPortDriver">Implementation of <see cref="ICasySerialPortDriver"/></param>
        /// <param name="errorController">Implementation of <see cref="IErrorContoller"/></param>
        /// <param name="eventAggregatorProvider">Implementation of <see cref="IEventAggregatorProvider"/> </param>
        [ImportingConstructor]
        public CasyController(IConfigService configService,
            [Import(AllowDefault = true)] ICasySerialPortDriver casySerialPortDriver,
            [Import(AllowDefault = true)] IErrorContoller errorController,
            IEventAggregatorProvider eventAggregatorProvider,
            ILocalizationService localizationService, IEnvironmentService environmentService,
            [Import(AllowDefault = true)] ITTSwitchService ttSwitchService,
            IMonitoringService monitoringService,
            IFileSystemStorageService fileSystemStorageService,
            IEncryptionProvider encryptionProvider,
            ILogger logger)
            : base(configService, casySerialPortDriver)
        {
            _eventAggregatorProvider = eventAggregatorProvider;
            _errorController = errorController;
            _localizationService = localizationService;
            _environmentService = environmentService;
            _ttSwitchService = ttSwitchService;
            _monitoringService = monitoringService;
            _fileSystemStorageService = fileSystemStorageService;
            _encryptionProvider = encryptionProvider;
            _logger = logger;
        }

        /// <summary>
        /// Starts async a self test of the device.
        /// Self test is devided in three seperated operations
        /// - Hardware test
        /// - Software test
        /// - Pressure system test
        /// </summary>
        /// <returns>Response string of the operation</returns>
        public void StartSelfTest(bool doShowLoginScreen)
        {
            _initialSelfTestDone = true;
            _logger.Info(LogCategory.SelfTest, string.Format("Self test procedure has been started."));

            var errorResult = new ErrorResult();

            var showProgressWrapper = new ShowProgressDialogWrapper
            {
                Title = "ProgressBox_SelfTest_Title", Message = "ProgressBox_SelfTest_Message"
            };

            var pending = _localizationService.GetLocalizedString("ProgressBox_SelfTest_Result_Pending");
            showProgressWrapper.MessageParameter = new[] { pending, pending, pending };
            showProgressWrapper.IsFinished = false;

            _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);

            ExecuteSelfTest(showProgressWrapper, CasySerialPortDriver.StartHardwareSelfTest, errorResult, 0);
            ExecuteSelfTest(showProgressWrapper, CasySerialPortDriver.StartSoftwareSelfTest, errorResult, 1);
            ExecuteSelfTest(showProgressWrapper, CasySerialPortDriver.StartPressureSystemSelfTest, errorResult, 2);

            showProgressWrapper.IsFinished = true;
            _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);

            if(errorResult.SoftErrorDetails.Count > 0 || errorResult.FatalErrorDetails.Count > 0)
            {
                foreach (var error in errorResult.SoftErrorDetails)
                {
                    _logger.Info(LogCategory.SelfTest, $"Self test procedure soft error: {error.Description}");
                }

                foreach (var error in errorResult.FatalErrorDetails)
                {
                    _logger.Info(LogCategory.SelfTest, $"Self test procedure fatal error: {error.Description}");
                }

                _eventAggregatorProvider.Instance.GetEvent<ErrorResultEvent>().Publish(errorResult);
            }

            //Thread.Sleep(500);

            _logger.Info(LogCategory.SelfTest, "Self test procedure finished.");
            if (doShowLoginScreen)
            { 
                _eventAggregatorProvider.Instance.GetEvent<ShowLoginScreenEvent>().Publish();
            }
        }

        private void ExecuteSelfTest(ShowProgressDialogWrapper showProgressWrapper, Func<IProgress<string>, string> selfTestFuntion, ErrorResult errorResult, int parameterIndex)
        {
            showProgressWrapper.MessageParameter[parameterIndex] = _localizationService.GetLocalizedString("ProgressBox_SelfTest_Result_InProgress");
            _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);

            var selfTestResult = selfTestFuntion(new Progress<string>());
            var hwErrorResult = _errorController.ParseError(selfTestResult);

            if (hwErrorResult.ErrorResultType == ErrorResultType.NoError)
            {
                showProgressWrapper.MessageParameter[parameterIndex] = _localizationService.GetLocalizedString("ProgressBox_SelfTest_Result_Ok");
            }
            else
            {
                showProgressWrapper.MessageParameter[parameterIndex] = _localizationService.GetLocalizedString("ProgressBox_SelfTest_Result_Failed");
                foreach (var error in hwErrorResult.FatalErrorDetails)
                {
                    errorResult.FatalErrorDetails.Add(error);
                }
                foreach (var error in hwErrorResult.SoftErrorDetails)
                {
                    errorResult.FatalErrorDetails.Add(error);
                }

            }
            _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);
        }

        public bool IsConnected => CasySerialPortDriver != null && CasySerialPortDriver.IsConnected;

        public bool ForceCheckIsConnected()
        {
            return CasySerialPortDriver?.CheckCasyDeviceConnection() ?? false;
        }

        public override void Prepare(IProgress<string> progress)
        {
            if (CasySerialPortDriver != null)
            {
                CasySerialPortDriver.OnIsConnectedChangedEvent += OnCasyConnectionStateChanged;
                
                _monitoringService.RegisterMonitoringJob(new MonitoringJob()
                {
                    Name = Enum.GetName(typeof(MonitoringTypes), MonitoringTypes.CheckForCasyConnection),
                    IntervalInSeconds = 30,
                    JobFunction = parameter =>
                    {
                        Task.Run(() =>
                        {
                            //bool isConnected = (bool) _environmentService.GetEnvironmentInfo("IsCasyConnected");

                            //if(isConnected)
                            //{
                            //this.CasySerialPortDriver.Co
                            //}
                            CasySerialPortDriver.CheckCasyDeviceConnection();
                        });
                        return null;
                    }
                });
            }
            else
            {
                OnCasyConnectionStateChanged(this, null);
            }
        }

        /// <summary>
        /// Method will be called when all MEF imports are fullfilled
        /// </summary>
        public void OnImportsSatisfied()
        {
            
        }

        public event EventHandler<IsConnectedChangedEventArgs> OnIsConnectedChangedEvent;

        private void OnCasyConnectionStateChanged(object sender, EventArgs e)
        {
            if (CasySerialPortDriver != null && CasySerialPortDriver.IsConnected)
            {
                _environmentService.SetEnvironmentInfo("IsCasyConnected", true);

                var serialNumber = GetSerialNumber();

                _logger.Info(LogCategory.Instrument, $"CASY device has been connected: {serialNumber}.");

                if ((serialNumber.StartsWith("TT-") /*|| serialNumber.StartsWith("TTT-")*/) && _ttSwitchService != null)
                {
                    _ttSwitchService.SwitchToTTC();
                }

                if (_initialSelfTestDone)
                {
                    StartSelfTest(true);
                }
            }
            else
            {
                //_initialSelfTestDone = false;

                _logger.Info(LogCategory.Instrument, "CASY device has been disconnected.");

                _environmentService.SetEnvironmentInfo("IsCasyConnected", false);
                _eventAggregatorProvider.Instance.GetEvent<ShowLoginScreenEvent>().Publish();
            }

            if (OnIsConnectedChangedEvent == null) return;
            foreach (var @delegate in OnIsConnectedChangedEvent.GetInvocationList())
            {
                var receiver = (EventHandler<IsConnectedChangedEventArgs>) @delegate;
                receiver.BeginInvoke(this, new IsConnectedChangedEventArgs(CasySerialPortDriver != null && CasySerialPortDriver.IsConnected), null, null);
            }
        }

        public string GetSerialNumber()
        {
            var serialNumber = CasySerialPortDriver?.GetSerialNumber(new Progress<string>());
            if(serialNumber == null)
            {
                return null;
            }
            _environmentService.SetEnvironmentInfo("SerialNumber", serialNumber.Item1);
            return serialNumber.Item1;
        }

        public bool SetSerialNumber(string serialNumber)
        {
            var result = CasySerialPortDriver.SetSerialNumber(serialNumber, new Progress<string>());
            return result;
        }

        [Export("GetCounts")]
        public int GetCounts()
        {
            var license = this._environmentService.GetEnvironmentInfo("License") as License;
            return license == null ? 0 : license.CountsLeft;
        }

        [Export("StoreCounts")]
        public async void StoreCounts(int counts)
        {
            var license = this._environmentService.GetEnvironmentInfo("License") as License;
            license.CountsLeft = counts;
            this._environmentService.SetEnvironmentInfo("License", license);
            await this.WriteLicense(license);

        }

        public async Task WriteLicense(License license)
        {
            using (var memoryStream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(memoryStream, license);

                memoryStream.Seek(0, SeekOrigin.Begin);

                var bytes = new byte[memoryStream.Length];
                memoryStream.Read(bytes, 0, (int)memoryStream.Length);

                var cpuId = _environmentService.GetEnvironmentInfo("CpuId") as string;

                bytes = _encryptionProvider.Encrypt(bytes, cpuId);

                await _fileSystemStorageService.CreateFileAsync("casy.lic", bytes);
            }
        }
    }
}
