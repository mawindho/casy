using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;

namespace OLS.Casy.Models
{
    /// <summary>
    /// Implementation of <see cref="IUser"/> for local authentication with database.
    /// </summary>
    public class User : IPrincipal
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="identity">Identity of the user</param>
        /// <param name="surname">Surname of the user</param>
        /// <param name="forename">Forename of the user</param>
        /// <param name="password">Password of the user</param>
        /// <param name="userRole">Role of the user</param>
        /// <param name="findRoleDelegate">Delegate to find the role</param>
        /// <param name="workingUserRole">Optional: Working user role</param>
        public User(string identity, UserRole userRole)
        {
            this.Identity = new UserIdentity(identity);
            this.UserRole = userRole;
            //this.WorkingUserRole = workingUserRole == null ? userRole : workingUserRole;
            this.SelectedTableColumns = new List<string>();
            this.RecentTemplateIds = new List<int>();
            this.FavoriteTemplateIds = new List<int>();
            this.UserGroups = new List<UserGroup>();
        }

        /// <summary>
        /// Id of the user entity
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        ///     <see cref="IUser.Password" />
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        ///     <see cref="IUser.UserRole" />
        /// </summary>
        public UserRole UserRole { get; set; }

        /// <summary>
        ///     <see cref="IUser.WorkingUserRole" />
        /// </summary>
        //public UserRole WorkingUserRole { get; set; }

        public Func<string, UserRole> FindRoleDelegate { get; set; }

        /// <summary>
        ///     <see cref="IPrincipal.IsInRole" />
        /// </summary>
        public bool IsInRole(string role)
        {
            var userRole = FindRoleDelegate(role);
            return userRole.Name == this.UserRole.Name;
        }

        /// <summary>
        ///     <see cref="IPrincipal.Identity" />
        /// </summary>
        public IIdentity Identity { get; private set; }

        /// <summary>
        ///     <see cref="IUser.Forename" />
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        ///     <see cref="IUser.Surename" />
        /// </summary>
        public string LastName { get; set; }

        public string JobTitle { get; set; }

        public string CountryRegionName { get; set; }

        public string KeyboardCountryRegionName { get; set; }

        public string EmailAddress { get; set; }

        public byte[] Image { get; set; }

        public bool ForceCreatePassword { get; set; }

        public List<string> SelectedTableColumns { get; set; }

        public List<int> RecentTemplateIds { get; set; }

        public List<int> FavoriteTemplateIds { get; set; }
       
        public int LastUsedSetupId { get; set; }

        public bool IsEmergencyUser { get; set; }
        public List<UserGroup> UserGroups { get; set; }

        public string Name { get { return this.Identity.Name; } }
    }
}
