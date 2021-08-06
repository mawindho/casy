using OLS.Casy.Com.Api;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Config.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Core.Logging.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Models;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Base.ViewModels;
using OLS.Casy.Ui.Base.ViewModels.Wizard;
using OLS.Casy.Ui.Core.Api;
using OLS.Casy.Ui.MainControls.Api;
using OLS.Casy.Ui.MainControls.ViewModels.Wizard;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using MahApps.Metro.IconPacks;

namespace OLS.Casy.Ui.MainControls.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(ISettingsCategoryViewModel))]
    public class SettingsViewModel : ViewModelBase, ISettingsCategoryViewModel, IPartImportsSatisfiedNotification
    {
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly IConfigService _configService;
        private readonly IDatabaseStorageService _databaseStorageService;
        private readonly ILocalizationService _localizationService;
        private readonly IFolderBrowserDialogService _folderBrowserDialogService;
        private readonly IOpenFileDialogService _openFileDialogService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IBackupService _backupService;
        private readonly IMeasureResultManager _measureResultManager;
        private readonly ICompositionFactory _compositionFactory;
        private readonly IActivationService _activationService;
        private readonly ICasySerialPortDriver _serialPortDriver;
        private readonly ILogger _logger;
        private readonly IEnvironmentService _environmentService;

        private long _autoLogOffTime;
        private bool _showLastLoggedInUserName;
        private bool _isHighSecurityModeOn;

        private bool _isBackupEnabled;
        private string _backupPath;
        private long _backupInterval;
        private BackupInterval _backupIntervalInternal;

        private string _defaultBrowseLocation;
        private string _serialPort;
        private readonly List<AnnotationType> _removedAnnotationTypes;

        private List<Language> _languages;
        private string _selectedLanguage;
        private string _newAnnotationTypeName;
        private bool _showVirtualKeyboard;
        private bool _showMouseOverInGraph;
        private string _adGroupSupervisor;
        private string _adGroupOperator;
        private string _adGroupUser;
        private bool _isVisible = true;
        private bool _isActive;
        private ChevronState _chevronState = ChevronState.Hide;
        private bool _isSelectedState;
        private bool _showProbeName;
        private bool _isWeeklyCleanMandatory;
        private int _weeklyCleanNotificationDuration;
        private bool _isSystemFormatChecked;
        private bool _isCustomFormatChecked;
        private string _customFormat;

        [ImportingConstructor]
        public SettingsViewModel(IEventAggregatorProvider eventAggregatorProvider,
            IConfigService configService,
            IDatabaseStorageService databaseStorageService,
            ILocalizationService localizationService,
            IFolderBrowserDialogService folderBrowserDialogService,
            IOpenFileDialogService openFileDialogService,
            IAuthenticationService authenticationService,
            IBackupService backupService,
            IMeasureResultManager measureResultManager,
            ICompositionFactory compositionFactory,
            IActivationService activationService,
            ILogger logger,
            IEnvironmentService environmentService,
            [Import(AllowDefault = true)] ICasySerialPortDriver serialPortDriver)
        {
            _eventAggregatorProvider = eventAggregatorProvider;
            _configService = configService;
            _databaseStorageService = databaseStorageService;
            _localizationService = localizationService;
            _folderBrowserDialogService = folderBrowserDialogService;
            _openFileDialogService = openFileDialogService;
            _authenticationService = authenticationService;
            _backupService = backupService;
            _measureResultManager = measureResultManager;
            _compositionFactory = compositionFactory;
            _activationService = activationService;
            _serialPortDriver = serialPortDriver;
            _logger = logger;
            _environmentService = environmentService;

            BackupIntervals = new ObservableCollection<ComboBoxItemWrapperViewModel<BackupInterval>>();
            AnnotationTypes = new ObservableCollection<Tuple<string, string, AnnotationType>>();
            AutoLogoffIntervals = new List<ComboBoxItemWrapperViewModel<long>>();
            SerialPorts = new ObservableCollection<ComboBoxItemWrapperViewModel<string>>();
            WeeklyCleanNotificationDurations = new ObservableCollection<ComboBoxItemWrapperViewModel<int>>();

            _removedAnnotationTypes = new List<AnnotationType>();
        }

        public IEnumerable<Language> Languages => _languages;

        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (value == _selectedLanguage) return;
                _selectedLanguage = value;
                NotifyOfPropertyChange();
            }
        }

        public List<ComboBoxItemWrapperViewModel<long>> AutoLogoffIntervals { get; }

        [ConfigItem(10)]
        public long AutoLogOffTime
        {
            get => _autoLogOffTime;
            set
            {
                _autoLogOffTime = value;
                NotifyOfPropertyChange();
            }
        }

        [ConfigItem(true)]
        public bool ShowLastLoggedInUserName
        {
            get => _showLastLoggedInUserName;
            set
            {
                _showLastLoggedInUserName = value;
                NotifyOfPropertyChange();
            }
        }

        [ConfigItem(false)]
        public bool IsHighSecurityModeOn
        {
            get => _isHighSecurityModeOn;
            set
            {
                _isHighSecurityModeOn = value;
                NotifyOfPropertyChange();
            }
        }

        [ConfigItem(false)]
        public bool IsBackupEnabled
        {
            get => _isBackupEnabled;
            set
            {
                _isBackupEnabled = value;
                NotifyOfPropertyChange();
            }
        }

        [ConfigItem("")]
        public string BackupPath
        {
            get => _backupPath;
            set
            {
                _backupPath = value;
                NotifyOfPropertyChange();
            }
        }

        [ConfigItem(0)]
        public long BackupInterval
        {
            get => _backupInterval;
            set
            {
                _backupInterval = value;
                NotifyOfPropertyChange();
            }
        }

        [ConfigItem(true)]
        public bool ShowMouseOverInGraph
        {
            get => _showMouseOverInGraph;
            set
            {
                _showMouseOverInGraph = value;
                NotifyOfPropertyChange();
            }
        }

        public BackupInterval BackupIntervalInternal
        {
            get => _backupIntervalInternal;
            set
            {
                _backupIntervalInternal = value;
                NotifyOfPropertyChange();

                BackupInterval = (int)value;
            }
        }

        [ConfigItem("")]
        public string DefaultBrowseLocation
        {
            get => _defaultBrowseLocation;
            set
            {
                _defaultBrowseLocation = value;
                NotifyOfPropertyChange();
            }
        }

        public ObservableCollection<ComboBoxItemWrapperViewModel<string>> SerialPorts { get; }

        [ConfigItem("Auto")]
        public string SerialPort
        {
            get => _serialPort;
            set
            {
                _serialPort = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsSystemFormatChecked
        {
            get => _isSystemFormatChecked;
            set
            {
                _isSystemFormatChecked = value;
                NotifyOfPropertyChange();

                if (_isSystemFormatChecked)
                {
                    IsCustomFormatChecked = !value;
                }
            }
        }

        public bool IsCustomFormatChecked
        {
            get => _isCustomFormatChecked;
            set
            {
                _isCustomFormatChecked = value;
                NotifyOfPropertyChange();

                if (_isCustomFormatChecked)
                {
                    IsSystemFormatChecked = !value;
                    CustomFormat = "dd.MM.yyyy HH:mm:ss \"UTC\"zzz";
                }
            }
        }

        public string CustomFormat
        {
            get => _customFormat;
            set
            {
                _customFormat = value;
                NotifyOfPropertyChange();
            }
        }

        public ICommand SelectDefaultBrowseLocationCommand => new OmniDelegateCommand(OnSelectDefaultBrowseLocation);

        public ObservableCollection<Tuple<string, string, AnnotationType>> AnnotationTypes { get; }

        public string NewAnnotationType
        {
            get => _newAnnotationTypeName;
            set
            {
                if (value == _newAnnotationTypeName) return;
                _newAnnotationTypeName = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange("CanSubmitAnnotationType");
            }
        }

        public ICommand SubmitNewAnnotationTypeCommand => new OmniDelegateCommand(OnSubmitNewAnnotationType);

        public bool CanSubmitAnnotationType
        {
            get
            {
                var regexItem = new Regex("^[a-zA-Z0-9]*$");
                return !string.IsNullOrEmpty(NewAnnotationType) && AnnotationTypes.All(a => a.Item2 != NewAnnotationType) && regexItem.IsMatch(NewAnnotationType);
            }
        }

        private void OnSubmitNewAnnotationType()
        {
            if (!CanSubmitAnnotationType) return;
            AnnotationTypes.Add(new Tuple<string, string, AnnotationType>((AnnotationTypes.Count + 1).ToString(), NewAnnotationType, new AnnotationType
            {
                AnnotationTypeId = -1,
                AnnottationTypeName = NewAnnotationType
            }));

            NewAnnotationType = string.Empty;
        }

        public ICommand DeleteAnnotationTypeCommand => new OmniDelegateCommand<AnnotationType>(OnDeleteAnnotation);

        private void OnDeleteAnnotation(AnnotationType annotationType)
        {
            var toRemove = AnnotationTypes.FirstOrDefault(at => at.Item3 == annotationType);
            if (toRemove == null) return;
            AnnotationTypes.Remove(toRemove);
            _removedAnnotationTypes.Add(toRemove.Item3);
        }

        public ObservableCollection<ComboBoxItemWrapperViewModel<BackupInterval>> BackupIntervals { get; }

        public ICommand SelectBackupPathCommand => new OmniDelegateCommand(OnSelectBackupPath);

        public ICommand RestoreBackupCommand
        {
            get { return new OmniDelegateCommand(async() => await OnRestoreBackup()); }
        }

        public bool ShowVirtualKeyboard
        {
            get => _showVirtualKeyboard;
            set
            {
                if (value == _showVirtualKeyboard) return;
                _showVirtualKeyboard = value;

                Task.Factory.StartNew(() =>
                {
                    var awaiter = new ManualResetEvent(false);

                    var showMessageBoxEventWrapper = new ShowMessageBoxDialogWrapper
                    {
                        Awaiter = awaiter,
                        Message = "ChangeVirtualKeyboardDialog_Content",
                        Title = "ChangeVirtualKeyboardDialog_Title"
                    };

                    _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>().Publish(showMessageBoxEventWrapper);

                    if (!awaiter.WaitOne()) return;
                    var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    config.AppSettings.Settings["UseTipTap"].Value = _showVirtualKeyboard ? "True" : "False";
                    config.Save(ConfigurationSaveMode.Full);
                    ConfigurationManager.RefreshSection("appSettings");
                });
            }
        }

        public bool IsAdAuthSectionVisible => _authenticationService.LoggedInUser.UserRole.Priority > 2 && _activationService.IsModuleEnabled("adAuth");

        public bool IsLocalAuthSectionVisible => _authenticationService.LoggedInUser.UserRole.Priority > 2 && _activationService.IsModuleEnabled("localAuth");

        public bool IsComModuleEnabled => _authenticationService.LoggedInUser.UserRole.Priority > 2 && _activationService.IsModuleEnabled("control");

        public bool IsCFRVisible => _activationService.IsModuleEnabled("cfr") && _authenticationService.LoggedInUser.UserRole.Priority > 2;

        public bool ShowProbeName
        {
            get => _showProbeName;
            set
            {
                _showProbeName = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsWeeklyCleanMandatory
        {
            get => _isWeeklyCleanMandatory;
            set
            {
                _isWeeklyCleanMandatory = value;
                NotifyOfPropertyChange();
            }
        }

        public ObservableCollection<ComboBoxItemWrapperViewModel<int>> WeeklyCleanNotificationDurations { get; set; }

        public int WeeklyCleanNotificationDuration
        {
            get => _weeklyCleanNotificationDuration;
            set
            {
                _weeklyCleanNotificationDuration = value;
                NotifyOfPropertyChange();
            }
        }

        public string AdGroupUser
        {
            get => _adGroupUser;
            set
            {
                if (value == _adGroupUser) return;
                _adGroupUser = value;
                NotifyOfPropertyChange();
            }
        }

        public string AdGroupOperator
        {
            get => _adGroupOperator;
            set
            {
                if (value == _adGroupOperator) return;
                _adGroupOperator = value;
                NotifyOfPropertyChange();
            }
        }

        public string AdGroupSupervisor
        {
            get => _adGroupSupervisor;
            set
            {
                if (value == _adGroupSupervisor) return;
                _adGroupSupervisor = value;
                NotifyOfPropertyChange();
            }
        }

        public ICommand CleanupSystemLogCommand => new OmniDelegateCommand<string>(days => CleanupSystemLog(int.Parse(days)));

        private async void CleanupSystemLog(int days)
        {
            var confirmationResult = await Task.Factory.StartNew<bool>(() =>
            {
                var awaiter = new ManualResetEvent(false);

                var showMessageBoxEventWrapper = new ShowMessageBoxDialogWrapper
                {
                    Awaiter = awaiter,
                    Message = "DeleteSystemLogMessageBox_Content",
                    Title = "DeleteSystemLogMessageBox_Title"
                };

                _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>().Publish(showMessageBoxEventWrapper);

                if (awaiter.WaitOne())
                {
                    return showMessageBoxEventWrapper.Result;
                }
                return false;
            });

            if(confirmationResult)
            {
                _logger.CleanupSystemLogEntries(days);
            }
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

        public UserRole MinRequiredRole => _authenticationService.GetRoleByName("User");

        public PackIconFontAwesomeKind Glyph => PackIconFontAwesomeKind.CogSolid;

        public string Name => "General";

        public int Order => 0;

        public ChevronState ChevronState
        {
            get => _chevronState;
            set
            {
                if (value == _chevronState) return;
                _chevronState = value;
                NotifyOfPropertyChange();
            }
        }

        public ICommand SelectCommand => new OmniDelegateCommand(OnSelected);
        private void OnSelected()
        {
            IsActive = true;
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (value == _isActive) return;
                _isActive = value;
                IsSelectedState = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsSelectedState
        {
            get => _isSelectedState;
            set
            {
                if (value == _isSelectedState) return;
                IsActive = value;
                _isSelectedState = value;
                NotifyOfPropertyChange();
            }
        }

        public void OnImportsSatisfied()
        {
            _configService.InitializeByConfiguration(this);
            _localizationService.LanguageChanged += OnLanguageChanged;

            BackupIntervalInternal = (BackupInterval)((int)BackupInterval);

            var annotationTypes = _databaseStorageService.GetAnnotationTypes();
            for(var i = 0; i < annotationTypes.Count(); i++)
            {
                var annotationType = annotationTypes.ElementAt(i);
                AnnotationTypes.Add(new Tuple<string, string, AnnotationType>($"{i + 1}.", annotationType.AnnottationTypeName, annotationType));
            }

            var backupIntervalNames = Enum.GetNames(typeof(BackupInterval));
            foreach(var backupIntervalName in backupIntervalNames)
            {
                var comboBoxWrapperItem =
                    new ComboBoxItemWrapperViewModel<BackupInterval>(
                        (BackupInterval) Enum.Parse(typeof(BackupInterval), backupIntervalName))
                    {
                        DisplayItem = _localizationService.GetLocalizedString($"BackupInterval_{backupIntervalName}_Name")
                    };
                BackupIntervals.Add(comboBoxWrapperItem);
            }

            _languages = _localizationService.PossibleLanguages.Select(language => new Language
            {
                Name = language.Name,
                NativeName = language.NativeName,
                Flag = $"{language.Name.Replace("-", "")}Icon"
            }).ToList();

            if (IsComModuleEnabled)
            {
                SerialPorts.Add(new ComboBoxItemWrapperViewModel<string>("Auto")
                {
                    DisplayItem = "Auto"
                });
                var comPorts = _serialPortDriver.SerialPorts;
                foreach (var comPort in comPorts)
                {
                    SerialPorts.Add(new ComboBoxItemWrapperViewModel<string>(comPort)
                    {
                        DisplayItem = comPort
                    });
                }
            }

            SelectedLanguage = _authenticationService.LoggedInUser.CountryRegionName;

            AutoLogoffIntervals.Add(new ComboBoxItemWrapperViewModel<long>(0)
            {
                DisplayItem = _localizationService.GetLocalizedString("SettingsView_AutoLogoff_Label_Never")
            });
            AutoLogoffIntervals.Add(new ComboBoxItemWrapperViewModel<long>(1)
            {
                DisplayItem = $"1 {_localizationService.GetLocalizedString("SettingsView_AutoLogoff_Label_Minute")}"
            });
            AutoLogoffIntervals.Add(new ComboBoxItemWrapperViewModel<long>(2)
            {
                DisplayItem = $"2 {_localizationService.GetLocalizedString("SettingsView_AutoLogoff_Label_Minute")}"
            });
            AutoLogoffIntervals.Add(new ComboBoxItemWrapperViewModel<long>(3)
            {
                DisplayItem = $"3 {_localizationService.GetLocalizedString("SettingsView_AutoLogoff_Label_Minute")}"
            });
            AutoLogoffIntervals.Add(new ComboBoxItemWrapperViewModel<long>(5)
            {
                DisplayItem = $"5 {_localizationService.GetLocalizedString("SettingsView_AutoLogoff_Label_Minute")}"
            });
            AutoLogoffIntervals.Add(new ComboBoxItemWrapperViewModel<long>(10)
            {
                DisplayItem = $"10 {_localizationService.GetLocalizedString("SettingsView_AutoLogoff_Label_Minute")}"
            });
            AutoLogoffIntervals.Add(new ComboBoxItemWrapperViewModel<long>(15)
            {
                DisplayItem = $"15 {_localizationService.GetLocalizedString("SettingsView_AutoLogoff_Label_Minute")}"
            });
            AutoLogoffIntervals.Add(new ComboBoxItemWrapperViewModel<long>(30)
            {
                DisplayItem = $"30 {_localizationService.GetLocalizedString("SettingsView_AutoLogoff_Label_Minute")}"
            });
            AutoLogoffIntervals.Add(new ComboBoxItemWrapperViewModel<long>(60)
            {
                DisplayItem = $"60 {_localizationService.GetLocalizedString("SettingsView_AutoLogoff_Label_Minute")}"
            });
            AutoLogoffIntervals.Add(new ComboBoxItemWrapperViewModel<long>(120)
            {
                DisplayItem = $"120 {_localizationService.GetLocalizedString("SettingsView_AutoLogoff_Label_Minute")}"
            });

            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            _showVirtualKeyboard = config.AppSettings.Settings["UseTipTap"].Value == "True";
            NotifyOfPropertyChange("ShowVirtualKeyboard");

            var settings = _databaseStorageService.GetSettings();
            if (IsAdAuthSectionVisible)
            {
                _adGroupUser = !settings.TryGetValue("AdGroupUser", out var userGroupSetting) ? "CASY-User" : userGroupSetting.Value;
                _adGroupOperator = !settings.TryGetValue("AdGroupOperator", out var operatorGroupSetting) ? "CASY-Operator" : operatorGroupSetting.Value;
                _adGroupSupervisor = !settings.TryGetValue("AdGroupSupervisor", out var superVisorGroupSetting) ? "CASY-Supervisor" : superVisorGroupSetting.Value;
            }

            if(IsCFRVisible)
            {
                _showProbeName = !settings.TryGetValue("ShowProbeName", out var showProbeNameSetting) ? false : showProbeNameSetting.Value == "true";
                _isWeeklyCleanMandatory = !settings.TryGetValue("WeeklyCleanMandatory", out var weeklyCleanMandatorySetting) ? false : weeklyCleanMandatorySetting.Value == "true";
                _weeklyCleanNotificationDuration = !settings.TryGetValue("WeeklyCleanNotificationDuration", out var weeklyCleanNotificationDurationSetting) ? 24 : int.Parse(weeklyCleanNotificationDurationSetting.Value);
            }

            WeeklyCleanNotificationDurations.Add(new ComboBoxItemWrapperViewModel<int>(12)
            {
                DisplayItem = $"12 {_localizationService.GetLocalizedString("SettingsView_Hours")}"
            });
            WeeklyCleanNotificationDurations.Add(new ComboBoxItemWrapperViewModel<int>(24)
            {
                DisplayItem = $"24 {_localizationService.GetLocalizedString("SettingsView_Hours")}"
            });

            _isSystemFormatChecked = !settings.TryGetValue("DateTimeFormat", out var dateTimeFormatSetting) || dateTimeFormatSetting.Value == "System";
            _isCustomFormatChecked = !_isSystemFormatChecked;
            _customFormat = !settings.TryGetValue("DateTimeFormat", out var dateTimeFormatSetting2) ? "System" : dateTimeFormatSetting2.Value;
            NotifyOfPropertyChange("IsSystemFormatChecked");
            NotifyOfPropertyChange("IsCustomFormatChecked");
            NotifyOfPropertyChange("CustomFormat");
        }

        private void OnLanguageChanged(object sender, LocalizationEventArgs e)
        {
            foreach (var backupInterval in BackupIntervals)
            {
                backupInterval.DisplayItem = _localizationService.GetLocalizedString(
                    $"BackupInterval_{Enum.GetName(typeof(BackupInterval), backupInterval.ValueItem)}_Name");
            }
        }

        public bool CanOk
        {
            get
            {
                var result = new List<ValidationResult>();
                return Validator.TryValidateObject(this, new ValidationContext(this, null, null), result);
            }
        }

        public void OnOk()
        {

            if (!CanOk) return;

            _authenticationService.LoggedInUser.CountryRegionName = _selectedLanguage;
            _authenticationService.SaveUser(this._authenticationService.LoggedInUser);

            _localizationService.CurrentCulture = new CultureInfo(_selectedLanguage);

            var models = new List<ConfigItemModel>
            {
                new ConfigItemModel {Name = "AutoLogOffTime", Value = AutoLogOffTime},
                new ConfigItemModel {Name = "ShowLastLoggedInUserName", Value = ShowLastLoggedInUserName},
                new ConfigItemModel {Name = "IsHighSecurityModeOn", Value = IsHighSecurityModeOn},
                new ConfigItemModel {Name = "IsBackupEnabled", Value = IsBackupEnabled},
                new ConfigItemModel {Name = "BackupPath", Value = BackupPath},
                new ConfigItemModel {Name = "BackupInterval", Value = BackupInterval},
                new ConfigItemModel
                {
                    Name = "LastBackupTime", Value = _configService.GetConfigItemValue<long>("LastBackupTime")
                },
                new ConfigItemModel {Name = "DefaultBrowseLocation", Value = DefaultBrowseLocation},
                new ConfigItemModel {Name = "SerialPort", Value = SerialPort},
                new ConfigItemModel {Name = "ShowMouseOverInGraph", Value = ShowMouseOverInGraph}
            };
            _configService.UpdateConfiguration(models);

            foreach (var (_, item2, item3) in AnnotationTypes)
            {
                if(item3.AnnotationTypeId == -1)
                {
                    _logger.Info(Casy.Models.Enums.LogCategory.General, $"Annotation Type '{item2}' has been created.");
                }
                item3.AnnottationTypeName = item2;
                _databaseStorageService.SaveAnnotationType(item3);
            }

            foreach(var annotationType in _removedAnnotationTypes)
            {
                _logger.Info(Casy.Models.Enums.LogCategory.General, $"Annotation Type '{annotationType}' has been deleted.");
                _databaseStorageService.DeleteAnnotationType(annotationType);
            }
            _removedAnnotationTypes.Clear();

            if (IsAdAuthSectionVisible)
            {
                _databaseStorageService.SaveSetting("AdGroupUser", AdGroupUser);
                _databaseStorageService.SaveSetting("AdGroupOperator", AdGroupOperator);
                _databaseStorageService.SaveSetting("AdGroupSupervisor", AdGroupSupervisor);
            }

            if (IsCFRVisible)
            {
                _databaseStorageService.SaveSetting("ShowProbeName", ShowProbeName ? "true" : "false");
                _databaseStorageService.SaveSetting("WeeklyCleanMandatory", IsWeeklyCleanMandatory ? "true" : "false");
                _databaseStorageService.SaveSetting("WeeklyCleanNotificationDuration", WeeklyCleanNotificationDuration.ToString());
            }

            _databaseStorageService.SaveSetting("DateTimeFormat", IsSystemFormatChecked ? "System" : CustomFormat);
            _environmentService.SetEnvironmentInfo("DateTimeFormat", IsSystemFormatChecked ? "System" : CustomFormat);

            _eventAggregatorProvider.Instance.GetEvent<ConfigurationChangedEvent>().Publish();
        }

        public void OnCancel()
        {
            _configService.InitializeByConfiguration(this);
        }

        private void OnSelectBackupPath()
        {
            _folderBrowserDialogService.Description = _localizationService.GetLocalizedString("SettingsView_BackupSection_SelectPathDialog_Description");
            _folderBrowserDialogService.ShowNewFolderButton = true;
            _folderBrowserDialogService.RootFolder = Environment.SpecialFolder.MyComputer;

            if(!string.IsNullOrEmpty(DefaultBrowseLocation))
            {
                _folderBrowserDialogService.SelectedPath = DefaultBrowseLocation;
            }
            var result = _folderBrowserDialogService.ShowDialog();
            if (result.HasValue && result.Value)
            {
                BackupPath = _folderBrowserDialogService.SelectedPath;
            }
        }

        private async Task OnRestoreBackup()
        {
            await Task.Factory.StartNew(async () =>
            {
                var success = await _measureResultManager.SaveChangedMeasureResults();

                if (success != ButtonResult.Cancel)
                {
                    var awaiter = new ManualResetEvent(false);

                    var viewModelExport = _compositionFactory.GetExport<IWizardContainerViewModel>();
                    var viewModel = viewModelExport.Value;

                    var initialWizardStepViewModel = new StandardWizardStepViewModel
                    {
                        PrimaryHeader = _localizationService.GetLocalizedString("RestoreBackupWizard_initialStep_PrimaryHeader"),
                        PrimaryText = _localizationService.GetLocalizedString("RestoreBackupWizard_initialStep_PrimaryText")
                    };

                    viewModel.AddWizardStepViewModel(initialWizardStepViewModel);

                    var selectBackupStepViewModel = new SelectBackupWizardStepViewModel(_openFileDialogService,
                        _localizationService, _defaultBrowseLocation)
                    {
                        Header = _localizationService.GetLocalizedString("RestoreBackupWizard_SelectBackup_Header"),
                        Text = _localizationService.GetLocalizedString("RestoreBackupWizard_SelectBackup_Text"),
                        NextButtonText = "Restore"
                    };
                    selectBackupStepViewModel.NextButtonPressedAction = () =>
                    {
                        if (string.IsNullOrEmpty(selectBackupStepViewModel.RestoreBackupPath)) return false;
                        _backupService.RestoreBackup(selectBackupStepViewModel.RestoreBackupPath);
                        _eventAggregatorProvider.Instance.GetEvent<MeasureResultsDeletedEvent>().Publish();

                        return true;
                    };
                    
                    viewModel.AddWizardStepViewModel(selectBackupStepViewModel);

                    var finalWizardStepViewModel = new StandardWizardStepViewModel
                    {
                        PrimaryHeader = _localizationService.GetLocalizedString("RestoreBackupWizard_Finish_Header"),
                        PrimaryText = _localizationService.GetLocalizedString("RestoreBackupWizard_Finish_Text"),
                        NextButtonText = _localizationService.GetLocalizedString("DeepCleanWizard_Button_Finish"),
                        IsCancelButtonVisible = false
                    };

                    viewModel.AddWizardStepViewModel(finalWizardStepViewModel);

                    viewModel.WizardTitle = "RestoreBackupWizard_Title";

                    var titleBinding = new Binding("Title") {Source = viewModel};

                    var wrapper = new ShowCustomDialogWrapper
                    {
                        Awaiter = awaiter,
                        TitleBinding = titleBinding,
                        DataContext = viewModel,
                        DialogType = typeof(IWizardContainerDialog)
                    };

                    _eventAggregatorProvider.Instance.GetEvent<ShowCustomDialogEvent>().Publish(wrapper);
                    if (awaiter.WaitOne())
                    {
                    }
                    _compositionFactory.ReleaseExport(viewModelExport);
                }
            });
        }

        private void OnSelectDefaultBrowseLocation()
        {
            _folderBrowserDialogService.Description = _localizationService.GetLocalizedString("SettingsView_BrowseLocationSection_SelectPathDialog_Description");
            _folderBrowserDialogService.ShowNewFolderButton = true;
            _folderBrowserDialogService.RootFolder = Environment.SpecialFolder.MyComputer;
            var result = _folderBrowserDialogService.ShowDialog();
            if (result.HasValue && result.Value)
            {
                DefaultBrowseLocation = _folderBrowserDialogService.SelectedPath;
            }
        }
    }
}
