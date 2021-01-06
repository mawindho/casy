using OLS.Casy.App.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Text;
using OLS.Casy.App.Services.Settings;
using OLS.Casy.App.Services.Detection;
using OLS.Casy.App.Models;
using Xamarin.Forms;
using System.Windows.Input;

namespace OLS.Casy.App.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private string _casyEndpoint;

        private readonly ISettingsService _settingsService;
        private readonly IDetectionService _detectionService;

        public SettingsViewModel(ISettingsService settingsService, IDetectionService detectionService)
        {
            _settingsService = settingsService;
            _detectionService = detectionService;

            _casyEndpoint = _settingsService.CasyEndpointBase;
        }

        public string CasyEndpoint
        {
            get => _casyEndpoint;
            set
            {
                _casyEndpoint = value;
                if (!string.IsNullOrEmpty(_casyEndpoint))
                {
                    UpdateCasyEndpoint();
                }
                RaisePropertyChanged(() => CasyEndpoint);
            }
        }

        public IEnumerable<CasyModel> CasyModels => _detectionService.CasyModels;

        public ICommand SetSelectedCasyModelCommand => new Command<CasyModel>(casyModel => CasyEndpoint = $"http://{casyModel.IpAddress}:8536");

        private void UpdateCasyEndpoint()
        {
            GlobalSetting.Instance.CasyEndpointBase = _settingsService.CasyEndpointBase = _casyEndpoint;
        }
    }
}
