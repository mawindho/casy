using OLS.Casy.Controller.Api;
using OLS.Casy.Core;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Models;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Core.Api;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;

namespace OLS.Casy.Ui.Core.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(SelectMeasurementsViewModel))]
    public class SelectMeasurementsViewModel : ViewModelBase, IPartImportsSatisfiedNotification
    {
        private readonly IMeasureResultManager _measureResultManager;
        private readonly IMeasureResultStorageService _measureResultStorageService;
        private readonly IAuthenticationService _authenticationService;

        private readonly SmartCollection<MeasureResult> _measureResults;
        private readonly SmartCollection<MeasureResult> _selectedMeasureResults;

        private ListCollectionView _measureResultsViewSource;
        private ListCollectionView _selectedMeasureResultsViewSource;

        private string _filterName;
        private string _filterExperiment;
        private string _filterGroup;
        private string _filterDate;

        [ImportingConstructor]
        public SelectMeasurementsViewModel(
            IMeasureResultManager measureResultManager,
            IMeasureResultStorageService measureResultStorageService,
            IAuthenticationService authenticationService)
        {
            _measureResultManager = measureResultManager;
            _measureResultStorageService = measureResultStorageService;
            _authenticationService = authenticationService;

            _measureResults = new SmartCollection<MeasureResult>();
            _selectedMeasureResults = new SmartCollection<MeasureResult>();
        }

        public ListCollectionView MeasureResultsViewSource
        {
            get
            {
                if (_measureResultsViewSource == null)
                {
                    _measureResultsViewSource = CollectionViewSource.GetDefaultView(this._measureResults) as ListCollectionView;
                    _measureResultsViewSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
                    _measureResultsViewSource.Filter = item =>
                    {
                        var measureResult = item as MeasureResult;

                        DateTime filterDate = default(DateTime);

                        var result = !_selectedMeasureResults.Any(x => x.Experiment == measureResult.Experiment && x.Group == measureResult.Group && x.Name == measureResult.Name)
                            && (string.IsNullOrEmpty(_filterName) || measureResult.Name.IndexOf(this._filterName, StringComparison.OrdinalIgnoreCase) >= 0)
                            && (string.IsNullOrEmpty(_filterExperiment) || measureResult.Experiment == _filterExperiment)
                            && (string.IsNullOrEmpty(_filterGroup) || measureResult.Group == _filterGroup)
                            && (string.IsNullOrEmpty(_filterDate) || !DateTime.TryParse(_filterDate, out filterDate) || measureResult.MeasuredAt.Date == filterDate.Date);

                        return result;
                    };
                    _measureResultsViewSource.IsLiveFiltering = true;
                    _measureResultsViewSource.IsLiveSorting = true;
                }
                return _measureResultsViewSource;
            }
        }

        public ListCollectionView SelectedMeasureResultsViewSource
        {
            get
            {
                if (_selectedMeasureResultsViewSource == null)
                {
                    _selectedMeasureResultsViewSource = CollectionViewSource.GetDefaultView(this._selectedMeasureResults) as ListCollectionView;
                    _selectedMeasureResultsViewSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
                    _selectedMeasureResultsViewSource.IsLiveSorting = true;
                }
                return _selectedMeasureResultsViewSource;
            }
        }

        public INotifyCollectionChanged SelectedMeasureResults
        {
            get { return this._selectedMeasureResults; }
        }

        public string FilterName
        {
            get { return _filterName; }
            set
            {
                if(value != _filterName)
                {
                    this._filterName = value;
                    NotifyOfPropertyChange();
                    MeasureResultsViewSource.Refresh();
                }
            }
        }

        public string FilterDate
        {
            get { return _filterDate; }
            set
            {
                if (value != _filterDate)
                {
                    this._filterDate = value;
                    NotifyOfPropertyChange();
                    MeasureResultsViewSource.Refresh();
                }
            }
        }

        public IEnumerable<string> KnownExperiments
        {
            get => _measureResultManager.GetExperiments().OrderBy(x => x);
        }

        public string FilterExperiment
        {
            get => _filterExperiment;
            set
            {
                if (value != _filterExperiment)
                {
                    _filterExperiment = value;
                    NotifyOfPropertyChange();
                    NotifyOfPropertyChange("KnownGroups");

                    MeasureResultsViewSource.Refresh();
                }
            }
        }

        public IEnumerable<string> KnownGroups
        {
            get
            {
                return _measureResultManager.GetGroups(_filterExperiment).OrderBy(x => x);
            }
        }

        public string FilterGroup
        {
            get { return _filterGroup; }
            set
            {
                var newValue = value == string.Empty ? null : value;

                if (newValue != _filterGroup)
                {
                    _filterGroup = newValue;
                    NotifyOfPropertyChange();

                    MeasureResultsViewSource.Refresh();
                }
            }
        }

        public ICommand SelectedItemCommand
        {
            get { return new OmniDelegateCommand<MeasureResult>(OnSelectItem); }
        }

        public ICommand SelectAllCommand
        {
            get { return new OmniDelegateCommand(OnSelectAll); }
        }

        public ICommand DeselectedItemCommand
        {
            get { return new OmniDelegateCommand<MeasureResult>(OnDeselectItem); }
        }

        public ICommand DeselectAllCommand
        {
            get { return new OmniDelegateCommand(OnDeselectAll); }
        }

        public void OnImportsSatisfied()
        {
            this._selectedMeasureResults.AddRange(_measureResultManager.SelectedMeasureResults.ToArray());

            LoadMeasureResults();
        }

        private void LoadMeasureResults()
        {
            var isSupervisor = this._authenticationService.LoggedInUser.UserRole.Priority == 3;
            var userId = this._authenticationService.LoggedInUser.Id;
            var groupIds = this._authenticationService.LoggedInUser.UserGroups.Select(g => g.Id);

            var measureResults = _measureResultStorageService.GetMeasureResults(_filterExperiment, _filterGroup, nullAsNoValue: true).Where(mr => (isSupervisor || mr.AccessMappings.Count == 0 || mr.AccessMappings.Any(am => am.UserId.HasValue && am.UserId.Value == userId) || mr.AccessMappings.Where(x => x.UserGroupId.HasValue).Select(x => x.UserGroupId.Value).Intersect(groupIds).Any()));
            _measureResults.AddRange(measureResults.ToArray());
        }

        private void OnSelectItem(MeasureResult measureResult)
        {
            this._selectedMeasureResults.Add(measureResult);
            MeasureResultsViewSource.Refresh();
        }

        private void OnDeselectItem(MeasureResult measureResult)
        {
            this._selectedMeasureResults.Remove(measureResult);
            MeasureResultsViewSource.Refresh();
        }

        private void OnSelectAll()
        {
            this._selectedMeasureResults.AddRange(_measureResultsViewSource.Cast<MeasureResult>());
            MeasureResultsViewSource.Refresh();
        }

        private void OnDeselectAll()
        {
            this._selectedMeasureResults.Clear();
            MeasureResultsViewSource.Refresh();
        }
    }
}
