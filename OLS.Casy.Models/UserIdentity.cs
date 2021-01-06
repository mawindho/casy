using System.Security.Principal;

namespace OLS.Casy.Models
{
    /// <summary>
    ///     Identity class.
    /// </summary>
    public class UserIdentity : IIdentity
    {
        /// <summary>
        /// </summary>
        /// <param name="name"></param>
        public UserIdentity(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        ///     Not used yet
        /// </summary>
        public string AuthenticationType
        {
            get { return string.Empty; }
        }

        /// <summary>
        ///     Not needed to check for authentication yet
        /// </summary>
        public bool IsAuthenticated
        {
            get { return true; }
        }
    }
}
