using DevExpress.Mvvm;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Models;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Core.Api;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Events;

namespace OLS.Casy.Ui.Core.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(IExportDialogModel))]
    public class ExportDialogModel : DialogModelBase, IExportDialogModel, IPartImportsSatisfiedNotification
    {
        private readonly ILocalizationService _localizationService;
        private readonly SelectMeasurementsViewModel _selectMeasurementsViewModel;
        private readonly ExportOptionsViewModel _exportOptionsViewModel;
        private readonly IBinaryImportExportProvider _binaryImportExportProvider;
        private readonly IRawDataExportProvider _rawDataExportProvider;
        private readonly IEventAggregatorProvider _eventAggregatorProvider;

        private bool _isSelectMesaurementsActive = true;

        [ImportingConstructor]
        public ExportDialogModel(ILocalizationService localizationService,
            SelectMeasurementsViewModel selectMeasurementsViewModel,
            ExportOptionsViewModel exportOptionsViewModel,
            IBinaryImportExportProvider binaryImportExportProvider,
            IRawDataExportProvider rawDataExportProvider,
            IEventAggregatorProvider eventAggregatorProvider)
        {
            _localizationService = localizationService;
            _selectMeasurementsViewModel = selectMeasurementsViewModel;
            _exportOptionsViewModel = exportOptionsViewModel;
            this._binaryImportExportProvider = binaryImportExportProvider;
            this._rawDataExportProvider = rawDataExportProvider;
            _eventAggregatorProvider = eventAggregatorProvider;
        }

        public SelectMeasurementsViewModel SelectMeasurementsViewModel
        {
            get { return _selectMeasurementsViewModel; }
        }

        public ExportOptionsViewModel ExportOptionsViewModel
        {
            get { return _exportOptionsViewModel; }
        }

        public bool IsSelectMesaurementsActive
        {
            get { return _isSelectMesaurementsActive; }
            set
            {
                if (value != _isSelectMesaurementsActive)
                {
                    this._isSelectMesaurementsActive = value;
                    NotifyOfPropertyChange();

                    UpdateTitle();
                }
            }
        }

        public ICommand NextCommand
        {
            get { return new OmniDelegateCommand(OnNext, CanNext); }
        }

        public bool CanNext
        {
            get
            {
                return ((IEnumerable<MeasureResult>) this._selectMeasurementsViewModel.SelectedMeasureResults).Any();
            }
        }

        public ICommand ExportCommand
        {
            get { return new OmniDelegateCommand(OnExport); }
        }

        public bool CanExport
        {
            get
            {
                return !string.IsNullOrEmpty(this._exportOptionsViewModel.ExportPath) && (this._exportOptionsViewModel.FileCountOption == IO.Api.FileCountOption.FilePerMeasurement || !string.IsNullOrEmpty(this._exportOptionsViewModel.FileName));
            }
        }

        public void OnImportsSatisfied()
        {
            UpdateTitle();

            this._selectMeasurementsViewModel.SelectedMeasureResults.CollectionChanged += OnSelectedMeasureResultsChanged;
            this._exportOptionsViewModel.PropertyChanged += OnExportOptionsChanged;
        }

        private void OnExportOptionsChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case "ExportPath":
                case "FileCountOption":
                case "FileName":
                    NotifyOfPropertyChange("CanExport");
                    break;
            }
        }

        private void OnSelectedMeasureResultsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyOfPropertyChange("CanNext");
        }

        private void UpdateTitle()
        {
            if (this.IsSelectMesaurementsActive)
            {
                this.Title = this._localizationService.GetLocalizedString("SelectMeasurementsView_Title");
            }
            else
            {
                this.Title = this._localizationService.GetLocalizedString("ExportOptionsView_Title");
            }
        }

        private void OnNext()
        {
            this.IsSelectMesaurementsActive = false;
            UpdateTitle();
        }

        private async void OnExport()
        {
            var measureResults = this.SelectMeasurementsViewModel.SelectedMeasureResults as IEnumerable<MeasureResult>;

            if (measureResults != null)
            {
                if (this._exportOptionsViewModel.FileCountOption == IO.Api.FileCountOption.OneFile)
                {
                    string fileName = Path.Combine(this._exportOptionsViewModel.ExportPath, this._exportOptionsViewModel.FileName);

                    await ExportToFile(fileName, measureResults);
                }
                else if(this._exportOptionsViewModel.FileCountOption == FileCountOption.FilePerMeasurement)
                {
                    foreach(var measureResult in measureResults)
                    {
                        string fileName = Path.Combine(this._exportOptionsViewModel.ExportPath, measureResult.Name);

                        await ExportToFile(fileName, new[] { measureResult });
                    }
                }
            }

            base.OnOk();
        }
        private async Task ExportToFile(string fileName, IEnumerable<MeasureResult> measureResults)
        {
            switch (this._exportOptionsViewModel.ExportFormat)
            {
                case ExportFormat.Csy:
                    fileName += ".csy";
                    break;
                case ExportFormat.Raw:
                    fileName += ".raw";
                    break;
            }

            try
            {
                FileInfo fileInfo = new FileInfo(fileName);

                int count = 1;
                while (fileInfo.Exists)
                {
                    fileInfo = new FileInfo(
                        Path.Combine(fileInfo.DirectoryName, Path.GetFileNameWithoutExtension(fileInfo.Name)) + "_" +
                        count++ + Path.GetExtension(fileInfo.Name));
                }

                switch (this._exportOptionsViewModel.ExportFormat)
                {
                    case IO.Api.ExportFormat.Csy:
                        await _binaryImportExportProvider.ExportMeasureResultsAsync(measureResults, fileInfo.FullName);
                        break;
                    case IO.Api.ExportFormat.Raw:
                        await _rawDataExportProvider.ExportMeasureResultsAsync(measureResults, fileInfo.FullName);
                        break;
                }
            }
            catch (FileFormatException ffe)
            {
                await Task.Factory.StartNew(() =>
                {
                    var awaiter = new ManualResetEvent(false);

                    var messageBoxWrapper = new ShowMessageBoxDialogWrapper()
                    {
                        Awaiter = awaiter,
                        Title = "Invalid file format",
                        Message =
                            "An error occured while exporting data. The format of your selected file seems to be invalid\n\n" + ffe.Message
                    };

                    _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>().Publish(messageBoxWrapper);

                    if (awaiter.WaitOne())
                    {
                    }
                });
            }
            catch (Exception ex)
            {
                await Task.Factory.StartNew(() =>
                {
                    var awaiter = new ManualResetEvent(false);

                    var messageBoxWrapper = new ShowMessageBoxDialogWrapper()
                    {
                        Awaiter = awaiter,
                        Title = "Error during file export",
                        Message =
                            "An error occured while exporting data\n\n" + ex.Message
                    };

                    _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>().Publish(messageBoxWrapper);

                    if (awaiter.WaitOne())
                    {
                    }
                });
            }
        }
    }
}
