using OLS.Casy.Core.Api;
using OLS.Casy.WebService.Host.Dtos;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using OLS.Casy.IO.SQLite.Standard;

namespace OLS.Casy.WebService.Host.Services
{
    public class UserService
    {
        private CasyContext _casyContext;
        private readonly IEncryptionProvider _encryptionProvider;
        //private readonly LicenseService _licenseService;

        public UserService(CasyContext casyContext, IEncryptionProvider encryptionProvider)
            //LicenseService licenseService)
        {
            _casyContext = casyContext;
            _encryptionProvider = encryptionProvider;
            //_licenseService = licenseService;
        }

        public UserDto Authenticate(string username, string password)
        {
            //if (_licenseService.License == null)
            //{
                //return null;
            //}

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return null;

            //if (_licenseService.License.AddOns.Contains("localAuth"))
            //{
                var user = _casyContext.Users.FirstOrDefault(x => x.Username == username);

                if (user == null)
                    return null;

                var decrypted = _encryptionProvider.DecryptPassword(user.Password);
                if (decrypted != password)
                {
                    return null;
                }

                return new UserDto
                {
                    Username = username,
                    FirstName = user.FirstName,
                    LastName = user.LastName
                };
            /*}
            else if (_licenseService.License.AddOns.Contains("adAuth"))
            {
                try
                {
                    AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
                    //TODO
                    return null;
                }
                catch
                {
                    return null;
                }
            }*/

            return new UserDto
            {
                Username = username,
                FirstName = "CASY",
                LastName = "User"
            };
        }

        private static bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt, bool forceCreatePassword)
        {
            if (forceCreatePassword) return true;
            if (password == null)
                throw new ArgumentNullException(nameof(password));

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Value cannot be empty or whitespace only string.", nameof(password));

            if (storedHash.Length != 64)
                throw new ArgumentException("Invalid length of password hash (64 bytes expected).", nameof(storedHash));

            if (storedSalt.Length != 128)
                throw new ArgumentException("Invalid length of password salt (128 bytes expected).", nameof(storedSalt));

            using (var hmac = new HMACSHA512(storedSalt))
            {
                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

                if (computedHash.Where((t, i) => t != storedHash[i]).Any())
                {
                    return false;
                }
            }

            return true;
        }
    }
}
