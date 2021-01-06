using OLS.Casy.Controller.Api;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.MainControls.Api;
using OLS.Casy.Ui.MainControls.Views;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using MahApps.Metro.IconPacks;

namespace OLS.Casy.Ui.MainControls.ViewModels
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export("UserMenuCommand", typeof(CommandViewModel))]
    public class ServiceCommandViewModel : CommandViewModel, IPartImportsSatisfiedNotification
    {
        private readonly ILocalizationService _localizationService;
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly ICompositionFactory _compositionFactory;
        private readonly IServiceController _serviceController;
        private readonly IEnvironmentService _environmentService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IDatabaseStorageService _databaseStorageService;

        [ImportingConstructor]
        public ServiceCommandViewModel(ILocalizationService localizationService,
             IEventAggregatorProvider eventAggregatorProvider, 
             [Import (AllowDefault = true)] IServiceController serviceController,
             ICompositionFactory compositionFactory,
             IEnvironmentService environmentService,
             IAuthenticationService authenticationService,
             IDatabaseStorageService databaseStorageService)
        {
            _localizationService = localizationService;
            _eventAggregatorProvider = eventAggregatorProvider;
            _compositionFactory = compositionFactory;
            _serviceController = serviceController;
            _environmentService = environmentService;
            _authenticationService = authenticationService;
            _databaseStorageService = databaseStorageService;

            Order = 1;
            AwesomeGlyph = PackIconFontAwesomeKind.ToolsSolid;
        }

        private void OnToolboxPressed()
        {
            Task.Factory.StartNew(() =>
            {
                var awaiter = new System.Threading.ManualResetEvent(false);
                var viewModelExport = _compositionFactory.GetExport<ToolboxViewModel>();
                var viewModel = viewModelExport.Value;

                var wrapper = new ShowCustomDialogWrapper()
                {
                    Awaiter = awaiter,
                    DataContext = viewModel,
                    DialogType = typeof(ToolboxView)
                };

                _eventAggregatorProvider.Instance.GetEvent<ShowCustomDialogEvent>().Publish(wrapper);
                if (awaiter.WaitOne())
                {
                }

                _compositionFactory.ReleaseExport(viewModelExport);
            });
        }

        public virtual void OnImportsSatisfied()
        {
            OnLanguageChanged(null, null);
            Command = new OmniDelegateCommand(OnToolboxPressed);

            _localizationService.LanguageChanged += OnLanguageChanged;
            IsVisible = _serviceController != null;

            _eventAggregatorProvider.Instance.GetEvent<ShowServiceEvent>().Subscribe(OnToolboxPressed);
            _environmentService.EnvironmentInfoChangedEvent += OnEnvironmentInfoChanged;

            _authenticationService.UserLoggedIn += OnUserLoggedIn;
        }

        private async void OnUserLoggedIn(object sender, AuthenticationEventArgs e)
        {
            _databaseStorageService.GetSettings().TryGetValue("CurWeeklyCleanStep", out var curWeeklyCleanStepSetting);

            if (curWeeklyCleanStepSetting == null || string.IsNullOrEmpty(curWeeklyCleanStepSetting.Value)) return;
            var split = curWeeklyCleanStepSetting.Value.Split('|');

            if (split[0] == "0") return;
            var viewModelExport = _compositionFactory.GetExport<ToolboxViewModel>();
            var viewModel = viewModelExport.Value;

            await viewModel.WeeklyClean(int.Parse(split[1]));

            _compositionFactory.ReleaseExport(viewModelExport);
        }

        private void OnLanguageChanged(object sender, LocalizationEventArgs e)
        {
            DisplayName = _localizationService.GetLocalizedString("UserMenu_Button_Service");
        }

        private void OnEnvironmentInfoChanged(object sender, string e)
        {
            if (e == "IsCasyConnected")
            {
                IsEnabled = (bool)_environmentService.GetEnvironmentInfo("IsCasyConnected");
            }
        }
    }
}
