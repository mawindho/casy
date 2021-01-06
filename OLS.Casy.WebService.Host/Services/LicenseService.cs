using Microsoft.Extensions.Configuration;
//using OLS.Casy.Core.Activation;
using OLS.Casy.Core.Api;
using System;
using System.IO;
//using System.Management;
using System.Runtime.Serialization.Formatters.Binary;

namespace OLS.Casy.WebService.Host.Services
{
    public class LicenseService
    {
       // private License _licence;

        public LicenseService(IConfiguration configuration, IEncryptionProvider encryptionProvider)
        {
            //var licenseFilePath = configuration.GetValue<string>("CasyLicensePath");
            //if (!string.IsNullOrEmpty(licenseFilePath))
            //{
                //try
                //{
                    //var licBytes = File.ReadAllBytes(licenseFilePath);
                    //licBytes = encryptionProvider.Decrypt(licBytes, GetUniqueId());

                    //using (var memoryStream = new MemoryStream(licBytes))
                    //{
                        //var formatter = new BinaryFormatter();
                        //_licence = formatter.Deserialize(memoryStream) as License;
                    //}
                //}
                //catch (Exception e)
                //{
                //}
            //}
        }

        //public License License
        //{
            //get { return _licence; }
        //}

        public string GetUniqueId()
        {
            //string cpuInfo = string.Empty;
            //ManagementClass mc = new ManagementClass("win32_processor");
            //ManagementObjectCollection moc = mc.GetInstances();

            //foreach (ManagementObject mo in moc)
            //{
            //cpuInfo = mo.Properties["processorID"].Value.ToString();
            //break;
            //}

            //string drive = "C";
            //ManagementObject dsk = new ManagementObject(
            //@"win32_logicaldisk.deviceid=""" + drive + @":""");
            //dsk.Get();
            //string volumeSerial = dsk["VolumeSerialNumber"].ToString();

            //string uniqueId = cpuInfo + volumeSerial;
            //return uniqueId;
            return string.Empty;
        }
    }
}
