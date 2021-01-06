using System;
using System.Threading;

namespace OLS.Casy.Com
{
    internal class CasyCommandWrapper
    {
        public CasyCommandWrapper()
        {
            Timeout = 60000;
            CommandParameter = 0;
        }

        public CasyCommand Command { get; set; }
        public object CommandParameter { get; set; }
        public IProgress<string> Progress { get; set; }
        public ManualResetEvent Awaiter { get; set; }
        public int Timeout { get; set; }
        public object Result { get; set; }
    }
}
