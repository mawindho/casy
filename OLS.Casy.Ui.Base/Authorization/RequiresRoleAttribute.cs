using OLS.Casy.Models;
using System;
using System.Security.Principal;

namespace OLS.Casy.Ui.Base.Authorization
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field,
        AllowMultiple = true, Inherited = true)]
    internal sealed class RequiresRoleAttribute : AuthorizationAttribute
    {
        private readonly UserRole _minRequiredRole;

        public RequiresRoleAttribute(UserRole minRequiredRole)
        {
            this._minRequiredRole = minRequiredRole;
        }

        protected override AuthorizationResult IsAuthorized(IPrincipal principal)
        {
            if (principal == null && this._minRequiredRole == null)
            {
                return AuthorizationResult.Allowed;
            }

            if (principal == null)
            {
                return new AuthorizationResult("Invalid user");
            }

            var user = principal as User;

            if (user == null)
            {
                return new AuthorizationResult("Invalid user");
            }

            if (this._minRequiredRole == null || user.UserRole.Priority >= this._minRequiredRole.Priority)
            {
                return AuthorizationResult.Allowed;
            }
            return new AuthorizationResult("Not in role");
        }
    }
}
