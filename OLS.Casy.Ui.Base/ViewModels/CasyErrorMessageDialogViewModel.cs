using DevExpress.Mvvm;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Models;
using OLS.Casy.Ui.Base;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Input;

namespace OLS.Casy.Ui.Base.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(CasyErrorMessageDialogViewModel))]
    public class CasyErrorMessageDialogViewModel : DialogModelBase
    {
        private readonly ILocalizationService _localizationService;

        private IEnumerable<ErrorDetails> _errorDetails;

        private int _currentIndex;
        private string _errorMessage;
        private string _errorTitle;

        [ImportingConstructor]
        public CasyErrorMessageDialogViewModel(ILocalizationService localizationService)
        {
            _errorDetails = new List<ErrorDetails>();
            _localizationService = localizationService;
        }

        public void SetErrorDetails(IEnumerable<ErrorDetails> errorDetails)
        {
            this._currentIndex = 0;
            this._errorDetails = errorDetails;
            NotifyOfPropertyChange("ErrorDetailsCount");

            Update();
        }

        public string ErrorMessage
        {
            get { return _errorMessage; }
            set
            {
                if(value != _errorMessage)
                {
                    _errorMessage = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string ErrorTitle
        {
            get { return _errorTitle; }
            set
            {
                if (value != _errorTitle)
                {
                    _errorTitle = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public int ErrorDetailsCount
        {
            get { return _errorDetails.Count(); }
        }

        public ICommand NavigateLeftCommand
        {
            get { return new OmniDelegateCommand(OnNavigateLeft); }
        }

        public ICommand NavigateRightCommand
        {
            get { return new OmniDelegateCommand(OnNavigateRight); }
        }

        private void OnNavigateLeft()
        {
            if(--_currentIndex < 0)
            {
                _currentIndex = _errorDetails.Count() - 1;
            }
            Update(); 
        }

        private void OnNavigateRight()
        {
            if (++_currentIndex >= _errorDetails.Count())
            {
                _currentIndex = 0;
            }
            Update();
        }

        private void Update()
        {
            var errorDetail = _errorDetails.ElementAt(_currentIndex);

            if (!string.IsNullOrEmpty(errorDetail.Description))
            {
                object localizedTitle;
                this._localizationService.TryGetLocalizedObject(errorDetail.Description, out localizedTitle);
                ErrorTitle = localizedTitle as string;
            }
            else
            {
                ErrorTitle = string.Format("{0}: {1}", _localizationService.GetLocalizedString("ErrorMessageDialogView_ErrorCode_Label"), errorDetail.ErrorCode);
            }

            string notice = string.Empty;
            if(!string.IsNullOrEmpty(errorDetail.DeviceErrorName))
            {
                notice += string.Format("{0}: {1}\n", _localizationService.GetLocalizedString("ErrorMessageDialogView_ErrorNumber_Label"), errorDetail.DeviceErrorName);
            }

            if (!string.IsNullOrEmpty(errorDetail.Notice))
            {
                notice += this._localizationService.GetLocalizedString(errorDetail.Notice);
            }

            if(!string.IsNullOrEmpty(errorDetail.ErrorCode))
            {
                notice += string.Format("\n\n{0}: {1}", _localizationService.GetLocalizedString("ErrorMessageDialogView_ErrorCode_Label"), errorDetail.ErrorCode);
            }
            ErrorMessage = notice;
        }
    }
}
