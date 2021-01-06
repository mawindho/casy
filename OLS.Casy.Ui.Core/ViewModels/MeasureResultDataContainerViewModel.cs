using OLS.Casy.Controller.Api;
using OLS.Casy.Core;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Models;
using OLS.Casy.Models.Enums;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Core.Api;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Windows;

namespace OLS.Casy.Ui.Core.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(MeasureResultDataContainerViewModel))]
    public class MeasureResultDataContainerViewModel : ViewModelBase, IPartImportsSatisfiedNotification
    {
        private readonly IMeasureResultManager _measureResultManager;
        private readonly IMeasureResultDataCalculationService _measureResultDataCalculationService;
        private readonly ICompositionFactory _compositionFactory;
        private readonly ILocalizationService _localizationService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IAccessManager _accessManager;
        
        private List<MeasureResult> _measureResults;
        private MeasureSetup _specializedMeasureSetup;

        private static readonly SemaphoreSlim SlowStuffSemaphore = new SemaphoreSlim(1, 1);

        private static readonly char[] CharArray = "ABCDE".ToCharArray();

        [ImportingConstructor]
        public MeasureResultDataContainerViewModel(
            IMeasureResultManager measureResultManager,
            IMeasureResultDataCalculationService measureResultDataCalculationService,
            ICompositionFactory compositionFactory,
            ILocalizationService localizationService,
            IAuthenticationService authenticationService,
            [Import(AllowDefault =true)] IAccessManager accessManager
            )
        {
            _measureResultManager = measureResultManager;
            _measureResultDataCalculationService = measureResultDataCalculationService;
            _compositionFactory = compositionFactory;
            _localizationService = localizationService;
            _authenticationService = authenticationService;
            _accessManager = accessManager;

            MeasureResultDataViewModelExports = new SmartCollection<Lazy<MeasureResultDataViewModel>>();
        }

        public void AddMeasureResults(MeasureResult[] measureResults)
        {
            if(_measureResults == null)
            {
                _measureResults = new List<MeasureResult>();
            }

            lock (((ICollection)_measureResults).SyncRoot)
            {
                _measureResults.AddRange(measureResults);
            }

            var measureResultDataViewModelExports = new List<Lazy<MeasureResultDataViewModel>>();

            foreach (var measureResult in measureResults)
            {
                var measureResultDataViewModelExport = _compositionFactory.GetExport<MeasureResultDataViewModel>();
                var measureResultDataViewModel = measureResultDataViewModelExport.Value;

                measureResultDataViewModel.MeasureResult = measureResult;
                
                MeasureSetup measureSetup;
                if (SpecializedMeasureSetup != null)
                {
                    measureSetup = SpecializedMeasureSetup;
                    measureResultDataViewModel.SpecializedMeasureSetup = SpecializedMeasureSetup;
                }
                else
                {
                    measureSetup = measureResult.MeasureSetup;
                }

                if(measureSetup == null) continue;

                IsViabilityMode = measureSetup.MeasureMode == MeasureModes.Viability;

                if (measureSetup.MeasureMode == MeasureModes.Viability && measureSetup.SmoothedDiameters != null && measureSetup.Cursors != null && measureSetup.Cursors.Any())
                {
                    var cursorResultDataViewModelExport = _compositionFactory.GetExport<CursorResultDataViewModel>();
                    var cursorResultDataViewModel = cursorResultDataViewModelExport.Value;

                    cursorResultDataViewModel.IsViability = true;

                    var minCursor = measureSetup.Cursors.Where(item => item != null).Min(item => item.MinLimit);
                    cursorResultDataViewModel.DebriDisplay = minCursor.ToString("0.##");
                    cursorResultDataViewModel.HasSubpopulations = false;

                    measureResultDataViewModel.CursorResultDataViewModelExports.Add(cursorResultDataViewModelExport);
                    measureResultDataViewModel.CursorResultDataViewModels.Add(cursorResultDataViewModel);
                }

                if (measureSetup.Cursors != null)
                {
                    foreach (var cursor in measureSetup.Cursors.OrderBy(c => c.MinLimit))
                    {
                        var cursorResultDataViewModelExport =
                            _compositionFactory.GetExport<CursorResultDataViewModel>();
                        var cursorResultDataViewModel = cursorResultDataViewModelExport.Value;

                        cursorResultDataViewModel.Cursor = cursor;
                        cursorResultDataViewModel.IsViability = measureSetup.MeasureMode == MeasureModes.Viability;
                        cursorResultDataViewModel.HasSubpopulations = measureSetup.HasSubpopulations;

                        if (cursorResultDataViewModel.HasSubpopulations)
                        {
                            cursorResultDataViewModel.AvailableSubPopulations = CharArray
                                .Take(measureSetup.Cursors.Count).Select(c => c.ToString()).ToList();
                            cursorResultDataViewModel.Subpopulation = cursor.Subpopulation;
                        }

                        measureResultDataViewModel.CursorResultDataViewModelExports
                            .Add(cursorResultDataViewModelExport);
                        measureResultDataViewModel.CursorResultDataViewModels.Add(cursorResultDataViewModel);
                    }
                }

                measureResultDataViewModelExports.Insert(0, measureResultDataViewModelExport);
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                MeasureResultDataViewModelExports.AddRange(measureResultDataViewModelExports);
            });

            NotifyOfPropertyChange("MeasureResultDataViewModels");
            NotifyOfPropertyChange("IsReadOnly");
            NotifyOfPropertyChange("DoBubbleScrollEvent");

            OnSelectedMeasureResultDataChanged(null, null);
        }

        public void SetVisibility(MeasureResult measureResult, bool newValue)
        {
            var toChangeVisibilityItem = MeasureResultDataViewModelExports.FirstOrDefault(vm => vm.Value.MeasureResult == measureResult);
            if(toChangeVisibilityItem != null)
            {
                toChangeVisibilityItem.Value.IsVisible = newValue;
            }
        }

        public void RemoveMeasureResults(MeasureResult[] measureResults)
        {
            var oldItems = new List<Lazy<MeasureResultDataViewModel>>();

            if (_measureResults != null)
            {
                lock (((ICollection)_measureResults).SyncRoot)
                {
                    foreach (var measureResult in measureResults)
                    {
                        _measureResults.Remove(measureResult);
                        oldItems.AddRange(
                            MeasureResultDataViewModelExports.Where(vm => vm.Value.MeasureResult == measureResult));
                    }
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var oldItem in oldItems)
                {
                    MeasureResultDataViewModelExports.Remove(oldItem);
                }
            });

            foreach (var oldItem in oldItems)
            {
                oldItem.Value.Dispose();
                _compositionFactory.ReleaseExport(oldItem);
            }

            NotifyOfPropertyChange("MeasureResultDataViewModels");
            OnSelectedMeasureResultDataChanged(null, null);
        }

        private void SetInitialData(IList<MeasureResult> measureResults)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var measureResultDataViewModelExports = new List<Lazy<MeasureResultDataViewModel>>();
                if (measureResults.Count == 1 || SpecializedMeasureSetup != null)
                {
                    foreach (var measureResult in measureResults)
                    {
                        var measureResultDataViewModelExport = _compositionFactory.GetExport<MeasureResultDataViewModel>();
                        var measureResultDataViewModel = measureResultDataViewModelExport.Value;

                        measureResultDataViewModel.MeasureResult = measureResult;

                        MeasureSetup measureSetup;
                        if (SpecializedMeasureSetup != null)
                        {
                            measureSetup = SpecializedMeasureSetup;
                            measureResultDataViewModel.SpecializedMeasureSetup = SpecializedMeasureSetup;
                        }
                        else
                        {
                            measureSetup = measureResult.MeasureSetup;
                        }

                        IsViabilityMode = measureSetup.MeasureMode == MeasureModes.Viability;

                        if (measureSetup.MeasureMode == MeasureModes.Viability && measureSetup.SmoothedDiameters != null && measureSetup.Cursors != null && measureSetup.Cursors.Any())
                        {
                            var cursorResultDataViewModelExport = _compositionFactory.GetExport<CursorResultDataViewModel>();
                            var cursorResultDataViewModel = cursorResultDataViewModelExport.Value;

                            cursorResultDataViewModel.IsViability = true;

                            var minCursor = measureSetup.Cursors.Where(item => item != null).Min(item => item.MinLimit);
                            cursorResultDataViewModel.DebriDisplay = minCursor.ToString("0.##");
                            cursorResultDataViewModel.HasSubpopulations = false;

                            measureResultDataViewModel.CursorResultDataViewModelExports.Add(cursorResultDataViewModelExport);
                            measureResultDataViewModel.CursorResultDataViewModels.Add(cursorResultDataViewModel);
                        }

                        if (measureSetup.Cursors != null)
                        {
                            foreach (var cursor in measureSetup.Cursors.OrderBy(c => c.MinLimit))
                            {
                                var cursorResultDataViewModelExport =
                                    _compositionFactory.GetExport<CursorResultDataViewModel>();
                                var cursorResultDataViewModel = cursorResultDataViewModelExport.Value;

                                cursorResultDataViewModel.Cursor = cursor;
                                cursorResultDataViewModel.IsViability =
                                    measureSetup.MeasureMode == MeasureModes.Viability;
                                cursorResultDataViewModel.HasSubpopulations = measureSetup.HasSubpopulations;

                                if (cursorResultDataViewModel.HasSubpopulations)
                                {
                                    cursorResultDataViewModel.AvailableSubPopulations = CharArray
                                        .Take(measureSetup.Cursors.Count).Select(c => c.ToString()).ToList();
                                    cursorResultDataViewModel.Subpopulation = cursor.Subpopulation;
                                }

                                measureResultDataViewModel.CursorResultDataViewModelExports.Add(
                                    cursorResultDataViewModelExport);
                                measureResultDataViewModel.CursorResultDataViewModels.Add(cursorResultDataViewModel);
                            }
                        }

                        measureResultDataViewModelExports.Insert(0, measureResultDataViewModelExport);
                    }
                }

                var oldItems = MeasureResultDataViewModelExports.ToArray();
                MeasureResultDataViewModelExports.Reset(measureResultDataViewModelExports);

                foreach(var oldItem in oldItems)
                {
                    oldItem.Value.Dispose();
                    _compositionFactory.ReleaseExport(oldItem);
                }

                NotifyOfPropertyChange("MeasureResultDataViewModels");
                NotifyOfPropertyChange("IsReadOnly");
                NotifyOfPropertyChange("DoBubbleScrollEvent");

                OnSelectedMeasureResultDataChanged(null, null);
            });
        }

        public bool IsReadOnly
        {
            get
            {
                lock (((ICollection)_measureResults).SyncRoot)
                {
                    return _measureResults.Count == 1 && _measureResults[0].IsReadOnly;
                }
            }
        }

        public bool DoBubbleScrollEvent
        {
            get
            {
                if (_measureResults == null)
                {
                    return true;
                }

                return _measureResults.Count <= 1;
            }
        }

        public bool IsViabilityMode
        {
            get => _isViablityMode;
            set
            {
                if (value == _isViablityMode) return;
                _isViablityMode = value;
                NotifyOfPropertyChange();
            }
        }

        public IEnumerable<MeasureResultDataViewModel> MeasureResultDataViewModels
        {
            get { return MeasureResultDataViewModelExports.Select(l => l.Value).AsEnumerable(); }
        }

        public SmartCollection<Lazy<MeasureResultDataViewModel>> MeasureResultDataViewModelExports { get; }

        public MeasureSetup SpecializedMeasureSetup
        {
            get => _specializedMeasureSetup;
            set
            {
                if (value == _specializedMeasureSetup) return;
                _specializedMeasureSetup = value;
                NotifyOfPropertyChange();

                if (_measureResults == null) return;

                List<MeasureResult> measureResults;
                lock (((ICollection)_measureResults).SyncRoot)
                {
                    measureResults = _measureResults.ToList();
                }
                SetInitialData(measureResults);
            }
        }

        public void OnImportsSatisfied()
        {
            _measureResultManager.SelectedMeasureResultDataChangedEvent += OnSelectedMeasureResultDataChanged;
            _localizationService.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object sender, LocalizationEventArgs e)
        {
            OnSelectedMeasureResultDataChanged(null, null);
        }

        private bool HasEditRights(MeasureResult measureResult)
        {
            if(SpecializedMeasureSetup != null)
            {
                return true;
            }

            if (_authenticationService.LoggedInUser == null || measureResult == null)
            {
                return false;
            }

            var userId = _authenticationService.LoggedInUser.Id;
            var groupIds = _authenticationService.LoggedInUser.UserGroups.Select(g => g.Id);
            var isSupervisor = _authenticationService.LoggedInUser.UserRole.Priority == 3;

            lock (((ICollection)_measureResults).SyncRoot)
            {
                return _accessManager == null || measureResult.AccessMappings.Count == 0 || _measureResults.Count > 1 ||
                       _measureResults.FirstOrDefault() == _measureResultManager.MeanMeasureResult || isSupervisor ||
                       measureResult.AccessMappings.Any(am =>
                           am.CanWrite && am.UserId.HasValue && am.UserId.Value == userId) || measureResult
                           .AccessMappings.Where(x => x.CanWrite && x.UserGroupId.HasValue)
                           .Select(x => x.UserGroupId.Value).Intersect(groupIds).Any();
            }
        }

        private async void OnSelectedMeasureResultDataChanged(object sender, MeasureResultDataChangedEventArgs eventArgs)
        {
            await SlowStuffSemaphore.WaitAsync();

            try
            {
            if (_measureResults != null)
            {
                IList<MeasureResult> measureResults;
                MeasureResult[] selectedMeasureResults;
                MeasureResultDataViewModel[] measureResultDataViewModels;

                lock (((ICollection)_measureResults).SyncRoot)
                {
                    measureResults = _measureResults.ToList();
                }

                lock (((ICollection) _measureResultManager.SelectedMeasureResults).SyncRoot)
                {
                    selectedMeasureResults = _measureResultManager.SelectedMeasureResults.ToArray();
                }

                //lock (((ICollection) MeasureResultDataViewModels).SyncRoot)
                //{
                measureResultDataViewModels = MeasureResultDataViewModels.ToArray();
                //}

                foreach (var measureResult in measureResults)
                {
                    var hasEditRights = HasEditRights(measureResult);

                    var measureResultDataViewModel = measureResultDataViewModels.FirstOrDefault(x => x.MeasureResult.MeasureResultId == measureResult.MeasureResultId);
                    if (measureResultDataViewModel == null) continue;

                    var measureSetup = SpecializedMeasureSetup ?? measureResult.MeasureSetup;

                    measureResultDataViewModel.ToDiameter = measureSetup.ToDiameter.ToString();

                    List<MeasureResultItem> measureResultItems;

                    if (measureResult == _measureResultManager.MeanMeasureResult)
                    {
                        await _measureResultDataCalculationService.UpdateMeanDeviationsAsync(measureResult, selectedMeasureResults);
                        measureResultItems = measureResult.MeasureResultItemsContainers.Select(x => x.Value).Select(y => y.MeasureResultItem).ToList();
                        measureResultItems.AddRange(measureResult.MeasureResultItemsContainers.Select(x => x.Value).SelectMany(y => y.CursorItems));
                    }
                    else
                    {
                            //measureResultItems = SpecializedMeasureSetup != null ? (await _measureResultDataCalculationService.GetMeasureResultDataAsync(measureResult, SpecializedMeasureSetup)).ToList() : (await _measureResultDataCalculationService.GetMeasureResultDataAsync(measureResult, null)).ToList();
                            measureResultItems = SpecializedMeasureSetup != null ? (await _measureResultDataCalculationService.GetMeasureResultDataAsync(measureResult, SpecializedMeasureSetup)).ToList() : measureResult.MeasureResultItemsContainers.Select(x => x.Value).Select(y => y.CursorItems.Any() ? y.CursorItems.ToArray() : new [] { y.MeasureResultItem }).SelectMany(x => x).Distinct().ToList();
                        }

                    bool hasSubPopA = false, hasSubPopB = false, hasSubPopC = false, hasSubPopD = false, hasSubPopE = false;
                    if (measureSetup.HasSubpopulations)
                    {
                        hasSubPopA = measureSetup.Cursors.Any(c => c.Subpopulation == "A");
                        hasSubPopB = measureSetup.Cursors.Any(c => c.Subpopulation == "B");
                        hasSubPopC = measureSetup.Cursors.Any(c => c.Subpopulation == "C");
                        hasSubPopD = measureSetup.Cursors.Any(c => c.Subpopulation == "D");
                        hasSubPopE = measureSetup.Cursors.Any(c => c.Subpopulation == "E");
                    }

                    if (measureSetup.MeasureMode == MeasureModes.Viability)
                    {
                        measureResultDataViewModel.AggregationFactorColumnWidth = new GridLength(1, GridUnitType.Star);

                        measureResultDataViewModel.CountsTitle = _localizationService.GetLocalizedString("MeasureResultDataView_Label_ViableCells");
                        measureResultDataViewModel.PercentageTitle = _localizationService.GetLocalizedString("MeasureResultDataView_Label_Viability");

                        var debriCursorResultDataViewModel = measureResultDataViewModel.CursorResultDataViewModels.FirstOrDefault(item => item.IsDebris);

                        if (debriCursorResultDataViewModel == null)
                        {
                            var debriCursorResultDataViewModelExport = _compositionFactory.GetExport<CursorResultDataViewModel>();
                            debriCursorResultDataViewModel = debriCursorResultDataViewModelExport.Value;

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                measureResultDataViewModel.CursorResultDataViewModelExports.Add(debriCursorResultDataViewModelExport);
                                measureResultDataViewModel.CursorResultDataViewModels.Add(debriCursorResultDataViewModel);
                            });
                        }
                            
                        if(measureSetup.SmoothedDiameters != null && measureSetup.Cursors.Any())
                        { 
                            debriCursorResultDataViewModel.DebriDisplay = measureSetup.Cursors.Min(item => item.MinLimit).ToString("0.##");
                        }

                        var debriCountItem = measureResultItems.FirstOrDefault(item => item.MeasureResultItemType == MeasureResultItemTypes.DebrisCount);
                        if (debriCountItem != null)
                        {
                            debriCursorResultDataViewModel.CountsPerMl = debriCountItem.ResultItemValue.ToString(debriCountItem.ValueFormat);

                            if (debriCountItem.Deviation.HasValue)
                            {
                                debriCursorResultDataViewModel.CountsPerMl +=
                                    $"\n(\u00B1 {debriCountItem.Deviation.Value:0.0} %)";
                            }
                        }
                    }
                    else
                    {
                        measureResultDataViewModel.AggregationFactorColumnWidth = new GridLength(1, GridUnitType.Star);

                        measureResultDataViewModel.CountsTitle = _localizationService.GetLocalizedString("MeasureResultDataView_Label_CountsPerMl");
                        measureResultDataViewModel.PercentageTitle = _localizationService.GetLocalizedString("MeasureResultDataView_Label_CountsPercentage");

                        measureResultDataViewModel.HasSubpopulationA = hasSubPopA;
                        if (hasSubPopA)
                        {
                            var totalCountsItem = measureResultItems.FirstOrDefault(item => item.MeasureResultItemType == MeasureResultItemTypes.SubpopulationACountsPerMl && item.Cursor == null);
                            if (totalCountsItem != null)
                            {
                                measureResultDataViewModel.TotalCountsPerMlA =
                                    totalCountsItem.ResultItemValue.ToString(totalCountsItem.ValueFormat);
                            }

                            var percentageItem = measureResultItems.FirstOrDefault(item => item.MeasureResultItemType == MeasureResultItemTypes.SubpopulationAPercentage && item.Cursor == null);
                            if (percentageItem != null)
                            {
                                measureResultDataViewModel.TotalCountsPercentageA =
                                    percentageItem.ResultItemValue.ToString(percentageItem.ValueFormat);
                            }
                        }

                        measureResultDataViewModel.HasSubpopulationB = hasSubPopB;
                        if (hasSubPopB)
                        {
                            var totalCountsItem = measureResultItems.FirstOrDefault(item => item.MeasureResultItemType == MeasureResultItemTypes.SubpopulationBCountsPerMl && item.Cursor == null);
                            if (totalCountsItem != null)
                            {
                                measureResultDataViewModel.TotalCountsPerMlB =
                                    totalCountsItem.ResultItemValue.ToString(totalCountsItem.ValueFormat);
                            }

                            var percentageItem = measureResultItems.FirstOrDefault(item => item.MeasureResultItemType == MeasureResultItemTypes.SubpopulationBPercentage && item.Cursor == null);
                            if (percentageItem != null)
                            {
                                measureResultDataViewModel.TotalCountsPercentageB =
                                    percentageItem.ResultItemValue.ToString(percentageItem.ValueFormat);
                            }
                        }

                        measureResultDataViewModel.HasSubpopulationC = hasSubPopC;
                        if (hasSubPopC)
                        {
                            var totalCountsItem = measureResultItems.FirstOrDefault(item => item.MeasureResultItemType == MeasureResultItemTypes.SubpopulationCCountsPerMl && item.Cursor == null);
                            if (totalCountsItem != null)
                            {
                                measureResultDataViewModel.TotalCountsPerMlC =
                                    totalCountsItem.ResultItemValue.ToString(totalCountsItem.ValueFormat);
                            }

                            var percentageItem = measureResultItems.FirstOrDefault(item => item.MeasureResultItemType == MeasureResultItemTypes.SubpopulationCPercentage && item.Cursor == null);
                            if (percentageItem != null)
                            {
                                measureResultDataViewModel.TotalCountsPercentageC =
                                    percentageItem.ResultItemValue.ToString(percentageItem.ValueFormat);
                            }
                        }

                        measureResultDataViewModel.HasSubpopulationD = hasSubPopD;
                        if (hasSubPopD)
                        {
                            var totalCountsItem = measureResultItems.FirstOrDefault(item => item.MeasureResultItemType == MeasureResultItemTypes.SubpopulationDCountsPerMl && item.Cursor == null);
                            if (totalCountsItem != null)
                            {
                                measureResultDataViewModel.TotalCountsPerMlD =
                                    totalCountsItem.ResultItemValue.ToString(totalCountsItem.ValueFormat);
                            }

                            var percentageItem = measureResultItems.FirstOrDefault(item => item.MeasureResultItemType == MeasureResultItemTypes.SubpopulationDPercentage && item.Cursor == null);
                            if (percentageItem != null)
                            {
                                measureResultDataViewModel.TotalCountsPercentageD =
                                    percentageItem.ResultItemValue.ToString(percentageItem.ValueFormat);
                            }
                        }

                        measureResultDataViewModel.HasSubpopulationE = hasSubPopE;
                        if (hasSubPopE)
                        {
                            var totalCountsItem = measureResultItems.FirstOrDefault(item => item.MeasureResultItemType == MeasureResultItemTypes.SubpopulationECountsPerMl && item.Cursor == null);
                            if (totalCountsItem != null)
                            {
                                measureResultDataViewModel.TotalCountsPerMlE =
                                    totalCountsItem.ResultItemValue.ToString(totalCountsItem.ValueFormat);
                            }

                            var percentageItem = measureResultItems.FirstOrDefault(item => item.MeasureResultItemType == MeasureResultItemTypes.SubpopulationEPercentage && item.Cursor == null);
                            if (percentageItem != null)
                            {
                                measureResultDataViewModel.TotalCountsPercentageE =
                                    percentageItem.ResultItemValue.ToString(percentageItem.ValueFormat);
                            }
                        }
                    }

                    var groupedByCursor = measureResultItems.Where(item => item?.Cursor != null).OrderBy(item => item.Cursor.MinLimit).GroupBy(item => item.Cursor).ToList();

                    var totalCountsPerMl = 0d;
                    var totalVolPerMl = 0d;

                    foreach (var cursorGroup in groupedByCursor)
                    {
                        var cursorResultDataViewModel = cursorGroup.Key == null ? measureResultDataViewModel.CursorResultDataViewModels.FirstOrDefault(item => Equals(item.Cursor, cursorGroup.Key)) : measureResultDataViewModel.CursorResultDataViewModels.FirstOrDefault(item => cursorGroup.Key != null && item.Cursor != null && item.Cursor.Equals(cursorGroup.Key));

                        if (cursorResultDataViewModel == null)
                        {
                            var cursorResultDataViewModelExport = _compositionFactory.GetExport<CursorResultDataViewModel>();
                            cursorResultDataViewModel = cursorResultDataViewModelExport.Value;
                            cursorResultDataViewModel.Cursor = cursorGroup.Key;
                            cursorResultDataViewModel.IsViability = measureSetup.MeasureMode == MeasureModes.Viability;

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                if (measureResultDataViewModel.CursorResultDataViewModels.Any(item =>
                                    Equals(item.Cursor, cursorGroup.Key))) return;
                                measureResultDataViewModel.CursorResultDataViewModelExports.Add(cursorResultDataViewModelExport);
                                measureResultDataViewModel.CursorResultDataViewModels.Add(cursorResultDataViewModel);
                            });
                        }

                        if (!cursorResultDataViewModel.HasSubpopulations && measureSetup.HasSubpopulations)
                        {
                            cursorResultDataViewModel.AvailableSubPopulations = CharArray.Take(measureSetup.Cursors.Count).Select(c => c.ToString()).ToList();
                        }
                        else if (cursorResultDataViewModel.HasSubpopulations && !measureSetup.HasSubpopulations)
                        {
                            cursorResultDataViewModel.AvailableSubPopulations = new List<string>();
                            cursorResultDataViewModel.Subpopulation = null;
                        }
                        cursorResultDataViewModel.HasSubpopulations = measureSetup.HasSubpopulations;
                        cursorResultDataViewModel.HasSubpopulationA = hasSubPopA;
                        cursorResultDataViewModel.HasSubpopulationB = hasSubPopB;
                        cursorResultDataViewModel.HasSubpopulationC = hasSubPopC;
                        cursorResultDataViewModel.HasSubpopulationD = hasSubPopD;
                        cursorResultDataViewModel.HasSubpopulationE = hasSubPopE;

                        if (cursorResultDataViewModel.Cursor.Subpopulation == "A")
                        {
                            var subPopAPercentageItem = cursorGroup.FirstOrDefault(item => item.MeasureResultItemType == MeasureResultItemTypes.SubpopulationAPercentage);
                            cursorResultDataViewModel.CountsPercentageA = subPopAPercentageItem != null ? subPopAPercentageItem.ResultItemValue.ToString(subPopAPercentageItem.ValueFormat) : "";

                            var subPopACountsItem = cursorGroup.FirstOrDefault(item => item.MeasureResultItemType == MeasureResultItemTypes.CountsPerMl);
                            cursorResultDataViewModel.CountsPerMlA = subPopACountsItem != null ? subPopACountsItem.ResultItemValue.ToString(subPopACountsItem.ValueFormat) : "";
                        }

                        if (cursorResultDataViewModel.Cursor.Subpopulation == "B")
                        {
                            var subPopBItem = cursorGroup.FirstOrDefault(item => item.MeasureResultItemType == MeasureResultItemTypes.SubpopulationBPercentage);
                            cursorResultDataViewModel.CountsPercentageB = subPopBItem != null ? subPopBItem.ResultItemValue.ToString(subPopBItem.ValueFormat) : "";

                            var subPopBCountsItem = cursorGroup.FirstOrDefault(item => item.MeasureResultItemType == MeasureResultItemTypes.CountsPerMl);
                            cursorResultDataViewModel.CountsPerMlB = subPopBCountsItem != null ? subPopBCountsItem.ResultItemValue.ToString(subPopBCountsItem.ValueFormat) : "";
                        }

                        if (cursorResultDataViewModel.Cursor.Subpopulation == "C")
                        {
                            var subPopCItem = cursorGroup.FirstOrDefault(item => item.MeasureResultItemType == MeasureResultItemTypes.SubpopulationCPercentage);
                            cursorResultDataViewModel.CountsPercentageC = subPopCItem != null ? subPopCItem.ResultItemValue.ToString(subPopCItem.ValueFormat) : "";

                            var subPopCCountsItem = cursorGroup.FirstOrDefault(item => item.MeasureResultItemType == MeasureResultItemTypes.CountsPerMl);
                            cursorResultDataViewModel.CountsPerMlC = subPopCCountsItem != null ? subPopCCountsItem.ResultItemValue.ToString(subPopCCountsItem.ValueFormat) : "";
                        }

                        if (cursorResultDataViewModel.Cursor.Subpopulation == "D")
                        {
                            var subPopDItem = cursorGroup.FirstOrDefault(item => item.MeasureResultItemType == MeasureResultItemTypes.SubpopulationDPercentage);
                            cursorResultDataViewModel.CountsPercentageD = subPopDItem != null ? subPopDItem.ResultItemValue.ToString(subPopDItem.ValueFormat) : "";

                            var subPopDCountsItem = cursorGroup.FirstOrDefault(item => item.MeasureResultItemType == MeasureResultItemTypes.CountsPerMl);
                            cursorResultDataViewModel.CountsPerMlD = subPopDCountsItem != null ? subPopDCountsItem.ResultItemValue.ToString(subPopDCountsItem.ValueFormat) : "";
                        }

                        if (cursorResultDataViewModel.Cursor.Subpopulation == "E")
                        {
                            var subPopEItem = cursorGroup.FirstOrDefault(item => item.MeasureResultItemType == MeasureResultItemTypes.SubpopulationEPercentage);
                            cursorResultDataViewModel.CountsPercentageE = subPopEItem != null ? subPopEItem.ResultItemValue.ToString(subPopEItem.ValueFormat) : "";

                            var subPopECountsItem = cursorGroup.FirstOrDefault(item => item.MeasureResultItemType == MeasureResultItemTypes.CountsPerMl);
                            cursorResultDataViewModel.CountsPerMlE = subPopECountsItem != null ? subPopECountsItem.ResultItemValue.ToString(subPopECountsItem.ValueFormat) : "";
                        }

                        cursorResultDataViewModel.IsReadOnly = measureResult.IsReadOnly || measureResult.IsDeletedResult || !hasEditRights || (_specializedMeasureSetup != null && measureResult.OriginalMeasureSetup == _specializedMeasureSetup);
                        cursorResultDataViewModel.IsNotReadOnlyMeasureResult = !measureResult.IsReadOnly && !measureResult.IsDeletedResult && hasEditRights && !(_specializedMeasureSetup != null && measureResult.OriginalMeasureSetup == _specializedMeasureSetup);
                        cursorResultDataViewModel.IsViability = measureSetup.MeasureMode == MeasureModes.Viability;

                        if (measureSetup.MeasureMode == MeasureModes.Viability)
                        {
                            var viableCellsItem = cursorGroup.FirstOrDefault(item => item.MeasureResultItemType == MeasureResultItemTypes.ViableCellsPerMl);
                            if (viableCellsItem != null)
                            {
                                cursorResultDataViewModel.CountsPerMl = viableCellsItem.ResultItemValue.ToString(viableCellsItem.ValueFormat);
                                totalCountsPerMl += viableCellsItem.ResultItemValue;

                                if (viableCellsItem.Deviation.HasValue)
                                {
                                    cursorResultDataViewModel.CountsPerMl +=
                                        $"\n(\u00B1 {viableCellsItem.Deviation.Value:0.0} %)";
                                }
                            }
                        }
                        else
                        {
                            var countsPerMlItem = cursorGroup.FirstOrDefault(item => item.MeasureResultItemType == MeasureResultItemTypes.CountsPerMl);
                            if (countsPerMlItem != null)
                            {
                                cursorResultDataViewModel.CountsPerMl = countsPerMlItem.ResultItemValue.ToString(countsPerMlItem.ValueFormat);
                                totalCountsPerMl += countsPerMlItem.ResultItemValue;

                                if (countsPerMlItem.Deviation.HasValue)
                                {
                                    cursorResultDataViewModel.CountsPerMl +=
                                        $"\n(\u00B1 {countsPerMlItem.Deviation.Value:0.0} %)";
                                }
                            }
                        }

                        if (measureSetup.MeasureMode == MeasureModes.MultipleCursor || (!cursorGroup.Key.IsDeadCellsCursor && measureSetup.MeasureMode == MeasureModes.Viability))
                        {
                            if (cursorGroup.Key.MeasureSetup.AggregationCalculationMode == AggregationCalculationModes.Off)
                            {
                                cursorResultDataViewModel.AggregationFactor = this._localizationService.GetLocalizedString("AggregationCalculationMode_Off_Name");
                            }
                            else
                            {
                                var aggregationFactorItem = cursorGroup.FirstOrDefault(item => item.MeasureResultItemType == MeasureResultItemTypes.AggregationFactor);
                                if (aggregationFactorItem != null)
                                {
                                    cursorResultDataViewModel.AggregationFactor = aggregationFactorItem.ResultItemValue.ToString(aggregationFactorItem.ValueFormat);

                                    if (aggregationFactorItem.Deviation.HasValue)
                                    {
                                        cursorResultDataViewModel.AggregationFactor +=
                                            $"\n(\u00B1 {aggregationFactorItem.Deviation.Value:0.0} %)";
                                    }
                                }
                            }
                        }

                        if (!cursorGroup.Key.IsDeadCellsCursor && measureSetup.MeasureMode == MeasureModes.Viability)
                        {
                            var viabilityItem = cursorGroup.FirstOrDefault(item => item.MeasureResultItemType == MeasureResultItemTypes.Viability);
                            if (viabilityItem != null)
                            {
                                cursorResultDataViewModel.CountsPercentage = viabilityItem.ResultItemValue.ToString(viabilityItem.ValueFormat);

                                if (viabilityItem.Deviation.HasValue)
                                {
                                    cursorResultDataViewModel.CountsPercentage +=
                                        $"\n(\u00B1 {viabilityItem.Deviation.Value:0.0} %)";
                                }
                            }
                        }
                        else if(measureSetup.MeasureMode != MeasureModes.Viability)
                        {
                            var countsPercentageItem = cursorGroup.FirstOrDefault(item => item.MeasureResultItemType == MeasureResultItemTypes.CountsPercentage);
                            if (countsPercentageItem != null)
                            {
                                cursorResultDataViewModel.CountsPercentage = countsPercentageItem.ResultItemValue.ToString(countsPercentageItem.ValueFormat);

                                if (countsPercentageItem.Deviation.HasValue)
                                {
                                    cursorResultDataViewModel.CountsPercentage +=
                                        $"\n(\u00B1 {countsPercentageItem.Deviation.Value:0.0} %)";
                                }
                            }
                        }

                        var volumePerMlItem = cursorGroup.FirstOrDefault(item => item.MeasureResultItemType == MeasureResultItemTypes.VolumePerMl);
                        if (volumePerMlItem != null)
                        {
                            cursorResultDataViewModel.VolumePerMl =
                                $"{volumePerMlItem.ResultItemValue.ToString(volumePerMlItem.ValueFormat)}";
                            totalVolPerMl += volumePerMlItem.ResultItemValue;

                            if (volumePerMlItem.Deviation.HasValue)
                            {
                                cursorResultDataViewModel.VolumePerMl +=
                                    $"\n(\u00B1 {volumePerMlItem.Deviation.Value:0.0} %)";
                            }
                        }

                        var peakVolumeItem = cursorGroup.FirstOrDefault(item => item.MeasureResultItemType == MeasureResultItemTypes.PeakVolume);
                        if (peakVolumeItem != null)
                        {
                            cursorResultDataViewModel.PeakVolume =
                                $"{peakVolumeItem.ResultItemValue.ToString(peakVolumeItem.ValueFormat)}";
                                
                            if (peakVolumeItem.Deviation.HasValue)
                            {
                                cursorResultDataViewModel.PeakVolume +=
                                    $"\n(\u00B1 {peakVolumeItem.Deviation.Value:0.0} %)";
                            }
                        }

                        var peakDiameterItem = cursorGroup.FirstOrDefault(item => item.MeasureResultItemType == MeasureResultItemTypes.PeakDiameter);
                        if (peakDiameterItem != null)
                        {
                            cursorResultDataViewModel.PeakDiameter =
                                $"{peakDiameterItem.ResultItemValue.ToString(peakDiameterItem.ValueFormat)}";

                            if (peakDiameterItem.Deviation.HasValue)
                            {
                                cursorResultDataViewModel.PeakDiameter +=
                                    $"\n(\u00B1 {peakDiameterItem.Deviation.Value:0.0} %)";
                            }
                        }

                        var meanDiameterItem = cursorGroup.FirstOrDefault(item => item.MeasureResultItemType == MeasureResultItemTypes.MeanDiameter);
                        if (meanDiameterItem == null) continue;
                        cursorResultDataViewModel.MeanDiameter =
                            $"{meanDiameterItem.ResultItemValue.ToString(meanDiameterItem.ValueFormat)}";

                        if (meanDiameterItem.Deviation.HasValue)
                        {
                            cursorResultDataViewModel.MeanDiameter +=
                                $"\n(\u00B1 {meanDiameterItem.Deviation.Value:0.0} %)";
                        }
                    }

                    var toRemoves = measureResultDataViewModel.CursorResultDataViewModelExports.Where(item =>
                    {
                        if (measureSetup.MeasureMode == MeasureModes.MultipleCursor)
                        {
                            return !measureSetup.Cursors.Contains(item.Value.Cursor);
                        }

                        if (!item.Value.IsDebris)
                        {
                            return !measureSetup.Cursors.Contains(item.Value.Cursor);
                        }
                        return false;
                    }).ToArray();

                    foreach (var toRemove in toRemoves)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            measureResultDataViewModel.CursorResultDataViewModels.Remove(toRemove.Value);
                            measureResultDataViewModel.CursorResultDataViewModelExports.Remove(toRemove);
                            _compositionFactory.ReleaseExport(toRemove);
                        });
                    }

                    measureResultDataViewModel.TotalCountsPerMl = totalCountsPerMl.ToString("0.000E+00");
                    measureResultDataViewModel.TotalVolPerMl = $"{totalVolPerMl:0.000E+00} fl";
                    measureResultDataViewModel.HasSubpopulations = measureSetup.HasSubpopulations && measureSetup.MeasureMode == MeasureModes.MultipleCursor;


                }
            }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                //Ignore

            }
            SlowStuffSemaphore.Release();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls
        private bool _isViablityMode;

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this._measureResultManager.SelectedMeasureResultDataChangedEvent -= OnSelectedMeasureResultDataChanged;

                    if (this.MeasureResultDataViewModelExports != null)
                    {
                        foreach(var viewModelExport in MeasureResultDataViewModelExports)
                        {
                            this._compositionFactory.ReleaseExport(viewModelExport);
                        }
                    }
                }

                this.MeasureResultDataViewModelExports?.Clear();

                disposedValue = true;
            }
            base.Dispose(disposing);
        }
        #endregion
    }
}
