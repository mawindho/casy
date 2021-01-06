using OLS.Casy.Ui.Base.Api;
using System;

namespace OLS.Casy.Ui.Base.ViewModels
{
    public class ComboBoxItemWrapperViewModel<T> : IComboBoxEditItem
    {
        private string _displayItem;
        private T _valueItem;
        private bool _isEnabled = true;
        private bool _isSelected;

        public ComboBoxItemWrapperViewModel(T item)
        {
            this._valueItem = item;
        }

        public string DisplayItem
        {
            get { return _displayItem; }
            set
            {
                if(value != _displayItem)
                {
                    this._displayItem = value;
                }
            }
        }

        public T ValueItem
        {
            get { return _valueItem; }
        }

        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                if (value != _isEnabled)
                {
                    this._isEnabled = value;
                }
            }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value != _isSelected)
                {
                    this._isSelected = value;
                    OnIsSelectedChanged?.Invoke();
                }
            }
        }

        public Action OnIsSelectedChanged { get; set; }
    }
}
