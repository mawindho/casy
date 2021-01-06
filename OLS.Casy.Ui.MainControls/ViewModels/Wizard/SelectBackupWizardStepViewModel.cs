using DevExpress.Mvvm;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Ui.Base.ViewModels.Wizard;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using OLS.Casy.Ui.Base;

namespace OLS.Casy.Ui.MainControls.ViewModels.Wizard
{
    public class SelectBackupWizardStepViewModel : WizardStepViewModelBase
    {
        private readonly Core.Api.IOpenFileDialogService _openFileDialogService;
        private readonly ILocalizationService _localizationService;

        private string _header;
        private string _text;
        private string _restoreBackupPath;
        private string _defaultPath;

        public SelectBackupWizardStepViewModel(Core.Api.IOpenFileDialogService openFileDialogService,
            ILocalizationService localizationService,
            string defaultPath)
        {
            this._openFileDialogService = openFileDialogService;
            this._localizationService = localizationService;
            this._defaultPath = defaultPath;
        }

        public string Header
        {
            get { return _header; }
            set
            {
                if (value != _header)
                {
                    _header = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string Text
        {
            get { return _text; }
            set
            {
                if (value != _text)
                {
                    _text = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public ICommand SelectRestoreBackupPathCommand
        {
            get { return new OmniDelegateCommand(OnSelectRestoreBackupPath); }
        }

        public string RestoreBackupPath
        {
            get { return _restoreBackupPath; }
            set
            {
                _restoreBackupPath = value;
                NotifyOfPropertyChange();
            }
        }

        public Func<bool> NextButtonPressedAction { get; set; }

        public async override Task<bool> OnNextButtonPressed()
        {
            if (NextButtonPressedAction != null)
            {
                return await Task.Factory.StartNew(NextButtonPressedAction);
            }
            return await base.OnNextButtonPressed();
        }

        private void OnSelectRestoreBackupPath()
        {
            this._openFileDialogService.Title = _localizationService.GetLocalizedString("SettingsView_RestoreBackupSection_SelectPathDialog_Title");
            this._openFileDialogService.Filter = "CASY Backup file|*.*";
            this._openFileDialogService.InitialDirectory = this._defaultPath;
            this._openFileDialogService.Multiselect = false;
            var result = _openFileDialogService.ShowDialog();
            if (result.HasValue && result.Value)
            {
                this.RestoreBackupPath = _openFileDialogService.FileNames.FirstOrDefault();
            }
        }
    }
}
