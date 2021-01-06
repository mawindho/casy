using DevExpress.Mvvm;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Models;
using OLS.Casy.Ui.Authorization.Api;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using OLS.Casy.Ui.Base;

namespace OLS.Casy.Ui.Authorization.Access.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(IGroupManagementViewModel))]
    public class GroupManagementViewModel : Base.ViewModelBase, IGroupManagementViewModel, IPartImportsSatisfiedNotification
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IAccessManager _accessManager;

        private ObservableCollection<UserGroup> _userGroups;
        private ObservableCollection<User> _availableUsers;
        private ObservableCollection<User> _inGroupUsers;

        private ListCollectionView _userGroupsViewSource;
        private ListCollectionView _availableUsersViewSource;
        private ListCollectionView _inGroupUsersViewSource;

        private string _newUserGroupName;
        private UserGroup _selectedUserGroup;

        [ImportingConstructor]
        public GroupManagementViewModel(
            IAuthenticationService authorizationService,
            IEnvironmentService environmentService,
            IAccessManager accessManager)
        {
            this._authenticationService = authorizationService;
            this._accessManager = accessManager;

            this._userGroups = new ObservableCollection<UserGroup>();
            this._availableUsers = new ObservableCollection<User>();
            this._inGroupUsers = new ObservableCollection<User>();
        }

        public ListCollectionView UserGroupsViewSource
        {
            get
            {
                if (_userGroupsViewSource == null)
                {
                    _userGroupsViewSource = CollectionViewSource.GetDefaultView(this._userGroups) as ListCollectionView;
                    _userGroupsViewSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
                    _userGroupsViewSource.IsLiveSorting = true;
                }
                return _userGroupsViewSource;
            }
        }

        public ListCollectionView AvailableUsersViewSource
        {
            get
            {
                if (_availableUsersViewSource == null)
                {
                    _availableUsersViewSource = CollectionViewSource.GetDefaultView(this._availableUsers) as ListCollectionView;
                    _availableUsersViewSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
                    _availableUsersViewSource.IsLiveSorting = true;
                }
                return _availableUsersViewSource;
            }
        }

        public ListCollectionView InGroupUsersViewSource
        {
            get
            {
                if (_inGroupUsersViewSource == null)
                {
                    _inGroupUsersViewSource = CollectionViewSource.GetDefaultView(this._inGroupUsers) as ListCollectionView;
                    _inGroupUsersViewSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
                    _inGroupUsersViewSource.IsLiveSorting = true;
                }
                return _inGroupUsersViewSource;
            }
        }

        public string NewUserGroupName
        {
            get { return this._newUserGroupName; }
            set
            {
                if (value != _newUserGroupName)
                {
                    this._newUserGroupName = value;
                    NotifyOfPropertyChange();
                    NotifyOfPropertyChange("CanCreate");
                }
            }
        }

        public bool CanCreate
        {
            get { return !string.IsNullOrEmpty(this.NewUserGroupName) && !this._userGroups.Any(ug => ug.Name == this.NewUserGroupName); }
        }

        public ICommand CreateUserGroupCommand
        {
            get { return new OmniDelegateCommand(OnCreateUserGroup); }
        }

        public ICommand SelectUserGroupCommand
        {
            get { return new OmniDelegateCommand<UserGroup>(OnSelectUserGroup); }
        }

        public bool IsGroupSelected
        {
            get { return this._selectedUserGroup != null; }
        }

        public ICommand AddToGroupCommand
        {
            get { return new OmniDelegateCommand<User>(OnAddToGroup); }
        }

        public ICommand RemoveFromGroupCommand
        {
            get { return new OmniDelegateCommand<User>(OnRemoveFromGroup); }
        }

        public ICommand SelectAllCommand
        {
            get { return new OmniDelegateCommand<User>(OnSelectAll); }
        }

        public ICommand DeselectAllCommand
        {
            get { return new OmniDelegateCommand<User>(OnDeselectAll); }
        }

        public void OnCancel()
        {            
            this._accessManager.RejectChanges();
            this.NewUserGroupName = null;
        }

        public void OnOk()
        {
            this._accessManager.SaveChanges();
        }

        public void OnImportsSatisfied()
        {
            var userGroups = this._accessManager.UserGroups;

            foreach(var userGroup in userGroups)
            {
                this._userGroups.Add(userGroup);
            }
        }

        private void OnCreateUserGroup()
        {
            var newUserGroup = this._accessManager.CreateUserGroup(this.NewUserGroupName);
            this._userGroups.Add(newUserGroup);
            this.NewUserGroupName = string.Empty;

            this.OnSelectUserGroup(newUserGroup);
        }

        private void OnSelectUserGroup(UserGroup selectedUserGroup)
        {
            this._selectedUserGroup = selectedUserGroup;
            NotifyOfPropertyChange("IsGroupSelected");

            var users = this._authenticationService.UsersList;
            this._inGroupUsers.Clear();
            this._availableUsers.Clear();

            foreach(var user in users)
            {
                if(selectedUserGroup.Users.Any(u => u.Id == user.Id))
                {
                    this._inGroupUsers.Add(user);
                }
                else
                {
                    this._availableUsers.Add(user);
                }
            }
        }

        private void OnAddToGroup(User user)
        {
            if(this._selectedUserGroup != null)
            {
                this._selectedUserGroup.Users.Add(user);
                this._inGroupUsers.Add(user);
                this._availableUsers.Remove(user);
            }
        }

        private void OnRemoveFromGroup(User user)
        {
            if (this._selectedUserGroup != null)
            {
                this._selectedUserGroup.Users.Remove(user);
                this._inGroupUsers.Remove(user);
                this._availableUsers.Add(user);
            }
        }

        private void OnSelectAll(User obj)
        {
            if(this._selectedUserGroup != null)
            {
                foreach(var user in this._availableUsers)
                {
                    this._selectedUserGroup.Users.Add(user);
                    this._inGroupUsers.Add(user);
                }
                this._availableUsers.Clear();
            }
        }

        private void OnDeselectAll(User obj)
        {
            if (this._selectedUserGroup != null)
            {
                foreach (var user in this._inGroupUsers)
                {
                    this._selectedUserGroup.Users.Remove(user);
                    this._availableUsers.Add(user);
                }
                this._inGroupUsers.Clear();
            }
        }
    }
}
