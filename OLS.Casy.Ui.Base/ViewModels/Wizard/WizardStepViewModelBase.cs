using System.Threading.Tasks;
using OLS.Casy.Ui.Core.Api;

namespace OLS.Casy.Ui.Base.ViewModels.Wizard
{
    public abstract class WizardStepViewModelBase : ViewModelBase, IWizardStepViewModelBase
    {
        private bool _isActive;
        private bool _canNextButtonCommand = true;
        private bool _isCancelButtonVisible = true;

        public virtual void OnPreviousButtonPressed()
        {
        }

        public virtual async Task<bool> OnNextButtonPressed()
        {
            return await Task.Factory.StartNew(() => true);
        }

        public virtual async Task<bool> OnCancelButtonPressed()
        {
            return await Task.Factory.StartNew(() => false);
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (value == _isActive) return;
                _isActive = value;
                if(_isActive)
                {
                    OnIsActivated();
                }
                NotifyOfPropertyChange();
            }
        }

        public virtual void OnIsActivated()
        {

        }

        public virtual string NextButtonText { get; set; }

        public virtual string CancelButtonText { get; set; }

        public virtual bool CanNextButtonCommand
        {
            get => _canNextButtonCommand;
            set
            {
                if (value == _canNextButtonCommand) return;
                _canNextButtonCommand = value;
                NotifyOfPropertyChange();
            }
        }

        public virtual bool IsCancelButtonVisible
        {
            get => _isCancelButtonVisible;
            set
            {
                if (value == _isCancelButtonVisible) return;
                _isCancelButtonVisible = value;
                NotifyOfPropertyChange();
            }
        }
    }
}
