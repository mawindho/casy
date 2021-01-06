using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Input;
using OLS.Casy.Core.Activation;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Ui.Base;

namespace OLS.Casy.Ui.MainControls.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(AboutViewModel))]
    public class AboutViewModel : DialogModelBase, IPartImportsSatisfiedNotification
    {
        private readonly IAppService _appService;
        private readonly IEnvironmentService _environmentService;
        private readonly ILocalizationService _localizationService;
        private readonly IDatabaseStorageService _databaseStorageService;
        private readonly IUpdateService _updateService;
        private DateTime _warrantyExpiresOn;
        private bool _isControlModule;
        private bool _isLocalAuthModule;
        private bool _isAdAuthModule;
        private bool _isAccessControlModule;
        private bool _isCounterModule;
        private bool _isCfr;
        private bool _isTrialModule;
        private string _expiresOn;
        private string _updateChannel;

        [ImportingConstructor]
        public AboutViewModel(IAppService appService,
            IEnvironmentService environmentService,
            ILocalizationService localizationService,
            IDatabaseStorageService databaseStorageService,
            IUpdateService updateService)
        {
            _environmentService = environmentService;
            _appService = appService;
            _localizationService = localizationService;
            _databaseStorageService = databaseStorageService;
            _updateService = updateService;

            UpdateChannels = new ObservableCollection<string> {"Stable", "Development", "Performance" };
        }

        public string Version => _appService.Version;

        public ObservableCollection<string> UpdateChannels { get; }

        public ICommand CheckUpdatesCommand => new OmniDelegateCommand(OnCheckUpdates);

        private void OnCheckUpdates()
        {
            _updateService.CheckForOnlineUpdate();
        }

        public string UpdateChannel
        {
            get => _updateChannel;
            set
            {
                if (value == _updateChannel) return;
                _updateChannel = value;
                NotifyOfPropertyChange();

                _databaseStorageService.SaveSetting("UpdateChannel", value);
            }
        }

        public string WarrantyExpiresOn
        {
            get => _environmentService.GetDateTimeString(_warrantyExpiresOn);
        }

        public bool IsControlModule
        {
            get => _isControlModule;
            set
            {
                if (value == _isControlModule) return;
                _isControlModule = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsLocalAuthModule
        {
            get => _isLocalAuthModule;
            set
            {
                if (value == _isLocalAuthModule) return;
                _isLocalAuthModule = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsAdAuthModule
        {
            get => _isAdAuthModule;
            set
            {
                if (value == _isAdAuthModule) return;
                _isAdAuthModule = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsAccessControlModule
        {
            get => _isAccessControlModule;
            set
            {
                if (value == _isAccessControlModule) return;
                _isAccessControlModule = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsCounterModule
        {
            get => _isCounterModule;
            set
            {
                if (value == _isCounterModule) return;
                _isCounterModule = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsCfrModule
        {
            get => _isCfr;
            set
            {
                if (value == _isCfr) return;
                _isCfr = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsTrialModule
        {
            get => _isTrialModule;
            set
            {
                if (value == _isTrialModule) return;
                _isTrialModule = value;
                NotifyOfPropertyChange();
            }
        }

        public string ExpiresOn
        {
            get => _expiresOn;
            set
            {
                if (value == _expiresOn) return;
                _expiresOn = value;
                NotifyOfPropertyChange();
            }
        }

        public void OnImportsSatisfied()
        {
            base.Title = _localizationService.GetLocalizedString("AboutDialog_Title");

            var license = _environmentService.GetEnvironmentInfo("License") as License;
            _warrantyExpiresOn = license.ValidTo;
            NotifyOfPropertyChange("WarrantyExpiresOn");
            IsControlModule = license.AddOns.Contains("control");
            IsLocalAuthModule = license.AddOns.Contains("localAuth");
            IsAdAuthModule = license.AddOns.Contains("adAuth");
            IsAccessControlModule = license.AddOns.Contains("access");
            IsCounterModule = license.AddOns.Contains("counter");
            IsCfrModule = license.AddOns.Contains("cfr");
            IsTrialModule = license.AddOns.Contains("trial");

            if (IsTrialModule)
            {
                ExpiresOn = _environmentService.GetDateTimeString(license.ValidTo);
            }

            _databaseStorageService.GetSettings().TryGetValue("UpdateChannel", out var updateChannel);
            if (updateChannel == null || string.IsNullOrEmpty(updateChannel.Value))
            {
                UpdateChannel = "Stable";
            }
            else
            {
                UpdateChannel = updateChannel.Value;
            }
        }
    }
}
