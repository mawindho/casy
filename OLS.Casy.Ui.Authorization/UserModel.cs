using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Models;
using System;
using System.Linq;

namespace OLS.Casy.Ui.Authorization
{
    public class UserModel : ModelBase, IComparable, IComparable<UserModel>
    {
        
        private readonly IAuthenticationService _authenticationService;

        public UserModel(User user, IAuthenticationService authenticationService)
        {
            this.AssociatedUser = user;
            this._authenticationService = authenticationService;
        }

        public User AssociatedUser { get; }

        public string UserName
        {
            get { return AssociatedUser.Identity.Name; }
        }

        public string UserRole
        {
            get { return AssociatedUser.UserRole.Name; }
            set
            {
                AssociatedUser.UserRole = _authenticationService.RolesList.FirstOrDefault(item => item.Name == value);
                NotifyOfPropertyChange();
            }
        }

        public string JobTitle
        {
            get { return AssociatedUser.JobTitle; }
            set { AssociatedUser.JobTitle = value; }
        }

        public string Country
        {
            get { return AssociatedUser.CountryRegionName; }
            set { AssociatedUser.CountryRegionName = value; }
        }

        public string FirstName
        {
            get { return AssociatedUser.FirstName; }
            set { AssociatedUser.FirstName = value; }
        }

        public string LastName
        {
            get { return AssociatedUser.LastName; }
            set { AssociatedUser.LastName = value; }
        }

        public string FullName
        {
            get { return string.Format("{0} {1}", AssociatedUser.FirstName, AssociatedUser.LastName); }
        }

        public string Contacts
        {
            get { return AssociatedUser.EmailAddress; }
            set { AssociatedUser.EmailAddress = value; }
        }

        public byte[] Image
        {
            get { return AssociatedUser.Image; }
        }

        public int CompareTo(object obj)
        {
            if (obj == null)
            {
                return 1;
            }
            UserModel other = obj as UserModel; // avoid double casting
            if (other == null)
            {
                throw new ArgumentException("A UserModel object is required for comparison.", "obj");
            }
            return this.CompareTo(other);
        }

        public int CompareTo(UserModel other)
        {
            if (object.ReferenceEquals(other, null))
            {
                return 1;
            }
            // Ratings compare opposite to normal string order, 
            // so reverse the value returned by String.CompareTo.
            return -string.Compare(this.UserName, other.UserName, StringComparison.OrdinalIgnoreCase);
        }

        public static int Compare(UserModel left, UserModel right)
        {
            if (object.ReferenceEquals(left, right))
            {
                return 0;
            }
            if (object.ReferenceEquals(left, null))
            {
                return -1;
            }
            return left.CompareTo(right);
        }

        public override bool Equals(object obj)
        {
            UserModel other = obj as UserModel; //avoid double casting
            if (object.ReferenceEquals(other, null))
            {
                return false;
            }
            return this.CompareTo(other) == 0;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                // Maybe nullity checks, if these are objects not primitives!
                hash = hash * 23 + UserName.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(UserModel left, UserModel right)
        {
            if (object.ReferenceEquals(left, null))
            {
                return object.ReferenceEquals(right, null);
            }
            return left.Equals(right);
        }
        public static bool operator !=(UserModel left, UserModel right)
        {
            return !(left == right);
        }
        public static bool operator <(UserModel left, UserModel right)
        {
            return (Compare(left, right) < 0);
        }
        public static bool operator >(UserModel left, UserModel right)
        {
            return (Compare(left, right) > 0);
        }
    }
}
