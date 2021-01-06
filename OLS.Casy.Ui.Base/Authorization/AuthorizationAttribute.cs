using OLS.Casy.Ui.Base.DyamicUiHelper;
using System;
using System.Security.Principal;

namespace OLS.Casy.Ui.Base.Authorization
{
    internal abstract class AuthorizationAttribute : AttributeBase
    {
        public AuthorizationResult Authorize(IPrincipal principal)
        {
            return this.IsAuthorized(principal);
        }

        protected abstract AuthorizationResult IsAuthorized(IPrincipal principal);
    }
}
