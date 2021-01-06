using OLS.Casy.Core.Api;
using System.Threading;

namespace OLS.Casy.Core.Events
{
    public class ShowMultiButtonMessageBoxDialogWrapper
    {
        public ShowMultiButtonMessageBoxDialogWrapper()
        {
            OkButtonUse = ButtonResult.Accept;
            FirstButtonUse = ButtonResult.None;
            SecondButtonUse = ButtonResult.Cancel;
        }

        public ManualResetEvent Awaiter { get; set; }
        public ButtonResult Result { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string[] MessageParameter { get; set; }
        public ButtonResult OkButtonUse { get; set; }
        public ButtonResult FirstButtonUse { get; set; }
        public ButtonResult SecondButtonUse { get; set; }

        public string OkButtonString { get; set; }
        public string FirstButtonString { get; set; }
        public string SecondButtonString { get; set; }
    }
}
