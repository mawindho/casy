using OLS.Casy.Models;
using System;
using System.Collections.Generic;

namespace OLS.Casy.Core.Authorization.Api
{
    /// <summary>
    ///     Interface for authentication services.
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        ///     List of all defined users.
        /// </summary>		
        IEnumerable<User> UsersList { get; }

        /// <summary>
        ///     Returns the default role of the system
        /// </summary>
        /// <returns></returns>
        UserRole GetDefaultRole();

        UserRole GetRoleByName(string roleName);

        /// <summary>
        ///     List of all definded roles.
        /// </summary>
        IEnumerable<UserRole> RolesList { get; }

        /// <summary>
        ///     Log out for the current active user. 
        ///     Pre condiction: no user is logged in.
        /// </summary>
        User LoggedInUser { get; }

        User GetUser(int id);
        User GetUserByName(string userName);

        //string LastLoggedInUserName { get; }

        /// <summary>
        ///     Log in request. The user must be activated.
        /// </summary>
        /// <param name="identifier">Unique identifier.</param>
        /// <param name="password">Password for authentication.</param>		
        /// <param name="loggedInCallback">Optional parameter. Callback to get user after succesful login.</param>
        /// <param name="workingUserRole">Used role during session.</param>
        /// <returns>True if user authentication was successful.</returns>
        //bool LogIn(string identifier, string password, Action<User> loggedInCallback = null);

        /// <summary>
        ///     Log out of the current user.
        /// </summary>
        /// <param name="loggedOutCallback">Optional parameter. Callback to get user after succesful login.</param>
        //void LogOut(Action loggedOutCallback = null);

        void AuthenticateCurrentUser();

        /// <summary>
        ///     Event to signal a user login.
        /// </summary>
        event EventHandler<AuthenticationEventArgs> UserLoggedIn;

        /// <summary>
        ///     Event to signal a user logout.
        /// </summary>
        event EventHandler<AuthenticationEventArgs> UserLoggedOut;

        /// <summary>
        ///     Event to signal a changed user list.
        /// </summary>
        //event EventHandler UserListChanged;

        event EventHandler AutoLogOffRaised;
        void DisableAutoLogOff();
        void EnableAutoLogOff();

        /// <summary>
        ///     Adds an user the list of managed users.
        /// </summary>
        /// <param name="userToCreate">Date of new user.</param>
        /// <returns>True means user was created else user already exists or password invalid.</returns>
		/// <exception cref="ArgumentException">If the user IF already exists.</exception>
        //User CreateUser(string username, string password = null);

        /// <summary>
        ///     Searches an user by id. The user must be activated to be find.
        /// </summary>
        /// <param name="identifier">Unique identifier.</param>
        /// <returns>Returns the user as IUser instance if user exists else null will be returned.</returns>
        //User TryGetUser(string identifier);

        /// <summary>
        ///     Method to change the current password of an existing user.
        /// </summary>
        /// <param name="identifier">Unique identifier.</param>
        /// <param name="currentPassword">Current password as hash code.</param>
        /// <param name="newPassword">New password.</param>	    
        /// <returns>True means password was changed else user not found or password could not be changed.</returns>
        //bool ChangePassword(string identifier, string currentPassword, string newPassword);

        /// <summary>
        ///     Method to check the combination of ueser identifier and password.
        /// </summary>
        /// <param name="identifier">Unique identifier.</param>
        /// <param name="password">Password, as has value, to be checked.</param>
        //bool CheckPassword(string identifier, string password);

        /// <summary>
        ///     Triggers the storage of all users.
        /// </summary>
        //void SaveUserList();

        void SaveUser(User user);

        //void DeleteUser(string identifier);

        //void RejectChanges();
    }
}
