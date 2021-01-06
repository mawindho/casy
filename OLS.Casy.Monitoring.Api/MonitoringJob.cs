using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OLS.Casy.Monitoring.Api
{
    public class MonitoringJob : IDisposable
    {
        public string Name { get; set; }
        public int IntervalInSeconds { get; set; }
        public Func<string, string> JobFunction { get; set; } 
        public Timer Timer { get; set; }

        public void Dispose()
        {
            Timer?.Dispose();
        }
    }
}
