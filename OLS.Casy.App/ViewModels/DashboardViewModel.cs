using OLS.Casy.App.Models;
using OLS.Casy.App.Services.MeasureResults;
using OLS.Casy.App.Services.Settings;
using OLS.Casy.App.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace OLS.Casy.App.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private readonly ISettingsService _settingsService;
        private readonly IMeasureResultsService _measureResultsService;

        private string _userName;

        public DashboardViewModel(ISettingsService settingsService,
            IMeasureResultsService measureResultsService)
        {
            _settingsService = settingsService;
            _measureResultsService = measureResultsService;

            LastSelectedLeft = new ObservableCollection<LastSelected>();
            LastSelectedRight = new ObservableCollection<LastSelected>();
            var lastSelected = _settingsService.GetValueOrDefault("LastSelected", string.Empty);
            if(!string.IsNullOrEmpty(lastSelected))
            {
                var split = lastSelected.Split(';');
                for(int i = 0; i < split.Length; i++)
                {
                    if (split.Length < 2) continue;

                    var split2 = split[i].Split('|');
                    if (split2.Length >= 2)
                    {
                        if ((i + 1) % 2 == 0)
                        {
                            LastSelectedRight.Add(new LastSelected { Id = int.Parse(split2[0]), Name = split2[1] });
                        }
                        else
                        {
                            LastSelectedLeft.Add(new LastSelected { Id = int.Parse(split2[0]), Name = split2[1] });
                        }
                    }
                }
            }
        }

        public ObservableCollection<LastSelected> LastSelectedLeft { get; }

        public ObservableCollection<LastSelected> LastSelectedRight { get; }

        public string UserName
        {
            get => _userName;
            set
            {
                _userName = value;
                RaisePropertyChanged(() => UserName);
            }
        }

        public override async Task InitializeAsync(Dictionary<string, object> navigationData)
        {
            UserName = _settingsService.GetValueOrDefault("LoggedInUser", string.Empty);
        }

        public ICommand SelectLastSelected => new Command<LastSelected>(async (lastSelected) => await OnSelectLastSelected(lastSelected));

        private async Task OnSelectLastSelected(LastSelected lastSelected)
        {
            await _measureResultsService.AddSelectedMeasureResult(lastSelected);
        }
    }
}
