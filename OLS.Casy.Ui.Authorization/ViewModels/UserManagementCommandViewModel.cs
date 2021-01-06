using OLS.Casy.Core.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.MainControls.Api;
using System.ComponentModel.Composition;
using System.Windows.Input;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Models;
using OLS.Casy.Core.Authorization.Api;
using System.Threading.Tasks;
using OLS.Casy.Ui.Authorization.Views;
using DevExpress.Mvvm;
using MahApps.Metro.IconPacks;

namespace OLS.Casy.Ui.Authorization.ViewModels
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export("UserMenuCommand", typeof(CommandViewModel))]
    public class UserManagementCommandViewModel : CommandViewModel, IPartImportsSatisfiedNotification
    {
        private readonly ILocalizationService _localizationService;
        private readonly ICompositionFactory _compositionFactory;
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly IAuthenticationService _authenticationService;

        [ImportingConstructor]
        public UserManagementCommandViewModel(IEventAggregatorProvider eventAggregatorProvider,
            ILocalizationService localizationService,
            ICompositionFactory compositionFactory,
            IAuthenticationService authenticationService)
        {
            this._eventAggregatorProvider = eventAggregatorProvider;
            this._localizationService = localizationService;
            this._authenticationService = authenticationService;
            this._compositionFactory = compositionFactory;
            this.Order = 1;
            this.AwesomeGlyph = PackIconFontAwesomeKind.UsersSolid;
        }

        public ICommand PressedCommand
        {
            get
            {
                return new OmniDelegateCommand(OnUserPressed);
            }
        }

        private void OnUserPressed()
        {
            Task.Factory.StartNew(() =>
            {
                var awaiter = new System.Threading.ManualResetEvent(false);
                var viewModel = this._compositionFactory.GetExport<AuthorizationManagementViewModel>().Value;

                ShowCustomDialogWrapper wrapper = new ShowCustomDialogWrapper()
                {
                    Awaiter = awaiter,
                    DataContext = viewModel,
                    DialogType = typeof(AuthorizationManagementView)
                };

                this._eventAggregatorProvider.Instance.GetEvent<ShowCustomDialogEvent>().Publish(wrapper);
                if (awaiter.WaitOne())
                {
                }
            });
        }

        public void OnImportsSatisfied()
        {
            this.MinRequiredRole = _authenticationService.GetRoleByName("Supervisor");

            this.DisplayName = _localizationService.GetLocalizedString("UserMenu_Button_UserManagement");
            this.Command = new OmniDelegateCommand(OnUserPressed);

            _localizationService.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object sender, LocalizationEventArgs e)
        {
            this.DisplayName = _localizationService.GetLocalizedString("UserMenu_Button_UserManagement");
        }
    }
}
