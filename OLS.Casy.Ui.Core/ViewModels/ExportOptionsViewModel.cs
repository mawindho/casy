using DevExpress.Mvvm;
using OLS.Casy.Core.Config.Api;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Core.Api;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OLS.Casy.Ui.Core.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(ExportOptionsViewModel))]
    public class ExportOptionsViewModel : Base.ViewModelBase, IPartImportsSatisfiedNotification
    {
        private readonly Api.IFolderBrowserDialogService _folderBrowserDialogService;
        private readonly ILocalizationService _localizationService;
        private readonly IConfigService _configService;
        private readonly IDatabaseStorageService _databaseStorageService;

        private ExportFormat _exportFormat = ExportFormat.Csy;
        private string _exportPath;
        private FileCountOption _fileCountOption;
        private string _fileName;

        [ImportingConstructor]
        public ExportOptionsViewModel(Api.IFolderBrowserDialogService folderBrowserDialogService,
            ILocalizationService localizationService,
            IConfigService configService,
            IDatabaseStorageService databaseStorageService)
        {
            this._folderBrowserDialogService = folderBrowserDialogService;
            this._localizationService = localizationService;
            this._configService = configService;
            _databaseStorageService = databaseStorageService;
        }

        public ExportFormat ExportFormat
        {
            get { return _exportFormat; }
            set
            {
                if(value != _exportFormat)
                {
                    this._exportFormat = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string ExportPath
        {
            get { return _exportPath; }
            set
            {
                _exportPath = value;
                NotifyOfPropertyChange();
            }
        }

        [ConfigItem("")]
        public string DefaultBrowseLocation { get; set; }

        public FileCountOption FileCountOption
        {
            get { return _fileCountOption; }
            set
            {
                if(value != _fileCountOption)
                {
                    this._fileCountOption = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public ICommand SelectExportPathCommand
        {
            get { return new OmniDelegateCommand(OnSelectExportPath); }
        }

        public string FileName
        {
            get { return _fileName; }
            set
            {
                if(value != _fileName)
                {
                    this._fileName = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public void OnImportsSatisfied()
        {
            this._configService.InitializeByConfiguration(this);

            _databaseStorageService.GetSettings().TryGetValue("LastImportExportPath", out var importPath);

            if (importPath != null && !string.IsNullOrEmpty(importPath.Value))
            {
                this.ExportPath = importPath.Value;
            }
            else
            {
                this.ExportPath = this.DefaultBrowseLocation;
            }
        }

        private void OnSelectExportPath()
        {
            _folderBrowserDialogService.Description = _localizationService.GetLocalizedString("SettingsView_BackupSection_SelectPathDialog_Description");
            _folderBrowserDialogService.ShowNewFolderButton = true;
            _folderBrowserDialogService.SelectedPath = ExportPath;

            var result = _folderBrowserDialogService.ShowDialog();
            if (result.HasValue && result.Value)
            {
                this.ExportPath = _folderBrowserDialogService.SelectedPath;
                _databaseStorageService.SaveSetting("LastImportExportPath", ExportPath);
            }
        }
    }
}
