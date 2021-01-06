using OLS.Casy.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OLS.Casy.Core.Authorization.Api
{
    public interface IAccessManager
    {
        UserGroup GetUserGroup(int id);
        IEnumerable<UserGroup> UserGroups { get; }
        UserGroup CreateUserGroup(string userGroupName);
        Task SaveChanges();
        void RejectChanges();
    }
}
