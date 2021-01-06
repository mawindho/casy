using MigraDoc.Rendering;
using OLS.Casy.Controller.Api;
using OLS.Casy.Core;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Models.Enums;
using OLS.Casy.Monitoring.Api;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Base.ViewModels.Wizard;
using OLS.Casy.Ui.Core.Api;
using OLS.Casy.Ui.Core.Documents;
using OLS.Casy.Ui.MainControls.Model;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using OLS.Casy.Models;
using OLS.Casy.Ui.Core.ViewModels;
using OLS.Casy.Ui.Core.Views;
using OLS.Casy.Core.Logging.Api;

namespace OLS.Casy.Ui.MainControls.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(ToolboxViewModel))]
    public class ToolboxViewModel : DialogModelBase, IPartImportsSatisfiedNotification
    {
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly IServiceController _serviceController;
        private readonly ICalibrationController _calibrationController;
        private readonly ICasyController _casyController;
        private readonly IMeasureController _measureController;
        private readonly IMeasureResultManager _generalMeasureResultManager;
        private readonly ICompositionFactory _compositionFactory;
        private readonly IAuthenticationService _authenticationService;
        private readonly ILocalizationService _localizationService;
        private readonly IMeasureResultDataCalculationService _measureResultDataCalculationService;
        private readonly IMonitoringService _monitoringService;
        private readonly IErrorContoller _errorController;
        private readonly Casy.Core.Notification.Api.INotificationService _notificationService;
        private readonly IDatabaseStorageService _databaseStorageService;
        private readonly IDocumentSettingsManager _documentSettingsManager;
        private readonly ILogger _logger;
        private readonly IMeasureResultManager _measureResultManager;
        private readonly IEnvironmentService _environmentService;

        private bool _isValidServicePin;
        private bool _messagesChanged;
        private string _command;
        private string _servicePin;

        [ImportingConstructor]
        public ToolboxViewModel(IEventAggregatorProvider eventAggregatorProvider,
            [Import(AllowDefault = true)] IServiceController serviceController,
            [Import(AllowDefault = true)] ICalibrationController calibrationController,
            ICasyController casyController,
            IMeasureController measureController,
            IMeasureResultManager generalMeasureResultManager,
            ICompositionFactory compositionFactory,
            IAuthenticationService authenticationService,
            IMeasureResultDataCalculationService measureResultDataCalculationService,
            ILocalizationService localizationService,
            IMonitoringService monitoringService,
            IErrorContoller errorController,
            Casy.Core.Notification.Api.INotificationService notificationService,
            IDatabaseStorageService databaseStorageService,
            IDocumentSettingsManager documentSettingsManager,
            IMeasureResultManager measureResultManager,
            ILogger logger,
            IEnvironmentService environmentService)
        {
            _eventAggregatorProvider = eventAggregatorProvider;
            _serviceController = serviceController;
            _calibrationController = calibrationController;
            _casyController = casyController;
            _measureController = measureController;
            _generalMeasureResultManager = generalMeasureResultManager;
            _compositionFactory = compositionFactory;
            _authenticationService = authenticationService;
            _localizationService = localizationService;
            _measureResultDataCalculationService = measureResultDataCalculationService;
            _monitoringService = monitoringService;
            _errorController = errorController;
            _notificationService = notificationService;
            _databaseStorageService = databaseStorageService;
            _documentSettingsManager = documentSettingsManager;
            _logger = logger;
            _measureResultManager = measureResultManager;
            _environmentService = environmentService;

            CommandLineMessages = new ObservableCollection<CommandLineMessage>();
        }

        public ObservableCollection<CommandLineMessage> CommandLineMessages { get; }

        public bool IsValidServicePin
        {
            get => _isValidServicePin;
            set
            {
                if (value == _isValidServicePin) return;
                _isValidServicePin = value;
                NotifyOfPropertyChange();
            }
        }

        public string ServicePin
        {
            get => _servicePin;
            set
            {
                if (value == _servicePin) return;
                _servicePin = value;
                NotifyOfPropertyChange();
            }
        }

        public ICommand SubmitServicePinCommand => new OmniDelegateCommand(OnSubmitServicePin);

        public ICommand GetCalibrationVerificationDataCommand => new OmniDelegateCommand(GetCalibrationVerificationData);

        public ICommand GetHeaderCommand => new OmniDelegateCommand(GetHeader);

        public ICommand RequestLastChecksumCommand => new OmniDelegateCommand(RequestLastChecksum);

        public ICommand GetTestPatternCommand
        {
            get { return new OmniDelegateCommand<string>(capillary => GetTestPattern(int.Parse(capillary))); }
        }

        public bool Has45Capillary => _calibrationController.KnownCappillarySizes.Contains(45);

        public bool Has60Capillary => _calibrationController.KnownCappillarySizes.Contains(60);

        public bool Has150Capillary => _calibrationController.KnownCappillarySizes.Contains(150);

        public ICommand DryCommand => new OmniDelegateCommand(Dry);

        public ICommand LEDTestCommand => new OmniDelegateCommand(LEDTest);

        public ICommand BlowCommand => new OmniDelegateCommand(Blow);

        public ICommand SuckCommand => new OmniDelegateCommand(Suck);

        public ICommand PrintStatisticsCommand => new OmniDelegateCommand(PrintStatistics);

        public ICommand SelfTestCommand => new OmniDelegateCommand(SelfTest);

        public ICommand StartUpCommand => new OmniDelegateCommand(StartUp);

        public ICommand ShutDownCommand => new OmniDelegateCommand(ShutDown);

        public ICommand DeepCleanCommand
        {
            get { return new OmniDelegateCommand<string>(capillary => DeepClean(int.Parse(capillary))); }
        }

        public ICommand WeeklyCleanCommand
        {
            get { return new OmniDelegateCommand<string>(async capillary => await WeeklyClean(int.Parse(capillary))); }
        }

        public ICommand CleanCapillaryCommand
        {
            get { return new OmniDelegateCommand<string>(capillary => CleanCapillary(int.Parse(capillary))); }
        }

        public ICommand AbandonmentCommand
        {
            get { return new OmniDelegateCommand<string>(async capillary => await Abandonment(int.Parse(capillary))); }
        }

        public ICommand CheckTightnessCommand => new OmniDelegateCommand(CheckTightness);

        public ICommand CheckRiseTimeCommand
        {
            get { return new OmniDelegateCommand<string>(capillary => CheckRiseTime(int.Parse(capillary))); }
        }

        public ICommand GetCapillaryVoltageCommand => new OmniDelegateCommand(GetCapillaryVoltage);

        public string Command
        {
            get => _command;
            set
            {
                var command = value.Split(' ');
                switch (command[0])
                {
                    case "EnterServicePIN":
                        if(command.Length < 2)
                        {
                            AddCommandLineMessage("  Usage: EnterServicePIN [ServicePIN]", "CommandLineColorRed");
                        }
                        else
                        { 
                            Task.Factory.StartNew(() => EnterServicePIN(command[1]));
                        }
                        break;
                    case "Dry":
                        Task.Factory.StartNew(Dry);
                        break;
                    case "VerifyCalibrationData":
                        Task.Factory.StartNew(GetCalibrationVerificationData);
                        break;
                    case "GetHeader":
                        Task.Factory.StartNew(GetHeader);
                        break;
                    case "GetLastChecksum":
                        Task.Factory.StartNew(RequestLastChecksum);
                        break;
                    case "GetTestPattern":
                        if (command.Length < 2 || !int.TryParse(command[1], out var capillary))
                        {
                            AddCommandLineMessage("  Usage: GetTestPattern [Capillary]", "CommandLineColorRed");
                        }
                        else
                        {
                            Task.Factory.StartNew(() => GetTestPattern(capillary));
                        }
                        break;
                    case "LEDTest":
                        Task.Factory.StartNew(LEDTest);
                        break;
                    case "Blow":
                        Task.Factory.StartNew(Blow);
                        break;
                    case "Suck":
                        Task.Factory.StartNew(Suck);
                        break;
                    case "GetValveStates":
                        Task.Factory.StartNew(GetValveStates);
                        break;
                    case "SetValveState":
                        if (command.Length < 3)
                        {
                            AddCommandLineMessage("  Usage: SetValveState [Valve] [State]", "CommandLineColorRed");
                        }
                        else
                        {
                            Task.Factory.StartNew(() => SetValveState(command[1], command[2]));
                        }
                        break;
                    case "SelfTest":
                        Task.Factory.StartNew(SelfTest);
                        break;
                    case "SetCapillaryVoltage":
                        if (command.Length < 2)
                        {
                            AddCommandLineMessage("  Usage: SetCapillaryVoltage [value]", "CommandLineColorRed");
                        }
                        else
                        {
                            Task.Factory.StartNew(() => SetCapillaryVoltage(command[1]));
                        }
                        break;
                    case "GetPressure":
                        Task.Factory.StartNew(GetPressure);
                        break;
                    case "GetCapillaryVoltage":
                        Task.Factory.StartNew(GetCapillaryVoltage);
                        break;
                    case "Clean":
                        if (command.Length < 2)
                        {
                            AddCommandLineMessage("  Usage: Clean [Number of Cleans]", "CommandLineColorRed");
                        }
                        else
                        {
                            Task.Factory.StartNew(() => Clean(command[1]));
                        }
                        break;
                    case "Measure":
                        if(command.Length < 2)
                        {
                            Task.Factory.StartNew(() => Measure(command[1]));
                        }
                        else
                        {
                            Task.Factory.StartNew(() => Measure(command[1]));
                        }
                        break;
                    case "CleanWaste":
                        Task.Factory.StartNew(CleanWaste);
                        break;
                    case "CleanCapillary":
                        Task.Factory.StartNew(CleanCapillary);
                        break;
                    case "ClearErrorBytes":
                        Task.Factory.StartNew(ClearErrorBytes);
                        break;
                    case "ResetStatistic":
                        Task.Factory.StartNew(ResetStatistic);
                        break;
                    case "ResetCalibration":
                        Task.Factory.StartNew(ResetCalibration);
                        break;
                    case "GetDateTime":
                        Task.Factory.StartNew(GetDateTime);
                        break;
                    case "SetDateTime":
                        if (command.Length < 2)
                        {
                            AddCommandLineMessage("  Usage: SetDateTime [NOW|UTCNOW|DateTime]", "CommandLineColorRed");
                        }
                        else
                        {
                            Task.Factory.StartNew(() => SetDateTime(command[1]));
                        }
                        break;
                    case "GetSerialNumber":
                        Task.Factory.StartNew(GetSerialNumber);
                        break;
                    case "SetSerialNumber":
                        if (command.Length < 2)
                        {
                            AddCommandLineMessage("  Usage: SetSerialNumber [SerialNumber]", "CommandLineColorRed");
                        }
                        else
                        {
                            Task.Factory.StartNew(() => SetSerialNumber(command[1]));
                        }
                        break;
                    case "DeepCleanWizard":
                        if (command.Length < 2)
                        {
                            AddCommandLineMessage("  Usage: DeepCleanWizard [CapillarySize (45/60/150)]", "CommandLineColorRed");
                        }
                        Task.Factory.StartNew(() => DeepClean(int.Parse(command[1])));
                        break;
                    case "WeeklyCleanWizard":
                        if (command.Length < 2)
                        {
                            AddCommandLineMessage("  Usage: WeeklyCleanWizard [CapillarySize (45/60/150)]", "CommandLineColorRed");
                        }
                        Task.Factory.StartNew(() => WeeklyClean(int.Parse(command[1])));
                        break;
                    case "CleanCapillaryWizard":
                        if (command.Length < 2)
                        {
                            AddCommandLineMessage("  CleanCapillaryWizard: CleanCapillary [CapillarySize (45/60/150)]", "CommandLineColorRed");
                        }
                        Task.Factory.StartNew(() => CleanCapillary(int.Parse(command[1])));
                        break;
                    case "AbandonmentWizard":
                        if (command.Length < 2)
                        {
                            AddCommandLineMessage("  CleanCapillaryWizard: CleanCapillary [CapillarySize (45/60/150)]", "CommandLineColorRed");
                        }
                        Task.Factory.StartNew(() => Abandonment(int.Parse(command[1])));
                        break;
                    case "StartUp":
                        Task.Factory.StartNew(() => StartUp());
                        break;
                    case "ShutDown":
                        Task.Factory.StartNew(() => ShutDown());
                        break;
                    default:
                        AddCommandLineMessage("Service>"+value, "CommandLineColorBlue");
                        AddCommandLineMessage("  Unknown command", "CommandLineColorRed");
                        break;
                }
                _command = string.Empty;
                NotifyOfPropertyChange();
            }
        }

        private void AddCommandLineMessage(string message, string color)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                _messagesChanged = false;
                CommandLineMessages.Add(new CommandLineMessage
                {
                    Message = message,
                    Color = color
                });
                MessagesChanged = true;
            }, DispatcherPriority.ApplicationIdle);
        }

        public bool MessagesChanged
        {
            get => _messagesChanged;
            set
            {
                if (value == _messagesChanged) return;
                _messagesChanged = value;
                NotifyOfPropertyChange();
            }
        }

        private void OnSubmitServicePin()
        {
            EnterServicePIN(ServicePin);
            ServicePin = string.Empty;
        }

        private void EnterServicePIN(string servicePin)
        {
            AddCommandLineMessage("Service>EnterServicePIN " + (servicePin ?? string.Empty), "CommandLineColorBlue");
            var isValid = false;
            if (!string.IsNullOrEmpty(servicePin))
            {
                isValid = _serviceController.VerifyMasterPin(servicePin);
            }

            if (isValid)
            {
                AddCommandLineMessage("  Service PIN verification successful", "CommandLineColorGreen");
                IsValidServicePin = true;
            }
            else
            {
                AddCommandLineMessage("  Service PIN verification failed", "CommandLineColorRed");
                IsValidServicePin = false;
            }
        }

        private void GetCalibrationVerificationData()
        {
            AddCommandLineMessage("Service>VerifyCalibrationData", "CommandLineColorBlue");

            var result = _calibrationController.VerifyActiveCalibration(new Progress<string>());
            if(result)
            {
                AddCommandLineMessage("  Calibration data verification successful", "CommandLineColorGreen");
            }
            else
            {
                AddCommandLineMessage("  Calibration data verification failed", "CommandLineColorRed");
            }
        }

        private void GetHeader()
        {
            AddCommandLineMessage("Service>GetHeader", "CommandLineColorBlue");

            var result = _serviceController.GetHeader();

            AddCommandLineMessage("  " + Encoding.Default.GetString(result.Item1), "CommandLineColorGreen");
        }

        private void RequestLastChecksum()
        {
            AddCommandLineMessage("Service>GetLastChecksum", "CommandLineColorBlue");

            var result = _serviceController.RequestLastChecksum();

            AddCommandLineMessage("  " + result.ToString(), "CommandLineColorGreen");
        }

        private void GetTestPattern(int capillary)
        {
            AddCommandLineMessage("Service>GetTestPattern " + capillary, "CommandLineColorBlue");

            Task.Factory.StartNew(async () =>
            {
                var success = await _generalMeasureResultManager.SaveChangedMeasureResults();

                if(success != ButtonResult.Cancel)
                { 
                    var result = await _serviceController.GetTestPattern(capillary);

                    if(result != null)
                    {
                        Thread.Sleep(500);
                        _eventAggregatorProvider.Instance.GetEvent<NavigateToEvent>().Publish(new NavigationArgs(NavigationCategory.AnalyseGraph)
                        {
                            Parameter = true
                        });
                        result.IsTemporary = false;
                        await _generalMeasureResultManager.AddSelectedMeasureResults(new[] { result });
                        OkCommand.Execute(null);
                    }
                    else
                    {
                        AddCommandLineMessage("  Failed", "CommandLineColorRed");
                    }
                }
                else
                {
                    AddCommandLineMessage("  Aborted", "CommandLineColorYellow");
                }
            });
        }

        private void Dry()
        {
            AddCommandLineMessage("Service>Dry", "CommandLineColorBlue");

            Task.Factory.StartNew(() =>
            {
                var result = _serviceController.Dry();
                if (result.ErrorResultType != ErrorResultType.NoError)
                {
                    _eventAggregatorProvider.Instance.GetEvent<ErrorResultEvent>().Publish(result);

                    AddCommandLineMessage("  Error: " + string.Join(" - ", result.SoftErrorDetails.Select(sed => sed.ErrorCode)) + " - " + string.Join(" - ", result.FatalErrorDetails.Select(sed => sed.ErrorCode)), "CommandLineColorRed");
                }
                else
                {
                    AddCommandLineMessage("  Finished", "CommandLineColorGreen");
                }
            });
        }

        private void LEDTest()
        {
            AddCommandLineMessage("Service>LEDTest", "CommandLineColorBlue");

            var result = _serviceController.PerformLEDTest();

            var ledNames = Enum.GetNames(typeof(LEDs));
            foreach(var ledName in ledNames)
            {
                LEDs ledValue = (LEDs) Enum.Parse(typeof(LEDs), ledName);
                AddCommandLineMessage($"  {ledName}: {(result.Contains(ledValue) ? "ON" : "OFF")}", "CommandLineColorGreen");
            }
        }

        private void Blow()
        {
            AddCommandLineMessage("Service>Blow", "CommandLineColorBlue");

            _serviceController.PerformBlow();

            AddCommandLineMessage("  Finished", "CommandLineColorGreen");
        }

        private void Suck()
        {
            AddCommandLineMessage("Service>Suck", "CommandLineColorBlue");

            _serviceController.PerformSuck();

            AddCommandLineMessage("  Finished", "CommandLineColorGreen");
        }

        private void GetValveStates()
        {
            AddCommandLineMessage("Service>GetValveStates", "CommandLineColorBlue");

            var states = _serviceController.GetValvesState();

            var valveNames = Enum.GetNames(typeof(Valves));
            foreach (var valveName in valveNames)
            {
                var valveValue = (Valves)Enum.Parse(typeof(Valves), valveName);
                AddCommandLineMessage($"  {valveName}: {(states[valveValue] ? "ON" : "OFF")}", "CommandLineColorGreen");
            }
        }

        private void SetValveState(string valveName, string stateName)
        {
            AddCommandLineMessage($"Service>SetValveState {valveName} {stateName}", "CommandLineColorBlue");

            if(!Enum.TryParse(valveName, out Valves valve))
            {
                AddCommandLineMessage("  Invalid valve name parameter: '" + valveName + "'", "CommandLineColorRed");
                AddCommandLineMessage("  Possible parameter: " + string.Join(", ", Enum.GetNames(typeof(Valves))), "CommandLineColorRed");
                return;
            }

            if(!bool.TryParse(stateName, out var state))
            {
                if(!int.TryParse(stateName, out var intState))
                {
                    AddCommandLineMessage("  Invalid state parameter: '" + stateName + "'", "CommandLineColorRed");
                    AddCommandLineMessage("  Possible parameter: 1, 0, true, false", "CommandLineColorRed");
                    return;
                }

                if(intState != 0 && intState != 1)
                {
                    AddCommandLineMessage("  Invalid state parameter: '" + stateName + "'", "CommandLineColorRed");
                    AddCommandLineMessage("  Possible parameter: 1, 0, true, false", "CommandLineColorRed");
                    return;
                }

                state = intState == 1;
            }

            var result = _serviceController.SetValveState(valve, state);

            if(result)
            { 
                AddCommandLineMessage("  Finished", "CommandLineColorGreen");
            }
            else
            { 
                AddCommandLineMessage("  Failed", "CommandLineColorRed");
            }
        }

        private void PrintStatistics()
        {
            AddCommandLineMessage("Service>PrintStatistics", "CommandLineColorBlue");

            Task.Factory.StartNew(() =>
            {
                var statistic = _serviceController.GetStatistic();
                var serialNumber = _casyController.GetSerialNumber();

                var renderer = new PdfDocumentRenderer(false);
                var statisticDocument = new StatisticDocument(_localizationService, _authenticationService, _documentSettingsManager, _environmentService);
                renderer.Document = statisticDocument.CreateDocument(statistic, serialNumber, this._errorController);

                var fileName = $"{"Statistic"}_{serialNumber}_{DateTime.Now:yyyy-dd-M--HH-mm-ss}.pdf";

                renderer.RenderDocument();

                if (renderer.PdfDocument.Version < 14)
                {
                    renderer.PdfDocument.Version = 14;
                }

                var appDataFolder = Path.Combine(Environment.GetFolderPath(
                        Environment.SpecialFolder.ApplicationData), "Casy", "temp");

                if (!Directory.Exists(appDataFolder))
                {
                    Directory.CreateDirectory(appDataFolder);
                }
                fileName = Path.Combine(appDataFolder, fileName);

                renderer.PdfDocument.Save(fileName);

                try
                {
                    Process.Start(fileName);
                }
                catch (Exception)
                {
                    Task.Factory.StartNew(() =>
                    {
                        var awaiter2 = new ManualResetEvent(false);

                        var messageBoxDialogWrapper = new ShowMessageBoxDialogWrapper()
                        {
                            Awaiter = awaiter2,
                            Message = "FailedToOpenFile_Message",
                            Title = "FailedToOpenFile_Title",
                            MessageParameter = new[] { fileName }
                        };

                        _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>()
                            .Publish(messageBoxDialogWrapper);
                        awaiter2.WaitOne();
                    });
                }
            });
        }

        private void SetCapillaryVoltage(string stringValue)
        {
            AddCommandLineMessage($"Service>SetCapillaryVoltage {stringValue}", "CommandLineColorBlue");

            if(!int.TryParse(stringValue, out var value))
            {
                AddCommandLineMessage("  Invalid parameter: '" + stringValue + "'", "CommandLineColorRed");
                AddCommandLineMessage("  Valid values: 220 - 255", "CommandLineColorRed");
                return;
            }
            if(value < 220 || value > 255)
            {
                AddCommandLineMessage("  Invalid parameter: '" + stringValue + "'", "CommandLineColorRed");
                AddCommandLineMessage("  Valid values: 220 - 255", "CommandLineColorRed");
                return;
            }

            var result = _serviceController.SetCapillaryVoltage(value);

            if (result)
            {
                AddCommandLineMessage("  Finished", "CommandLineColorGreen");
            }
            else
            {
                AddCommandLineMessage("  Failed", "CommandLineColorRed");
            }
        }

        private void GetCapillaryVoltage()
        {
            AddCommandLineMessage("Service>GetCapillaryVoltage", "CommandLineColorBlue");
            var result = _serviceController.GetCapillaryVoltage();
            AddCommandLineMessage($"  Capillary Voltage: {result}", "CommandLineColorGreen");
        }

        private void SelfTest()
        {
            Task.Factory.StartNew(() => { 
                AddCommandLineMessage("Service>SelfTest", "CommandLineColorBlue");
                _casyController.StartSelfTest(false);
                AddCommandLineMessage("  Finished", "CommandLineColorGreen");
            });
        }

        private void StartUp()
        {
            Task.Factory.StartNew(() => {
                AddCommandLineMessage("Service>StartUp", "CommandLineColorBlue");
                _serviceController.StartStartupWizard();
                AddCommandLineMessage("  Finished", "CommandLineColorGreen");
            });
        }

        private void ShutDown()
        {
            Task.Factory.StartNew(() => {
                AddCommandLineMessage("Service>ShutDown", "CommandLineColorBlue");
                _serviceController.StartShutdownWizard();
                AddCommandLineMessage("  Finished", "CommandLineColorGreen");
            });
        }

        private void DeepClean(int capillarySize)
        {
            AddCommandLineMessage("Service>DeepCleanWizard " + capillarySize, "CommandLineColorBlue");

            Task.Factory.StartNew(async () =>
            {
                var success = await _generalMeasureResultManager.SaveChangedMeasureResults();

                if (success != ButtonResult.Cancel)
                {
                    var awaiter = new ManualResetEvent(false);

                    var viewModelExport = _compositionFactory.GetExport<IWizardContainerViewModel>();
                    var viewModel = viewModelExport.Value;

                    var initialWizardStepViewModel = new StandardWizardStepViewModel
                    {
                        PrimaryHeader =
                            _localizationService.GetLocalizedString("DeepCleanWizard_InitialStep_PrimaryHeader"),
                        PrimaryText =
                            _localizationService.GetLocalizedString("DeepCleanWizard_InitialStep_PrimaryText"),
                        SecondaryHeader =
                            _localizationService.GetLocalizedString("DeepCleanWizard_InitialStep_SecondaryHeader"),
                        SecondaryText =
                            _localizationService.GetLocalizedString("DeepCleanWizard_InitialStep_SecondaryText"),
                        ThirdHeader =
                            _localizationService.GetLocalizedString("DeepCleanWizard_InitialStep_ThirdHeader"),
                        ThirdText = _localizationService.GetLocalizedString("DeepCleanWizard_InitialStep_ThirdText"),
                        NextButtonPressedAction = () =>
                        {
                            var result = _measureController.Clean(3);
                            return !result.HasCanceled;
                        }
                    };

                    //0
                    viewModel.AddWizardStepViewModel(initialWizardStepViewModel);

                    var wizardStep2ViewModel = new StandardWizardStepViewModel
                    {
                        PrimaryHeader =
                            _localizationService.GetLocalizedString("DeepCleanWizard_Step2to4_PrimaryHeader"),
                        PrimaryText = _localizationService.GetLocalizedString("DeepCleanWizard_Step2to4_PrimaryText"),
                        SecondaryHeader =
                            _localizationService.GetLocalizedString("DeepCleanWizard_Step2to4_SecondaryHeader"),
                        SecondaryText =
                            _localizationService.GetLocalizedString("DeepCleanWizard_Step2to4_SecondaryText"),
                        NextButtonPressedAction = () =>
                        {
                            var result = _measureController.Clean(5);
                            return !result.HasCanceled;
                        }
                    };

                    //1
                    viewModel.AddWizardStepViewModel(wizardStep2ViewModel);
                    //2
                    viewModel.AddWizardStepViewModel(wizardStep2ViewModel);
                    //3
                    viewModel.AddWizardStepViewModel(wizardStep2ViewModel);

                    var wizardStep3ViewModel = new StandardWizardStepViewModel
                    {
                        PrimaryHeader =
                            _localizationService.GetLocalizedString("DeepCleanWizard_Step5to7_PrimaryHeader"),
                        PrimaryText = _localizationService.GetLocalizedString("DeepCleanWizard_Step5to7_PrimaryText"),
                        SecondaryHeader =
                            _localizationService.GetLocalizedString("DeepCleanWizard_Step5to7_SecondaryHeader"),
                        SecondaryText =
                            _localizationService.GetLocalizedString("DeepCleanWizard_Step5to7_SecondaryText"),
                        NextButtonPressedAction = () =>
                        {
                            var result = _measureController.Clean(5);
                            return !result.HasCanceled;
                        }
                    };

                    //4
                    viewModel.AddWizardStepViewModel(wizardStep3ViewModel);
                    //5
                    viewModel.AddWizardStepViewModel(wizardStep3ViewModel);
                    //6
                    viewModel.AddWizardStepViewModel(wizardStep3ViewModel);

                    var measureCount = 1;

                    var resultStepViewModel = new BackgroundResultWizardStepViewModel
                    {
                        Header = _localizationService.GetLocalizedString("BackgroundWizzard_Result_Header"),
                        Text = _localizationService.GetLocalizedString("BackgroundWizzard_Result_Text"),
                        NextButtonText =
                            _localizationService.GetLocalizedString("BackgroundWizzard_Result_Button_Repeat"),
                        NextButtonPressedAction = () =>
                        {
                            measureCount = 3;
                            viewModel.GotoStep(27);
                        },
                        CancelButtonText =
                            _localizationService.GetLocalizedString("BackgroundWizzard_Result_Button_Accept"),
                        CancelButtonPressedAction = () =>
                        {
                            measureCount = 1;
                            viewModel.GotoStep(29);
                        }
                    };

                    var wizardStep4ViewModel = new StandardWizardStepViewModel
                    {
                        PrimaryHeader = _localizationService.GetLocalizedString("DeepCleanWizard_Step8_PrimaryHeader"),
                        PrimaryText = _localizationService.GetLocalizedString("DeepCleanWizard_Step8_PrimaryText"),
                        NextButtonPressedAction = () =>
                        {
                            var task = Task.Run(async () =>
                                await _serviceController.MeasureBackground(capillarySize, false, measureCount));
                            var (_, item2) = task.Result;

                            if (item2.HasCanceled)
                            {
                                return false;
                            }

                            if (item2.FatalErrorDetails.Count > 0 || item2.SoftErrorDetails.Count > 0)
                            {
                                _eventAggregatorProvider.Instance.GetEvent<ErrorResultEvent>()
                                    .Publish(item2);
                            }

                            return true;
                        }
                    };

                    //7
                    viewModel.AddWizardStepViewModel(wizardStep4ViewModel);

                    var wizardStep5ViewModel = new StandardWizardStepViewModel
                    {
                        PrimaryHeader =
                            _localizationService.GetLocalizedString("DeepCleanWizard_Step9_PrimaryHeader"),
                        PrimaryText = _localizationService.GetLocalizedString("DeepCleanWizard_Step9_PrimaryText")
                    };

                    //8
                    viewModel.AddWizardStepViewModel(wizardStep5ViewModel);

                    var wizardStep6ViewModel = new StandardWizardStepViewModel
                    {
                        PrimaryHeader =
                            _localizationService.GetLocalizedString("DeepCleanWizard_Step10_PrimaryHeader"),
                        PrimaryText = _localizationService.GetLocalizedString("DeepCleanWizard_Step10_PrimaryText")
                    };

                    //9
                    viewModel.AddWizardStepViewModel(wizardStep6ViewModel);

                    var wizardStep7ViewModel = new StandardWizardStepViewModel
                    {
                        PrimaryHeader =
                            _localizationService.GetLocalizedString("DeepCleanWizard_Step11to15_PrimaryHeader"),
                        PrimaryText = _localizationService.GetLocalizedString("DeepCleanWizard_Step11to15_PrimaryText"),
                        SecondaryHeader =
                            _localizationService.GetLocalizedString("DeepCleanWizard_Step11to15_SecondaryHeader"),
                        SecondaryText =
                            _localizationService.GetLocalizedString("DeepCleanWizard_Step11to15_SecondaryText"),
                        NextButtonPressedAction = () =>
                        {
                            var cleanResult = _measureController.Clean(5);

                            if (cleanResult.HasCanceled)
                            {
                                return false;
                            }

                            Thread.Sleep(500);

                            var task = Task.Run(async () =>
                                await _serviceController.MeasureBackground(capillarySize, false, measureCount));
                            var (_, item2) = task.Result;

                            if (item2.HasCanceled)
                            {
                                return false;
                            }

                            if (item2.FatalErrorDetails.Count > 0 || item2.SoftErrorDetails.Count > 0)
                            {
                                _eventAggregatorProvider.Instance.GetEvent<ErrorResultEvent>()
                                    .Publish(item2);
                            }

                            return true;
                        }
                    };

                    //10
                    viewModel.AddWizardStepViewModel(wizardStep7ViewModel);

                    //11
                    viewModel.AddWizardStepViewModel(wizardStep7ViewModel);

                    //12
                    viewModel.AddWizardStepViewModel(wizardStep7ViewModel);

                    //13
                    viewModel.AddWizardStepViewModel(wizardStep7ViewModel);

                    //14
                    viewModel.AddWizardStepViewModel(wizardStep7ViewModel);

                    //15
                    viewModel.AddWizardStepViewModel(wizardStep7ViewModel);

                    var wizardStep8ViewModel = new TimerWizardStepViewModel
                    {
                        Header = _localizationService.GetLocalizedString("DeepCleanWizard_Step16_PrimaryHeader"),
                        Text = _localizationService.GetLocalizedString("DeepCleanWizard_Step16_PrimaryText"),
                        TimeSpan = TimeSpan.FromHours(1)
                    };

                    //16
                    viewModel.AddWizardStepViewModel(wizardStep8ViewModel);

                    //17
                    viewModel.AddWizardStepViewModel(wizardStep7ViewModel);

                    //18
                    viewModel.AddWizardStepViewModel(wizardStep7ViewModel);

                    //19
                    viewModel.AddWizardStepViewModel(wizardStep7ViewModel);

                    //20
                    viewModel.AddWizardStepViewModel(wizardStep7ViewModel);

                    //21
                    viewModel.AddWizardStepViewModel(wizardStep7ViewModel);

                    var wizardStep9ViewModel = new StandardWizardStepViewModel
                    {
                        PrimaryHeader =
                            _localizationService.GetLocalizedString("DeepCleanWizard_Step17to19_PrimaryHeader"),
                        PrimaryText = _localizationService.GetLocalizedString("DeepCleanWizard_Step17to19_PrimaryText"),
                        SecondaryHeader =
                            _localizationService.GetLocalizedString("DeepCleanWizard_Step17to19_SecondaryHeader"),
                        SecondaryText =
                            _localizationService.GetLocalizedString("DeepCleanWizard_Step17to19_SecondaryText"),
                        NextButtonPressedAction = () =>
                        {
                            var result = _measureController.Clean(5);
                            return !result.HasCanceled;
                        }
                    };

                    //22
                    viewModel.AddWizardStepViewModel(wizardStep9ViewModel);
                    //23
                    viewModel.AddWizardStepViewModel(wizardStep9ViewModel);
                    //24
                    viewModel.AddWizardStepViewModel(wizardStep9ViewModel);

                    var wizardStep10ViewModel = new StandardWizardStepViewModel
                    {
                        PrimaryHeader =
                            _localizationService.GetLocalizedString("DeepCleanWizard_Step20_PrimaryHeader"),
                        PrimaryText = _localizationService.GetLocalizedString("DeepCleanWizard_Step20_PrimaryText")
                    };

                    //25
                    viewModel.AddWizardStepViewModel(wizardStep10ViewModel);

                    var wizardStep11ViewModel = new StandardWizardStepViewModel
                    {
                        PrimaryHeader = _localizationService.GetLocalizedString("DeepCleanWizard_Step21_PrimaryHeader"),
                        PrimaryText = _localizationService.GetLocalizedString("DeepCleanWizard_Step21_PrimaryText"),
                        SecondaryHeader =
                            _localizationService.GetLocalizedString("DeepCleanWizard_Step21_SecondaryHeader"),
                        SecondaryText = _localizationService.GetLocalizedString("DeepCleanWizard_Step21_SecondaryText"),
                        NextButtonPressedAction = () =>
                        {
                            var result = _measureController.Clean(5);
                            return !result.HasCanceled;
                        }
                    };

                    //26
                    viewModel.AddWizardStepViewModel(wizardStep11ViewModel);

                    var wizardStep12ViewModel = new StandardWizardStepViewModel
                    {
                        PrimaryHeader = _localizationService.GetLocalizedString("DeepCleanWizard_Step22_PrimaryHeader"),
                        PrimaryText = _localizationService.GetLocalizedString("DeepCleanWizard_Step22_PrimaryText"),
                        SecondaryHeader =
                            _localizationService.GetLocalizedString("DeepCleanWizard_Step22_SecondaryHeader"),
                        SecondaryText = _localizationService.GetLocalizedString("DeepCleanWizard_Step22_SecondaryText"),
                        NextButtonPressedAction = () =>
                        {
                            var task = Task.Run(async () =>
                                await _serviceController.MeasureBackground(capillarySize, false, measureCount));
                            var (item1, item2) = task.Result;

                            if (item2.HasCanceled)
                            {
                                return false;
                            }

                            if (item2.FatalErrorDetails.Count > 0 || item2.SoftErrorDetails.Count > 0)
                            {
                                _eventAggregatorProvider.Instance.GetEvent<ErrorResultEvent>()
                                    .Publish(item2);
                            }

                            if (item2.FatalErrorDetails.Count > 0 || item2.SoftErrorDetails.Count > 0)
                            {
                                resultStepViewModel.TotalCountsState = "Red";
                                resultStepViewModel.Text =
                                    _localizationService.GetLocalizedString("BackgroundWizzard_Result_Text_Error");
                            }

                            var countsPerMlItem = item1?.MeasureResultItemsContainers[MeasureResultItemTypes.CountsPerMl].MeasureResultItem;

                            if (countsPerMlItem == null) return true;
                            resultStepViewModel.TotalCounts =
                                countsPerMlItem.ResultItemValue.ToString("0.00E+00");

                            switch (capillarySize)
                            {
                                case 150:
                                    resultStepViewModel.TotalCountsState = countsPerMlItem.ResultItemValue > 200
                                        ? "Red"
                                        : countsPerMlItem.ResultItemValue > 100 ? "Yellow" : "Green";
                                    break;
                                case 60:
                                    resultStepViewModel.TotalCountsState = countsPerMlItem.ResultItemValue > 400
                                        ? "Red"
                                        : countsPerMlItem.ResultItemValue > 200 ? "Yellow" : "Green";
                                    break;
                                case 45:
                                    resultStepViewModel.TotalCountsState =
                                        countsPerMlItem.ResultItemValue > 6000
                                            ? "Red"
                                            : countsPerMlItem.ResultItemValue > 3000 ? "Yellow" : "Green";
                                    break;
                            }

                            var task2 = Task.Run(async () =>
                                await _measureResultDataCalculationService.SumMeasureResultDataAsync(
                                    item1));
                            var chartDataSet = task2.Result;
                            resultStepViewModel.SetChartData(chartDataSet,
                                item1.MeasureSetup.SmoothedDiameters);

                            return true;
                        }
                    };

                    //27
                    viewModel.AddWizardStepViewModel(wizardStep12ViewModel);

                    //28
                    viewModel.AddWizardStepViewModel(resultStepViewModel);

                    var finalWizardStepViewModel = new StandardWizardStepViewModel
                    {
                        PrimaryHeader = _localizationService.GetLocalizedString("DeepCleanWizard_Button_Finish"),
                        PrimaryText = _localizationService.GetLocalizedString("DeepCleanWizard_Button_Finish"),
                        NextButtonText = _localizationService.GetLocalizedString("DeepCleanWizard_Button_Finish"),
                        IsCancelButtonVisible = false
                    };

                    //29
                    viewModel.AddWizardStepViewModel(finalWizardStepViewModel);
                    viewModel.WizardTitle = "DeepCleanWizard_Title";

                    var titleBinding = new Binding("Title") {Source = viewModel};

                    var wrapper = new ShowCustomDialogWrapper
                    {
                        Awaiter = awaiter,
                        TitleBinding = titleBinding,
                        DataContext = viewModel,
                        DialogType = typeof(IWizardContainerDialog)
                    };

                    _authenticationService.DisableAutoLogOff();
                    _eventAggregatorProvider.Instance.GetEvent<ShowCustomDialogEvent>().Publish(wrapper);
                    if (awaiter.WaitOne())
                    {
                    }
                    _authenticationService.EnableAutoLogOff();
                    if (viewModel.CurStep < viewModel.StepCount - 1)
                    {
                        AddCommandLineMessage("  Aborted", "CommandLineColorYellow");
                    }
                    else
                    {
                        AddCommandLineMessage("  Finished", "CommandLineColorGreen");
                    }
                    
                    _compositionFactory.ReleaseExport(viewModelExport);
                    return;
                }
                AddCommandLineMessage("  DeepCleanWizard failed.", "CommandLineColorRed");
            });
        }

        public async Task WeeklyClean(int capillarySize)
        {
            AddCommandLineMessage("Service>WeeklyCleanWizard " + capillarySize, "CommandLineColorBlue");

            _logger.Info(LogCategory.WeeklyClean, $"Weekly Clean ({capillarySize} µm capillary) routine has been started");

            await Task.Factory.StartNew(async () =>
            {
                var success = await _generalMeasureResultManager.SaveChangedMeasureResults();

                if (success != ButtonResult.Cancel)
                {
                    var curWeeklyCleanStep = "";

                    _databaseStorageService.GetSettings().TryGetValue("CurWeeklyCleanStep", out var curWeeklyCleanSetting);
                    if(curWeeklyCleanSetting != null && !string.IsNullOrEmpty(curWeeklyCleanSetting.Value))
                    {
                        var split = curWeeklyCleanSetting.Value.Split('|');
                        curWeeklyCleanStep = split[0];
                    }

                    var awaiter = new ManualResetEvent(false);

                    var viewModelExport = _compositionFactory.GetExport<IWizardContainerViewModel>();
                    var viewModel = viewModelExport.Value;

                    var initialWizardStepViewModel = new StandardWizardStepViewModel
                    {
                        PrimaryHeader =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_initialStep_PrimaryHeader"),
                        PrimaryText =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_initialStep_PrimaryText"),
                        SecondaryHeader =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_initialStep_SecondaryHeader"),
                        SecondaryText =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_initialStep_SecondaryText"),
                        ThirdHeader =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_initialStep_ThirdHeader"),
                        ThirdText = _localizationService.GetLocalizedString("WeeklyCleanWizard_initialStep_ThirdText"),
                        NextButtonPressedAction = () =>
                        {
                            var result = _measureController.Clean(3);
                            if (!result.HasCanceled)
                            {
                                _databaseStorageService.SaveSetting("CurWeeklyCleanStep", "0|" + capillarySize);
                            }

                            return !result.HasCanceled;
                        }
                    };

                    //0
                    viewModel.AddWizardStepViewModel(initialWizardStepViewModel);

                    var wizardStep2ViewModel = new StandardWizardStepViewModel
                    {
                        PrimaryHeader =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_Step2_PrimaryHeader"),
                        PrimaryText = _localizationService.GetLocalizedString("WeeklyCleanWizard_Step2_PrimaryText"),
                        SecondaryHeader =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_Step2_SecondaryHeader"),
                        SecondaryText =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_Step2_SecondaryText"),
                        NextButtonPressedAction = () =>
                        {
                            var result = _measureController.Clean(5);
                            if (!result.HasCanceled)
                            {
                                _databaseStorageService.SaveSetting("CurWeeklyCleanStep", "1|" + capillarySize);
                            }

                            return !result.HasCanceled;
                        }
                    };

                    //1
                    viewModel.AddWizardStepViewModel(wizardStep2ViewModel);

                    var measureCount = 1;

                    var wizardStep3ViewModel = new StandardWizardStepViewModel
                    {
                        PrimaryHeader =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_Step3_PrimaryHeader"),
                        PrimaryText = _localizationService.GetLocalizedString("WeeklyCleanWizard_Step3_PrimaryText"),
                        SecondaryHeader =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_Step3_SecondaryHeader"),
                        SecondaryText =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_Step3_SecondaryText"),
                        NextButtonPressedAction = () =>
                        {
                            var cleanResult = this._measureController.Clean(5);
                            if (cleanResult.HasCanceled)
                            {
                                return false;
                            }

                            Thread.Sleep(200);

                            var task = Task.Run(async () =>
                                await this._serviceController.MeasureBackground(capillarySize, false, measureCount));
                            var (_, item2) = task.Result;

                            if (item2.HasCanceled)
                            {
                                return false;
                            }

                            if (item2.FatalErrorDetails.Count > 0 || item2.SoftErrorDetails.Count > 0)
                            {
                                _eventAggregatorProvider.Instance.GetEvent<ErrorResultEvent>()
                                    .Publish(item2);
                            }

                            _databaseStorageService.SaveSetting("CurWeeklyCleanStep", "2|" + capillarySize);
                            return true;
                        }
                    };

                    //2
                    viewModel.AddWizardStepViewModel(wizardStep3ViewModel);

                    var wizardStep4ViewModel = new TimerWizardStepViewModel
                    {
                        Header = _localizationService.GetLocalizedString("WeeklyCleanWizard_Step4_PrimaryHeader"),
                        Text = _localizationService.GetLocalizedString("WeeklyCleanWizard_Step4_PrimaryText"),
                        NextButtonPressedAction = () =>
                        {
                            _databaseStorageService.SaveSetting("CurWeeklyCleanStep", "3|" + capillarySize);
                            return true;
                        }
                    };

                    wizardStep4ViewModel.TimeSpan = curWeeklyCleanStep == "2" ? TimeSpan.FromSeconds(0) : TimeSpan.FromHours(1);
                    
                    //3
                    viewModel.AddWizardStepViewModel(wizardStep4ViewModel);

                    var wizardStep5ViewModel = new StandardWizardStepViewModel
                    {
                        PrimaryHeader =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_Step3_PrimaryHeader"),
                        PrimaryText = _localizationService.GetLocalizedString("WeeklyCleanWizard_Step3_PrimaryText"),
                        SecondaryHeader =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_Step3_SecondaryHeader"),
                        SecondaryText =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_Step3_SecondaryText"),
                        NextButtonPressedAction = () =>
                        {
                            var cleanResult = _measureController.Clean(5);
                            if (cleanResult.HasCanceled)
                            {
                                return false;
                            }

                            Thread.Sleep(200);

                            var task = Task.Run(async () =>
                                await _serviceController.MeasureBackground(capillarySize, false, measureCount));
                            var (_, item2) = task.Result;

                            if (item2.HasCanceled)
                            {
                                return false;
                            }

                            if (item2.FatalErrorDetails.Count > 0 || item2.SoftErrorDetails.Count > 0)
                            {
                                _eventAggregatorProvider.Instance.GetEvent<ErrorResultEvent>()
                                    .Publish(item2);
                            }

                            _databaseStorageService.SaveSetting("CurWeeklyCleanStep", "4|" + capillarySize);
                            return true;
                        }
                    };

                    //4
                    viewModel.AddWizardStepViewModel(wizardStep5ViewModel);

                    var wizardStep6ViewModel = new StandardWizardStepViewModel
                    {
                        PrimaryHeader =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_Step2_PrimaryHeader"),
                        PrimaryText = _localizationService.GetLocalizedString("WeeklyCleanWizard_Step2_PrimaryText"),
                        SecondaryHeader =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_Step2_SecondaryHeader"),
                        SecondaryText =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_Step2_SecondaryText"),
                        NextButtonPressedAction = () =>
                        {
                            var result = _measureController.Clean(5);
                            if (!result.HasCanceled)
                            {
                                _databaseStorageService.SaveSetting("CurWeeklyCleanStep", "5|" + capillarySize);
                            }

                            return !result.HasCanceled;
                        }
                    };

                    //5
                    viewModel.AddWizardStepViewModel(wizardStep6ViewModel);

                    var wizardStep7ViewModel = new StandardWizardStepViewModel
                    {
                        PrimaryHeader =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_Step5_PrimaryHeader"),
                        PrimaryText = _localizationService.GetLocalizedString("WeeklyCleanWizard_Step5_PrimaryText"),
                        SecondaryHeader =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_Step5_SecondaryHeader"),
                        SecondaryText =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_Step5_SecondaryText"),
                        NextButtonPressedAction = () =>
                        {
                            var result = _measureController.Clean(5);
                            if (!result.HasCanceled)
                            {
                                _databaseStorageService.SaveSetting("CurWeeklyCleanStep", "7|" + capillarySize);
                            }

                            return !result.HasCanceled;
                        }
                    };

                    //6
                    viewModel.AddWizardStepViewModel(wizardStep7ViewModel);

                    var backgroundResultStepViewModel = new BackgroundResultWizardStepViewModel
                    {
                        Header = _localizationService.GetLocalizedString("BackgroundWizzard_Result_Header"),
                        Text = _localizationService.GetLocalizedString("BackgroundWizzard_Result_Text"),
                        NextButtonText =
                            _localizationService.GetLocalizedString("BackgroundWizzard_Result_Button_Repeat"),
                        CanNextButtonCommand = true,
                        CancelButtonText =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_Button_Finish"),
                        IsPrintButtonVisible = true,
                        NextButtonPressedAction = () =>
                        {
                            measureCount = 3;
                            viewModel.GotoStep(7);
                        },
                        CancelButtonPressedAction = () => { }
                    };

                    backgroundResultStepViewModel.PrintButtonPressedAction = async () =>
                    {
                        var renderer = new PdfDocumentRenderer(false);

                        var measureResult = backgroundResultStepViewModel.MeasureResult;
                        if (measureResult != null)
                        {
                            await Task.Factory.StartNew(() =>
                            {
                                var awaiter2 = new ManualResetEvent(false);

                                var viewModelExport2 = _compositionFactory.GetExport<AddCommentDialogModel>();
                                var viewModel2 = viewModelExport2.Value;

                                var wrapper2 = new ShowCustomDialogWrapper()
                                {
                                    Awaiter = awaiter2,
                                    Title = "AddCommentDialog_Title",
                                    DataContext = viewModel2,
                                    DialogType = typeof(AddCommentDialog)
                                };

                                _eventAggregatorProvider.Instance.GetEvent<ShowCustomDialogEvent>().Publish(wrapper2);

                                if (!awaiter2.WaitOne()) return;
                                if (!string.IsNullOrEmpty(viewModel2.Comment))
                                {
                                    measureResult.Comment = viewModel2.Comment;
                                }
                                _compositionFactory.ReleaseExport(viewModelExport);
                            });

                            string documentLogoName = "OLS_Logo.png";
                            if (!_databaseStorageService.GetSettings().TryGetValue("DocumentLogoName", out var documentLogoSetting))
                            {
                                _databaseStorageService.SaveSetting("DocumentLogoName", "OLS_Logo.png");
                            }
                            else
                            {
                                documentLogoName = documentLogoSetting.Value;
                            }
                            var weeklyCleanResultDocument = new WeeklyCleanResultDocument(_localizationService, _authenticationService, _documentSettingsManager, _environmentService);
                            renderer.Document = weeklyCleanResultDocument.CreateDocument(measureResult);

                            if (measureResult == null) return;

                            var fileName =
                                $"Background_{measureResult.MeasureSetup.CapillarySize}_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.pdf";
                            renderer.RenderDocument();

                            if (renderer.PdfDocument.Version < 14)
                            {
                                renderer.PdfDocument.Version = 14;
                            }

                            var appDataFolder = Path.Combine(Environment.GetFolderPath(
                                Environment.SpecialFolder.ApplicationData), "Casy", "temp");

                            if (!Directory.Exists(appDataFolder))
                            {
                                Directory.CreateDirectory(appDataFolder);
                            }
                            fileName = Path.Combine(appDataFolder, fileName);

                            renderer.PdfDocument.Save(fileName);
                            try
                            {
                                Process.Start(fileName);
                            }
                            catch (Exception)
                            {
                                await Task.Factory.StartNew(() =>
                                {
                                    var awaiter2 = new ManualResetEvent(false);

                                    var messageBoxDialogWrapper = new ShowMessageBoxDialogWrapper()
                                    {
                                        Awaiter = awaiter2,
                                        Message = "FailedToOpenFile_Message",
                                        Title = "FailedToOpenFile_Title",
                                        MessageParameter = new[] { fileName }
                                    };

                                    _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>()
                                        .Publish(messageBoxDialogWrapper);
                                    awaiter2.WaitOne();
                                });
                            }
                        }
                    };

                    var finalWizardStepViewModel = new StandardWizardStepViewModel
                    {
                        PrimaryHeader =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_FinalStep_PrimaryHeader"),
                        PrimaryText =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_FinalStep_PrimaryText"),
                        SecondaryHeader =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_FinalStep_SecondaryHeader"),
                        SecondaryText =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_FinalStep_SecondaryText"),
                        NextButtonPressedAction = () =>
                        {
                            var task = Task.Run(async () =>
                                await _serviceController.MeasureBackground(capillarySize, false, measureCount));
                            var (item1, item2) = task.Result;

                            if (item2.HasCanceled)
                            {
                                return false;
                            }

                            if (item2.FatalErrorDetails.Count > 0 || item2.SoftErrorDetails.Count > 0)
                            {
                                _eventAggregatorProvider.Instance.GetEvent<ErrorResultEvent>()
                                    .Publish(item2);
                            }

                            if (item2.FatalErrorDetails.Count > 0 || item2.SoftErrorDetails.Count > 0)
                            {
                                backgroundResultStepViewModel.TotalCountsState = "Red";
                                backgroundResultStepViewModel.Text =
                                    _localizationService.GetLocalizedString("BackgroundWizzard_Result_Text_Error");
                            }

                            var countsPerMlItem = item1?.MeasureResultItemsContainers[MeasureResultItemTypes.CountsPerMl].MeasureResultItem;

                            if (countsPerMlItem == null) return true;
                            backgroundResultStepViewModel.TotalCounts =
                                countsPerMlItem.ResultItemValue.ToString("0.00E+00");

                            switch (capillarySize)
                            {
                                case 150:
                                    backgroundResultStepViewModel.TotalCountsState =
                                        countsPerMlItem.ResultItemValue > 200
                                            ? "Red"
                                            : countsPerMlItem.ResultItemValue > 100 ? "Yellow" : "Green";
                                    break;
                                case 60:
                                    backgroundResultStepViewModel.TotalCountsState =
                                        countsPerMlItem.ResultItemValue > 400
                                            ? "Red"
                                            : countsPerMlItem.ResultItemValue > 200 ? "Yellow" : "Green";
                                    break;
                                case 45:
                                    backgroundResultStepViewModel.TotalCountsState =
                                        countsPerMlItem.ResultItemValue > 6000
                                            ? "Red"
                                            : countsPerMlItem.ResultItemValue > 3000 ? "Yellow" : "Green";
                                    break;
                            }

                            var task2 = Task.Run(async () =>
                                await _measureResultDataCalculationService.SumMeasureResultDataAsync(
                                    item1));
                            var chartDataSet = task2.Result;
                            backgroundResultStepViewModel.MeasureResult = item1;
                            backgroundResultStepViewModel.SetChartData(chartDataSet,
                                item1.MeasureSetup.SmoothedDiameters);

                            return true;
                        }
                    };

                    //7
                    viewModel.AddWizardStepViewModel(finalWizardStepViewModel);

                    //8
                    viewModel.AddWizardStepViewModel(backgroundResultStepViewModel);

                    viewModel.WizardTitle = "WeeklyCleanWizard_Title";

                    var titleBinding = new Binding("Title") {Source = viewModel};

                    var wrapper = new ShowCustomDialogWrapper
                    {
                        Awaiter = awaiter,
                        TitleBinding = titleBinding,
                        DataContext = viewModel,
                        DialogType = typeof(IWizardContainerDialog)
                    };

                    if(!string.IsNullOrEmpty(curWeeklyCleanStep) && curWeeklyCleanStep != "0")
                    {
                        viewModel.GotoStep(int.Parse(curWeeklyCleanStep) + 1);
                    }

                    _authenticationService.DisableAutoLogOff();
                    _eventAggregatorProvider.Instance.GetEvent<ShowCustomDialogEvent>().Publish(wrapper);
                    if (awaiter.WaitOne())
                    {
                        if(viewModel.CurStep >= viewModel.StepCount)
                        { 
                            _monitoringService.UpdateMonitoringValue(Enum.GetName(typeof(MonitoringTypes), MonitoringTypes.WeeklyCleanNotification), DateTime.UtcNow.Ticks.ToString());
                            _monitoringService.UpdateMonitoringValue(Enum.GetName(typeof(MonitoringTypes), MonitoringTypes.WeeklyCleanAnnouncementNotification), DateTime.UtcNow.Ticks.ToString());
                        }

                        _databaseStorageService.SaveSetting("CurWeeklyCleanStep", "");
                    }
                    _authenticationService.EnableAutoLogOff();
                    if (viewModel.CurStep < viewModel.StepCount)
                    {
                        _logger.Info(LogCategory.WeeklyClean, "Weekly Clean routine ({capillarySize} µm capillary) has been aborted");
                        AddCommandLineMessage("  Aborted", "CommandLineColorYellow");
                    }
                    else
                    {
                        _logger.Info(LogCategory.WeeklyClean, "Weekly Clean routine ({capillarySize} µm capillary) has been finished successfully");
                        var toRemoves = _notificationService.Notifications.Where(n => n.NotificationType == NotificationType.WeeklyCleanMandetory).ToList();
                        foreach (var toRemove in toRemoves)
                        {
                            _notificationService.RemoveNotification(toRemove);
                        }
                        _serviceController.IsWeeklyCleanMandatory = false;
                        AddCommandLineMessage("  Finished", "CommandLineColorGreen");
                    }
                    _compositionFactory.ReleaseExport(viewModelExport);

                    return;
                }
                _logger.Info(LogCategory.WeeklyClean, "Weekly Clean routine ({capillarySize} µm capillary) has been failed");
                AddCommandLineMessage("  WeeklyCleanWizard failed.", "CommandLineColorRed");
            });
        }

        public async Task Abandonment(int capillarySize)
        {
            AddCommandLineMessage("Service>AbandonmentWizard " + capillarySize, "CommandLineColorBlue");

            await Task.Factory.StartNew(async () =>
            {
                var success = await _generalMeasureResultManager.SaveChangedMeasureResults();

                if (success != ButtonResult.Cancel)
                {
                    var awaiter = new ManualResetEvent(false);

                    var viewModelExport = _compositionFactory.GetExport<IWizardContainerViewModel>();
                    var viewModel = viewModelExport.Value;

                    var initialWizardStepViewModel = new StandardWizardStepViewModel
                    {
                        PrimaryHeader =
                            _localizationService.GetLocalizedString("AbandonmentWizard_initialStep_PrimaryHeader"),
                        PrimaryText =
                            _localizationService.GetLocalizedString("AbandonmentWizard_initialStep_PrimaryText"),
                        SecondaryHeader =
                            _localizationService.GetLocalizedString("AbandonmentWizard_initialStep_SecondaryHeader"),
                        SecondaryText =
                            _localizationService.GetLocalizedString("AbandonmentWizard_initialStep_SecondaryText"),
                        ThirdHeader =
                            _localizationService.GetLocalizedString("AbandonmentWizard_initialStep_ThirdHeader"),
                        ThirdText = _localizationService.GetLocalizedString("AbandonmentWizard_initialStep_ThirdText"),
                        NextButtonPressedAction = () =>
                        {
                            var result = _measureController.Clean(3);
                            return !result.HasCanceled;
                        }
                    };

                    //0
                    viewModel.AddWizardStepViewModel(initialWizardStepViewModel);

                    var wizardStep2ViewModel = new StandardWizardStepViewModel
                    {
                        PrimaryHeader =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_Step2_PrimaryHeader"),
                        PrimaryText = _localizationService.GetLocalizedString("WeeklyCleanWizard_Step2_PrimaryText"),
                        SecondaryHeader =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_Step2_SecondaryHeader"),
                        SecondaryText =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_Step2_SecondaryText"),
                        NextButtonPressedAction = () =>
                        {
                            var result = _measureController.Clean(5);
                            return !result.HasCanceled;
                        }
                    };

                    //1
                    viewModel.AddWizardStepViewModel(wizardStep2ViewModel);

                    var measureCount = 1;

                    var wizardStep3ViewModel = new StandardWizardStepViewModel
                    {
                        PrimaryHeader =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_Step3_PrimaryHeader"),
                        PrimaryText = _localizationService.GetLocalizedString("WeeklyCleanWizard_Step3_PrimaryText"),
                        SecondaryHeader =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_Step3_SecondaryHeader"),
                        SecondaryText =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_Step3_SecondaryText"),
                        NextButtonPressedAction = () =>
                        {
                            var cleanResult = this._measureController.Clean(5);
                            if (cleanResult.HasCanceled)
                            {
                                return false;
                            }

                            Thread.Sleep(200);

                            var task = Task.Run(async () =>
                                await this._serviceController.MeasureBackground(capillarySize, false, measureCount));
                            var (_, item2) = task.Result;

                            if (item2.HasCanceled)
                            {
                                return false;
                            }

                            if (item2.FatalErrorDetails.Count > 0 || item2.SoftErrorDetails.Count > 0)
                            {
                                _eventAggregatorProvider.Instance.GetEvent<ErrorResultEvent>()
                                    .Publish(item2);
                            }

                            return true;
                        }
                    };

                    //2
                    viewModel.AddWizardStepViewModel(wizardStep3ViewModel);

                    var wizardStep4ViewModel = new TimerWizardStepViewModel
                    {
                        Header = _localizationService.GetLocalizedString("WeeklyCleanWizard_Step4_PrimaryHeader"),
                        Text = _localizationService.GetLocalizedString("WeeklyCleanWizard_Step4_PrimaryText"),
                        NextButtonPressedAction = () => true,
                        TimeSpan = TimeSpan.FromHours(1)
                    };

                    //3
                    viewModel.AddWizardStepViewModel(wizardStep4ViewModel);

                    var wizardStep5ViewModel = new StandardWizardStepViewModel
                    {
                        PrimaryHeader =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_Step3_PrimaryHeader"),
                        PrimaryText = _localizationService.GetLocalizedString("WeeklyCleanWizard_Step3_PrimaryText"),
                        SecondaryHeader =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_Step3_SecondaryHeader"),
                        SecondaryText =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_Step3_SecondaryText"),
                        NextButtonPressedAction = () =>
                        {
                            var cleanResult = _measureController.Clean(5);
                            if (cleanResult.HasCanceled)
                            {
                                return false;
                            }

                            Thread.Sleep(200);

                            var task = Task.Run(async () =>
                                await _serviceController.MeasureBackground(capillarySize, false, measureCount));
                            var (_, item2) = task.Result;

                            if (item2.HasCanceled)
                            {
                                return false;
                            }

                            if (item2.FatalErrorDetails.Count > 0 || item2.SoftErrorDetails.Count > 0)
                            {
                                _eventAggregatorProvider.Instance.GetEvent<ErrorResultEvent>()
                                    .Publish(item2);
                            }
                            return true;
                        }
                    };

                    //4
                    viewModel.AddWizardStepViewModel(wizardStep5ViewModel);

                    var wizardStep6ViewModel = new StandardWizardStepViewModel
                    {
                        PrimaryHeader =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_Step2_PrimaryHeader"),
                        PrimaryText = _localizationService.GetLocalizedString("WeeklyCleanWizard_Step2_PrimaryText"),
                        SecondaryHeader =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_Step2_SecondaryHeader"),
                        SecondaryText =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_Step2_SecondaryText"),
                        NextButtonPressedAction = () =>
                        {
                            var result = _measureController.Clean(5);
                            return !result.HasCanceled;
                        }
                    };

                    //5
                    viewModel.AddWizardStepViewModel(wizardStep6ViewModel);

                    var wizardStep7ViewModel = new StandardWizardStepViewModel
                    {
                        PrimaryHeader =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_Step5_PrimaryHeader"),
                        PrimaryText = _localizationService.GetLocalizedString("WeeklyCleanWizard_Step5_PrimaryText"),
                        SecondaryHeader =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_Step5_SecondaryHeader"),
                        SecondaryText =
                            _localizationService.GetLocalizedString("WeeklyCleanWizard_Step5_SecondaryText"),
                        NextButtonPressedAction = () =>
                        {
                            var result = _measureController.Clean(5);
                            return !result.HasCanceled;
                        }
                    };

                    //6
                    viewModel.AddWizardStepViewModel(wizardStep7ViewModel);

                    var wizardStep8ViewModel = new StandardWizardStepViewModel
                    {
                        PrimaryHeader =
                            _localizationService.GetLocalizedString("AbandonmentWizard_Step7_PrimaryHeader"),
                        PrimaryText = _localizationService.GetLocalizedString("AbandonmentWizard_Step7_PrimaryText"),
                        SecondaryHeader =
                            _localizationService.GetLocalizedString("AbandonmentWizard_Step7_SecondaryHeader"),
                        SecondaryText =
                            _localizationService.GetLocalizedString("AbandonmentWizard_Step7_SecondaryText"),
                        NextButtonPressedAction = () =>
                        {
                            var result = _measureController.Clean(3);

                            return !result.HasCanceled;
                        }
                    };

                    viewModel.AddWizardStepViewModel(wizardStep8ViewModel);

                    viewModel.AddWizardStepViewModel(wizardStep8ViewModel);

                    var wizardStep9ViewModel = new StandardWizardStepViewModel
                    {
                        PrimaryHeader =
                            _localizationService.GetLocalizedString("AbandonmentWizard_Step8_PrimaryHeader"),
                        PrimaryText = _localizationService.GetLocalizedString("AbandonmentWizard_Step8_PrimaryText"),
                        SecondaryHeader =
                            _localizationService.GetLocalizedString("AbandonmentWizard_Step8_SecondaryHeader"),
                        SecondaryText =
                            _localizationService.GetLocalizedString("AbandonmentWizard_Step8_SecondaryText"),
                        NextButtonPressedAction = () =>
                        {
                            var result = _serviceController.Dry();
                            if (result.HasCanceled) return false;

                            result = _serviceController.Dry();
                            return !result.HasCanceled;
                        }
                    };

                    viewModel.AddWizardStepViewModel(wizardStep9ViewModel);

                    var finalWizardStepViewModel = new StandardWizardStepViewModel
                    {
                        PrimaryHeader =
                            _localizationService.GetLocalizedString("AbandonmentWizard_FinalStep_PrimaryHeader"),
                        PrimaryText =
                            _localizationService.GetLocalizedString("AbandonmentWizard_FinalStep_PrimaryText"),
                        SecondaryHeader =
                            _localizationService.GetLocalizedString("AbandonmentWizard_FinalStep_SecondaryHeader"),
                        SecondaryText =
                            _localizationService.GetLocalizedString("AbandonmentWizard_FinalStep_SecondaryText"),
                        CancelButtonText = _localizationService.GetLocalizedString("WeeklyCleanWizard_Button_Finish"),
                        CanNextButtonCommand = false
                    };

                    //7
                    viewModel.AddWizardStepViewModel(finalWizardStepViewModel);

                    viewModel.WizardTitle = "AbandonmentWizard_Title";

                    var titleBinding = new Binding("Title") { Source = viewModel };

                    var wrapper = new ShowCustomDialogWrapper
                    {
                        Awaiter = awaiter,
                        TitleBinding = titleBinding,
                        DataContext = viewModel,
                        DialogType = typeof(IWizardContainerDialog)
                    };

                    _authenticationService.DisableAutoLogOff();
                    _eventAggregatorProvider.Instance.GetEvent<ShowCustomDialogEvent>().Publish(wrapper);
                    if (awaiter.WaitOne())
                    {
                    }
                    _authenticationService.EnableAutoLogOff();
                    if (viewModel.CurStep < viewModel.StepCount - 1)
                    {
                        AddCommandLineMessage("  Aborted", "CommandLineColorYellow");
                    }
                    else
                    {
                        AddCommandLineMessage("  Finished", "CommandLineColorGreen");
                    }
                    _compositionFactory.ReleaseExport(viewModelExport);

                    return;
                }
                AddCommandLineMessage("  AbandonmentWizard failed.", "CommandLineColorRed");
            });
        }

        private void CleanCapillary(int capillarySize)
        {
            AddCommandLineMessage("Service>CleanCapillaryWizard " + capillarySize, "CommandLineColorBlue");

            Task.Factory.StartNew(async () =>
            {
                var success = await _generalMeasureResultManager.SaveChangedMeasureResults();

                if (success != ButtonResult.Cancel)
                {
                    var awaiter = new ManualResetEvent(false);

                    var viewModelExport = _compositionFactory.GetExport<IWizardContainerViewModel>();
                    var viewModel = viewModelExport.Value;

                    var initialWizardStepViewModel = new StandardWizardStepViewModel
                    {
                        PrimaryHeader =
                            _localizationService.GetLocalizedString("CleanCapillaryWizard_InitialStep_PrimaryHeader"),
                        PrimaryText =
                            _localizationService.GetLocalizedString("CleanCapillaryWizard_InitialStep_PrimaryText"),
                        SecondaryHeader =
                            _localizationService.GetLocalizedString("CleanCapillaryWizard_InitialStep_SecondaryHeader"),
                        SecondaryText =
                            _localizationService.GetLocalizedString("CleanCapillaryWizard_InitialStep_SecondaryText")
                    };

                    //1                   
                    viewModel.AddWizardStepViewModel(initialWizardStepViewModel);

                    var measureCount = 1;

                    var wizardStep1ViewModel = new StandardWizardStepViewModel
                    {
                        PrimaryHeader =
                            _localizationService.GetLocalizedString("CleanCapillaryWizard_Step2_PrimaryHeader"),
                        PrimaryText = _localizationService.GetLocalizedString("CleanCapillaryWizard_Step2_PrimaryText"),
                        SecondaryHeader =
                            _localizationService.GetLocalizedString("CleanCapillaryWizard_Step2_SecondaryHeader"),
                        SecondaryText =
                            _localizationService.GetLocalizedString("CleanCapillaryWizard_Step2_SecondaryText"),
                        NextButtonPressedAction = () =>
                        {
                            var cleanResult = _measureController.Clean(3);

                            return !cleanResult.HasCanceled;
                        }
                    };

                    //2
                    viewModel.AddWizardStepViewModel(wizardStep1ViewModel);

                    var wizardStep2ViewModel = new StandardWizardStepViewModel
                    {
                        PrimaryHeader =
                            _localizationService.GetLocalizedString("CleanCapillaryWizard_Step22_PrimaryHeader"),
                        PrimaryText =
                            _localizationService.GetLocalizedString("CleanCapillaryWizard_Step22_PrimaryText"),
                        SecondaryHeader =
                            _localizationService.GetLocalizedString("CleanCapillaryWizard_Step22_SecondaryHeader"),
                        SecondaryText =
                            _localizationService.GetLocalizedString("CleanCapillaryWizard_Step22_SecondaryText"),
                        NextButtonPressedAction = () =>
                        {
                            var task = Task.Run(async () =>
                                await _serviceController.MeasureBackground(capillarySize, false, measureCount));
                            var result = task.Result;

                            if (result.Item2.HasCanceled)
                            {
                                return false;
                            }

                            if (result.Item2 == null) return true;
                            if (result.Item2.ErrorResultType != ErrorResultType.NoError)
                            {
                                if (result.Item2.FatalErrorDetails.Count == 0 &&
                                    result.Item2.SoftErrorDetails.Count > 0)
                                {
                                    if (result.Item2.SoftErrorDetails.TakeWhile(softError => softError.ErrorNumber == "0" || softError.ErrorNumber == "1").Any())
                                    {
                                        return true;
                                    }
                                }

                                _eventAggregatorProvider.Instance.GetEvent<ErrorResultEvent>()
                                    .Publish(result.Item2);
                                return false;
                            }

                            viewModel.GotoStep(4);

                            return true;
                        }
                    };

                    //3
                    viewModel.AddWizardStepViewModel(wizardStep2ViewModel);

                    var wizardStep3ViewModel = new StandardWizardStepViewModel
                    {
                        PrimaryHeader =
                            _localizationService.GetLocalizedString("CleanCapillaryWizard_Step3_PrimaryHeader"),
                        PrimaryText = _localizationService.GetLocalizedString("CleanCapillaryWizard_Step3_PrimaryText"),
                        NextButtonPressedAction = () =>
                        {
                            viewModel.GotoStep(2);
                            return true;
                        }
                    };

                    //4
                    viewModel.AddWizardStepViewModel(wizardStep3ViewModel);

                    var backgroundResultStepViewModel =
                        new BackgroundResultWizardStepViewModel
                        {
                            Header = _localizationService.GetLocalizedString("BackgroundWizzard_Result_Header"),
                            Text = _localizationService.GetLocalizedString("BackgroundWizzard_Result_Text"),
                            NextButtonText =
                                _localizationService.GetLocalizedString("BackgroundWizzard_Result_Button_Repeat"),
                            CanNextButtonCommand = true,
                            CancelButtonText =
                                _localizationService.GetLocalizedString("BackgroundWizzard_Result_Button_Accept"),
                            CancelButtonPressedAction = () => { },
                            NextButtonPressedAction = () =>
                            {
                                measureCount = 3;
                                viewModel.GotoStep(4);
                            }
                        };

                    var finalWizardStepViewModel = new StandardWizardStepViewModel
                    {
                        PrimaryHeader =
                            _localizationService.GetLocalizedString("CleanCapillaryWizard_FinalStep_PrimaryHeader"),
                        PrimaryText =
                            _localizationService.GetLocalizedString("CleanCapillaryWizard_FinalStep_PrimaryText"),
                        SecondaryHeader =
                            _localizationService.GetLocalizedString("CleanCapillaryWizard_FinalStep_SecondaryHeader"),
                        SecondaryText =
                            _localizationService.GetLocalizedString("CleanCapillaryWizard_FinalStep_SecondaryText"),
                        NextButtonText = _localizationService.GetLocalizedString("CleanCapillaryWizard_Button_Measure"),
                        CancelButtonText =
                            _localizationService.GetLocalizedString("CleanCapillaryWizard_Button_Finish"),
                        NextButtonPressedAction = () =>
                        {
                            var task = Task.Run(async () =>
                                await _serviceController.MeasureBackground(capillarySize, false, measureCount));
                            var (measureResult, errorResult) = task.Result;

                            if (errorResult.HasCanceled)
                            {
                                return false;
                            }

                            if (errorResult.FatalErrorDetails.Count > 0 || errorResult.SoftErrorDetails.Count > 0)
                            {
                                _eventAggregatorProvider.Instance.GetEvent<ErrorResultEvent>()
                                    .Publish(errorResult);
                            }

                            if (errorResult.FatalErrorDetails.Count > 0 || errorResult.SoftErrorDetails.Count > 0)
                            {
                                backgroundResultStepViewModel.TotalCountsState = "Red";
                                backgroundResultStepViewModel.Text =
                                    _localizationService.GetLocalizedString("BackgroundWizzard_Result_Text_Error");
                            }

                            var countsPerMlItem = measureResult?.MeasureResultItemsContainers[MeasureResultItemTypes.CountsPerMl].MeasureResultItem;

                            if (countsPerMlItem == null) return true;
                            backgroundResultStepViewModel.TotalCounts =
                                countsPerMlItem.ResultItemValue.ToString("0.00E+00");

                            switch (capillarySize)
                            {
                                case 150:
                                    backgroundResultStepViewModel.TotalCountsState =
                                        countsPerMlItem.ResultItemValue > 200
                                            ? "Red"
                                            : countsPerMlItem.ResultItemValue > 100 ? "Yellow" : "Green";
                                    break;
                                case 60:
                                    backgroundResultStepViewModel.TotalCountsState =
                                        countsPerMlItem.ResultItemValue > 400
                                            ? "Red"
                                            : countsPerMlItem.ResultItemValue > 200 ? "Yellow" : "Green";
                                    break;
                                case 45:
                                    backgroundResultStepViewModel.TotalCountsState =
                                        countsPerMlItem.ResultItemValue > 6000
                                            ? "Red"
                                            : countsPerMlItem.ResultItemValue > 3000 ? "Yellow" : "Green";
                                    break;
                            }

                            var task2 = Task.Run(async () =>
                                await _measureResultDataCalculationService.SumMeasureResultDataAsync(
                                    measureResult));
                            var chartDataSet = task2.Result;
                            backgroundResultStepViewModel.SetChartData(chartDataSet,
                                measureResult.MeasureSetup.SmoothedDiameters);

                            return true;
                        }
                    };

                    //5
                    viewModel.AddWizardStepViewModel(finalWizardStepViewModel);

                    //6
                    viewModel.AddWizardStepViewModel(backgroundResultStepViewModel);

                    viewModel.WizardTitle = "CleanCapillaryWizard_Title";

                    var titleBinding = new Binding("Title") {Source = viewModel};

                    var wrapper = new ShowCustomDialogWrapper
                    {
                        Awaiter = awaiter,
                        TitleBinding = titleBinding,
                        DataContext = viewModel,
                        DialogType = typeof(IWizardContainerDialog)
                    };

                    _authenticationService.DisableAutoLogOff();
                    _eventAggregatorProvider.Instance.GetEvent<ShowCustomDialogEvent>().Publish(wrapper);
                    if (awaiter.WaitOne())
                    {
                    }

                    _authenticationService.EnableAutoLogOff();

                    if (viewModel.CurStep < viewModel.StepCount - 1)
                    {
                        AddCommandLineMessage("  Aborted", "CommandLineColorYellow");
                    }
                    else
                    {
                        AddCommandLineMessage("  Finished", "CommandLineColorGreen");
                    }

                    _compositionFactory.ReleaseExport(viewModelExport);
                    return;
                }

                AddCommandLineMessage("  CleanCapillaryWizard failed.", "CommandLineColorRed");
            });
        }

        private void GetPressure()
        {
            AddCommandLineMessage("Service>GetPressure", "CommandLineColorBlue");
            var result = _serviceController.GetPressure();
            AddCommandLineMessage($"  Pressure: {result}", "CommandLineColorGreen");
        }

        private void Measure(string numString = null)
        {
            if (numString == null)
            {
                AddCommandLineMessage($"Service>Measure", "CommandLineColorBlue");
            }
            else
            {
                AddCommandLineMessage($"Service>Measure {numString}", "CommandLineColorBlue");
            }

            Task.Factory.StartNew(async () =>
            {
                var result = await this._measureResultManager.SaveChangedMeasureResults();
                if (result != ButtonResult.Cancel)
                {
                    var args = new NavigationArgs(NavigationCategory.Measurement)
                    {
                        Parameter = numString
                    };
                    _eventAggregatorProvider.Instance.GetEvent<NavigateToEvent>().Publish(args);
                }
            });
        }

        private void Clean(string numString)
        {
            AddCommandLineMessage($"Service>Clean {numString}", "CommandLineColorBlue");

            if (!int.TryParse(numString, out var value))
            {
                AddCommandLineMessage("  Invalid parameter: '" + numString + "'", "CommandLineColorRed");
                AddCommandLineMessage("  Valid values: 1 - 9", "CommandLineColorRed");
                return;
            }
            if (value < 1 || value > 9)
            {
                AddCommandLineMessage("  Invalid parameter: '" + numString + "'", "CommandLineColorRed");
                AddCommandLineMessage("  Valid values: 1 - 9", "CommandLineColorRed");
                return;
            }

            _measureController.Clean(value);
        }

        private void CleanWaste()
        {
            AddCommandLineMessage("Service>CleanWaste", "CommandLineColorBlue");

            var result = _measureController.CleanWaste();

            if (result.ErrorResultType != ErrorResultType.NoError)
            {
                _eventAggregatorProvider.Instance.GetEvent<ErrorResultEvent>().Publish(result);

                AddCommandLineMessage("  Error: " + string.Join(" - ", result.SoftErrorDetails.Select(sed => sed.ErrorCode)) + " - " + string.Join(" - ", result.FatalErrorDetails.Select(sed => sed.ErrorCode)), "CommandLineColorRed");
            }
            else
            {
                AddCommandLineMessage("  Finished", "CommandLineColorGreen");
            }
        }

        private void CleanCapillary()
        {
            AddCommandLineMessage("Service>CleanCapillary", "CommandLineColorBlue");

            var result = _measureController.CleanCapillary();

            if (result.ErrorResultType != ErrorResultType.NoError)
            {
                _eventAggregatorProvider.Instance.GetEvent<ErrorResultEvent>().Publish(result);

                AddCommandLineMessage("  Error: " + string.Join(" - ", result.SoftErrorDetails.Select(sed => sed.ErrorCode)) + " - " + string.Join(" - ", result.FatalErrorDetails.Select(sed => sed.ErrorCode)), "CommandLineColorRed");
            }
            else
            {
                AddCommandLineMessage("  Finished", "CommandLineColorGreen");
            }
        }

        private void ClearErrorBytes()
        {
            AddCommandLineMessage("Service>ClearErrorBytes", "CommandLineColorBlue");

            var result = _serviceController.ClearErrorBytes();

            if (result)
            {
                AddCommandLineMessage("  Finished", "CommandLineColorGreen");
            }
            else
            {
                AddCommandLineMessage("  Failed", "CommandLineColorRed");
            }
        }

        private void ResetStatistic()
        {
            AddCommandLineMessage("Service>ResetStatistic", "CommandLineColorBlue");

            var result = _serviceController.ResetStatistic();

            if (result)
            {
                AddCommandLineMessage("  Finished", "CommandLineColorGreen");
            }
            else
            {
                AddCommandLineMessage("  Failed", "CommandLineColorRed");
            }
        }

        private void ResetCalibration()
        {
            AddCommandLineMessage("Service>ResetCalibration", "CommandLineColorBlue");

            var result = _serviceController.ResetCalibration();

            if (result)
            {
                AddCommandLineMessage("  Finished", "CommandLineColorGreen");
            }
            else
            {
                AddCommandLineMessage("  Failed", "CommandLineColorRed");
            }
        }

        private void GetDateTime()
        {
            AddCommandLineMessage("Service>GetDateTime", "CommandLineColorBlue");
            var result = _serviceController.GetDateTime();
            if(result == null)
            {
                AddCommandLineMessage("  Failed", "CommandLineColorRed");
            }
            else
            { 
                AddCommandLineMessage("  DateTime: " + result.Item1.ToString("yyyy-MM-dd HH:mm:ss \"GMT\"zzz"), "CommandLineColorGreen");
            }
        }

        private void SetDateTime(string dateTimeString)
        {
            AddCommandLineMessage($"Service>SetDateTime {dateTimeString}", "CommandLineColorBlue");

            DateTime dateTime;
            switch (dateTimeString)
            {
                case "NOW":
                    dateTime = DateTime.Now;
                    break;
                case "UTCNOW":
                    dateTime = DateTime.UtcNow;
                    break;
                default:
                {
                    if(!DateTime.TryParse(dateTimeString, out dateTime))
                    {
                        AddCommandLineMessage("  Invalid parameter: '" + dateTimeString + "'", "CommandLineColorRed");
                        AddCommandLineMessage("  Valid values: NOW, UTCNOW, [DateTimePattern]", "CommandLineColorRed");
                        return;
                    }

                    break;
                }
            }

            var result = _serviceController.SetDateTime(dateTime);

            if (!result)
            {
                AddCommandLineMessage("  Failed", "CommandLineColorRed");
            }
            else
            {
                AddCommandLineMessage(" Finished", "CommandLineColorGreen");
            }
        }

        private void GetSerialNumber()
        {
            AddCommandLineMessage("Service>GetSerialNumber", "CommandLineColorBlue");
            var result = _casyController.GetSerialNumber();
            AddCommandLineMessage("  Serial Number: " + result, "CommandLineColorGreen");
        }

        private void SetSerialNumber(string serialNumber)
        {
            AddCommandLineMessage($"Service>SetSerialNumber {serialNumber}", "CommandLineColorBlue");
            var result = _casyController.SetSerialNumber(serialNumber);

            if (!result)
            {
                AddCommandLineMessage("  Failed", "CommandLineColorRed");
            }
            else
            {
                AddCommandLineMessage("  Finished", "CommandLineColorGreen");
            }
        }

        private void CheckTightness()
        {
            AddCommandLineMessage("Service>CheckTightness", "CommandLineColorBlue");
            Task.Factory.StartNew(async () =>
            {
                var result = await _serviceController.CheckTightness();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (result != null)
                    {
                        if(result.ErrorResult != null)
                        {
                            AddCommandLineMessage("  Errors occured", "CommandLineColorRed");
                            foreach (var softError in result.ErrorResult.SoftErrorDetails)
                            {
                                AddCommandLineMessage(
                                    !string.IsNullOrEmpty(softError.DeviceErrorName)
                                        ? $"  Error number: {softError.DeviceErrorName}"
                                        : $"  Error code: {softError.ErrorCode}",
                                    "CommandLineColorRed");
                            }
                            foreach (var fatalError in result.ErrorResult.FatalErrorDetails)
                            {
                                AddCommandLineMessage(
                                    !string.IsNullOrEmpty(fatalError.DeviceErrorName)
                                        ? $"  Error number: {fatalError.DeviceErrorName}"
                                        : $"  Error code: {fatalError.ErrorCode}",
                                    "CommandLineColorRed");
                            }
                        }
                        AddCommandLineMessage($"  High Pressure Difference: {result.MaxPressureDifference}", "CommandLineColorGreen");
                        AddCommandLineMessage($"  Low Pressure Difference: {result.MinPressureDifference}", "CommandLineColorGreen");
                    }
                    else
                    {
                        AddCommandLineMessage("  Failed.", "CommandLineColorRed");
                    }
                });
            });
        }

        private void CheckRiseTime(int capillarySize)
        {
            AddCommandLineMessage("Service>CheckRiseTime", "CommandLineColorBlue");
            Task.Factory.StartNew(async () =>
            {
                var result = await _serviceController.CheckRiseTime(capillarySize);

                if (result != null)
                {
                    AddCommandLineMessage($"  Min Time to Green LED: {result.MinTimeGreen}", "CommandLineColorGreen");
                    AddCommandLineMessage($"  Max Time to Green LED: {result.MaxTimeGreen}", "CommandLineColorGreen");
                    AddCommandLineMessage($"  Average Time to Green LED: {result.AverageTimeGreen}", "CommandLineColorGreen");
                    AddCommandLineMessage($"  Min Time to 200er LED: {result.MinTime200}", "CommandLineColorGreen");
                    AddCommandLineMessage($"  Max Time to 200er LED: {result.MaxTime200}", "CommandLineColorGreen");
                    AddCommandLineMessage($"  Average Time to 200er LED: {result.AverageTime200}", "CommandLineColorGreen");
                    AddCommandLineMessage($"  Min Time to 400er LED: {result.MinTime400}", "CommandLineColorGreen");
                    AddCommandLineMessage($"  Max Time to 400er LED: {result.MaxTime400}", "CommandLineColorGreen");
                    AddCommandLineMessage($"  Average Time to 400er LED: {result.AverageTime400}", "CommandLineColorGreen");
                    AddCommandLineMessage($"  Cycles: {result.Cycles}", "CommandLineColorGreen");
                }
                else
                {
                    AddCommandLineMessage("  Failed.", "CommandLineColorRed");
                }
            });
        }

        public void OnImportsSatisfied()
        {
            Title = _localizationService.GetLocalizedString("ToolboxView_Title");
        }
    }
}
