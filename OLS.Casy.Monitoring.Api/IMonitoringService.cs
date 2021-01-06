using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OLS.Casy.Monitoring.Api
{
    public interface IMonitoringService
    {
        void RegisterMonitoringJob(MonitoringJob monitoringJob);
        void UpdateMonitoringValue(string name, object value);
        object GetMonitoringValue(string name);
    }
}
