using System;
using System.Threading;

namespace OLS.Casy.Core.Events
{
    public class ShowInputDialogWrapper
    {
        public ManualResetEvent Awaiter { get; set; }
        public string Result { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string[] MessageParameter { get; set; }
        public string DefaultText { get; set; }
        public string Watermark { get; set; }
        public bool IsCancel { get; set; }
        public Func<string, bool> CanOkDelegate { get; set; }
    }
}
