using OLS.Casy.Controller.Api;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Config.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Models;
using OLS.Casy.Models.Enums;
using OLS.Casy.RemoteIPS.Api;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Base.ViewModels;
using OLS.Casy.Ui.Core.Api;
using OLS.Casy.Ui.Measure.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace OLS.Casy.Ui.Measure.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(StartMeasureDialogViewModel))]
    public class StartMeasureDialogViewModel : DialogModelBase, IPartImportsSatisfiedNotification
    {
        private readonly ILocalizationService _localizationService;
        private readonly IMeasureController _measureController;
        private readonly IMeasureResultManager _measureResultManager;
        private readonly IConfigService _configService;
        private readonly IMeasureCounter _measureCounter;
        private readonly ICalibrationController _calibrationController;
        private readonly IAuthenticationService _authenticationService;
        private readonly IActivationService _activationService;
        private readonly IRemoteIpsService _remoteIpsService;
        private readonly IDatabaseStorageService _databaseStorageService;
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly ICompositionFactory _compositionFactory;
        private bool _isManualMeasurementMode;

        private MeasureSetup _template;
        private string _workbook;

        [ImportingConstructor]
        public StartMeasureDialogViewModel(ILocalizationService localizationService,
            ISelectTemplateViewModel selectTemplateViewModel,
            ManualMeasurementViewModel manualMeasurementViewModel,
            IMeasureController measureController,
            IMeasureResultManager measureResultManager,
            IConfigService configService,
            ICalibrationController calibrationController,
            IAuthenticationService authenticationService,
            IActivationService activationService,
            IDatabaseStorageService databaseStorageService,
            IEventAggregatorProvider eventAggregatorProvider,
            ICompositionFactory compositionFactory,
            [Import(AllowDefault = true)] IMeasureCounter measureCounter,
            [Import(AllowDefault = true)] IRemoteIpsService remoteIpsService)
        {
            _localizationService = localizationService;
            SelectTemplateViewModel = selectTemplateViewModel;
            ManualMeasurementViewModel = manualMeasurementViewModel;
            _measureController = measureController;
            _measureResultManager = measureResultManager;
            _configService = configService;
            _measureCounter = measureCounter;
            _calibrationController = calibrationController;
            _authenticationService = authenticationService;
            _activationService = activationService;
            _remoteIpsService = remoteIpsService;
            _databaseStorageService = databaseStorageService;
            _eventAggregatorProvider = eventAggregatorProvider;
            _compositionFactory = compositionFactory;

            Workbooks = new ObservableCollection<ComboBoxItemWrapperViewModel<string>>();
        }

        public ISelectTemplateViewModel SelectTemplateViewModel { get; }

        public ManualMeasurementViewModel ManualMeasurementViewModel { get; }

        public ICommand MeasureCommand => new OmniDelegateCommand(OnMeasure);

        public ICommand ToggleMeasurementModeCommand => new OmniDelegateCommand(OnToggleMeasurementMode);

        public int? CustomRepeats { get; set; }

        public bool IsManualMeasurementMode
        {
            get => _isManualMeasurementMode;
            set
            {
                if (value != _isManualMeasurementMode)
                {
                    _isManualMeasurementMode = value;
                    NotifyOfPropertyChange();

                    UpdateTitle();
                    NotifyOfPropertyChange("ToggleMeasurementModeButtonText");
                }
            }
        }

        public string ToggleMeasurementModeButtonText => IsManualMeasurementMode ? _localizationService.GetLocalizedString("StartMeasureDialog_Button_SelectTemplate") : _localizationService.GetLocalizedString("StartMeasureDialog_Button_ManualMesurement");

        public bool CanMeasure
        {
            get
            {
                var canMeasure = _measureController.SelectedTemplate != null && _measureController.IsValidTemplate(_measureController.SelectedTemplate); 
                if (canMeasure && _measureCounter != null)
                {
                    canMeasure &= _measureCounter.HasAvailableCounts(_measureController.SelectedTemplate.Repeats);
                }
                return canMeasure;
            }
        }

        public bool IsRemoteIpsEnabled
        {
            get { return _remoteIpsService != null; }
        }

        public ObservableCollection<ComboBoxItemWrapperViewModel<string>> Workbooks { get; }

        public string SelectedWorkbook
        {
            get => _workbook;
            set
            {
                _workbook = value;
                NotifyOfPropertyChange();
            }
        }

        public void OnImportsSatisfied()
        {
            _measureController.SelectedTemplateChangedEvent += OnSelectedTemplateChanged;

            if(_measureController.SelectedTemplate != null)
            {
                if(_measureController.SelectedTemplate.IsManual)
                {
                    _measureController.SelectedTemplate = null;
                }
                else
                { 
                    var templateViewModel = SelectTemplateViewModel.TemplateViewModels.FirstOrDefault(tvm => tvm.TemplateId == _measureController.SelectedTemplate.MeasureSetupId);
                    if(templateViewModel != null)
                    {
                        templateViewModel.IsSelected = true;
                    }
                }
            }

            SelectTemplateViewModel.ShowSettings = false;
            UpdateTitle();

            if(_remoteIpsService != null)
            {
                var workbooks = _remoteIpsService.GetWorkbookNames();

                foreach (var workbook in workbooks)
                {
                    Workbooks.Add(new ComboBoxItemWrapperViewModel<string>(workbook)
                    {
                        DisplayItem = workbook
                    });
                }
            }
        }

        private void UpdateTitle()
        {
            if(IsManualMeasurementMode)
            {
                Title = _localizationService.GetLocalizedString("ManualMeasurementView_Header");
            }
            else
            {
                Title = _localizationService.GetLocalizedString("StartMeasureDialog_Title");
            }
        }

        private void OnSelectedTemplateChanged(object sender, SelectedTemplateChangedEventArgs e)
        {
            if(_template != null)
            {
                _template.PropertyChanged -= OnTemplatePropertyChanged;
            }

            _template = _measureController.SelectedTemplate;

            if (_template != null)
            {
                _template.PropertyChanged += OnTemplatePropertyChanged;
            }

            NotifyOfPropertyChange("CanMeasure");
        }

        private void OnTemplatePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(IsManualMeasurementMode)
            {
                NotifyOfPropertyChange("CanMeasure");
            }
        }

        private async void OnMeasure()
        {
            var isCfr = _activationService.IsModuleEnabled("cfr");
            var showProbeName = !_databaseStorageService.GetSettings().TryGetValue("ShowProbeName", out var showProbeNameSetting) ? false : showProbeNameSetting.Value == "true";

            var template = _measureController.SelectedTemplate;

            var measurementName = template.AutoSaveName;
            var experiment = template.DefaultExperiment;
            var group = template.DefaultGroup;

            if (isCfr && showProbeName)
            {
                var result = await Task.Factory.StartNew<bool>(() =>
                {
                    var awaiter = new System.Threading.ManualResetEvent(false);

                    var viewModelExport = _compositionFactory.GetExport<PreselectMeasurementNameDialogModel>();
                    var viewModel = viewModelExport.Value;
                    viewModel.MeasureResultName = measurementName;
                    viewModel.SelectedExperiment = experiment;
                    viewModel.SelectedGroup = group;

                    var wrapper = new ShowCustomDialogWrapper()
                    {
                        Awaiter = awaiter,
                        Title = "EnterMeasurementName_Title",
                        DataContext = viewModel,
                        DialogType = typeof(PreselectMeasurementNameDialog)
                    };

                    _eventAggregatorProvider.Instance.GetEvent<ShowCustomDialogEvent>().Publish(wrapper);

                    if (awaiter.WaitOne())
                    {
                        if (viewModel.ButtonResult == ButtonResult.Cancel)
                        {
                            return false;
                        }
                        measurementName = viewModel.MeasureResultName;
                        experiment = viewModel.SelectedExperiment;
                        group = viewModel.SelectedGroup;
                        return true;
                    }
                    return false;
                });

                if (!result)
                    return;
            }


            OkCommand.Execute(null);
            var now = DateTime.Now;

            if(!template.IsManual && template.MeasureSetupId > -1)
            {
                if(_authenticationService.LoggedInUser.RecentTemplateIds.Contains(template.MeasureSetupId))
                {
                    _authenticationService.LoggedInUser.RecentTemplateIds.Remove(template.MeasureSetupId);
                }

                _authenticationService.LoggedInUser.RecentTemplateIds.Insert(0, template.MeasureSetupId);
                _authenticationService.SaveUser(_authenticationService.LoggedInUser);
            }

            var lastColorIndex = _measureController.LastColorIndex;
            var colorName = "ChartColor" + (lastColorIndex % 10 == 0 ? 1 : 1 + (lastColorIndex % 10));

            var measureSetup = template;
            if (template.MeasureSetupId != -1)
            {
                measureSetup = _measureResultManager.CloneTemplate(template);
            }

            var measureResult = new MeasureResult()
            {
                MeasureResultGuid = Guid.NewGuid(),
                MeasureSetup = measureSetup,
                OriginalMeasureSetup = measureSetup,
                Name = string.IsNullOrEmpty(measurementName) ? 
                            string.Format(_localizationService.GetLocalizedString("MeasureController_TemporaryMeasureResultName", (string.IsNullOrEmpty(template.Name) ? _localizationService.GetLocalizedString("MeasureController_TemporaryTemplateName") : template.Name).ToUpper(), now.ToString("yyyy-MM-dd HH-mm"))) :
                            measurementName,
                Color = ((SolidColorBrush)Application.Current.Resources[colorName]).Color.ToString(),
                MeasuredAt = DateTime.Now,
                MeasuredAtTimeZone = TimeZoneInfo.Local,
                IsCfr = isCfr,
                Experiment = experiment,
                Group = group
            };

            var tempName = _measureResultManager.FindMeasurementName(measureResult);
            measureResult.Name = tempName;

            measureSetup.MeasureResult = measureResult;

            await _measureResultManager.AddSelectedMeasureResults(new[] { measureResult });

            _measureController.LastColorIndex++;

            await Task.Factory.StartNew(async () =>
            {
                var result = await _measureController.Measure(measureResult, workbook: SelectedWorkbook, customRepeats: CustomRepeats);

                if (result == null)
                {
                    await _measureResultManager.RemoveSelectedMeasureResults(new[] {measureResult});
                }

                if (!template.IsTemplate)
                {
                    this._measureController.SelectedTemplate = null;
                }
            });
        }

        private void OnToggleMeasurementMode()
        {
            _measureController.SelectedTemplate = CreateDefaultTemplate();
            IsManualMeasurementMode = !IsManualMeasurementMode;
        }

        private MeasureSetup CreateDefaultTemplate()
        {
            var types = new List<MeasureResultItemTypes>();
            foreach (var type in Enum.GetNames(typeof(MeasureResultItemTypes)))
            {
                types.Add((MeasureResultItemTypes)Enum.Parse(typeof(MeasureResultItemTypes), type));
            }

            var defaultTemplate = new MeasureSetup()
            {
                Repeats = 1,
                Volume = Volumes.TwoHundred,
                AggregationCalculationMode = AggregationCalculationModes.Off,
                MeasureMode = MeasureModes.MultipleCursor,
                ScalingMode = ScalingModes.Auto,
                UnitMode = UnitModes.Counts,
                SmoothingFactor = 5d,
                DilutionFactor = 101d,
                DilutionSampleVolume = 100d,
                DilutionCasyTonVolume = 10d,
                ResultItemTypes = string.Join(";", types),
                IsManual = true
            };

            if(_measureController.LastSelectedCapillary != 0)
            {
                defaultTemplate.CapillarySize = _measureController.LastSelectedCapillary;
            }
            else if (_calibrationController.KnownCappillarySizes.Count() == 1)
            {
                defaultTemplate.CapillarySize = _calibrationController.KnownCappillarySizes.First();
            }

            return defaultTemplate;
        }
    }
}
