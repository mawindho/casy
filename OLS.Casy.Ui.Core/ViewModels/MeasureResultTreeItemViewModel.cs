using OLS.Casy.Models;
using OLS.Casy.Ui.Api;
using OLS.Casy.Ui.Base;
using System.Windows.Input;

namespace OLS.Casy.Ui.Core.ViewModels
{
    public enum MeasureResultTreeItemType
    {
        None,
        Experiment,
        Group,
        MeasureResult,
        Selected
    }

    public class MeasureResultTreeItemViewModel : ViewModelBase, IDraggable, IDroppable
    {
        private string _buttonText;
        private readonly int _itemsCount;
        private readonly int? _itemsCount2;
        private readonly object _parentViewModel;
        private bool _isSelected;
        private bool _isVisible = true;
        private bool _isDragging;
        private double _dragPositionLeft;
        private double _dragPositionTop;
        private int _draggableOverLocation;

        public MeasureResultTreeItemViewModel(
            object parentViewModel,
            MeasureResultTreeItemType measureResultTreeItemType, 
            string buttonText,
            int itemsCount,
            object associatedObject,
            bool isVisible = true)
        {
            _parentViewModel = parentViewModel;
            _buttonText = buttonText;
            MeasureResultTreeItemType = measureResultTreeItemType;
            _itemsCount = itemsCount;
            AssociatedObject = associatedObject;
            _isVisible = isVisible;
        }

        public MeasureResultTreeItemViewModel(
            object parentViewModel,
            MeasureResultTreeItemType measureResultTreeItemType,
            string buttonText,
            int itemsCount,
            int itemsCount2,
            object associatedObject)
        {
            _parentViewModel = parentViewModel;
            _buttonText = buttonText;
            MeasureResultTreeItemType = measureResultTreeItemType;
            _itemsCount = itemsCount;
            _itemsCount2 = itemsCount2;
            AssociatedObject = associatedObject;
        }

        public string ButtonText
        {
            get => _buttonText;
            set
            {
                if (value == _buttonText) return;
                _buttonText = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsDeleted => AssociatedObject is MeasureResult measureResult && measureResult.IsDeletedResult;

        public int DisplayOrder => AssociatedObject is MeasureResult measureResult ? measureResult.DisplayOrder : 0;

        public string ItemsCount
        {
            get
            {
                var result = _itemsCount.ToString();
                if(_itemsCount2.HasValue)
                {
                    result += "/" + _itemsCount2.Value;
                }
                return result;
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value == _isSelected) return;
                _isSelected = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (value == _isVisible) return;
                _isVisible = value;
                NotifyOfPropertyChange();

                if (_parentViewModel is SelectedMeasureResultsTreeViewModel selectedMeasureResultsTreeViewModel)
                {
                    selectedMeasureResultsTreeViewModel.ToggleVisibility(AssociatedObject as MeasureResult);
                }
            }
        }

        public ICommand ToggleVisibilityCommand => new OmniDelegateCommand(OnToggleVisibility);

        public ICommand ButtonCommand => new OmniDelegateCommand(OnButtonPressed);

        public ICommand RemoveFromSelectionCommand => new OmniDelegateCommand(OnRemoveFromSelection);

        public MeasureResultTreeItemType MeasureResultTreeItemType { get; }

        public object AssociatedObject { get; }

        public string AssociatedExperiment { get; set; }

        public bool IsDragging
        {
            get => _isDragging;
            set
            {
                if (value == _isDragging) return;
                _isDragging = value;
                NotifyOfPropertyChange();
            }
        }
        public double DragPositionLeft
        {
            get => _dragPositionLeft;
            set
            {
                if (value == _dragPositionLeft) return;
                _dragPositionLeft = value;
                NotifyOfPropertyChange();
            }
        }
        public double DragPositionTop
        {
            get => _dragPositionTop;
            set
            {
                if (value == _dragPositionTop) return;
                _dragPositionTop = value;
                NotifyOfPropertyChange();
            }
        }

        public int DraggableOverLocation
        {
            get => _draggableOverLocation;
            set
            {
                if (value == _draggableOverLocation) return;
                _draggableOverLocation = value;
                NotifyOfPropertyChange();
            }
        }

        public void PerformDrop(IDraggable draggable)
        {
            if (!(_parentViewModel is SelectedMeasureResultsTreeViewModel viewModel)) return;

            if(draggable is MeasureResultTreeItemViewModel draggedModel)
            {
                viewModel.MeasureResultManager.MoveSelectedMeasureResult((MeasureResult) draggedModel.AssociatedObject, (MeasureResult) AssociatedObject, DraggableOverLocation);
            }

            viewModel.MeasureResultContainerViewSource.Refresh();
        }

        private void OnButtonPressed()
        {
            if (!(_parentViewModel is MeasureResultTreeViewModel measureResultTreeViewModel)) return;
            switch (MeasureResultTreeItemType)
            {
                case MeasureResultTreeItemType.Experiment:
                    measureResultTreeViewModel.SetSelectedExperiment(AssociatedObject as string);
                    break;
                case MeasureResultTreeItemType.Group:
                    measureResultTreeViewModel.SetSelectedGroup(AssociatedObject as string);
                    break;
                case MeasureResultTreeItemType.MeasureResult:
                    IsSelected = !IsSelected;
                    measureResultTreeViewModel.ToggleSelection(IsSelected, AssociatedObject as MeasureResult);
                    break;

            }
        }

        private void OnRemoveFromSelection()
        {
            if(_parentViewModel is SelectedMeasureResultsTreeViewModel selectedMeasureResultsTreeViewModel)
            {
                selectedMeasureResultsTreeViewModel.RemoveFromSelection(AssociatedObject as MeasureResult);
            }
        }
        
        private void OnToggleVisibility()
        {
            IsVisible = !IsVisible;
        }
    }
}
