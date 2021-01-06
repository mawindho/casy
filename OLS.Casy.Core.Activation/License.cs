using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace OLS.Casy.Core.Activation
{
    [Serializable]
    public class License
    {
        public string ActivationKey { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public string LicenseType { get; set; }
        public int CountsLeft { get; set; }
        public string[] AddOns { get; set; }
        public bool ReloadCalibration { get; set; }

        [OptionalField]
        public string SerialNumber;
    }
}
