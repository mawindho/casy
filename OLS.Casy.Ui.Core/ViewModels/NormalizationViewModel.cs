using OLS.Casy.Ui.Base;
using System.ComponentModel.Composition;

namespace OLS.Casy.Ui.Core.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(NormalizationViewModel))]
    public class NormalizationViewModel : ViewModelBase
    {
        private int _minLimit;
        private int _maxLimit;
        private double _minLimitSmoothed;
        private double _maxLimitSmoothed;
        private bool _isVisible;

        public int MinLimit
        {
            get { return _minLimit; }
            set
            {
                if (value != _minLimit)
                {
                    _minLimit = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public double MinLimitSmoothed
        {
            get { return _minLimitSmoothed; }
            set
            {
                if (value != _minLimitSmoothed)
                {
                    _minLimitSmoothed = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public int MaxLimit
        {
            get { return _maxLimit; }
            set
            {
                if (value != _maxLimit)
                {
                    _maxLimit = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public double MaxLimitSmoothed
        {
            get { return _maxLimitSmoothed; }
            set
            {
                if (value != _maxLimitSmoothed)
                {
                    _maxLimitSmoothed = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                if (value != _isVisible)
                {
                    this._isVisible = value;
                    NotifyOfPropertyChange();
                }
            }
        }
    }
}
