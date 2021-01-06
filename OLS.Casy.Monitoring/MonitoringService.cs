using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Monitoring.Api;
using OLS.Casy.Monitoring.Entities;
using OLS.Casy.Monitoring.IO;
using System;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OLS.Casy.Monitoring
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IMonitoringService))]
    [Export(typeof(IService))]
    public class MonitoringService : IService, IMonitoringService, IPartImportsSatisfiedNotification
    {
        private readonly IAuthenticationService _authenticationService;

        private ConcurrentDictionary<string, MonitoringJob> _monitoringJobs;
        private readonly object _lock = new object();

        [ImportingConstructor]
        public MonitoringService(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
            _monitoringJobs = new ConcurrentDictionary<string, MonitoringJob>();
        }

        public void RegisterMonitoringJob(MonitoringJob monitoringJob)
        {
            if (_monitoringJobs.Any(x => x.Key == monitoringJob.Name)) return;

            var timer = new Timer(state => { Task.Run(() => OnMonitoringJobTriggered(monitoringJob)); }, null, (int) 0, monitoringJob.IntervalInSeconds * 1000);
            monitoringJob.Timer = timer;
            _monitoringJobs.TryAdd(monitoringJob.Name, monitoringJob);
        }

        private void OnMonitoringJobTriggered(MonitoringJob monitoringJob)
        {
            using (var context = new MonitoringContext())
            {
                var entity = GetOrCreateMonitoringItem(context, monitoringJob.Name, out var newlyCreated);

                if (newlyCreated) return;

                var jobResult = monitoringJob.JobFunction.Invoke(entity.Value);
                if (jobResult == null) return;

                entity.Value = jobResult;
                context.SaveChanges();
            }
        }

        public void UpdateMonitoringValue(string name, object value)
        {
            using (var context = new MonitoringContext())
            {
                var entity = GetOrCreateMonitoringItem(context, name, out _);

                if (entity == null) return;
                entity.Value = value.ToString();
                context.SaveChanges();
            }
        }

        public object GetMonitoringValue(string name)
        {
            using (var context = new MonitoringContext())
            {
                var entity = GetOrCreateMonitoringItem(context, name, out _);
                return entity.Value;
            }
        }

        private MonitoringItemEntity GetOrCreateMonitoringItem(MonitoringContext context, string name,
            out bool newlyCreated)
        {
            var entity = context.MonitoringItems.FirstOrDefault(e => e.Name == name);
            if (entity == null)
            {
                entity = new MonitoringItemEntity
                {
                    Id = -1,
                    Name = name,
                    Type = "string"
                };

                newlyCreated = true;
                context.MonitoringItems.Add(entity);
                context.SaveChanges();
            }
            else
            {
                newlyCreated = false;
            }

            return entity;
        }

        public void OnImportsSatisfied()
        {
            this._authenticationService.UserLoggedIn += OnUserLoggedIn;
        }

        private void OnUserLoggedIn(object sender, AuthenticationEventArgs e)
        {
            foreach(var job in this._monitoringJobs)
            {
                OnMonitoringJobTriggered(job.Value);
            }
        }

        public void Prepare(IProgress<string> progress)
        {
        }

        public void Deinitialize(IProgress<string> progress)
        {
            foreach (var job in _monitoringJobs)
            {
                job.Value.Dispose();
            }
            _monitoringJobs.Clear();
        }
    }
}
