using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OLS.Casy.Core.Activation.Model
{
    public class ActivationModel
    {
        public string CpuId { get; set; }
        public string ActivationKey { get; set; }
        public string SerialNumber { get; set; }
        public bool IsValid { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public string ProductType { get; set; }
        public string ValidationError { get; set; }
        public string ComputerName { get; set; }
        public string CalibrationDownloadPath { get; set; }
        public List<string> AddOns { get; set; }
    }
}
