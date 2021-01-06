using OLS.Casy.Models;
using System;

namespace OLS.Casy.Core.Authorization.Api
{
    /// <summary>
    ///     Event aggument for a user login or user logout.
    /// </summary>
    public class AuthenticationEventArgs : EventArgs
    {
        /// <summary>
        ///     Changed user.
        /// </summary>
        public User User { get; set; }

        public bool DiffersFromLastUser { get; set; }
    }
}
