using OLS.Casy.Ui.Api;
using OLS.Casy.Ui.Base;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;
using OLS.Casy.Ui.MainControls.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Api;
using System.Collections.ObjectModel;
using System.Linq;
using DevExpress.Mvvm;

namespace OLS.Casy.Ui.MainControls.ViewModels
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IMainContainerViewModel))]
    public class MainControlsContainerViewModel : Base.ViewModelBase, IMainContainerViewModel, IPartImportsSatisfiedNotification
    {
        private readonly IEventAggregatorProvider _eventAggregatorProvider;

        private readonly IEnumerable<HostViewModel> _hostViewModels;

        private readonly MenuViewModel _menuViewModel;
        private readonly ITreeViewModel _treeViewModel;
        private readonly TopMenuViewModel _topMenuViewModel;

        private bool _isExpandViewCollapsed;
        private readonly double _maxExpandViewColumnWidth = 212;
        private GridLength _expandViewWidth;
        private bool _isTreeViewVisible;

        private ObservableCollection<Base.ViewModelBase> _overlayViewModels;
        private GridLength _restExpandViewWidth;

        [ImportingConstructor]
        public MainControlsContainerViewModel(
            MenuViewModel menuViewModel,
            TopMenuViewModel topMenuViewModel,
            [ImportMany] IEnumerable<HostViewModel> hostViewModels,
            IEventAggregatorProvider eventAggregatorProvider,
            ITreeViewModel treeViewModel
            )
        {
            this._eventAggregatorProvider = eventAggregatorProvider;
            this._hostViewModels = hostViewModels;

            this._menuViewModel = menuViewModel;
            this._topMenuViewModel = topMenuViewModel;
            this._treeViewModel = treeViewModel;

            this._expandViewWidth = new GridLength(_maxExpandViewColumnWidth);
            this._restExpandViewWidth = new GridLength(1, GridUnitType.Star);

            this._overlayViewModels = new ObservableCollection<Base.ViewModelBase>();
        }

        public MenuViewModel MenuViewModel
        {
            get { return _menuViewModel; }
        }

        public TopMenuViewModel TopMenuViewModel
        {
            get { return _topMenuViewModel; }
        }

        public ITreeViewModel TreeViewModel
        {
            get { return this._treeViewModel; }
        }

        public ObservableCollection<Base.ViewModelBase> OverlayViewModels
        {
            get { return _overlayViewModels; }
        }

        public ICommand ExpandButtonCommand
        {
            get
            {
                return new OmniDelegateCommand(DoExpand);
            }
        }

        public bool IsTreeViewVisible
        {
            get { return _isTreeViewVisible; }
            set
            {
                if(value != _isTreeViewVisible)
                {
                    this._isTreeViewVisible = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public GridLength ExpandViewWidth
        {
            get { return _expandViewWidth; }
            set
            {
                if (value != _expandViewWidth)
                {
                    _expandViewWidth = value;
                    NotifyOfPropertyChange();
                    NotifyOfPropertyChange("RestExpandViewWidth");
                }
            }
        }

        public GridLength RestExpandViewWidth
        {
            get { return _restExpandViewWidth; }
        }

        public bool IsExpandViewCollapsed
        {
            get { return _isExpandViewCollapsed; }
            set
            {
                if(value != _isExpandViewCollapsed)
                {
                    this._isExpandViewCollapsed = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public IEnumerable<HostViewModel> HostViewModels
        {
            get { return _hostViewModels; }
        }

        private void DoExpand()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsExpandViewCollapsed = !_isExpandViewCollapsed;

                if (_isExpandViewCollapsed)
                {
                    ExpandViewWidth = new GridLength(0);
                }
                else
                {
                    ExpandViewWidth = new GridLength(_maxExpandViewColumnWidth);
                }

                this._eventAggregatorProvider.Instance.GetEvent<ExpandEvent>().Publish();
            });
        }

        public void OnImportsSatisfied()
        {
            this._eventAggregatorProvider.Instance.GetEvent<NavigateToEvent>().Subscribe(OnNavigateToEvent);
            this._eventAggregatorProvider.Instance.GetEvent<AddMainControlsOverlayEvent>().Subscribe(OnAddMainControlsOverlay);
            this._eventAggregatorProvider.Instance.GetEvent<RemoveMainControlsOverlayEvent>().Subscribe(OnRemoveMainControlsOverlay);
        }

        private void OnRemoveMainControlsOverlay(object obj)
        {
            if(obj == null)
            {
                var toRemoves = this._overlayViewModels.ToList();
                foreach(var toRemove in toRemoves)
                {
                    this._overlayViewModels.Remove(toRemove);
                }
                return;
            }

            Base.ViewModelBase viewModelBase = obj as Base.ViewModelBase;
            if (viewModelBase != null)
            {
                this._overlayViewModels.Remove(viewModelBase);
            }
        }

        private void OnAddMainControlsOverlay(object obj)
        {
            Base.ViewModelBase viewModelBase = obj as Base.ViewModelBase;
            if(viewModelBase != null && !this._overlayViewModels.Contains(viewModelBase))
            {
                this._overlayViewModels.Add(viewModelBase);
            }
        }

        private void OnNavigateToEvent(object argument)
        {
            NavigationArgs navigationArgs = (NavigationArgs)argument;

            switch (navigationArgs.NavigationCategory)
            {
                case NavigationCategory.MeasureResults:
                    this.IsTreeViewVisible = !this.IsTreeViewVisible;
                    break;
            }
        }
    }
}
