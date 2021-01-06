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

namespace OLS.Casy.App.ViewModels
{
    public class SelectGroupViewModel : ViewModelBase
    {
        private readonly IMeasureResultsService _measureResultsService;
        private readonly INavigationService _navigationService;
        private string _title;
        private ObservableCollection<Group> _groups;

        private Experiment _selectedExperiment;

        public SelectGroupViewModel(IMeasureResultsService measureResultsService,
            INavigationService navigationService)
        {
            _measureResultsService = measureResultsService;
            _navigationService = navigationService;

            Title = "Groups";
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

        public ObservableCollection<Group> Groups
        {
            get => _groups;
            set
            {
                _groups = value;
                RaisePropertyChanged(() => Groups);
            }
        }

        public ICommand SelectGroupCommand => new Command<Group>(async (group) => await SetSelectedGroup(group));

        public override async Task InitializeAsync(Dictionary<string, object> navigationData)
        {
            IsBusy = true;
            _selectedExperiment = navigationData["Experiment"] as Experiment;
            ResetGroups(_selectedExperiment.Name);
            IsBusy = false;
        }

        private async Task SetSelectedGroup(Group group)
        {
            await _navigationService.NavigateToAsync<SelectResultsViewModel>(new Dictionary<string, object> { { "Experiment", _selectedExperiment }, { "Group", group} });
        }

        private async void ResetGroups(string experiment)
        {
            Groups = new ObservableCollection<Group>((await _measureResultsService.GetGroups(experiment)).OrderBy(x => x.Name));
        }
    }
}
