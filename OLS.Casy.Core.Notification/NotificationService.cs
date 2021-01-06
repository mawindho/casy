using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Core.Notification.Api;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace OLS.Casy.Core.Notification
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(INotificationService))]
    public class NotificationService : INotificationService, IPartImportsSatisfiedNotification, IDisposable
    {
        private readonly ILocalizationService _localizationService;
        private readonly List<NotificationObject> _notifications;

        [ImportingConstructor]
        public NotificationService(ILocalizationService localizationService)
        {
            this._localizationService = localizationService;
            this._notifications = new List<NotificationObject>();
        }

        public event EventHandler NotificationsChanged;

        public IEnumerable<NotificationObject> Notifications { get => _notifications; }

        public NotificationObject CreateNotification(NotificationType notificationType)
        {
            var notification = new NotificationObject(notificationType, this._localizationService)
            {
                IsUnread = true
            };

            return notification;
        }

        public void RemoveNotification(NotificationObject notification)
        {
            this._notifications.Remove(notification);
            notification.Dispose();
            RaiseNotificationsChanged();
        }

        public void AddNotification(NotificationObject notificationObject)
        {
            this._notifications.Add(notificationObject);
            RaiseNotificationsChanged();
        }

        public void RaiseNotificationsChanged()
        {
            if (NotificationsChanged != null)
            {
                NotificationsChanged.Invoke(this, EventArgs.Empty);
            }
        }

        public void OnImportsSatisfied()
        {
        }

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach(var notification in this._notifications)
                    {
                        notification.Dispose();
                    }
                }

                disposedValue = true;
            }
        }

        ~NotificationService()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
