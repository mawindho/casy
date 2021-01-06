using OLS.Casy.Controller.Api;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Models;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Core.Api;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace OLS.Casy.Ui.MainControls.ViewModels
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(MenuViewModel))]
    public class MenuViewModel : Base.ViewModelBase, IPartImportsSatisfiedNotification
    {
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly IAuthenticationService _authenticationService;
        private readonly IMeasureController _measureController;
        private readonly ICompositionFactory _compositionFactory;

        private NavigationCategoryViewModel _analyzeNavigationCategoryViewModel;
        private NavigationCategoryViewModel _analyzeGraphNavigationCategoryViewModel;
        private NavigationCategoryViewModel _analyzeTableNavigationCategoryViewModel;
        private NavigationCategoryViewModel _analyzeOverlayNavigationCategoryViewModel;
        private NavigationCategoryViewModel _analyzeMeanNavigationCategoryViewModel;
        private bool _isExpandViewCollapsed;
        private GridLength _expandViewHeight;
        private NavigationCategory _previous = NavigationCategory.Dashboard;

        [ImportingConstructor]
        public MenuViewModel(IEventAggregatorProvider eventAggregatorProvider,
            IAuthenticationService authenticationService,
            ISelectedMeasureResultsTreeViewModel selectedMeasureResultsTreeViewModel,
            IMeasureController measureController,
            ICompositionFactory compositionFactory)
        {
            _eventAggregatorProvider = eventAggregatorProvider;
            _authenticationService = authenticationService;
            _measureController = measureController;
            SelectedMeasureResultsTreeViewModel = selectedMeasureResultsTreeViewModel;
            _compositionFactory = compositionFactory;

            NavigationCategories = new ObservableCollection<NavigationCategoryViewModel>();
            _expandViewHeight = new GridLength(1, GridUnitType.Star);
        }

        public ObservableCollection<NavigationCategoryViewModel> NavigationCategories { get; }

        public ICommand ExpandButtonCommand => new OmniDelegateCommand(DoExpand);

        public ISelectedMeasureResultsTreeViewModel SelectedMeasureResultsTreeViewModel { get; }

        public GridLength ExpandViewHeight
        {
            get => _expandViewHeight;
            set
            {
                if (value == _expandViewHeight) return;
                _expandViewHeight = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsExpandViewCollapsed
        {
            get => _isExpandViewCollapsed;
            set
            {
                if (value == _isExpandViewCollapsed) return;
                _isExpandViewCollapsed = value;
                NotifyOfPropertyChange();
            }
        }

        public ICommand ExportCommand => new OmniDelegateCommand(OnExport);

        public void OnImportsSatisfied()
        {
            _eventAggregatorProvider.Instance.GetEvent<NavigateToEvent>().Subscribe(OnNavigateToEvent);

            NavigationCategories.Add(new NavigationCategoryViewModel(NavigationCategory.Dashboard, _authenticationService.GetRoleByName("User"), _eventAggregatorProvider)
            {
                Glyph = "icon_dashboard",
                ChevronState = ChevronState.Hide,
                CanSelect = true
            });
            NavigationCategories[0].IsSelected = true;

            _analyzeNavigationCategoryViewModel = new NavigationCategoryViewModel(NavigationCategory.Analyse, _authenticationService.GetRoleByName("User"), _eventAggregatorProvider)
            {
                Glyph = "icon_analyze",
                ChevronState = ChevronState.Down
            };
            NavigationCategories.Add(_analyzeNavigationCategoryViewModel);

            _analyzeGraphNavigationCategoryViewModel = new NavigationCategoryViewModel(NavigationCategory.AnalyseGraph, _authenticationService.GetRoleByName("User"), _eventAggregatorProvider)
            {
                IsVisible = false,
                ChevronState = ChevronState.Hide,
                CanSelect = true
            };
            NavigationCategories.Add(_analyzeGraphNavigationCategoryViewModel);

            _analyzeOverlayNavigationCategoryViewModel = new NavigationCategoryViewModel(NavigationCategory.AnalyseOverlay, _authenticationService.GetRoleByName("User"), _eventAggregatorProvider)
            {
                IsVisible = false,
                ChevronState = ChevronState.Hide,
                CanSelect = true
            };
            NavigationCategories.Add(_analyzeOverlayNavigationCategoryViewModel);

            _analyzeMeanNavigationCategoryViewModel = new NavigationCategoryViewModel(NavigationCategory.AnalyseMean, _authenticationService.GetRoleByName("User"), _eventAggregatorProvider)
            {
                IsVisible = false,
                ChevronState = ChevronState.Hide,
                CanSelect = true
            };
            NavigationCategories.Add(_analyzeMeanNavigationCategoryViewModel);

            _analyzeTableNavigationCategoryViewModel = new NavigationCategoryViewModel(NavigationCategory.AnalyseTable, _authenticationService.GetRoleByName("User"), _eventAggregatorProvider)
            {
                IsVisible = false,
                ChevronState = ChevronState.Hide,
                CanSelect = true
            };
            NavigationCategories.Add(_analyzeTableNavigationCategoryViewModel);

            NavigationCategories.Add(new NavigationCategoryViewModel(NavigationCategory.MeasureResults, _authenticationService.GetRoleByName("User"), _eventAggregatorProvider)
            {
                Glyph = "icon_data",
                ChevronState = ChevronState.Right
            });
            NavigationCategories.Add(new NavigationCategoryViewModel(NavigationCategory.Template, _authenticationService.GetRoleByName("Operator"), _eventAggregatorProvider)
            {
                Glyph = "icon_template",
                ChevronState = ChevronState.Hide
            });
        }

        private void OnNavigateToEvent(object argument)
        {    
            var navigationArgs = (NavigationArgs)argument;

            if(navigationArgs.NavigationCategory == NavigationCategory.Previous)
            {
                var prevViewModel = NavigationCategories.FirstOrDefault(nc => nc.NavigationCategory == _previous);
                if (prevViewModel == null) return;
                prevViewModel.IsSelected = true;
                _eventAggregatorProvider.Instance.GetEvent<NavigateToEvent>().Publish(new NavigationArgs(_previous));
                return;
            }

            if (navigationArgs.NavigationCategory != NavigationCategory.MeasureResults)
            {
                _previous = navigationArgs.NavigationCategory;
            }

            var viewModel = NavigationCategories.FirstOrDefault(nc => nc.NavigationCategory == navigationArgs.NavigationCategory);
            if(viewModel != null && !viewModel.IsSelected)
            {
                viewModel.IsSelected = true;
            }

            switch (navigationArgs.NavigationCategory)
            {
                case NavigationCategory.AnalyseGraph:
                case NavigationCategory.AnalyseTable:
                case NavigationCategory.AnalyseOverlay:
                case NavigationCategory.AnalyseMean:
                    if (_analyzeNavigationCategoryViewModel.ChevronState != ChevronState.Up)
                    {
                        _analyzeNavigationCategoryViewModel.ChevronState = ChevronState.Up;
                        _analyzeGraphNavigationCategoryViewModel.IsVisible = true;
                        _analyzeTableNavigationCategoryViewModel.IsVisible = true;
                        _analyzeOverlayNavigationCategoryViewModel.IsVisible = true;
                        _analyzeMeanNavigationCategoryViewModel.IsVisible = true;
                    }
                    break;
                case NavigationCategory.Analyse:
                    if(navigationArgs.Parameter == null)
                    { 
                        _analyzeNavigationCategoryViewModel.ChevronState = _analyzeNavigationCategoryViewModel.ChevronState == ChevronState.Up ? ChevronState.Down : ChevronState.Up;
                        _analyzeGraphNavigationCategoryViewModel.IsVisible = _analyzeNavigationCategoryViewModel.ChevronState == ChevronState.Up;
                        _analyzeTableNavigationCategoryViewModel.IsVisible = _analyzeNavigationCategoryViewModel.ChevronState == ChevronState.Up;
                        _analyzeOverlayNavigationCategoryViewModel.IsVisible = _analyzeNavigationCategoryViewModel.ChevronState == ChevronState.Up;
                        _analyzeMeanNavigationCategoryViewModel.IsVisible = _analyzeNavigationCategoryViewModel.ChevronState == ChevronState.Up;
                    }
                    break;
                case NavigationCategory.Measurement:
                    if (navigationArgs.Parameter != null)
                    {
                        _analyzeGraphNavigationCategoryViewModel.IsSelected = true;
                        _measureController.SelectedTemplate = navigationArgs.Parameter as MeasureSetup;
                    }
                    break;
            }
        }

        private void DoExpand()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsExpandViewCollapsed = !_isExpandViewCollapsed;
                ExpandViewHeight = _isExpandViewCollapsed ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
            });
        }

        private void OnExport()
        {
            Task.Factory.StartNew(() =>
            {
                var awaiter = new ManualResetEvent(false);
                var viewModelExport = _compositionFactory.GetExport<IExportDialogModel>();
                var viewModel = viewModelExport.Value;

                var wrapper = new ShowCustomDialogWrapper
                {
                    Awaiter = awaiter,
                    DataContext = viewModel,
                    DialogType = typeof(IExportDialog)
                };

                _eventAggregatorProvider.Instance.GetEvent<ShowCustomDialogEvent>().Publish(wrapper);

                awaiter.WaitOne();

                _compositionFactory.ReleaseExport(viewModelExport);
            });
        }

        public ICommand RemoveAllFromSelectionCommand => new OmniDelegateCommand(OnRemoveAllFromSelection);

        private void OnRemoveAllFromSelection()
        {
            SelectedMeasureResultsTreeViewModel.RemoveAllFromSelection();
        }
    }
}
