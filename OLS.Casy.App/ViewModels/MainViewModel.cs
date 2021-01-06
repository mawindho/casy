using OLS.Casy.App.Models;
using OLS.Casy.App.ViewModels.Base;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using OLS.Casy.App.Services.MeasureResults;
using Xamarin.Forms;
using System;
using Xamarin.Forms.Internals;
using OLS.Casy.App.Services.Settings;

namespace OLS.Casy.App.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ISettingsService _settingsService;
        private IMeasureResultsService _measureResultsService;
        private ObservableCollection<MeasureResult> _selectedMeasurements;
        private bool _isDashboardSelected;
        private bool _isMeasureResultsSelected;
        private bool _isSingleSelected;
        private bool _isOverlaySelected;
        private bool _isMeanSelected;

        public MainViewModel(ISettingsService settingsService, IMeasureResultsService measureResultsService)
        {
            _settingsService = settingsService;
            _measureResultsService = measureResultsService;
            _measureResultsService.SelectedMeasureResultsChanged += OnSelectedMeasureResultsChanged;
            IsDashboardSelected = true;
        }

        public ICommand DashboardCommand => new Command(async () => await DashboardAsync());
        public ICommand MeasureResultsCommand => new Command(async () => await MeasureResultsAsync());
        public ICommand SingleResultCommand => new Command(async () => await SingleResultAsync());
        public ICommand OverlayCommand => new Command(async () => await OverlayAsync());
        public ICommand MeanCommand => new Command(async () => await MeanAsync());
        public ICommand RemoveCommand => new Command<MeasureResult>(async (measureResult) => await RemoveSelected(measureResult));
        public ICommand LogoutCommand => new Command(async () => await LogoutAsync());

        private async Task RemoveSelected(MeasureResult measureResult)
        {
            await _measureResultsService.RemoveSelectedMeasureResult(measureResult);
        }

        public ObservableCollection<MeasureResult> SelectedMeasurements
        {
            get => _selectedMeasurements;
            set
            {
                _selectedMeasurements = value;
                OnPropertyChanged();
            }
        }

        private async Task DashboardAsync()
        {
            IsDashboardSelected = true;
            IsMeasureResultsSelected = false;
            IsSingleSelected = false;
            IsOverlaySelected = false;
            IsMeanSelected = false;
            await NavigationService.NavigateToAsync<DashboardViewModel>();
        }

        public bool IsDashboardSelected
        {
            get => _isDashboardSelected;
            set
            {
                _isDashboardSelected = value;
                RaisePropertyChanged(() => IsDashboardSelected);
            }
        }

        private async Task LogoutAsync()
        {
            _settingsService.AuthAccessToken = string.Empty;
            await _settingsService.AddOrUpdateValue("LoggedInUser", string.Empty);

            await NavigationService.NavigateToAsync<LoginViewModel>();
        }

        private async Task MeasureResultsAsync()
        {
            IsDashboardSelected = false;
            IsMeasureResultsSelected = true;
            IsSingleSelected = false;
            IsOverlaySelected = false;
            IsMeanSelected = false;
            await NavigationService.NavigateToAsync<SelectMeasureResultsViewModel>();
        }

        public bool IsMeasureResultsSelected
        {
            get => _isMeasureResultsSelected;
            set
            {
                _isMeasureResultsSelected = value;
                RaisePropertyChanged(() => IsMeasureResultsSelected);
            }
        }

        private async Task SingleResultAsync()
        {
            IsDashboardSelected = false;
            IsMeasureResultsSelected = false;
            IsSingleSelected = true;
            IsOverlaySelected = false;
            IsMeanSelected = false;
            await NavigationService.NavigateToAsync<SingleMeasurementViewModel>();
        }

        public bool IsSingleSelected
        {
            get => _isSingleSelected;
            set
            {
                _isSingleSelected = value;
                RaisePropertyChanged(() => IsSingleSelected);
            }
        }

        private async Task OverlayAsync()
        {
            IsDashboardSelected = false;
            IsMeasureResultsSelected = false;
            IsSingleSelected = false;
            IsOverlaySelected = true;
            IsMeanSelected = false;
            await NavigationService.NavigateToAsync<OverlayViewModel>();
        }

        public bool IsOverlaySelected
        {
            get => _isOverlaySelected;
            set
            {
                _isOverlaySelected = value;
                RaisePropertyChanged(() => IsOverlaySelected);
            }
        }

        private async Task MeanAsync()
        {
            IsDashboardSelected = false;
            IsMeasureResultsSelected = false;
            IsSingleSelected = false;
            IsOverlaySelected = false;
            IsMeanSelected = true;
            await NavigationService.NavigateToAsync<MeanViewModel>();
        }

        public bool IsMeanSelected
        {
            get => _isMeanSelected;
            set
            {
                _isMeanSelected = value;
                RaisePropertyChanged(() => IsMeanSelected);
            }
        }

        public override async Task InitializeAsync(Dictionary<string, object> navigationData)
        {
            IsBusy = true;
            if (Device.Idiom == TargetIdiom.Tablet || Device.Idiom == TargetIdiom.Desktop)
            {
                await DashboardAsync();
            }
            SelectedMeasurements = new ObservableCollection<MeasureResult>(_measureResultsService.SelectedMeasureResults);
            IsBusy = false;
        }

        private void OnSelectedMeasureResultsChanged(object sender, EventArgs e)
        {
            IsBusy = true;
            SelectedMeasurements = new ObservableCollection<MeasureResult>(_measureResultsService.SelectedMeasureResults);
            IsBusy = false;
        }
    }
}
