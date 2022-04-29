using OLS.Casy.Base;
using OLS.Casy.Com.Api;
using OLS.Casy.Controller.Api;
using OLS.Casy.Core;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Config.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Core.Logging.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Models;
using OLS.Casy.Models.Enums;
using OLS.Casy.Monitoring.Api;
using OLS.Casy.RemoteIPS.Api;
using OLS.Casy.Ui.Core.Api;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OLS.Casy.Controller.Measure
{
    /// <summary>
    /// Implementation of <see cref="IMeasureController"/> communicating with the casy device via serial port
    /// </summary>
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IMeasureController))]
    [Export(typeof(IService))]
    public class MeasureController : BaseCasyController, IMeasureController, IPartImportsSatisfiedNotification
    {
        private readonly IDatabaseStorageService _databaseStorageService;
        private readonly ICalibrationController _calibrationController;
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly IErrorContoller _errorController;
        private readonly ICasyController _casyController;
        private readonly ILocalizationService _localizationService;
        private readonly IMeasureResultStorageService _measureResultStorageService;
        private readonly IMeasureResultDataCalculationService _measureResultDataCalculationService;
        private readonly IMeasureResultManager _measureResultManager;
        private readonly IMeasureCounter _measureCounter;
        private readonly ILogger _logger;
        private readonly IEnvironmentService _environmentService;
        private readonly IRemoteIpsService _remoteIpsService;
        private readonly IMonitoringService _monitoringService;

        private MeasureSetup _selectedTemplate;

        /// <summary>
        /// MEF importing constructor
        /// </summary>
        /// <param name="logger">Implementation of <see cref="ILogger"/></param>
        /// <param name="configService">Implementation of <see cref="IConfigService"/></param>
        /// <param name="databaseStorageService">Implementation of <see cref="IDatabaseStorageService"/></param>
        /// <param name="calibrationController">Implementation of <see cref="ICalibrationController"/></param>
        /// <param name="casySerialPortDriver">Implementation of <see cref="ICasySerialPortDriver"/></param>
        /// <param name="eventAggregatorProvider">Implementation of <see cref="IEventAggregatorProvider"/> </param>
        /// <param name="errorController">Implementation of <see cref="IErrorContoller"/></param>
        /// <param name="measureCounter"></param>
        [ImportingConstructor]
        public MeasureController(IConfigService configService,
            IDatabaseStorageService databaseStorageService,
            [Import(AllowDefault = true)] ICalibrationController calibrationController,
            [Import(AllowDefault = true)] ICasySerialPortDriver casySerialPortDriver,
            IEventAggregatorProvider eventAggregatorProvider,
            [Import(AllowDefault = true)] IErrorContoller errorController,
            ICasyController casyController,
            ILocalizationService localizationService,
            IMeasureResultManager measureResultManager,
            IMeasureResultStorageService measureResultStorageService,
            IMeasureResultDataCalculationService measureResultDataCalculationService,
            IEnvironmentService environmentService,
            ILogger logger,
            [Import(AllowDefault = true)] IMeasureCounter measureCounter,
            [Import(AllowDefault = true)] IRemoteIpsService remoteIpsService,
            IMonitoringService monitoringService)
            : base(configService, casySerialPortDriver)
        {
            _databaseStorageService = databaseStorageService;
            _calibrationController = calibrationController;
            _eventAggregatorProvider = eventAggregatorProvider;
            _errorController = errorController;
            _measureResultManager = measureResultManager;
            _casyController = casyController;
            _localizationService = localizationService;
            _measureResultStorageService = measureResultStorageService;
            _measureResultDataCalculationService = measureResultDataCalculationService;
            _measureCounter = measureCounter;
            _logger = logger;
            _environmentService = environmentService;
            _remoteIpsService = remoteIpsService;
            _monitoringService = monitoringService;
        }

        /// <summary>
        /// Event is raised when the currently active <see cref="MeasureSetup"/> has been changed 
        /// </summary>
        public event EventHandler<SelectedTemplateChangedEventArgs> SelectedTemplateChangedEvent;

        /// <summary>
        /// Returns the currently active <see cref="MeasureSetup"/>
        /// </summary>
        public MeasureSetup SelectedTemplate
        {
            get => _selectedTemplate;
            set
            {
                _selectedTemplate = value;

                if (_selectedTemplate != null)
                {
                    LastSelectedCapillary = _selectedTemplate.CapillarySize;
                }

                if (SelectedTemplateChangedEvent == null) return;
                foreach (var @delegate in SelectedTemplateChangedEvent.GetInvocationList())
                {
                    var receiver = (EventHandler<SelectedTemplateChangedEventArgs>) @delegate;
                    receiver.BeginInvoke(this, new SelectedTemplateChangedEventArgs(_selectedTemplate), null, null);
                }
            }
        }

        public string ConnectedSerialPort => CasySerialPortDriver == null ? string.Empty : CasySerialPortDriver.ConnectedSerialPort;

        public int LastColorIndex { get; set; }

        public int LastSelectedCapillary { get; set; }

        /// <summary>
        /// Starts async a measurement
        /// </summary>
        /// <returns><see cref="ErrorResult"/> of the measurement operation</returns>
        public async Task<MeasureResult> Measure(MeasureResult measureResult, ShowProgressDialogWrapper showProgressWrapper = null, string workbook = null, int? customRepeats = null)
        {
            while (true)
            {
                var isCancelRequested = false;
                var mustStop = false;

                if (SelectedTemplate == null) return null;

                if (SelectedTemplate.IsManual)
                {
                    _logger.Info(LogCategory.Measurement,
                        $"Individual measuring procedure has been started. Capillary: {SelectedTemplate.CapillarySize}; Range: {SelectedTemplate.ToDiameter}; Cycles : {SelectedTemplate.Repeats}.");
                }
                else
                {
                    _logger.Info(LogCategory.Measurement,
                        $"Measuring procedure has been started with template '{SelectedTemplate.Name}.");
                }

                if (showProgressWrapper == null)
                {
                    showProgressWrapper = new ShowProgressDialogWrapper
                    {
                        Title = "ProgressBox_Measure_Title",
                        Message = "ProgressBox_Measure_Message",
                        IsCancelButtonAvailable = true,
                        CancelAction = isCancel =>
                        {
                            isCancelRequested = true;
                            if (!mustStop) return;

                            CasySerialPortDriver.Stop(new Progress<string>());
                            _logger.Info(LogCategory.Measurement, "Measuring procedure has been canceled.");
                        }
                    };

                    var measureProgress = _localizationService.GetLocalizedString("ProgressBox_Measure_Message_Calibration");

                    showProgressWrapper.MessageParameter = new[] {measureProgress};
                    showProgressWrapper.IsFinished = false;

                    _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);
                }

                mustStop = true;
                var volumeCorrectionFactor = _calibrationController.GetVolumeCorrection(SelectedTemplate);
                mustStop = false;
                SelectedTemplate.VolumeCorrectionFactor = volumeCorrectionFactor;
                measureResult.MeasureSetup.VolumeCorrectionFactor = volumeCorrectionFactor;
                measureResult.OriginalMeasureSetup.VolumeCorrectionFactor = volumeCorrectionFactor;

                var progress = new Progress<string>();
                var errorResult = Calibrate(progress, SelectedTemplate, false);

                if (errorResult.ErrorResultType == ErrorResultType.NoError && !isCancelRequested)
                {
                    if (string.IsNullOrEmpty(measureResult.SerialNumber))
                    {
                        measureResult.SerialNumber = _casyController.GetSerialNumber();
                    }

                    var lastWeeklyClean = (string) _monitoringService.GetMonitoringValue(Enum.GetName(typeof(MonitoringTypes), MonitoringTypes.WeeklyCleanNotification));
                    measureResult.LastWeeklyClean = new DateTime(long.Parse(lastWeeklyClean));

                    if(customRepeats.HasValue)
                    {
                        SelectedTemplate.Repeats = customRepeats.Value;
                    }
                    

                    for (var i = 0; i < SelectedTemplate.Repeats; i++)
                    {
                        string response = null;
                        if (!isCancelRequested)
                        {
                            showProgressWrapper.MessageParameter[0] += "\n";
                            showProgressWrapper.MessageParameter[0] += string.Format(_localizationService.GetLocalizedString("ProgressBox_Measure_Message_StartingRepeat"), i + 1, SelectedTemplate.Repeats);

                            _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);

                            if (SelectedTemplate.Volume == Volumes.TwoHundred)
                            {
                                mustStop = true;
                                response = CasySerialPortDriver.Measure200(progress);
                                mustStop = false;
                            }
                            else
                            {
                                mustStop = true;
                                response = CasySerialPortDriver.Measure400(progress);
                                mustStop = false;
                            }
                        }

                        if (isCancelRequested) continue;
                        errorResult = _errorController.ParseError(response);

                        IEnumerable<ErrorDetails> softErrors = errorResult.SoftErrorDetails;

                        switch (errorResult.ErrorResultType)
                        {
                            case ErrorResultType.SingleError:
                                if (!softErrors.Any())
                                {
                                    foreach (var error in errorResult.FatalErrorDetails)
                                    {
                                        _logger.Error(LogCategory.Measurement, $"Fatal Error in measuring procedure: {error.Description}. Aborted.");
                                    }

                                    // There is no soft error with special handling. Return the error result
                                    showProgressWrapper.IsFinished = true;
                                    _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>()
                                        .Publish(showProgressWrapper);
                                    _eventAggregatorProvider.Instance.GetEvent<ErrorResultEvent>().Publish(errorResult);
                                    return null;
                                }

                                break;
                            case ErrorResultType.MutipleError:
                                if (errorResult.FatalErrorDetails.Count > 0)
                                {
                                    foreach (var error in errorResult.FatalErrorDetails)
                                    {
                                        _logger.Error(LogCategory.Measurement, $"Fatal Error in measuring procedure: {error.Description}.");
                                    }

                                    showProgressWrapper.IsFinished = true;
                                    _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>()
                                        .Publish(showProgressWrapper);
                                    _eventAggregatorProvider.Instance.GetEvent<ErrorResultEvent>().Publish(errorResult);
                                    return null;
                                }

                                break;
                        }

                        if (!softErrors.Any())
                        {
                            mustStop = true;
                            var measureResultData =
                                await GetCachedMeasureResult(measureResult, progress);
                            mustStop = false;
                            
                            if (measureResultData == null)
                            {
                                return null;
                            }
                            
                            continue;
                        }

                        var softErrorHandleResult = await HandleSoftErrors(progress, softErrors, measureResult, LogCategory.Measurement);

                        switch (softErrorHandleResult)
                        {
                            case ButtonResult.Cancel:
                                showProgressWrapper.IsFinished = true;
                                _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>()
                                    .Publish(showProgressWrapper);
                                return null;
                            case ButtonResult.Retry:
                                i--;
                                //measureResult.MeasureResultDatas.Remove(measureResultData);
                                break;
                            //case ButtonResult.Accept:
                                //measureResult.MeasureResultDatas.Add(measureResultData);
                                //break;
                        }
                    }

                    if (!isCancelRequested)
                    {
                        _measureCounter?.DecreaseCounts(1);

                        showProgressWrapper.MessageParameter[0] += "\n";
                        showProgressWrapper.MessageParameter[0] += _localizationService.GetLocalizedString("ProgressBox_Measure_Message_CalculateResultData");
                        _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);

                        measureResult.ForceClearMeasureResultItems();
                        await _measureResultDataCalculationService.UpdateMeasureResultDataAsync(measureResult, SelectedTemplate);

                        if (SelectedTemplate.IsDeviationControlEnabled && SelectedTemplate.DeviationControlItems.Any())
                        {
                            var deviationViolations = string.Empty;

                            foreach (var measureResultItemType in Enum.GetValues(typeof(MeasureResultItemTypes)))
                            {
                                var deviationControlItem = SelectedTemplate.DeviationControlItems.FirstOrDefault(item => item.MeasureResultItemType == (MeasureResultItemTypes) measureResultItemType);
                                if (deviationControlItem == null) continue;

                                var deviationResultItem = measureResult
                                    .MeasureResultItemsContainers[(MeasureResultItemTypes) measureResultItemType]
                                    .MeasureResultItem;

                                if (deviationResultItem == null) continue;

                                string resultItemTypeName;
                                if (deviationControlItem.MinLimit.HasValue && deviationResultItem.ResultItemValue < deviationControlItem.MinLimit.Value)
                                {
                                    resultItemTypeName = _localizationService.GetLocalizedString($"ResultItemType_{Enum.GetName(typeof(MeasureResultItemTypes), (MeasureResultItemTypes) measureResultItemType)}_Name");
                                    deviationViolations += _localizationService.GetLocalizedString("MessageBox_DeviationControl_MinViolation_Message", resultItemTypeName, deviationControlItem.MinLimit.Value.ToString(CultureInfo.InvariantCulture), deviationResultItem.ResultItemValue.ToString(deviationResultItem.ValueFormat));
                                    deviationViolations += "\n";
                                }

                                if (!deviationControlItem.MaxLimit.HasValue || !(deviationResultItem.ResultItemValue > deviationControlItem.MaxLimit.Value)) continue;

                                resultItemTypeName = _localizationService.GetLocalizedString($"ResultItemType_{Enum.GetName(typeof(MeasureResultItemTypes), (MeasureResultItemTypes) measureResultItemType)}_Name");
                                deviationViolations += _localizationService.GetLocalizedString("MessageBox_DeviationControl_MaxViolation_Message", resultItemTypeName, deviationControlItem.MaxLimit.Value.ToString(CultureInfo.InvariantCulture), deviationResultItem.ResultItemValue.ToString(deviationResultItem.ValueFormat));
                                deviationViolations += "\n";
                            }

                            if (!string.IsNullOrEmpty(deviationViolations))
                            {
                                _logger.Info(LogCategory.Measurement,
                                    $"Measuring procedure detected diviations.");

                                var awaiter = new ManualResetEvent(false);

                                var multiButtonMessageBoxWrapper = new ShowMultiButtonMessageBoxDialogWrapper() {Awaiter = awaiter, Title = "MessageBox_DeviationControl_Title", Message = deviationViolations, FirstButtonUse = ButtonResult.Retry};

                                multiButtonMessageBoxWrapper.FirstButtonUse = ButtonResult.Retry;

                                _eventAggregatorProvider.Instance.GetEvent<ShowMultiButtonMessageBoxEvent>().Publish(multiButtonMessageBoxWrapper);

                                if (awaiter.WaitOne())
                                {
                                    switch (multiButtonMessageBoxWrapper.Result)
                                    {
                                        case ButtonResult.Retry:
                                            measureResult.MeasureResultDatas.Clear();
                                            continue;
                                        case ButtonResult.Cancel:
                                            showProgressWrapper.IsFinished = true;
                                            _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);

                                            _logger.Info(LogCategory.Measurement, $"Measuring procedure caceled due diviation control.");

                                            return null;
                                    }
                                }
                            }
                        }

                        if (SelectedTemplate.IsTemplate)
                        {
                            measureResult.OriginalMeasureSetup = _databaseStorageService.GetMeasureSetup(SelectedTemplate.MeasureSetupId, true);
                            measureResult.OriginalMeasureSetup.MeasureSetupId = -1;
                            measureResult.OriginalMeasureSetup.CapillarySize = SelectedTemplate.CapillarySize;
                            measureResult.OriginalMeasureSetup.ToDiameter = SelectedTemplate.ToDiameter;
                            measureResult.OriginalMeasureSetup.Volume = SelectedTemplate.Volume;
                            measureResult.OriginalMeasureSetup.Repeats = SelectedTemplate.Repeats;
                            measureResult.OriginalMeasureSetup.DilutionFactor = SelectedTemplate.DilutionFactor;
                            measureResult.OriginalMeasureSetup.ChannelCount = measureResult.MeasureResultDatas[0].DataBlock.Length;
                            measureResult.OriginalMeasureSetup.MeasureResult = measureResult;

                            measureResult.OriginalMeasureSetup.IsTemplate = false;

                            foreach (var cursor in measureResult.OriginalMeasureSetup.Cursors)
                            {
                                cursor.CursorId = -1;
                                cursor.MeasureSetup = measureResult.OriginalMeasureSetup;
                            }

                            foreach (var deviationControlItem in measureResult.OriginalMeasureSetup.DeviationControlItems)
                            {
                                deviationControlItem.DeviationControlItemId = -1;
                                deviationControlItem.MeasureSetup = measureResult.OriginalMeasureSetup;
                            }
                        }
                        else
                        {
                            measureResult.OriginalMeasureSetup = SelectedTemplate;
                            measureResult.OriginalMeasureSetup.ChannelCount = measureResult.MeasureResultDatas[0].DataBlock.Length;
                        }


                        measureResult.MeasureSetup.ChannelCount = measureResult.MeasureResultDatas[0].DataBlock.Length;
                        //measureResult.Experiment = SelectedTemplate.DefaultExperiment;
                        //measureResult.Group = SelectedTemplate.DefaultGroup;
                        _measureResultStorageService.StoreMeasureResults(new[] { measureResult }, cloneOriginalSetup: true);

                        _logger.Info(LogCategory.Measurement, $"Measuring procedure successfully completed.");

                        showProgressWrapper.IsFinished = true;
                        _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);

                        if (_remoteIpsService != null && !string.IsNullOrEmpty(workbook))
                        {
                            await _remoteIpsService.PostMeasureResult(measureResult, workbook);
                        }

                        if (!SelectedTemplate.IsAutoSave) return measureResult;
                        var tempName = SelectedTemplate.AutoSaveName;

                        if (_measureResultStorageService.GetMeasureResults(measureResult.Experiment, measureResult.Group).FirstOrDefault(mr => mr.Name == tempName) != null)
                        {
                            var count = 1;
                            var measurementName = $"{tempName}_{count.ToString()}";
                            while (_measureResultStorageService.GetMeasureResults(measureResult.Experiment, measureResult.Group).FirstOrDefault(mr => mr.Name == measurementName) != null)
                            {
                                count++;
                                measurementName = $"{tempName}_{count.ToString()}";
                            }

                            tempName = measurementName;
                        }

                        await _measureResultManager.SaveMeasureResults(new [] { measureResult }, tempName, SelectedTemplate.DefaultExperiment, SelectedTemplate.DefaultGroup);

                        

                        return measureResult;
                        //_measureResultStorageService.ActiveMeasureResult = measureResult;
                    }
                }

                if (isCancelRequested) return null;
                
                showProgressWrapper.IsFinished = true;
                _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);
                _eventAggregatorProvider.Instance.GetEvent<ErrorResultEvent>().Publish(errorResult);

                return null;
            }
        }

        public async Task<ButtonResult> HandleSoftErrors(Progress<string> progress, IEnumerable<ErrorDetails> softErrors, MeasureResult measureResult, LogCategory logCategory)
        {
            //return await Task.Run(() =>
            //{

                MeasureResultData measureResultData = null;
            
                foreach (var softError in softErrors)
                {
                    _logger.Info(logCategory, $"Soft Error detected: {softError.Description}");

                    var awaiter = new ManualResetEvent(false);

                    var softErrorEventWrapper = new ShowMultiButtonMessageBoxDialogWrapper()
                    {
                        Awaiter = awaiter,
                        Title = softError.Description,
                        Message = softError.Notice,
                    };

                    switch (softError.ErrorNumber)
                    {
                        // Timeout
                        case "0":
                        case "1":
                            softErrorEventWrapper.OkButtonUse = ButtonResult.Clean;
                            softErrorEventWrapper.OkButtonString = "MessageBox_Button_Clean_Text";
                            softErrorEventWrapper.FirstButtonUse = ButtonResult.Retry;
                            softErrorEventWrapper.FirstButtonString = "MessageBox_Button_Retry_Text";
                            break;
                        // Air bubble
                        case "2":
                            softErrorEventWrapper.FirstButtonUse = ButtonResult.None;
                            softErrorEventWrapper.SecondButtonUse = ButtonResult.None;
                            softErrorEventWrapper.OkButtonUse = ButtonResult.Cancel;
                            softErrorEventWrapper.OkButtonString = "MessageBox_Button_Ok_Text";
                            //softErrorEventWrapper.OkButtonString = "MessageBox_Button_Cancel_Text";
                            //softErrorEventWrapper.
                            break;
                        //Concentration too high
                        case "4":
                            measureResultData = await GetCachedMeasureResult(measureResult, progress);
                            var totalCounts = measureResultData.DataBlock.Select(data => (int)data).Sum();
                            var maxCounts = _calibrationController.MaxCounts;
                            softErrorEventWrapper.MessageParameter = new[] { totalCounts.ToString(), maxCounts.ToString() };
                            softErrorEventWrapper.FirstButtonUse = ButtonResult.Retry;
                            softErrorEventWrapper.FirstButtonString = "MessageBox_Button_Retry_Text";
                            break;
                    }

                    _eventAggregatorProvider.Instance.GetEvent<ShowMultiButtonMessageBoxEvent>().Publish(softErrorEventWrapper);

                    if (!awaiter.WaitOne()) continue;
                    
                    switch (softErrorEventWrapper.Result)
                    {
                        case ButtonResult.Accept:
                            if (measureResultData == null)
                            {
                                measureResultData = await GetCachedMeasureResult(measureResult, progress);
                            }
                            
                            if (softError.ErrorNumber != "4") return ButtonResult.Accept;
                            
                            if (measureResultData != null) measureResultData.ConcentrationTooHigh = true;

                        _logger.Info(logCategory, $"Soft Error accepted.");
                        return ButtonResult.Accept;
                        case ButtonResult.Clean:
                        //if(showProgressWrapper != null)
                        //{
                        //showProgressWrapper.IsFinished = true;
                        //this._eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);
                        //Thread.Sleep(1000);
                        //}

                        //await Task.Factory.StartNew(() =>
                        //{

                        var cleanCount = 1;
                        var buttonResult = ButtonResult.Clean;

                        ErrorResult result = Clean(cleanCount);

                        var awaiter2 = new ManualResetEvent(false);
                        var softErrorEventWrapper2 = new ShowMultiButtonMessageBoxDialogWrapper()
                        {
                            Awaiter = awaiter2,
                            Title = "MeasureController_CleanResult_Title",
                            Message = result.ErrorResultType == ErrorResultType.NoError ? "MeasureController_CleanResult_Content_Success" : "MeasureController_CleanResult_Content_Failed",
                            OkButtonUse = ButtonResult.Ok,
                            OkButtonString = "MessageBox_Button_Continue_Text",
                            FirstButtonUse = ButtonResult.None,
                            SecondButtonUse = ButtonResult.Cancel
                        };

                        _eventAggregatorProvider.Instance.GetEvent<ShowMultiButtonMessageBoxEvent>()
                            .Publish(softErrorEventWrapper2);

                        if (awaiter2.WaitOne())
                        {
                            buttonResult = softErrorEventWrapper2.Result;

                            if (buttonResult == ButtonResult.Cancel)
                            {
                                _logger.Info(logCategory, $"Soft Error canceled.");
                                return ButtonResult.Cancel;
                            }
                        }
                        else
                        {
                            _logger.Info(logCategory, $"Soft Error canceled.");
                            return ButtonResult.Cancel;
                        }

                        return result.ErrorResultType == ErrorResultType.NoError ? ButtonResult.Retry : ButtonResult.Cancel;
                        //});
                        //break; 
                        default:
                            return softErrorEventWrapper.Result;
                    }
                }

                return ButtonResult.Cancel;
            //});
        }

        /// <summary>
        /// Returns a <see cref="MeasureResult"/> representing the last measurement result of the casy device
        /// </summary>
        /// <param name="measureResult"></param>
        /// <param name="progress">Optional: Implementation of <see cref="IProgress{T}"/> to track the progress of the operation</param>
        /// <param name="hasSoftErrors"></param>
        /// <returns><see cref="MeasureResult"/>respresenting the last maeasurement result of the casy device</returns>
        public async Task<MeasureResultData> GetCachedMeasureResult(MeasureResult measureResult, IProgress<string> progress)
        {
            //if (measureResult == null) return null;
            
            var measureData = CasySerialPortDriver.GetBinaryData(null);
            if (measureData == null) return null;
            
            var dataLength = (measureData.Length - 16) / 2;

            var dataBlock = new double[dataLength];

            using (var ms = new MemoryStream(measureData))
            {
                for (var i = 0; i < dataBlock.Length; i++)
                {
                    var buf = new byte[2];
                    ms.Read(buf, 0, 2);
                    dataBlock[i] = SwapHelper.SwapBytes(BitConverter.ToUInt16(buf, 0));
                }
            }

            var measureResultData = new MeasureResultData {DataBlock = dataBlock , MeasureResult = measureResult};

            if (measureData.Length >= ((dataLength * 2) + 4))
            {
                measureResultData.BelowMeasureLimtCount = SwapHelper.SwapBytes(BitConverter.ToInt32(measureData, dataLength * 2));
            }
            if (measureData.Length >= ((dataLength * 2) + 8))
            {
                measureResultData.BelowCalibrationLimitCount = SwapHelper.SwapBytes(BitConverter.ToInt32(measureData, (dataLength * 2) + 4));
            }
            if (measureData.Length >= ((dataLength * 2) + 12))
            {
                measureResultData.AboveCalibrationLimitCount = SwapHelper.SwapBytes(BitConverter.ToInt32(measureData, (dataLength * 2) + 8));
            }

            var checksum = SwapHelper.SwapBytes(BitConverter.ToUInt32(measureData, measureData.Length - 4));

            var calcCheckSum = Calculations.CalcChecksum(measureData.SubArray(0, measureData.Length - 4));

            if (calcCheckSum != checksum)
            {
                _logger.Info(LogCategory.Measurement, "Invalid checksum detected. Measurement aborted.");

                await Task.Factory.StartNew(() =>
                {
                    var awaiter = new ManualResetEvent(false);

                    var messageBoxWrapper = new ShowMessageBoxDialogWrapper()
                    {
                        Awaiter = awaiter,
                        Title = "Invalid checksum",
                        Message =
                            "The device returned an invalid checksum. Measure result is not valid and will be dismissed."
                    };

                    _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>().Publish(messageBoxWrapper);

                    if (awaiter.WaitOne())
                    {
                    }
                });
                return null;
            }

            measureResult?.MeasureResultDatas.Add(measureResultData);

            return measureResultData;
        }

        /// <summary>
        /// Starts a clean operation with the passed count.
        /// </summary>
        /// <param name="cleanCount">Clean count</param>
        /// <returns><see cref="ErrorResult"/> representing the operations result</returns>
        public ErrorResult Clean(int cleanCount = 1)
        {
            _logger.Info(LogCategory.Service, $"Purging the system '{cleanCount}' times requested.");

            var isCancelRequested = false;
            var mustStop = false;

            ErrorResult errorResult = null;

            var showProgressWrapper = new ShowProgressDialogWrapper
            {
                Title = "ProgressBox_Purge_Title",
                Message = "ProgressBox_Purge_Message",
                MessageParameter = new[] {cleanCount.ToString()},
                IsCancelButtonAvailable = true,
                CancelAction = (isCancel) =>
                {
                    isCancelRequested = true;

                    if (errorResult != null && isCancel)
                    {
                        errorResult.HasCanceled = true;
                    }

                    if (!mustStop) return;
                    CasySerialPortDriver.Stop(new Progress<string>());
                    _logger.Info(LogCategory.Service, 
                        $"Purging the system '{cleanCount}' times has been canceled.");
                },
                IsFinished = false
            };

            _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);

            var progress = new Progress<string>();
            errorResult = Calibrate(progress, null, true);

            if (errorResult.ErrorResultType == ErrorResultType.NoError && !isCancelRequested)
            {
                mustStop = true;
                var response = CasySerialPortDriver.Clean(progress, cleanCount);
                mustStop = false;
                errorResult = _errorController.ParseError(response);   
            }

            if (errorResult.ErrorResultType != ErrorResultType.NoError)
            {
                _logger.Info(LogCategory.Service,
                        $"Errors detected while purging the system '{cleanCount}' times.");
                _eventAggregatorProvider.Instance.GetEvent<ErrorResultEvent>().Publish(errorResult);
            }

            showProgressWrapper.IsFinished = true;
            _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);

            _logger.Info(LogCategory.Service,
                        $"Purging the system '{cleanCount}' times successfully completed");

            return errorResult;
        }

        public ErrorResult CleanWaste()
        {
            _logger.Info(LogCategory.Service, "Cleaning waste procedure has been started.");

            
            if (SelectedTemplate == null) return null;

            var showProgressWrapper = new ShowProgressDialogWrapper
            {
                Title = "ProgressBox_PurgeWaste_Title",
                Message = "ProgressBox_PurgeWaste_Message",
                IsFinished = false
            };

            _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);

            var progress = new Progress<string>();
            var errorResult = Calibrate(progress, null, true);

            if (errorResult.ErrorResultType == ErrorResultType.NoError)
            {
                var response = CasySerialPortDriver.CleanWaste(progress);
                errorResult = _errorController.ParseError(response);

                if (errorResult.ErrorResultType != ErrorResultType.NoError)
                {
                    _logger.Info(LogCategory.Service, "Errors detected while cleaning waste procedure. Aborted.");
                    _eventAggregatorProvider.Instance.GetEvent<ErrorResultEvent>().Publish(errorResult);
                }
            }

            showProgressWrapper.IsFinished = true;
            _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);

            _logger.Info(LogCategory.Service, "Cleaning waste procedure has been successfully completed.");
            return errorResult;
        }

        public ErrorResult CleanCapillary()
        {
            _logger.Info(LogCategory.Service, "Cleaning capillary procedure has been started.");

            var showProgressWrapper = new ShowProgressDialogWrapper
            {
                Title = "ProgressBox_PurgeCapillary_Title",
                Message = "ProgressBox_PurgeCapillary_Message",
                IsFinished = false
            };

            _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);

            var progress = new Progress<string>();
            var errorResult = Calibrate(progress, null, true);

            if (errorResult.ErrorResultType == ErrorResultType.NoError)
            {
                var response = CasySerialPortDriver.CleanCapillary(progress);
                errorResult = _errorController.ParseError(response);

                if (errorResult.ErrorResultType != ErrorResultType.NoError)
                {
                    _logger.Info(LogCategory.Service, "Errors occured while cleaning capillary procedure. Aborted");
                    _eventAggregatorProvider.Instance.GetEvent<ErrorResultEvent>().Publish(errorResult);
                }
            }

            showProgressWrapper.IsFinished = true;
            _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);
            _logger.Info(LogCategory.Service, "Cleaning capillary procedure successfully completed");

            return errorResult;
        }

        private ErrorResult Calibrate(IProgress<string> progress, MeasureSetup measureSetup, bool allowDefaultCalibration)
        {
            var response = _calibrationController.TransferCalibration(progress, measureSetup, allowDefaultCalibration);
            return _errorController.ParseError(response);
        }

        public bool IsValidTemplate(MeasureSetup template)
        {
            return template.CapillarySize != 0 && template.ToDiameter != 0 && !double.IsNaN(template.DilutionFactor);
        }

        public void OnImportsSatisfied()
        {
            this._eventAggregatorProvider.Instance.GetEvent<RemoteCommandEvent>().Subscribe(OnRemoteCommand);
        }

        private void OnRemoteCommand(RemoteCommand remoteCommand)
        {
            if (remoteCommand.Command == "Clean")
            {

            }
        }

        

        
    }
}
