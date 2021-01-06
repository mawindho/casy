using DevExpress.Mvvm;
using OLS.Casy.Controller.Api;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Models;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Core.Api;
using OLS.Casy.Ui.MainControls.Api;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace OLS.Casy.Ui.MainControls.ViewModels
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IDashboardPageViewModel))]
    public class ShortcutsDashboardPageViewModel : Base.ViewModelBase, IDashboardPageViewModel, IPartImportsSatisfiedNotification
    {
        private readonly ILocalizationService _localizationService;
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly IAuthenticationService _authenticationService;
        private readonly IDatabaseStorageService _databaseStorageService;
        private readonly IMeasureController _measureController;
        private readonly IServiceController _serviceController;
        private readonly IMeasureResultManager _measureResultManager;
        private readonly IMeasureCounter _measureCounter;
        private readonly IEnvironmentService _environmentService;

        private readonly ObservableCollection<ShortcutViewModel> _shortcutViewModels;
        private readonly ObservableCollection<ShortcutViewModel> _favoriteTemplateViewModels;

        [ImportingConstructor]
        public ShortcutsDashboardPageViewModel(ILocalizationService localizationService,
            IEventAggregatorProvider eventAggregatorProvider,
            [Import(AllowDefault = true)]IMeasureController measureController,
            IAuthenticationService authenticationService,
            IDatabaseStorageService databaseStorageService,
            [Import(AllowDefault = true)] IServiceController serviceController,
            IMeasureResultManager measureResultManager,
            [Import(AllowDefault = true)] IMeasureCounter measureCounter,
            IEnvironmentService environmentService)
        {
            this._shortcutViewModels = new ObservableCollection<ShortcutViewModel>();
            this._favoriteTemplateViewModels = new ObservableCollection<ShortcutViewModel>();

            this._measureController = measureController;
            this._localizationService = localizationService;
            this._eventAggregatorProvider = eventAggregatorProvider;
            this._authenticationService = authenticationService;
            this._databaseStorageService = databaseStorageService;
            this._serviceController = serviceController;
            this._measureResultManager = measureResultManager;
            this._measureCounter = measureCounter;
            this._environmentService = environmentService;
        }

        public int Order => 0;

        public string Test
        {
            get { return "Shortcuts"; }
        }

        public ObservableCollection<ShortcutViewModel> ShortcutViewModels
        {
            get { return _shortcutViewModels; }
        }

        public ObservableCollection<ShortcutViewModel> FavoriteTemplateViewModels
        {
            get { return _favoriteTemplateViewModels; }
        }

        public bool IsMeasureControllerLoaded
        {
            get { return _measureController != null; }
        }

        public ICommand AnalyzeCommand
        {
            get { return new OmniDelegateCommand(OnAnalyze); }
        }

        public ICommand MeasureCommand
        {
            get { return new OmniDelegateCommand(OnMeasure); }
        }

        public ICommand DataCommand
        {
            get { return new OmniDelegateCommand(OnData); }
        }

        public ICommand TemplatesCommand
        {
            get { return new OmniDelegateCommand(OnTemplates); }
        }

        public ICommand RecentTemplateCommand
        {
            get { return new OmniDelegateCommand<object>(OnRecentTemplate); }
        }

        public ICommand PurgeCommand
        {
            get { return new OmniDelegateCommand(OnPurge); }
        }

        public ICommand BackgroundCommand
        {
            get { return new OmniDelegateCommand(OnBackground); }
        }

        public void OnImportsSatisfied()
        {
            _localizationService.LanguageChanged += OnLanguageChanged;
            _authenticationService.UserLoggedIn += (s,e) => OnLanguageChanged(null, null);
            _authenticationService.UserLoggedOut += OnUserLoggedOut;

            this._eventAggregatorProvider.Instance.GetEvent<ConfigurationChangedEvent>().Subscribe(OnConfigurationChangedEvent);
            this._eventAggregatorProvider.Instance.GetEvent<TemplateSavedEvent>().Subscribe(OnTemplateSaved);

            if (this._measureCounter != null)
            {
                this._measureCounter.AvailableCountsChanged += OnCountsChanged;
            }
            if(_serviceController != null)
            {
                _serviceController.WeeklyCleanMandatoryChangedEvent += OnCountsChanged;
            }

            this._environmentService.EnvironmentInfoChangedEvent += OnEnvironmentInfoChanged;

            OnLanguageChanged(null, null);
        }

        private void OnTemplateSaved(MeasureSetup obj)
        {
            OnUserLoggedIn(null, null);
        }

        private void OnEnvironmentInfoChanged(object sender, string e)
        {
            if(e == "IsCasyConnected")
            {
                var measureShortCut = _shortcutViewModels.FirstOrDefault(s => s.Order == 0);
                if (measureShortCut != null)
                {
                    measureShortCut.IsCasyConnected = this._measureController != null && (bool)_environmentService.GetEnvironmentInfo("IsCasyConnected");
                    if(!measureShortCut.IsCasyConnected)
                    {
                        measureShortCut.Header2 = this._localizationService.GetLocalizedString("TopMenuView_NoCasyConnected");
                    }
                    else
                    {
                        measureShortCut.Header2 = this._measureCounter == null ? string.Empty : _localizationService.GetLocalizedString("TopMenuView_CountsLeft", _measureCounter.GetAvailableCounts().ToString());
                    }
                }

                var purgeShortCut = _shortcutViewModels.FirstOrDefault(s => s.Order == 1);
                if (purgeShortCut != null)
                {
                    purgeShortCut.IsCasyConnected = this._measureController != null && (bool)_environmentService.GetEnvironmentInfo("IsCasyConnected");
                }

                var backgroundShortCut = _shortcutViewModels.FirstOrDefault(s => s.Order == 2);
                if (backgroundShortCut != null)
                {
                    backgroundShortCut.IsCasyConnected = this._measureController != null && (bool)_environmentService.GetEnvironmentInfo("IsCasyConnected");
                }

                foreach(var fav in this._favoriteTemplateViewModels)
                {
                    fav.IsCasyConnected = this._measureController != null && (bool)_environmentService.GetEnvironmentInfo("IsCasyConnected");
                }
            }
        }

        private void OnCountsChanged(object sender, EventArgs e)
        {
            var measureShortCut = _shortcutViewModels.FirstOrDefault(s => s.Order == 0);
            if(measureShortCut != null)
            {
                if (!measureShortCut.IsCasyConnected)
                {
                    measureShortCut.Header2 = this._localizationService.GetLocalizedString("TopMenuView_NoCasyConnected");
                }
                else
                {
                    measureShortCut.Header2 = this._measureCounter == null ? string.Empty : _localizationService.GetLocalizedString("TopMenuView_CountsLeft", _measureCounter.GetAvailableCounts().ToString());
                }
                //measureShortCut.Header2 = this._measureCounter == null ? string.Empty : _localizationService.GetLocalizedString("TopMenuView_CountsLeft", _measureCounter.GetAvailableCounts().ToString());
                measureShortCut.IsOrange = HasLessCounts;
                measureShortCut.IsRed = !CanMeasure;
            }

            foreach (var fav in this._favoriteTemplateViewModels)
            {
                fav.IsOrange = HasLessCounts;
                fav.IsRed = !CanMeasure;
            }
        }

        private void OnConfigurationChangedEvent()
        {
            OnUserLoggedIn(null, null);
        }

        private void OnUserLoggedOut(object sender, AuthenticationEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var toRemoves = new List<ShortcutViewModel>(this._favoriteTemplateViewModels);
                foreach (var item in toRemoves)
                {
                    _favoriteTemplateViewModels.Remove(item);
                }
            });
        }

        private void OnUserLoggedIn(object sender, AuthenticationEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                OnUserLoggedOut(null, null);

                if (_authenticationService.LoggedInUser != null)
                {
                    var isCasyConnected = _environmentService.GetEnvironmentInfo("IsCasyConnected");

                    int order = 0;
                    foreach (var id in _authenticationService.LoggedInUser.FavoriteTemplateIds.Distinct())
                    {
                        var template = _databaseStorageService.GetMeasureSetup(id);

                        if (template != null)
                        {
                            var viewModel = new ShortcutViewModel()
                            {
                                Header = template.Name,
                                Command = RecentTemplateCommand,
                                CommandParameter = template,
                                Order = order++,
                                IsOrange = HasLessCounts,
                                IsRed = !CanMeasure,
                                IsCasyConnected = this._measureController != null && isCasyConnected != null && (bool)isCasyConnected
                            };

                            this._favoriteTemplateViewModels.Add(viewModel);
                        }
                    }
                }
            });
        }

        public bool HasLessCounts
        {
            get { return this._measureCounter == null ? false : _measureCounter.CountsLeft <= 100 && _measureCounter.CountsLeft > 0; }
        }

        public bool HasNoCounts
        {
            get { return this._measureCounter == null ? false : _measureCounter.CountsLeft <= 0; }
        }

        public bool CanMeasure
        {
            get
            {
                return !HasNoCounts && !_serviceController.IsWeeklyCleanMandatory;
            }
        }


        private void OnLanguageChanged(object sender, LocalizationEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            { 
            var toRemoves = this._shortcutViewModels.ToArray();
            _shortcutViewModels.Clear();

            foreach(var toRemove in toRemoves)
            {
                toRemove.Dispose();
            }

            if (_serviceController != null)
            {
                var isCasyConnected = _environmentService.GetEnvironmentInfo("IsCasyConnected");

                var measureShortCut = new ShortcutViewModel()
                {
                    Header = _localizationService.GetLocalizedString("ShortcutDashboardPageView_ShortCut_Measure"),
                    ImagePath = "icon_measure",
                    Command = this.MeasureCommand,
                    Order = 0,
                    MinRequiredRole = _authenticationService.RolesList.FirstOrDefault(role => role.Name == "User"),
                //Header2 = this._measureCounter == null ? string.Empty : _localizationService.GetLocalizedString("TopMenuView_CountsLeft", _measureCounter.GetAvailableCounts().ToString()),
                    IsOrange = HasLessCounts,
                    IsRed = !CanMeasure,
                    IsCasyConnected = this._measureController != null && isCasyConnected != null && (bool) isCasyConnected
                };

                if (!measureShortCut.IsCasyConnected)
                {
                    measureShortCut.Header2 = this._localizationService.GetLocalizedString("TopMenuView_NoCasyConnected");
                }
                else
                {
                    measureShortCut.Header2 = this._measureCounter == null ? string.Empty : _localizationService.GetLocalizedString("TopMenuView_CountsLeft", _measureCounter.GetAvailableCounts().ToString());
                }

                _shortcutViewModels.Add(measureShortCut);

                _shortcutViewModels.Add(new ShortcutViewModel()
                {
                    Header = _localizationService.GetLocalizedString("ShortcutDashboardPageView_ShortCut_Clean"),
                    ImagePath = "icon_purge",
                    Command = this.PurgeCommand,
                    Order = 1,
                    MinRequiredRole = _authenticationService.RolesList.FirstOrDefault(role => role.Name == "User"),
                    IsCasyConnected = this._measureController != null && isCasyConnected != null && (bool)isCasyConnected
                });

                _shortcutViewModels.Add(new ShortcutViewModel()
                {
                    Header = _localizationService.GetLocalizedString("ShortcutDashboardPageView_ShortCut_Background"),
                    ImagePath = "icon_background",
                    Command = this.BackgroundCommand,
                    Order = 2,
                    MinRequiredRole = _authenticationService.RolesList.FirstOrDefault(role => role.Name == "User"),
                    IsCasyConnected = this._measureController != null && isCasyConnected != null && (bool)isCasyConnected
                });
            }

            _shortcutViewModels.Add(new ShortcutViewModel()
            {
                Header = _localizationService.GetLocalizedString("ShortcutDashboardPageView_ShortCut_Analyze"),
                ImagePath = "icon_analyze",
                Command = this.AnalyzeCommand,
                Order = 3,
                MinRequiredRole = _authenticationService.RolesList.FirstOrDefault(role => role.Name == "User"),
                IsCasyConnected = true
            });

            _shortcutViewModels.Add(new ShortcutViewModel()
            {
                Header = _localizationService.GetLocalizedString("ShortcutDashboardPageView_ShortCut_Data"),
                ImagePath = "icon_data",
                Command = this.DataCommand,
                Order = 4,
                MinRequiredRole = _authenticationService.RolesList.FirstOrDefault(role => role.Name == "User"),
                IsCasyConnected = true
            });

            _shortcutViewModels.Add(new ShortcutViewModel()
            {
                Header = _localizationService.GetLocalizedString("ShortcutDashboardPageView_ShortCut_Template"),
                ImagePath = "icon_template",
                Command = this.TemplatesCommand,
                Order = 5,
                MinRequiredRole = _authenticationService.RolesList.FirstOrDefault(role => role.Name == "User"),
                IsCasyConnected = true
            });

            OnUserLoggedIn(null, null);
            });
        }

        private void OnAnalyze()
        {
            this._eventAggregatorProvider.Instance.GetEvent<NavigateToEvent>().Publish(new NavigationArgs(NavigationCategory.AnalyseGraph));
        }

        private void OnData()
        {
            this._eventAggregatorProvider.Instance.GetEvent<NavigateToEvent>().Publish(new NavigationArgs(NavigationCategory.AnalyseTable));
        }

        private async void OnMeasure()
        {
            var result = await this._measureResultManager.SaveChangedMeasureResults();
            if (result != ButtonResult.Cancel)
            {
                var args = new NavigationArgs(NavigationCategory.Measurement);
                this._eventAggregatorProvider.Instance.GetEvent<NavigateToEvent>().Publish(args);
            }
        }

        private void OnTemplates()
        {
            this._eventAggregatorProvider.Instance.GetEvent<NavigateToEvent>().Publish(new NavigationArgs(NavigationCategory.Template));
        }

        private async void OnRecentTemplate(object obj)
        {
            var template = obj as MeasureSetup;

            if(template != null)
            {
                var result = await this._measureResultManager.SaveChangedMeasureResults();
                if (result != ButtonResult.Cancel)
                {
                    var args = new NavigationArgs(NavigationCategory.Measurement)
                    {
                        Parameter = template
                    };
                    this._eventAggregatorProvider.Instance.GetEvent<NavigateToEvent>().Publish(args);
                }
            }
        }

        private void OnPurge()
        {
            Task.Factory.StartNew(() => _measureController.Clean());
        }

        private void OnBackground()
        {
            this._serviceController.StartMeasureBackgroundWizard();
        }
    }
}
