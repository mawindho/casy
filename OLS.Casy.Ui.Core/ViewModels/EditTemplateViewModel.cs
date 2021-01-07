using OLS.Casy.Controller.Api;
using OLS.Casy.Core;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Core.Logging.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Models;
using OLS.Casy.Models.Enums;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Base.ViewModels;
using OLS.Casy.Ui.Core.Api;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using OLS.Casy.Ui.Core.UndoRedo;

namespace OLS.Casy.Ui.Core.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(EditTemplateViewModel))]
    public class EditTemplateViewModel : ValidationViewModelBase, IPartImportsSatisfiedNotification
    {
        private readonly IUIProjectManager _uiProject;
        private readonly ICalibrationController _calibrationController;
        private readonly IMeasureResultStorageService _measureResultStorageService;
        private readonly ILocalizationService _localizationService;
        private readonly ITemplateManager _editTemplateManager;
        private readonly IMeasureResultManager _measureResultManager;
        private readonly IDatabaseStorageService _databaseStorageService;
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly ILogger _logger;

        private MeasureSetup _template;
        private string _smoothingFactorDisplay;
        private string _originalName;
        private bool _ignoreRecalcSampleVolume;
        private bool _ignoreRecalcDilutionFactor;
        private SmartCollection<string> _knownToDiameters;

        [ImportingConstructor]
        public EditTemplateViewModel(
            IUIProjectManager uiProject,
            [Import(AllowDefault = true)] ICalibrationController calibrationController,
            IMeasureResultStorageService measureResultStorageService,
            ILocalizationService localizationService,
            ITemplateManager editTemplateManager,
            IMeasureResultManager measureResultManager,
            IDatabaseStorageService databaseStorageService,
            IEventAggregatorProvider eventAggregatorProvider,
            ILogger logger
            )
        {
            _uiProject = uiProject;
            _calibrationController = calibrationController;
            _measureResultStorageService = measureResultStorageService;
            _localizationService = localizationService;
            _editTemplateManager = editTemplateManager;
            _measureResultManager = measureResultManager;
            _databaseStorageService = databaseStorageService;
            _eventAggregatorProvider = eventAggregatorProvider;
            _logger = logger;

            AvailableCapillarySizes = new SmartCollection<string>();
            KnownToDiameters = new SmartCollection<string>();
            AggregationCalculationModes = new SmartCollection<ComboBoxItemWrapperViewModel<AggregationCalculationModes>>();
        }

        public MeasureSetup Template
        {
            get { return _template; }
            set
            {
                if(value != _template)
                {
                    if (_template != null)
                    {
                        _template.PropertyChanged -= OnTemplatePropertyChanged;
                        _originalName = null;
                    }

                    _template = value;

                    if (_template != null)
                    {
                        _template.PropertyChanged += OnTemplatePropertyChanged;
                        _originalName = _template.Name;
                    }

                    NotifyOfPropertyChange("Name");

                    if(_calibrationController == null)
                    {
                        AvailableCapillarySizes.Clear();
                        AvailableCapillarySizes.Add(this._template.CapillarySize.ToString());
                    }

                    NotifyOfPropertyChange("CapillarySize");
                    FillKnownToDiameters();

                    NotifyOfPropertyChange("ToDiameter");
                    NotifyOfPropertyChange("Repeats");
                    NotifyOfPropertyChange("Volume");
                    NotifyOfPropertyChange("IsMulticursorMode");
                    NotifyOfPropertyChange("IsViabilityMode");
                    NotifyOfPropertyChange("DilutionFactor");
                    NotifyOfPropertyChange("DilutionSampleVolume");
                    NotifyOfPropertyChange("DilutionCasyTonVolume");
                    NotifyOfPropertyChange("MeasureMode");

                    NotifyOfPropertyChange("DefaultExperiment");
                    NotifyOfPropertyChange("DefaultGroup");

                    NotifyOfPropertyChange("IsAutoSave");
                    NotifyOfPropertyChange("AutoSaveName");
                    NotifyOfPropertyChange("IsManualAggregationCalculationMode");
                    NotifyOfPropertyChange("ManualAggregationCalculationFactor");

                    NotifyOfPropertyChange("IsScalingModeAuto");
                    NotifyOfPropertyChange("IsScalingModeMaxRange");
                    NotifyOfPropertyChange("ScalingMaxRange");

                    NotifyOfPropertyChange("IsCountUnitMode");
                    NotifyOfPropertyChange("IsVolumeUnitMode");
                    NotifyOfPropertyChange("SmoothingFactor");
                    this.SmoothingFactorDisplay = this._template != null && this._template.IsSmoothing ? string.Format("x{0}", this._template.SmoothingFactor) : _localizationService.GetLocalizedString("MeasureResultChartView_SmoothingFactor_Off");

                    NotifyOfPropertyChange("IsAutoComment");
                    NotifyOfPropertyChange("TemplateVersion");

                    NotifyOfPropertyChange();
                }
            }
        }

        public string Name
        {
            get { return _template == null ? string.Empty : _template.Name; }
            set
            {
                if (value != _template.Name)
                {
                    this._uiProject.SendUIEdit(_template, "Name", value);
                }
            }
        }

        public string TemplateVersion
        {
            get { return _template == null ? "0" : _template.Version.ToString(); }
            set { }
        }

        public SmartCollection<string> AvailableCapillarySizes { get; }

        public string CapillarySize
        {
            get { return _template == null || _template.CapillarySize == 0 ? null : _template.CapillarySize.ToString(); }
            set
            {
                if (value != _template.CapillarySize.ToString())
                {
                    this._uiProject.StartUndoGroup();
                    this._uiProject.SendUIEdit(_template, "CapillarySize", !string.IsNullOrEmpty(value) ? int.Parse(value) : 0);
                    SetDefaults();
                    this._uiProject.SubmitUndoGroup();
                }
            }
        }

        public SmartCollection<string> KnownToDiameters
        {
            get => _knownToDiameters;
            set
            {
                _knownToDiameters = value;
                NotifyOfPropertyChange();
            }
        }

        public string ToDiameter
        {
            get => _template == null || _template.ToDiameter == 0 ? null : _template.ToDiameter.ToString();
            set
            {
                if (value != _template.ToDiameter.ToString())
                {
                    //Application.Current.Dispatcher.Invoke(() =>
                    //{
                        _uiProject.StartUndoGroup();
                        CreateDefaultCursorAsync().ContinueWith(result =>
                        {
                            _uiProject.SendUIEdit(_template, "ToDiameter",
                                !string.IsNullOrEmpty(value) ? int.Parse(value) : 0);
                            _uiProject.SubmitUndoGroup();
                        });
                        //{
                        //if (result.Result)
                        //{


                        //    });
                        //}
                        //});
                }
            }
        }

        public double Repeats
        {
            get { return _template == null ? 0d : _template.Repeats; }
            set
            {
                if (value != _template.Repeats)
                {
                    this._uiProject.SendUIEdit(_template, "Repeats", (int)value);
                    NotifyOfPropertyChange("CanMeasure");
                }
            }
        }

        public Volumes Volume
        {
            get { return _template == null ? Volumes.TwoHundred : _template.Volume; }
            set
            {
                if (value != _template.Volume)
                {
                    this._uiProject.SendUIEdit(_template, "Volume", value);
                }
            }
        }

        public bool IsMulticursorMode
        {
            get { return _template != null && _template.MeasureMode == MeasureModes.MultipleCursor; }
            set
            {
                if (_template.MeasureMode != MeasureModes.MultipleCursor)
                {
                    this._uiProject.StartUndoGroup();
                    //if (CreateDefaultCursorAsync().GetAwaiter().GetResult())
                    CreateDefaultCursorAsync().ContinueWith(result =>
                    {
                        this._uiProject.SendUIEdit(_template, "MeasureMode", MeasureModes.MultipleCursor);
                        NotifyOfPropertyChange();
                        NotifyOfPropertyChange("IsViabilityMode");
                        this._uiProject.SubmitUndoGroup();
                    });
                    
                }
            }
        }

        public bool IsViabilityMode
        {
            get { return _template != null && _template.MeasureMode == MeasureModes.Viability; }
            set
            {
                if (_template.MeasureMode != MeasureModes.Viability)
                {
                    this._uiProject.StartUndoGroup();
                    //if (CreateDefaultCursorAsync().GetAwaiter().GetResult())
                    CreateDefaultCursorAsync().ContinueWith(result =>
                    {
                        this._uiProject.SendUIEdit(_template, "MeasureMode", MeasureModes.Viability);
                        NotifyOfPropertyChange();
                        NotifyOfPropertyChange("IsMulticursorMode");
                        this._uiProject.SubmitUndoGroup();
                    });
                }
            }
        }

        [RegularExpression("[0-9]+")]
        public double DilutionFactor
        {
            get { return _template == null ? 0d : _template.DilutionFactor; }
            set
            {
                if (value != _template.DilutionFactor)
                {
                    this._uiProject.SendUIEdit(_template, "DilutionFactor", value);
                    this._ignoreRecalcDilutionFactor = true;
                    CalcDilutionSampleVolume();
                    this._ignoreRecalcDilutionFactor = false;
                }
            }
        }

        [RegularExpression("^[0-9]+([.,][0-9]+)?$")]
        public double DilutionSampleVolume
        {
            get { return _template == null ? 0d : _template.DilutionSampleVolume; }
            set
            {
                if (value != _template.DilutionSampleVolume)
                {
                    this._uiProject.SendUIEdit(_template, "DilutionSampleVolume", value);
                    _ignoreRecalcSampleVolume = true;
                    CalcDilutionFactor();
                    _ignoreRecalcSampleVolume = false;
                }
            }
        }

        [RegularExpression("[0-9]+")]
        public double DilutionCasyTonVolume
        {
            get { return _template == null ? 0d : _template.DilutionCasyTonVolume; }
            set
            {
                if (value != _template.DilutionCasyTonVolume)
                {
                    this._uiProject.SendUIEdit(_template, "DilutionCasyTonVolume", value);
                    _ignoreRecalcSampleVolume = true;
                    CalcDilutionFactor();
                    _ignoreRecalcSampleVolume = false;
                }
            }
        }

        private void CalcDilutionFactor()
        {
            if (!_ignoreRecalcDilutionFactor && DilutionCasyTonVolume > 0d && DilutionSampleVolume > 0d)
            {
                this.DilutionFactor = (this.DilutionCasyTonVolume * 1000 + DilutionSampleVolume) / DilutionSampleVolume;
            }
        }

        private void CalcDilutionSampleVolume()
        {
            if (_ignoreRecalcSampleVolume || !(DilutionCasyTonVolume > 0d)) return;
            var newValue = (1000 * DilutionCasyTonVolume) / (DilutionFactor - 1);
            _uiProject.SendUIEdit(_template, "DilutionSampleVolume", newValue);
            NotifyOfPropertyChange("DilutionSampleVolume");
            //if (!_ignoreRecalcSampleVolume && DilutionCasyTonVolume == 10d)
            //{
            //this.DilutionSampleVolume = (1000 * this.DilutionCasyTonVolume) / (DilutionFactor - 1);
            //}
        }

        public IEnumerable<string> KnownExperiments
        {
            get => _measureResultManager.GetExperiments().OrderBy(x => x);
            //get { return _databaseStorageService.GetExperiments().Where(e => !string.IsNullOrEmpty(e.Item1)).Select(e => e.Item1).Distinct().OrderBy(experiment => experiment).ToList(); }
        }

        public string DefaultExperiment
        {
            get { return _template == null ? null : _template.DefaultExperiment; }
            set
            {
                if (value != _template.DefaultExperiment)
                {
                    this._uiProject.SendUIEdit(_template, "DefaultExperiment", value);
                    NotifyOfPropertyChange();
                    NotifyOfPropertyChange("KnownExperiments");
                    NotifyOfPropertyChange("KnownGroups");
                }
            }
        }

        public IEnumerable<string> KnownGroups
        {
            get
            {
                return _measureResultManager.GetGroups(DefaultExperiment).OrderBy(x => x);
                //if (string.IsNullOrEmpty(this.DefaultExperiment))
                //
                //return new string[0];
                //}

                //return _databaseStorageService.GetGroups(DefaultExperiment).Where(g => !string.IsNullOrEmpty(g.Item1)).Select(g => g.Item1).Distinct().OrderBy(group => group).ToList();
            }
        }

        public string DefaultGroup
        {
            get { return _template == null ? null : _template.DefaultGroup; }
            set
            {
                var newValue = value == string.Empty ? null : value;
                if (newValue != _template.DefaultGroup)
                {
                    this._uiProject.SendUIEdit(_template, "DefaultGroup", newValue);
                    NotifyOfPropertyChange();
                    NotifyOfPropertyChange("KnownGroups");
                }
            }
        }

        public SmartCollection<ComboBoxItemWrapperViewModel<AggregationCalculationModes>> AggregationCalculationModes { get; }

        public AggregationCalculationModes AggregationCalculationMode
        {
            get { return this._template == null ? Casy.Models.Enums.AggregationCalculationModes.Off : this._template.AggregationCalculationMode; }
            set
            {
                if (value != this._template.AggregationCalculationMode)
                {
                    this._uiProject.SendUIEdit(this._template, "AggregationCalculationMode", value);
                    NotifyOfPropertyChange("IsManualAggregationCalculationMode");
                }
            }
        }

        public bool IsManualAggregationCalculationMode
        {
            get { return this._template != null && this._template.AggregationCalculationMode == Casy.Models.Enums.AggregationCalculationModes.Manual; }
        }

        public double ManualAggregationCalculationFactor
        {
            get { return this._template == null ? 0d : this._template.ManualAggregationCalculationFactor; }
            set
            {
                if (value != this._template.ManualAggregationCalculationFactor)
                {
                    this._uiProject.SendUIEdit(this._template, "ManualAggregationCalculationFactor", value);
                }
            }
        }

        public bool IsScalingModeAuto
        {
            get { return this._template != null && this._template.ScalingMode == ScalingModes.Auto; }
            set
            {
                if (this._template.ScalingMode != ScalingModes.Auto)
                {
                    this._uiProject.SendUIEdit(this._template, "ScalingMode", ScalingModes.Auto);
                    NotifyOfPropertyChange();
                    NotifyOfPropertyChange("IsScalingModeMaxRange");
                }
            }
        }

        public bool IsScalingModeMaxRange
        {
            get { return this._template != null && this._template.ScalingMode == ScalingModes.MaxRange; }
            set
            {
                if (this._template.ScalingMode != ScalingModes.MaxRange)
                {
                    this._uiProject.SendUIEdit(this._template, "ScalingMode", ScalingModes.MaxRange);
                    NotifyOfPropertyChange();
                    NotifyOfPropertyChange("IsScalingModeAuto");
                }
            }
        }


        public double ScalingMaxRange
        {
            get { return this._template == null ? 100 : Convert.ToUInt32(this._template.ScalingMaxRange < 0 ? 20d : this._template.ScalingMaxRange); }
            set
            {
                if (value != this._template.ScalingMaxRange)
                {
                    this._uiProject.SendUIEdit(this._template, "ScalingMaxRange", Convert.ToInt32(value));
                }
            }
        }

        public bool IsVolumeUnitMode
        {
            get { return this._template != null && this._template.UnitMode == UnitModes.Volume; }
            set
            {
                if (this._template.UnitMode != UnitModes.Volume)
                {
                    this._uiProject.SendUIEdit(this._template, "UnitMode", UnitModes.Volume);
                    NotifyOfPropertyChange();
                    NotifyOfPropertyChange("IsCountUnitMode");
                    NotifyOfPropertyChange("SmoothingFactor");
                }
            }
        }

        public bool IsCountUnitMode
        {
            get { return this._template != null && this._template.UnitMode == UnitModes.Counts; }
            set
            {
                if (this._template.UnitMode != UnitModes.Counts)
                {
                    this._uiProject.SendUIEdit(this._template, "UnitMode", UnitModes.Counts);
                    NotifyOfPropertyChange();
                    NotifyOfPropertyChange("IsVolumeUnitMode");
                }
            }
        }

        public int SmoothingFactor
        {
            get { return this._template == null || !this._template.IsSmoothing ? 0 : (int)this._template.SmoothingFactor; }
            set
            {
                double doubleValue = Convert.ToDouble(value);
                if (doubleValue != this._template.SmoothingFactor)
                {
                    if (doubleValue == 0d)
                    {
                        if (this._template.IsSmoothing)
                        {
                            this._uiProject.SendUIEdit(this._template, "IsSmoothing", false);
                            this._uiProject.SendUIEdit(this._template, "SmoothingFactor", 0d);
                        }
                        this.SmoothingFactorDisplay = _localizationService.GetLocalizedString("MeasureResultChartView_SmoothingFactor_Off");
                    }
                    else
                    {
                        if (!this._template.IsSmoothing)
                        {
                            this._uiProject.SendUIEdit(this._template, "IsSmoothing", true);
                        }
                        this._uiProject.SendUIEdit(this._template, "SmoothingFactor", doubleValue);
                        this.SmoothingFactorDisplay = string.Format("x{0}", value);
                    }
                }
            }
        }

        public string SmoothingFactorDisplay
        {
            get { return _smoothingFactorDisplay; }
            set
            {
                if (value != _smoothingFactorDisplay)
                {
                    this._smoothingFactorDisplay = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool IsAutoSave
        {
            get { return _template != null && _template.IsAutoSave; }
            set
            {
                if (value != _template.IsAutoSave)
                {
                    this._uiProject.SendUIEdit(_template, "IsAutoSave", value);
                }
            }
        }

        public string AutoSaveName
        {
            get { return _template == null ? string.Empty : _template.AutoSaveName; }
            set
            {
                if (value != _template.AutoSaveName)
                {
                    this._uiProject.SendUIEdit(_template, "AutoSaveName", value);
                }
            }
        }

        public bool IsAutoComment
        {
            get { return _template != null && _template.IsAutoComment; }
            set
            {
                if (value != _template.IsAutoComment)
                {
                    this._uiProject.SendUIEdit(_template, "IsAutoComment", value);
                }
            }
        }

        public bool IsDeviationControlEnabled
        {
            get { return _template != null && _template.IsDeviationControlEnabled; }
            set
            {
                if (value != _template.IsDeviationControlEnabled)
                {
                    this._uiProject.SendUIEdit(_template, "IsDeviationControlEnabled", value);
                    CheckForDeviationControlItem();
                }
            }
        }

        private DeviationControlItem CheckForDeviationControlItem()
        {
            var deviationControlItem = _template.DeviationControlItems.FirstOrDefault(item => item.MeasureResultItemType == MeasureResultItemTypes.Deviation);
            if (deviationControlItem == null)
            {
                deviationControlItem = new DeviationControlItem()
                {
                    MeasureResultItemType = MeasureResultItemTypes.Deviation,
                    MeasureSetup = _template
                };

                UICollectionUndoItem insertItem = new UICollectionUndoItem(_template.DeviationControlItems);
                insertItem.ModelObject = _template;
                insertItem.Info = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, deviationControlItem, 0);
                _uiProject.Submit(insertItem);
            }
            return deviationControlItem;
        }

        [Range(0d, 100d)]
        public double DeviationControlValue
        {
            get
            {
                var item = CheckForDeviationControlItem();
                return item.MaxLimit.HasValue ? item.MaxLimit.Value : 0d;
            }
            set
            {
                var item = CheckForDeviationControlItem();
                if(value != item.MaxLimit)
                {
                    this._uiProject.SendUIEdit(item, "MaxLimit", value);
                }
            }
        }

        public void OnImportsSatisfied()
        {
            if (this._calibrationController != null)
            {
                this.AvailableCapillarySizes.AddRange(_calibrationController.KnownCappillarySizes.Select(capillary => capillary.ToString()));
            }

            var aggregationCalculationModes = Enum.GetNames(typeof(AggregationCalculationModes));
            foreach (var aggregationCalculationMode in aggregationCalculationModes)
            {
                if (aggregationCalculationMode != "FromParent")
                {
                    var comboBoxWrapperItem = new ComboBoxItemWrapperViewModel<AggregationCalculationModes>((AggregationCalculationModes)Enum.Parse(typeof(AggregationCalculationModes), aggregationCalculationMode));
                    comboBoxWrapperItem.DisplayItem = _localizationService.GetLocalizedString(string.Format("AggregationCalculationMode_{0}_Name", aggregationCalculationMode));
                    this.AggregationCalculationModes.Add(comboBoxWrapperItem);
                }
            }
        }

        internal async Task<bool> SaveChanges()
        {
            return await Task<bool>.Factory.StartNew(() =>
            {
                if (!string.IsNullOrEmpty(this._originalName) && this._originalName != this.Name)
                {
                    MeasureSetup existingTemplate = _databaseStorageService.GetMeasureSetupTemplates().FirstOrDefault(ms => ms.Name.ToLower() == this._template.Name.ToLower());

                    if (existingTemplate != null)
                    {
                        //int setupId = existingTemplate.MeasureSetupId;

                        var awaiter = new ManualResetEvent(false);

                        // Template with same name already exists
                        ShowMessageBoxDialogWrapper messageBoxWrapper = new ShowMessageBoxDialogWrapper()
                        {
                            Awaiter = awaiter,
                            Message = "EditTemplateView_SetupAlreadyExists_Content",
                            Title = "EditTemplateView_SetupAlreadyExists_Title",
                            HideCancelButton = true
                        };

                        _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>().Publish(messageBoxWrapper);

                        if (awaiter.WaitOne())
                        {
                            return false;
                        }
                    }
                }

                this._editTemplateManager.SaveTemplate(this._template);
                this.ClearChanges();

                return true;
            });
        }

        internal async Task SaveAsCopy()
        {
            await Task.Factory.StartNew(() =>
            {
                //MeasureSetup existingTemplate = _databaseStorageService.GetMeasureSetupTemplates().FirstOrDefault(ms => ms.Name.ToLower() == this._template.Name.ToLower());
                MeasureSetup newTemplate = null;
                this._editTemplateManager.CloneSetup(this._template, ref newTemplate);

                var tempName = this._template.Name;
                int count = 1;
                var templateName = tempName;
                while (_databaseStorageService.GetMeasureSetupTemplates().FirstOrDefault(ms => ms.Name.ToLower() == templateName.ToLower()) != null)
                {
                    templateName = string.Format("{0}_{1}", tempName, count.ToString());
                    count++;
                }
                newTemplate.Name = templateName;

                newTemplate.IsTemplate = true;

                _logger.Info(LogCategory.Template, string.Format("Template '{0}' has been created.", templateName));
                _databaseStorageService.SaveMeasureSetup(newTemplate);

                this.UndoChanges();
            });
        }

        public bool CanSave()
        {
            return !string.IsNullOrEmpty(this.Name) && this.Name.IndexOfAny(new[] { '/', '\\', ':', '*', '<', '>', '|' }) == -1;
        }

        internal void UndoChanges()
        {
            if(this._template != null)
            {
                //if (this._template.Cursors != null)
                //{
                //    foreach (var cursor in this._template.Cursors)
                //    {
                //        this._uiProject.UndoAll(cursor);
                //    }
                //}

                //if (this._template.DeviationControlItems != null)
                //{
                //    foreach (var deviationControlItem in this._template.DeviationControlItems)
                //    {
                //        this._uiProject.UndoAll(deviationControlItem);
                //    }
                //}

                this._uiProject.UndoAll(/*this._template*/);
            }
        }

        internal void ClearChanges()
        {
            if (this._template != null)
            {
                this._uiProject.Clear();
                //if(this._template.Cursors != null)
                //{ 
                //    foreach (var cursor in this._template.Cursors)
                //    {
                //        this._uiProject.Clear(cursor);
                //    }
                //}

                //if (this._template.DeviationControlItems != null)
                //{
                //    foreach (var deviationControlItem in this._template.DeviationControlItems)
                //    {
                //        this._uiProject.Clear(deviationControlItem);
                //    }
                //}

                //this._uiProject.Clear(this._template);
            }
        }

        private async Task CreateDefaultCursorAsync()
        {
            await Task.Factory.StartNew(() =>
            {
                var awaiter = new ManualResetEvent(false);

                var messageBoxWrapper = new ShowMessageBoxDialogWrapper
                {
                    Awaiter = awaiter,
                    Title = _localizationService.GetLocalizedString("EditTemplateViewModel_ResetCursorMessage_Title"),
                    Message = _localizationService.GetLocalizedString("EditTemplateViewModel_ResetCursorMessage_Message"),
                };

                _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>().Publish(messageBoxWrapper);

                if (awaiter.WaitOne() && messageBoxWrapper.Result)
                {
                    while (_template.Cursors.Any())
                    {
                        _template.Cursors.RemoveAt(0);
                    }

                    if (_template.MeasureMode == MeasureModes.Viability)
                    {
                        var minLimit = _measureResultManager.GetMinMeasureLimit(this._template.CapillarySize);

                        _template.Cursors.Add(new Casy.Models.Cursor()
                        {
                            Name = "Cursor_DeadCells_Name",
                            MinLimit = minLimit,//Calculations.CalcChannel(_template.FromDiameter, _template.ToDiameter, minLimit),
                            MaxLimit = (_template.ToDiameter / 2) - 0.01, //Calculations.CalcChannel(_template.FromDiameter, _template.ToDiameter, (_template.ToDiameter / 2)) - 1,
                            MeasureSetup = _template,
                            IsDeadCellsCursor = true
                        });

                        _template.Cursors.Add(new Casy.Models.Cursor()
                        {
                            Name = "Cursor_VitalCells_Name",
                            MinLimit = (_template.ToDiameter / 2), //Calculations.CalcChannel(_template.FromDiameter, _template.ToDiameter, (_template.ToDiameter / 2)),
                            MaxLimit = _template.ToDiameter,//Calculations.CalcChannel(_template.FromDiameter, _template.ToDiameter, _template.ToDiameter),
                            MeasureSetup = _template
                        });
                    }
                }

                //return messageBoxWrapper.Result;
            });

            //task.Wait();
            //if (!task.Result) return false;
            //if (!messageBoxWrapper.Result) return false;

            
            //return true;
        }

        private void FillKnownToDiameters()
        {
            
            var result = new List<string>();
            if (this._calibrationController != null)
            {
                if (_template != null && _template.CapillarySize != 0)
                {
                    result.AddRange(_calibrationController.GetDiametersByCappillarySize(_template.CapillarySize).Select(size => size.ToString()));
                }
            }
            else
            {
                if(_template != null)
                {
                    result.Add(this._template.ToDiameter.ToString());
                }
            }
            
            Application.Current.Dispatcher.BeginInvoke((Action)delegate ()
            {
                this._uiProject.IgnoreUndoRedo = true;
                this.KnownToDiameters = new SmartCollection<string>(result);
                this._uiProject.IgnoreUndoRedo = false;
            });
        }

        private void OnTemplatePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CapillarySize")
            {
                FillKnownToDiameters();

                if (!string.IsNullOrEmpty(this.ToDiameter))
                {
                    if (!KnownToDiameters.Contains(this.ToDiameter))
                    {
                        this.ToDiameter = null;
                    }
                    else
                    {
                        NotifyOfPropertyChange("ToDiameter");
                    }
                }
                NotifyOfPropertyChange("CapillarySize");
            }
            else if (e.PropertyName == "ToDiameter")
            {
                NotifyOfPropertyChange("ToDiameter");
            }
            else
            {
                NotifyOfPropertyChange(e.PropertyName);
            }
        }

        private void SetDefaults()
        {
            if (this._template.CapillarySize == 150)
            {
                this.Volume = Volumes.FourHundred;
                this.Repeats = 3;
            }
        }
    }
}
