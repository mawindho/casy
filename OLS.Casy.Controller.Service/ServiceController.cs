using MigraDoc.Rendering;
using OLS.Casy.Com.Api;
using OLS.Casy.Controller.Api;
using OLS.Casy.Core;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Config.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Core.Logging.Api;
using OLS.Casy.Core.Notification.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Models;
using OLS.Casy.Models.Enums;
using OLS.Casy.Monitoring.Api;
using OLS.Casy.Ui.Base.ViewModels.Wizard;
using OLS.Casy.Ui.Core.Api;
using OLS.Casy.Ui.Core.Documents;
using OLS.Casy.Ui.Core.ViewModels;
using OLS.Casy.Ui.Core.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using OLS.Casy.Base;

namespace OLS.Casy.Controller.Service
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IServiceController))]
    public class ServiceController : BaseCasyController, IServiceController, IPartImportsSatisfiedNotification
    {
        private readonly IMeasureResultManager _measureResultManager;
        private readonly ILocalizationService _localizationService;
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly IErrorContoller _errorController;
        private readonly ICalibrationController _calibrationController;
        private readonly IMeasureController _measureController;
        private readonly IMeasureResultDataCalculationService _measureResultDataCalculationService;
        private readonly IDatabaseStorageService _databaseStorageService;
        private readonly ICompositionFactory _compositionFactory;
        private readonly IAuthenticationService _authenticationService;
        private readonly IMonitoringService _monitoringService;
        private readonly INotificationService _notificationService;
        private readonly ILogger _logger;
        private readonly IDocumentSettingsManager _documentSettingsManager;
        private readonly IEnvironmentService _environmentService;

        private readonly object _lock = new object();

        private string _lastUsedCapillary;
        private bool _isWeeklyCleanMandatory;

        [ImportingConstructor]
        public ServiceController(IConfigService configService,
            [Import(AllowDefault = true)] ICasySerialPortDriver casySerialPortDriver,
            ILocalizationService localizationService,
            IEventAggregatorProvider eventAggregatorProvider,
            [Import(AllowDefault = true)] IErrorContoller errorController,
            [Import(AllowDefault = true)] ICalibrationController calibrationController,
            IMeasureController measureController,
            IMeasureResultDataCalculationService measureResultDataCalculationService,
            IDatabaseStorageService databaseStorageService,
            IMeasureResultManager measureResultManager,
            ICompositionFactory compositionFactory,
            IAuthenticationService authenticationService,
            [Import(AllowDefault = true)] IMonitoringService monitoringService,
            INotificationService notificationService,
            ILogger logger,
            IDocumentSettingsManager documentSettingsManager,
            IEnvironmentService environmentService)
            : base(configService, casySerialPortDriver)
        {
            _localizationService = localizationService;
            _eventAggregatorProvider = eventAggregatorProvider;
            _errorController = errorController;
            _calibrationController = calibrationController;
            _measureController = measureController;
            _measureResultDataCalculationService = measureResultDataCalculationService;
            _databaseStorageService = databaseStorageService;
            _measureResultManager = measureResultManager;
            _compositionFactory = compositionFactory;
            _authenticationService = authenticationService;
            _monitoringService = monitoringService;
            _notificationService = notificationService;
            _logger = logger;
            _documentSettingsManager = documentSettingsManager;
            _environmentService = environmentService;
        }

        public bool VerifyMasterPin(string masterPin)
        {
            _logger.Info(LogCategory.Service, $"Service function 'VerifyMasterPin' called.");
            return CasySerialPortDriver.VerifyMasterPin(masterPin, new Progress<string>());
        }

        public Tuple<byte[], uint> GetHeader()
        {
            _logger.Info(LogCategory.Service, $"Service function 'GetHeader' called.");
            return CasySerialPortDriver.GetHeader(new Progress<string>());
        }

        public uint RequestLastChecksum()
        {
            _logger.Info(LogCategory.Service, $"Service function 'RequestLastChecksum' called.");
            return CasySerialPortDriver.RequestLastChecksum(new Progress<string>());
        }

        public async Task<Tuple<MeasureResult, ErrorResult>> MeasureBackground(int capillarySize, bool showErrorResult = true, int? repeats = null)
        {
            var isCancelRequested = false;
            var mustStop = false;

            _logger.Info(LogCategory.Service, $"Background measuring procedure has been started for capillary {capillarySize}.");

            var result = new MeasureResult
            {
                MeasuredAt = DateTime.Now,
                MeasuredAtTimeZone = TimeZoneInfo.Local,
                CreatedBy = string.Format("{0} {1} ({2})", _authenticationService.LoggedInUser.FirstName, _authenticationService.LoggedInUser.LastName, _authenticationService.LoggedInUser.Identity.Name),
                IsReadOnly = true,
                IsBackground = true,
                Name = "Background " + capillarySize
            };

            ErrorResult errorResult = null;

            var showProgressWrapper = new ShowProgressDialogWrapper
            {
                Title = "ProgressBox_Background_Title",
                Message = "ProgressBox_Background_Message",
                IsCancelButtonAvailable = true,
                CancelAction = (isCancel) =>
                {
                    isCancelRequested = true;
                    if (null == errorResult) return;
                    
                    errorResult.HasCanceled = true;
                    if (!mustStop) return;
                    
                    _logger.Info(LogCategory.Service, "Background measuring procedure has been canceled.");
                    CasySerialPortDriver.Stop(new Progress<string>());
                }
            };

            var measureProgress = _localizationService.GetLocalizedString("ProgressBox_Background_Message_Calibration");

            showProgressWrapper.MessageParameter = new[] { measureProgress };
            showProgressWrapper.IsFinished = false;

            _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);

            var progress = new Progress<string>();

            var backgroundTemplate = _databaseStorageService.GetPredefinedTemplate("Background", capillarySize);
            if (backgroundTemplate == null)
            {
                throw new ArgumentNullException("Template for background capillary " + capillarySize + " not found!");
            }
            backgroundTemplate.VolumeCorrectionFactor = 10000d;
            result.MeasureSetup = backgroundTemplate;

            mustStop = true;
            var response = _calibrationController.TransferCalibration(progress, backgroundTemplate, false);
            mustStop = false;
            errorResult = _errorController.ParseError(response);

            if (errorResult.ErrorResultType == ErrorResultType.NoError && !isCancelRequested)
            {
                showProgressWrapper.MessageParameter[0] += "\n";
                showProgressWrapper.MessageParameter[0] += _localizationService.GetLocalizedString("ProgressBox_Background_Message_Measure");

                _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);

                var realRepeats = repeats ?? backgroundTemplate.Repeats;

                backgroundTemplate.Repeats = realRepeats;
                for (var i = 0; i < realRepeats; i++)
                {
                    if (!isCancelRequested)
                    {
                        switch (backgroundTemplate.Volume)
                        {
                            case Volumes.TwoHundred:
                                mustStop = true;
                                response = CasySerialPortDriver.Measure200(progress);
                                mustStop = false;
                                break;
                            case Volumes.FourHundred:
                                mustStop = true;
                                response = CasySerialPortDriver.Measure400(progress);
                                mustStop = false;
                                break;
                        }
                    }

                    IEnumerable<ErrorDetails> softErrors = null;

                    if (!isCancelRequested)
                    {
                        errorResult = _errorController.ParseError(response);

                        softErrors = errorResult.SoftErrorDetails;
                        switch (errorResult.ErrorResultType)
                        {
                            case ErrorResultType.SingleError:
                                if (!softErrors.Any())
                                {
                                    showProgressWrapper.IsFinished = true;
                                    _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);

                                    foreach(var error in errorResult.FatalErrorDetails)
                                    {
                                        _logger.Info(LogCategory.Service, $"Fatal Error occured while measuring background: {error.Description}");
                                    }

                                    // There is no soft error with special handling. Return the error result
                                    if (showErrorResult)
                                    {
                                        _eventAggregatorProvider.Instance.GetEvent<ErrorResultEvent>().Publish(errorResult);
                                    }
                                    return new Tuple<MeasureResult, ErrorResult>(null, errorResult);
                                }
                                break;
                            case ErrorResultType.MutipleError:
                                if (errorResult.FatalErrorDetails.Count > 0)
                                {
                                    showProgressWrapper.IsFinished = true;
                                    _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);

                                    foreach (var error in errorResult.FatalErrorDetails)
                                    {
                                        _logger.Info(LogCategory.Service, $"Fatal Error occured while measuring background: {error.Description}");
                                    }

                                    if (showErrorResult)
                                    {
                                        _eventAggregatorProvider.Instance.GetEvent<ErrorResultEvent>().Publish(errorResult);
                                    }
                                    return new Tuple<MeasureResult, ErrorResult>(null, errorResult);
                                }
                                break;
                        }
                    }

                    if (!isCancelRequested)
                    {
                        if (!softErrors.Any())
                        {
                            mustStop = true;
                            var measureResultData =
                                await _measureController.GetCachedMeasureResult(result, progress);
                            mustStop = false;
                            
                            if (measureResultData == null)
                            {
                                return null;
                            }
                        }
                        
                        if (softErrors.Any() && showErrorResult)
                        {
                            var softErrorHandleResult = await _measureController.HandleSoftErrors(progress, softErrors, result, LogCategory.Service);

                            switch (softErrorHandleResult)
                            {
                                case ButtonResult.Cancel:
                                    showProgressWrapper.IsFinished = true;
                                    _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);

                                    return new Tuple<MeasureResult, ErrorResult>(null, errorResult);
                                case ButtonResult.Retry:
                                    i--;
                                    //result.MeasureResultDatas.Remove(measureResultData);
                                    break;
                                //case ButtonResult.Accept:
                                 //   result.MeasureResultDatas.Add(measureResultData);
                                 //   break;
                            }
                        }
                    }

                    await _measureResultDataCalculationService.UpdateMeasureResultDataAsync(result, backgroundTemplate);
                }

                showProgressWrapper.MessageParameter[0] += "\n";
                showProgressWrapper.MessageParameter[0] += _localizationService.GetLocalizedString("ProgressBox_Background_Message_Results");

                showProgressWrapper.MessageParameter[0] += "\n";
                showProgressWrapper.MessageParameter[0] += _localizationService.GetLocalizedString("ProgressBox_Background_Message_Calculate");

                _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);
            }

            if (isCancelRequested) return new Tuple<MeasureResult, ErrorResult>(result, errorResult);
            
            showProgressWrapper.IsFinished = true;
            _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);

            if (showErrorResult && errorResult.ErrorResultType != ErrorResultType.NoError)
            {
                _eventAggregatorProvider.Instance.GetEvent<ErrorResultEvent>().Publish(errorResult);
            }

            if (!isCancelRequested)
            {
                _logger.Info(LogCategory.Service, "Background measuring procedure completed.");
            }
            return new Tuple<MeasureResult, ErrorResult>(result, errorResult);
        }

        public async Task<MeasureResult> GetTestPattern(int capillarySize)
        {
            _logger.Info(LogCategory.Service, $"Test pattern procedure has been started for capillary {capillarySize}.");

            var testPatternTemplate = _databaseStorageService.GetPredefinedTemplate("Test Pattern", capillarySize);
            var volumeCorrectionFactor =  _calibrationController.GetVolumeCorrection(testPatternTemplate);
            testPatternTemplate.VolumeCorrectionFactor = volumeCorrectionFactor;

            if (testPatternTemplate == null)
            {
                throw new ArgumentNullException("Template for test pattern capillar " + capillarySize + " not found!");
            }

            var result = new MeasureResult
            {
                MeasuredAt = DateTime.Now,
                MeasuredAtTimeZone = TimeZoneInfo.Local,
                IsReadOnly = true,
                Name = "Test Pattern " + capillarySize,
                MeasureSetup = testPatternTemplate,
                CreatedBy =
                    $"{_authenticationService.LoggedInUser.FirstName} {_authenticationService.LoggedInUser.LastName} ({_authenticationService.LoggedInUser.Identity.Name})",
                CreatedAt = DateTime.Now
            };

            var showProgressWrapper = new ShowProgressDialogWrapper
            {
                Title = "ProgressBox_TestPattern_Title", Message = "ProgressBox_TestPattern_Message"
            };

            var measureProgress = _localizationService.GetLocalizedString("ProgressBox_TestPattern_Message_Calibration");

            showProgressWrapper.MessageParameter = new[] { measureProgress };
            showProgressWrapper.IsFinished = false;

            _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);

            var progress = new Progress<string>();

            var response = _calibrationController.TransferCalibration(progress, testPatternTemplate, false);
            var errorResult = _errorController.ParseError(response);

            if(errorResult.ErrorResultType == ErrorResultType.NoError)
            {
                showProgressWrapper.MessageParameter[0] += "\n";
                showProgressWrapper.MessageParameter[0] += _localizationService.GetLocalizedString("ProgressBox_TestPattern_Message_CreateTestPattern");

                _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);

                for (var i = 0; i < testPatternTemplate.Repeats; i++)
                {
                    response = CasySerialPortDriver.CreateTestPattern(progress);
                    errorResult = _errorController.ParseError(response);

                    IEnumerable<ErrorDetails> softErrors = errorResult.SoftErrorDetails;
                    switch (errorResult.ErrorResultType)
                    {
                        case ErrorResultType.SingleError:
                            if (!softErrors.Any())
                            {
                                showProgressWrapper.IsFinished = true;
                                _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);

                                foreach (var error in errorResult.FatalErrorDetails)
                                {
                                    _logger.Info(LogCategory.Service, $"Fatal Error occured while measuring test pattern: {error.Description}");
                                }

                                // There is no soft error with special handling. Return the error result
                                _eventAggregatorProvider.Instance.GetEvent<ErrorResultEvent>().Publish(errorResult);
                                return null;
                            }
                            break;
                        case ErrorResultType.MutipleError:
                            if (errorResult.FatalErrorDetails.Count > 0)
                            {
                                foreach (var error in errorResult.FatalErrorDetails)
                                {
                                    _logger.Info(LogCategory.Service, $"Fatal Error occured while measuring test pattern: {error.Description}");
                                }

                                showProgressWrapper.IsFinished = true;
                                _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);

                                _eventAggregatorProvider.Instance.GetEvent<ErrorResultEvent>().Publish(errorResult);
                                return null;
                            }
                            break;
                    }

                    if (!softErrors.Any())
                    {
                        var measureResultData =
                            await _measureController.GetCachedMeasureResult(result, progress);

                        if (measureResultData == null)
                        {
                            return null;
                        }

                        continue;
                    }
                    
                    var softErrorHandleResult = await _measureController.HandleSoftErrors(progress, softErrors, result, LogCategory.Service);

                    switch (softErrorHandleResult)
                    {
                        case ButtonResult.Cancel:
                            showProgressWrapper.IsFinished = true;
                            _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);
                            return null;
                        case ButtonResult.Retry:
                            i--;
                            //result.MeasureResultDatas.Remove(measureResultData);
                            break;
                        //case ButtonResult.Accept:
                            //result.MeasureResultDatas.Add(measureResultData);
                            //break;
                    }
                }

                showProgressWrapper.MessageParameter[0] += "\n";
                showProgressWrapper.MessageParameter[0] += _localizationService.GetLocalizedString("ProgressBox_TestPattern_Message_RetrieveTestPattern");

                _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);
                    
                await _measureResultDataCalculationService.UpdateMeasureResultDataAsync(result, testPatternTemplate);
            }

            showProgressWrapper.IsFinished = true;
            _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);

            if (errorResult.ErrorResultType != ErrorResultType.NoError)
            {
                _eventAggregatorProvider.Instance.GetEvent<ErrorResultEvent>().Publish(errorResult);
            }

            _logger.Info(LogCategory.Service, $"Test pattern procedure completed.");
            return result;
        }

        public ErrorResult Dry()
        {
            var mustStop = false;

            _logger.Info(LogCategory.Service, "Dry procedure has been started.");
            var isCanceled = false;
            var showProgressWrapper = new ShowProgressDialogWrapper
            {
                Title = "ProgressBox_Dry_Title",
                Message = "ProgressBox_Dry_Message",
                IsCancelButtonAvailable = true,
                CancelAction = (isCancel) =>
                {
                    if (!mustStop) return;
                    _logger.Info(LogCategory.Service, "Dry procedure has been canceled.");
                    CasySerialPortDriver.Stop(new Progress<string>());
                    isCanceled = true;
                }
            };

            _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);

            mustStop = true;
            var response = CasySerialPortDriver.Dry(new Progress<string>());
            mustStop = false;

            var errorResult = _errorController.ParseError(response);

            showProgressWrapper.IsFinished = true;
            _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);

            if (errorResult.ErrorResultType == ErrorResultType.NoError)
            {
                if (!isCanceled)
                {
                    _logger.Info(LogCategory.Service, "Dry procedure has been completed successfully.");
                }
            }
            else
            {
                _logger.Info(LogCategory.Service, "Dry procedure has been completed with errors.");
            }

            return errorResult;
        }

        public LEDs[] PerformLEDTest()
        {
            var result = CasySerialPortDriver.StartLEDTest(new Progress<string>());
            var res = new List<LEDs>();
            for(var i = 0; i < 8; i++)
            {
                if((result & (1 << i)) != 0)
                {
                    res.Add((LEDs)(1 << i));
                }
            }
            
            return res.ToArray();
        }

        public bool PerformBlow()
        {
            var result = CasySerialPortDriver.PerformBlow(new Progress<string>());
            _logger.Info(LogCategory.Service, $"Service function 'PerformBlow' called with result {result.ToString()}.");
            return result;
        }

        public bool PerformSuck()
        {
            var result = CasySerialPortDriver.PerformSuck(new Progress<string>());
            _logger.Info(LogCategory.Service, $"Service function 'PerformSuck' called with result {result.ToString()}.");
            return result;
        }

        public Dictionary<Valves, bool> GetValvesState()
        {
            var result = CasySerialPortDriver.GetValveState(new Progress<string>());
            var res = new Dictionary<Valves, bool>();
            string[] stringResults = new string[8];
            for (var i = 0; i < 8; i++)
            {
                var valve = (Valves)(1 << i);
                var value = (result & (1 << i)) != 0;
                res.Add(valve, value);
                stringResults[i] = $"{Enum.GetName(typeof(Valves), valve)} - {value.ToString()}";
            }
            _logger.Info(LogCategory.Service, $"Service function 'GetValvesState' called with result {string.Join(",", stringResults)}");
            return res;
        }

        public bool SetValveState(Valves valve, bool state)
        {
            var result = false;
            switch(valve)
            {
                case Valves.Vacuum:
                    result = CasySerialPortDriver.SetVacuumVentilState(state, new Progress<string>());
                    break;
                case Valves.PumpEngine:
                    result = CasySerialPortDriver.SetPumpEngineState(state, new Progress<string>());
                    break;
                case Valves.Capillary:
                    result = CasySerialPortDriver.SetCapillaryRelayVoltage(state, new Progress<string>());
                    break;
                case Valves.Meas:
                    result = CasySerialPortDriver.SetMeasValveRelayVoltage(state, new Progress<string>());
                    break;
                case Valves.Waste:
                    result = CasySerialPortDriver.SetWasteValveRelayVoltage(state, new Progress<string>());
                    break;
                case Valves.Clean:
                    result = CasySerialPortDriver.SetCleanValveRelayVoltage(state, new Progress<string>());
                    break;
                case Valves.Blow:
                    result = CasySerialPortDriver.SetBlowValveRelayVoltage(state, new Progress<string>());
                    break;
                case Valves.Suck:
                    result = CasySerialPortDriver.SetSuckValveRelayVoltage(state, new Progress<string>());
                    break;
            }
            _logger.Info(LogCategory.Service, $"Service function 'SetValvesState({Enum.GetName(typeof(Valves), valve)}, {state.ToString()}' called with result {result.ToString()}");
            return result;
        }

        public Statistic GetStatistic()
        {
            var statisticData = CasySerialPortDriver.GetStatistik(new Progress<string>());

            var checksum = SwapHelper.SwapBytes(BitConverter.ToUInt32(statisticData, statisticData.Length - 4));

            var calcCheckSum = Calculations.CalcChecksum(statisticData.SubArray(0, statisticData.Length - 4));

            if (calcCheckSum != checksum)
            {
                throw new WrongChecksumException();
            }

            _logger.Info(LogCategory.Service, $"Service function 'GetStatistic' called.");
            return ResponseDataConverter.ReadStatisticsData(statisticData);
        }

        public bool SetCapillaryVoltage(int value)
        {
            var result = CasySerialPortDriver.SetCapillaryVoltage(value, new Progress<string>());
            _logger.Info(LogCategory.Service, $"Service function 'SetCapillaryVoltage({value.ToString()})' called with result {result.ToString()}.");
            return result;
        }

        public double GetCapillaryVoltage()
        {
            var result = CasySerialPortDriver.GetCapillaryVoltage(new Progress<string>());
            _logger.Info(LogCategory.Service, $"Service function 'GetCapillaryVoltage' called with result {result.ToString()}.");
            return result;
        }

        public double GetPressure()
        {
            var result = CasySerialPortDriver.GetPressure(new Progress<string>());
            _logger.Info(LogCategory.Service, $"Service function 'GetPressure' called with result {result.ToString()}.");
            return result;
        }

        public bool ClearErrorBytes()
        {
            var result = CasySerialPortDriver.ClearErrorBytes(new Progress<string>());
            _logger.Info(LogCategory.Service, $"Service function 'ClearErrorBytes' called with result {result.ToString()}.");
            return result;
        }

        public bool ResetStatistic()
        {
            var result = CasySerialPortDriver.ResetStatistic(new Progress<string>());
            _logger.Info(LogCategory.Service, $"Service function 'ResetStatistic' called with result {result.ToString()}.");
            return result;
        }

        public bool ResetCalibration()
        {
            var result = CasySerialPortDriver.ResetCalibration(new Progress<string>());
            _logger.Info(LogCategory.Service, $"Service function 'ResetCalibration' called with result {result.ToString()}.");
            return result;
        }

        public Tuple<DateTime, uint> GetDateTime()
        {
            var result = CasySerialPortDriver.GetDateTime(new Progress<string>());
            _logger.Info(LogCategory.Service, $"Service function 'GetDateTime' called with result {result.Item1.ToString()}.");
            return result;
        }

        public bool SetDateTime(DateTime dateTime)
        {
            var response = CasySerialPortDriver.SetDateTime(dateTime, new Progress<string>());
            _logger.Info(LogCategory.Service, $"Service function 'SetDateTime' called with result {response.ToString()}.");
            return response;
        }

        public async Task<RisetimeResponse> CheckRiseTime(int capillarySize)
        {
            return await Task.Factory.StartNew(() =>
            {
                _logger.Info(LogCategory.Service, $"Performing service function 'CheckRiseTime' for capillary {capillarySize}.");

                var backgroundTemplate = _databaseStorageService.GetPredefinedTemplate("Background", capillarySize);

                _calibrationController.TransferCalibration(new Progress<string>(), backgroundTemplate, false);

                var mustStop = false;
                var isCanceled = false;
                var showProgressWrapper = new ShowProgressDialogWrapper
                {
                    Title = "ProgressBox_CheckRisetime_Title",
                    Message = "ProgressBox_CheckRisetime_Message",
                    IsCancelButtonAvailable = true,
                    CancelAction = (isCancel) =>
                    {
                        _logger.Info(LogCategory.Service, $"Service function 'CheckRiseTime' canceled.");
                        if (mustStop)
                        {
                            CasySerialPortDriver.Stop(new Progress<string>());
                        }
                        isCanceled = true;
                    },
                    IsFinished = false
                };
                _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);

                mustStop = true;
                var response = CasySerialPortDriver.CheckRisetime(new Progress<string>());
                mustStop = false;
                var errorResult = _errorController.ParseError(response);

                RisetimeResponse result = null;

                if (CheckErrorResult(errorResult))
                {
                    var pieces = response.Split(',').Skip(3).ToArray();

                    result = new RisetimeResponse
                    {
                        MaxTimeGreen = int.Parse(pieces[0], System.Globalization.NumberStyles.HexNumber),
                        MinTimeGreen = int.Parse(pieces[1], System.Globalization.NumberStyles.HexNumber),
                        AverageTimeGreen = int.Parse(pieces[2], System.Globalization.NumberStyles.HexNumber),
                        MaxTime200 = int.Parse(pieces[3], System.Globalization.NumberStyles.HexNumber),
                        MinTime200 = int.Parse(pieces[4], System.Globalization.NumberStyles.HexNumber),
                        AverageTime200 = int.Parse(pieces[5], System.Globalization.NumberStyles.HexNumber),
                        MaxTime400 = int.Parse(pieces[6], System.Globalization.NumberStyles.HexNumber),
                        MinTime400 = int.Parse(pieces[7], System.Globalization.NumberStyles.HexNumber),
                        AverageTime400 = int.Parse(pieces[8], System.Globalization.NumberStyles.HexNumber),
                        Cycles = 10
                    };
                }

                showProgressWrapper.IsFinished = true;
                _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);

                if(!isCanceled)
                {
                    _logger.Info(LogCategory.Service, $"Service function 'CheckRiseTime' completed successfully.");
                }
                return result;
            });
        }

        public async Task<TightnessResponse> CheckTightness()
        {
            return await Task.Factory.StartNew(() =>
            {
                _logger.Info(LogCategory.Service, $"Performing service function 'CheckTightness'.");

                var mustStop = false;

                var isCanceled = false;
                var showProgressWrapper = new ShowProgressDialogWrapper
                {
                    Title = "ProgressBox_CheckTightness_Title",
                    Message = "ProgressBox_CheckTightness_Message",
                    IsCancelButtonAvailable = true,
                    CancelAction = (isCancel) =>
                    {
                        _logger.Info(LogCategory.Service, $"Service function 'CheckTightness' canceled.");
                        if (mustStop)
                        {
                            CasySerialPortDriver.Stop(new Progress<string>());
                        }
                        isCanceled = true;
                    },
                    IsFinished = false
                };
                _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);

                mustStop = true;
                var response = CasySerialPortDriver.CheckTightness(new Progress<string>());
                mustStop = false;

                var errorResult = _errorController.ParseError(response);

                var hasError = CheckErrorResult(errorResult);

                var pieces = response.Split(',').Skip(3).ToArray();

                var result = new TightnessResponse
                {
                    MaxPressureBegin = Convert.ToInt32(pieces[0], 16),
                    MaxPressureEnd = Convert.ToInt32(pieces[1], 16),
                    MaxPressureDifference = Convert.ToInt32(pieces[2], 16),
                    MinPressureBegin = Convert.ToInt32(pieces[3], 16),
                    MinPressureEnd = Convert.ToInt32(pieces[4], 16),
                    MinPressureDifference = Convert.ToInt32(pieces[5], 16),
                    FillTime400 = Convert.ToInt32(pieces[6], 16),
                    BubbleTime = Convert.ToInt32(pieces[7], 16)
                };

                if(!hasError)
                {
                    result.ErrorResult = errorResult;
                }

                
                showProgressWrapper.IsFinished = true;
                _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);

                if (!isCanceled)
                {
                    _logger.Info(LogCategory.Service, $"Service function 'CheckTightness' completed successfully.");
                }

                return result;
            });
        }

        private bool CheckErrorResult(ErrorResult errorResult)
        {
            IEnumerable<ErrorDetails> softErrors = errorResult.SoftErrorDetails;
            switch (errorResult.ErrorResultType)
            {
                case ErrorResultType.SingleError:
                    if (!softErrors.Any())
                    {
                        // There is no soft error with special handling. Return the error result
                        _eventAggregatorProvider.Instance.GetEvent<ErrorResultEvent>().Publish(errorResult);
                        return false;
                    }
                    break;
                case ErrorResultType.MutipleError:
                    if (errorResult.FatalErrorDetails.Count > 0)
                    {
                        _eventAggregatorProvider.Instance.GetEvent<ErrorResultEvent>().Publish(errorResult);
                        return false;
                    }
                    break;
            }
            return true;
        }

        public void StartMeasureBackgroundWizard()
        {
            lock (_lock)
            {
                Task.Factory.StartNew(async () =>
                {
                    var success = await _measureResultManager.SaveChangedMeasureResults();

                    if (success != ButtonResult.Cancel)
                    {
                        var measureCount = 1;

                        var awaiter = new ManualResetEvent(false);

                        var viewModelExport = _compositionFactory.GetExport<IWizardContainerViewModel>();
                        var viewModel = viewModelExport.Value;

                        var initialWizardStepViewModel = new StandardWizardStepViewModel
                        {
                            PrimaryText =
                                _localizationService.GetLocalizedString("BackgroundWizzard_Initial_PrimaryText"),
                            SecondaryHeader =
                                _localizationService.GetLocalizedString("BackgroundWizzard_Initial_SecondaryHeader"),
                            SecondaryText =
                                _localizationService.GetLocalizedString("BackgroundWizzard_Initial_SecondaryText")
                        };

                        viewModel.AddWizardStepViewModel(initialWizardStepViewModel);

                        var resultStepViewModel = new BackgroundResultWizardStepViewModel
                        {
                            Text = _localizationService.GetLocalizedString("BackgroundWizzard_Result_Text"),
                            NextButtonText =
                                _localizationService.GetLocalizedString("BackgroundWizzard_Result_Button_Repeat"),
                            CancelButtonText =
                                _localizationService.GetLocalizedString("BackgroundWizzard_Result_Button_Accept"),
                            NextButtonPressedAction = () =>
                            {
                                measureCount = 3;
                                viewModel.GotoStep(2);
                            }
                        };

                        resultStepViewModel.PrintButtonPressedAction = async () =>
                        {
                            var renderer = new PdfDocumentRenderer(false);

                            var measureResult = resultStepViewModel.MeasureResult;
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
                            }

                            string documentLogoName = "OLS_Logo.png";
                            if (!_databaseStorageService.GetSettings().TryGetValue("DocumentLogoName", out var documentLogoSetting))
                            {
                                _databaseStorageService.SaveSetting("DocumentLogoName", "OLS_Logo.png");
                            }
                            else
                            {
                                documentLogoName = documentLogoSetting.Value;
                            }
                            var measureResultDocument = new SingleMeasureResultDocument(_localizationService, _authenticationService, _documentSettingsManager, _environmentService);
                            renderer.Document = measureResultDocument.CreateDocument(measureResult, null, false, false);

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
                        };

                        _lastUsedCapillary = _measureController.LastSelectedCapillary.ToString();

                        var selectCapillaryStepViewModel =
                            new SelectCapillaryWizardStepViewModel(
                                _calibrationController.KnownCappillarySizes.ToArray(),
                                _measureController.LastSelectedCapillary)
                            {
                                Text =
                                    _localizationService.GetLocalizedString("BackgroundWizzard_SelectCapillary_Text"),
                                SelectedCapillarySize = _measureController.LastSelectedCapillary == 0
                                    ? null
                                    : _lastUsedCapillary
                            };
                        viewModel.AddWizardStepViewModel(selectCapillaryStepViewModel);

                        var wizardStep1ViewModel = new StandardWizardStepViewModel
                        {
                            PrimaryHeader =
                                _localizationService.GetLocalizedString("BackgroundWizzard_Step1_PrimaryHeader"),
                            PrimaryText =
                                _localizationService.GetLocalizedString("BackgroundWizzard_Step1_PrimaryText"),
                            SecondaryHeader =
                                _localizationService.GetLocalizedString("BackgroundWizzard_Step1_SecondaryHeader"),
                            SecondaryText =
                                _localizationService.GetLocalizedString("BackgroundWizzard_Step1_SecondaryText"),
                            NextButtonPressedAction = () =>
                            {
                                _lastUsedCapillary = selectCapillaryStepViewModel.SelectedCapillarySize;

                                if (!string.IsNullOrEmpty(_lastUsedCapillary) && _lastUsedCapillary != _measureController.LastSelectedCapillary.ToString())
                                {
                                    _measureController.LastSelectedCapillary = int.Parse(_lastUsedCapillary);
                                }

                                var capillarySize = int.Parse(selectCapillaryStepViewModel.SelectedCapillarySize);
                                var measureBackgroundTask = Task.Run(async () => await MeasureBackground(capillarySize, true, measureCount));
                                var (item1, item2) = measureBackgroundTask.Result;

                                if (item2.HasCanceled)
                                {
                                    return false;
                                }

                                if (item2.FatalErrorDetails.Count > 0 ||
                                    item2.SoftErrorDetails.Count > 0)
                                {
                                    _eventAggregatorProvider.Instance.GetEvent<ErrorResultEvent>()
                                        .Publish(item2);
                                }

                                if (item2.FatalErrorDetails.Count > 0 ||
                                    item2.SoftErrorDetails.Count > 0)
                                {
                                    resultStepViewModel.TotalCountsState = "Red";
                                    resultStepViewModel.Text =
                                        _localizationService.GetLocalizedString(
                                            "BackgroundWizzard_Result_Text_Error");
                                }

                                var countsPerMlItem = item1?.MeasureResultItemsContainers[MeasureResultItemTypes.CountsPerMl]?.MeasureResultItem;
                                if (countsPerMlItem == null) return true;

                                resultStepViewModel.TotalCounts =
                                    countsPerMlItem.ResultItemValue.ToString("0.00E+00");

                                switch (capillarySize)
                                {
                                    case 150:
                                        resultStepViewModel.TotalCountsState =
                                            countsPerMlItem.ResultItemValue > 200
                                                ? "Red"
                                                : (countsPerMlItem.ResultItemValue > 100 ? "Yellow" : "Green");
                                        break;
                                    case 60:
                                        resultStepViewModel.TotalCountsState =
                                            countsPerMlItem.ResultItemValue > 400
                                                ? "Red"
                                                : (countsPerMlItem.ResultItemValue > 200 ? "Yellow" : "Green");
                                        break;
                                    case 45:
                                        resultStepViewModel.TotalCountsState =
                                            countsPerMlItem.ResultItemValue > 6000
                                                ? "Red"
                                                : (countsPerMlItem.ResultItemValue > 3000 ? "Yellow" : "Green");
                                        break;
                                }

                                var chartDataSet =
                                    Task.Run(async () => await _measureResultDataCalculationService.SumMeasureResultDataAsync(item1));
                                resultStepViewModel.MeasureResult = item1;
                                resultStepViewModel.SetChartData(chartDataSet.Result,
                                    item1.MeasureSetup.SmoothedDiameters);

                                return true;
                            }
                        };

                        viewModel.AddWizardStepViewModel(wizardStep1ViewModel);

                        viewModel.AddWizardStepViewModel(resultStepViewModel);

                        viewModel.WizardTitle = "BackgroundWizard_Title";

                        var titleBinding = new Binding("Title") { Source = viewModel };

                        var wrapper = new ShowCustomDialogWrapper()
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
                        _compositionFactory.ReleaseExport(viewModelExport);
                        _authenticationService.EnableAutoLogOff();
                    }
                });
            }
        }

        public void OnImportsSatisfied()
        {
            _authenticationService.UserLoggedIn += (s, e) => OnUserLoggedIn();
        }

        private void OnUserLoggedIn()
        {
            if(_authenticationService.LoggedInUser != null && CasySerialPortDriver != null)
            {
                var showStartUp = "true";
                if (_databaseStorageService.GetSettings().TryGetValue($"ShowStartUp-{_authenticationService.LoggedInUser.Name}", out var showStartUpSetting))
                {
                    showStartUp = showStartUpSetting.Value;
                }
                if (showStartUp == "true")
                {
                    DateTime lastShowStartUp = DateTime.MinValue;
                    if (_databaseStorageService.GetSettings().TryGetValue($"LastStartUpWizard", out var lastShowStartUpSetting))
                    {
                        DateTime.TryParse(lastShowStartUpSetting.Value, out lastShowStartUp);
                    }

                    if (lastShowStartUp.Date < DateTime.Now.Date)
                    {
                        _databaseStorageService.SaveSetting("LastStartUpWizard", DateTime.Now.ToString());
                        StartStartupWizard();
                    }
                }
            }
            RegisterJobs();
        }

        private void RegisterJobs()
        {
            if (_monitoringService == null) return;
            if (CasySerialPortDriver != null)
            {
                var settings = _databaseStorageService.GetSettings();
                var weeklyCleanIsMandatory = !settings.TryGetValue("WeeklyCleanMandatory", out var weeklyCleanMandatorySetting) ? false : weeklyCleanMandatorySetting.Value == "true";
                
                _monitoringService.RegisterMonitoringJob(new MonitoringJob
                {
                    Name = Enum.GetName(typeof(MonitoringTypes), MonitoringTypes.WeeklyCleanNotification),
                    IntervalInSeconds = (int) TimeSpan.FromHours(1).TotalSeconds,
                    JobFunction = parameter =>
                    {
                        if(parameter == null)
                        {
                            return DateTime.UtcNow.Ticks.ToString();
                        }

                        if (_authenticationService.LoggedInUser == null)
                        {
                            return null;
                        }

                        var dateTimeToCheck = new DateTime(long.Parse(parameter));
                        var dateTimeNow = DateTime.UtcNow;
                        var difference = dateTimeNow - dateTimeToCheck;

                        if (!(difference.TotalHours >= (7d * 24d))) return null;

                        if (_notificationService.Notifications.Any(n => n.NotificationType == NotificationType.WeeklyCleanMandetory)) return null;
                        
                        var notification = _notificationService.CreateNotification(NotificationType.WeeklyCleanMandetory);
                        notification.Title = weeklyCleanIsMandatory ? "Notification_WeeklyCleanMandatory_Title" : "Notification_WeeklyClean_Title";
                        notification.Message = weeklyCleanIsMandatory ? "Notification_WeeklyCleanMandatory_Message" : "Notification_WeeklyClean_Message";
                        notification.ActionDescription = "Notification_WeeklyClean_Action";
                        notification.Action = () =>
                        {
                            _eventAggregatorProvider.Instance.GetEvent<ShowServiceEvent>().Publish();
                        };
                        _notificationService.AddNotification(notification);
                        IsWeeklyCleanMandatory = true;
                        RaiseWeeklyCleanMandatoryChanged();

                        return null;
                    }
                });

                if (weeklyCleanIsMandatory)
                {
                    double duration = !settings.TryGetValue("WeeklyCleanNotificationDuration", out var weeklyCleanNotificationDurationSetting) ? 24d : double.Parse(weeklyCleanNotificationDurationSetting.Value);

                    _monitoringService.RegisterMonitoringJob(new MonitoringJob
                    {
                        Name = Enum.GetName(typeof(MonitoringTypes), MonitoringTypes.WeeklyCleanAnnouncementNotification),
                        IntervalInSeconds = (int)TimeSpan.FromHours(1).TotalSeconds,
                        JobFunction = parameter =>
                        {
                            if (parameter == null)
                            {
                                return DateTime.UtcNow.Ticks.ToString();
                            }

                            if (_authenticationService.LoggedInUser == null)
                            {
                                return null;
                            }

                            var dateTimeToCheck = new DateTime(long.Parse(parameter));
                            var dateTimeNow = DateTime.UtcNow;
                            var difference = dateTimeNow - dateTimeToCheck;

                            if (!(difference.TotalHours >= ((7d * 24d) - duration))) return null;

                            if (_notificationService.Notifications.Any(n => n.NotificationType == NotificationType.WeeklyCleanAnnouncementNotification)) return null;

                            var notification = _notificationService.CreateNotification(NotificationType.WeeklyCleanAnnouncementNotification);
                            notification.Title = "Notification_WeeklyCleanAnnouncement_Title";
                            notification.Message = string.Format(_localizationService.GetLocalizedString("Notification_WeeklyCleanAnnouncement_Message"), duration == 12d ? "12" : "24");
                            notification.ActionDescription = "Notification_WeeklyClean_Action";
                            notification.Action = () =>
                            {
                                _eventAggregatorProvider.Instance.GetEvent<ShowServiceEvent>().Publish();
                            };
                            _notificationService.AddNotification(notification);

                            return null;
                        }
                    });
                }
            }
        }

        public void StartStartupWizard()
        {
            lock (_lock)
            {
                Task.Factory.StartNew(async () =>
                {
                    var success = await _measureResultManager.SaveChangedMeasureResults();

                    if (success != ButtonResult.Cancel)
                    {
                        //var measureCount = 1;

                        var awaiter = new ManualResetEvent(false);

                        var viewModelExport = _compositionFactory.GetExport<IWizardContainerViewModel>();
                        var viewModel = viewModelExport.Value;

                        var initialWizardStepViewModel = new StandardWizardStepViewModel
                        {
                            PrimaryText =
                                _localizationService.GetLocalizedString("StartUpWizzard_Initial_PrimaryText"),
                            SecondaryHeader =
                                _localizationService.GetLocalizedString("StartUpWizzard_Initial_SecondaryHeader"),
                            SecondaryText =
                                _localizationService.GetLocalizedString("StartUpWizzard_Initial_SecondaryText"),
                            NextButtonPressedAction = () =>
                            {
                                var result = _measureController.Clean(3);
                                return !result.HasCanceled;
                            },
                            IsCancelButtonVisible = true,
                            IsDoNotShowAgainVisible = true
                        };

                        viewModel.AddWizardStepViewModel(initialWizardStepViewModel);

                        _lastUsedCapillary = _measureController.LastSelectedCapillary.ToString();

                        var selectCapillaryStepViewModel =
                            new SelectCapillaryWizardStepViewModel(
                                _calibrationController.KnownCappillarySizes.ToArray(),
                                _measureController.LastSelectedCapillary)
                            {
                                Text =
                                    _localizationService.GetLocalizedString("StartUpWizzard_SelectCapillary_Text"),
                                SelectedCapillarySize = _measureController.LastSelectedCapillary == 0
                                    ? null
                                    : _lastUsedCapillary
                            };
                        viewModel.AddWizardStepViewModel(selectCapillaryStepViewModel);

                        var resultStepViewModel = new BackgroundResultWizardStepViewModel
                        {
                            Text = _localizationService.GetLocalizedString("StartUpWizzard_Result_Text"),
                            NextButtonText =
                                _localizationService.GetLocalizedString("StartUpWizzard_Result_Button_Repeat"),
                            CancelButtonText =
                                _localizationService.GetLocalizedString("StartUpWizzard_Result_Button_Accept"),
                            NextButtonPressedAction = () =>
                            {
                                viewModel.GotoStep(0);
                            }
                        };
                        resultStepViewModel.PrintButtonPressedAction = async () =>
                        {
                            var renderer = new PdfDocumentRenderer(false);

                            var measureResult = resultStepViewModel.MeasureResult;
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
                            }

                            string documentLogoName = "OLS_Logo.png";
                            if (!_databaseStorageService.GetSettings().TryGetValue("DocumentLogoName", out var documentLogoSetting))
                            {
                                _databaseStorageService.SaveSetting("DocumentLogoName", "OLS_Logo.png");
                            }
                            else
                            {
                                documentLogoName = documentLogoSetting.Value;
                            }
                            var measureResultDocument = new SingleMeasureResultDocument(_localizationService, _authenticationService, _documentSettingsManager, _environmentService);
                            renderer.Document = measureResultDocument.CreateDocument(measureResult, null, false, false);

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
                        };

                        var wizardStep2ViewModel = new StandardWizardStepViewModel
                        {
                            PrimaryHeader = _localizationService.GetLocalizedString("StartUpWizzard_Step2_PrimaryHeader"),
                            PrimaryText = _localizationService.GetLocalizedString("StartUpWizzard_Step2_PrimaryText"),
                            SecondaryHeader = _localizationService.GetLocalizedString("StartUpWizzard_Step2_SecondaryHeader"),
                            SecondaryText = _localizationService.GetLocalizedString("StartUpWizzard_Step2_SecondaryText"),
                            NextButtonPressedAction = () =>
                            {
                                _lastUsedCapillary = selectCapillaryStepViewModel.SelectedCapillarySize;

                                if (!string.IsNullOrEmpty(_lastUsedCapillary) && _lastUsedCapillary != _measureController.LastSelectedCapillary.ToString())
                                {
                                    _measureController.LastSelectedCapillary = int.Parse(_lastUsedCapillary);
                                }

                                var capillarySize = int.Parse(selectCapillaryStepViewModel.SelectedCapillarySize);
                                var measureBackgroundTask = Task.Run(async () => await MeasureBackground(capillarySize, true, 1));
                                var (item1, item2) = measureBackgroundTask.Result;

                                if (item2.HasCanceled)
                                {
                                    return false;
                                }

                                if (item2.FatalErrorDetails.Count > 0 ||
                                    item2.SoftErrorDetails.Count > 0)
                                {
                                    _eventAggregatorProvider.Instance.GetEvent<ErrorResultEvent>()
                                        .Publish(item2);
                                }

                                if (item2.FatalErrorDetails.Count > 0 ||
                                    item2.SoftErrorDetails.Count > 0)
                                {
                                    resultStepViewModel.TotalCountsState = "Red";
                                    resultStepViewModel.Text =
                                        _localizationService.GetLocalizedString(
                                            "BackgroundWizzard_Result_Text_Error");
                                }

                                var countsPerMlItem = item1?.MeasureResultItemsContainers[MeasureResultItemTypes.CountsPerMl]?.MeasureResultItem;
                                if (countsPerMlItem == null) return true;

                                resultStepViewModel.TotalCounts =
                                    countsPerMlItem.ResultItemValue.ToString("0.00E+00");

                                switch (capillarySize)
                                {
                                    case 150:
                                        resultStepViewModel.TotalCountsState =
                                            countsPerMlItem.ResultItemValue > 200
                                                ? "Red"
                                                : (countsPerMlItem.ResultItemValue > 100 ? "Yellow" : "Green");
                                        break;
                                    case 60:
                                        resultStepViewModel.TotalCountsState =
                                            countsPerMlItem.ResultItemValue > 400
                                                ? "Red"
                                                : (countsPerMlItem.ResultItemValue > 200 ? "Yellow" : "Green");
                                        break;
                                    case 45:
                                        resultStepViewModel.TotalCountsState =
                                            countsPerMlItem.ResultItemValue > 6000
                                                ? "Red"
                                                : (countsPerMlItem.ResultItemValue > 3000 ? "Yellow" : "Green");
                                        break;
                                }

                                var chartDataSet =
                                    Task.Run(async () => await _measureResultDataCalculationService.SumMeasureResultDataAsync(item1));
                                resultStepViewModel.MeasureResult = item1;
                                resultStepViewModel.SetChartData(chartDataSet.Result,
                                    item1.MeasureSetup.SmoothedDiameters);

                                return true;
                            }
                        };

                        //1
                        viewModel.AddWizardStepViewModel(wizardStep2ViewModel);

                        viewModel.AddWizardStepViewModel(resultStepViewModel);

                        viewModel.WizardTitle = "StartUpWizard_Title";

                        var titleBinding = new Binding("Title") { Source = viewModel };

                        var wrapper = new ShowCustomDialogWrapper()
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
                            if(initialWizardStepViewModel.DoNotShowAgain)
                            {
                                _databaseStorageService.SaveSetting($"ShowStartUp-{_authenticationService.LoggedInUser.Name}", "false");
                            }
                        }
                        _compositionFactory.ReleaseExport(viewModelExport);
                        _authenticationService.EnableAutoLogOff();
                    }
                });
            }
        }

        public bool StartShutdownWizard()
        {
            ButtonResult success = ButtonResult.Accept;
            var awaiter = new ManualResetEvent(false);

            var viewModelExport = _compositionFactory.GetExport<IWizardContainerViewModel>();
            var viewModel = viewModelExport.Value;

            var initialWizardStepViewModel = new StandardWizardStepViewModel
            {
                PrimaryText =
                    _localizationService.GetLocalizedString("ShutDownWizzard_Initial_PrimaryText"),
                SecondaryHeader =
                    _localizationService.GetLocalizedString("ShutDownWizzard_Initial_SecondaryHeader"),
                SecondaryText =
                    _localizationService.GetLocalizedString("ShutDownWizzard_Initial_SecondaryText"),
                NextButtonPressedAction = () =>
                {
                    var result = _measureController.Clean(3);
                    return !result.HasCanceled;
                },
                IsCancelButtonVisible = true,
                IsDoNotShowAgainVisible = true
            };

            viewModel.AddWizardStepViewModel(initialWizardStepViewModel);

            _lastUsedCapillary = _measureController.LastSelectedCapillary.ToString();

            var selectCapillaryStepViewModel =
                new SelectCapillaryWizardStepViewModel(
                    _calibrationController.KnownCappillarySizes.ToArray(),
                    _measureController.LastSelectedCapillary)
                {
                    Text =
                        _localizationService.GetLocalizedString("ShutDownWizzard_SelectCapillary_Text"),
                    SelectedCapillarySize = _measureController.LastSelectedCapillary == 0
                        ? null
                        : _lastUsedCapillary
                };
            viewModel.AddWizardStepViewModel(selectCapillaryStepViewModel);

            var resultStepViewModel = new BackgroundResultWizardStepViewModel
            {
                Text = _localizationService.GetLocalizedString("ShutDownWizzard_Result_Text"),
                NextButtonText =
                    _localizationService.GetLocalizedString("ShutDownWizzard_Result_Button_Repeat"),
                CancelButtonText =
                    _localizationService.GetLocalizedString("ShutDownWizzard_Result_Button_Shutdown"),
                NextButtonPressedAction = () =>
                {
                    viewModel.GotoStep(0);
                },
                CancelButtonPressedAction = () =>
                {
                    success = ButtonResult.Accept;
                }
            };

            var wizardStep2ViewModel = new StandardWizardStepViewModel
            {
                PrimaryHeader = _localizationService.GetLocalizedString("ShutDownWizzard_Step2_PrimaryHeader"),
                PrimaryText = _localizationService.GetLocalizedString("ShutDownWizzard_Step2_PrimaryText"),
                SecondaryHeader = _localizationService.GetLocalizedString("ShutDownWizzard_Step2_SecondaryHeader"),
                SecondaryText = _localizationService.GetLocalizedString("ShutDownWizzard_Step2_SecondaryText"),
                NextButtonPressedAction = () =>
                {
                    _lastUsedCapillary = selectCapillaryStepViewModel.SelectedCapillarySize;

                    if (!string.IsNullOrEmpty(_lastUsedCapillary) && _lastUsedCapillary != _measureController.LastSelectedCapillary.ToString())
                    {
                        _measureController.LastSelectedCapillary = int.Parse(_lastUsedCapillary);
                    }

                    var capillarySize = int.Parse(selectCapillaryStepViewModel.SelectedCapillarySize);
                    var measureBackgroundTask = Task.Run(async () => await MeasureBackground(capillarySize, true, 1));
                    var (item1, item2) = measureBackgroundTask.Result;

                    if (item2.HasCanceled)
                    {
                        return false;
                    }

                    if (item2.FatalErrorDetails.Count > 0 ||
                        item2.SoftErrorDetails.Count > 0)
                    {
                        _eventAggregatorProvider.Instance.GetEvent<ErrorResultEvent>()
                            .Publish(item2);
                    }

                    if (item2.FatalErrorDetails.Count > 0 ||
                        item2.SoftErrorDetails.Count > 0)
                    {
                        resultStepViewModel.TotalCountsState = "Red";
                        resultStepViewModel.Text =
                            _localizationService.GetLocalizedString(
                                "BackgroundWizzard_Result_Text_Error");
                    }

                    var countsPerMlItem = item1?.MeasureResultItemsContainers[MeasureResultItemTypes.CountsPerMl]?.MeasureResultItem;
                    if (countsPerMlItem == null) return true;

                    resultStepViewModel.TotalCounts =
                        countsPerMlItem.ResultItemValue.ToString("0.00E+00");

                    switch (capillarySize)
                    {
                        case 150:
                            resultStepViewModel.TotalCountsState =
                                countsPerMlItem.ResultItemValue > 200
                                    ? "Red"
                                    : (countsPerMlItem.ResultItemValue > 100 ? "Yellow" : "Green");
                            break;
                        case 60:
                            resultStepViewModel.TotalCountsState =
                                countsPerMlItem.ResultItemValue > 400
                                    ? "Red"
                                    : (countsPerMlItem.ResultItemValue > 200 ? "Yellow" : "Green");
                            break;
                        case 45:
                            resultStepViewModel.TotalCountsState =
                                countsPerMlItem.ResultItemValue > 6000
                                    ? "Red"
                                    : (countsPerMlItem.ResultItemValue > 3000 ? "Yellow" : "Green");
                            break;
                    }

                    var chartDataSet =
                        Task.Run(async () => await _measureResultDataCalculationService.SumMeasureResultDataAsync(item1));
                    resultStepViewModel.MeasureResult = item1;
                    resultStepViewModel.SetChartData(chartDataSet.Result,
                        item1.MeasureSetup.SmoothedDiameters);

                    return true;
                }
            };

            //1
            viewModel.AddWizardStepViewModel(wizardStep2ViewModel);

            viewModel.AddWizardStepViewModel(resultStepViewModel);

            viewModel.WizardTitle = "ShutDownWizard_Title";

            var titleBinding = new Binding("Title") { Source = viewModel };

            var wrapper = new ShowCustomDialogWrapper()
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
                if (initialWizardStepViewModel.DoNotShowAgain)
                {
                    _databaseStorageService.SaveSetting($"ShowShutDown-{_authenticationService.LoggedInUser.Name}", "false");
                }
            }
            _compositionFactory.ReleaseExport(viewModelExport);
            _authenticationService.EnableAutoLogOff();

            return success == ButtonResult.Accept;
        }

        public bool IsWeeklyCleanMandatory
        {
            get
            {
                var settings = _databaseStorageService.GetSettings();
                var weeklyCleanIsMandatory = !settings.TryGetValue("WeeklyCleanMandatory", out var weeklyCleanMandatorySetting) ? false : weeklyCleanMandatorySetting.Value == "true";
                return weeklyCleanIsMandatory && _isWeeklyCleanMandatory;
            }
            set
            {
                _isWeeklyCleanMandatory = value;
                RaiseWeeklyCleanMandatoryChanged();
            }
        }

        public event EventHandler WeeklyCleanMandatoryChangedEvent;

        private void RaiseWeeklyCleanMandatoryChanged()
        {
            if (WeeklyCleanMandatoryChangedEvent != null)
            {
                foreach (EventHandler receiver in WeeklyCleanMandatoryChangedEvent.GetInvocationList())
                {
                    receiver.BeginInvoke(this, EventArgs.Empty, null, null);
                }

            }
        }
    }
}
