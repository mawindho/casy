using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OLS.Casy.Core.Activation.Model
{
    public class UpdateRequest
    {
        public string ActivationKey { get; set; }
        public Version CurrentVersion { get; set; }
        public List<UpdateVersion> UpdateVersions { get; set; }
        public string RequestError { get; set; }
        public string Environment { get; set; }
        public string CpuId { get; set; }
    }
}
