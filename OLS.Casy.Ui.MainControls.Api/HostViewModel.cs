using OLS.Casy.Ui.Base;

namespace OLS.Casy.Ui.MainControls.Api
{
    public abstract class HostViewModel : ViewModelBase
    {
        private bool _isActive;

        public virtual bool IsActive
        {
            get { return _isActive; }
            set
            {
                if(value != _isActive)
                {
                    this._isActive = value;
                    NotifyOfPropertyChange();
                }
            }
        }
    }
}
