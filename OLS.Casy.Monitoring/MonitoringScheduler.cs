using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace OLS.Casy.Monitoring
{
    internal class MonitoringScheduler : IDisposable
    {
        private IScheduler _scheduler;
        private bool disposedValue = false; // To detect redundant calls
        private static MonitoringScheduler _self;

        private Dictionary<string, JobKey> _jobKeys;

        public MonitoringScheduler()
        {
            var schedFact = new StdSchedulerFactory();
            this._scheduler = schedFact.GetScheduler();

            MonitoringScheduler._self = this;
            this._jobKeys = new Dictionary<string, JobKey>();
        }

        ~MonitoringScheduler()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        public Action<string, IDictionary<string, object>> OnJobTriggered { get; set; }

        public void RegisterJob(string jobName, int intervallInSeconds, Dictionary<string, object> jobData = null)
        {
            var trigger = TriggerBuilder.Create().WithIdentity(jobName).StartNow().WithSimpleSchedule(x => x.WithIntervalInSeconds(intervallInSeconds).RepeatForever()).Build();

            var jobBuilder = JobBuilder.Create<SchedulerJob>().WithIdentity(jobName).UsingJobData("jobName", jobName);

            if (jobData != null)
            {
                foreach (var data in jobData)
                {
                    jobBuilder = jobBuilder.UsingJobData(data.Key, data.Value as string);
                }
            }

            var jobDetail = jobBuilder.Build();

            if (this._scheduler == null)
            {
                NameValueCollection props = new NameValueCollection
                {
                    { "quartz.serializer.type", "binary" }
                };

                var schedFact = new StdSchedulerFactory(props);
                this._scheduler = schedFact.GetScheduler();
            }

            _scheduler.ScheduleJob(jobDetail, trigger);

            _jobKeys.Add(jobName, jobDetail.Key);

            //OnJobTriggered(jobName, jobData);
        }

        public void UnregisterJob(string jobName)
        {
            this._scheduler.DeleteJob(_jobKeys[jobName]);
        }

        public void Reset()
        {
            _scheduler.Shutdown();
        }

        public void Start()
        {
            this._scheduler.StartDelayed(TimeSpan.FromSeconds(10));
        }

        private class SchedulerJob : IJob
        {
            public void Execute(IJobExecutionContext context)
            {
                MonitoringScheduler._self.OnJobTriggered.Invoke(
                    context.JobDetail.JobDataMap.GetString("jobName"),
                    context.JobDetail.JobDataMap.WrappedMap);
            }
        }

        #region IDisposable Support


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (this._scheduler != null)
                    {
                        this._scheduler.Shutdown(false);
                        this._scheduler = null;
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        #endregion
    }
}
