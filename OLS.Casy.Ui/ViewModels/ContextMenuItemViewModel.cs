using OLS.Casy.Ui.Api;
using OLS.Casy.Ui.Base;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace OLS.Casy.Ui.ViewModels
{
    public class ContextMenuItemViewModel : ViewModelBase, IContextMenuItemViewModel
    {
        private ContextMenuItemState _currentState;
        private string _contextMenuItemText;
        private readonly Action<IContextMenuItemViewModel, object> _onContextMenuItemPressed;
        private readonly Func<object, IList<IContextMenuItemViewModel>> _populateSubMenu;
        private IList<IContextMenuItemViewModel> _subContextMenuItems;
        private int _displayOrder = 0;
        private Func<IContextMenuItemViewModel, object, bool> _isContextMenuItemChecked;

        public ContextMenuItemViewModel(string contextMenuItemText, Action<IContextMenuItemViewModel, object> onContextMenuItemPressed, Type[] activeForDataContextTypes, Func<object, IList<IContextMenuItemViewModel>> populateSubMenu, int displayOrder)
            : this(contextMenuItemText, onContextMenuItemPressed, activeForDataContextTypes, displayOrder)
        {
            _populateSubMenu = populateSubMenu;
        }

        public ContextMenuItemViewModel(string contextMenuItemText, Action<IContextMenuItemViewModel, object> onContextMenuItemPressed, Type[] activeForDataContextTypes, int displayOrder)
        {
            _contextMenuItemText = contextMenuItemText;
            ActiveForDataContextTypes = activeForDataContextTypes;
            _onContextMenuItemPressed = onContextMenuItemPressed;
            _displayOrder = displayOrder;
        }

        public IContextMenuItemViewModel ParentViewModel { get; set; }

        public IEnumerable<string> AvoidEntryForElements { get; set; }

        public IEnumerable<string> EntryForElementsOnly { get; set; }

        public IEnumerable<Type> ActiveForDataContextTypes { get; set; }

        public bool HasSubMenu { get; set; }

        public Func<object, IEnumerable<IContextMenuItemViewModel>> PopulateSubMenu
        {
            get { return _populateSubMenu; }
        }

        public int DisplayOrder
        {
            get
            {
                return _displayOrder;
            }
            set
            {
                if (value != _displayOrder)
                {
                    _displayOrder = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string ContextMenuItemText
        {
            get { return _contextMenuItemText; }
            set
            {
                if (value != _contextMenuItemText)
                {
                    _contextMenuItemText = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public Func<IContextMenuItemViewModel, object, bool> IsContextMenuItemChecked
        {
            get { return _isContextMenuItemChecked; }
            set
            {
                this._isContextMenuItemChecked = value;

                if(this._isContextMenuItemChecked != null)
                {
                    CurrentState = _isContextMenuItemChecked(this, DataContext) ? ContextMenuItemState.Checked : ContextMenuItemState.Unchecked;
                }
            }
        }

        public ContextMenuItemState CurrentState
        {
            get
            {
                // Wenn das Delegate gesetzt ist, wird dieses ausgeführt um den Status zu überprüfen.
                // Ansonsten wird der manuell gesetzte Status zurückgegeben
                //if (IsContextMenuItemChecked != null)
                //{
                //    return IsContextMenuItemChecked(DataContext) ? ContextMenuItemState.Checked : ContextMenuItemState.Unchecked;
               // }
                return _currentState;
            }
            set
            {
                if (value != _currentState)
                {
                    _currentState = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public UiCommand ContextMenuItemCommand
        {
            get { return new UiCommand(OnContextMenuItemExecuted); }
        }

        private void OnContextMenuItemExecuted(object input)
        {
            if (_populateSubMenu != null)
            {
                SubContextMenuItems = _populateSubMenu(DataContext);
            }

            _onContextMenuItemPressed(this, DataContext);

            if(ParentViewModel != null)
            {
                foreach(var viewModel in ParentViewModel.SubContextMenuItems)
                {
                    ContextMenuItemViewModel contextItemViewModel = viewModel as ContextMenuItemViewModel;
                    contextItemViewModel.CurrentState = contextItemViewModel.IsContextMenuItemChecked(contextItemViewModel, contextItemViewModel.DataContext) ? ContextMenuItemState.Checked : ContextMenuItemState.Unchecked;
                }
            }
            else if (IsContextMenuItemChecked != null)
            {
                CurrentState = IsContextMenuItemChecked(this, DataContext) ? ContextMenuItemState.Checked : ContextMenuItemState.Unchecked;
            }
        }

        public object DataContext { get; set; }

        public Point CurrentMouseLocation { get; set; }

        public UiCommand<EventInformation<MouseEventArgs>> MouseEnterCommand
        {
            get { return new UiCommand<EventInformation<MouseEventArgs>>(OnMouseEntered); }
        }

        private void OnMouseEntered(EventInformation<MouseEventArgs> information)
        {
            CurrentMouseLocation = information.EventArgs.GetPosition(null);
        }

        public IList<IContextMenuItemViewModel> SubContextMenuItems
        {
            get { return _subContextMenuItems; }
            set
            {
                if (value != _subContextMenuItems)
                {
                    _subContextMenuItems = value;
                    NotifyOfPropertyChange();
                    HasSubMenu = _subContextMenuItems != null && _subContextMenuItems.Count > 0;
                }
            }
        }
    }
}
