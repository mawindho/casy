using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Config.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace OLS.Casy.Core.Authorization.Default
{
    [Export(typeof(IService))]
    [Export(typeof(IAuthenticationService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class DefaultAuthenticationService : AbstractService, IAuthenticationService, IPartImportsSatisfiedNotification
    {
        private readonly IEnvironmentService _environmentService;
        private readonly IDatabaseStorageService _databaseStorageService;
        private readonly ILocalizationService _localizationService;
        private readonly IEventAggregatorProvider _eventAggregatorProvider;

        private List<UserRole> _userRoles;

        private User _defaultUser;
        private User _loggedInUser;

        [ImportingConstructor]
        public DefaultAuthenticationService(IConfigService configService,
            IEnvironmentService environmentService,
            ILocalizationService localizationService,
            IDatabaseStorageService databaseStorageService,
            IEventAggregatorProvider eventAggregatorProvider)
            : base(configService)
        {
            this._environmentService = environmentService;
            this._localizationService = localizationService;
            this._databaseStorageService = databaseStorageService;
            this._eventAggregatorProvider = eventAggregatorProvider;

            this._userRoles = new List<UserRole>();

            //InitDefaultRoles();
            //InitDefaultUser();
        }

        public IEnumerable<UserRole> RolesList => this._userRoles;

        public User LoggedInUser
        {
            get { return _loggedInUser; }
            private set
            {
                this._loggedInUser = value;
                this._environmentService.SetEnvironmentInfo("LoggedInUserName", _loggedInUser == null ? "Unknown" : string.Format("{0} {1} ({2})", _loggedInUser.FirstName, _loggedInUser.LastName, _loggedInUser.Identity.Name));
            }
        }

        public IEnumerable<User> UsersList => new List<User>(new[] { this._defaultUser });

        public event EventHandler<AuthenticationEventArgs> UserLoggedIn;
        public event EventHandler<AuthenticationEventArgs> UserLoggedOut;
        public event EventHandler AutoLogOffRaised;

        public void AuthenticateCurrentUser()
        {
            this.LoggedInUser = this._defaultUser;
            this._localizationService.CurrentCulture = new CultureInfo(this._defaultUser.CountryRegionName);

            //this._localizationService.CurrentKeyboardCulture = new CultureInfo(this._defaultUser.KeyboardCountryRegionName);

            if (this.UserLoggedIn != null)
            {
                this.UserLoggedIn?.Invoke(this, new AuthenticationEventArgs
                {
                    User = this.LoggedInUser,
                    DiffersFromLastUser = true
                });
            }
            this._eventAggregatorProvider.Instance.GetEvent<ShowLoginScreenEvent>().Publish();
        }

        public void DisableAutoLogOff()
        {
            // Not relevant
        }

        public void EnableAutoLogOff()
        {
            // Not relevant
        }

        public UserRole GetDefaultRole()
        {
            return _userRoles.First();
        }

        public UserRole GetRoleByName(string roleName)
        {
            return this.RolesList.FirstOrDefault(role => role.Name == roleName);
        }

        public User GetUser(int id)
        {
            return this._databaseStorageService.GetUser(id);
        }

        public User GetUserByName(string userName)
        {
            return this._databaseStorageService.GetUserByName(userName);
        }

        public void SaveUser(User user)
        {
            _databaseStorageService.SaveUser(user);
        }

        public void OnImportsSatisfied()
        {
            this._databaseStorageService.OnDatabaseReadyEvent += OnDatabaseReady;

            if (_databaseStorageService.IsDatabaseReady)
            {
                OnDatabaseReady(null, null);
            }
            //InitDefaultUser();
        }

        private void InitDefaultRoles()
        {
            this._userRoles.Add(new UserRole() { Name = "User", Priority = 1 });
            this._userRoles.Add(new UserRole() { Name = "Operator", Priority = 2 });
            this._userRoles.Add(new UserRole() { Name = "Supervisor", Priority = 3 });

            foreach (var role in _userRoles)
            {
                _databaseStorageService.SaveUserRole(role);
            }
        }

        private void OnDatabaseReady(object sender, EventArgs e)
        {
            var userRoles = _databaseStorageService.GetUserRoles();
            foreach (var role in userRoles)
            {
                _userRoles.Add(role);
            }

            if (_userRoles.Count == 0)
            {
                InitDefaultRoles();
            }

            var users = _databaseStorageService.GetUsers();
            this._defaultUser = users.FirstOrDefault(user => user.Identity.Name == "CASY-User");
            if(this._defaultUser == null)
            {
                this.InitDefaultUser();
            }
        }

        private void InitDefaultUser()
        {
            this._defaultUser = new User("CASY-User", this._userRoles.FirstOrDefault(ur => ur.Priority == 3))
            {
                FirstName = "CASY",
                LastName = "User",
                CountryRegionName = "en-US",
                KeyboardCountryRegionName = "en-US"
            };
        }
    }
}
