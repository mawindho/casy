using OLS.Casy.Controller.Api;
using OLS.Casy.Models;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Linq;
using OLS.Casy.Models.Enums;
using OLS.Casy.Ui.Core.Api;
using OLS.Casy.Core;
using OLS.Casy.Core.Authorization.Api;
using System;
using System.Collections.Generic;
using OLS.Casy.Core.Localization.Api;
using System.Threading.Tasks;
using System.Threading;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Api;
using System.Windows;
using System.Windows.Media;
using System.ComponentModel;
using System.Collections;

namespace OLS.Casy.Ui.Analyze.ViewModels
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(AnalyzeMeanChartViewModel))]
    public class AnalyzeMeanChartViewModel : AnalyzeChartViewModelBase
    {
        private readonly IMeasureResultDataCalculationService _measureResultDataCalculationService;
        private readonly IAuthenticationService _authenticationService;
        private readonly ILocalizationService _localizationService;
        private readonly IUIProjectManager _uiProjectManager;
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly IMeasureController _measureController;
        private readonly IActivationService _activationService;

        private string _invalidParameters;

        [ImportingConstructor]
        public AnalyzeMeanChartViewModel(
            IMeasureResultManager measureResultManager,
            //Lazy<IMeasureResultContainerViewModel> measureResultContainerViewModel,
            IMeasureResultDataCalculationService measureResultDataCalculationService,
            IAuthenticationService authenticationService,
            ILocalizationService localizationService,
            IUIProjectManager uiProjectManager,
            IEventAggregatorProvider eventAggregatorProvider,
            IMeasureController measureController,
            IActivationService activationService,
            ICompositionFactory compositionFactory
            ) : base(measureResultManager, compositionFactory)
        {
            this._measureResultDataCalculationService = measureResultDataCalculationService;
            this._authenticationService = authenticationService;
            this._localizationService = localizationService;
            this._uiProjectManager = uiProjectManager;
            this._eventAggregatorProvider = eventAggregatorProvider;
            this._measureController = measureController;
            this._activationService = activationService;
        }

        public IMeasureResultContainerViewModel MeasureResultContainerViewModel
        {
            get { return this.MeasureResultContainers[0]; }
        }

        public override void OnImportsSatisfied()
        {
            base.OnImportsSatisfied();
            var export = CompositionFactory.GetExport<IMeasureResultContainerViewModel>();
            AddMeasureResultContainer(new Tuple<Lazy<IMeasureResultContainerViewModel>, IMeasureResultContainerViewModel>(export, export.Value));
            this._eventAggregatorProvider.Instance.GetEvent<MeasureResultStoredEvent>().Subscribe(OnMeasureResultStored);
        }

        private async void OnMeasureResultStored()
        {
            if(this.MeanMeasureResult != null && !this.MeanMeasureResult.IsTemporary)
            {
                if(!this.MeasureResultManager.SelectedMeasureResults.Contains(this.MeanMeasureResult))
                {
                    await this.MeasureResultManager.AddSelectedMeasureResults(new[] { this.MeanMeasureResult });
                    return;
                    //this.MeasureResultManager.SelectedMeasureResults.Insert(0, this.MeanMeasureResult);
                }
            }

            UpdateMeanResult();
        }

        private MeasureResult MeanMeasureResult
        {
            get { return this.MeasureResultManager.MeanMeasureResult; }
            set { this.MeasureResultManager.MeanMeasureResult = value; }
        }

        protected override void OnIsActiveChanged()
        {
            if(this.IsActive)
            {
                lock (((ICollection)MeasureResultManager.SelectedMeasureResults).SyncRoot)
                {
                    foreach (var item in MeasureResultManager.SelectedMeasureResults)
                    {
                        item.PropertyChanged += OnPropertyChanged;
                    }
                }
                

                UpdateMeanResult();

                this.MeasureResultManager.SelectedMeasureResultsChanged += OnSelectedMeasureResultsChanged;
                //ShowInvalidMessageBox();
                //this.IsBusy = false;
            }
            else
            {
                lock (((ICollection)MeasureResultManager.SelectedMeasureResults).SyncRoot)
                {
                    foreach (var item in MeasureResultManager.SelectedMeasureResults)
                    {
                        item.PropertyChanged -= OnPropertyChanged;
                    }
                }

                this.MeasureResultManager.SelectedMeasureResultsChanged -= OnSelectedMeasureResultsChanged;
            }
        }

        protected void OnSelectedMeasureResultsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //Task.Factory.StartNew( () =>
            //{
                if (e.OldItems != null)
                {
                    foreach (var oldItem in e.OldItems.OfType<MeasureResult>())
                    {
                        oldItem.PropertyChanged -= OnPropertyChanged;
                //        oldItem.MeasureSetup.PropertyChanged -= OnMeasureSetupChanged;
                //        oldItem.MeasureSetup.Cursors.CollectionChanged -= OnCursorsChanged;

                //        foreach (var cursor in oldItem.MeasureSetup.Cursors)
                //        {
                //            cursor.PropertyChanged -= OnCursorChanged;
                //        }
                    }
                }

                if (this.IsActive)
                {
                    UpdateMeanResult(e.OldItems != null);
                }

                if (e.NewItems != null)
                {
                    foreach (var newItem in e.NewItems.OfType<MeasureResult>())
                    {
                        newItem.PropertyChanged += OnPropertyChanged;
                //        newItem.MeasureSetup.PropertyChanged += OnMeasureSetupChanged;
                        //newItem.MeasureSetup.Cursors.CollectionChanged += OnCursorsChanged;

                        //foreach (var cursor in newItem.MeasureSetup.Cursors)
                        //{
                        //    cursor.PropertyChanged += OnCursorChanged;
                        //}
                    }
                }
            //});
        }

        

        //private void OnCursorChanged(object sender, PropertyChangedEventArgs e)
        //{
        //    switch(e.PropertyName)
        //    {
        //        case "MinLimit":
        //        case "MaxLimit":
        //        case "Name":
        //            UpdateMeanResult();
        //            break;
        //    }

        //}

        //private void OnCursorsChanged(object sender, NotifyCollectionChangedEventArgs e)
        //{
        //    UpdateMeanResult();
        //}

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!IsActive) return;

            if (e.PropertyName == "IsVisible")
            {
                UpdateMeanResult();
            }
        }

        private object _lock = new object();
        private void UpdateMeanResult(bool haveRemovedItems = false)
        {
            if (!IsActive) return;
            //this.IsBusy = true;
            Task.Run(() =>
            {
                lock (_lock)
                {
                    this.MeasureResultContainerViewModel.IsShowParentsAvailable = false;
                    this.MeasureResultContainerViewModel.ClearMeasureResults();

                    if (this.MeanMeasureResult != null)
                    {
                        this.MeasureResultContainerViewModel.RemoveMeasureResult(this.MeanMeasureResult);
                        

                        this._uiProjectManager.Clear();
                        //foreach (var cursor in this.MeanMeasureResult.MeasureSetup.Cursors)
                        //{
                        //    this._uiProjectManager.Clear(cursor);
                        //}
                        //this._uiProjectManager.Clear(this.MeanMeasureResult.MeasureSetup);
                        //this._uiProjectManager.Clear(this.MeanMeasureResult);
                    }

                    var isCfr = this._activationService.IsModuleEnabled("cfr");

                    var meanMeasureResult = new MeasureResult()
                    {
                        MeasuredAt = DateTime.Now,
                        MeasuredAtTimeZone = TimeZoneInfo.Local,
                        Origin = "Mean",
                        IsCfr = isCfr
                    };

                    this.MeanMeasureResult = null;
                    this.MeasureResultManager.MeanMeasureResult = null;
                    var measureResults = MeasureResultManager.SelectedMeasureResults.Where(mr => mr.IsVisible && mr.MeasureSetup != null).ToArray();

                    if (measureResults.Length > 0)
                    {
                        _invalidParameters = string.Empty;

                        var toDiameters = measureResults.Select(mr => mr.MeasureSetup.ToDiameter).Distinct().ToArray();
                        if (toDiameters.Count() > 1)
                        {
                            _invalidParameters += string.Format("- {0}\n", _localizationService.GetLocalizedString("AnnotationType_ToDiameter_Header"));
                            meanMeasureResult = null;
                        }

                        var fromDiameters = measureResults.Select(mr => mr.MeasureSetup.FromDiameter).Distinct().ToArray();
                        if (fromDiameters.Count() > 1)
                        {
                            _invalidParameters += string.Format("- {0}\n", _localizationService.GetLocalizedString("AnnotationType_FromDiameter_Header"));
                            meanMeasureResult = null;
                        }

                        var repeats = measureResults.Select(mr => mr.MeasureSetup.Repeats).Distinct().ToArray();
                        if (repeats.Count() > 1)
                        {
                            _invalidParameters += string.Format("- {0}\n", _localizationService.GetLocalizedString("AnnotationType_Repeats_Header"));
                            meanMeasureResult = null;
                        }

                        //Kapillare
                        var capillaries = measureResults.Select(mr => mr.MeasureSetup.CapillarySize).Distinct().ToArray();
                        if (capillaries.Count() > 1)
                        {
                            _invalidParameters += string.Format("- {0}\n", _localizationService.GetLocalizedString("AnnotationType_CapillarySize_Header"));
                            meanMeasureResult = null;
                        }

                        var dilutionFactors = measureResults.Select(mr => mr.MeasureSetup.DilutionFactor).Distinct().ToArray();
                        if (dilutionFactors.Count() > 1)
                        {
                            _invalidParameters += string.Format("- {0}\n", _localizationService.GetLocalizedString("AnnotationType_DilutionFactor_Header"));
                            meanMeasureResult = null;
                        }

                        var volumes = measureResults.Select(mr => mr.MeasureSetup.Volume).Distinct().ToArray();
                        if (volumes.Count() > 1)
                        {
                            _invalidParameters += string.Format("- {0}\n", _localizationService.GetLocalizedString("AnnotationType_Volume_Header"));
                            meanMeasureResult = null;
                        }

                        if (meanMeasureResult != null && this._authenticationService.LoggedInUser != null)
                        {
                            var now = DateTime.Now;
                            meanMeasureResult.CreatedBy = string.Format("{0} {1} ({2})", _authenticationService.LoggedInUser.FirstName, _authenticationService.LoggedInUser.LastName, _authenticationService.LoggedInUser.Identity.Name);
                            meanMeasureResult.CreatedAt = now;

                            MeasureSetup meanMeasureSetup = new MeasureSetup();
                            meanMeasureSetup.ChannelCount = measureResults.Max(mr => mr.MeasureSetup.ChannelCount);
                            meanMeasureSetup.CreatedBy = string.Format("{0} {1} ({2})", _authenticationService.LoggedInUser.FirstName, _authenticationService.LoggedInUser.LastName, _authenticationService.LoggedInUser.Identity.Name);
                            meanMeasureSetup.CreatedAt = now;
                            
                            meanMeasureResult.MeasureSetup = meanMeasureSetup;
                            meanMeasureResult.OriginalMeasureSetup = meanMeasureSetup;
                            meanMeasureSetup.MeasureResult = meanMeasureResult;

                            meanMeasureSetup.ToDiameter = toDiameters[0];
                            meanMeasureSetup.FromDiameter = fromDiameters[0];
                            meanMeasureSetup.Repeats = repeats[0];
                            meanMeasureSetup.CapillarySize = capillaries[0];
                            meanMeasureSetup.DilutionFactor = dilutionFactors[0];
                            meanMeasureSetup.Volume = volumes[0];

                            var unitModes = measureResults.Select(mr => mr.MeasureSetup.UnitMode).Distinct().ToArray();
                            meanMeasureSetup.UnitMode = unitModes.Count() > 1 ? UnitModes.Counts : unitModes[0];

                            meanMeasureSetup.IsSmoothing = measureResults.All(mr => mr.MeasureSetup.IsSmoothing);
                            if (meanMeasureSetup.IsSmoothing)
                            {
                                var smoothingFactors = measureResults.Select(mr => mr.MeasureSetup.SmoothingFactor).Distinct().ToArray();
                                meanMeasureSetup.SmoothingFactor = smoothingFactors.Count() > 0 ? 5d : smoothingFactors[0];
                            }

                            var scalingModes = measureResults.Select(mr => mr.MeasureSetup.ScalingMode).Distinct().ToArray();
                            meanMeasureSetup.ScalingMode = scalingModes.Count() > 1 ? ScalingModes.Auto : scalingModes[0];

                            if (meanMeasureSetup.ScalingMode == ScalingModes.MaxRange)
                            {
                                var scalingMaxRanges = measureResults.Select(mr => mr.MeasureSetup.ScalingMaxRange).Distinct().ToArray();
                                meanMeasureSetup.ScalingMaxRange = scalingMaxRanges.Count() > 1 ? scalingMaxRanges.Max() : scalingMaxRanges[0];
                            }

                            var aggregationCalculationModes = measureResults.Select(mr => mr.MeasureSetup.AggregationCalculationMode).Distinct().ToArray();
                            meanMeasureSetup.AggregationCalculationMode = aggregationCalculationModes.Count() > 1 ? AggregationCalculationModes.Off : aggregationCalculationModes[0];

                            if (meanMeasureSetup.AggregationCalculationMode == AggregationCalculationModes.Manual)
                            {
                                var aggregationCalculationManualValues = measureResults.Select(mr => mr.MeasureSetup.ManualAggregationCalculationFactor).Distinct().ToArray();
                                meanMeasureSetup.ManualAggregationCalculationFactor = aggregationCalculationManualValues.Count() > 1 ? aggregationCalculationManualValues.Max() : aggregationCalculationManualValues[0];
                            }

                            var measureModes = measureResults.Select(mr => mr.MeasureSetup.MeasureMode).Distinct().ToArray();
                            meanMeasureSetup.MeasureMode = measureModes.Count() > 1 ? MeasureModes.MultipleCursor : measureModes[0];

                            if (measureModes.Length == 1)
                            {
                                var cursors = measureResults[0].MeasureSetup.Cursors.ToArray();

                                bool cursorEqual = measureResults.Select(mr => mr.MeasureSetup.Cursors.Count()).Distinct().Count() == 1;
                                foreach (var measureResult in measureResults)
                                {
                                    if (measureResult != measureResults[0])
                                    {
                                        foreach (var cursor in measureResult.MeasureSetup.Cursors)
                                        {
                                            cursorEqual &= cursors.Any(c => c.Name == cursor.Name && c.MaxLimit == cursor.MaxLimit && c.MinLimit == cursor.MinLimit);

                                            if (!cursorEqual)
                                            {
                                                break;
                                            }
                                        }
                                    }
                                    if (!cursorEqual)
                                    {
                                        break;
                                    }
                                }

                                if (cursorEqual)
                                {
                                    foreach (var cursor in cursors)
                                    {
                                        meanMeasureSetup.AddCursor(new Cursor()
                                        {
                                            Name = cursor.Name,
                                            MinLimit = cursor.MinLimit,
                                            MaxLimit = cursor.MaxLimit,
                                            IsDeadCellsCursor = cursor.IsDeadCellsCursor,
                                            Color = cursor.Color,
                                            MeasureSetup = meanMeasureSetup
                                        });
                                    }
                                }
                                else if (measureModes[0] == MeasureModes.Viability)
                                {
                                    var minLimit = MeasureResultManager.GetMinMeasureLimit(meanMeasureSetup.CapillarySize);
                                    var deadCellsRange = new Cursor()
                                    {
                                        Name = "Cursor_DeadCells_Name",
                                        MinLimit = minLimit,//Calculations.CalcChannel(meanMeasureSetup.FromDiameter, meanMeasureSetup.ToDiameter, minLimit),
                                        MaxLimit = (meanMeasureSetup.ToDiameter / 2), //Calculations.CalcChannel(meanMeasureSetup.FromDiameter, meanMeasureSetup.ToDiameter, (meanMeasureSetup.ToDiameter / 2)) - 1,
                                        MeasureSetup = meanMeasureSetup,
                                        IsDeadCellsCursor = true
                                    };

                                    meanMeasureSetup.AddCursor(deadCellsRange);

                                    var vitalCellsRange = new Cursor()
                                    {
                                        Name = "Cursor_VitalCells_Name",
                                        MinLimit = (meanMeasureSetup.ToDiameter / 2),//Calculations.CalcChannel(meanMeasureSetup.FromDiameter, meanMeasureSetup.ToDiameter, (meanMeasureSetup.ToDiameter / 2)),
                                        MaxLimit = meanMeasureSetup.ToDiameter,//Calculations.CalcChannel(meanMeasureSetup.FromDiameter, meanMeasureSetup.ToDiameter, meanMeasureSetup.ToDiameter),
                                        MeasureSetup = meanMeasureSetup
                                    };

                                    meanMeasureSetup.AddCursor(vitalCellsRange);
                                }
                            }

                            var lastColorIndex = _measureController.LastColorIndex;
                            var colorName = "ChartColor" + (lastColorIndex % 10 == 0 ? 1 : 1 + (lastColorIndex % 10));

                            meanMeasureResult.Name = _localizationService.GetLocalizedString("MeanMeasureResult_Name");
                            meanMeasureResult.Color = ((SolidColorBrush)Application.Current.Resources[colorName]).Color.ToString();

                            for (var i = 0; i < meanMeasureSetup.Repeats; i++)
                            {
                                MeasureResultData meanMeasureResultData = new MeasureResultData();
                                meanMeasureResultData.MeasureResult = meanMeasureResult;
                                meanMeasureResult.MeasureResultDatas.Add(meanMeasureResultData);
                            }

                            _measureController.LastColorIndex++;

                            UpdateMeanData(meanMeasureResult);

                            this.MeasureResultManager.MeanMeasureResult = meanMeasureResult;
                            //this.MeasureResultContainerViewModel.MeasureSetup = meanMeasureSetup;
                            this.MeasureResultContainerViewModel.AddMeasureResults(new[] { meanMeasureResult });

                            if (this.IsActive)
                            {
                                this.MeasureResultContainerViewModel.ForceUpdate();
                            }
                        }
                        else if (this.IsActive)
                        {
                            ShowInvalidMessageBox();
                        }

                        this.MeasureResultContainerViewModel.IsShowParentsAvailable = measureResults.Length > 1;
                        this.MeasureResultManager.MeanMeasureResult = meanMeasureResult;
                    }
                }
            });
            //this.IsBusy = false;
        }

        //private void OnCursorChanged(object sender, PropertyChangedEventArgs e)
        //{
        //    UpdateMeanResult();
        //}

        //private void OnCursorsChanged(object sender, NotifyCollectionChangedEventArgs e)
        //{
        //    UpdateMeanResult();
        //}

        //private void OnMeasureSetupChanged(object sender, PropertyChangedEventArgs e)
        //{
        //    switch (e.PropertyName)
        //    {
        //        case "Repeats":
        //        case "Volume":
        //        case "MeasureMode":
        //        case "UnitMode":
        //        case "DilutionFactor":
        //        case "IsSmoothing":
        //        case "SmoothingFactor":
        //        case "ScalingMode":
        //        case "ScalingMaxRange":
        //        case "AggregationCalculationMode":
        //            UpdateMeanResult();
        //            break;
        //    }
        //}

        private void ShowInvalidMessageBox()
        {
            if (!string.IsNullOrEmpty(_invalidParameters))
            {
                Task.Factory.StartNew(() =>
                {
                    ManualResetEvent awaiter = new ManualResetEvent(false);

                    ShowMessageBoxDialogWrapper messageBoxDialogWrapper = new ShowMessageBoxDialogWrapper()
                    {
                        Awaiter = awaiter,
                        Message = "AnalyzeMeanChartViewModel_InvalidSelectedMeasurementResult_Message",
                        Title = "AnalyzeMeanChartViewModel_InvalidSelectedMeasurementResult_Title",
                        MessageParameter = new[] { this._invalidParameters }
                    };

                    this._eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>().Publish(messageBoxDialogWrapper);
                    awaiter.WaitOne();
                });
            }
        }

        private void UpdateMeanData(MeasureResult meanMeasureResult)
        {
            var measureResults = this.MeasureResultManager.SelectedMeasureResults.Where(mr => mr.IsVisible).ToArray();

            if (measureResults != null && measureResults.Length > 0)
            {
                var measureResultData = measureResults[0].MeasureResultDatas.FirstOrDefault();

                if (measureResultData != null)
                {
                    double[] meanData = new double[measureResultData.DataBlock.Length];

                    foreach (var measureResult in measureResults)
                    {
                        var summedData = Task.Run(async () => await _measureResultDataCalculationService.SumMeasureResultDataAsync(measureResult)).Result;
                        for (int i = 0; i < summedData.Length; i++)
                        {
                            meanData[i] += summedData[i];
                        }
                    }

                    for (int i = 0; i < meanData.Length; i++)
                    {
                        meanData[i] /= measureResults.Length;
                    }

                    for (int i = 0; i < meanMeasureResult.MeasureResultDatas.Count; i++)
                    {
                        MeasureResultData meanMeasureResultData = meanMeasureResult.MeasureResultDatas.ElementAt(i);

                        double[] dataBlock = new double[meanData.Length];
                        for (int j = 0; j < meanData.Length; j++)
                        {
                            dataBlock[j] = meanData[j] / (double)meanMeasureResult.MeasureResultDatas.Count;
                        }

                        meanMeasureResultData.DataBlock = dataBlock;
                        meanMeasureResultData.AboveCalibrationLimitCount = (measureResults.Select(mr => mr.MeasureResultDatas.Sum(mrd => mrd.AboveCalibrationLimitCount)).Sum() / measureResults.Count()) / meanMeasureResult.MeasureResultDatas.Count;
                        meanMeasureResultData.BelowCalibrationLimitCount = (measureResults.Select(mr => mr.MeasureResultDatas.Sum(mrd => mrd.BelowCalibrationLimitCount)).Sum() / measureResults.Count()) / meanMeasureResult.MeasureResultDatas.Count;
                        meanMeasureResultData.BelowMeasureLimtCount = (measureResults.Select(mr => mr.MeasureResultDatas.Sum(mrd => mrd.BelowMeasureLimtCount)).Sum() / measureResults.Count()) / meanMeasureResult.MeasureResultDatas.Count;
                        meanMeasureResultData.ConcentrationTooHigh = measureResults.Any(item => item.MeasureResultDatas.Any(data => data.ConcentrationTooHigh));
                    }

                    MeasureSetup meanMeasureSetup = meanMeasureResult.MeasureSetup;
                    meanMeasureSetup.VolumeCorrectionFactor = measureResults[0].MeasureSetup.VolumeCorrectionFactor;
                }
            }
        }
    }
}
