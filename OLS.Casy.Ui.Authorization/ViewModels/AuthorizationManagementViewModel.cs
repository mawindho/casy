using OLS.Casy.Core.Authorization.Local;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Ui.Authorization.Api;
using OLS.Casy.Ui.Base;
using System.ComponentModel.Composition;
using System.Windows.Input;

namespace OLS.Casy.Ui.Authorization.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(AuthorizationManagementViewModel))]
    public class AuthorizationManagementViewModel : DialogModelBase, IPartImportsSatisfiedNotification
    {
        private readonly ILocalizationService _localizationService;
        private readonly LocalAuthenticationService _authenticationService;

        private UserManagementViewModel _userManagementViewModel;
        private IGroupManagementViewModel _groupManagementViewModel;
        private bool _isGroupManagementMode;

        [ImportingConstructor]
        public AuthorizationManagementViewModel(
            ILocalizationService localizationService,
            LocalAuthenticationService authenticationService,
            UserManagementViewModel userManagementViewModel,
            [Import(AllowDefault = true)] IGroupManagementViewModel groupManagementViewModel)
        {
            this._localizationService = localizationService;
            this._authenticationService = authenticationService;
            this._userManagementViewModel = userManagementViewModel;
            this._groupManagementViewModel = groupManagementViewModel;
        }

        public IGroupManagementViewModel GroupManagementViewModel
        {
            get { return _groupManagementViewModel; }
        }

        public UserManagementViewModel UserManagementViewModel
        {
            get { return _userManagementViewModel; }
        }

        public ICommand ToggleUserManagementModeCommand
        {
            get { return new OmniDelegateCommand(OnToggleUserManagementMode); }
        }

        public bool IsGroupManagementMode
        {
            get { return _isGroupManagementMode; }
            set
            {
                if (value != _isGroupManagementMode)
                {
                    this._isGroupManagementMode = value;
                    NotifyOfPropertyChange();

                    UpdateTitle();
                    NotifyOfPropertyChange("ToggleUserManagementModeButtonText");
                }
            }
        }

        public bool IsGroupManagementAvailable
        {
            get { return this._groupManagementViewModel != null; }
        }

        public string ToggleUserManagementModeButtonText
        {
            get { return !IsGroupManagementMode ? _localizationService.GetLocalizedString("GroupManagementView_Button_Toggle") : _localizationService.GetLocalizedString("UserManagementView_Button_Toggle"); }
        }

        protected async override void OnOk()
        {
            await this.UserManagementViewModel.OnOk();

            if (this.GroupManagementViewModel != null)
            {
                this.GroupManagementViewModel.OnOk();
            }

            base.OnOk();
        }

        protected override void OnCancel()
        {
            this.UserManagementViewModel.OnCancel();

            if (this.GroupManagementViewModel != null)
            {
                this.GroupManagementViewModel.OnCancel();
            }

            _authenticationService.RejectChanges();
            base.OnCancel();
        }

        private void OnToggleUserManagementMode()
        {
            this.IsGroupManagementMode = !this.IsGroupManagementMode;
        }

        public void OnImportsSatisfied()
        {
            this._localizationService.LanguageChanged += (s, e) => UpdateTitle();

            this.UpdateTitle();
        }

        private void UpdateTitle()
        {
            if (this.IsGroupManagementMode)
            {
                this.Title = this._localizationService.GetLocalizedString("GroupManagementView_Title");
            }
            else
            {
                this.Title = this._localizationService.GetLocalizedString("UserManagementView_Title");
            }
        }
    }
}
