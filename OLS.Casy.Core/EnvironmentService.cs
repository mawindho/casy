using OLS.Casy.Core.Api;
using OLS.Casy.IO.Api;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security;
using System.Security.AccessControl;
using System.Security.Permissions;
using System.Security.Principal;

namespace OLS.Casy.Core
{
    /// <summary>
    /// Implementation of <see cref="IEnvironmentService"/> using simple Dictionary to store information in RAM 
    /// </summary>
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IEnvironmentService))]
    public class EnvironmentService : IEnvironmentService
    {
        private readonly Dictionary<string, object> _environmentInfos;

        /// <summary>
        /// MEF importing constructor
        /// </summary>
        [ImportingConstructor]
        public EnvironmentService()
        {
            this._environmentInfos = new Dictionary<string, object>();
            this.SetEnvironmentInfo("MachineName", System.Environment.MachineName);
        }

        public event EventHandler<string> EnvironmentInfoChangedEvent;

        /// <summary>
        /// Gets environment information for the passed key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Value</returns>
        public object GetEnvironmentInfo(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            object result;
            if (_environmentInfos.TryGetValue(key, out result))
            {
                return result;
            }
            return null;
        }

        public string GetExecutionPath()
        {
            return Assembly.GetEntryAssembly().Location;
        }

        /// <summary>
        /// Sets environment information
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        public void SetEnvironmentInfo(string key, object value)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            if (!_environmentInfos.ContainsKey(key))
            {
                _environmentInfos.Add(key, value);
            }
            else
            {
                _environmentInfos[key] = value;
            }

            EnvironmentInfoChangedEvent?.Invoke(this, key);
        }

        public string GetDateTimeString(DateTime dateTime, bool ignoreTimezone = false)
        {
            var customFormat = (string) GetEnvironmentInfo("DateTimeFormat");
            var dateTimeString = string.Empty;
            if (customFormat != "System")
            {
                try
                {
                    if (ignoreTimezone)
                    {
                        customFormat = customFormat.Replace("zzz", "");
                    }
                    dateTimeString = dateTime.ToString(customFormat);
                }
                catch
                {
                }
            }
            else if (dateTimeString == string.Empty)
            {
                dateTimeString = $"{dateTime.ToShortDateString()} {dateTime.ToLongTimeString()}";
                if(!ignoreTimezone)
                {
                    dateTimeString += $" UTC{dateTime.ToString("zzz")}";
                }
            }

            return dateTimeString;
        }

        public string GetUniqueId()
        {
            string cpuInfo = string.Empty;
            ManagementClass mc = new ManagementClass("win32_processor");
            ManagementObjectCollection moc = mc.GetInstances();

            foreach (ManagementObject mo in moc)
            {
                cpuInfo = mo.Properties["processorID"].Value.ToString();
                break;
            }

            string drive = "C";
            ManagementObject dsk = new ManagementObject(
                @"win32_logicaldisk.deviceid=""" + drive + @":""");
            dsk.Get();
            string volumeSerial = dsk["VolumeSerialNumber"].ToString();

            string uniqueId = cpuInfo + volumeSerial;
            return uniqueId;
        }

        public static bool CheckForInternetConnection()
        {
            try
            {
                using (var client = new WebClient())
                using (client.OpenRead("https://www.google.com/"))
                {
                    return true;
                }
            }
            catch (Exception)
            {
            }
            
            try { 
                var myPing = new Ping();
                const string host = "google.com";
                var buffer = new byte[32];
                const int timeout = 1000;
                var pingOptions = new PingOptions();
                var reply = myPing.Send(host, timeout, buffer, pingOptions);
                return reply != null && (reply.Status == IPStatus.Success);
            }
            catch (Exception) {
            }

            return false;
/*
            try
            {
                using (var client = new WebClient())
                using (var stream = client.OpenRead("http://www.ols-bio.de"))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
*/
        }

        public static bool HasEveryOneWritePermissions(string path)
        {
            //DirectoryInfo di = new DirectoryInfo(path);
            //DirectorySecurity acl = di.GetAccessControl(AccessControlSections.All);

            SecurityIdentifier everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            System.Security.Principal.NTAccount everyOneAccount = everyone.Translate(typeof(System.Security.Principal.NTAccount)) as System.Security.Principal.NTAccount;
            string everyoneAccountString = everyOneAccount.ToString();

            DirectoryInfo dInfo = new DirectoryInfo(path);
            var security = dInfo.GetAccessControl();
            var authorizationRules = security.GetAccessRules(true, true, typeof(NTAccount));

            foreach (var rule in authorizationRules)
            {
                FileSystemAccessRule fileRule = rule as FileSystemAccessRule;
                if (fileRule.IdentityReference.Value.Equals(everyoneAccountString, StringComparison.CurrentCultureIgnoreCase))
                {
                    var filesystemAccessRule = (FileSystemAccessRule)rule;

                    if ((filesystemAccessRule.FileSystemRights & FileSystemRights.WriteData) > 0 && filesystemAccessRule.AccessControlType != AccessControlType.Deny)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
