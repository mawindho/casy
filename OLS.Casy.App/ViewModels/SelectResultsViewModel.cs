using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using OLS.Casy.App.Models;
using OLS.Casy.App.Services.MeasureResults;
using OLS.Casy.App.Services.Navigation;
using OLS.Casy.App.ViewModels.Base;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace OLS.Casy.App.ViewModels
{
    public class SelectResultsViewModel : ViewModelBase
    {
        private readonly IMeasureResultsService _measureResultsService;
        private readonly INavigationService _navigationService;
        private string _title;
        private ObservableCollection<MeasureResult> _measureResults;

        private Experiment _selectedExperiment;
        private Group _selectedGroup;

        public SelectResultsViewModel(IMeasureResultsService measureResultsService,
            INavigationService navigationService)
        {
            _measureResultsService = measureResultsService;
            _navigationService = navigationService;

            Title = "Measurement Results";
        }

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                RaisePropertyChanged(() => Title);
            }
        }

        public ObservableCollection<MeasureResult> MeasureResults
        {
            get => _measureResults;
            set
            {
                _measureResults = value;
                RaisePropertyChanged(() => MeasureResults);
            }
        }

        public ICommand ToggleMeasureResultSelectedCommand => new Command<MeasureResult>(async (measureResult) => await ToggleMeasureResultSelected(measureResult));

        public override async Task InitializeAsync(Dictionary<string, object> navigationData)
        {
            IsBusy = true;
            _selectedExperiment = navigationData["Experiment"] as Experiment;
            _selectedGroup = navigationData["Group"] as Group;
            ResetMeasureResults(_selectedExperiment.Name, _selectedGroup.Name);
            IsBusy = false;
        }

        private async Task ToggleMeasureResultSelected(MeasureResult measureResult)
        {
            measureResult.IsSelected = !measureResult.IsSelected;

            if (measureResult.IsSelected)
            {
                await _measureResultsService.AddSelectedMeasureResult(measureResult);
            }
            else
            {
                await _measureResultsService.RemoveSelectedMeasureResult(measureResult);
            }
        }

        private async void ResetMeasureResults(string experiment, string group)
        {
            MeasureResults = new ObservableCollection<MeasureResult>((await _measureResultsService.GetMeasureResults(experiment, group)).OrderBy(x => x.Name));
            MeasureResults.ForEach(x => x.IsSelected = _measureResultsService.SelectedMeasureResults.Any(x2 => x2.Id == x.Id));
        }
    }
}
