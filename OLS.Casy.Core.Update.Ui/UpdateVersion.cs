
namespace OLS.Casy.Core.Update.Ui
{
    public class UpdateVersion
    {
        public string Version { get; set; }
        public string Location { get; set; }
        public string TempFile { get; set; }
        public string UpdateDirectory { get; set; }
        public bool IsSimulator { get; set; }
        public bool ForceRestart { get; set; }
        public string[] FilesToDelete { get; set; }
    }
}
