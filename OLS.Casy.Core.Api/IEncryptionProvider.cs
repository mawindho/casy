namespace OLS.Casy.Core.Api
{
    /// <summary>
    ///     Interface for a service providing encryption and decryption mechanism for passwords
    /// </summary>
    public interface IEncryptionProvider
    {
        byte[] Encrypt(byte[] data, string securityKey);
        byte[] Encrypt(byte[] data);

        byte[] Decrypt(byte[] chiperData, string securityKey);
        byte[] Decrypt(byte[] chiperData);

        /// <summary>
        /// Encrypts the passed password
        /// </summary>
        /// <param name="password">Password to encrypt</param>
        /// <returns>Encrypted password</returns>
        string EncryptPassword(string password);

        /// <summary>
        /// Decrypts the passed string
        /// </summary>
        /// <param name="cipherString">Encrypted string</param>
        /// <returns>Decrypted string</returns>
        string DecryptPassword(string cipherString);
    }
}
