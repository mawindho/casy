using OLS.Casy.Core.Api;
using System;
using System.ComponentModel.Composition;
using System.Security.Cryptography;
using System.Text;

namespace OLS.Casy.Core.Authorization.Encryption
{
    /// <summary>
    /// Implementation of <see cref="IEncryptionProvider"/>.
    /// Uses a salted MD5 encryption.
    /// </summary>
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IEncryptionProvider))]
    public class EncryptionProvider : IEncryptionProvider
    {
        private const string SECURITYKEY = "ThisIsCasyBamBamBam";

        public byte[] Encrypt(byte[] data)
        {
            return this.Encrypt(data, SECURITYKEY);
        }

        public byte[] Encrypt(byte[] data, string securityKey)
        {
            byte[] keyArray;

            MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
            keyArray = hashmd5.ComputeHash(Encoding.UTF8.GetBytes(securityKey));

            //Always release the resources and flush data
            // of the Cryptographic service provide. Best Practice
            hashmd5.Clear();

            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            //set the secret key for the tripleDES algorithm
            tdes.Key = keyArray;
            //mode of operation. there are other 4 modes.
            //We choose ECB(Electronic code Book)
            tdes.Mode = CipherMode.ECB;
            //padding mode(if any extra byte added)

            tdes.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = tdes.CreateEncryptor();
            //transform the specified region of bytes array to resultArray
            byte[] resultArray =
              cTransform.TransformFinalBlock(data, 0,
              data.Length);
            //Release resources held by TripleDes Encryptor
            tdes.Clear();

            return resultArray;
        }

        public string EncryptPassword(string password)
        {
            
            byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(password);
            var encryptedData = Encrypt(toEncryptArray);
            //Return the encrypted data into unreadable string format
            return Convert.ToBase64String(encryptedData, 0, encryptedData.Length);
        }

        public byte[] Decrypt(byte[] chiperData)
        {
            return this.Decrypt(chiperData, SECURITYKEY);
        }

        public byte[] Decrypt(byte[] chiperData, string securityKey)
        {
            byte[] keyArray;

            //if hashing was used get the hash code with regards to your key
            MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
            keyArray = hashmd5.ComputeHash(Encoding.UTF8.GetBytes(securityKey));
            //release any resource held by the MD5CryptoServiceProvider

            hashmd5.Clear();

            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            //set the secret key for the tripleDES algorithm
            tdes.Key = keyArray;
            //mode of operation. there are other 4 modes. 
            //We choose ECB(Electronic code Book)

            tdes.Mode = CipherMode.ECB;
            //padding mode(if any extra byte added)
            tdes.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = tdes.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(chiperData, 0, chiperData.Length);
            //Release resources held by TripleDes Encryptor                
            tdes.Clear();
            return resultArray;
        }

        public string DecryptPassword(string cipherString)
        {
            
            //get the byte code of the string
            byte[] toEncryptArray = Convert.FromBase64String(cipherString);
            var resultArray = Decrypt(toEncryptArray);

            //return the Clear decrypted TEXT
            return UTF8Encoding.UTF8.GetString(resultArray);
        }
    }
}
