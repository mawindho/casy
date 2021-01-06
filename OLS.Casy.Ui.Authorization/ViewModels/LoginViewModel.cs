using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Config.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Models;
using OLS.Casy.Ui.Authorization.Api;
using OLS.Casy.Ui.Authorization.Views;
using OLS.Casy.Ui.Base;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Windows.Input;
using System;
using OLS.Casy.Core.Authorization.Local;
using DevExpress.Mvvm;

namespace OLS.Casy.Ui.Authorization.ViewModels
{
    /// <summary>
	///     View model for the embedded login view.
	/// </summary>
	[PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(ILoginViewModel))]
    public class LoginViewModel : Base.ViewModelBase, ILoginViewModel, IPartImportsSatisfiedNotification
    {
        private readonly LocalAuthenticationService _authenticationService;
        private readonly ILocalizationService _localizationService;
        private readonly ICompositionFactory _compositionFactory;
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly ISuperPasswordProvider _superPasswordProvider;
        private readonly IEnvironmentService _environmentService;
        private readonly IConfigService _configService;

        private bool _isLoginError;
        private bool _isValidUser;
        private string _password;
        private string _userId;
        private bool _isForceNewPassword;
        private bool _isLoginEnabled;

        /// <summary>
        ///     Importing constructor
        /// </summary>
        [ImportingConstructor]
        public LoginViewModel(
            LocalAuthenticationService authenticationService,
            ILocalizationService localizationService,
            ICompositionFactory compositionFactory,
            ISuperPasswordProvider superPasswordProvider,
            IEnvironmentService environmentService,
            IConfigService configService,
            IEventAggregatorProvider eventAggregatorProvider)
        {
            this._authenticationService = authenticationService;
            this._localizationService = localizationService;
            this._compositionFactory = compositionFactory;
            this._eventAggregatorProvider = eventAggregatorProvider;
            this._superPasswordProvider = superPasswordProvider;
            this._environmentService = environmentService;
            this._configService = configService;

            this.ResetError();
        }

        public string SessionId
        {
            get { return _superPasswordProvider.GenerateSessionId(); }
        }

        /// <summary>
        ///     <see cref="ILoginViewModel.ResetError" />
        /// </summary>
        public void ResetError()
        {
            //this.Password = "";
            this.IsValidUser = true;
            this.IsLoginError = false;
        }

        /// <summary>
        ///     Getter for ResetPassword-Command.
        /// </summary>
        public ICommand ChangePasswordCommand
        {
            get { return new OmniDelegateCommand(this.OnChangePassword); }
        }

        public bool IsForceNewPassword
        {
            get { return _isForceNewPassword; }
            set
            {
                if (value != _isForceNewPassword)
                {
                    _isForceNewPassword = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        /// <summary>
        ///     Gette/setterr for Password property.
        /// </summary>
        public string Password
        {
            get { return this._password; }
            set
            {
                if (this._password != value)
                {
                    this._password = value;
                    this.NotifyOfPropertyChange();
                }
            }
        }

        /// <summary>
        ///     Getter/setter for UserId property.
        /// </summary>
        public string UserId
        {
            get { return this._userId; }
            set
            {
                if (this._userId != value)
                {
                    this._userId = value;

                    this.IsLoginError = false;

                    User user = this._authenticationService.TryGetUser(this._userId);
                    if (null != user)
                    {
                        this.IsValidUser = true;
                        this.IsForceNewPassword = user.ForceCreatePassword;
                    }
                    else
                    {
                        this.IsValidUser = false;
                    }

                    this.NotifyOfPropertyChange();
                }
            }
        }

        /// <summary>
        ///     Getter/setter for IsValidUser property.
        /// </summary>
        public bool IsValidUser
        {
            get { return this._isValidUser; }
            set
            {
                this._isValidUser = value;
                this.NotifyOfPropertyChange();
            }
        }


        /// <summary>
        ///     Getter/setter for IsLoginError property.
        /// </summary>
        public bool IsLoginError
        {
            get { return this._isLoginError; }
            set
            {
                this._isLoginError = value;
                this.NotifyOfPropertyChange();
            }
        }

        public bool IsLoginEnabled
        {
            get { return this._isLoginEnabled; }
            set
            {
                if(value != this._isLoginEnabled)
                {
                    this._isLoginEnabled = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool IsShutDownButtonVisible
        {
            get { return !((bool)_environmentService.GetEnvironmentInfo("IsCasyConnected")); }
        }

        public ICommand OkCommand
        {
            get { return new OmniDelegateCommand(this.OnOk); }
        }

        public void OnImportsSatisfied()
        {
            this._configService.ConfigurationChangedEvent += OnConfigurationChanged;
            this._eventAggregatorProvider.Instance.GetEvent<ShowLoginScreenEvent>().Subscribe(() => this.EnableLoginScreen());

            var isConnected = _environmentService.GetEnvironmentInfo("IsCasyConnected");
            if(isConnected != null && !(bool)isConnected)
            {
                EnableLoginScreen();
            }

            OnConfigurationChanged(null, null);
        }

        private void EnableLoginScreen()
        {
            this.IsLoginEnabled = true;
        }

        private void OnConfigurationChanged(object sender, ConfigurationChangedEventArgs e)
        {
            if (this._authenticationService.ShowLastLoggedInUserName)
            {
                this.UserId = this._authenticationService.LastLoggedInUserName;
            }
            else
            {
                this._userId = null;
                NotifyOfPropertyChange("UserId");
            }
        }

        private void OnOk()
        {
            if (this.UserId == null)
            {
                this.IsValidUser = false;
                return;
            }

            if (this.UserId.Length == 0)
            {
                this.IsValidUser = false;
                return;
            }

            if (this._authenticationService.LogIn(this.UserId, this.Password, sessionId: SessionId))
            {
                this.Password = "";
                this.IsLoginError = false;
                if (!this._authenticationService.ShowLastLoggedInUserName)
                {
                    this._userId = null;
                    NotifyOfPropertyChange("UserId");
                }
            }
            else
                {
                    this.IsLoginError = true;
                }

        }

        protected void OnChangePassword()
        {
            User user = this._authenticationService.TryGetUser(this.UserId);

            if (user != null)
            {
                Task.Factory.StartNew(() =>
                {
                    var awaiter = new System.Threading.ManualResetEvent(false);
                    var viewModelExport = this._compositionFactory.GetExport<PasswordDialogViewModel>();
                    var viewModel = viewModelExport.Value;

                    viewModel.CurrentUser = user;

                    ShowCustomDialogWrapper wrapper = new ShowCustomDialogWrapper()
                    {
                        Awaiter = awaiter,
                        DataContext = viewModel,
                        Title = "PasswordDialogView_HeadingLabel_Content",
                        DialogType = typeof(PasswordDialogView)
                    };

                    this._eventAggregatorProvider.Instance.GetEvent<ShowCustomDialogEvent>().Publish(wrapper);
                    if (awaiter.WaitOne())
                    {
                        this.IsForceNewPassword = user.ForceCreatePassword;
                        this.Password = viewModel.NewPassword;
                        viewModel.ResetTextFields();
                    }

                    this._compositionFactory.ReleaseExport(viewModelExport);
                });

                this._authenticationService.SaveChanges();
            }
        }
    }
}
