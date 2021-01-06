using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Config.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Core.Logging.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Models;
using OLS.Casy.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace OLS.Casy.Core.Authorization.Local
{
    /// <summary>
	///     Authentication service. This implementation allows the user creation, deletion (deactivation) and manipulation, also user login and logout.
	///     But new user are transient data.
	///     For test issues 6 default user are defined with identities 'level1' to 'level6', passwords like the identity.
	/// </summary>
	[Export(typeof(IService))]
    [Export(typeof(IAuthenticationService))]
    [Export(typeof(LocalAuthenticationService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class LocalAuthenticationService : AbstractService, IAuthenticationService, IPartImportsSatisfiedNotification
    {
        private readonly LocalAuthenticationProvider _authenticationProvider;
        private readonly ILocalizationService _localizationService;
        private readonly IEnvironmentService _environmentService;
        private readonly IDatabaseStorageService _databaseStorageService;
        private readonly ISuperPasswordProvider _superPasswordProvider;
        private readonly ILogger _logger;
        private readonly IEventAggregatorProvider _eventAggregatorProvider;

        private User _loggedInUser;
        private string _lastLoggedInUserName;
        private bool _autoLogoffOptionStarted;

        /// <summary>
        ///     Constructor. Creates the test user.
        /// </summary>
        [ImportingConstructor]
        public LocalAuthenticationService(
            ILogger logger, 
            IConfigService configService,
            LocalAuthenticationProvider authProvider, 
            IEnvironmentService environmentService,
            IDatabaseStorageService databaseStorageService,
            ILocalizationService localizationService,
            ISuperPasswordProvider superPasswordProvider,
            IEventAggregatorProvider eventAggregatorProvider)
            : base(configService)
        {
            this._authenticationProvider = authProvider;
            this._environmentService = environmentService;
            this._databaseStorageService = databaseStorageService;
            this._localizationService = localizationService;
            this._superPasswordProvider = superPasswordProvider;
            this._logger = logger;
            this._eventAggregatorProvider = eventAggregatorProvider;
        }

        [ConfigItem(true)]
        public bool ShowLastLoggedInUserName { get; set; }

        [ConfigItem("")]
        public string LastLoggedInUserName
        {
            get { return _lastLoggedInUserName; }
            set { this._lastLoggedInUserName = value; }
        }

        /// <summary>
        ///     Auto logoff time in milliseconds.
        /// </summary>
        [ConfigItem(10)]
        public long AutoLogOffTime { get; set; }

        #region IAuthenticationService Members

        /// <summary>
        ///     <see cref="IAuthenticationService.UsersList" />
        /// </summary>		
        public IEnumerable<User> UsersList
        {
            get { return _authenticationProvider.UsersList; }
        }

        public IEnumerable<UserRole> RolesList
        {
            get { return _authenticationProvider.RolesList; }
        }

        /// <summary>
        ///     <see cref="IAuthenticationService.LoggedInUser" />
        /// </summary>
        public User LoggedInUser
        {
            get { return _loggedInUser; }
            private set
            {
                this._loggedInUser = value;
                this._environmentService.SetEnvironmentInfo("LoggedInUserName", _loggedInUser == null ? "Unknown" : string.Format("{0} {1} ({2})", _loggedInUser.FirstName, _loggedInUser.LastName, _loggedInUser.Identity.Name));
                if (_loggedInUser != null)
                {
                    if (this.LastLoggedInUserName != _loggedInUser.Identity.Name)
                    {
                        this.LastLoggedInUserName = _loggedInUser.Identity.Name;

                        ConfigItemModel model = new ConfigItemModel()
                        {
                            Name = "LastLoggedInUserName",
                            Value = this.LastLoggedInUserName
                        };
                        ConfigService.UpdateConfiguration(new[] { model });
                    }
                }
            }
        }

        public User GetUser(int id)
        {
            return this._databaseStorageService.GetUser(id);
        }

        public User GetUserByName(string userName)
        {
            return this._databaseStorageService.GetUserByName(userName);
        }

        /// <summary>
        ///     <see cref="IAuthenticationService.LogIn" />
        /// </summary>
        public bool LogIn(string identifier, string password, Action<User> loggedInCallback = null, string sessionId = null)
        {
            if (this.LoggedInUser != null)
            {
                // this is an error on  application level, because login not allowed if any user is logged in.
                //throw new AuthenticationException("An user is already logged in.");
                return false;
            }

            User user = this.UsersList.FirstOrDefault(s => s.Identity.Name == identifier);
            if (null != user)
            {
                bool isAutherized = false;
                if(!user.IsEmergencyUser)
                {
                    isAutherized = _authenticationProvider.CheckPassword(user, password);
                }
                else
                {
                    isAutherized = password == _superPasswordProvider.GenerateSuperPassword(_environmentService.GetEnvironmentInfo("SerialNumber") as string, DateTime.Now);
                    isAutherized |= password == _superPasswordProvider.GenerateSuperPassword(sessionId, DateTime.Now);
                }

                if(isAutherized)
                {
                    var lastLoggedInUser = this.LastLoggedInUserName;

                    this.LoggedInUser = user;

                    this._localizationService.CurrentCulture = new CultureInfo(user.CountryRegionName);
                    //this._localizationService.CurrentKeyboardCulture = new CultureInfo(user.KeyboardCountryRegionName);

                    if (this.UserLoggedIn != null)
                    {
                        this.UserLoggedIn.Invoke(this, new AuthenticationEventArgs
                        {
                            User = user,
                            DiffersFromLastUser = user.Identity.Name != lastLoggedInUser
                        });
                    }

                    if (loggedInCallback != null)
                    {
                        loggedInCallback.Invoke(user);
                    }

                    this._logger.Info(LogCategory.UserManagement, string.Format("Successful login for user '{0}'", identifier));

                    this.StartAutoLogoffOption();
                    this.StartAutoLogoffTimer();
                    return true;
                }
                else
                {
                    this._logger.Info(LogCategory.UserManagement, string.Format("Login for user '{0}' failed. Invalid password.", identifier));
                }
            }

            this._logger.Info(LogCategory.UserManagement, string.Format("Login for user '{0}' failed. Unknown user.", identifier));
            return false;
        }

        public void AuthenticateCurrentUser()
        {
        }

        /// <summary>
        ///     <see cref="IAuthenticationService.LoggedInUser" />
        /// </summary>
        public void LogOut(Action loggedOutCallback = null)
        {
            if (null != this.LoggedInUser)
            {
                this._logger.Info(LogCategory.UserManagement, String.Format("Current user '{0}' was logged out", this.LoggedInUser.Identity.Name));
            }
            var args = new AuthenticationEventArgs { User = this.LoggedInUser };
            this.LoggedInUser = null;

            this.StopAutoLogoffTimer();

            if (this.UserLoggedOut != null)
            {
                this.UserLoggedOut.Invoke(this, args);
            }

            if (loggedOutCallback != null)
            {
                loggedOutCallback.Invoke();
            }
        }

        /// <summary>
        ///     <see cref="IAuthenticationService.UserLoggedIn" />
        /// </summary>
        public event EventHandler<AuthenticationEventArgs> UserLoggedIn;

        /// <summary>
        ///     <see cref="IAuthenticationService.UserLoggedOut" />
        /// </summary>
        public event EventHandler<AuthenticationEventArgs> UserLoggedOut;

        /// <summary>
        ///     <see cref="IAuthenticationService.UserListChanged" />
        /// </summary>
        //public event EventHandler UserListChanged;

        public event EventHandler AutoLogOffRaised;

        /// <summary>
        ///     <see cref="IAuthenticationService.AddUser" />
        /// </summary>
        public User CreateUser(string username, string password = null)
        {
             var user = _authenticationProvider.CreateUser(username, password);
            //UserListChanged?.Invoke(this, null);
            return user;
        }

        /// <summary>
        ///     <see cref="IAuthenticationService.TryGetUser" />
        /// </summary>
        public User TryGetUser(string identityToFind)
        {
            return _authenticationProvider.TryGetUser(identityToFind);
        }

        /// <summary>
        ///     <see cref="IAuthenticationService.ChangePassword" />
        /// </summary>
        public bool ChangePassword(string identifier, string currentPassword, string newPassword)
        {
            var user = TryGetUser(identifier);

            if (user != null)
            {
                return _authenticationProvider.ChangePassword(user, currentPassword, newPassword);
            }
            return false;
        }

        /// <summary>
        ///     <see cref="IAuthenticationService.CheckPassword" />
        /// </summary>
        public bool CheckPassword(string identifier, string password)
        {
            User user = this.TryGetUser(identifier);
            if (null == user)
            {
                return false; // user unknown
            }

            return _authenticationProvider.CheckPassword(user, password);
        }

        /// <summary>
        ///     <see cref="IAuthenticationService.SaveUserList" />
        /// </summary>
        public async void SaveChanges()
        {
            await this._authenticationProvider.SaveChanges();
        }

        public void SaveUser(User user)
        {
            this._authenticationProvider.SaveUser(user);
        }

        public void DeleteUser(string identifier)
        {
            User user = this.TryGetUser(identifier);
            if (null != user)
            {
                this._authenticationProvider.DeleteUser(user);
                //UserListChanged?.Invoke(this, null);
            }
        }

        public void RejectChanges()
        {
            this._authenticationProvider.RejectChanges();
        }

        public UserRole GetDefaultRole()
        {
            return _authenticationProvider.DefaultRole;
        }

        #endregion

        #region IService overrides

        /// <summary>
        ///     This method triges a user list storage and stops the Auto-Logoff function.
        ///     See also <see cref="IService.Prepare" />.
        /// </summary>
        public override void Deinitialize(IProgress<string> progress)
        {
            base.Deinitialize(progress);
            this.StopAutoLogoffOption();
        }

        #endregion

        #region auto logoff functions

        private void AutoLogOffHelper_MakeAutoLogOffEvent(object sender, EventArgs e)
        {
            if (null != this.LoggedInUser)
            {
                this._logger.Info(LogCategory.UserManagement, string.Format("Auto log off for user '{0}' triggered!", this.LoggedInUser.Identity.Name));
            }

            if (this.AutoLogOffRaised != null)
            {
                this.AutoLogOffRaised.Invoke(this, EventArgs.Empty);
            }

            this.LogOut();
        }

        private void StartAutoLogoffTimer()
        {
            AutoLogOffHelper.ResetLogoffTimer();
        }

        private void StopAutoLogoffTimer()
        {
            this._autoLogoffOptionStarted = false;
            AutoLogOffHelper.StopLogoffTimer();
            AutoLogOffHelper.MakeAutoLogOffEvent -= this.AutoLogOffHelper_MakeAutoLogOffEvent;
        }

        private void StartAutoLogoffOption()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if(this.AutoLogOffTime > 0)
                { 
                    if (this._autoLogoffOptionStarted)
                    {
                        return;
                    }
                    this._autoLogoffOptionStarted = true;

                    IntPtr active = GetActiveWindow();

                    var activeWindow = Application.Current.Windows.OfType<Window>()
                        .SingleOrDefault(window => new WindowInteropHelper(window).Handle == active);

                    if (activeWindow != null)
                    {
                        HwndSource windowSpecificOsMessageListener = HwndSource.FromHwnd(new WindowInteropHelper(activeWindow).Handle);
                        if (windowSpecificOsMessageListener != null)
                        {
                            windowSpecificOsMessageListener.AddHook(this.CallBackMethod);
                        }
                    }

                //TODO: Shall import some implementation from UI Project!
                // The application is listening for window specific OS messages, OS will not send one window's message to a different window. 
                // For that reason, in each window (visable at this moment), the application has to listen for the OS messages. So the application has to hook for the windows message in each window 
                // and when the message of type user activity is received, the application has to reset the timer. Therefore, you have to write some repetitive code in each window. 
                /*if (null != Application.Current)
                {
                    foreach (object window in Application.Current.Windows)
                    {
                        HwndSource windowSpecificOsMessageListener = HwndSource.FromHwnd(new WindowInteropHelper((Window)window).Handle);
                        if (windowSpecificOsMessageListener != null)
                        {
                            windowSpecificOsMessageListener.AddHook(this.CallBackMethod);
                        }
                    }
                }*/
                    AutoLogOffHelper.LogOffTime = (int)this.AutoLogOffTime;
                    AutoLogOffHelper.MakeAutoLogOffEvent += this.AutoLogOffHelper_MakeAutoLogOffEvent;
                    AutoLogOffHelper.StartAutoLogoffFunction();
                }
            });
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetActiveWindow();

        private void StopAutoLogoffOption()
        {
            if (!this._autoLogoffOptionStarted)
            {
                return;
            }
            this._autoLogoffOptionStarted = false;

            AutoLogOffHelper.StopAutoLogoffFunction();
            AutoLogOffHelper.MakeAutoLogOffEvent -= this.AutoLogOffHelper_MakeAutoLogOffEvent;
        }

        /// <summary>
        ///     This method checks all system messages for user input
        /// </summary>		
        private IntPtr CallBackMethod(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            //  Testing OS message to determine whether it is a user activity or not
            // 0x0200 ...  0x020A mouse events
            // 0x0106 WM_SYSCHAR 
            // 0x00A0 WM_NCMOUSEMOVE
            // 0x0001 WM_CREATE, can be used to detect any touch unput
            // 0x0002 WM_DESTROY, can be used to detect any touch unput
            if ((msg >= 0x0200 && msg <= 0x020A) || (msg <= 0x0106 && msg >= 0x00A0) ||
                msg == 0x0001 || msg == 0x0002)
            {
                AutoLogOffHelper.ResetLogoffTimer();
            }

            return IntPtr.Zero;
        }

        public void OnImportsSatisfied()
        {
            this.ConfigService.InitializeByConfiguration(this);

            this._databaseStorageService.OnDatabaseReadyEvent += OnDatabaseReady;
            base.ConfigService.ConfigurationChangedEvent += OnConfigChanged;

            this._eventAggregatorProvider.Instance.GetEvent<ConfigurationChangedEvent>().Subscribe(() => OnConfigChanged(null, null));

            if (_databaseStorageService.IsDatabaseReady)
            {
                OnDatabaseReady(null, null);
            }
        }

        private void OnConfigChanged(object sender, ConfigurationChangedEventArgs e)
        {
            this.ConfigService.InitializeByConfiguration(this);

            if (this._autoLogoffOptionStarted)
            { 
                this.StopAutoLogoffOption();
                this.StartAutoLogoffOption();
                this.StartAutoLogoffTimer();
            }

            
        }

        private void OnDatabaseReady(object sender, EventArgs e)
        {
            this._authenticationProvider.LoadUserList();

            //if (this.UserListChanged != null)
            //    this.UserListChanged.Invoke(this, EventArgs.Empty);
        }

        public UserRole GetRoleByName(string roleName)
        {
            return this.RolesList.FirstOrDefault(role => role.Name == roleName);
        }

        public void DisableAutoLogOff()
        {
            this.StopAutoLogoffOption();
        }

        public void EnableAutoLogOff()
        {
            this.StartAutoLogoffOption();
            this.StartAutoLogoffTimer();
        }

        #endregion
    }
}
