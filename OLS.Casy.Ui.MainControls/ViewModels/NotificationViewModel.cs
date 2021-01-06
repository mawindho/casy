using OLS.Casy.Core;
using OLS.Casy.Core.Notification.Api;
using OLS.Casy.Models;
using OLS.Casy.Ui.Base;
using System.Windows.Input;
using System;
using System.ComponentModel;
using DevExpress.Mvvm;

namespace OLS.Casy.Ui.MainControls.ViewModels
{
    public class NotificationViewModel : Base.ViewModelBase, IDisposable
    {
        private readonly NotificationObject _notification;
        private readonly Casy.Core.Notification.Api.INotificationService _notificationService;

        public NotificationViewModel(Casy.Core.Notification.Api.INotificationService notificationService, NotificationObject notification)
        {
            this._notification = notification;
            this._notificationService = notificationService;

            this._notification.PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case "Title":
                    NotifyOfPropertyChange("Title");
                    break;
                case "Message":
                    NotifyOfPropertyChange("Message");
                    break;
                case "ActionDescription":
                    NotifyOfPropertyChange("ButtonText");
                    break;
                case "Action":
                    NotifyOfPropertyChange("ButtonCommand");
                    break;
            }
        }

        public string Title { get => _notification.Title; }
        public string Message { get => _notification.Message; }
        public string ButtonText { get => _notification.ActionDescription; }

        public string NotificationType { get => Enum.GetName(typeof(NotificationType), _notification.NotificationType); }
        public ICommand ButtonCommand
        {
            get 
            {
                return new OmniDelegateCommand(OnNotificationClicked);
            }
        }

        private void OnNotificationClicked()
        {
            _notification.Action.Invoke();
            _notificationService.RemoveNotification(_notification);
        }

        private bool _disposed;
        protected override void Dispose(bool disposing)
        {
            if(!_disposed)
            {
                if(disposing)
                {
                    this._notification.PropertyChanged -= OnPropertyChanged;
                }
            }
            base.Dispose(disposing);
        }
    }
}
