using System;
using System.Threading;

namespace OLS.Casy.Core.Events
{
    public class ShowCustomDialogWrapper
    {
        public ManualResetEvent Awaiter { get; set; }
        public string Title { get; set; }
        public object TitleBinding { get; set; }
        public object DataContext { get; set; }
        public Type DialogType { get; set; }
    }
}
