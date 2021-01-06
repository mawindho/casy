using OLS.Casy.Ui.Api;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Input;

namespace OLS.Casy.Ui.ViewModels
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(ContextMenuViewModel))]
    public class ContextMenuViewModel : ViewModelBase, IPartImportsSatisfiedNotification
    {
        private readonly ContextMenuService _contextMenuService;
        private bool _isContextMenuActive;

        private ReadOnlyCollection<IContextMenuItemViewModel> _activeContextMenuItems;
        private double _tooltipLocationX;
        private double _tooltipLocationY;

        private bool _isSubContextMenuActive;
        private ReadOnlyCollection<IContextMenuItemViewModel> _activeSubContextMenuItems;
        private double _subTooltipLocationX;
        private double _subTooltipLocationY;

        private Size _size;

        [ImportingConstructor]
        public ContextMenuViewModel(IContextMenuService contextMenuService)
        {
            _contextMenuService = contextMenuService as ContextMenuService;
        }

        public bool IsContextMenuActive
        {
            get { return _isContextMenuActive; }
            set
            {
                if (value != _isContextMenuActive)
                {
                    _isContextMenuActive = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        private void UpdateTooltipLocation(Point point)
        {
            TooltipLocationX = point.X;
            TooltipLocationY = point.Y;
        }

        public double TooltipLocationX
        {
            get { return _tooltipLocationX; }
            set
            {
                if (value != _tooltipLocationX)
                {
                    _tooltipLocationX = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public double TooltipLocationY
        {
            get { return _tooltipLocationY; }
            set
            {
                if (value != _tooltipLocationY)
                {
                    _tooltipLocationY = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public UiCommand<EventInformation<MouseEventArgs>> ContextMenuMouseLeaveCommand
        {
            get { return new UiCommand<EventInformation<MouseEventArgs>>(OnContextMenuMouseLeave); }
        }

        private void OnContextMenuMouseLeave(EventInformation<MouseEventArgs> mouseEventArgs)
        {
            if (!IsSubContextMenuActive)
            {
                // Kontextmenü wieder ausblenden
                ActiveContextMenuItems = null;
            }
        }

        public UiCommand<EventInformation<MouseEventArgs>> SubContextMenuMouseLeaveCommand
        {
            get { return new UiCommand<EventInformation<MouseEventArgs>>(OnSubContextMenuMouseLeave); }
        }

        private void OnSubContextMenuMouseLeave(EventInformation<MouseEventArgs> mouseEventArgs)
        {
            // Kontextmenü wieder ausblenden
            ActiveContextMenuItems = null;
            ActiveSubContextMenuItems = null;
        }

        public ReadOnlyCollection<IContextMenuItemViewModel> ActiveContextMenuItems
        {
            get
            {
                return _activeContextMenuItems;
            }
            set
            {
                _activeContextMenuItems = value;
                NotifyOfPropertyChange();
                IsContextMenuActive = _activeContextMenuItems != null && _activeContextMenuItems.Count > 0;
            }
        }

        public UiCommand<EventInformation<SizeChangedEventArgs>> ContextMenuSizeChangedCommand
        {
            get { return new UiCommand<EventInformation<SizeChangedEventArgs>>(OnContextMenuSizeChanged); }
        }

        private void OnContextMenuSizeChanged(EventInformation<SizeChangedEventArgs> eventArgs)
        {
            _size = eventArgs.EventArgs.NewSize;
        }

        public void OpenSubMenu(IContextMenuItemViewModel sender, object dataContext)
        {
            ContextMenuItemViewModel viewModel = sender as ContextMenuItemViewModel;

            if (viewModel.HasSubMenu)
            {
                double y = _tooltipLocationY + (viewModel.CurrentMouseLocation.Y - _tooltipLocationY) - 5;

                var subTooltipLocation = new Point(_tooltipLocationX + _size.Width, y);
                SubTooltipLocationX = subTooltipLocation.X;
                SubTooltipLocationY = subTooltipLocation.Y;

                foreach(var item in viewModel.SubContextMenuItems)
                {
                    ContextMenuItemViewModel subViewModel = item as ContextMenuItemViewModel;
                    if (subViewModel.IsContextMenuItemChecked != null)
                    {
                        subViewModel.CurrentState = subViewModel.IsContextMenuItemChecked(subViewModel, subViewModel.DataContext) ? ContextMenuItemState.Checked : ContextMenuItemState.Unchecked;
                    }
                }

                ActiveSubContextMenuItems = new ReadOnlyCollection<IContextMenuItemViewModel>(sender.SubContextMenuItems);
            }
        }

        public bool IsSubContextMenuActive
        {
            get { return _isSubContextMenuActive; }
            set
            {
                if (value != _isSubContextMenuActive)
                {
                    _isSubContextMenuActive = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public double SubTooltipLocationX
        {
            get { return _subTooltipLocationX; }
            set
            {
                if (value != _subTooltipLocationX)
                {
                    _subTooltipLocationX = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public double SubTooltipLocationY
        {
            get { return _subTooltipLocationY; }
            set
            {
                if (value != _subTooltipLocationY)
                {
                    _subTooltipLocationY = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public ReadOnlyCollection<IContextMenuItemViewModel> ActiveSubContextMenuItems
        {
            get
            {
                return _activeSubContextMenuItems;
            }
            set
            {
                _activeSubContextMenuItems = value;
                NotifyOfPropertyChange();

                IsSubContextMenuActive = ActiveSubContextMenuItems != null && ActiveSubContextMenuItems.Count > 0;
            }
        }

        public void OnImportsSatisfied()
        {
            _contextMenuService.OpenSubMenuRequest += OpenSubMenu;
            _contextMenuService.ActiveContextMenuItemsChanged += _contextMenuService_ActiveContextMenuItemsChanged;
            _contextMenuService.ActiveSubContextMenuItemsChanged += _contextMenuService_ActiveSubContextMenuItemsChanged;
            _contextMenuService.TooltipPositionChanged += _contextMenuService_TooltipPositionChanged;
        }

        void _contextMenuService_TooltipPositionChanged(object sender, EventArgs e)
        {
            var position = _contextMenuService.TooltipLocation;
            TooltipLocationX = position.X;
            TooltipLocationY = position.Y;
        }

        void _contextMenuService_ActiveSubContextMenuItemsChanged(object sender, EventArgs e)
        {
            if (ActiveSubContextMenuItems == null)
            {
                ActiveSubContextMenuItems = null;
            }
            else
            {
                ActiveSubContextMenuItems = new ReadOnlyCollection<IContextMenuItemViewModel>(_contextMenuService.ActiveSubContextMenuItems);
            }
        }

        void _contextMenuService_ActiveContextMenuItemsChanged(object sender, EventArgs e)
        {
            if (_contextMenuService.ActiveContextMenuItems == null)
            {
                ActiveContextMenuItems = null;
            }
            else
            {
                ActiveContextMenuItems = new ReadOnlyCollection<IContextMenuItemViewModel>(_contextMenuService.ActiveContextMenuItems);
            }
        }
    }
}
