using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace OLS.Casy.Ui.Core.Api
{
    public interface IWizardStepViewModelBase : INotifyPropertyChanged, IDisposable
    {
        Task<bool> OnNextButtonPressed();
        Task<bool> OnCancelButtonPressed();
        void OnPreviousButtonPressed();
        bool IsActive { get; set; }
        string NextButtonText { get; set; }
        string CancelButtonText { get; set; }
        bool CanNextButtonCommand { get; }
        bool IsCancelButtonVisible { get; }
    }
}
