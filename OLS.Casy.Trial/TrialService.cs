using OLS.Casy.Core;
using OLS.Casy.Core.Activation;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Config.Api;
using OLS.Casy.Core.Notification.Api;
using OLS.Casy.Monitoring.Api;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OLS.Casy.Trial
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IService))]
    public class TrialService : AbstractService, IPartImportsSatisfiedNotification
    {
        private readonly INotificationService _notificationService;
        private readonly IEnvironmentService _environmentService;
        private readonly IMonitoringService _monitoringService;
        private readonly IAuthenticationService _authenticationService;

        [ImportingConstructor]
        public TrialService(IConfigService configService,
            INotificationService notificationService,
            IEnvironmentService environmentService,
            IMonitoringService monitoringService,
            IAuthenticationService authenticationService)
            : base(configService)
        {
            this._notificationService = notificationService;
            this._environmentService = environmentService;
            this._monitoringService = monitoringService;
            this._authenticationService = authenticationService;
        }

        public void OnImportsSatisfied()
        {
            RegisterJobs();
        }

        private void RegisterJobs()
        {
            if (this._monitoringService != null)
            {
                this._monitoringService.RegisterMonitoringJob(new MonitoringJob
                {
                    Name = Enum.GetName(typeof(MonitoringTypes), MonitoringTypes.CheckTrial),
                    IntervalInSeconds = (int)TimeSpan.FromHours(1).TotalSeconds,
                    JobFunction = parameter =>
                    {
                        if (this._authenticationService.LoggedInUser == null)
                        {
                            return null;
                        }

                        var license = this._environmentService.GetEnvironmentInfo("License") as License;

                        var dateTimeToCheck = license.ValidTo;
                        var dateTimeNow = DateTime.UtcNow;
                        var difference = dateTimeNow - dateTimeToCheck;

                        if (difference.TotalHours <= (3d * 24d))
                        {
                            if (!this._notificationService.Notifications.Any(n => n.NotificationType == NotificationType.TrialExpires))
                            {
                                var notification = _notificationService.CreateNotification(NotificationType.TrialExpires);
                                notification.Title = "Notification_TrialExpires_Title";
                                notification.Message = "Notification_TrialExpires_Message";
                                notification.ActionDescription = "Notification_TrialExpires_Action";
                                notification.Action = () =>
                                {
                                };
                                this._notificationService.AddNotification(notification);
                            }
                        }

                        return null;
                    }
                });
            }
        }
    }
}
