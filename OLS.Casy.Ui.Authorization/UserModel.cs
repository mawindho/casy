using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Models;
using System;
using System.Linq;

namespace OLS.Casy.Ui.Authorization
{
    public class UserModel : ModelBase, IComparable, IComparable<UserModel>
    {
        private User _associatedUser;
        private readonly IAuthenticationService _authenticationService;

        public UserModel(User user, IAuthenticationService authenticationService)
        {
            this._associatedUser = user;
            this._authenticationService = authenticationService;
        }

        public string UserName
        {
            get { return _associatedUser.Identity.Name; }
        }

        public string UserRole
        {
            get { return _associatedUser.UserRole.Name; }
            set
            {
                _associatedUser.UserRole = _authenticationService.RolesList.FirstOrDefault(item => item.Name == value);
                NotifyOfPropertyChange();
            }
        }

        public string JobTitle
        {
            get { return _associatedUser.JobTitle; }
            set { _associatedUser.JobTitle = value; }
        }

        public string Country
        {
            get { return _associatedUser.CountryRegionName; }
            set { _associatedUser.CountryRegionName = value; }
        }

        public string FirstName
        {
            get { return _associatedUser.FirstName; }
            set { _associatedUser.FirstName = value; }
        }

        public string LastName
        {
            get { return _associatedUser.LastName; }
            set { _associatedUser.LastName = value; }
        }

        public string FullName
        {
            get { return string.Format("{0} {1}", _associatedUser.FirstName, _associatedUser.LastName); }
        }

        public string Contacts
        {
            get { return _associatedUser.EmailAddress; }
            set { _associatedUser.EmailAddress = value; }
        }

        public byte[] Image
        {
            get { return _associatedUser.Image; }
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
