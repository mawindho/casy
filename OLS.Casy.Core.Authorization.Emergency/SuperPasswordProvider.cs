using OLS.Casy.Core.Authorization.Api;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace OLS.Casy.Authorization.Emergency
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(ISuperPasswordProvider))]
    public class SuperPasswordProvider : ISuperPasswordProvider
    {
        private const int PWD_LENGTH = 8;
        private string _currentSessionId;

        [ImportingConstructor]
        public SuperPasswordProvider()
        {
            _currentSessionId = this.RandomString(8);
        }

        private static Random random = new Random((int)DateTime.Now.Ticks);
        private string RandomString(int size)
        {
            StringBuilder builder = new StringBuilder();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }

            return builder.ToString();
        }


        public string GenerateSessionId()
        {
            return _currentSessionId;
        }

        public string GenerateSuperPassword(string serialNumber, DateTime dateTime)
        {
            ushort w1 = (ushort)(76 * dateTime.Day * dateTime.Day + 7411);
            ushort w2 = (ushort)(371 * dateTime.Day * dateTime.Month + 1711);
            ushort w3 = (ushort) (3 * dateTime.Day * dateTime.Month * dateTime.Year + 9947);

            char[] serialNumberChars = new char[0];
            if (!string.IsNullOrEmpty(serialNumber))
            {
                serialNumberChars = serialNumber.ToArray();
            }

            var szKey = string.Format("{0}{1}{2}", w1, w2, w3).ToArray();

            int byteSum = 0;
            for(int i = 0; i < serialNumber.Length; i++)
            {
                byteSum += Convert.ToByte(serialNumber[i]);
            }

            int iSer = 0;
            int iKey = dateTime.Day % szKey.Length;
            char[] superPassword = new char[PWD_LENGTH];
            for(int i = 0; i < PWD_LENGTH; i++, iSer++, iKey++)
            {
                sbyte temp; 
                if (!string.IsNullOrEmpty(serialNumber))
                {
                    temp = (sbyte)((sbyte)serialNumberChars[iSer % serialNumber.Length] + (sbyte)szKey[iKey % szKey.Length]);
                }
                else
                {
                    temp = (sbyte)szKey[iKey % szKey.Length];
                }
                superPassword[i] = Convert.ToChar(Math.Abs(temp + byteSum) % 26 + 'A');
            }

            return new string(superPassword);
        }
    }
}
