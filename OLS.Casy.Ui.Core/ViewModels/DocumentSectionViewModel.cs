using System;
using OLS.Casy.Models;
using OLS.Casy.Ui.Api;
using OLS.Casy.Ui.Base;

namespace OLS.Casy.Ui.Core.ViewModels
{
    public class DocumentSectionViewModel : ViewModelBase, IDraggable, IDroppable
    {
        private double _dragPositionLeft;
        private double _dragPositionTop;
        private bool _isDragging;
        private int _draggableOverLocation;
        private readonly Action<IDraggable, IDroppable> _dragDropAction;
        private readonly DocumentSetting _documentSetting;

        public DocumentSectionViewModel(DocumentSetting documentSetting, Action<IDraggable, IDroppable> dragDropAction)
        {
            _documentSetting = documentSetting;
            _dragDropAction = dragDropAction;
        }

        public string Name
        {
            get => _documentSetting.Name;
            set
            {
                if (value == _documentSetting.Name) return;
                _documentSetting.Name = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsSelected
        {
            get => _documentSetting.IsSelected;
            set
            {
                if (value == _documentSetting.IsSelected) return;
                _documentSetting.IsSelected = value;
                NotifyOfPropertyChange();
            }
        }

        public int DisplayOrder
        {
            get => _documentSetting.DisplayOrder;
            set
            {
                if (value == _documentSetting.DisplayOrder) return;
                _documentSetting.DisplayOrder = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsViability
        {
            get => _documentSetting.IsViability;
            set
            {
                if (value == _documentSetting.IsViability) return;
                _documentSetting.IsViability = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsFreeRanges
        {
            get => _documentSetting.IsFreeRanges;
            set
            {
                if (value == _documentSetting.IsFreeRanges) return;
                _documentSetting.IsFreeRanges = value;
                NotifyOfPropertyChange();
            }
        }

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
            _dragDropAction?.Invoke(draggable, this);
        }
    }
}
