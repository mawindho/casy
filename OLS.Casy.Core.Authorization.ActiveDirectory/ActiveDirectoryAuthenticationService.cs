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
using System.DirectoryServices.AccountManagement;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace OLS.Casy.Core.Authorization.ActiveDirectory
{
    [Export(typeof(IService))]
    [Export(typeof(IAuthenticationService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ActiveDirectoryAuthenticationService : AbstractService, IAuthenticationService, IPartImportsSatisfiedNotification
    {
        private readonly IDatabaseStorageService _databaseStorageService;
        private readonly IEnvironmentService _environmentService;
        private readonly ILocalizationService _localizationService;
        private readonly IEventAggregatorProvider _eventAggregatorProvider;

        private User _loggedInUser;
        private readonly List<UserRole> _userRoles;
        private string _adGroupSupervisor;
        private string _adGroupOperator;
        private string _adGroupUser;

        private bool _isAdReachable = true;

        [ImportingConstructor]
        public ActiveDirectoryAuthenticationService(
            IConfigService configService,
            IDatabaseStorageService databaseStorageService,
            IEnvironmentService environmentService,
            ILocalizationService localizationService,
            IEventAggregatorProvider eventAggregatorProvider)
            : base(configService)
        {
            _databaseStorageService = databaseStorageService;
            _environmentService = environmentService;
            _localizationService = localizationService;
            _eventAggregatorProvider = eventAggregatorProvider;

            _userRoles = new List<UserRole>();
        }

        public IEnumerable<UserRole> RolesList => _userRoles;

        public User LoggedInUser
        {
            get => _loggedInUser;
            private set
            {
                _loggedInUser = value;
                _environmentService.SetEnvironmentInfo("LoggedInUserName", _loggedInUser == null ? "Unknown" : $"{_loggedInUser.FirstName} {_loggedInUser.LastName} ({_loggedInUser.Identity.Name})");
            }
        }

        public IEnumerable<User> UsersList => _databaseStorageService.GetUsers();

        public User GetUserByName(string userName)
        {
            return _databaseStorageService.GetUserByName(userName);
        }

        public event EventHandler<AuthenticationEventArgs> UserLoggedIn;
        public event EventHandler<AuthenticationEventArgs> UserLoggedOut;
        public event EventHandler AutoLogOffRaised;

        public UserRole GetDefaultRole()
        {
            return _userRoles.FirstOrDefault(item => item.Id == 1);
        }

        public UserRole GetRoleByName(string roleName)
        {
            return RolesList.FirstOrDefault(role => role.Name == roleName);
        }

        public string AdGroupUser
        {
            get => _adGroupUser;
            set
            {
                if (value != null && value != _adGroupUser)
                {
                    _adGroupUser = value;
                }
            }
        }

        public string AdGroupOperator
        {
            get => _adGroupOperator;
            set
            {
                if (_adGroupOperator != null && value != _adGroupOperator)
                {
                    _adGroupOperator = value;
                }
            }
        }

        public string AdGroupSupervisor
        {
            get => _adGroupSupervisor;
            set
            {
                if (value != null && value != _adGroupSupervisor)
                {
                    _adGroupSupervisor = value;
                }
            }
        }

        public void OnImportsSatisfied()
        {
            try
            {
                AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
            }
            catch
            {
                _isAdReachable = false;
            }

            _databaseStorageService.OnDatabaseReadyEvent += OnDatabaseReady;

            if (_databaseStorageService.IsDatabaseReady)
            {
                OnDatabaseReady(null, null);
            }

            _eventAggregatorProvider.Instance.GetEvent<ConfigurationChangedEvent>().Subscribe(OnConfigurationChanged);
        }

        private void OnConfigurationChanged()
        {
            InitAdGroups();
        }

        public void SaveUser(User user)
        {
            _databaseStorageService.SaveUser(user);
        }

        public void AuthenticateCurrentUser()
        {
            InitAdGroups();

            if (!_isAdReachable) return;
            var principal = Thread.CurrentPrincipal;
            if (principal.Identity.IsAuthenticated)
            {
                using (var pc = new PrincipalContext(ContextType.Domain, "olsfile.ols.local"))
                //using (var pc = new PrincipalContext(ContextType.Domain, "192.168.110.2", @"mwindhorst@ols.local", "ZcM53211"))
                //using (var pc = new PrincipalContext(ContextType.Domain))
                //using (var pc = new PrincipalContext(ContextType.Domain, "192.168.110.2", "CN=Users,DC=ols,DC=local", ContextOptions.Negotiate | ContextOptions.SecureSocketLayer))
                using (var up = UserPrincipal.FindByIdentity(pc, principal.Identity.Name))
                {
                    var dbUser = _databaseStorageService.GetUserByName(up.UserPrincipalName) ?? CreateUser(up);

                    dbUser.FirstName = up.GivenName;
                    dbUser.LastName = up.Surname;

                    var groups = up.GetAuthorizationGroups();

                    dbUser.UserRole = GetMaxUserRole(groups.ToList());

                    if (dbUser.UserRole != UserRole.None)
                    {
                        LoggedInUser = dbUser;

                        _localizationService.CurrentCulture = new CultureInfo(dbUser.CountryRegionName);
                    }
                }

                UserLoggedIn?.Invoke(this, new AuthenticationEventArgs
                {
                    User = LoggedInUser,
                    DiffersFromLastUser = true
                });
            }
        }

        public void DisableAutoLogOff()
        {
            // Not relevant in AD-Auth
        }

        public void EnableAutoLogOff()
        {
            // Not relevant in AD-Auth
        }

        private User CreateUser(UserPrincipal userPrincipal)
        {
            var groups = userPrincipal.GetAuthorizationGroups();

            var user = new User(userPrincipal.UserPrincipalName, GetMaxUserRole(groups.ToList()))
            {
                CountryRegionName = "en-US", KeyboardCountryRegionName = "en-US",
            };

            _databaseStorageService.SaveUser(user);
            return user;
        }

        private UserRole GetMaxUserRole(IEnumerable<Principal> groups)
        {
            if (groups.Any(grp => grp.SamAccountName == _adGroupSupervisor))
            {
                return _userRoles.FirstOrDefault(r => r.Priority == 3);
            }

            if (groups.Any(grp => grp.SamAccountName == _adGroupOperator))
            {
                return _userRoles.FirstOrDefault(r => r.Priority == 2);
            }

            if (groups.Any(grp => grp.SamAccountName == _adGroupUser))
            {
                return _userRoles.FirstOrDefault(r => r.Priority == 1);
            }
            return UserRole.None;
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

            InitAdGroups();
        }

        private void InitAdGroups()
        {
            var settings = _databaseStorageService.GetSettings();

            if (!settings.TryGetValue("AdGroupUser", out var userGroupSetting))
            {
                _adGroupUser = "CASY-User";
                _databaseStorageService.SaveSetting("AdGroupUser", _adGroupUser);
            }
            else
            {
                _adGroupUser = userGroupSetting.Value;
            }

            if (!settings.TryGetValue("AdGroupOperator", out var operatorGroupSetting))
            {
                _adGroupOperator = "CASY-Operator";
                _databaseStorageService.SaveSetting("AdGroupOperator", _adGroupOperator);
            }
            else
            {
                _adGroupOperator = operatorGroupSetting.Value;
            }

            if (!settings.TryGetValue("AdGroupSupervisor", out var superVisorGroupSetting))
            {
                _adGroupSupervisor = "CASY-Supervisor";
                _databaseStorageService.SaveSetting("AdGroupSupervisor", _adGroupSupervisor);
            }
            else
            {
                _adGroupSupervisor = superVisorGroupSetting.Value;
            }
        }

        private void InitDefaultRoles()
        {
            _userRoles.Add(new UserRole { Name = "User", Priority = 1 });
            _userRoles.Add(new UserRole { Name = "Operator", Priority = 2 });
            _userRoles.Add(new UserRole { Name = "Supervisor", Priority = 3 });

            foreach (var role in _userRoles)
            {
                _databaseStorageService.SaveUserRole(role);
            }
        }

        public User GetUser(int id)
        {
            return _databaseStorageService.GetUser(id);
        }
    }
}
