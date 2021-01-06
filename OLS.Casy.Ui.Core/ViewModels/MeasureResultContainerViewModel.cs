using MigraDoc.Rendering;
using OLS.Casy.Controller.Api;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Config.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Core.Logging.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Models;
using OLS.Casy.Models.Enums;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Core.Api;
using OLS.Casy.Ui.Core.Documents;
using OLS.Casy.Ui.Core.Models;
using OLS.Casy.Ui.Core.Views;
using PdfSharp.Pdf;
using Prism.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using OLS.Casy.Ui.Core.UndoRedo;
using ChartColorModel = OLS.Casy.Ui.Core.Models.ChartColorModel;

namespace OLS.Casy.Ui.Core.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(IMeasureResultContainerViewModel))]
    public class MeasureResultContainerViewModel : Base.ViewModelBase, IMeasureResultContainerViewModel, IPartImportsSatisfiedNotification
    {
        private readonly ILocalizationService _localizationService;
        private readonly IMeasureResultManager _measureResultManager;
        private readonly IMeasureResultStorageService _measureResultStorageService;
        private readonly IMeasureResultDataCalculationService _measureResultDataCalculationService;
        private readonly ITemplateManager _editTemplateManager;
        private readonly IUIProjectManager _uiProject;
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly IDatabaseStorageService _databaseStorageService;
        private readonly ICompositionFactory _compositionFactory;
        private readonly IAuditTrailViewModel _auditTrailViewModel;
        private readonly IAuthenticationService _authenticationService;
        private readonly ISaveFileDialogService _saveFileDialogService;
        private readonly IEnvironmentService _environmentService;
        private readonly ILogger _logger;
        private readonly IActivationService _activationService;
        private readonly IConfigService _configService;
        private readonly IDocumentSettingsManager _documentSettingsManager;

        private readonly IList<MeasureResult> _measureResults;
        private MeasureSetup _measureSetup;

        private string _totalCountsLabel;
        private string _totalCounts;
        private string _totalCountsState;
        private List<CountsItem> _singleCounts;
        private string _countsAboveDiameter;
        private string _diameter;
        private string _totalCountsCursorLabel;
        private string _totalCountsCursor;
        private string _totalCountsCursorState;
        private string _concentration;

        private GridLength _chartRowHeight = new GridLength(9, GridUnitType.Star);
        private GridLength _dataRowHeight = new GridLength(8, GridUnitType.Star);

        private bool _isExpandViewCollapsed;
        private bool _isSaveVisible = true;
        private bool _isVisible = true;
        private bool _isApplyToParentsAvailable;
        private bool _isShowParentsAvailable;

        private bool _isButtonMenuCollapsed;

        private bool _showParents;

        private static readonly SemaphoreSlim SlowStuffSemaphore = new SemaphoreSlim(1, 1);

        private readonly Dictionary<Type, SubscriptionToken> _subscriptionTokens = new Dictionary<Type, SubscriptionToken>();

        private int _displayOrder;
        
        private bool _disposedValue; // To detect redundant calls
        private bool _canApplyToParents;
        private bool _showOriginal;
        private bool _showMouseOverInGraph;

        [ImportingConstructor]
        public MeasureResultContainerViewModel(
            MeasureResultChartViewModel measureResultChartViewModel,
            MeasureResultDataContainerViewModel measureResultDataContainerViewModel,
            ILocalizationService localizationService,
            IMeasureResultManager measureResultManager,
            IMeasureResultStorageService measureResultStorageService,
            IMeasureResultDataCalculationService measureResultDataCalculationService,
            ITemplateManager editTemplateManager,
            IUIProjectManager uiProject,
            IEventAggregatorProvider eventAggregatorProvider,
            IDatabaseStorageService databaseStorageService,
            ICompositionFactory compositionFactory,
            IAuthenticationService authenticationService,
            [Import(AllowDefault = true)]IAuditTrailViewModel auditTrailViewModel,
            ISaveFileDialogService saveFileDialogService,
            IEnvironmentService environmentService,
            ILogger logger,
            IActivationService activationService,
            IConfigService configService,
            IDocumentSettingsManager documentSettingsManager)
        {
            MeasureResultChartViewModel = measureResultChartViewModel;
            MeasureResultDataContainerViewModel = measureResultDataContainerViewModel;
            _localizationService = localizationService;
            _measureResultManager = measureResultManager;
            _measureResultStorageService = measureResultStorageService;
            _measureResultDataCalculationService = measureResultDataCalculationService;
            _editTemplateManager = editTemplateManager;
            _uiProject = uiProject;
            _databaseStorageService = databaseStorageService;
            _eventAggregatorProvider = eventAggregatorProvider;
            _compositionFactory = compositionFactory;
            _auditTrailViewModel = auditTrailViewModel;
            _authenticationService = authenticationService;
            _saveFileDialogService = saveFileDialogService;
            _environmentService = environmentService;
            _logger = logger;
            _activationService = activationService;
            _configService = configService;
            _documentSettingsManager = documentSettingsManager;

            _measureResults = new List<MeasureResult>();
            _singleCounts = new List<CountsItem>();
        }


        [ConfigItem(true)]
        public bool ShowMouseOverInGraph
        {
            get => _showMouseOverInGraph;
            set
            {
                _showMouseOverInGraph = value;
                NotifyOfPropertyChange();

                MeasureResultChartViewModel.ShowMouseOverInGraph = value;
            }
        }

        public GridLength ChartRowHeight
        {
            get => _chartRowHeight;
            set
            {
                if (value == _chartRowHeight) return;
                _chartRowHeight = value;
                NotifyOfPropertyChange();
            }
        }

        public GridLength DataRowHeight
        {
            get => _dataRowHeight;
            set
            {
                if (value == _dataRowHeight) return;
                _dataRowHeight = value;
                NotifyOfPropertyChange();
            }
        }

        public ICommand ExpandButtonCommand => new OmniDelegateCommand(() => Task.Run(async () => await DoExpandAsync()));

        public bool IsExpandViewCollapsed
        {
            get => _isExpandViewCollapsed;
            set
            {
                if (value == _isExpandViewCollapsed) return;
                _isExpandViewCollapsed = value;
                MeasureResultChartViewModel.IsExpandViewCollapsed = value;
                NotifyOfPropertyChange();
            }
        }

        public ICommand ToggleButtonMenuCommand => new OmniDelegateCommand(DoToggleButtonMenu);

        public bool IsButtonMenuCollapsed
        {
            get => _isButtonMenuCollapsed;
            set
            {
                if (value == _isButtonMenuCollapsed) return;
                _isButtonMenuCollapsed = value;
                MeasureResultChartViewModel.IsButtonMenuCollapsed = value;
                NotifyOfPropertyChange();
            }
        }

        private async Task DoExpandAsync()
        {
            IsExpandViewCollapsed = !IsExpandViewCollapsed;
            await UpdateRowHeightAsync();
        }

        private void DoToggleButtonMenu()
        {
            IsButtonMenuCollapsed = !IsButtonMenuCollapsed;
        }

        public async Task UpdateRowHeightAsync()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (IsExpandViewCollapsed)
                {
                    if (DataRowHeight.Value != 0)
                    {
                        DataRowHeight = new GridLength(0, GridUnitType.Star);
                    }
                }
                else
                {
                    if (DataRowHeight.Value != 8)
                    {
                        DataRowHeight = new GridLength(8, GridUnitType.Star);
                    }
                }
            });
            await UpdateChartDataAsync();
        }

        public void AddMeasureResults(MeasureResult[] measureResults)
        {
            foreach (var measureResult in measureResults)
            {
                var measureResultInternal = measureResult;

                if (measureResultInternal.MeasureSetup == null)
                {
                    measureResultInternal = _measureResultStorageService.GetMeasureResultById(measureResultInternal.MeasureResultId);
                }

                lock (((ICollection)_measureResults).SyncRoot)
                {
                    _measureResults.Add(measureResultInternal);
                }

                measureResultInternal.PropertyChanged += OnMeasureResultPropertyChanged;
                measureResultInternal.MeasureResultDatas.CollectionChanged += OnMeasureResultDataChanged;
            }
            
            Task.Run(async () =>
            {
                await UpdateChartDataAsync();
                await UpdateDataAsync();
            });
            MeasureResultDataContainerViewModel.AddMeasureResults(measureResults);

            NotifyOfPropertyChange("IsSaveAsTemplateCommandEnabled");
            NotifyOfPropertyChange("IsAuditTrailAvailable");
            NotifyOfPropertyChange("IsMultiResultChart");
            NotifyOfPropertyChange("IsReadOnly");
            NotifyOfPropertyChange("CanShowOriginal");
        }

        private void OnMeasureResultDataChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Task.Run(async () => await UpdateChartDataAsync());
        }

        public IEnumerable<MeasureResult> MeasureResults
        {
            get
            {
                lock (((ICollection)_measureResults).SyncRoot)
                {
                    return _measureResults;
                }
            }
        }

        public void RemoveMeasureResult(MeasureResult measureResult)
        {
            List<MeasureResult> toRemoves;
            lock (((ICollection)_measureResults).SyncRoot)
            {
                toRemoves = _measureResults.Where(mr => mr.MeasureResultId == measureResult.MeasureResultId)
                    .ToList();
                foreach (var toRemove in toRemoves)
                {
                    _measureResults.Remove(toRemove);
                    toRemove.PropertyChanged -= OnMeasureResultPropertyChanged;
                }
            }

            Task.Run(async () =>
            {
                await UpdateChartDataAsync();
                await UpdateDataAsync();
            });
            MeasureResultDataContainerViewModel.RemoveMeasureResults(toRemoves.ToArray());

            NotifyOfPropertyChange("IsSaveAsTemplateCommandEnabled");
            NotifyOfPropertyChange("IsAuditTrailAvailable");
            NotifyOfPropertyChange("IsMultiResultChart");
            NotifyOfPropertyChange("IsReadOnly");
            NotifyOfPropertyChange("CanShowOriginal");
        }

        public void ClearMeasureResults()
        {
            lock (((ICollection)_measureResults).SyncRoot)
            {
                var toRemoves = _measureResults.ToList();
                foreach (var toRemove in toRemoves)
                {
                    _measureResults.Remove(toRemove);
                    toRemove.PropertyChanged -= OnMeasureResultPropertyChanged;
                }
                MeasureResultDataContainerViewModel.RemoveMeasureResults(toRemoves.ToArray());
            }
        }

        public MeasureSetup MeasureSetup
        {
            get => _measureSetup;
            set
            {
                _measureSetup = value;
                MeasureResultDataContainerViewModel.SpecializedMeasureSetup = _measureSetup;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange("IsMultiResultChart");
                Task.Run(async () =>
                {
                    await UpdateChartDataAsync();
                    await UpdateDataAsync();
                });
            }
        }

        public MeasureResult SingleResult
        {
            get
            {
                lock (((ICollection)_measureResults).SyncRoot)
                {
                    return _measureResults.Count == 1 ? _measureResults[0] : null;
                }
            }
        }

        public MeasureResultChartViewModel MeasureResultChartViewModel { get; }

        public MeasureResultDataContainerViewModel MeasureResultDataContainerViewModel { get; }

        public bool IsMultiResultChart
        {
            get
            {
                lock (((ICollection)_measureResults).SyncRoot)
                {
                    return _measureResults.Count > 1 || (_measureResults.FirstOrDefault() != null &&
                                                         _measureResults.FirstOrDefault() ==
                                                         _measureResultManager.MeanMeasureResult);
                }
            }
        }

        public bool IsOverlayMode
        {
            get => MeasureResultChartViewModel.IsOverlayMode;
            set => MeasureResultChartViewModel.IsOverlayMode = value;
        }

        public string TotalCountsLabel
        {
            get => _totalCountsLabel;
            set
            {
                if (value == _totalCountsLabel) return;
                _totalCountsLabel = value;
                NotifyOfPropertyChange();
            }
        }

        public string TotalCounts
        {
            get => _totalCounts;
            set
            {
                if (value == _totalCounts) return;
                _totalCounts = value;
                NotifyOfPropertyChange();
            }
        }

        public string TotalCountsState
        {
            get => _totalCountsState;
            set
            {
                if (value == _totalCountsState) return;
                _totalCountsState = value;
                NotifyOfPropertyChange();
            }
        }

        public IEnumerable<CountsItem> SingleCounts => _singleCounts;

        public string TotalCountsCursorLabel
        {
            get => _totalCountsCursorLabel;
            set
            {
                if (value == _totalCountsCursorLabel) return;
                _totalCountsCursorLabel = value;
                NotifyOfPropertyChange();
            }
        }

        public string TotalCountsCursor
        {
            get => _totalCountsCursor;
            set
            {
                if (value == _totalCountsCursor) return;
                _totalCountsCursor = value;
                NotifyOfPropertyChange();
            }
        }

        public string TotalCountsCursorState
        {
            get => _totalCountsCursorState;
            set
            {
                if (value == _totalCountsCursorState) return;
                _totalCountsCursorState = value;
                NotifyOfPropertyChange();
            }
        }

        public string Diameter
        {
            get => _diameter;
            set
            {
                if (value == _diameter) return;
                _diameter = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange("CountsAboveDiameterLabel");
            }
        }

        public string CountsAboveDiameterLabel => string.Format(_localizationService.GetLocalizedString("MeasureContainerView_Label_CountsAboveDiamter"), Diameter);

        public string CountsAboveDiameter
        {
            get => _countsAboveDiameter;
            set
            {
                if (value == _countsAboveDiameter) return;
                _countsAboveDiameter = value;
                NotifyOfPropertyChange();
            }
        }

        public string Concentration
        {
            get => _concentration;
            set
            {
                if (value == _concentration) return;
                _concentration = value;
                NotifyOfPropertyChange();
            }
        }

        public int DisplayOrder
        {
            get => _displayOrder;
            set
            {
                if (value == _displayOrder) return;
                _displayOrder = value;
                NotifyOfPropertyChange();
            }
        }

        public ICommand AuditTrailCommand => new OmniDelegateCommand(OnAuditTrail);

        public bool IsAuditTrailAvailable => _auditTrailViewModel != null && SingleResult != null && !SingleResult.IsTemporary && !SingleResult.IsReadOnly;

        public ICommand ApplyToParentsCommand => new OmniDelegateCommand(OnApplyToParents);

        public bool IsApplyToParentsAvailable
        {
            get => _isApplyToParentsAvailable;
            set
            {
                if (value == _isApplyToParentsAvailable) return;
                _isApplyToParentsAvailable = value;
                NotifyOfPropertyChange();
            }
        }

        public bool CanApplyToParents
        {
            get => _canApplyToParents;
            set
            {
                if (value == _canApplyToParents) return;
                _canApplyToParents = value;
                NotifyOfPropertyChange();
            }
        }

        public ICommand RestoreCommand => new OmniDelegateCommand(async () => await OnRestore());

        public bool ShowParents
        {
            get => _showParents;
            set
            {
                if (value == _showParents) return;
                _showParents = value;
                NotifyOfPropertyChange();
                Task.Run(async () => await UpdateChartDataAsync());
            }
        }

        public ICommand ToggleShowParentsCommand => new OmniDelegateCommand(OnToggleShowParents);

        public bool IsShowParentsAvailable
        {
            get => _isShowParentsAvailable;
            set
            {
                if (value == _isShowParentsAvailable) return;
                _isShowParentsAvailable = value;
                ShowParents = true;
                NotifyOfPropertyChange();
            }
        }

        public ICommand PrintCommand => new OmniDelegateCommand(OnPrint);

        public ICommand SaveCommand => new OmniDelegateCommand(async () => await OnSave());

        public bool IsSaveVisible
        {
            get
            {
                if(SingleResult != null && SingleResult.IsReadOnly)
                {
                    return false;
                }
                return _isSaveVisible; }
            set
            {
                if (value == _isSaveVisible) return;
                _isSaveVisible = value;
                NotifyOfPropertyChange();
            }
        }

        public ICommand SaveAsTemplateCommand => new OmniDelegateCommand(OnSaveAsTemplate);

        public bool IsSaveAsTemplateCommandEnabled
        {
            get
            {
                lock (((ICollection)_measureResults).SyncRoot)
                {
                    return _measureResults.Count > 0 && (SingleResult == null || !SingleResult.IsReadOnly);
                }
            }
        }

        public ICommand SavePictureCommand => new OmniDelegateCommand(OnSavePicture);

        public bool CanShowOriginal => !IsOverlayMode && SingleResult != null && _activationService.IsModuleEnabled("cfr") && IsAuditTrailAvailable;

        public ICommand ShowOriginalCommand => new OmniDelegateCommand(OnShowOriginal);

        public bool ShowOriginal
        {
            get => _showOriginal;
            set
            {
                if (value == _showOriginal) return;
                _showOriginal = value;
                NotifyOfPropertyChange();
            }
        }

        private void OnShowOriginal()
        {
            _auditTrailViewModel.LoadAuditTrailEntries(SingleResult);

            ShowOriginal = !_showOriginal;
            MeasureSetup = _showOriginal ? SingleResult.OriginalMeasureSetup : null;
        }

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (value == _isVisible) return;
                _isVisible = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsReadOnly => SingleResult != null && (SingleResult.IsReadOnly || SingleResult.IsDeletedResult || _showOriginal);

        public bool IsDeleted => SingleResult != null && SingleResult.IsDeletedResult && _activationService.IsModuleEnabled("cfr");

        public void OnImportsSatisfied()
        {
            _configService.InitializeByConfiguration(this);
            _configService.ConfigurationChangedEvent += OnConfigurationChanged;

            MeasureResultChartViewModel.ChartImageCapturesEvent += OnChartImageCaptured;
            _subscriptionTokens.Add(typeof(MeasureResultStoredEvent), _eventAggregatorProvider.Instance.GetEvent<MeasureResultStoredEvent>().Subscribe(OnMeasureResultStored));
            _subscriptionTokens.Add(typeof(ExpandEvent), _eventAggregatorProvider.Instance.GetEvent<ExpandEvent>().Subscribe(OnExpandEvent));

            _measureResultManager.SelectedMeasureResultDataChangedEvent += OnSelectedMeasureResultDataChanged;
            _localizationService.LanguageChanged += OnLanguageChanged;
        }

        

        private void OnConfigurationChanged(object sender, ConfigurationChangedEventArgs e)
        {
            if (e.ChangedItemNames.Contains("ShowMouseOverInGraph"))
            {
                NotifyOfPropertyChange("ShowMouseOverInGraph");
            }
        }

        private void OnSelectedMeasureResultDataChanged(object sender, MeasureResultDataChangedEventArgs e)
        {
            Task.Run(async () => await UpdateDataAsync());
        }

        private void OnExpandEvent()
        {
            Task.Run(async () => await UpdateChartDataAsync());
        }

        private void OnMeasureResultStored()
        {
            NotifyOfPropertyChange("IsAuditTrailAvailable");
            NotifyOfPropertyChange("CanShowOriginal");
        }

        private void OnLanguageChanged(object sender, LocalizationEventArgs e)
        {
            NotifyOfPropertyChange("CountsAboveDiameterLabel");
        }

        public void ForceUpdate()
        {
            Task.Run(async () => await UpdateChartDataAsync());
        }

        private async Task UpdateChartDataAsync()
        {
            var measureResult = SingleResult;
            if (measureResult != null)
            {
                if (MeasureSetup == null)
                {
                    if (_showParents && measureResult == _measureResultManager.MeanMeasureResult)
                    {
                        var selectedMeasureResults = _measureResultManager.SelectedMeasureResults.Where(mr => mr.IsVisible).ToArray();

                        var chartDataSets = new List<double[]>();
                        var chartColors = new List<ChartColorModel>();
                        foreach (var mr in selectedMeasureResults)
                        {
                            var summedData = await _measureResultDataCalculationService.SumMeasureResultDataAsync(mr);
                            chartDataSets.Add(summedData);

                            chartColors.Add(new ChartColorModel(_uiProject, mr, true, 1));
                        }
                        
                        chartDataSets.Add(await _measureResultDataCalculationService.SumMeasureResultDataAsync(measureResult));
                        chartColors.Add(new ChartColorModel(_uiProject, measureResult, false));

                        var seriesDescriptions = selectedMeasureResults.Select(mr => mr.Name).ToList();
                        seriesDescriptions.Add(measureResult.Name);

                        MeasureResultChartViewModel.SetInitialChartData(measureResult.MeasureSetup, chartDataSets.ToArray(), chartColors.ToArray(), seriesDescriptions.ToArray(), IsReadOnly);
                    }
                    else
                    {
                        MeasureResultChartViewModel.SetInitialChartData(_showOriginal ? measureResult.OriginalMeasureSetup :  measureResult.MeasureSetup, new[] { await _measureResultDataCalculationService.SumMeasureResultDataAsync(measureResult) }, new[] { new ChartColorModel(_uiProject, measureResult, false) }, new[] { measureResult.Name }, IsReadOnly, _showOriginal);
                    }
                }
                else
                {
                    MeasureResultChartViewModel.SetInitialChartData(MeasureSetup, new[] { await _measureResultDataCalculationService.SumMeasureResultDataAsync(measureResult) }, new[] { new ChartColorModel(_uiProject, measureResult, false) }, new[] { measureResult.Name }, IsReadOnly, _showOriginal);
                }
            }
            else
            {
                List<MeasureResult> measureResults;
                lock (((ICollection)_measureResults).SyncRoot)
                {
                    measureResults = _measureResults.Where(mr => mr.IsVisible).ToList();
                }

                var chartDataSets = new List<double[]>();
                var chartColors = new List<ChartColorModel>();
                foreach (var mr in measureResults)
                {
                    var summedData = await _measureResultDataCalculationService.SumMeasureResultDataAsync(mr);
                    chartDataSets.Add(summedData);

                    chartColors.Add(new ChartColorModel(_uiProject, mr, false));
                }

                MeasureResultChartViewModel.SetInitialChartData(MeasureSetup, chartDataSets.ToArray(), chartColors,
                    measureResults.Select(mr => mr.Name).ToArray());
            }

            lock (((ICollection)_measureResults).SyncRoot)
            {
                MeasureResultChartViewModel.DisplayName = string.Join(";",
                    _measureResults.Where(mr => mr.IsVisible).Select(mr => mr.Name));
            }
        }

        private async Task UpdateDataAsync()
        {
            await SlowStuffSemaphore.WaitAsync();

            try
            {
                _singleCounts = new List<CountsItem>();

                lock (((ICollection)_measureResults).SyncRoot)
                {
                    if (_measureResults.Count <= 0 || SingleResult == null) return;
                }

                MeasureResultItem countsItem, deviationItem, countsAboveDiameterItem, concentrationItem;
                IEnumerable<MeasureResultItem> countsCursorItems;

                if (_showOriginal)
                {
                    var measureResultItems = (await _measureResultDataCalculationService.GetMeasureResultDataAsync(SingleResult, SingleResult.OriginalMeasureSetup)).ToList();
                    countsItem = measureResultItems.FirstOrDefault(x => x.MeasureResultItemType == MeasureResultItemTypes.Counts && x.Cursor == null);
                    deviationItem = measureResultItems.FirstOrDefault(x => x.MeasureResultItemType == MeasureResultItemTypes.Deviation && x.Cursor == null);
                    countsAboveDiameterItem = measureResultItems.FirstOrDefault(x => x.MeasureResultItemType == MeasureResultItemTypes.CountsAboveDiameter && x.Cursor == null);
                    concentrationItem = measureResultItems.FirstOrDefault(x => x.MeasureResultItemType == MeasureResultItemTypes.Concentration && x.Cursor == null);

                    countsCursorItems = measureResultItems.Where(x => x.MeasureResultItemType == MeasureResultItemTypes.Counts && x.Cursor != null);
                }
                else
                { 
                    //await _measureResultDataCalculationService.UpdateMeasureResultDataAsync(SingleResult);

                    countsItem = SingleResult.MeasureResultItemsContainers[MeasureResultItemTypes.Counts].MeasureResultItem;
                    countsCursorItems = SingleResult.MeasureResultItemsContainers[MeasureResultItemTypes.Counts].CursorItems;
                    deviationItem = SingleResult.MeasureResultItemsContainers[MeasureResultItemTypes.Deviation].MeasureResultItem;
                    countsAboveDiameterItem = SingleResult.MeasureResultItemsContainers[MeasureResultItemTypes.CountsAboveDiameter].MeasureResultItem;
                    concentrationItem = SingleResult.MeasureResultItemsContainers[MeasureResultItemTypes.Concentration].MeasureResultItem;
                }

                if (SingleResult.MeasureSetup == null)
                {
                    TotalCounts = "";
                    TotalCountsCursor = "";
                    TotalCountsCursorLabel = "";
                    TotalCountsCursorState = "Transparent";
                    CountsAboveDiameter = "";

                    return;
                }

                if (countsItem != null)
                {
                    TotalCounts = Math.Round(countsItem.ResultItemValue, 0)
                        .ToString(SingleResult.MeasureResultItemsContainers[MeasureResultItemTypes.Counts]
                            .ValueFormat);

                    var count = 1;
                    foreach (var data in SingleResult.MeasureResultDatas)
                    {
                        var item = new CountsItem
                        {
                            Name = $"#{count}: ",
                            Value = data.DataBlock.Sum().ToString(SingleResult
                                .MeasureResultItemsContainers[MeasureResultItemTypes.Counts].ValueFormat)
                        };
                        count++;
                        _singleCounts.Add(item);
                    }

                    NotifyOfPropertyChange("SingleCounts");

                    if (countsItem.Deviation.HasValue)
                    {
                        TotalCounts += $" (\u00B1 {countsItem.Deviation.Value:0.##} %)";
                    }
                    else if (SingleResult.MeasureResultDatas.Count > 1)
                    {
                        if (deviationItem != null)
                        {
                            TotalCounts += $" (\u00B1 {deviationItem.ResultItemValue:0.##} %)";
                        }
                    }
                }

                if (countsCursorItems.Any())
                {
                    var measureMode = _showOriginal ? SingleResult.OriginalMeasureSetup.MeasureMode : SingleResult.MeasureSetup.MeasureMode;
                    if (measureMode == MeasureModes.MultipleCursor)
                    {
                        TotalCountsCursorLabel =
                            _localizationService.GetLocalizedString(
                                "MeasureContainerView_Label_TotalCountsCursor");

                        //if (_measureResultManager.MeanMeasureResult == SingleResult)
                        //{
                            //TotalCountsCursor =
                                //Math.Round(countsCursorItems.Select(item => item.ResultItemValue).Sum() / _measureResultManager.SelectedMeasureResults.Count, 0)
                                    //.ToString(SingleResult
                                        //.MeasureResultItemsContainers[MeasureResultItemTypes.Counts].ValueFormat);
                        //}
                        //else
                        //{ 
                            TotalCountsCursor =
                                Math.Round(countsCursorItems.Select(item => item.ResultItemValue).Sum(), 0)
                                    .ToString(SingleResult
                                        .MeasureResultItemsContainers[MeasureResultItemTypes.Counts].ValueFormat);
                        //}
                    }
                    else
                    {
                        TotalCountsCursorLabel =
                            _localizationService.GetLocalizedString(
                                "MeasureContainerView_Label_TotalCountsViability");

                        //if (_measureResultManager.MeanMeasureResult == SingleResult)
                        //{
                            //TotalCountsCursor =
                                //Math.Round(countsCursorItems.Where(item => !item.Cursor.IsDeadCellsCursor)
                                        //.Select(item => item.ResultItemValue).Sum() / _measureResultManager.SelectedMeasureResults.Count, 0)
                                    //.ToString(SingleResult
                                        //.MeasureResultItemsContainers[MeasureResultItemTypes.Counts].ValueFormat);
                        //}
                        //else
                        //{
                            TotalCountsCursor =
                                Math.Round(countsCursorItems.Where(item => !item.Cursor.IsDeadCellsCursor)
                                        .Select(item => item.ResultItemValue).Sum(), 0)
                                    .ToString(SingleResult
                                        .MeasureResultItemsContainers[MeasureResultItemTypes.Counts].ValueFormat);
                        //}
                    }
                }

                if (countsItem != null)
                {
                    var totalCountsCompare =
                        countsItem.ResultItemValue;
                    var totalCountsCursorCompare = !countsCursorItems.Any()
                        ? -1
                        : countsCursorItems.Select(item =>
                            item.ResultItemValue).Sum();

                    if (SingleResult.MeasureSetup != null)
                    {
                        switch (SingleResult.MeasureSetup.CapillarySize)
                        {
                            case 150:
                                TotalCountsState = totalCountsCompare > 20000 ? "Red" : string.Empty;
                                TotalCountsCursorState = totalCountsCursorCompare == -1d
                                    ? "Transparent"
                                    : totalCountsCursorCompare > -1 && totalCountsCursorCompare < 500
                                        ? "Orange"
                                        : string.Empty;
                                break;
                            case 60:
                                TotalCountsState = totalCountsCompare > 100000 ? "Red" : string.Empty;
                                TotalCountsCursorState = totalCountsCursorCompare == -1d
                                    ? "Transparent"
                                    : totalCountsCursorCompare > -1 && totalCountsCursorCompare < 1000
                                        ? "Orange"
                                        : string.Empty;
                                break;
                            case 45:
                                TotalCountsState = totalCountsCompare > 250000 ? "Red" : string.Empty;
                                TotalCountsCursorState = totalCountsCursorCompare == -1d
                                    ? "Transparent"
                                    : totalCountsCursorCompare > -1 && totalCountsCursorCompare < 15000
                                        ? "Orange"
                                        : string.Empty;
                                break;
                        }
                    }
                }

                Diameter = SingleResult.MeasureSetup.ToDiameter.ToString();
                TotalCountsLabel =
                    string.Format(
                        _localizationService.GetLocalizedString("MeasureContainerView_Label_TotalCounts"),
                        SingleResult.MeasureSetup.Repeats, (int)SingleResult.MeasureSetup.Volume);

                if (countsAboveDiameterItem != null)
                {
                    CountsAboveDiameter =
                        countsAboveDiameterItem.ResultItemValue.ToString(SingleResult
                            .MeasureResultItemsContainers[MeasureResultItemTypes.CountsAboveDiameter]
                            .ValueFormat);

                    if (countsAboveDiameterItem.Deviation.HasValue)
                    {
                        CountsAboveDiameter += $" (\u00B1 {countsAboveDiameterItem.Deviation.Value:0.##} %)";
                    }
                }

                if (concentrationItem != null)
                {
                    Concentration = _localizationService.GetLocalizedString(
                        concentrationItem.ResultItemValue == 0d
                            ? "MeasureResult_Concentration_Ok"
                            : "MeasureResult_Concentration_TooHigh");
                }
            }
            catch (Exception ex)
            {
                //_logger.Error(ex.Message);
            }
            finally
            {
                SlowStuffSemaphore.Release();
            }            
        }

        private void OnMeasureResultPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var measureResult = sender as MeasureResult;

            switch (e.PropertyName)
            {
                case "Name":
                    lock (((ICollection)_measureResults).SyncRoot)
                    {
                        MeasureResultChartViewModel.DisplayName =
                            string.Join(";", _measureResults.Select(mr => mr.Name));
                    }
                    break;
                case "IsVisible":
                    Task.Run(async () =>
                    {
                        await UpdateChartDataAsync();
                        await UpdateDataAsync();
                    });

                    MeasureResultDataContainerViewModel.SetVisibility(measureResult, measureResult?.IsVisible ?? false);
                    break;
            }
        }

        private async void OnSaveAsTemplate()
        {
            MeasureSetup setupToTemplate = null;
            MeasureSetup newTemplate = null;

            var measureResult = SingleResult;

            await Task.Factory.StartNew(() =>
            {
                if (measureResult != null)
                {
                    setupToTemplate = _showOriginal ? measureResult.OriginalMeasureSetup : measureResult.MeasureSetup;
                    setupToTemplate.ChannelCount = 1024;
                }
                else if (MeasureSetup != null)
                {
                    setupToTemplate = MeasureSetup;
                }

                var awaiter = new ManualResetEvent(false);

                var showInputWrapper = new ShowInputDialogWrapper
                {
                    Awaiter = awaiter,
                    Message = "SaveAsTemplateDialog_Content",
                    Title = "SaveAsTemplateDialog_Title",
                    Watermark = "SaveAsTemplateDialog_Watermark",
                    CanOkDelegate = input => !string.IsNullOrEmpty(input) && input.IndexOfAny(new[] { '/', '\\', ':', '*', '<', '>', '|' }) == -1
                };

                if(setupToTemplate != null && !string.IsNullOrEmpty(setupToTemplate.Name))
                {
                    showInputWrapper.DefaultText = setupToTemplate.Name;
                }

                _eventAggregatorProvider.Instance.GetEvent<ShowInputEvent>().Publish(showInputWrapper);

                if (!awaiter.WaitOne() || showInputWrapper.IsCancel) return;
                var templateName = showInputWrapper.Result;

                if (string.IsNullOrEmpty(templateName)) return;
                var existingTemplate = _databaseStorageService.GetMeasureSetupTemplates().FirstOrDefault(ms => string.Equals(ms.Name, templateName, StringComparison.CurrentCultureIgnoreCase));

                if (existingTemplate != null)
                {
                    awaiter = new ManualResetEvent(false);

                    ShowMultiButtonMessageBoxDialogWrapper multiButtonMessageBoxWrapper;
                    if (_authenticationService.LoggedInUser.UserRole.Priority == 3)
                    {
                        // Template with same name already exists
                        multiButtonMessageBoxWrapper = new ShowMultiButtonMessageBoxDialogWrapper
                        {
                            Awaiter = awaiter,
                            Message = "SaveAsTemplateDialog_SetupAlreadyExists_Content",
                            Title = "SaveAsTemplateDialog_SetupAlreadyExists_Title",
                            FirstButtonUse = ButtonResult.Cancel,
                            OkButtonUse = ButtonResult.Replace,
                            SecondButtonUse = ButtonResult.SaveAsCopy
                        };
                    }
                    else
                    {
                        multiButtonMessageBoxWrapper = new ShowMultiButtonMessageBoxDialogWrapper
                        {
                            Awaiter = awaiter,
                            Message = "SaveAsTemplateDialog_SetupAlreadyExists_Content",
                            Title = "SaveAsTemplateDialog_SetupAlreadyExists_Title",
                            FirstButtonUse = ButtonResult.Cancel,
                            OkButtonUse = ButtonResult.SaveAsCopy
                        };
                    }

                    _eventAggregatorProvider.Instance.GetEvent<ShowMultiButtonMessageBoxEvent>().Publish(multiButtonMessageBoxWrapper);

                    if (awaiter.WaitOne())
                    {
                        switch (multiButtonMessageBoxWrapper.Result)
                        {
                            case ButtonResult.Cancel:
                                return;
                            case ButtonResult.SaveAsCopy:
                                _editTemplateManager.CloneSetup(existingTemplate, ref newTemplate);

                                var tempName = templateName;
                                var count = 1;
                                while (_databaseStorageService.GetMeasureSetupTemplates().FirstOrDefault(ms => string.Equals(ms.Name, templateName, StringComparison.CurrentCultureIgnoreCase)) != null)
                                {
                                    templateName = $"{tempName}_{count.ToString()}";
                                    count++;
                                }
                                newTemplate.Name = templateName;

                                break;
                            case ButtonResult.Replace:
                                _editTemplateManager.CloneSetup(setupToTemplate, ref existingTemplate);
                                newTemplate = existingTemplate;
                                break;
                        }

                        if (measureResult != null && newTemplate != null)
                        {
                            newTemplate.DefaultExperiment = measureResult.Experiment;
                            newTemplate.DefaultGroup = measureResult.Group;
                        }
                    }
                }
                else
                {
                    if (measureResult != null)
                    {
                        _editTemplateManager.CloneSetup(setupToTemplate, ref newTemplate);
                        newTemplate.DefaultExperiment = measureResult.Experiment;
                        newTemplate.DefaultGroup = measureResult.Group;
                        newTemplate.Name = templateName;
                    }
                    else
                    {
                        _editTemplateManager.CloneSetup(setupToTemplate, ref newTemplate);
                        newTemplate.Name = templateName;
                    }
                }

                if (newTemplate != null)
                {
                    newTemplate.Name = templateName;
                    newTemplate.IsTemplate = true;

                    _logger.Info(LogCategory.Template, $"Template '{templateName}' has been created.");
                    _databaseStorageService.SaveMeasureSetup(newTemplate);

                    if (!newTemplate.IsManual && newTemplate.MeasureSetupId > -1)
                    {
                        if (_authenticationService.LoggedInUser.RecentTemplateIds.Contains(newTemplate.MeasureSetupId))
                        {
                            _authenticationService.LoggedInUser.RecentTemplateIds.Remove(newTemplate.MeasureSetupId);
                        }

                        _authenticationService.LoggedInUser.RecentTemplateIds.Insert(0, newTemplate.MeasureSetupId);
                        _authenticationService.SaveUser(_authenticationService.LoggedInUser);
                    }
                }

                _eventAggregatorProvider.Instance.GetEvent<TemplateSavedEvent>().Publish(null);
            });
        }

        private async Task OnSave()
        {
            _environmentService.SetEnvironmentInfo("IsBusy", true);

            List<MeasureResult> toSave;
            lock (((ICollection)_measureResults).SyncRoot)
            {
                toSave = _measureResults.ToList();
            }

            var canSaveAll = toSave.Count > 1 && !toSave.Any(mr => mr.IsTemporary);
            var buttonResult = ButtonResult.None;

            foreach (var measureResult in toSave)
            {
                if (buttonResult == ButtonResult.SaveAll)
                {
                    await _measureResultManager.SaveMeasureResults(toSave, showConfirmationScreen: false,
                        keepAuditTrail: measureResult.IsTemporary);
                    break;
                }

                buttonResult = await _measureResultManager.SaveMeasureResults(new[] {measureResult},
                    isSaveAllAllowed: canSaveAll, keepAuditTrail: measureResult.IsTemporary);
            }

            _environmentService.SetEnvironmentInfo("IsBusy", false);
        }

        private async void OnPrint()
        {
            var result = await Task.Run(async () => await _measureResultManager.SaveChangedMeasureResults());
            if (result == ButtonResult.Cancel)
            {
                return;
            }

            MeasureResultChartViewModel.CaptureImage();
        }

        public void CaptureImage(bool isPrintAll = true)
        {
            IsPrintAll = isPrintAll;
            MeasureResultChartViewModel.CaptureImage();
        }

        public object CurrentDocument { get; set; }

        public bool IsPrintAll { get; set; }

        private void OnSavePicture()
        {
            _saveFileDialogService.Title = _localizationService.GetLocalizedString("SaveAsImageDialog_Title");
            _saveFileDialogService.Filter = "Images|*.png;*.bmp;*.jpg;*.tiff";
            var result = _saveFileDialogService.ShowDialog();
            if (!result.HasValue || !result.Value) return;
            var fileInfo = new FileInfo(_saveFileDialogService.FileName);
            MeasureResultChartViewModel.CaptureImage(fileInfo);
        }

        private async void OnChartImageCaptured(object sender, EventArgs args)
        {
            if (sender != MeasureResultChartViewModel) return;
            
            if (MeasureResultChartViewModel.CapturedImage != null)
            {
                var renderer = new PdfDocumentRenderer(false);
                string fileName;

                var singleResult = SingleResult;
                if (singleResult != null)
                {
                    if (singleResult == _measureResultManager.MeanMeasureResult)
                    {
                        var meanDocument = new MeanDocument(_localizationService, _measureResultDataCalculationService, _authenticationService, _documentSettingsManager, _environmentService);
                        renderer.Document = meanDocument.CreateDocument(_measureResultManager.MeanMeasureResult, _measureResultManager.SelectedMeasureResults.Where(mr => mr.IsVisible).ToArray(), MeasureResultChartViewModel.CapturedImage);

                        fileName = $"Mean_{DateTime.Now:yyyy-dd-M--HH-mm-ss}.pdf";
                    }
                    else
                    {
                        if(singleResult.IsReadOnly)
                        {
                            await Task.Factory.StartNew(() =>
                            {
                                var awaiter = new ManualResetEvent(false);

                                var viewModelExport = _compositionFactory.GetExport<AddCommentDialogModel>();
                                var viewModel = viewModelExport.Value;

                                var wrapper = new ShowCustomDialogWrapper
                                {
                                    Awaiter = awaiter,
                                    Title = "AddCommentDialog_Title",
                                    DataContext = viewModel,
                                    DialogType = typeof(AddCommentDialog)
                                };

                                _eventAggregatorProvider.Instance.GetEvent<ShowCustomDialogEvent>().Publish(wrapper);

                                if (!awaiter.WaitOne()) return;
                                if (!string.IsNullOrEmpty(viewModel.Comment))
                                {
                                    singleResult.Comment = viewModel.Comment;
                                }
                                _compositionFactory.ReleaseExport(viewModelExport);
                            });
                        }

                        
                        var measureResultDocument = new SingleMeasureResultDocument(_localizationService, _authenticationService, _documentSettingsManager, _environmentService);
                        renderer.Document = measureResultDocument.CreateDocument(singleResult, MeasureResultChartViewModel.CapturedImage, _showOriginal, _activationService.IsModuleEnabled("cfr"));

                        fileName = singleResult.IsTemporary ? $"Temporary_{singleResult.MeasureSetup.Name}_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.pdf"
                            : $"{singleResult.Name}_{DateTime.Now:yyyy-dd-M--HH-mm-ss}.pdf";
                    }
                }
                else
                {
                    lock (((ICollection)_measureResults).SyncRoot)
                    {
                        var overlayDocument = new OverlayDocument(_localizationService,
                            _measureResultDataCalculationService, _authenticationService, _documentSettingsManager, _environmentService);
                        renderer.Document = overlayDocument.CreateDocument(
                            _measureResults.Where(mr => mr.IsVisible).ToArray(),
                            MeasureResultChartViewModel.CapturedImage, _measureResultManager.OverlayMeasureSetup);
                    }

                    fileName = $"Overlay_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.pdf";
                }

                renderer.RenderDocument();

                if (renderer.PdfDocument.Version < 14)
                {
                    renderer.PdfDocument.Version = 14;
                }

                if (IsPrintAll)
                {
                    this.CurrentDocument = renderer.PdfDocument;
                    IsPrintAll = false;
                    return;
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
            else
            {
                if (MeasureResultChartViewModel.FileInfo != null && !string.IsNullOrEmpty(MeasureResultChartViewModel.FileInfo.FullName))
                {
                    try
                    {
                        Process.Start(MeasureResultChartViewModel.FileInfo.FullName);
                    }
                    catch (Exception)
                    {
                        await Task.Factory.StartNew(() =>
                        {
                            var awaiter = new ManualResetEvent(false);

                            var messageBoxDialogWrapper = new ShowMessageBoxDialogWrapper()
                            {
                                Awaiter = awaiter,
                                Message = "FailedToOpenFile_Message",
                                Title = "FailedToOpenFile_Title",
                                MessageParameter = new[] { MeasureResultChartViewModel.FileInfo.Name }
                            };

                            _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>()
                                .Publish(messageBoxDialogWrapper);
                            awaiter.WaitOne();
                        });
                    }
                }
                MeasureResultChartViewModel.FileInfo = null;
            }
        }

        private void OnAuditTrail()
        {
            Task.Factory.StartNew(async () =>
            {
                var result = await _measureResultManager.SaveChangedMeasureResults();

                if (result != ButtonResult.Cancel)
                {
                    var awaiter = new ManualResetEvent(false);

                    var wrapper = new ShowCustomDialogWrapper()
                    {
                        Awaiter = awaiter,
                        DataContext = _auditTrailViewModel,
                        DialogType = typeof(IAuditTrailView)
                    };

                    _auditTrailViewModel.LoadAuditTrailEntries(SingleResult);

                    _eventAggregatorProvider.Instance.GetEvent<ShowCustomDialogEvent>().Publish(wrapper);

                    awaiter.WaitOne();
                }
            });
        }

        private void OnToggleShowParents()
        {
            _showParents = !_showParents;
            NotifyOfPropertyChange("ShowParents");

            Task.Run(async () => await UpdateChartDataAsync());
        }

        private async void OnApplyToParents()
        {
            Application.Current.Dispatcher.Invoke(() => { _environmentService.SetEnvironmentInfo("IsBusy", true); });

            await Task.Run(() =>
            {
                try
                {
                    lock (((ICollection)_measureResults).SyncRoot)
                    {
                        foreach (var measureResult in _measureResults)
                        {
                            _uiProject.StartUndoGroup();

                            if (measureResult.MeasureSetup.MeasureMode != MeasureSetup.MeasureMode)
                            {
                                _uiProject.SendUIEdit(measureResult.MeasureSetup, "MeasureMode",
                                    MeasureSetup.MeasureMode);
                            }

                            if (MeasureSetup.AggregationCalculationMode != AggregationCalculationModes.FromParent &&
                                measureResult.MeasureSetup.AggregationCalculationMode !=
                                MeasureSetup.AggregationCalculationMode)
                            {
                                _uiProject.SendUIEdit(measureResult.MeasureSetup, "AggregationCalculationMode",
                                    MeasureSetup.AggregationCalculationMode);
                            }

                            if (measureResult.MeasureSetup.ManualAggregationCalculationFactor !=
                                MeasureSetup.ManualAggregationCalculationFactor)
                            {
                                _uiProject.SendUIEdit(measureResult.MeasureSetup, "ManualAggregationCalculationFactor",
                                    MeasureSetup.ManualAggregationCalculationFactor);
                            }

                            if (measureResult.MeasureSetup.ScalingMode != MeasureSetup.ScalingMode)
                            {
                                _uiProject.SendUIEdit(measureResult.MeasureSetup, "ScalingMode",
                                    MeasureSetup.ScalingMode);

                                if (MeasureSetup.ScalingMode == ScalingModes.MaxRange)
                                {
                                    if (measureResult.MeasureSetup.ScalingMaxRange != MeasureSetup.ScalingMaxRange)
                                    {
                                        _uiProject.SendUIEdit(measureResult.MeasureSetup, "ScalingMaxRange",
                                            MeasureSetup.ScalingMaxRange);
                                    }
                                }
                            }

                            if (measureResult.MeasureSetup.UnitMode != MeasureSetup.UnitMode)
                            {
                                _uiProject.SendUIEdit(measureResult.MeasureSetup, "UnitMode", MeasureSetup.UnitMode);
                            }

                            if (measureResult.MeasureSetup.SmoothingFactor != MeasureSetup.SmoothingFactor)
                            {
                                _uiProject.SendUIEdit(measureResult.MeasureSetup, "SmoothingFactor",
                                    MeasureSetup.SmoothingFactor);
                            }

                            if (measureResult.MeasureSetup.IsSmoothing != MeasureSetup.IsSmoothing)
                            {
                                _uiProject.SendUIEdit(measureResult.MeasureSetup, "IsSmoothing",
                                    MeasureSetup.IsSmoothing);
                            }

                            var notModifiedCursors = measureResult.MeasureSetup.Cursors.ToList();
                            foreach (var cursor in _measureSetup.Cursors)
                            {
                                Casy.Models.Cursor cursorToModify = null;

                                if (measureResult.MeasureSetup.Cursors.Count > 0)
                                {
                                    cursorToModify =
                                        measureResult.MeasureSetup.Cursors.FirstOrDefault(c => c.Name == cursor.Name);
                                }

                                if (cursorToModify == null)
                                {
                                    cursorToModify = new Casy.Models.Cursor
                                    {
                                        Name = cursor.Name,
                                        MinLimit = cursor.MinLimit,
                                        MaxLimit = cursor.MaxLimit,
                                        IsDeadCellsCursor = cursor.IsDeadCellsCursor,
                                        Color = cursor.Color,
                                        MeasureSetup = measureResult.MeasureSetup
                                    };

                                    var insertItem = new UICollectionUndoItem(measureResult.MeasureSetup.Cursors)
                                    {
                                        ModelObject = measureResult.MeasureSetup,
                                        Info = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
                                            cursorToModify, 0),
                                    };
                                    _uiProject.Submit(insertItem);
                                }
                                else
                                {
                                    notModifiedCursors.Remove(cursorToModify);
                                    if (cursorToModify.MinLimit != cursor.MinLimit)
                                    {
                                        _uiProject.SendUIEdit(cursorToModify, "MinLimit", cursor.MinLimit);
                                    }

                                    if (cursorToModify.MaxLimit != cursor.MaxLimit)
                                    {
                                        _uiProject.SendUIEdit(cursorToModify, "MaxLimit", cursor.MaxLimit);
                                    }

                                    if (cursorToModify.IsDeadCellsCursor != cursor.IsDeadCellsCursor)
                                    {
                                        _uiProject.SendUIEdit(cursorToModify, "IsDeadCellsCursor",
                                            cursor.IsDeadCellsCursor);
                                    }

                                    if (cursorToModify.Color != cursor.Color)
                                    {
                                        _uiProject.SendUIEdit(cursorToModify, "Color", cursor.Color);
                                    }
                                }

                            }

                            foreach (var cursor in notModifiedCursors)
                            {
                                var removeItem = new UICollectionUndoItem(measureResult.MeasureSetup.Cursors)
                                {
                                    ModelObject = measureResult.MeasureSetup,
                                    Info = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                                        cursor)
                                };
                                _uiProject.Submit(removeItem);
                            }

                            _uiProject.SubmitUndoGroup();
                        }
                    }

                    Task.Factory.StartNew(() =>
                    {
                        var awaiter = new ManualResetEvent(false);

                        var messageBoxDialogWrapper = new ShowMessageBoxDialogWrapper()
                        {
                            Awaiter = awaiter,
                            Message = "MesureResultContainerViewModel_ApplyToParents_Message",
                            Title = "MesureResultContainerViewModel_ApplyToParents_Title"
                        };

                        _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>()
                            .Publish(messageBoxDialogWrapper);
                        awaiter.WaitOne();
                    });
                }
                catch (Exception)
                {
                    // ignored
                }
                finally
                {
                    _environmentService.SetEnvironmentInfo("IsBusy", false);
                }
            });
        }

        private async Task OnRestore()
        {
            var deletedId = SingleResult.MeasureResultId;

            SingleResult.MeasureResultId = 0;
            SingleResult.MeasureSetup.MeasureSetupId = 0;
            SingleResult.OriginalMeasureSetup.MeasureSetupId = 0;
            foreach (var cursor in SingleResult.MeasureSetup.Cursors)
            {
                cursor.CursorId = 0;
            }
            foreach (var cursor in SingleResult.OriginalMeasureSetup.Cursors)
            {
                cursor.CursorId = 0;
            }
            foreach (var auditTrail in SingleResult.AuditTrailEntries)
            {
                auditTrail.AuditTrailEntryId = 0;
            }

            foreach (var auditTrail in SingleResult.MeasureSetup.AuditTrailEntries)
            {
                auditTrail.AuditTrailEntryId = 0;
            }

            foreach (var auditTrail in SingleResult.OriginalMeasureSetup.AuditTrailEntries)
            {
                auditTrail.AuditTrailEntryId = 0;
            }

            foreach(var data in SingleResult.MeasureResultDatas)
            {
                data.MeasureResultDataId = 0;
            }

            await OnSave();

            _measureResultStorageService.RemoveDeletedMeasureResult(deletedId);
            SingleResult.IsDeletedResult = false;
            //await _measureResultManager.ReplaceSelectedMeasureResult(SingleResult, measureResult);
            _eventAggregatorProvider.Instance.GetEvent<MeasureResultsDeletedEvent>().Publish();
            NotifyOfPropertyChange("IsDeleted");
        }

        

        protected override void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    MeasureResultChartViewModel.ChartImageCapturesEvent -= OnChartImageCaptured;
                    _localizationService.LanguageChanged -= OnLanguageChanged;
                    _measureResultManager.SelectedMeasureResultDataChangedEvent -= OnSelectedMeasureResultDataChanged;
                    _eventAggregatorProvider.Instance.GetEvent<MeasureResultStoredEvent>().Unsubscribe(_subscriptionTokens[typeof(MeasureResultStoredEvent)]);
                    _eventAggregatorProvider.Instance.GetEvent<ExpandEvent>().Unsubscribe(_subscriptionTokens[typeof(ExpandEvent)]);

                    lock (((ICollection) _measureResults).SyncRoot)
                    {
                        foreach (var measureResult in _measureResults)
                        {
                            measureResult.PropertyChanged -= OnMeasureResultPropertyChanged;
                            measureResult.MeasureResultDatas.CollectionChanged -= OnMeasureResultDataChanged;
                        }
                    }
                    MeasureResultChartViewModel.Dispose();
                    _saveFileDialogService.Dispose();
                }

                _disposedValue = true;
            }
            base.Dispose(disposing);
        }
    }
}
