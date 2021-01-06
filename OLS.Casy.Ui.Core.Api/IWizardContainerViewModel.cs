namespace OLS.Casy.Ui.Core.Api
{
    public interface IWizardContainerViewModel
    {
        void AddWizardStepViewModel(IWizardStepViewModelBase wizardStepViewModel);
        string WizardTitle { get; set; }
        int StepCount { get; }
        int CurStep { get; }
        void GotoStep(int stepCount);
        bool IsCancel { get; }
    }
}
