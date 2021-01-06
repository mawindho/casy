using OLS.Casy.Models;
using System;

namespace OLS.Casy.Ui.Authorization.Api
{
    /// <summary>
	///     View model for the login dialog control menu view
	/// </summary>
	public interface ILoginViewModel
    {
        /// <summary>
        ///     Getter/setter for UserId property.
        /// </summary>
        string UserId { set; get; }

        /// <summary>
        ///     Gette/setterr for Password property.
        /// </summary>
        string Password { set; get; }

        /// <summary>
        ///     Getter/setter for IsValidUser property.
        /// </summary>
        bool IsValidUser { set; get; }

        /// <summary>
        ///     Getter/setter for IsLoginError property.
        /// </summary>
        bool IsLoginError { set; get; }
    }
}
