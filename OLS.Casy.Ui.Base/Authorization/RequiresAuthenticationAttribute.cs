using System;
using System.Security.Principal;

namespace OLS.Casy.Ui.Base.Authorization
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field,
        AllowMultiple = false, Inherited = true)]
    internal class RequiresAuthenticationAttribute : AuthorizationAttribute
    {
        protected override AuthorizationResult IsAuthorized(IPrincipal principal)
        {
            return principal.Identity.IsAuthenticated ? AuthorizationResult.Allowed : new AuthorizationResult("Not autenticated");
        }
    }
}
