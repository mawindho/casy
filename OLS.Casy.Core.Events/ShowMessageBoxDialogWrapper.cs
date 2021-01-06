using System.Threading;

namespace OLS.Casy.Core.Events
{
    public class ShowMessageBoxDialogWrapper
    {
        public ManualResetEvent Awaiter { get; set; }
        public bool Result { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string[] MessageParameter { get; set; }
        public bool HideCancelButton { get; set; }
    }
}
