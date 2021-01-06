using System;
using System.Threading.Tasks;

namespace OLS.Casy.Ui.Base.ViewModels.Wizard
{
    public class StandardWizardStepViewModel : WizardStepViewModelBase
    {
        private string _imagePath;
        private string _primaryHeader;
        private string _primaryText;
        private string _secondaryHeader;
        private string _secondaryText;
        private string _thirdHeader;
        private string _thirdText;
        private string _nextButtonText = null;
        private bool _isDoNotShowAgainVisible;
        private bool _doNotShowAgain;

        public string ImagePath
        {
            get { return _imagePath; }
            set
            {
                if(value != _imagePath)
                {
                    _imagePath = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string PrimaryHeader
        {
            get { return _primaryHeader; }
            set
            {
                if (value != _primaryHeader)
                {
                    _primaryHeader = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string PrimaryText
        {
            get { return _primaryText; }
            set
            {
                if (value != _primaryText)
                {
                    _primaryText = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string SecondaryHeader
        {
            get { return _secondaryHeader; }
            set
            {
                if (value != _secondaryHeader)
                {
                    _secondaryHeader = value;
                    NotifyOfPropertyChange();
                }
            }
        }
        public string SecondaryText
        {
            get { return _secondaryText; }
            set
            {
                if (value != _secondaryText)
                {
                    _secondaryText = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string ThirdHeader
        {
            get { return _thirdHeader; }
            set
            {
                if (value != _thirdHeader)
                {
                    _thirdHeader = value;
                    NotifyOfPropertyChange();
                }
            }
        }
        public string ThirdText
        {
            get { return _thirdText; }
            set
            {
                if (value != _thirdText)
                {
                    _thirdText = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public Func<bool> NextButtonPressedAction { get; set; }

        public async override Task<bool> OnNextButtonPressed()
        {
            if(NextButtonPressedAction != null)
            {
                return await Task.Factory.StartNew(NextButtonPressedAction);
            }
            return await base.OnNextButtonPressed();
        }

        public override string NextButtonText
        {
            get { return _nextButtonText; }
            set
            {
                if(value != _nextButtonText)
                {
                    this._nextButtonText = value;
                    NotifyOfPropertyChange();
                }
            }

        }

        public bool IsDoNotShowAgainVisible
        {
            get { return _isDoNotShowAgainVisible; }
            set
            {
                if(value != _isDoNotShowAgainVisible)
                {
                    _isDoNotShowAgainVisible = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool DoNotShowAgain
        {
            get { return _doNotShowAgain; }
            set
            {
                if (value != _doNotShowAgain)
                {
                    _doNotShowAgain = value;
                    NotifyOfPropertyChange();
                }
            }
        }
    }
}
