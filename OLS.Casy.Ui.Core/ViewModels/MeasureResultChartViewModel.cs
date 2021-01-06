using DevExpress.Xpf.Charts;
using OLS.Casy.Calculation.Api;
using OLS.Casy.Core;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Models;
using OLS.Casy.Models.Enums;
using OLS.Casy.Ui.Authorization.Api;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Base.Models;
using OLS.Casy.Ui.Base.ViewModels;
using OLS.Casy.Ui.Core.Api;
using OLS.Casy.Ui.Core.Helper;
using OLS.Casy.Ui.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using OLS.Casy.Base;
using OLS.Casy.Core.Logging.Api;
using OLS.Casy.Ui.Core.UndoRedo;
using Cursor = OLS.Casy.Models.Cursor;

namespace OLS.Casy.Ui.Core.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(MeasureResultChartViewModel))]
    public sealed class MeasureResultChartViewModel : ValidationViewModelBase, IMirrorListConversor<Lazy<ChartCursorViewModel>, Cursor>, IPartImportsSatisfiedNotification
    {
        private readonly ILocalizationService _localizationService;
        private readonly ICompositionFactory _compositionFactory;
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly IUIProjectManager _uiProject;
        private readonly ISmootingCalculationProvider _smoothingCalculationProvider;
        private readonly IVolumeCalculationProvider _volumeCalculationProvider;
        private readonly IMeasureResultManager _measureResultManager;
        private readonly IAuthenticationService _authenticationService;
        private readonly IAccessManager _accessManager;
        private readonly IEnvironmentService _environmentService;
        private readonly EnumWrapperProviderHelper _enumWrapperProviderHelper;
        private readonly ILogger _logger;

        private ChartDataItemModel<double, double>[][] _measureResultData;

        private string _displayName;
        private bool _isShowVolumeChecked;

        private double[][] _chartDataSets;
        private MeasureSetup _measureSetup;

        private MirrorList<Lazy<ChartCursorViewModel>, Cursor> _cursorViewModels;
        private List<ChartColorModel> _chartColors;
        private string[] _seriesDescriptions;
        private bool _suppressCursorModificationEvents;

        private bool _isAutoRange = true;
        private double _maxRange;
        private double _minXValue;
        private double _maxXValue;

        private byte[] _capturedImage;

        private string _smoothingFactorDisplay;
        private volatile bool _refreshOverlays = true;
        private bool _isReadOnly;

        private bool _isNormalized;

        private bool? _rememberIsAutoRange;
        private double? _rememberMaxRange;

        private bool _ignoreRecalculateSampleVolume;
        
        private bool _disposedValue; // To detect redundant calls
        private MeasureSetup _doCaptureImage;
        private bool _forceUpdate;
        private FileInfo _fileInfo;
        private bool _ignoreRecalculateDilutionFactor;
        private bool _isOverlayMode;
        private bool _isButtonMenuCollapsed;

        private System.Timers.Timer _timer;
        private bool _notDrawed = true;
        private readonly object _lock = new object();

        [ImportingConstructor]
        public MeasureResultChartViewModel(ILocalizationService localizationService,
            ICompositionFactory compositionFactory,
            IEventAggregatorProvider eventAggregatorProvider,
            IUIProjectManager uiProject,
            ISmootingCalculationProvider smoothingCalculationProvider,
            IVolumeCalculationProvider volumeCalculationProvider,
            IMeasureResultManager measureResultManager,
            IAuthenticationService authenticationService,
            IEnvironmentService environmentService,
            EnumWrapperProviderHelper enumWrapperProviderHelper,
            ILogger logger,
            [Import(AllowDefault = true)] IAccessManager accessManager)
        {
            _localizationService = localizationService;
            _eventAggregatorProvider = eventAggregatorProvider;
            _compositionFactory = compositionFactory;
            _uiProject = uiProject;
            _smoothingCalculationProvider = smoothingCalculationProvider;
            _volumeCalculationProvider = volumeCalculationProvider;
            _measureResultManager = measureResultManager;
            _authenticationService = authenticationService;
            _accessManager = accessManager;
            _environmentService = environmentService;
            _enumWrapperProviderHelper = enumWrapperProviderHelper;
            _logger = logger;

            ChartOverlayViewModels = new SmartCollection<IChartOverlayViewModel>();
            AggregationCalculationModes = new SmartCollection<ComboBoxItemWrapperViewModel<AggregationCalculationModes>>();
        }

        public bool IsAutoRange
        {
            get => _isAutoRange;
            set
            {
                _isAutoRange = value;
                NotifyOfPropertyChange();
                
                if (!value)
                {
                    NotifyOfPropertyChange("MaxRange");   
                }
            }
        }

        public bool ShowMouseOverInGraph
        {
            get => _showMouseOverInGraph;
            set
            {
                _showMouseOverInGraph = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsMultiChartGraph
        {
            get
            {
                lock (_chartDataSets.SyncRoot)
                {
                    return _chartDataSets != null && _chartDataSets.Length > 1;
                }
            }
        }

        public bool IsOverlayMode
        {
            get => _isOverlayMode;
            set
            {
                if (value == _isOverlayMode) return;
                _isOverlayMode = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange("CanEditAccess");
                NotifyOfPropertyChange("HasEditRights");

                if(_isOverlayMode)
                {
                    AggregationCalculationModes.Add(new ComboBoxItemWrapperViewModel<AggregationCalculationModes>(Casy.Models.Enums.AggregationCalculationModes.FromParent)
                    {
                        DisplayItem = _localizationService.GetLocalizedString(
                            $"AggregationCalculationMode_{Enum.GetName(typeof(AggregationCalculationModes), Casy.Models.Enums.AggregationCalculationModes.FromParent)}_Name")
                    });
                }
            }
        }

        public void CaptureImage(FileInfo fileInfo = null)
        {
            FileInfo = fileInfo;
            DoCaptureImage = _measureSetup;
        }

        public MeasureSetup DoCaptureImage
        {
            get => _doCaptureImage;
            set
            {
                if (value == _doCaptureImage) return;
                _doCaptureImage = value;
                NotifyOfPropertyChange();
            }
        }

        public FileInfo FileInfo
        {
            get => _fileInfo;
            set
            {
                if (value == _fileInfo) return;
                _fileInfo = value;
                NotifyOfPropertyChange();
            }
        }

        public byte[] CapturedImage
        {
            get => _capturedImage;
            set
            {
                _capturedImage = value;
                ChartImageCapturesEvent?.Invoke(this, EventArgs.Empty);
                DoCaptureImage = null;
            }
        }

        public event EventHandler ChartImageCapturesEvent;

        public double MaxRange
        {
            get => _maxRange;
            set
            {
                if (value == _maxRange) return;
                
                _maxRange = value;
                NotifyOfPropertyChange();
            }
        }

        public double MinXValue
        {
            get => _minXValue;
            set
            {
                _minXValue = value;
                NotifyOfPropertyChange();
            }
        }

        public double MaxXValue
        {
            get => _maxXValue;
            set
            {
                _maxXValue = value;
                NotifyOfPropertyChange();
            }
        }

        public IEnumerable<ChartDataItemModel<double, double>> MeasureResultData
        {
            get
            {
                
                if (_measureResultData == null || _measureResultData.Length == 0 || _measureResultData[0] == null)
                {
                    return new ChartDataItemModel<double, double>[0];
                }

                lock (_measureResultData.SyncRoot)
                {
                    var measureResultDataCount = _measureResultData.Length;

                    var result = new ChartDataItemModel<double, double>[_measureResultData.Sum(mrd => mrd.Length)];

                    var processed = 0;
                    for (var i = 0; i < measureResultDataCount; i++)
                    {
                        var array = _measureResultData[i];
                        Array.Copy(array, 0, result, processed, array.Length);
                        processed += array.Length;
                    }

                    return result;
                }
            }
        }

        public string DisplayName
        {
            get => _displayName;
            set
            {
                if (value == _displayName) return;
                _displayName = value;
                NotifyOfPropertyChange();
            }
        }

        public string XAxisTitle => _localizationService.GetLocalizedString("MeasureResultChartView_XAxis_Label");

        public string YAxisTitle
        {
            get
            {
                var title = _localizationService.GetLocalizedString(_isShowVolumeChecked ? "MeasureResultChartView_YAxis_Label_Volume" : "MeasureResultChartView_YAxis_Label_Counts");
                if(_isNormalized)
                {
                    title = title + " %";
                }
                return title;
            }
        }

        public bool IsNormalized
        {
            get => _isNormalized;
            set
            {
                if (value == _isNormalized) return;
                _isNormalized = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange("YAxisTitle");
                UpdateChartItemModels();
            }
        }

        public bool CanNormalize => IsCountUnitMode;

        public bool HasSubpopulations
        {
            get => _measureSetup.HasSubpopulations;
            set
            {
                if (value == _measureSetup.HasSubpopulations) return;
                _measureSetup.HasSubpopulations = value;
                NotifyOfPropertyChange();
            }
        }

        public bool CanHasSubpopulations => IsMulticursorMode;

        public SmartCollection<ComboBoxItemWrapperViewModel<AggregationCalculationModes>> AggregationCalculationModes { get; }

        public AggregationCalculationModes AggregationCalculationMode
        {
            get => _measureSetup?.AggregationCalculationMode ?? Casy.Models.Enums.AggregationCalculationModes.Off;
            set
            {
                if (value == _measureSetup.AggregationCalculationMode) return;
                _uiProject.SendUIEdit(_measureSetup, "AggregationCalculationMode", value);
                NotifyOfPropertyChange("IsManualAggregationCalculationMode");
            }
        }

        public bool IsManualAggregationCalculationMode => _measureSetup != null && _measureSetup.AggregationCalculationMode == Casy.Models.Enums.AggregationCalculationModes.Manual;

        public double ManualAggregationCalculationFactor
        {
            get => _measureSetup?.ManualAggregationCalculationFactor ?? 0d;
            set
            {
                if (value != _measureSetup.ManualAggregationCalculationFactor)
                {
                    _uiProject.SendUIEdit(_measureSetup, "ManualAggregationCalculationFactor", value);
                }
            }
        }

        public bool IsMulticursorMode
        {
            get => _measureSetup != null && _measureSetup.MeasureMode == MeasureModes.MultipleCursor;
            set
            {
                if (_measureSetup.MeasureMode == MeasureModes.MultipleCursor) return;
                _uiProject.StartUndoGroup();
                _uiProject.SendUIEdit(_measureSetup, "MeasureMode", MeasureModes.MultipleCursor);
                //if(_measureSetup.AggregationCalculationMode != Casy.Models.Enums.AggregationCalculationModes.Off)
                //{
                //_uiProject.SendUIEdit(_measureSetup, "AggregationCalculationMode", Casy.Models.Enums.AggregationCalculationModes.Off);
                //}
                NotifyOfPropertyChange();
                NotifyOfPropertyChange("IsViabilityMode");
                NotifyOfPropertyChange("CanAddRange");
                NotifyOfPropertyChange("IsNotReadOnly");
                NotifyOfPropertyChange("CanHasSubpopulations");
                CreateDefaultCursor();
                _uiProject.SubmitUndoGroup();
            }
        }

        public bool IsViabilityMode
        {
            get => _measureSetup != null && _measureSetup.MeasureMode == MeasureModes.Viability;
            set
            {
                if (_measureSetup.MeasureMode == MeasureModes.Viability) return;
                _uiProject.StartUndoGroup();
                _uiProject.SendUIEdit(_measureSetup, "MeasureMode", MeasureModes.Viability);
                NotifyOfPropertyChange();
                NotifyOfPropertyChange("IsMulticursorMode");
                HasSubpopulations = false;
                NotifyOfPropertyChange("CanHasSubpopulations");
                CreateDefaultCursor();
                _uiProject.SubmitUndoGroup();
            }
        }

        public bool IsScalingModeAuto
        {
            get => _measureSetup != null && _measureSetup.ScalingMode == ScalingModes.Auto;
            set
            {
                if (!value || _measureSetup.ScalingMode == ScalingModes.Auto) return;
                _uiProject.SendUIEdit(_measureSetup, "ScalingMode", ScalingModes.Auto);
                _isAutoRange = false;
                IsAutoRange = true;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange("IsScalingModeMaxRange");
            }
        }

        public bool IsScalingModeMaxRange
        {
            get => _measureSetup != null && _measureSetup.ScalingMode == ScalingModes.MaxRange;
            set
            {
                if (!value || _measureSetup.ScalingMode == ScalingModes.MaxRange) return;
                _uiProject.SendUIEdit(_measureSetup, "ScalingMode", ScalingModes.MaxRange);
                NotifyOfPropertyChange();
                NotifyOfPropertyChange("IsScalingModeAuto");
            }
        }

        private string _scalingMaxRangeDisplay;
        private bool _showMouseOverInGraph;

        [RegularExpression("[0-9]+")]
        public string ScalingMaxRange
        {
            get => _scalingMaxRangeDisplay ?? (_measureSetup == null ? "0" : _measureSetup.ScalingMaxRange.ToString());
            set
            {
                if (value == _measureSetup.ScalingMaxRange.ToString()) return;
                if (int.TryParse(value, out var newValue))
                {
                    _scalingMaxRangeDisplay = null;
                    _uiProject.SendUIEdit(_measureSetup, "ScalingMaxRange", newValue);
                }
                else
                {
                    _scalingMaxRangeDisplay = value;
                }
            }
        }

        public bool IsVolumeUnitMode
        {
            get => _measureSetup != null && _measureSetup.UnitMode == UnitModes.Volume;
            set
            {
                if (_measureSetup.UnitMode == UnitModes.Volume) return;
                _uiProject.StartUndoGroup();
                _uiProject.SendUIEdit(_measureSetup, "UnitMode", UnitModes.Volume);
                _uiProject.SendUIEdit(_measureSetup, "ScalingMode", ScalingModes.Auto);
                _uiProject.SubmitUndoGroup();
                IsAutoRange = true;
                IsNormalized = false;

                NotifyOfPropertyChange();
                NotifyOfPropertyChange("IsCountUnitMode");
                NotifyOfPropertyChange("SmoothingFactor");
                NotifyOfPropertyChange("CanNormalize");
                NotifyOfPropertyChange("IsScalingModeAuto");
            }
        }

        public bool IsCountUnitMode
        {
            get => _measureSetup != null && _measureSetup.UnitMode == UnitModes.Counts;
            set
            {
                if (_measureSetup.UnitMode == UnitModes.Counts) return;
                _uiProject.SendUIEdit(_measureSetup, "UnitMode", UnitModes.Counts);
                NotifyOfPropertyChange();
                NotifyOfPropertyChange("IsVolumeUnitMode");
                NotifyOfPropertyChange("CanNormalize");
            }
        }

        public int SmoothingFactor
        {
            get => _measureSetup == null || !_measureSetup.IsSmoothing ? 0 : (int) _measureSetup.SmoothingFactor;
            set
            {
                var doubleValue = Convert.ToDouble(value);
                if (doubleValue == _measureSetup.SmoothingFactor) return;
                if (doubleValue == 0d)
                {
                    if (_measureSetup.IsSmoothing)
                    {
                        _uiProject.SendUIEdit(_measureSetup, "IsSmoothing", false);
                        _uiProject.SendUIEdit(_measureSetup, "SmoothingFactor", 0d);
                    }
                    SmoothingFactorDisplay = _localizationService.GetLocalizedString("MeasureResultChartView_SmoothingFactor_Off");
                }
                else
                {
                    if (!_measureSetup.IsSmoothing)
                    {
                        _uiProject.SendUIEdit(_measureSetup, "IsSmoothing", true);
                    }
                    _uiProject.SendUIEdit(_measureSetup, "SmoothingFactor", doubleValue);
                    SmoothingFactorDisplay = $"x{value}";
                }
            }
        }

        public string SmoothingFactorDisplay
        {
            get => _smoothingFactorDisplay;
            set
            {
                if (value == _smoothingFactorDisplay) return;
                _smoothingFactorDisplay = value;
                NotifyOfPropertyChange();
            }
        }

        [RegularExpression("^[0-9]+([.,][0-9]+)?$")]
        public double DilutionFactor
        {
            get => _measureSetup?.DilutionFactor ?? 0d;
            set
            {
                if (value == _measureSetup.DilutionFactor) return;
                _uiProject.SendUIEdit(_measureSetup, "DilutionFactor", value);
                _ignoreRecalculateDilutionFactor = true;
                CalcDilutionSampleVolume();
                _ignoreRecalculateDilutionFactor = false;
            }
        }

        [RegularExpression("^[0-9]+([.,][0-9]+)?$")]
        public double DilutionSampleVolume
        {
            get => _measureSetup?.DilutionSampleVolume ?? 1d;
            set
            {
                if (value == _measureSetup.DilutionSampleVolume) return;
                _uiProject.SendUIEdit(_measureSetup, "DilutionSampleVolume", value);
                _ignoreRecalculateSampleVolume = true;
                CalcDilutionFactor();
                _ignoreRecalculateSampleVolume = false;
                NotifyOfPropertyChange("DilutionFactor");
            }
        }

        [RegularExpression("[0-9]+")]
        public double DilutionCasyTonVolume
        {
            get => _measureSetup?.DilutionCasyTonVolume ?? 0d;
            set
            {
                if (value == _measureSetup.DilutionCasyTonVolume) return;
                _uiProject.SendUIEdit(_measureSetup, "DilutionCasyTonVolume", value);
                _ignoreRecalculateSampleVolume = true;
                CalcDilutionFactor();
                _ignoreRecalculateSampleVolume = false;
                NotifyOfPropertyChange("DilutionFactor");
            }
        }

        public List<ChartColorModel> ChartColors
        {
            get
            {
                // Exclude mother curves in mean view
                return _chartColors.Where(color => color.ChartThickness == 3).ToList();
            }
        }

        public ICommand OnBoundDataChangedCommand => new OmniDelegateCommand<EventInformation<RoutedEventArgs>>(OnBoundDataChanged);

        public ICommand OnZoomCommand => new OmniDelegateCommand<EventInformation<XYDiagram2DZoomEventArgs>>(OnZoom);

        public ICommand OnScrollCommand => new OmniDelegateCommand<EventInformation<XYDiagram2DScrollEventArgs>>(OnScroll);

        public ICommand MouseLeftButtonDownCommand => new OmniDelegateCommand<EventInformation<MouseButtonEventArgs>>(OnMouseLeftButtonDown);

        private static void OnMouseLeftButtonDown(EventInformation<MouseButtonEventArgs> obj)
        {
            if (!(obj.Sender is ChartControl chart)) return;
            
            chart.CrosshairEnabled = true;
            ((XYDiagram2D) chart.Diagram).ShowCrosshair(obj.EventArgs.GetPosition(chart));

            var timer = new DispatcherTimer(DispatcherPriority.Render) {Interval = TimeSpan.FromMilliseconds(1000)};
            timer.Tick += (s, e) =>
            {
                chart.CrosshairEnabled = false;
                //((XYDiagram2D)chart.Diagram).ShowCrosshair(new Point(0,0));
                timer.IsEnabled = false;
            };
            timer.IsEnabled = false;
        }

        public ICommand TouchDownCommand => new OmniDelegateCommand<EventInformation<TouchEventArgs>>(OnTouchDown);

        private static void OnTouchDown(EventInformation<TouchEventArgs> obj)
        {
            if (!(obj.Sender is ChartControl chart)) return;
            ((XYDiagram2D) chart.Diagram).ShowCrosshair(obj.EventArgs.GetTouchPoint(chart).Position);

            var timer = new DispatcherTimer(DispatcherPriority.Render) {Interval = TimeSpan.FromMilliseconds(4000)};
            timer.Tick += (s, e) =>
            {
                ((XYDiagram2D) chart.Diagram).ShowCrosshair(new Point(0, 0));
                timer.IsEnabled = false;
            };
            timer.IsEnabled = false;
        }

        public bool CanAddRange
        {
            get
            {
                if (_cursorViewModels == null)
                {
                    return false;
                }

                //lock (_cursorViewModels.SyncRoot)
                //{
                    return !_isReadOnly && IsMulticursorMode &&
                           _authenticationService.LoggedInUser.UserRole.Priority >=2 &&
                           _cursorViewModels.Count < 5 &&
                           _cursorViewModels.All(cvm => cvm.Value.MaxLimit < _measureSetup.ToDiameter) && HasEditRights;
                //}
            }
        }

        public bool IsNotReadOnly => !_isReadOnly;

        public ICommand AddRangeCommand => new OmniDelegateCommand(OnAddRange);

        public bool IsShowVolumeChecked
        {
            get => _isShowVolumeChecked;
            set
            {
                if (value == _isShowVolumeChecked) return;
                _isShowVolumeChecked = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange("YAxisTitle");
            }
        }

        public Lazy<ChartCursorViewModel> CreateViewItem(Casy.Models.Cursor modelItem, int index)
        {
            try
            {
                var viewModelExport = _compositionFactory.GetExport<ChartCursorViewModel>();
                viewModelExport.Value.Cursor = modelItem;
                viewModelExport.Value.Color = modelItem.Color;
                return viewModelExport;
            }
            catch (Exception e)
            {
                _logger.Error(LogCategory.General, "An Error occured while Lazy creation of cursor item",
                    () => CreateViewItem(modelItem, index), e);
            }

            return null;
        }

        public Casy.Models.Cursor GetModelItem(Lazy<ChartCursorViewModel> viewItem, int index)
        {
            return viewItem.Value.Cursor;
        }

        public SmartCollection<IChartOverlayViewModel> ChartOverlayViewModels { get; }

        public void AddRange(double initLimit, double initMaxLimit = -1d)
        {
            var run = true;
            var nextNumber = 1;
            while(run)
            {
                lock (_cursorViewModels.SyncRoot)
                {
                    if (_cursorViewModels.Any(cvm => cvm.Value.Name == $"Range {nextNumber}"))
                    {
                        nextNumber++;
                    }
                    else
                    {
                        run = false;
                    }
                }
            }

            var cursor = new Casy.Models.Cursor()
            {
                Name = $"Range {nextNumber}",
                MinLimit = initLimit,
                MaxLimit = initMaxLimit == -1d ? initLimit : initMaxLimit,
                MeasureSetup = _measureSetup,
                Color = ((SolidColorBrush)Application.Current.Resources[$"StripBorderColor{nextNumber}"]).Color.ToString()
            };

            lock (_cursorViewModels.SyncRoot)
            {
                _cursorViewModels?.Insert(_cursorViewModels.Count, cursor);
            }
        }

        public bool ForceUpdate
        {
            get => _forceUpdate;
            set
            {
                _forceUpdate = value;
                NotifyOfPropertyChange();
            }
        }

        public bool CanEditAccess => _accessManager != null && !IsOverlayMode && _measureSetup != null && _measureSetup.MeasureResult != _measureResultManager.MeanMeasureResult && _authenticationService.LoggedInUser != null && (_authenticationService.LoggedInUser.UserRole.Priority == 3 || (string)_environmentService.GetEnvironmentInfo("LoggedInUserName") == _measureSetup.MeasureResult.CreatedBy);

        public ICommand EditAccessCommand => new OmniDelegateCommand(OnEditAccess);

        public bool HasEditRights
        {
            get
            {
                if(IsOverlayMode || _accessManager == null)
                {
                    return true;
                }

                if(_authenticationService.LoggedInUser == null || _measureSetup == null || _measureSetup.MeasureResult == null)
                {
                    return false;
                }

                var userId = _authenticationService.LoggedInUser.Id;
                var groupIds = _authenticationService.LoggedInUser.UserGroups.Select(g => g.Id);
                var isSupervisor = _authenticationService.LoggedInUser.UserRole.Priority == 3;

                return  _measureSetup.MeasureResult.AccessMappings.Count == 0 || _measureSetup.MeasureResult == _measureResultManager.MeanMeasureResult || isSupervisor || _measureSetup.MeasureResult.AccessMappings.Any(am => am.CanWrite && am.UserId.HasValue && am.UserId.Value == userId) || _measureSetup.MeasureResult.AccessMappings.Where(x => x.CanWrite && x.UserGroupId.HasValue).Select(x => x.UserGroupId.Value).Intersect(groupIds).Any();
            }
        }

        public void OnImportsSatisfied()
        {
            _localizationService.LanguageChanged += OnLanguageChanged;
            
            var enumWrapper = _enumWrapperProviderHelper.GetAggregationModeWrapper(IsOverlayMode);
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                AggregationCalculationModes.Reset(enumWrapper);
            });
            _eventAggregatorProvider.Instance.GetEvent<DeleteCursorEvent>().Subscribe(OnCursorDeleted);
            _measureResultManager.SelectedMeasureResultDataChangedEvent += OnSelectedMeasureResultDataChanged;

            _timer = new System.Timers.Timer(3000);
            _timer.Elapsed += (sender, args) =>
            {
                if (_notDrawed)
                {
                    this._forceUpdate = false;
                    this.ForceUpdate = true;
                }
                _timer.Stop();
            };
            _timer.AutoReset = false;
            _timer.Start();
        }

        

        private void OnSelectedMeasureResultDataChanged(object sender, MeasureResultDataChangedEventArgs e)
        {
            if(IsNormalized)
            {
                UpdateChartItemModels();
            }
        }

        private void OnCursorDeleted(Tuple<object, object> tuple)
        {
            var (item1, item2) = tuple;
            var measureSetup = item1 as MeasureSetup;
            if (_measureSetup != measureSetup || _cursorViewModels == null) return;
            if (!(item2 is Cursor cursorToDelete)) return;

            lock (_cursorViewModels.SyncRoot)
            {
                var viewModelExportToDelete =
                    _cursorViewModels.FirstOrDefault(item => item.Value != null && item.Value.Cursor == cursorToDelete);

                if (viewModelExportToDelete == null) return;
                _cursorViewModels.Remove(cursorToDelete);
                _compositionFactory.ReleaseExport(viewModelExportToDelete);
            }
        }

        private void OnCursorViewModelsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _refreshOverlays = true;

            if (e.OldItems != null)
            {
                foreach(var viewModel in e.OldItems.OfType<Lazy<ChartCursorViewModel>>())
                {
                    viewModel.Value.PropertyChanged -= OnCursorViewModelPropertyChanged;
                    viewModel.Value.Cursor.PropertyChanged -= OnCursorPropertyChanged;
                }
            }

            if(e.NewItems != null)
            {
                foreach(var viewModel in e.NewItems.OfType<Lazy<ChartCursorViewModel>>())
                {
                    viewModel.Value.PropertyChanged += OnCursorViewModelPropertyChanged;
                    viewModel.Value.Cursor.PropertyChanged += OnCursorPropertyChanged;
                }
            }

            NotifyOfPropertyChange("CanAddRange");
            NotifyOfPropertyChange("IsNotReadOnly");

            //UpdateChartItemModels();
            _forceUpdate = false;
            ForceUpdate = true;
        }

        private void OnCursorPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_suppressCursorModificationEvents) return;
            
            switch (e.PropertyName)
            {
                case "MinLimit":
                case "MaxLimit":
                    _forceUpdate = false;
                    ForceUpdate = true;
                    break;
            }
        }

        private void OnCursorViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!_suppressCursorModificationEvents)
            { 
                switch (e.PropertyName)
                {
                    case "IsVisible":
                        _refreshOverlays = true;
                        UpdateChartItemModels();
                        break;
                }
            }

            NotifyOfPropertyChange("CanAddRange");
            NotifyOfPropertyChange("IsNotReadOnly");
        }
        
        private void OnLanguageChanged(object sender, LocalizationEventArgs e)
        {
            var enumWrapper = _enumWrapperProviderHelper.GetAggregationModeWrapper(IsOverlayMode);
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                AggregationCalculationModes.Reset(enumWrapper);
            });

            NotifyOfPropertyChange("DisplayName");
            NotifyOfPropertyChange("XAxisTitle");
            NotifyOfPropertyChange("YAxisTitle");
        }

        public void SetInitialChartData(MeasureSetup measureSetup, double[][] chartDataSets, IEnumerable<ChartColorModel> chartColors, string[] seriesDescriptions, bool isReadOnly = false, bool showOriginal = false)
        {
            var isNormalizationTemp = IsNormalized;

            _refreshOverlays = true;

            _isReadOnly = isReadOnly;

            if (_cursorViewModels != null)
            {
                lock (_cursorViewModels.SyncRoot)
                {
                    try
                    {
                        foreach (var viewModelExport in _cursorViewModels)
                        {
                            viewModelExport.Value.PropertyChanged -= OnCursorViewModelPropertyChanged;
                            viewModelExport.Value.Cursor.PropertyChanged -= OnCursorPropertyChanged;

                            viewModelExport.Value.Dispose();
                            _compositionFactory.ReleaseExport(viewModelExport);
                        }

                        _cursorViewModels.SubmitUndoItem -= OnSubmitUndoCursor;
                        _cursorViewModels.CollectionChanged -= OnCursorViewModelsChanged;
                    }
                    catch
                    {
                        // ignored
                    }
                }

                _cursorViewModels = null;
            }

            if (_measureSetup != null)
            {
                _measureSetup.PropertyChanged -= OnSetupChanged;
            }

            if (_chartColors != null)
            {
                try
                {
                    foreach (var chartColorModel in _chartColors)
                    {

                        chartColorModel.PropertyChanged -= OnColorChange;
                        chartColorModel.Dispose();
                    }
                }
                catch
                {
                    // ignored
                }
            }

            _measureSetup = measureSetup;

            if (_measureSetup != null)
            {   
                _measureSetup.PropertyChanged += OnSetupChanged;

                _cursorViewModels = new MirrorList<Lazy<ChartCursorViewModel>, Cursor>(_measureSetup.Cursors, this);
                lock (_cursorViewModels.SyncRoot)
                {
                    _cursorViewModels.SubmitUndoItem += OnSubmitUndoCursor;
                    _cursorViewModels.CollectionChanged += OnCursorViewModelsChanged;

                    foreach (var viewModelExport in _cursorViewModels)
                    {
                        if (viewModelExport != null && viewModelExport.IsValueCreated && viewModelExport.Value != null)
                        {
                            viewModelExport.Value.PropertyChanged += OnCursorViewModelPropertyChanged;
                            viewModelExport.Value.Cursor.PropertyChanged += OnCursorPropertyChanged;
                            viewModelExport.Value.IsReadOnly = _isReadOnly || !HasEditRights;
                        }
                    }
                }

                NotifyOfPropertyChange("CursorViewModels");
                NotifyOfPropertyChange("CanAddRange");
                NotifyOfPropertyChange("IsNotReadOnly");

                if (_chartDataSets == null)
                {
                    _chartDataSets = chartDataSets;
                }
                else
                {
                    lock (_chartDataSets.SyncRoot)
                    {
                        _chartDataSets = chartDataSets;
                    }
                }

                _chartColors = new List<ChartColorModel>(chartColors);

                foreach (var chartColorModel in _chartColors)
                {
                    if (chartColorModel != null)
                    {
                        chartColorModel.PropertyChanged += OnColorChange;
                    }
                }

                _seriesDescriptions = seriesDescriptions;
                
            }
            else
            {
                
                _chartDataSets = null;
                _chartColors = null;
                _seriesDescriptions = null;
            }

            NotifyOfPropertyChange("IsMultiChartGraph");
            NotifyOfPropertyChange("IsNotReadOnly");
            NotifyOfPropertyChange("CanEditAccess");
            NotifyOfPropertyChange("HasEditRights");

            lock (_lock)
            {
                //if (_chartDataSets == null)
                //{
                    //_measureResultData = new ChartDataItemModel<double, double>[0][];
                //}
                //else
                //{
                    //lock (_chartDataSets.SyncRoot)
                    //{
                        //_measureResultData = new ChartDataItemModel<double, double>[_chartDataSets.Length][];
                    //}
                //}

                InitChartDataItemModels();
            }

            if (showOriginal || !isNormalizationTemp)
            {
                _isNormalized = true;
                IsNormalized = false;
            }
            else
            {
                _isNormalized = false;
                IsNormalized = true;
            }
        }

        private void OnColorChange(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == "ChartColor")
            {
                InitChartDataItemModels();
            }
        }

        private void OnSubmitUndoCursor(object sender, NotifyCollectionChangedEventArgs info)
        {
            var insCompo = new UICollectionUndoItem(_measureSetup.Cursors)
            {
                ModelObject = _measureSetup, Info = info
            };
            _uiProject.Submit(insCompo);
        }

        private void OnSetupChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!(sender is MeasureSetup)) return;
            
            switch (e.PropertyName)
            {
                case "UnitMode":
                    IsShowVolumeChecked = _measureSetup.UnitMode == UnitModes.Volume;
                    _refreshOverlays = true;
                    InitChartDataItemModels();
                    break;
                case "IsSmoothing":
                case "SmoothingFactor":
                case "FromDiameter":
                case "ToDiameter":
                    UpdateChartItemModels();
                    break;
                case "ScalingMode":
                case "ScalingMaxRange":
                    if (_measureSetup.ScalingMode != ScalingModes.Auto)
                    {
                        MaxRange = _measureSetup.ScalingMaxRange;
                    }
                    else
                    {
                        lock (_chartDataSets.SyncRoot)
                        {
                            MaxRange = _chartDataSets?.Max(ds => ds.Max()) ?? 0;
                        }
                    }
                    IsAutoRange = _measureSetup.ScalingMode == ScalingModes.Auto;
                    InitChartDataItemModels();
                    break;
                case "ChartColor":
                    InitChartDataItemModels();
                    break;
                case "MeasureMode":
                    NotifyOfPropertyChange("CanAddRange");
                    NotifyOfPropertyChange("IsNotReadOnly");
                    break;
            }
        }

        private void InitChartDataItemModels()
        {
            ChartDataItemModel<double, double>[][] measureResultData = new ChartDataItemModel<double, double>[0][]; 
            //else
            //{
                //lock (_chartDataSets.SyncRoot)
                //{
                    //_measureResultData = new ChartDataItemModel<double, double>[_chartDataSets.Length][];
                //}
            //}

            if (_measureSetup?.SmoothedDiameters != null)
            {
                if (_chartDataSets != null)
                {
                    double[][] chartDataSets;
                    List<ChartColorModel> chartColorModels;
                    string[] seriesDescriptions;
                    int toDiameter = _measureSetup.ToDiameter;

                    lock (_chartDataSets.SyncRoot)
                    {
                        chartDataSets = _chartDataSets.ToArray();
                        chartColorModels = _chartColors.ToList();
                        seriesDescriptions = _seriesDescriptions.ToArray();
                        measureResultData = new ChartDataItemModel<double, double>[_chartDataSets.Length][];
                        
                    }
                    
                    //lock (_measureResultData.SyncRoot)
                    //{
                         //= _measureResultData.ToArray();
                    //}

                    for (var chartDataSetIndex = 0; chartDataSetIndex < chartDataSets.Length; chartDataSetIndex++)
                    {
                        if (chartDataSetIndex < measureResultData.Length && 
                            chartDataSetIndex < chartDataSets.Length && 
                            chartDataSets[chartDataSetIndex] != null)
                        {
                            var length = chartDataSets[chartDataSetIndex].Length;
                            measureResultData[chartDataSetIndex] = new ChartDataItemModel<double, double>[length];
                            for (var i = 0; i < length; i++)
                            {
                                var smoothedDiameter = Calculations.CalcSmoothedDiameter(0,
                                    toDiameter, i, length);

                                if (i < measureResultData[chartDataSetIndex].Length)
                                {
                                    measureResultData[chartDataSetIndex][i] =
                                        new ChartDataItemModel<double, double>(
                                            chartDataSetIndex < seriesDescriptions.Length
                                                ? seriesDescriptions[chartDataSetIndex] : string.Empty, smoothedDiameter,
                                            i < chartDataSets[chartDataSetIndex].Length
                                                ? chartDataSets[chartDataSetIndex][i] : 0d,
                                            chartDataSetIndex < chartColorModels.Count &&
                                            chartColorModels[chartDataSetIndex] != null
                                                ? chartColorModels[chartDataSetIndex]?.ChartColor
                                                : "#FF009FE3",
                                            chartDataSetIndex < chartColorModels.Count &&
                                            chartColorModels[chartDataSetIndex] != null
                                                ? chartColorModels[chartDataSetIndex].ChartThickness
                                                : 3);
                                }
                            }
                                
                            //Hier NullReference

                            UpdateChartItemModels(chartDataSets, chartDataSetIndex, measureResultData);
                        }
                    }
                }

                


                MinXValue = _measureResultManager.GetMinMeasureLimit(_measureSetup.CapillarySize);
                MaxXValue = _measureSetup.ToDiameter;

                IsShowVolumeChecked = _measureSetup != null && _measureSetup.UnitMode == UnitModes.Volume;

                //_isAutoRange = _measureSetup != null && _measureSetup.ScalingMode == ScalingModes.Auto;
                //NotifyOfPropertyChange("IsAutoRange");
                //if (!IsAutoRange)
                //{
                //MaxRange = _measureSetup != null && _measureSetup.ScalingMaxRange > 0 ? _measureSetup.ScalingMaxRange : _chartDataSets.Length == 0 ? 0 : _chartDataSets.Max(ds => ds.Max());

                //}

                if (_measureSetup.ScalingMode == ScalingModes.Auto)
                {
                    _isAutoRange = false;
                    _maxRange = _chartDataSets == null || _chartDataSets.All(ds => ds.Length == 0)
                            ? 0
                            : _chartDataSets.Max(ds => ds.Max());
                    IsAutoRange = true;
                }
                else
                {
                    MaxRange = _measureSetup.ScalingMaxRange;
                    _isAutoRange = true;
                    IsAutoRange = false;
                }

                SmoothingFactorDisplay = _measureSetup != null && _measureSetup.IsSmoothing ? SmoothingFactorDisplay =
                    $"x{_measureSetup.SmoothingFactor}"
                : SmoothingFactorDisplay = _localizationService.GetLocalizedString("MeasureResultChartView_SmoothingFactor_Off");
            }

            _measureResultData = measureResultData;

            Application.Current.Dispatcher.BeginInvoke((Action)delegate ()
            {
                NotifyOfPropertyChange("MeasureResultData");
                _forceUpdate = false;
                ForceUpdate = true;
            }, DispatcherPriority.ApplicationIdle);
        }

        private void UpdateChartItemModels()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    if (_chartDataSets != null)
                    {
                        lock (_chartDataSets.SyncRoot)
                        {

                            if (IsNormalized)
                            {
                                //dataBlock = dataBlock.Select(d => d * multiplicator).ToArray();

                                _rememberIsAutoRange = IsAutoRange;
                                _rememberMaxRange = MaxRange;
                                MaxRange = 100;
                                IsAutoRange = false;
                            }
                            else if (_rememberIsAutoRange.HasValue)
                            {
                                IsAutoRange = _rememberIsAutoRange.Value;
                                if (!_isAutoRange)
                                {
                                    if (_rememberMaxRange != null) MaxRange = _rememberMaxRange.Value;
                                }

                                _rememberIsAutoRange = null;
                                _rememberMaxRange = null;
                            }

                            for (var chartDataSetIndex = 0;
                                chartDataSetIndex < _chartDataSets.Length;
                                chartDataSetIndex++)
                            {
                                UpdateChartItemModels(_chartDataSets, chartDataSetIndex, _measureResultData);
                            }
                        }
                    }

                    if (Application.Current.Dispatcher != null)
                        Application.Current.Dispatcher.BeginInvoke(
                            (Action) delegate() { NotifyOfPropertyChange("MeasureResultData"); },
                            DispatcherPriority.ApplicationIdle);
                }
                catch
                {
                    //ignore
                }
            });
        }

        private void UpdateChartItemModels(double[][] chartDataSets, int chartDataSetIndex, ChartDataItemModel<double, double>[][] measureResultData)
        {
            if (chartDataSets == null || chartDataSetIndex >= chartDataSets.Length) return; 
            var dataBlock = chartDataSets[chartDataSetIndex];

            if(dataBlock == null || _measureSetup == null)
            {
                return;
            }

            if (_measureSetup.UnitMode == UnitModes.Volume)
            {
                dataBlock = _volumeCalculationProvider.TransformMeasureResultDataBlock(_measureSetup, dataBlock);
            }

            if (_measureSetup.IsSmoothing)
            {
                _smoothingCalculationProvider.Width = (int)_measureSetup.SmoothingFactor;
                dataBlock = _smoothingCalculationProvider.TransformMeasureResultDataBlock(dataBlock);
            }

            if (_isNormalized && _cursorViewModels != null)
            {
                var dataMax = 0d;
                var newDataMax = 0d;

                List<Lazy<ChartCursorViewModel>> cursorViewModels;
                lock (_cursorViewModels.SyncRoot)
                {
                    cursorViewModels = _cursorViewModels.ToList();
                }

                if (cursorViewModels.Count == 0)
                {
                    newDataMax = dataBlock.Max();
                }
                else if (_measureSetup.MeasureMode == MeasureModes.Viability)
                {
                    var viableCellsCursor = cursorViewModels
                        .FirstOrDefault(cvm => !cvm.Value.Cursor.IsDeadCellsCursor)?.Value;

                    if (viableCellsCursor != null)
                    {
                        if (viableCellsCursor.Cursor?.MeasureSetup != null)
                        {
                            var viableCellsCursorMin = Calculations.CalcChannel(0,
                                viableCellsCursor.Cursor.MeasureSetup.ToDiameter, viableCellsCursor.MinLimit,
                                viableCellsCursor.Cursor.MeasureSetup.ChannelCount);
                            var viableCellsCursorMax = Calculations.CalcChannel(0,
                                viableCellsCursor.Cursor.MeasureSetup.ToDiameter, viableCellsCursor.MaxLimit,
                                viableCellsCursor.Cursor.MeasureSetup.ChannelCount);

                            newDataMax = dataBlock.Skip(viableCellsCursorMin)
                                .Take(viableCellsCursorMax - viableCellsCursorMin).Max();
                        }
                    }
                    else
                    {
                        newDataMax = dataBlock.Max();
                    }
                }
                else
                {
                    foreach (var cursorViewModelExport in cursorViewModels)
                    {
                        if (!cursorViewModelExport.IsValueCreated) continue;
                        var cursorViewModel = cursorViewModelExport.Value;

                        if (cursorViewModel?.Cursor?.MeasureSetup == null) continue;
                        var cursorMin = Calculations.CalcChannel(0, cursorViewModel.Cursor.MeasureSetup.ToDiameter,
                            cursorViewModel.MinLimit, cursorViewModel.Cursor.MeasureSetup.ChannelCount);
                        var cursorMax = Calculations.CalcChannel(0, cursorViewModel.Cursor.MeasureSetup.ToDiameter,
                            cursorViewModel.MaxLimit, cursorViewModel.Cursor.MeasureSetup.ChannelCount);

                        var dataMaxCursor = dataBlock.Skip(cursorMin).Take(cursorMax - cursorMin).Max();
                        if (dataMaxCursor > newDataMax)
                        {
                            newDataMax = dataMaxCursor;
                        }
                    }
                }

                if (newDataMax > dataMax)
                {
                    dataMax = newDataMax;
                }

                if (dataMax == 0d)
                {
                    dataMax = 1d;
                }

                var multiplicand = 100 / dataMax;
                dataBlock = dataBlock.Select(d => d * multiplicand).ToArray();
            }

            //int startIndex = 0;
            //int endIndex = dataBlock.Length;
            //var fromDiameter = _measureSetup.FromDiameter;
            //var toDiameter = _measureSetup.ToDiameter;


            /*
            if (_normalizationViewModel.IsVisible)
            {
                //var minChannel = Calculations.CalcChannel(fromDiameter, toDiameter, ) - 1;
                //startIndex = _normalizationViewModel.MinLimit - 1;

                //var maxChannel = Calculations.CalcChannel(fromDiameter, toDiameter, ) + 1;
                //endIndex = _normalizationViewModel.MaxLimit + 1;

                dataBlock = dataBlock.Skip(_normalizationViewModel.MinLimit).Take(_normalizationViewModel.MaxLimit - _normalizationViewModel.MinLimit).ToArray();

                var dataMax = dataBlock.Max();
                var multiplicator = 100 / dataMax;

                dataBlock = dataBlock.Select(d => d * multiplicator).ToArray();

                _rememberIsAutoRange = IsAutoRange;
                _rememberMaxRange = MaxRange;
                MaxRange = 100;
                IsAutoRange = false;
            }
            else*/
            //if (_normalizationViewModel.IsVisible)
            //{
            //var minChannel = Calculations.CalcChannel(fromDiameter, toDiameter, _normalizationViewModel.MinLimit) - 1;
            //startIndex = minChannel;

            //var maxChannel = Calculations.CalcChannel(fromDiameter, toDiameter, _normalizationViewModel.MaxLimit) + 1;
            //endIndex = maxChannel;

            //var dataMax = dataBlock.Skip(_normalizationViewModel.MinLimit).Take(_normalizationViewModel.MaxLimit - _normalizationViewModel.MinLimit).ToArray().Max();
            //var multiplicator = 100 / dataMax;

            //dataBlock = dataBlock.Select(d => d * multiplicator).ToArray();

            //if(!_rememberIsAutoRange.HasValue)
            //{ 
            //_rememberIsAutoRange = IsAutoRange;
            //}

            //if(!_rememberMaxRange.HasValue)
            //{ 
            //_rememberMaxRange = MaxRange;
            //}
            //MaxRange = 100;
            //IsAutoRange = false;
            //}
            //else if (_rememberIsAutoRange.HasValue)
            //{
            //    IsAutoRange = _rememberIsAutoRange.Value;
            //    if(!_isAutoRange)
            //    { 
            //        MaxRange = _rememberMaxRange.Value;
            //    }

            //    _rememberIsAutoRange = null;
            //    _rememberMaxRange = null;
            //}
            //}

            if (measureResultData != null && chartDataSetIndex < measureResultData.Length)
            {
                lock (measureResultData.SyncRoot)
                {
                    var measureResultData1 = measureResultData[chartDataSetIndex];
                    var length = dataBlock.Length;
                    for (var i = 0; i < length; i++)
                    {
                        measureResultData1[i].ValueY = dataBlock[i];
                    }
                }
            }
        }

        private void OnScroll(EventInformation<XYDiagram2DScrollEventArgs> obj)
        {
            Task.Factory.StartNew(() =>
            {
                Application.Current.Dispatcher.Invoke(() => DrawOverlays(obj.Sender as XYDiagram2D));
            });
            obj.EventArgs.Handled = false;
        }

        private void OnZoom(EventInformation<XYDiagram2DZoomEventArgs> obj)
        {
            Task.Factory.StartNew(() =>
            {
                Application.Current.Dispatcher.Invoke(() => DrawOverlays(obj.Sender as XYDiagram2D));
            });
            obj.EventArgs.Handled = false;
        }

        private void OnBoundDataChanged(EventInformation<RoutedEventArgs> obj)
        {
            if (!(obj.Sender is ChartControl chart)) return;
            _refreshOverlays = true;
            DrawOverlays(chart.Diagram as XYDiagram2D);
        }

        public bool IsExpandViewCollapsed { get; set; }

        public bool IsButtonMenuCollapsed
        {
            get => _isButtonMenuCollapsed;
            set
            {
                if (value == _isButtonMenuCollapsed) return;
                _isButtonMenuCollapsed = value;
                NotifyOfPropertyChange();
            }
        }

        private void DrawOverlays(XYDiagram2D diagram)
        {
            _notDrawed = false;
            //_timer.Stop();
            _refreshOverlays = true;
            if (_refreshOverlays)
            {
                foreach (var overlayViewModel in ChartOverlayViewModels)
                {
                    if (overlayViewModel is RangeModificationHandleViewModel handleViewModel)
                    {
                        handleViewModel.OnWidthChanged = null;
                        handleViewModel.IsValidHorizontalChange = null;
                    }

                    overlayViewModel.Dispose();
                }

                ChartOverlayViewModels.Clear();
            }

            if (_measureSetup == null) return;
            if (diagram == null || diagram.Series.Count <= 0) return;
            foreach (var spline in diagram.Series.OfType<SplineSeries2D>())
            {
                if (spline.Points.Count > 0)
                {
                    var chartDataItemModel = (ChartDataItemModel<double, double>)spline.Points[0].Tag;
                    spline.Brush = new SolidColorBrush(chartDataItemModel.LineColor);
                    spline.LineStyle.Thickness = chartDataItemModel.LineThickness;
                }

                if (IsAutoRange)
                {
                    //diagram.ActualAxisY.VisualRange.SetAuto();
                    diagram.ActualAxisY.WholeRange.SetAuto();
                }
                else
                {
                    diagram.ActualAxisY.WholeRange.SetMinMaxValues(0, MaxRange);
                }
            }

            //MinXValue = _measureResultManager.GetMinMeasureLimit(_measureSetup.CapillarySize);
            //double maxRange = 0d;

            //if (_measureSetup.ScalingMode == ScalingModes.MaxRange)
            //{
            //    maxRange = _measureSetup.ScalingMaxRange;
            //}
            //else
            //{
            //    var minXValueChannel = Calculations.CalcChannel(_measureSetup.FromDiameter, _measureSetup.ToDiameter, MinXValue);

            //    if (_measureSetup.UnitMode == UnitModes.Volume)
            //    {
            //        //maxRange = _measureSetup.MeasureResult.MeasureResultItems.Where(mri => mri.MeasureResultItemType == MeasureResultItemTypes.PeakVolume).Max(mri => mri.ResultItemValue);
            //        //maxRange = _chartDataSets.Select(dataSet => dataSet.Skip(minXValueChannel).Max()).Max();
            //    }
            //    else if (_measureSetup.UnitMode == UnitModes.Counts)
            //    {
            //        //maxRange = _chartDataSets.Select(dataSet => dataSet.Skip(minXValueChannel).Max()).Max();

            //    }
            //    maxRange = _measureResultData.Select(data => data.Max(item => item.ValueY)).Max();

            //}

            //XYDiagram2D diagram = ((XYDiagram2D)chart.Diagram);

            if (_cursorViewModels == null || _measureResultData == null || !_cursorViewModels.Any()) return;
            var maxRange = 0d;
            lock (_measureResultData.SyncRoot)
            {
                if ((double)diagram.ActualAxisY.ActualVisualRange.ActualMaxValue == 1d && _measureResultData != null &&
                    _measureResultData.Any())
                {
                    var maxDataValues = _measureResultData.Select(data => data.Max(item => item.ValueY)).ToArray();
                    maxRange = maxDataValues.Any() ? maxDataValues.Max() : 0d;
                }
                else
                {
                    maxRange = (double)diagram.ActualAxisY.ActualVisualRange.ActualMaxValue;
                }
            }

            if(maxRange == 0d)
            {
                maxRange = 0.5d;
            }

            Lazy<ChartCursorViewModel>[] orderedCursorExports;
            lock (_cursorViewModels.SyncRoot)
            {
                orderedCursorExports = _cursorViewModels.Where(cvm => cvm.Value.IsVisible)
                    .OrderBy(cvm => cvm.Value.MinLimit).ToArray();
            }

            if (orderedCursorExports.Any())
                {
                    var firstCursorViewModel = orderedCursorExports[0].Value;

                    var topLeftDiagram = diagram
                        .DiagramToPoint((double) diagram.ActualAxisX.ActualVisualRange.ActualMinValue, maxRange).Point;
                    var bottomRightDiagram = diagram
                        .DiagramToPoint((double) diagram.ActualAxisX.ActualVisualRange.ActualMaxValue,
                            (double) diagram.ActualAxisY.ActualVisualRange.ActualMinValue).Point;

                    var bottomRightCursor = diagram.DiagramToPoint(firstCursorViewModel.MinLimit, 0.1).Point;

                    //Point bottomRight = diagram.DiagramToPoint((double)diagram.ActualAxisX.ActualVisualRange.ActualMaxValue, (double)diagram.ActualAxisY.ActualVisualRange.ActualMinValue).Point;


                    if (_refreshOverlays)
                    {
                        if (firstCursorViewModel.MinModificationHandleViewModel != null)
                        {
                            firstCursorViewModel.MinModificationHandleViewModel.Dispose();
                            firstCursorViewModel.MinModificationHandleViewModel = null;
                        }

                        var minModificationHandle = new RangeMinModificationHandleViewModel(firstCursorViewModel)
                        {
                            RangeDoubleClickCommand = RangeDoubleClickCommand
                        };
                        firstCursorViewModel.MinModificationHandleViewModel = minModificationHandle;

                        ChartOverlayViewModels.Add(firstCursorViewModel.MinModificationHandleViewModel);
                    }
                    else
                    {
                        firstCursorViewModel.MinModificationHandleViewModel.OnWidthChanged = null;
                        firstCursorViewModel.MinModificationHandleViewModel.IsValidHorizontalChange = null;
                    }

                    firstCursorViewModel.MinModificationHandleViewModel.PositionLeft = topLeftDiagram.X;
                    firstCursorViewModel.MinModificationHandleViewModel.PositionTop = topLeftDiagram.Y - 0;
                    firstCursorViewModel.MinModificationHandleViewModel.Height =
                        bottomRightDiagram.Y - topLeftDiagram.Y + 0;
                    if (bottomRightCursor.X >= topLeftDiagram.X)
                    {
                        firstCursorViewModel.MinModificationHandleViewModel.IsVisible = true;
                        firstCursorViewModel.MinModificationHandleViewModel.Width =
                            bottomRightCursor.X - topLeftDiagram.X;
                    }
                    else
                    {
                        firstCursorViewModel.MinModificationHandleViewModel.IsVisible = false;
                    }

                    firstCursorViewModel.MinModificationHandleViewModel.OnWidthChanged = (newWidth) =>
                    {
                        _suppressCursorModificationEvents = true;
                        var pos = diagram.PointToDiagram(new Point(newWidth + topLeftDiagram.X, bottomRightCursor.Y));
                        firstCursorViewModel.MinLimit = pos.NumericalArgument;
                        _suppressCursorModificationEvents = false;
                    };
                    firstCursorViewModel.MinModificationHandleViewModel.IsValidHorizontalChange = (offset, isMin) =>
                    {
                        if (firstCursorViewModel.MinModificationHandleViewModel == null) return false;
                        var newWidth = firstCursorViewModel.MinModificationHandleViewModel.Width + offset;
                        var pos = diagram.PointToDiagram(new Point(newWidth + topLeftDiagram.X, bottomRightCursor.Y));
                        return pos.NumericalArgument >= MinXValue &&
                               pos.NumericalArgument < firstCursorViewModel.MaxLimit;
                    };

                    for (var i = 1; i < orderedCursorExports.Length; i++)
                    {
                        var currentViewModel = orderedCursorExports[i - 1].Value;
                        var nextViewModel = orderedCursorExports[i].Value;

                        if (!(nextViewModel.MinLimit >= currentViewModel.MaxLimit)) continue;
                        var topLeftCursor3 = diagram.DiagramToPoint(currentViewModel.MaxLimit, maxRange).Point;
                        var bottomRightCursor3 = diagram.DiagramToPoint(nextViewModel.MinLimit, 0.1).Point;

                        var biModificationHandle =
                            currentViewModel.MaxModificationHandleViewModel as RangeBiModificationHandleViewModel;

                        if (_refreshOverlays)
                        {
                            if (currentViewModel.MaxModificationHandleViewModel != null)
                            {
                                currentViewModel.MaxModificationHandleViewModel.Dispose();
                                currentViewModel.MaxModificationHandleViewModel = null;
                            }

                            if (nextViewModel.MinModificationHandleViewModel != null)
                            {
                                nextViewModel.MinModificationHandleViewModel.Dispose();
                                nextViewModel.MinModificationHandleViewModel = null;
                            }

                            biModificationHandle =
                                new RangeBiModificationHandleViewModel(currentViewModel, nextViewModel)
                                {
                                    RangeDoubleClickCommand = RangeDoubleClickCommand
                                };
                            currentViewModel.MaxModificationHandleViewModel = biModificationHandle;
                            nextViewModel.MinModificationHandleViewModel = biModificationHandle;

                            ChartOverlayViewModels.Add(biModificationHandle);
                        }
                        else if (biModificationHandle != null)
                        {
                            biModificationHandle.OnWidthChanged = null;
                            biModificationHandle.IsValidHorizontalChange = null;
                        }

                        if (biModificationHandle == null) continue;
                        if (topLeftCursor3.X >= topLeftDiagram.X)
                        {
                            biModificationHandle.IsMinVisible = true;
                            biModificationHandle.PositionLeft = topLeftCursor3.X - 40;
                        }
                        else
                        {
                            biModificationHandle.IsMinVisible = false;
                            biModificationHandle.PositionLeft = topLeftDiagram.X;
                        }

                        biModificationHandle.PositionTop = topLeftCursor3.Y - 0;
                        biModificationHandle.Height = bottomRightDiagram.Y - topLeftCursor3.Y + 0;

                        if (bottomRightCursor3.X >= topLeftDiagram.X && bottomRightCursor3.X <= bottomRightDiagram.X)
                        {
                            biModificationHandle.Width =
                                bottomRightCursor3.X - (topLeftCursor3.X >= topLeftDiagram.X
                                    ? topLeftCursor3.X
                                    : topLeftDiagram.X);
                            biModificationHandle.IsMaxVisible = true;
                        }
                        else
                        {
                            biModificationHandle.Width = topLeftCursor3.X >= topLeftDiagram.X
                                ? bottomRightDiagram.X - topLeftCursor3.X
                                : 0;
                            biModificationHandle.IsMaxVisible = false;
                        }

                        biModificationHandle.OnWidthChanged = (d) =>
                        {
                            if (biModificationHandle == null) return;
                            _suppressCursorModificationEvents = true;

                            var pos = diagram.PointToDiagram(new Point(biModificationHandle.PositionLeft + 40,
                                bottomRightCursor3.Y));
                            var pos2 = diagram.PointToDiagram(new Point(d + biModificationHandle.PositionLeft + 40,
                                bottomRightCursor3.Y));

                            currentViewModel.MaxLimit = pos.NumericalArgument;
                            nextViewModel.MinLimit = pos2.NumericalArgument;
                            _suppressCursorModificationEvents = false;
                        };
                        biModificationHandle.IsValidHorizontalChange = (offset, isMin) =>
                        {
                            if (biModificationHandle == null) return false;
                            double newPosLeft, newWidth;
                            if (!isMin.HasValue)
                            {
                                newPosLeft = biModificationHandle.PositionLeft + offset;
                                newWidth = biModificationHandle.Width;
                            }
                            else if (isMin.Value)
                            {
                                newPosLeft = biModificationHandle.PositionLeft;
                                newWidth = biModificationHandle.Width + offset;
                            }
                            else
                            {
                                newPosLeft = biModificationHandle.PositionLeft + offset;
                                newWidth = biModificationHandle.Width - offset;
                            }

                            var posMax = diagram.PointToDiagram(new Point(newPosLeft + 40, bottomRightCursor3.Y));
                            var posMin =
                                diagram.PointToDiagram(new Point(newWidth + newPosLeft + 40, bottomRightCursor3.Y));

                            return posMax.NumericalArgument <= posMin.NumericalArgument &&
                                   posMax.NumericalArgument >= currentViewModel.MinLimit &&
                                   posMin.NumericalArgument <= nextViewModel.MaxLimit;
                        };
                    }

                    var lastViewModel = orderedCursorExports[orderedCursorExports.Length - 1].Value;
                    var topLeftCursor2 = diagram.DiagramToPoint(lastViewModel.MaxLimit, maxRange).Point;
                    //Point bottomRight2 = diagram.DiagramToPoint(_measureSetup.ToDiameter, 0.1).Point;

                    if (_refreshOverlays)
                    {
                        if (lastViewModel.MaxModificationHandleViewModel != null)
                        {
                            lastViewModel.MaxModificationHandleViewModel.Dispose();
                            lastViewModel.MaxModificationHandleViewModel = null;
                        }

                        var maxModificationHandle = new RangeMaxModificationHandleViewModel(lastViewModel)
                        {
                            RangeDoubleClickCommand = RangeDoubleClickCommand
                        };
                        lastViewModel.MaxModificationHandleViewModel = maxModificationHandle;

                        ChartOverlayViewModels.Add(lastViewModel.MaxModificationHandleViewModel);
                    }
                    else
                    {
                        lastViewModel.MaxModificationHandleViewModel.OnWidthChanged = null;
                        lastViewModel.MaxModificationHandleViewModel.IsValidHorizontalChange = null;
                    }

                    lastViewModel.MaxModificationHandleViewModel.PositionLeft = topLeftCursor2.X - 20;
                    lastViewModel.MaxModificationHandleViewModel.PositionTop = topLeftCursor2.Y - 0;
                    lastViewModel.MaxModificationHandleViewModel.Height = bottomRightDiagram.Y - topLeftCursor2.Y + 0;

                    if (topLeftCursor2.X <= bottomRightDiagram.X)
                    {
                        lastViewModel.MaxModificationHandleViewModel.IsVisible = true;
                        lastViewModel.MaxModificationHandleViewModel.Width = bottomRightDiagram.X - topLeftCursor2.X;
                    }
                    else
                    {
                        lastViewModel.MaxModificationHandleViewModel.IsVisible = false;
                    }


                    lastViewModel.MaxModificationHandleViewModel.OnWidthChanged = (d) =>
                    {
                        if (lastViewModel.MaxModificationHandleViewModel == null) return;
                        _suppressCursorModificationEvents = true;
                        var pos = diagram.PointToDiagram(new Point(
                            lastViewModel.MaxModificationHandleViewModel.PositionLeft + 20, topLeftCursor2.Y));
                        lastViewModel.MaxLimit = pos.NumericalArgument;
                        _suppressCursorModificationEvents = false;
                    };
                    lastViewModel.MaxModificationHandleViewModel.IsValidHorizontalChange = (offset, isMin) =>
                    {
                        if (lastViewModel.MaxModificationHandleViewModel == null || _measureSetup == null) return false;
                        var newPosLeft = lastViewModel.MaxModificationHandleViewModel.PositionLeft + offset;
                        //var newWidth = lastViewModel.MaxModificationHandleViewModel.Width - offset;
                        var pos = diagram.PointToDiagram(new Point(newPosLeft + 20, topLeftCursor2.Y));

                        return pos.NumericalArgument <= _measureSetup.ToDiameter &&
                               pos.NumericalArgument > lastViewModel.MinLimit;
                    };
                
            }

            _refreshOverlays = false;
        }

        public ICommand RangeDoubleClickCommand
        {
            get {
                return new OmniDelegateCommand<object>((parameter) =>
                {
                    if(_isReadOnly || !IsMulticursorMode || _cursorViewModels == null || _cursorViewModels.Count >= 5 || _authenticationService.LoggedInUser.UserRole.Priority <= 1)
                    {
                        return;
                    }

                    if (!(parameter is RangeModificationHandleViewModel viewModel)) return;
                    switch (viewModel)
                    {
                        case RangeMinModificationHandleViewModel rangeMin when rangeMin.Parent != null:
                        {
                            var minValue = MinXValue;
                            var maxValue = rangeMin.Parent.MinLimit;

                            AddRange(minValue, maxValue - 0.01);
                            return;
                        }
                        case RangeMaxModificationHandleViewModel rangeMax when rangeMax.Parent != null:
                        {
                            var minValue = rangeMax.Parent.MaxLimit;
                            var maxValue = _measureSetup.ToDiameter;

                            AddRange(minValue + 0.01, maxValue);
                            return;
                        }
                        case RangeBiModificationHandleViewModel rangeBi when rangeBi.MaxParent != null && rangeBi.MinParent != null:
                        {
                            var minValue = rangeBi.MaxParent.MaxLimit;
                            var maxValue = rangeBi.MinParent.MinLimit;

                            AddRange(minValue + 0.01, maxValue - 0.01);
                            return;
                        }
                    }
                });
            }
        }

        private void OnAddRange()
        {
            var minLeft = MinXValue;
            if (_cursorViewModels.Any())
            {
                minLeft = _cursorViewModels.Max(cvm => cvm.Value.MaxLimit) + 0.01;
            }

            AddRange(minLeft, _measureSetup.ToDiameter);
        }

        private void CreateDefaultCursor()
        {
            while(_measureSetup.Cursors.Count > 0)
            {
                var removeItem = new UICollectionUndoItem(_measureSetup.Cursors)
                {
                    ModelObject = _measureSetup,
                    Info = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                        _measureSetup.Cursors.ElementAt(0), 0)
                };
                _uiProject.Submit(removeItem);
            }

            if (_measureSetup.MeasureMode != MeasureModes.Viability) return;
            var deadCellsRange = new Cursor()
            {
                Name = "Cursor_DeadCells_Name",
                MinLimit = MinXValue,
                MaxLimit = (_measureSetup.ToDiameter / 2d) - 0.01,
                MeasureSetup = _measureSetup,
                IsDeadCellsCursor = true
            };

            var insertItem = new UICollectionUndoItem(_measureSetup.Cursors)
            {
                ModelObject = _measureSetup,
                Info = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, deadCellsRange, 0)
            };
            _uiProject.Submit(insertItem);

            var vitalCellsRange = new Cursor()
            {
                Name = "Cursor_VitalCells_Name",
                MinLimit = (_measureSetup.ToDiameter / 2d),
                MaxLimit = _measureSetup.ToDiameter,
                MeasureSetup = _measureSetup
            };

            insertItem = new UICollectionUndoItem(_measureSetup.Cursors)
            {
                ModelObject = _measureSetup,
                Info = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, vitalCellsRange, 0)
            };
            _uiProject.Submit(insertItem);
        }

        private void CalcDilutionFactor()
        {
            if (_ignoreRecalculateDilutionFactor || !(DilutionCasyTonVolume > 0d) || !(DilutionSampleVolume > 0d)) return;
            var newValue = (DilutionCasyTonVolume * 1000 + DilutionSampleVolume) / DilutionSampleVolume;
            _uiProject.SendUIEdit(_measureSetup, "DilutionFactor", newValue);
            NotifyOfPropertyChange("DilutionFactor");
        }

        private void CalcDilutionSampleVolume()
        {
            if (_ignoreRecalculateSampleVolume || !(DilutionCasyTonVolume > 0d)) return;
            var newValue = (1000 * DilutionCasyTonVolume) / (DilutionFactor - 1);
            _uiProject.SendUIEdit(_measureSetup, "DilutionSampleVolume", newValue);
            NotifyOfPropertyChange("DilutionSampleVolume");
        }

        private void OnEditAccess()
        {
            Task.Factory.StartNew(async () =>
            {
                var success = await _measureResultManager.SaveChangedMeasureResults();

                if (success != ButtonResult.Cancel)
                {
                    var awaiter = new ManualResetEvent(false);
                    var viewModel = _compositionFactory.GetExport<IAccessManagementViewModel>().Value;
                    viewModel.SetAssociatedMeasureResult(_measureSetup.MeasureResult);

                    var wrapper = new ShowCustomDialogWrapper()
                    {
                        Awaiter = awaiter,
                        DataContext = viewModel,
                        DialogType = typeof(IAccessManagementView)
                    };

                    _eventAggregatorProvider.Instance.GetEvent<ShowCustomDialogEvent>().Publish(wrapper);
                    if (awaiter.WaitOne())
                    {
                    }
                }
            });
        }

        #region IDisposable Support


        protected override void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _eventAggregatorProvider.Instance.GetEvent<DeleteCursorEvent>().Unsubscribe(OnCursorDeleted);

                    _measureResultManager.SelectedMeasureResultDataChangedEvent -= OnSelectedMeasureResultDataChanged;
                    _localizationService.LanguageChanged -= OnLanguageChanged;

                    if (_cursorViewModels != null)
                    {
                        _cursorViewModels.SubmitUndoItem -= OnSubmitUndoCursor;
                    }

                    if (_measureSetup != null)
                    {
                        _measureSetup.PropertyChanged -= OnSetupChanged;
                    }

                    if(_cursorViewModels != null)
                    { 
                        foreach(var viewModelExport in _cursorViewModels)
                        {
                            viewModelExport.Value.Dispose();
                            _compositionFactory.ReleaseExport(viewModelExport);
                        }

                        _cursorViewModels.Dispose();
                    }

                    if(_chartColors != null)
                    {
                        foreach(var chartColorModel in _chartColors)
                        {
                            chartColorModel.PropertyChanged -= OnColorChange;
                            chartColorModel.Dispose();
                        }
                    }

                    _timer?.Dispose();
                }

                _chartDataSets = null;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ChartOverlayViewModels.Clear();
                });
                _cursorViewModels = null;
                _measureResultData = null;

                _disposedValue = true;
            }
            base.Dispose(disposing);
        }
#endregion
    }
}
