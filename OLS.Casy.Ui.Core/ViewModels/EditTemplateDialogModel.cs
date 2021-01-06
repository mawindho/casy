using OLS.Casy.Controller.Api;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Models;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Core.Api;
using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OLS.Casy.Ui.Core.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(EditTemplateDialogModel))]
    public class EditTemplateDialogModel : DialogModelBase, IPartImportsSatisfiedNotification
    {
        private readonly ILocalizationService _localizationService;
        private readonly IMeasureController _measureController;
        private readonly IBinaryImportExportProvider _binaryImportExportProvider;
        private readonly IOpenFileDialogService _openFileDialogService;
        private readonly ISaveFileDialogService _saveFileDialogService;
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly IDatabaseStorageService _databaseStorageService;
        private readonly IAuthenticationService _authenticationService;
        private readonly ICalibrationController _calibrationController;
        private readonly IActivationService _activationService;
        private readonly IAuditTrailViewModel _auditTrailViewModel;
        private MeasureSetup _template;

        private bool _isEditTemplateMode;

        [ImportingConstructor]
        public EditTemplateDialogModel(ILocalizationService localizationService,
            IMeasureController measureController,
            IBinaryImportExportProvider binaryImportExportProvider,
            IOpenFileDialogService openFileDialogService,
            ISaveFileDialogService saveFileDialogService,
            IEventAggregatorProvider eventAggregatorProvider,
            IDatabaseStorageService databaseStorageService,
            ISelectTemplateViewModel selectTemplateViewModel,
            EditTemplateViewModel editTemplateViewModel,
            IAuthenticationService authenticationService,
            IActivationService activationService,
            [Import(AllowDefault = true)] ICalibrationController calibrationController,
            [Import(AllowDefault = true)] IAuditTrailViewModel auditTrailViewModel)
        {
            _localizationService = localizationService;
            SelectTemplateViewModel = selectTemplateViewModel;
            _measureController = measureController;
            _binaryImportExportProvider = binaryImportExportProvider;
            _openFileDialogService = openFileDialogService;
            _saveFileDialogService = saveFileDialogService;
            _eventAggregatorProvider = eventAggregatorProvider;
            _databaseStorageService = databaseStorageService;
            EditTemplateViewModel = editTemplateViewModel;
            _authenticationService = authenticationService;
            _calibrationController = calibrationController;
            _activationService = activationService;
            _auditTrailViewModel = auditTrailViewModel;
        }

        public bool CanSave => EditTemplateViewModel.CanSave();

        public ISelectTemplateViewModel SelectTemplateViewModel { get; }

        public EditTemplateViewModel EditTemplateViewModel { get; }

        public ICommand ImportCommand => new OmniDelegateCommand(OnImport);

        public ICommand ExportCommand => new OmniDelegateCommand(OnExport);

        public ICommand SaveCommand => new OmniDelegateCommand(OnSave);

        public ICommand SaveAsCommand => new OmniDelegateCommand(OnSaveAs);
        public ICommand AuditTrailCommand => new OmniDelegateCommand(OnAuditTrail);

        public bool CanExport => _template != null && _authenticationService.LoggedInUser.UserRole.Priority > 1;
        public bool IsSaveVisible => _isEditTemplateMode && _authenticationService.LoggedInUser.UserRole.Priority > 2;
        public bool IsAuditTrailVisible => _isEditTemplateMode && _activationService.IsModuleEnabled("cfr");

        public bool IsEditTemplateMode
        {
            get => _isEditTemplateMode;
            set
            {
                if (value == _isEditTemplateMode) return;
                _isEditTemplateMode = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange("IsSaveVisible");
                NotifyOfPropertyChange("IsAuditTrailVisible");

                UpdateTitle();
            }
        }

        public ICommand EditTemplateCommand => new OmniDelegateCommand<MeasureSetup>(OnEditTemplate);

        public ICommand DeleteTemplateCommand
        {
            get { return new OmniDelegateCommand<MeasureSetup>(OnDeleteTemplate); }
        }

        public void OnImportsSatisfied()
        {
            this.SelectTemplateViewModel.ShowSettings = true;
            this._measureController.SelectedTemplateChangedEvent += OnSelectedTemplateChanged;
            this.Title = this._localizationService.GetLocalizedString("EditTemplateDialog_Header");

            this.SelectTemplateViewModel.ShowSettings = true;
            this.SelectTemplateViewModel.EditTemplateCommand = this.EditTemplateCommand;
            this.SelectTemplateViewModel.DeleteTemplateCommand = this.DeleteTemplateCommand;

            OnSelectedTemplateChanged(null, null);
            UpdateTitle();
        }

        protected override void OnCancel()
        {
            this.EditTemplateViewModel.UndoChanges();
            base.OnCancel();
        }

        private void UpdateTitle()
        {
            if (this.IsEditTemplateMode)
            {
                this.Title = this._localizationService.GetLocalizedString("EditTemplateDialog_Header");
            }
            else
            {
                this.Title = this._localizationService.GetLocalizedString("SelectTemplateDialog_Header");
            }
        }

        private void OnSelectedTemplateChanged(object sender, SelectedTemplateChangedEventArgs e)
        {
            this._template = _measureController.SelectedTemplate;
            NotifyOfPropertyChange("CanExport");
        }

        private void OnTemplateChanged(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == "Name")
            {
                NotifyOfPropertyChange("CanSave");
            }
        }

        private async void OnImport()
        {
            this._openFileDialogService.Title = _localizationService.GetLocalizedString("LoadTemplateFileDialog_Title");

            _databaseStorageService.GetSettings().TryGetValue("LastImportExportPath", out var importPath);

            if (importPath != null && !string.IsNullOrEmpty(importPath.Value))
            {
                _openFileDialogService.InitialDirectory = importPath.Value;
            }

            this._openFileDialogService.Filter = "CASY Template|*.tcsy";
            var result = _openFileDialogService.ShowDialog();
            if (result.HasValue && result.Value)
            {
                FileInfo fileInfo = new FileInfo(_openFileDialogService.FileNames[0]);

                _databaseStorageService.SaveSetting("LastImportExportPath", fileInfo.DirectoryName);

                var measureSetup = await _binaryImportExportProvider.ImportMeasureSetupAsync(fileInfo.FullName);

                if(this._calibrationController != null)
                {
                    if(!this._calibrationController.IsValidCalibratrion(measureSetup.CapillarySize, measureSetup.ToDiameter))
                    {
                        await Task.Factory.StartNew(() =>
                        {
                            var awaiter = new System.Threading.ManualResetEvent(false);

                            ShowMessageBoxDialogWrapper messageBoxWrapper = new ShowMessageBoxDialogWrapper()
                            {
                                Awaiter = awaiter,
                                Title = "ImportTemplateFailed_Title",
                                Message = "ImportTemplateFailed_Content",
                                MessageParameter = new[] { measureSetup.CapillarySize.ToString(), measureSetup.ToDiameter.ToString() }
                            };

                            _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>().Publish(messageBoxWrapper);

                            if (awaiter.WaitOne() && messageBoxWrapper.Result)
                            {
                            }
                        });
                        return;
                    }
                }
                measureSetup.MeasureSetupId = -1;
                foreach(var cursor in measureSetup.Cursors)
                {
                    cursor.CursorId = -1;
                }
                foreach(var devi in measureSetup.DeviationControlItems)
                {
                    devi.DeviationControlItemId = -1;
                }

                await Task.Factory.StartNew(() =>
                {
                    var awaiter = new System.Threading.ManualResetEvent(false);

                    ShowInputDialogWrapper showInputWrapper = new ShowInputDialogWrapper()
                    {
                        Awaiter = awaiter,
                        Message = "SaveAsTemplateDialog_Content",
                        Title = "SaveAsTemplateDialog_Title",
                        DefaultText = Path.GetFileNameWithoutExtension(_openFileDialogService.FileNames[0]),
                        Watermark = "SaveAsTemplateDialog_Watermark",
                        CanOkDelegate = (input) =>
                        {
                            return !string.IsNullOrEmpty(input) && input.IndexOfAny(new[] { '/', '\\', ':', '*', '<', '>', '|' }) == -1;
                        }
                    };

                    _eventAggregatorProvider.Instance.GetEvent<ShowInputEvent>().Publish(showInputWrapper);

                    if (awaiter.WaitOne() && !showInputWrapper.IsCancel)
                    {
                        if (!showInputWrapper.IsCancel && !string.IsNullOrEmpty(showInputWrapper.Result))
                        {
                            measureSetup.Name = showInputWrapper.Result;
                            _databaseStorageService.SaveMeasureSetup(measureSetup);
                        }
                    }
                });

                _eventAggregatorProvider.Instance.GetEvent<TemplateSavedEvent>().Publish(null);
            }
        }

        private void OnExport()
        {
            if (this._template != null)
            {
                this._saveFileDialogService.Title = _localizationService.GetLocalizedString("SaveSetupFileDialog_Title");

                _databaseStorageService.GetSettings().TryGetValue("LastImportExportPath", out var importPath);

                if (importPath != null && !string.IsNullOrEmpty(importPath.Value))
                {
                    _saveFileDialogService.InitialDirectory = importPath.Value;
                }

                this._saveFileDialogService.FileName = this._template.Name;
                this._saveFileDialogService.Filter = "CASY Template|*.tcsy";
                var result = _saveFileDialogService.ShowDialog();
                if (result.HasValue && result.Value)
                {
                    FileInfo fileInfo = new FileInfo(this._saveFileDialogService.FileName);

                    _binaryImportExportProvider.ExportMeasureSetupAsync(_template, fileInfo.FullName);

                    _databaseStorageService.SaveSetting("LastImportExportPath", fileInfo.DirectoryName);
                }
            }
        }

        private void OnAuditTrail()
        {
            Task.Factory.StartNew(async () =>
            {
                //var result = await _measureResultManager.SaveChangedMeasureResults();

                //if (result != ButtonResult.Cancel)
                //{
                var awaiter = new ManualResetEvent(false);

                var wrapper = new ShowCustomDialogWrapper()
                {
                    Awaiter = awaiter,
                    DataContext = _auditTrailViewModel,
                    DialogType = typeof(IAuditTrailView)
                };

                _auditTrailViewModel.LoadAuditTrailEntries(EditTemplateViewModel.Template);

                _eventAggregatorProvider.Instance.GetEvent<ShowCustomDialogEvent>().Publish(wrapper);

                awaiter.WaitOne();
                //}
            });
        }

        private void OnEditTemplate(MeasureSetup template)
        {
            if(this.EditTemplateViewModel.Template != null)
            {
                this.EditTemplateViewModel.Template.PropertyChanged -= OnTemplateChanged;
            }

            this.EditTemplateViewModel.Template = template;
            this.IsEditTemplateMode = !this.IsEditTemplateMode;

            if (this.EditTemplateViewModel.Template != null)
            {
                this.EditTemplateViewModel.Template.PropertyChanged += OnTemplateChanged;
            }

            NotifyOfPropertyChange("CanSave");
        }

        private void OnDeleteTemplate(MeasureSetup template)
        {
            Task.Factory.StartNew(() =>
            {
                var awaiter = new System.Threading.ManualResetEvent(false);

                ShowMessageBoxDialogWrapper messageBoxWrapper = new ShowMessageBoxDialogWrapper()
                {
                    Awaiter = awaiter,
                    Title = "DeleteTemplateDialog_Title",
                    Message = "DeleteTemplateDialog_Content",
                    MessageParameter = new[] { template.Name }
                };

                _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>().Publish(messageBoxWrapper);

                if (awaiter.WaitOne() && messageBoxWrapper.Result)
                {
                    if(this._measureController.SelectedTemplate != null && this._measureController.SelectedTemplate.MeasureSetupId == template.MeasureSetupId)
                    {
                        this._measureController.SelectedTemplate = null;
                    }

                    _databaseStorageService.DeleteMeasureSetup(template);
                    this.SelectTemplateViewModel.LoadTemplates();

                    if(this._authenticationService.LoggedInUser.FavoriteTemplateIds.Contains(template.MeasureSetupId))
                    {
                        this._authenticationService.LoggedInUser.FavoriteTemplateIds.Remove(template.MeasureSetupId);

                        this._authenticationService.SaveUser(this._authenticationService.LoggedInUser);
                        this._eventAggregatorProvider.Instance.GetEvent<ConfigurationChangedEvent>().Publish();
                    }
                }
            });
        }

        private async void OnSave()
        {
            if (await this.EditTemplateViewModel.SaveChanges())
            {
                base.OnOk();

            }
        }

        private async void OnSaveAs()
        {
            await this.EditTemplateViewModel.SaveAsCopy();
            base.OnOk();
        }

        private bool disposedValue = false; // To detect redundant calls

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this._measureController.SelectedTemplateChangedEvent -= OnSelectedTemplateChanged;

                    this._saveFileDialogService.Dispose();
                    this._openFileDialogService.Dispose();

                    if (this.EditTemplateViewModel.Template != null)
                    {
                        this.EditTemplateViewModel.Template.PropertyChanged -= OnTemplateChanged;
                    }
                }
            }
            base.Dispose(disposing);
        }
    }
}
