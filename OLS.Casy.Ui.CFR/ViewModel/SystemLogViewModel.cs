using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Linq;
using MigraDoc.Rendering;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Core.Logging.Api;
using OLS.Casy.Models;
using OLS.Casy.Models.Enums;
using OLS.Casy.Ui.AuditTrail.Documents;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Base.ViewModels;
using OLS.Casy.Ui.Base.Virtualization;
using OLS.Casy.Ui.Core.Api;

namespace OLS.Casy.Ui.AuditTrail.ViewModel
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(SystemLogViewModel))]
    public class SystemLogViewModel : DialogModelBase, IPartImportsSatisfiedNotification
    {
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly IAuthenticationService _authenticationService;
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly IDocumentSettingsManager _documentSettingsManager;
        private readonly IEnvironmentService _environmentService;
        private DateTime _filterFromDate = DateTime.UtcNow.AddMonths(-1);
        private DateTime _filterToDate = DateTime.UtcNow;
        private string _selectedFilterCategory;

        [ImportingConstructor]
        public SystemLogViewModel(ILogger logger,
           ILocalizationService localizationService,
           IAuthenticationService authenticationService,
           IEventAggregatorProvider eventAggregatorProvider,
           IDocumentSettingsManager documentSettingsManager,
           IEnvironmentService environmentService)
        {
            _logger = logger;
            _localizationService = localizationService;
            _authenticationService = authenticationService;
            _eventAggregatorProvider = eventAggregatorProvider;
            _documentSettingsManager = documentSettingsManager;
            _environmentService = environmentService;

            KnownCategories = new ObservableCollection<ComboBoxItemWrapperViewModel<string>>(Enum.GetNames(typeof(LogCategory)).Select(x =>
            new ComboBoxItemWrapperViewModel<string>(x)
            {
                DisplayItem = x,
                OnIsSelectedChanged = () => UpdateSystemLogEntries()
            }));
        }
        
        public AsyncVirtualizingCollection<SystemLogEntry> SystemLogEntries { get; private set; }

        public ObservableCollection<ComboBoxItemWrapperViewModel<string>> KnownCategories { get; }

        public DateTime FilterFromDate
        {
            get => _filterFromDate;
            set
            {
                if (value == _filterFromDate) return;
                _filterFromDate = value;
                NotifyOfPropertyChange();

                UpdateSystemLogEntries();
            }
        }

        public DateTime FilterToDate
        {
            get => _filterToDate;
            set
            {
                if (value == _filterToDate) return;
                _filterToDate = value;
                NotifyOfPropertyChange();

                UpdateSystemLogEntries();
            }
        }

        public string SelectedCategory
        {
            get => _selectedFilterCategory;
            set
            {
                if (value == _selectedFilterCategory) return;
                _selectedFilterCategory = value;
                NotifyOfPropertyChange();

                UpdateSystemLogEntries();
            }
        }

        public void OnImportsSatisfied()
        {
            Application.Current.Dispatcher.Invoke(UpdateSystemLogEntries);

            Title = _localizationService.GetLocalizedString("SystemLogView_Title");
        }

        public ICommand PrintCommand => new OmniDelegateCommand(OnPrint);

        private void UpdateSystemLogEntries()
        {
            var systemLogEntryProvider = new SystemLogEntryProvider(_logger, _environmentService)
            {
                FromDate = FilterFromDate,
                ToDate = FilterToDate,
                Categories = string.IsNullOrEmpty(SelectedCategory) ? new int[0] : new[] { (int) Enum.Parse(typeof(LogCategory), SelectedCategory) }
            };
            SystemLogEntries = new AsyncVirtualizingCollection<SystemLogEntry>(systemLogEntryProvider, 100, 100 * 1000);
            NotifyOfPropertyChange("SystemLogEntries");
        }

        private void OnPrint()
        {
            var renderer = new PdfDocumentRenderer(false);

            var categories = KnownCategories.Where(x => x.IsSelected).Select(x => (int)Enum.Parse(typeof(LogCategory), x.ValueItem)).ToList();
            var tableDocument = new SystemLogDocument(_localizationService, _authenticationService, _documentSettingsManager, _environmentService);
            var count = _logger.GetSystemLogEntryCount(FilterFromDate, FilterToDate, categories);
            renderer.Document = tableDocument.CreateDocument(_logger.GetSystemLogEntries(FilterFromDate, FilterToDate, categories, 0, count));

            var fileName = $"SystemLog_{DateTime.Now:yyyy-dd-M--HH-mm-ss}.pdf";

            renderer.RenderDocument();

            if (renderer.PdfDocument.Version < 14)
            {
                renderer.PdfDocument.Version = 14;
            }

            var appDataFolder = Path.Combine(Environment.GetFolderPath(
                        Environment.SpecialFolder.ApplicationData), "Casy", "temp");

            if (!Directory.Exists(appDataFolder))
            {
                Directory.CreateDirectory(appDataFolder);
            }
            fileName = Path.Combine(appDataFolder, fileName);

            renderer.PdfDocument.Save(fileName);

            try
            {
                Process.Start(fileName);
            }
            catch (Exception)
            {
                Task.Factory.StartNew(() =>
                {
                    var awaiter2 = new ManualResetEvent(false);

                    var messageBoxDialogWrapper = new ShowMessageBoxDialogWrapper()
                    {
                        Awaiter = awaiter2,
                        Message = "FailedToOpenFile_Message",
                        Title = "FailedToOpenFile_Title",
                        MessageParameter = new[] { fileName }
                    };

                    _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>()
                        .Publish(messageBoxDialogWrapper);
                    awaiter2.WaitOne();
                });
            }
        }
    }
}
