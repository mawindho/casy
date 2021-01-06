using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Threading.Tasks;

namespace OLS.Casy.Core.Authorization.Access
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IAccessManager))]
    public class AccessManager : IAccessManager, IPartImportsSatisfiedNotification
    {
        //private readonly IDatabaseStorageService _databaseStorageService;
        private readonly IAuthenticationService _authenticationService;

        private readonly IList<UserGroup> _userGroupList;

        [ImportingConstructor]
        public AccessManager(
            //IDatabaseStorageService databaseStorageService,
            IAuthenticationService authenticationService)
        {
            //this._databaseStorageService = databaseStorageService;
            this._authenticationService = authenticationService;

            this._userGroupList = new List<UserGroup>();
        }

        public IEnumerable<UserGroup> UserGroups
        {
            get { return _userGroupList; }
        }

        public UserGroup GetUserGroup(int id)
        {
            //return this._databaseStorageService.GetUserGroup(id);
            //return this._authenticationService.GetUserGroup()
            return null;
        }

        public void LoadUserGroups()
        {
            _userGroupList.Clear();
            //var userGroups = this._databaseStorageService.GetUserGroups();
            //var userGroups = this._authenticationService.GetUserGroups();
            
            //foreach (var userGroup in userGroups)
            //{
                //this._userGroupList.Add(userGroup);
            //}
        }

        public UserGroup CreateUserGroup(string userGroupName)
        {
            //if (!this._authenticationService.GetUserGroups().Any(ug => ug.Name == userGroupName))
            //{
                //var userGroup = new UserGroup()
                //{
                    //Name = userGroupName
                //};
                //this._authenticationService.SaveUserGroup(userGroup);
                //return userGroup;
            //}
            return null;
        }

        public async Task SaveChanges()
        {
            await Task.Factory.StartNew(() =>
            {
                //foreach (var userGroup in this._userGroupList)
                //{
                    //this._authenticationService.SaveUserGroup(userGroup);
                //}
            });
        }

        public void RejectChanges()
        {
            LoadUserGroups();
        }


        public void OnImportsSatisfied()
        {
            //this._databaseStorageService.OnDatabaseReadyEvent += OnDatabaseReady;

            //if (_databaseStorageService.IsDatabaseReady)
            //{
                //OnDatabaseReady(null, null);
            //}
        }

        private void OnDatabaseReady(object sender, EventArgs e)
        {
            this.LoadUserGroups();
        }
    }
}
