using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Core.Api;
using System;
using System.ComponentModel;
using System.Windows.Input;

namespace OLS.Casy.Ui.Core.ViewModels
{
    public class RangeMinModificationHandleViewModel : RangeModificationHandleViewModel, IDisposable
    {
        public RangeMinModificationHandleViewModel(ChartCursorViewModel parentViewModel) : base(parentViewModel)
        {
            parentViewModel.PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == "Name")
            {
                NotifyOfPropertyChange("ParentName");
            }
        }

        public string ParentName
        {
            get { return this.Parent == null ? string.Empty : this.Parent.LocalizationService.GetLocalizedString(this.Parent.Name); }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (this.Parent != null)
                    {
                        this.Parent.PropertyChanged -= OnPropertyChanged;
                    }
                    //if(_minTimer != null)
                    //{
                    //this._minTimer.Dispose();
                    //}
                }

                disposedValue = true;
            }
            base.Dispose(disposing);
        }
        #endregion
    }

    public class RangeMaxModificationHandleViewModel : RangeModificationHandleViewModel, IDisposable
    {
        public RangeMaxModificationHandleViewModel(ChartCursorViewModel parentViewModel) : base(parentViewModel)
        {
        }
    }

    public class RangeBiModificationHandleViewModel : RangeModificationHandleViewModel, IDisposable
    {
        private ChartCursorViewModel _maxParentViewModel;
        private bool _isMinVisible = true;
        private bool _isMaxVisible = true;

        public RangeBiModificationHandleViewModel(ChartCursorViewModel parentViewModel) 
            : this(parentViewModel, null)
        {
            
        }

        public RangeBiModificationHandleViewModel(ChartCursorViewModel maxParentViewModel, ChartCursorViewModel minParentViewModel) : base(minParentViewModel)
        {
            _maxParentViewModel = maxParentViewModel;
            minParentViewModel.PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Name")
            {
                NotifyOfPropertyChange("MinParentName");
            }
        }

        public ChartCursorViewModel MinParent
        {
            get { return this.Parent; }
        }

        public string MinParentName
        {
            get { return this.Parent == null ? string.Empty : this.Parent.LocalizationService.GetLocalizedString(this.Parent.Name); }
        }

        public ChartCursorViewModel MaxParent
        {
            get { return _maxParentViewModel; }
        }

        public bool IsMinVisible
        {
            get { return this._isMinVisible; }
            set
            {
                if (value != _isMinVisible)
                {
                    this._isMinVisible = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool IsMaxVisible
        {
            get { return this._isMaxVisible; }
            set
            {
                if (value != _isMaxVisible)
                {
                    this._isMaxVisible = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (this.Parent != null)
                    {
                        this.Parent.PropertyChanged -= OnPropertyChanged;
                    }
                    _maxParentViewModel = null;
                    //if(_minTimer != null)
                    //{
                    //this._minTimer.Dispose();
                    //}
                }

                disposedValue = true;
            }
            base.Dispose(disposing);
        }
        #endregion
    }

    public abstract class RangeModificationHandleViewModel : ViewModelBase, IChartOverlayViewModel
    {
        private double _positionLeft;
        private double _positionTop;
        private double _width;
        private double _height;
        private ChartCursorViewModel _parentViewModel;
        private bool _isVisible = true;
        private ICommand _rangeDoubleClickCommand;

        public RangeModificationHandleViewModel(ChartCursorViewModel parentViewModel)
        {
            this._parentViewModel = parentViewModel;
        }

        public Action<double> OnWidthChanged { get; set; }
        public Func<double, bool?, bool> IsValidHorizontalChange { get; set; }

        public double PositionLeft
        {
            get { return _positionLeft; }
            set
            {
                if(value != _positionLeft)
                {
                    this._positionLeft = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public double PositionTop
        {
            get { return _positionTop; }
            set
            {
                if (value != _positionTop)
                {
                    this._positionTop = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public double Width
        {
            get { return _width < 1d ? 1d : _width; }
            set
            {
                if (value != _width)
                {
                    this._width = value;
                    NotifyOfPropertyChange();
                    OnWidthChanged?.Invoke(value);
                }
            }
        }

        public double Height
        {
            get { return _height; }
            set
            {
                if (value != _height)
                {
                    this._height = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool IsVisible
        {
            get { return this._isVisible; }
            set
            {
                if (value != _isVisible)
                {
                    this._isVisible = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public ChartCursorViewModel Parent
        {
            get { return _parentViewModel; }
        }

        public ICommand RangeDoubleClickCommand
        {
            get { return _rangeDoubleClickCommand; }
            set
            {
                if (value != _rangeDoubleClickCommand)
                {
                    this._rangeDoubleClickCommand = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool CanModifyRange
        {
            get { return _parentViewModel == null ? false : _parentViewModel.CanModifyRange; }
        }

        private bool disposedValue = false; // To detect redundant calls

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this._parentViewModel = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
