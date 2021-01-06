using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Models;
using OLS.Casy.Models.Enums;
using OLS.Casy.Ui.Analyze.Views;
using OLS.Casy.Ui.Core.Api;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace OLS.Casy.Ui.Analyze.ViewModels
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(AnalyzeOverlayChartViewModel))]
    public class AnalyzeOverlayChartViewModel : AnalyzeChartViewModelBase
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IUIProjectManager _uiProjectManager;
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly ILocalizationService _localizationService;

        private bool _areRangesEqual = true;

        private string _invalidParameters;
        private MeasureSetup _overlayMeasureSetup;

        [ImportingConstructor]
        public AnalyzeOverlayChartViewModel(
            IMeasureResultManager measureResultManager,
            //Lazy<IMeasureResultContainerViewModel> measureResultContainerViewModel,
            IAuthenticationService authenticationService,
            IUIProjectManager uiProjectManager,
            IEventAggregatorProvider eventAggregatorProvider,
            ILocalizationService localizationService,
            ICompositionFactory compositionFactory
            ) : base(measureResultManager, compositionFactory)
        {
            _authenticationService = authenticationService;
            _uiProjectManager = uiProjectManager;
            _eventAggregatorProvider = eventAggregatorProvider;
            _localizationService = localizationService;
        }

        public IMeasureResultContainerViewModel MeasureResultContainerViewModel => MeasureResultContainers[0];

        public override void OnImportsSatisfied()
        {
            base.OnImportsSatisfied();
            var export = CompositionFactory.GetExport<IMeasureResultContainerViewModel>();
            AddMeasureResultContainer(new Tuple<Lazy<IMeasureResultContainerViewModel>, IMeasureResultContainerViewModel>(export, export.Value));
            MeasureResultContainerViewModel.IsSaveVisible = false;
            MeasureResultContainerViewModel.IsOverlayMode = true;
            _eventAggregatorProvider.Instance.GetEvent<MeasureResultsDeletedEvent>().Subscribe(OnMeasureResultsDeleted);
        }

        private void OnMeasureResultsDeleted()
        {
            OnIsActiveChanged();
        }

        protected override async void OnIsActiveChanged()
        {
            if (IsActive)
            {
                await UpdateOverlayTemplate();
                //await ShowInvalidMessageBox();
                //await ShowSelectRangeParent();

                if (_overlayMeasureSetup != null)
                {
                    MeasureResultContainerViewModel.AddMeasureResults(MeasureResultManager.SelectedMeasureResults.Where(mr => mr.IsVisible && !mr.IsDeletedResult).ToArray());
                }
                MeasureResultManager.SelectedMeasureResultsChanged += OnSelectedMeasureResultsChanged;
            }
            else
            {
                MeasureResultManager.SelectedMeasureResultsChanged -= OnSelectedMeasureResultsChanged;

                MeasureResultContainerViewModel.ClearMeasureResults();
                try
                {
                    lock (((ICollection) MeasureResultManager.SelectedMeasureResults).SyncRoot)
                    {
                        foreach (var measureResult in MeasureResultManager.SelectedMeasureResults)
                        {
                            measureResult.PropertyChanged -= OnPropertyChanged;
                        }
                    }
                }
                catch
                {
                    //Ignored
                }
            }
        }

        protected void OnSelectedMeasureResultsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (IsActive)
            {
                Task.Factory.StartNew(async () =>
                {
                    if (e.OldItems != null)
                    {
                        foreach (var oldItem in e.OldItems.OfType<MeasureResult>())
                        {
                            MeasureResultContainerViewModel.RemoveMeasureResult(oldItem);
                            oldItem.PropertyChanged -= OnPropertyChanged;
                        }
                    }

                    await UpdateOverlayTemplate(e.OldItems != null);

                    if (e.NewItems != null)
                    {
                        var newItems = e.NewItems.OfType<MeasureResult>().ToArray();
                        MeasureResultContainerViewModel.AddMeasureResults(newItems);
                        foreach (var newItem in newItems)
                        {
                            newItem.PropertyChanged += OnPropertyChanged;
                        }
                    }
                });
            }
        }

        private async void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!IsActive) return;
            
            if (e.PropertyName == "IsVisible")
            {
                await UpdateOverlayTemplate();
            }
        }

        private async Task UpdateOverlayTemplate(bool haveRemovedItems = false)
        {
            if (_authenticationService.LoggedInUser == null)
            {
                return;
            }

            MeasureResultContainerViewModel.IsApplyToParentsAvailable = false;
            if (MeasureResultManager.OverlayMeasureSetup != null)
            {
                _uiProjectManager.Clear();
            }

            _overlayMeasureSetup = new MeasureSetup();

            var measureResults = MeasureResultManager.SelectedMeasureResults.Where(mr => mr.IsVisible).ToArray();
            if (measureResults.Length > 0)
            {
                _invalidParameters = string.Empty;

                var toDiameters = measureResults.Select(mr => mr.MeasureSetup.ToDiameter).Distinct().ToArray();
                if (toDiameters.Length > 1)
                {
                    _invalidParameters +=
                        $"- {_localizationService.GetLocalizedString("AnnotationType_ToDiameter_Header")}\n";
                    _overlayMeasureSetup = null;
                }

                var fromDiameters = measureResults.Select(mr => mr.MeasureSetup.FromDiameter).Distinct().ToArray();
                if (fromDiameters.Length > 1)
                {
                    _invalidParameters +=
                        $"- {_localizationService.GetLocalizedString("AnnotationType_FromDiameter_Header")}\n";
                    _overlayMeasureSetup = null;
                }
                
                //Kapillare
                var capillaries = measureResults.Select(mr => mr.MeasureSetup.CapillarySize).Distinct().ToArray();
                if (capillaries.Length > 1)
                {
                    _invalidParameters +=
                        $"- {_localizationService.GetLocalizedString("AnnotationType_CapillarySize_Header")}\n";
                    _overlayMeasureSetup = null;
                }

                if (_invalidParameters.Any())
                {
                    await ShowInvalidMessageBox();
                }

                if (_overlayMeasureSetup != null && _authenticationService.LoggedInUser != null)
                {
                    _overlayMeasureSetup.ChannelCount = measureResults.Max(mr => mr.MeasureSetup.ChannelCount);

                    var now = DateTime.Now;
                    _overlayMeasureSetup.CreatedBy =
                        $"{_authenticationService.LoggedInUser.FirstName} {_authenticationService.LoggedInUser.LastName} ({_authenticationService.LoggedInUser.Identity.Name})";
                    _overlayMeasureSetup.CreatedAt = now;

                    _overlayMeasureSetup.ToDiameter = toDiameters[0];
                    _overlayMeasureSetup.FromDiameter = fromDiameters[0];
                    _overlayMeasureSetup.CapillarySize = capillaries[0];
                    _overlayMeasureSetup.Repeats = measureResults.Max(mr => mr.MeasureSetup.Repeats);

                    var volumes = measureResults.Select(mr => mr.MeasureSetup.Volume).Distinct().ToArray();
                    _overlayMeasureSetup.Volume = volumes.Length > 1 ? Volumes.TwoHundred : volumes[0];
                    _overlayMeasureSetup.VolumeCorrectionFactor = measureResults[0].MeasureSetup.VolumeCorrectionFactor;

                    var measureModes = measureResults.Select(mr => mr.MeasureSetup.MeasureMode).Distinct().ToArray();
                    _overlayMeasureSetup.MeasureMode = measureModes.Length > 1 ? MeasureModes.MultipleCursor : measureModes[0];

                    var unitModes = measureResults.Select(mr => mr.MeasureSetup.UnitMode).Distinct().ToArray();
                    _overlayMeasureSetup.UnitMode = unitModes.Length > 1 ? UnitModes.Counts : unitModes[0];

                    _overlayMeasureSetup.DilutionFactor = measureResults[0].MeasureSetup.DilutionFactor;

                    _overlayMeasureSetup.IsSmoothing = measureResults.All(mr => mr.MeasureSetup.IsSmoothing);
                    if (_overlayMeasureSetup.IsSmoothing)
                    {
                        var smoothingFactors = measureResults.Select(mr => mr.MeasureSetup.SmoothingFactor).Distinct().ToArray();
                        _overlayMeasureSetup.SmoothingFactor = smoothingFactors.Length > 1 ? 0d : smoothingFactors[0];
                    }

                    var scalingModes = measureResults.Select(mr => mr.MeasureSetup.ScalingMode).Distinct().ToArray();
                    _overlayMeasureSetup.ScalingMode = scalingModes.Length > 1 ? ScalingModes.Auto : scalingModes[0];

                    if (_overlayMeasureSetup.ScalingMode == ScalingModes.MaxRange)
                    {
                        var scalingMaxRanges = measureResults.Select(mr => mr.MeasureSetup.ScalingMaxRange).Distinct().ToArray();
                        _overlayMeasureSetup.ScalingMaxRange = scalingMaxRanges.Length > 1 ? scalingMaxRanges.Max() : scalingMaxRanges[0];
                    }

                    var aggregationCalculationModes = measureResults.Select(mr => mr.MeasureSetup.AggregationCalculationMode).Distinct().ToArray();
                    _overlayMeasureSetup.AggregationCalculationMode = aggregationCalculationModes.Length > 1 ? AggregationCalculationModes.FromParent : aggregationCalculationModes[0];

                    if (_overlayMeasureSetup.AggregationCalculationMode == AggregationCalculationModes.Manual)
                    {
                        var aggregationCalculationManualValues = measureResults.Select(mr => mr.MeasureSetup.ManualAggregationCalculationFactor).Distinct().ToArray();
                        if (aggregationCalculationManualValues.Length > 1)
                        {
                            _overlayMeasureSetup.AggregationCalculationMode = AggregationCalculationModes.FromParent;
                        }
                        else
                        {
                            _overlayMeasureSetup.ManualAggregationCalculationFactor = aggregationCalculationManualValues[0];
                        }
                    }

                    var cursors = measureResults[0].MeasureSetup.Cursors.ToArray();

                    _areRangesEqual = measureResults.Select(mr => mr.MeasureSetup.Cursors.Count).Distinct().Count() == 1;

                    if (_areRangesEqual)
                    {
                        foreach (var measureResult in measureResults)
                        {
                            if (measureResult != measureResults[0])
                            {
                                foreach (var cursor in measureResult.MeasureSetup.Cursors)
                                {
                                    _areRangesEqual &= cursors.Any(c => c.Name == cursor.Name && c.MaxLimit == cursor.MaxLimit && c.MinLimit == cursor.MinLimit);
                                    if (!_areRangesEqual)
                                    {
                                        break;
                                    }
                                }
                            }
                            if (!_areRangesEqual)
                            {
                                break;
                            }
                        }
                    }

                    if (_areRangesEqual)
                    {
                        _overlayMeasureSetup.ClearCursors();
                        foreach (var cursor in cursors)
                        {
                            _overlayMeasureSetup.AddCursor(new Cursor
                            {
                                Name = cursor.Name,
                                MinLimit = cursor.MinLimit,
                                MaxLimit = cursor.MaxLimit,
                                IsDeadCellsCursor = cursor.IsDeadCellsCursor,
                                Color = cursor.Color,
                                MeasureSetup = _overlayMeasureSetup
                            });
                        }
                    }
                    else// if (IsActive && haveRemovedItems)
                    {
                        await ShowSelectRangeParent();
                    }
                }
            }

            if (MeasureResultContainerViewModel != null)
            {
                MeasureResultContainerViewModel.MeasureSetup = _overlayMeasureSetup;
                MeasureResultContainerViewModel.IsApplyToParentsAvailable = true;

                var isOperator = _authenticationService.LoggedInUser.UserRole.Priority == 2;
                var isSupervisor = _authenticationService.LoggedInUser.UserRole.Priority == 3;
                var userId = _authenticationService.LoggedInUser.Id;
                var groupIds = _authenticationService.LoggedInUser.UserGroups.Select(g => g.Id);

                MeasureResultContainerViewModel.CanApplyToParents = isSupervisor || isOperator && measureResults.All(mr =>
                                                                        mr.AccessMappings.Count == 0 ||
                                                                        mr.AccessMappings.Any(am =>
                                                                            am.CanWrite &&
                                                                            am.UserId.HasValue &&
                                                                            am.UserId.Value == userId) ||
                                                                        mr.AccessMappings
                                                                            .Where(x =>
                                                                                x.CanWrite &&
                                                                                x.UserGroupId.HasValue)
                                                                            .Select(x => x.UserGroupId.Value)
                                                                            .Intersect(groupIds).Any());
                MeasureResultManager.OverlayMeasureSetup = _overlayMeasureSetup;
            }
        }
        private async Task ShowInvalidMessageBox()
        {
            if (!string.IsNullOrEmpty(_invalidParameters))
            {
                await Task.Factory.StartNew(() =>
                {
                    var awaiter = new ManualResetEvent(false);

                    var messageBoxDialogWrapper = new ShowMessageBoxDialogWrapper()
                    {
                        Awaiter = awaiter,
                        Message = "AnalyzeOverlayChartViewModel_InvalidSelectedMeasurementResult_Message",
                        Title = "AnalyzeOverlayChartViewModel_InvalidSelectedMeasurementResult_Title",
                        MessageParameter = new[] { _invalidParameters }
                    };

                    _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>().Publish(messageBoxDialogWrapper);
                    awaiter.WaitOne();
                });
            }
        }

        private async Task ShowSelectRangeParent()
        {
            if (string.IsNullOrEmpty(_invalidParameters) && !_areRangesEqual)
            {
                await Task.Factory.StartNew(() =>
                {
                    var awaiter = new ManualResetEvent(false);

                    var viewModelExport = CompositionFactory.GetExport<SelectRangeParentDialogModel>();
                    var viewModel = viewModelExport.Value;

                    viewModel.RangeOptions = MeasureResultManager.SelectedMeasureResults.Where(mr => mr.IsVisible).Select(mr => new Tuple<string, MeasureResult>(
                        $"{mr.Name} - {(mr.MeasureSetup.MeasureMode == MeasureModes.Viability ? _localizationService.GetLocalizedString("MeasureMode_Viability_Name") : $"{mr.MeasureSetup.Cursors.Count.ToString()} {_localizationService.GetLocalizedString("SelectTemplateView_Label_Ranges")}")}",
                        mr)).ToList();

                    var showCustomDialogWrapper = new ShowCustomDialogWrapper()
                    {
                        Awaiter = awaiter,
                        DialogType = typeof(SelectRangeParentDialog),
                        DataContext = viewModel
                    };
                    _eventAggregatorProvider.Instance.GetEvent<ShowCustomDialogEvent>().Publish(showCustomDialogWrapper);
                    awaiter.WaitOne();

                    if(viewModel.SelectedRangeOption != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if(_overlayMeasureSetup.MeasureMode != viewModel.SelectedRangeOption.MeasureSetup.MeasureMode)
                            {
                                _overlayMeasureSetup.MeasureMode = viewModel.SelectedRangeOption.MeasureSetup.MeasureMode;
                            }

                            _overlayMeasureSetup.ClearCursors();
                            foreach (var cursor in viewModel.SelectedRangeOption.MeasureSetup.Cursors)
                            {
                                _overlayMeasureSetup.AddCursor(new Cursor
                                {
                                    Name = cursor.Name,
                                    MinLimit = cursor.MinLimit,
                                    MaxLimit = cursor.MaxLimit,
                                    IsDeadCellsCursor = cursor.IsDeadCellsCursor,
                                    Color = cursor.Color,
                                    MeasureSetup = _overlayMeasureSetup
                                });
                           }

                            _areRangesEqual = true;
                        });
                    }

                    CompositionFactory.ReleaseExport(viewModelExport);
                });
            }
        }
    }
}
