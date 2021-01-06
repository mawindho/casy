using System;
using System.Threading;
using System.Windows.Input;

namespace OLS.Casy.Core.Events
{
    public class ShowProgressDialogWrapper
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string[] MessageParameter { get; set; }
        public bool IsFinished { get; set; }
        //public ManualResetEvent Awaiter { get; set; }
        public bool IsCancelButtonAvailable { get; set; }
        public Action<bool> CancelAction { get; set; }
    }
}
