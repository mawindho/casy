using DevExpress.Mvvm;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Localization.Api;
using System.ComponentModel.Composition;
using System.Windows.Input;

namespace OLS.Casy.Ui.Base.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(CasyMessageDialogViewModel))]
    public class CasyMessageDialogViewModel : DialogModelBase
    {
        private readonly ILocalizationService _localizationService;

        private string _message;
        private string _secondButtonText = "MessageBox_Button_Cancel_Text";
        private bool _canSecondCommand = true;
        private string _firstButtonText;
        private bool _canFirstCommand = true;
        private string _okButtonText = "MessageBox_Button_Ok_Text";
        private bool _isSecondButtonVisible;
        private bool _isFirstButtonVisible;

        private ButtonResult _dialogResult = ButtonResult.Cancel;

        [ImportingConstructor]
        public CasyMessageDialogViewModel(ILocalizationService localizationService)
        {
            this._localizationService = localizationService;
            OkButtonResult = ButtonResult.Ok;
            SecondButtonResult = ButtonResult.Cancel;
        }

        protected ILocalizationService LocalizationService
        {
            get { return _localizationService; }
        }

        public string Message
        {
            get { return _message; }
            set
            {
                if (value != _message)
                {
                    this._message = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public ButtonResult OkButtonResult { get; set; }

        public string OkButtonText
        {
            get { return string.IsNullOrEmpty(_okButtonText) ? string.Empty : _localizationService.GetLocalizedString(this._okButtonText); }
            set
            {
                if(value != _okButtonText)
                {
                    this._okButtonText = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        protected override void OnOk()
        {
            _dialogResult = OkButtonResult;
            base.OnOk();
        }

        public ButtonResult SecondButtonResult { get; set; }

        public string SecondButtonText
        {
            get { return string.IsNullOrEmpty(_secondButtonText) ? string.Empty : _localizationService.GetLocalizedString(this._secondButtonText); }
            set
            {
                if(value != this._secondButtonText)
                {
                    this._secondButtonText = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public ICommand SecondButtonCommand
        {
            get
            {
                return new OmniDelegateCommand(() =>
                {
                    _dialogResult = SecondButtonResult;
                    base.OnCancel();
                });
            }
        }

        public bool CanSecondCommand
        {
            get { return this._canSecondCommand; }
            set
            {
                if(value != this._canSecondCommand)
                {
                    this._canSecondCommand = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool IsSecondButtonVisibile
        {
            get { return this._isSecondButtonVisible; }
            set
            {
                if(value != this._isSecondButtonVisible)
                {
                    this._isSecondButtonVisible = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public ButtonResult FirstButtonResult { get; set; }

        public string FirstButtonText
        {
            get { return string.IsNullOrEmpty(_firstButtonText) ? string.Empty : _localizationService.GetLocalizedString(this._firstButtonText); }
            set
            {
                if (value != this._firstButtonText)
                {
                    this._firstButtonText = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public ICommand FirstButtonCommand
        {
            get {
                return new OmniDelegateCommand(() =>
                {
                    _dialogResult = FirstButtonResult;
                    base.OnCancel();
                });
            }
        }

        public bool CanFirstCommand
        {
            get { return this._canFirstCommand; }
            set
            {
                if (value != this._canFirstCommand)
                {
                    this._canFirstCommand = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool IsFirstButtonVisibile
        {
            get { return this._isFirstButtonVisible; }
            set
            {
                if (value != this._isFirstButtonVisible)
                {
                    this._isFirstButtonVisible = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public ButtonResult DialogResult
        {
            get { return _dialogResult; }
        }
    }
}
