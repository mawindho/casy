using OLS.Casy.Core.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Core.Notification.Api;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OLS.Casy.Controller.MC
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IMeasureCounter))]
    public class MeasureCounter : IMeasureCounter, IPartImportsSatisfiedNotification
    {
        //private const string UNIQUEKEY = "BUe8ysitfaSfwy01Srib120qndwHOFhmDhAuzbHxsRmtUyCw3wdX8kHEQ";

        private readonly IEncryptionProvider _encryptionProvider;
        private readonly INotificationService _notificationService;
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly ILocalizationService _localizationService;
        private int _counts;

        public event EventHandler PossibleCountManipulationDetected;
        public event EventHandler AvailableCountsChanged;

        [ImportingConstructor]
        public MeasureCounter(IEncryptionProvider encryptionProvider,
            INotificationService notificationService,
            IEventAggregatorProvider eventAggregatorProvider,
            ILocalizationService localizationService)
        {
            this._encryptionProvider = encryptionProvider;
            this._notificationService = notificationService;
            this._eventAggregatorProvider = eventAggregatorProvider;
            this._localizationService = localizationService;
        }

        public void DecreaseCounts(int num)
        {
            _counts = GetAvailableCounts();
            _counts -= num;
            foreach (var action in StoreCounts)
            {
                action.Value.Invoke(_counts);
            }

            if (AvailableCountsChanged != null)
            {
                AvailableCountsChanged.Invoke(this, EventArgs.Empty);
            }
            CheckForNotifications();
        }

        public int CountsLeft
        {
            get { return _counts; }
        }
        
        public int GetAvailableCounts()
        {
            var getCounts = this.GetCounts;
            int[] counts = new int[getCounts.Count()];
            for(int i = 0; i < counts.Length; i++)
            {
                counts[i] = getCounts.ElementAt(i).Value.Invoke();
            }

            if(counts.Distinct().Count() > 1)
            {
                PossibleCountManipulationDetected?.Invoke(this, null);
                return 0;
            }
            
            _counts = counts[0];

            return _counts;

            //_counts = 102;
            //return _counts;
        }

        public bool HasAvailableCounts(int num)
        {
            var counts = GetAvailableCounts();
            return counts >= num;
        }

        public void OnImportsSatisfied()
        {
            GetAvailableCounts();
            CheckForNotifications();
        }

        private void CheckForNotifications()
        {
            if (_counts <= 100 && _counts > 0)
            {
                if (!_notificationService.Notifications.Any(n => n.NotificationType == Core.NotificationType.LessCounts))
                {
                    var notification = _notificationService.CreateNotification(Core.NotificationType.LessCounts);
                    notification.Title = "Notification_LessCounts_Title";
                    notification.Message = string.Format(this._localizationService.GetLocalizedString("Notification_LessCounts_Title"), _counts);
                    notification.ActionDescription = "Notification_LessCounts_ButtonText";
                    notification.Action = () =>
                    {
                        _eventAggregatorProvider.Instance.GetEvent<ShowSettingsEvent>().Publish();
                    };
                    this._notificationService.AddNotification(notification);
                }
            }
            else if (_counts <= 0)
            {
                if (!_notificationService.Notifications.Any(n => n.NotificationType == Core.NotificationType.NoCounts))
                {
                    var notification = _notificationService.CreateNotification(Core.NotificationType.NoCounts);
                    notification.Title = "Notification_NoCounts_Title";
                    notification.Message = "Notification_NoCounts_Message";
                    notification.ActionDescription = "Notification_NoCounts_ButtonText";
                    notification.Action = () =>
                    {
                        _eventAggregatorProvider.Instance.GetEvent<ShowSettingsEvent>().Publish();
                    };
                    this._notificationService.AddNotification(notification);
                }
            } 
            else
            {
                var toRemoves = _notificationService.Notifications.Where(n => n.NotificationType == Core.NotificationType.LessCounts || n.NotificationType == Core.NotificationType.NoCounts).ToList();
                foreach(var toRemove in toRemoves)
                {
                    _notificationService.RemoveNotification(toRemove);
                }
            }
        }

        [ImportMany("GetCounts")]
        public IEnumerable<Lazy<Func<int>>> GetCounts { get; set; }

        [ImportMany("StoreCounts")]
        public IEnumerable<Lazy<Action<int>>> StoreCounts { get; set; }

        //[ImportMany("CheckIdentifier")]
        //public IEnumerable<Lazy<Func<int, bool>>> CheckIdentifier { get; set; }

        /*
        public bool UnlockCounts(string activationKey)
        {
            string step1 = string.Empty;
            var charArray = activationKey.ToCharArray();
            for(int i = 1; i <= charArray.Length; i++)
            {
                if(i % 5 != 0)
                { 
                    step1 += charArray[i-1];
                }
            }
            var step2 = Convert.FromBase64String(step1);
            var step3 = _encryptionProvider.Decrypt(step2, UNIQUEKEY);
            var step4 = Encoding.UTF8.GetString(step3);
            var step5 = step4.Split(new[] { "|||" }, StringSplitOptions.RemoveEmptyEntries);

            var validTo = new DateTime(Convert.ToInt64(step5[0], CultureInfo.InvariantCulture));

            if(validTo >= DateTime.UtcNow)
            {
                bool isValidIdentifier = true;
                foreach (var action in CheckIdentifier)
                {
                    isValidIdentifier &= action.Value.Invoke(int.Parse(step5[2]));
                }

                if(!isValidIdentifier)
                {
                    return false;
                }

                _counts += Convert.ToInt32(step5[1], CultureInfo.InvariantCulture);
                foreach(var action in StoreCounts)
                {
                    action.Value.Invoke(_counts);
                }

                if(AvailableCountsChanged != null)
                {
                    AvailableCountsChanged.Invoke(this, EventArgs.Empty);
                }

                CheckForNotifications();
                return true;
            }
            return false;
        }
        */
    }
}
