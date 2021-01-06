using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Ui.Base.DyamicUiHelper;
using System.Linq;
using System.Windows;

namespace OLS.Casy.Ui.Base.Authorization
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal class AuthorizationSource : Source
    {
        private bool disposedValue = false; // To detect redundant calls

        protected override void Initialize()
        {
            if (this.Attributes.Any())
            {
                WeakEventManager<IAuthenticationService, AuthenticationEventArgs>.AddHandler(Authorization.AuthenticationService, "UserLoggedIn", HandleAuthenticationEvent);
                WeakEventManager<IAuthenticationService, AuthenticationEventArgs>.AddHandler(Authorization.AuthenticationService, "UserLoggedOut", HandleAuthenticationEvent);

                //Authorization.AuthenticationService.UserLoggedIn += this.HandleAuthenticationEvent;
                //Authorization.AuthenticationService.UserLoggedOut += this.HandleAuthenticationEvent;
            }
            this.Authorize();
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    //Authorization.AuthenticationService.UserLoggedIn -= this.HandleAuthenticationEvent;
                    //Authorization.AuthenticationService.UserLoggedOut -= this.HandleAuthenticationEvent;
                }
            }
            base.Dispose(disposing);
        }

        private void HandleAuthenticationEvent(object sender, AuthenticationEventArgs authenticationEventArgs)
        {
            this.Authorize();
        }

        private Result Authorize()
        {
            this.Result = this.AuthorizeCore();
            return this.Result;
        }

        private AuthorizationResult AuthorizeCore()
        {
            foreach (AuthorizationAttribute attribute in this.Attributes)
            {
                AuthorizationResult result = attribute.Authorize(Authorization.AuthenticationService.LoggedInUser);

                if (result != AuthorizationResult.Allowed)
                {
                    return result;
                }
            }
            return AuthorizationResult.Allowed;
        }
    }
}
