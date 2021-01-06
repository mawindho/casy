using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Base.ViewModels.Wizard;
using OLS.Casy.Ui.Core.Api;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Windows.Input;

namespace OLS.Casy.Ui.Core.ViewModels.Wizard
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(IWizardContainerViewModel))]
    public class WizardContainerViewModel : DialogModelBase, IWizardContainerViewModel, IPartImportsSatisfiedNotification
    {
        private readonly ILocalizationService _localizationService;

        private readonly List<IWizardStepViewModelBase> _wizardStepViewModels;
        private IWizardStepViewModelBase _activeWizardStepViewModel;

        private int _curStep = 1;
        private int _stepCount;

        private double _progressMaximum = 1.0;
        private double _progressMinimum;
        private double _progressValue;

        private bool _canNextButtonCommand = true;

        private bool _isPreviousButtonVisible;
        private bool _canPreviousButtonCommand;
        private string _nextButtonText;

        private bool _isCancelButtonVisible = true;
        private string _cancelButtonText;

        private string _wizardTitle;
        private bool _disposed;

        [ImportingConstructor]
        public WizardContainerViewModel(ILocalizationService localizationService)
        {
            _localizationService = localizationService;

            _wizardStepViewModels = new List<IWizardStepViewModelBase>();
        }

        public void AddWizardStepViewModel(IWizardStepViewModelBase wizardStepViewModel)
        {
            _wizardStepViewModels.Add(wizardStepViewModel);
            if(_wizardStepViewModels.Count == 1)
            {
                _activeWizardStepViewModel = wizardStepViewModel;
            }
            StepCount = _wizardStepViewModels.Count;
            UpdateButtons();
        }

        public IWizardStepViewModelBase ActiveWizardStepViewModel
        {
            get => _activeWizardStepViewModel;
            set
            {
                if(_activeWizardStepViewModel != null)
                {
                    _activeWizardStepViewModel.PropertyChanged -= OnActiveWizardStepViewModelPropertyChanged;
                }

                if (value == _activeWizardStepViewModel) return;
                _activeWizardStepViewModel = value;

                if(_activeWizardStepViewModel != null)
                {
                    _activeWizardStepViewModel.PropertyChanged += OnActiveWizardStepViewModelPropertyChanged;
                }

                NotifyOfPropertyChange();
            }
        }

        public ICommand NextButtonCommand => new OmniDelegateCommand(OnNextButtonPressed);

        protected virtual async void OnNextButtonPressed()
        {
            var success = await ActiveWizardStepViewModel.OnNextButtonPressed();
            if (!success) return;

            ActiveWizardStepViewModel.IsActive = false;

            if (_curStep < _wizardStepViewModels.Count)
            {
                ActiveWizardStepViewModel = _wizardStepViewModels[_curStep];
                ActiveWizardStepViewModel.IsActive = true;
                _curStep++;

                UpdateButtons();
            }
            else
            {
                IsCancel = false;
                CloseHandler.Invoke(this);
            }
        }

        public bool CanNextButtonCommand
        {
            get => _canNextButtonCommand;
            set
            {
                if (value == _canNextButtonCommand) return;
                _canNextButtonCommand = value;
                NotifyOfPropertyChange();
            }
        }

        public string NextButtonText
        {
            get => _nextButtonText;
            set
            {
                if (value == _nextButtonText) return;
                _nextButtonText = value;
                NotifyOfPropertyChange();
            }
        }

        protected override async void OnCancel()
        {
            var result = await ActiveWizardStepViewModel.OnCancelButtonPressed();
            if (!result)
            {
                base.OnCancel();
            }
            else
            {
                if (_curStep < _wizardStepViewModels.Count)
                {
                    ActiveWizardStepViewModel = _wizardStepViewModels[_curStep];
                    ActiveWizardStepViewModel.IsActive = true;
                    _curStep++;

                    UpdateButtons();
                }
                else
                {
                    base.OnCancel();
                }
            }
        }

        public ICommand PreviousButtonCommand => new OmniDelegateCommand(OnPreviousButtonPressed);

        protected virtual void OnPreviousButtonPressed()
        {
            _activeWizardStepViewModel.OnPreviousButtonPressed();
            ActiveWizardStepViewModel.IsActive = false;
            var index = _wizardStepViewModels.IndexOf(_activeWizardStepViewModel);
            ActiveWizardStepViewModel = _wizardStepViewModels[index - 1];

            ActiveWizardStepViewModel.IsActive = true;
            UpdateButtons();
        }

        public bool IsPreviousButtonVisible
        {
            get => _isPreviousButtonVisible;
            set
            {
                if (value == _isPreviousButtonVisible) return;
                _isPreviousButtonVisible = value;
                NotifyOfPropertyChange();
            }
        }

        public bool CanPreviousButtonCommand
        {
            get => _canPreviousButtonCommand;
            set
            {
                if (value == _canPreviousButtonCommand) return;
                _canPreviousButtonCommand = value;
                NotifyOfPropertyChange();
            }
        }

        public string CancelButtonText
        {
            get => _cancelButtonText;
            set
            {
                if (value == _cancelButtonText) return;
                _cancelButtonText = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsCancelButtonVisible
        {
            get => _isCancelButtonVisible;
            set
            {
                if (value == _isCancelButtonVisible) return;
                _isCancelButtonVisible = value;
                NotifyOfPropertyChange();
            }
        }

        public double ProgressMinimum
        {
            get => _progressMinimum;
            set
            {
                if (value == _progressMinimum) return;
                _progressMinimum = value;
                NotifyOfPropertyChange();
            }
        }

        public double ProgressMaximum
        {
            get => _progressMaximum;
            set
            {
                if (value == _progressMaximum) return;
                _progressMaximum = value;
                NotifyOfPropertyChange();
            }
        }

        public double ProgressValue
        {
            get => _progressValue;
            set
            {
                if (value == _progressValue) return;
                _progressValue = value;
                NotifyOfPropertyChange();
            }
        }

        public override string Title =>
            $"{_localizationService.GetLocalizedString(WizardTitle)} ({CurStep}/{StepCount})";

        public int CurStep
        {
            get => _curStep;
            set
            {
                if (value == _curStep) return;
                _curStep = value;
                NotifyOfPropertyChange();
            }
        }

        public int StepCount
        {
            get => _stepCount;
            set
            {
                if (value == _stepCount) return;
                _stepCount = value;
                NotifyOfPropertyChange();
            }
        }

        public string WizardTitle
        {
            get => _wizardTitle;
            set
            {
                if (value == _wizardTitle) return;
                _wizardTitle = value;
                NotifyOfPropertyChange();
            }
        }

        public void OnImportsSatisfied()
        {
            NextButtonText = _localizationService.GetLocalizedString("WizardContainerView_NextButton_Content");
            CancelButtonText = _localizationService.GetLocalizedString("WizardContainerView_CancelButton_Content");
        }

        private void UpdateButtons()
        {
            NextButtonText = string.IsNullOrEmpty(_activeWizardStepViewModel.NextButtonText) ? _localizationService.GetLocalizedString("WizardContainerView_NextButton_Content") : _activeWizardStepViewModel.NextButtonText;
            CanNextButtonCommand = _activeWizardStepViewModel.CanNextButtonCommand;
            CanPreviousButtonCommand = _curStep > 1;

            CancelButtonText = string.IsNullOrEmpty(_activeWizardStepViewModel.CancelButtonText) ? _localizationService.GetLocalizedString("WizardContainerView_CancelButton_Content") : _activeWizardStepViewModel.CancelButtonText;
            IsCancelButtonVisible = _activeWizardStepViewModel.IsCancelButtonVisible;

            ProgressValue = ProgressMaximum / _wizardStepViewModels.Count * _curStep;

            NotifyOfPropertyChange("Title");
        }

        private void OnActiveWizardStepViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case "CanNextButtonCommand":
                    CanNextButtonCommand = _activeWizardStepViewModel.CanNextButtonCommand;
                    break;
            }
        }

        public void GotoStep(int stepCount)
        {
            CurStep = stepCount;
            ActiveWizardStepViewModel.IsActive = false;
            ActiveWizardStepViewModel = _wizardStepViewModels[_curStep];
            ActiveWizardStepViewModel.IsActive = true;
            //_curStep++;
        }

        

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_wizardStepViewModels != null)
                    {
                        foreach (var viewModel in _wizardStepViewModels)
                        {
                            viewModel.Dispose();
                        }
                    }
                }
            }
            base.Dispose(disposing);
        }
    }
}
