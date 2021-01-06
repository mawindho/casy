using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
    public class SelectMeasureResultsViewModel : ViewModelBase
    {
        private readonly IMeasureResultsService _measureResultsService;
        private readonly INavigationService _navigationService;

        private string _title;
        private ObservableCollection<Experiment> _experiments;
        private ObservableCollection<Group> _groups;
        private ObservableCollection<MeasureResult> _measureResults;

        private Experiment _selectedExperiment;
        private Group _selectedGroup;

        public SelectMeasureResultsViewModel(IMeasureResultsService measureResultsService,
            INavigationService navigationService)
        {
            _measureResultsService = measureResultsService;
            _navigationService = navigationService;

            if (Device.Idiom == TargetIdiom.Tablet || Device.Idiom == TargetIdiom.Desktop)
            {
                Title = "Select measurement results";
            }
            else
            {
                Title = "Experiments";
            }
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

        public ObservableCollection<Experiment> Experiments
        {
            get => _experiments;
            set
            {
                _experiments = value;
                RaisePropertyChanged(() => Experiments);
            }
        }

        public ObservableCollection<Group> Groups
        {
            get => _groups;
            set
            {
                _groups = value;
                RaisePropertyChanged(() => Groups);
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

        public ICommand SelectExperimentCommand => new Command<Experiment>(async (experiment) => await SetSelectedExperiment(experiment));

        public ICommand SelectGroupCommand => new Command<Group>(async (group) => await SetSelectedGroup(group));

        public ICommand ToggleMeasureResultSelectedCommand => new Command<MeasureResult>(async (measureResult) => await ToggleMeasureResultSelected(measureResult));

        private async Task SetSelectedExperiment(Experiment experiment)
        {
            if (Device.Idiom == TargetIdiom.Tablet || Device.Idiom == TargetIdiom.Desktop)
            {
                IsBusy = true;
                _selectedExperiment = experiment;
                Experiments.ForEach(x => x.IsSelected = false);
                experiment.IsSelected = true;
                ResetGroups(experiment.Name);
                IsBusy = false;
            }
            else
            {
                await _navigationService.NavigateToAsync<SelectGroupViewModel>(new Dictionary<string, object> { {"Experiment", experiment} });
            }
        }

        private async Task SetSelectedGroup(Group group)
        {
            IsBusy = true;
            _selectedGroup = group;
            Groups.ForEach(x => x.IsSelected = false);
            group.IsSelected = true;
            ResetMeasureResults(_selectedExperiment.Name, group.Name);
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

        public override async Task InitializeAsync(Dictionary<string, object> navigationData)
        {
            IsBusy = true;
            //await _measureResultsService.Initialize();
            ResetExperiments();
            IsBusy = false;
        }

        private async void ResetExperiments()
        {
            Experiments = new ObservableCollection<Experiment>((await _measureResultsService.GetExperiments()).OrderBy(x => x.Name));
        }

        private async void ResetGroups(string experiment)
        {
            MeasureResults = new ObservableCollection<MeasureResult>();
            Groups = new ObservableCollection<Group>((await _measureResultsService.GetGroups(experiment)).OrderBy(x => x.Name));
        }

        private async void ResetMeasureResults(string experiment, string group)
        {
            MeasureResults = new ObservableCollection<MeasureResult>((await _measureResultsService.GetMeasureResults(experiment, group)).OrderBy(x => x.Name));
            MeasureResults.ForEach(x => x.IsSelected = _measureResultsService.SelectedMeasureResults.Any(x2 => x2.Id == x.Id));
        }
    }
}
