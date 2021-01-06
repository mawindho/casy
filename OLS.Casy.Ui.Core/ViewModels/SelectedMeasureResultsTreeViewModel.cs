using OLS.Casy.Controller.Api;
using OLS.Casy.Core;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Models;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Core.Api;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace OLS.Casy.Ui.Core.ViewModels
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(ISelectedMeasureResultsTreeViewModel))]
    public class SelectedMeasureResultsTreeViewModel : ViewModelBase, ISelectedMeasureResultsTreeViewModel, IPartImportsSatisfiedNotification
    {
        private readonly IEventAggregatorProvider _eventAggregatorProvider;

        private readonly SmartCollection<MeasureResultTreeItemViewModel> _selectedMeasureResultTreeItemViewModels;
        private ListCollectionView _selectedMeasureResultsViewSource;

        [ImportingConstructor]
        public SelectedMeasureResultsTreeViewModel(
            IMeasureResultManager measureResultManager,
            IMeasureResultStorageService measureResultStorageService,
            IEventAggregatorProvider eventAggregatorProvider)
        {
            MeasureResultManager = measureResultManager;
            _eventAggregatorProvider = eventAggregatorProvider;

            _selectedMeasureResultTreeItemViewModels = new SmartCollection<MeasureResultTreeItemViewModel>();
        }

        public ListCollectionView MeasureResultContainerViewSource
        {
            get
            {
                if (_selectedMeasureResultsViewSource == null)
                {
                    _selectedMeasureResultsViewSource = CollectionViewSource.GetDefaultView(_selectedMeasureResultTreeItemViewModels) as ListCollectionView;
                    _selectedMeasureResultsViewSource.SortDescriptions.Add(new SortDescription("DisplayOrder", ListSortDirection.Descending));
                    _selectedMeasureResultsViewSource.IsLiveSorting = true;
                }
                return _selectedMeasureResultsViewSource;
            }
        }

        public void RemoveFromSelection(MeasureResult measureResult)
        {
            Task.Factory.StartNew(async () =>
            {
                var toRemove = MeasureResultManager.SelectedMeasureResults.FirstOrDefault(mr => mr.MeasureResultId == measureResult.MeasureResultId);
                if (toRemove != null)
                {
                    toRemove.IsVisible = true;

                    await MeasureResultManager.SaveChangedMeasureResults(new[] { toRemove });

                    var viewModel = _selectedMeasureResultTreeItemViewModels.FirstOrDefault(item => item.AssociatedObject == toRemove);
                    if (viewModel != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _selectedMeasureResultTreeItemViewModels.Remove(viewModel);
                        });
                    }
                    await MeasureResultManager.RemoveSelectedMeasureResults(new[] { toRemove });
                }
            });
        }

        public void RemoveAllFromSelection()
        {
            Task.Factory.StartNew(async () =>
            {
                var toRemoves = MeasureResultManager.SelectedMeasureResults.Where(mr => !mr.IsTemporary).ToArray();

                await MeasureResultManager.SaveChangedMeasureResults(toRemoves);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var toRemove in toRemoves)
                    {
                        toRemove.IsVisible = true;

                        var viewModel = _selectedMeasureResultTreeItemViewModels.FirstOrDefault(item => item.AssociatedObject == toRemove);
                        if (viewModel != null)
                        {
                            _selectedMeasureResultTreeItemViewModels.Remove(viewModel);
                        }
                    }
                });
                await MeasureResultManager.RemoveSelectedMeasureResults(toRemoves);
            });
        }

        public void ToggleVisibility(MeasureResult measureResult)
        {
            var selected = MeasureResultManager.SelectedMeasureResults.FirstOrDefault(mr =>
                mr.MeasureResultId == measureResult.MeasureResultId);

            if (selected == null) return;

            selected.IsVisible = !measureResult.IsVisible;
        }

        public IMeasureResultManager MeasureResultManager { get; }

        public void OnImportsSatisfied()
        {
            MeasureResultManager.SelectedMeasureResultsChanged += OnSelectedMeasureResultsChanged;
            _eventAggregatorProvider.Instance.GetEvent<MeasureResultStoredEvent>().Subscribe(OnMeasureResultStored);
            _eventAggregatorProvider.Instance.GetEvent<MeasureResultsDeletedEvent>().Subscribe(OnMeasureResultsDeleted);
        }

        private void OnMeasureResultsDeleted()
        {
            OnSelectedMeasureResultsChanged(null, null);
        }

        private void OnMeasureResultStored()
        {
            OnSelectedMeasureResultsChanged(null, null);
        }

        
        private void OnSelectedMeasureResultsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var curSelected = MeasureResultManager.SelectedMeasureResults.ToList();

            var selected = curSelected.Where(mr => !mr.IsTemporary).Select(measureResult => new MeasureResultTreeItemViewModel(this, MeasureResultTreeItemType.Selected, measureResult.Name, 0, measureResult, measureResult.IsVisible)).ToList();

            Application.Current.Dispatcher.Invoke(() =>
            {
                var toRemoves = this._selectedMeasureResultTreeItemViewModels.ToArray();
                _selectedMeasureResultTreeItemViewModels.Reset(selected);

                foreach (var toRemove in toRemoves)
                {
                    toRemove.Dispose();
                }
            });
        }
    }
}
