using System;
using System.Collections.Generic;

namespace OLS.Casy.Core.Notification.Api
{
    public interface INotificationService
    {
        event EventHandler NotificationsChanged;

        IEnumerable<NotificationObject> Notifications { get; }

        NotificationObject CreateNotification(NotificationType notificationType);
        void RemoveNotification(NotificationObject notification);
        void AddNotification(NotificationObject notificationObject);
    }
}
