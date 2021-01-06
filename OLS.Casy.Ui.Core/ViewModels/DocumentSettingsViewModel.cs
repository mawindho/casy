using System.Collections.Generic;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Models;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.MainControls.Api;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Windows.Input;
using System;
using System.Linq;
using OLS.Casy.Core.Config.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Ui.Core.Api;
using System.IO;
using MahApps.Metro.IconPacks;

namespace OLS.Casy.Ui.Core.ViewModels
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(ISettingsCategoryViewModel))]
    public class DocumentSettingsViewModel : ViewModelBase, ISettingsCategoryViewModel, IPartImportsSatisfiedNotification
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly ILocalizationService _localizationService;
        private readonly IOpenFileDialogService _openFileDialogService;
        private readonly IConfigService _configService;
        private readonly IFileSystemStorageService _fileSystemStorageService;
        private readonly IDatabaseStorageService _databaseStorageService;
        private readonly IDocumentSettingsManager _documentSettingsManager;

        private bool _isActive;
        private bool _isSelectedState;
        private string _documentLogoName;
        private bool _showLastWeeklyClean;

        [ImportingConstructor]
        public DocumentSettingsViewModel(
            IAuthenticationService authenticationService,
            ILocalizationService localizationService,
            IOpenFileDialogService openFileDialogService,
            IConfigService configService,
            IFileSystemStorageService fileSystemStorageService,
            IDatabaseStorageService databaseStorageService,
            IDocumentSettingsManager documentSettingsManager
            )
        {
            _authenticationService = authenticationService;
            _localizationService = localizationService;
            _openFileDialogService = openFileDialogService;
            _configService = configService;
            _fileSystemStorageService = fileSystemStorageService;
            _databaseStorageService = databaseStorageService;
            _documentSettingsManager = documentSettingsManager;
        }

        public string DocumentLogoName
        {
            get { return _documentLogoName; }
            set
            {
                if (value == _documentLogoName) return;
                _documentLogoName = value;
                NotifyOfPropertyChange();
            }
        }

        [ConfigItem("")]
        public string DefaultBrowseLocation { get; set; }

        public ICommand SelectDocumentLogoCommand => new OmniDelegateCommand(OnSelectDocumentLogo);

        private void OnSelectDocumentLogo()
        {
            _openFileDialogService.Title = _localizationService.GetLocalizedString("OpenDocumentLogoDialog_Title");
            _openFileDialogService.Filter = "Images | *.png; *.bmp; *.jpg; *.tiff";
            _openFileDialogService.InitialDirectory = DefaultBrowseLocation;
            _openFileDialogService.Multiselect = false;
            var result = _openFileDialogService.ShowDialog();
            if (result.HasValue && result.Value)
            {
                var fileInfo = new FileInfo(_openFileDialogService.FileNames.First());
                _fileSystemStorageService.CopyFileAsync(fileInfo.FullName,
                    @"Resources\" + fileInfo.Name);

                _databaseStorageService.SaveSetting("DocumentLogoName", fileInfo.Name);

                DocumentLogoName = fileInfo.Name;
            }
        }

        public bool ShowLastWeeklyClean
        {
            get { return _showLastWeeklyClean; }
            set
            {
                _showLastWeeklyClean = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsVisible { get; } = true;
        public UserRole MinRequiredRole => _authenticationService.GetRoleByName("Supervisor");
        public PackIconFontAwesomeKind Glyph => PackIconFontAwesomeKind.PrintSolid;
        public int Order => 1;
        public string Name => _localizationService.GetLocalizedString("DocumentSettingsViewModel_Title");
        public ICommand SelectCommand => new OmniDelegateCommand(OnSelected);
        private void OnSelected()
        {
            IsActive = true;
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (value == _isActive) return;
                _isActive = value;
                IsSelectedState = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsSelectedState
        {
            get => _isSelectedState;
            set
            {
                if (value == _isSelectedState) return;
                IsActive = value;
                _isSelectedState = value;
                NotifyOfPropertyChange();
            }
        }

        public ChevronState ChevronState
        {
            get => ChevronState.Up;
            set
            {
            }
        }

        public bool CanOk
        {
            get
            {
                var result = new List<ValidationResult>();
                return Validator.TryValidateObject(this, new ValidationContext(this, null, null), result);
            }
        }

        public void OnCancel()
        {
            DocumentLogoName = _documentSettingsManager.DocumentLogoName;
        }

        public void OnOk()
        {
            _documentSettingsManager.UpdateDocumentLogoName(DocumentLogoName);
            _documentSettingsManager.ShowLastWeeklyClean = this.ShowLastWeeklyClean;
        }

        public void OnImportsSatisfied()
        {
            _configService.InitializeByConfiguration(this);
            DocumentLogoName = _documentSettingsManager.DocumentLogoName;
            ShowLastWeeklyClean = _documentSettingsManager.ShowLastWeeklyClean;
        }
    }
}
