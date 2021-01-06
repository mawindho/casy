using OLS.Casy.Core.Localization.Api;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OLS.Casy.Core
{
    public enum NotificationType
    {
        LessCounts,
        NoCounts,
        WeeklyCleanMandetory,
        WeeklyCleanAnnouncementNotification,
        TrialExpires,
        InvalidCalibrationsFound,
        NoNotifications
    }

    public class NotificationObject : INotifyPropertyChanged, IDisposable
    {
        private readonly ILocalizationService _localizationService;

        private bool _isUnread;
        private string _title;
        private string _message;
        private string _actionDescription;
        private Action _action;

        public NotificationObject(NotificationType notificationType, ILocalizationService localizationService)
        {
            this._localizationService = localizationService;
            this._localizationService.LanguageChanged += OnLanguageChanged;

            this.NotificationId = Guid.NewGuid();
            this.NotificationType = notificationType;
        }

        public NotificationType NotificationType { get; private set; }

        public Guid NotificationId { get; private set; }
        public bool IsUnread
        {
            get { return _isUnread; }
            set
            {
                if (value != _isUnread)
                {
                    _isUnread = value;
                    NotifyOfPropertyChange();
                }
            }
        }
        public string Title
        {
            get { return _localizationService.GetLocalizedString(_title); }
            set
            {
                if (value != _title)
                {
                    _title = value;
                    NotifyOfPropertyChange();
                }
            }
        }
        public string Message
        {
            get { return _localizationService.GetLocalizedString(_message); }
            set
            {
                if (value != _message)
                {
                    _message = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string ActionDescription
        {
            get { return _localizationService.GetLocalizedString(_actionDescription); }
            set
            {
                if (value != _actionDescription)
                {
                    _actionDescription = value;
                    NotifyOfPropertyChange();
                }
            }
        }
        public Action Action
        {
            get { return _action; }
            set
            {
                if (value != _action)
                {
                    _action = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyOfPropertyChange([CallerMemberName] string callerMemberName = "")
        {
            this.NotifyOfPropertyChangeInternal(callerMemberName);
        }

        private void NotifyOfPropertyChangeInternal(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnLanguageChanged(object sender, LocalizationEventArgs e)
        {
            this.NotifyOfPropertyChange("ActionDescription");
            this.NotifyOfPropertyChange("Message");
            this.NotifyOfPropertyChange("Title");
        }

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this._localizationService.LanguageChanged -= OnLanguageChanged;
                }

                disposedValue = true;
            }
        }

        ~NotificationObject()
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
