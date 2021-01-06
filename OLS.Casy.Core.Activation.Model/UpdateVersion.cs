using System;

namespace OLS.Casy.Core.Activation.Model
{
    public class UpdateVersion
    {
        public string Version { get; set; }
        public string Location { get; set; }
        public string[] TempFiles { get; set; }
        public string UpdateDirectory { get; set; }
        public bool IsSimulator { get; set; }
        public bool ForceRestart { get; set; }
        public string[] FilesToDelete { get; set; }
    }
}
