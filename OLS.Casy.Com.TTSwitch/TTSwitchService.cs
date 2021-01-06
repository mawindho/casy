using OLS.Casy.Com.Api;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace OLS.Casy.Com.TTSwitch
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(ITTSwitchService))]
    public class TTSwitchService : ITTSwitchService
    {
        private readonly ICasySerialPortDriver _casySerialPortDriver;

        [ImportingConstructor]
        public TTSwitchService(ICasySerialPortDriver casySerialPortDriver)
        {
            this._casySerialPortDriver = casySerialPortDriver;
        }

        public bool SwitchToTTC()
        {
            var progress = new Progress<string>();

            if (this._casySerialPortDriver.SendInfo(progress))
            {
                var serialNumber = _casySerialPortDriver.GetSerialNumber(progress);

                var dateTime = _casySerialPortDriver.GetDateTime(progress);

                var masterPin = this.GenerateServicePIN(serialNumber.Item1, dateTime.Item1);
                var success = _casySerialPortDriver.VerifyMasterPin(masterPin, progress);

                if (success)
                {
                    success = _casySerialPortDriver.SendSwitchToTTC(progress);
                }
                return success;
            }
            return false;
        }

        private string GenerateServicePIN(string serialNumberString, DateTime dateTime)
        {
            char[] serialNumber = serialNumberString.ToCharArray();
            byte[] result = new byte[2];

            if (serialNumber.Length < 2)
            {
                result[0] = 0;
                result[1] = 0;
            }
            else
            {
                result[0] = Convert.ToByte(serialNumber[serialNumber.Length - 2]);
                result[1] = Convert.ToByte(serialNumber[serialNumber.Length - 1]);
            }

            var yearString = dateTime.Year.ToString();
            var year = int.Parse(yearString.Substring(2));

            result[0] ^= (byte)(dateTime.Day ^ year);
            result[1] ^= (byte)((dateTime.Day << 3) ^ dateTime.Month);

            return BitConverter.ToUInt16(result, 0).ToString();
        }
    }
}
