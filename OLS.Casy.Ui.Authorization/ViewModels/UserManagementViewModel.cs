using DevExpress.Mvvm;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Local;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Core.Logging.Api;
using OLS.Casy.Models;
using OLS.Casy.Models.Enums;
using OLS.Casy.Ui.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ViewModelBase = OLS.Casy.Ui.Base.ViewModelBase;

namespace OLS.Casy.Ui.Authorization.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(UserManagementViewModel))]
    public class UserManagementViewModel : ViewModelBase, IPartImportsSatisfiedNotification
    {
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly LocalAuthenticationService _authenticationService;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;

        private readonly ObservableCollection<UserModel> _userViewModels;

        private string _userName;
        private string _firstName;
        private string _lastName;
        private string _language;
        private string _userRole;

        private bool _updateIsNotLastSupervisor;

        private Dictionary<string, Tuple<string, string>> _changedValues = new Dictionary<string, Tuple<string, string>>();

        [ImportingConstructor]
        public UserManagementViewModel(IEventAggregatorProvider eventAggregatorProvider,
            LocalAuthenticationService authenticationService,
            ILocalizationService localizationService,
            ILogger logger)
        {
            this._eventAggregatorProvider = eventAggregatorProvider;
            this._authenticationService = authenticationService;
            this._localizationService = localizationService;
            _logger = logger;

            this._userViewModels = new ObservableCollection<UserModel>();
        }

        public IList<UserModel> Users
        {
            get { return _userViewModels; }
        }

        public IEnumerable<UserRole> UserRoles
        {
            get { return _authenticationService.RolesList; }
        }

        public User LoggedInUser
        {
            get { return this._authenticationService.LoggedInUser; }
        }

        public IEnumerable<Language> Languages
        {
            get
            {
                return _localizationService.PossibleLanguages.Select(language => new Language()
                {
                    Name = language.Name,
                    NativeName = language.NativeName,
                    Flag = string.Format("{0}Icon", language.Name.Replace("-", ""))
                });
            }
        }

        public bool UpdateIsNotLastSupervisor
        {
            get { return _updateIsNotLastSupervisor; }
            set
            {
                this._updateIsNotLastSupervisor = value;
                NotifyOfPropertyChange();
            }
        }

        public ICommand DeleteCommand
        {
            get { return new OmniDelegateCommand<object>(OnDelete); }
        }

        private void OnDelete(object obj)
        {
            Task.Factory.StartNew(() =>
            {
                UserModel userModelToDelete = obj as UserModel;

                var awaiter = new System.Threading.ManualResetEvent(false);

                ShowMessageBoxDialogWrapper messageBoxWrapper = new ShowMessageBoxDialogWrapper()
                {
                    Awaiter = awaiter,
                    Title = "DeleteUsersDialog_Title",
                    Message = "DeleteUsersDialog_Content",
                    MessageParameter = new[] { userModelToDelete.UserName }
                };

                _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>().Publish(messageBoxWrapper);

                if (awaiter.WaitOne() && messageBoxWrapper.Result)
                {
                    _authenticationService.DeleteUser(userModelToDelete.UserName);
                    FillUserList();
                }

                if(_changedValues.ContainsKey(userModelToDelete.AssociatedUser.Identity.Name))
                {
                    _changedValues.Remove(userModelToDelete.AssociatedUser.Identity.Name);
                }
            });
        }

        public string UserName
        {
            get { return _userName; }
            set
            {
                if(value != _userName)
                {
                    this._userName = value;
                    NotifyOfPropertyChange();
                    NotifyOfPropertyChange("CanCreate");
                }
            }
        }

        public string FirstName
        {
            get { return _firstName; }
            set
            {
                if (value != _firstName)
                {
                    this._firstName = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string LastName
        {
            get { return _lastName; }
            set
            {
                if (value != _lastName)
                {
                    this._lastName = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string Language
        {
            get { return _language; }
            set
            {
                if (value != _language)
                {
                    this._language = value;
                    NotifyOfPropertyChange();
                    NotifyOfPropertyChange("CanCreate");
                }
            }
        }

        public string UserRole
        {
            get { return _userRole; }
            set
            {
                if (value != _userRole)
                {
                    this._userRole = value;
                    NotifyOfPropertyChange();
                    NotifyOfPropertyChange("CanCreate");
                }
            }
        }

        public bool CanCreate
        {
            get { return !string.IsNullOrEmpty(this.UserName) && !string.IsNullOrEmpty(this.UserRole) && !string.IsNullOrEmpty(this.Language); }
        }

        public ICommand CreateCommand
        {
            get { return new OmniDelegateCommand<object>(async (obj) => await OnCreate(obj)); }
        }

        private async Task OnCreate(object obj)
        {
            await Task.Factory.StartNew(() =>
            {
                if (!string.IsNullOrEmpty(this.UserName) && !string.IsNullOrEmpty(this.UserRole) && !string.IsNullOrEmpty(this.Language))
                {
                    if (_authenticationService.TryGetUser(this.UserName) != null)
                    {
                        var awaiter = new System.Threading.ManualResetEvent(false);

                        ShowMessageBoxDialogWrapper messageBoxWrapper = new ShowMessageBoxDialogWrapper()
                        {
                            Awaiter = awaiter,
                            Title = "UserAlreadyExistsDialog_Title",
                            Message = "UserAlreadyExistsDialog_Content",
                            MessageParameter = new[] { this.UserName }
                        };

                        _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>().Publish(messageBoxWrapper);

                        if (awaiter.WaitOne())
                        {
                            return;
                        }
                    }

                    var newUser = _authenticationService.CreateUser(this.UserName);
                    newUser.CountryRegionName = this.Language;
                    newUser.KeyboardCountryRegionName = this.Language;
                    newUser.FirstName = this.FirstName;
                    newUser.LastName = this.LastName;
                    newUser.UserRole = _authenticationService.GetRoleByName(this.UserRole);
                    FillUserList();

                    this.UserName = string.Empty;
                    this.FirstName = string.Empty;
                    this.LastName = string.Empty;
                    this.UserRole = this.UserRoles.FirstOrDefault().Name;
                }
            });
        }

        public void OnImportsSatisfied()
        {
            FillUserList();
        }

        private void FillUserList()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var toRemoves = this._userViewModels.ToArray();
                foreach(var toRemove in toRemoves)
                {
                    _userViewModels.Remove(toRemove);
                    toRemove.PropertyChanged -= OnUserModelPropertyChanged;
                }
                _userViewModels.Clear();
                foreach (var user in _authenticationService.UsersList.Where(user => !user.IsEmergencyUser))
                {
                    var userModel = new UserModel(user, _authenticationService);
                    userModel.PropertyChanged += OnUserModelPropertyChanged;
                    _userViewModels.Add(userModel);
                }

                NotifyOfPropertyChange("Users");
            });
        }

        private void OnUserModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case "UserRole":
                    this.UpdateIsNotLastSupervisor = !this._updateIsNotLastSupervisor;
                    _changedValues.Add(((UserModel)sender).AssociatedUser.Identity.Name, new Tuple<string, string>(e.PropertyName, ((UserModel)sender).AssociatedUser.UserRole.Name));
                    break;
            }
            
        }

        public void OnCancel()
        {
            _changedValues.Clear();
            this.UserName = string.Empty;
            this.FirstName = string.Empty;
            this.LastName = string.Empty;
            this.UserRole = this.UserRoles.FirstOrDefault().Name;
        }

        public async Task OnOk()
        {
            if (!string.IsNullOrEmpty(this.UserName))
            {
                await this.OnCreate(null);
            }

            IList<ValidationResult> result = new List<ValidationResult>();
            if (Validator.TryValidateObject(this, new ValidationContext(this, null, null), result))
            {
                this._authenticationService.SaveChanges();
            }

            foreach(var change in _changedValues)
            {
                this._logger.Info(LogCategory.UserManagement, string.Format("User '{0}' changed '{1}' to '{2}'", change.Key, change.Value.Item1, change.Value.Item2));
            }

            this.UserName = string.Empty;
            this.FirstName = string.Empty;
            this.LastName = string.Empty;
            this.UserRole = this.UserRoles.FirstOrDefault().Name;
        }
    }
}
