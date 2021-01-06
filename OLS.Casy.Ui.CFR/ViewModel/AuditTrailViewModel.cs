using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MigraDoc.Rendering;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Models;
using OLS.Casy.Ui.AuditTrail.Documents;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Base.ViewModels;
using OLS.Casy.Ui.Core.Api;

namespace OLS.Casy.Ui.AuditTrail.ViewModel
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IAuditTrailViewModel))]
    public class AuditTrailViewModel : DialogModelBase, IAuditTrailViewModel, IPartImportsSatisfiedNotification
    {
        private readonly ILocalizationService _localizationService;
        private readonly IDatabaseStorageService _databaseStorageService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly IDocumentSettingsManager _documentSettingsManager;
        private readonly IEnvironmentService _environmentService;

        private readonly ObservableCollection<AuditTrailEntityViewModel> _auditTrailEntries;
        private string _measurementNameAndOrigin;
        private object _auditedObject;
        private string _measuredAtAndSerial;

        [ImportingConstructor]
        public AuditTrailViewModel(
           ILocalizationService localizationService,
           IDatabaseStorageService databaseStorageService,
           IAuthenticationService authenticationService,
           IEventAggregatorProvider eventAggregatorProvider,
           IDocumentSettingsManager documentSettingsManager,
           IEnvironmentService environmentService)
        {
            _localizationService = localizationService;
            _databaseStorageService = databaseStorageService;
            _authenticationService = authenticationService;
            _eventAggregatorProvider = eventAggregatorProvider;
            _documentSettingsManager = documentSettingsManager;
            _environmentService = environmentService;

            _auditTrailEntries = new ObservableCollection<AuditTrailEntityViewModel>();

            
        }

        public IList<AuditTrailEntityViewModel> AuditTrailEntries => _auditTrailEntries;

        public ObservableCollection<ComboBoxItemWrapperViewModel<string>> KnownActions { get; private set; }
        public ObservableCollection<ComboBoxItemWrapperViewModel<string>> KnownEntities { get; private set; }

        private void ReloadAuditTrailEntries()
        {
            if(_auditedObject is MeasureSetup)
            {
                this.LoadAuditTrailEntries((MeasureSetup)_auditedObject);
            }
            else
            {
                this.LoadAuditTrailEntries((MeasureResult)_auditedObject);
            }
        }

        public void LoadAuditTrailEntries(MeasureResult measureResult)
        {
            _auditedObject = measureResult;

            Application.Current.Dispatcher.Invoke(() =>
            {
                MeasurementNameAndOrigin =
                    $"Measurement: {measureResult.Name}{(string.IsNullOrEmpty(measureResult.Origin) ? string.Empty : $" (Origin: {measureResult.Origin})")}";

                var timeZone = measureResult.MeasuredAtTimeZone == null ? TimeZoneInfo.Local : measureResult.MeasuredAtTimeZone;

                var isDaylightSaving = timeZone.IsDaylightSavingTime(measureResult.MeasuredAt);
                
                if (isDaylightSaving)
                {
                    var split = timeZone.DisplayName.Split(':');

                    if (split[0].Contains("+"))
                    {
                        var hours = (int.Parse(split[0].Split('+')[1]) + 1).ToString("D2");
                        var timeZoneString = $"(UTC+{hours}:{split[1]}";
                        //MeasuredAtAndSerial = $"Measurement Date: {measureResult.MeasuredAt:dd.MM.yyyy HH:mm:ss} {timeZoneString}";
                        MeasuredAtAndSerial = $"Measurement Date: {_environmentService.GetDateTimeString(measureResult.MeasuredAt, true)} {timeZoneString}";
                    }
                    else
                    {
                        var hours = (int.Parse(split[0].Split('-')[1]) + 1).ToString("D2");
                        var timeZoneString = $"(UTC-{hours}:{split[1]}";
                        //MeasuredAtAndSerial = $"Measurement Date: {measureResult.MeasuredAt:dd.MM.yyyy HH:mm:ss} {timeZoneString}";
                        MeasuredAtAndSerial = $"Measurement Date: {_environmentService.GetDateTimeString(measureResult.MeasuredAt, true)} {timeZoneString}";
                    }
                }
                else
                {
                    //MeasuredAtAndSerial = $"Measurement Date: {measureResult.MeasuredAt:dd.MM.yyyy HH:mm:ss} {timeZone.DisplayName}";
                    MeasuredAtAndSerial = $"Measurement Date: {_environmentService.GetDateTimeString(measureResult.MeasuredAt, true)} {timeZone.DisplayName}";
                }

                _auditTrailEntries.Clear();

                var filterActions = KnownActions.Where(x => x.IsSelected).Select(x => x.ValueItem);
                var filterEntities = KnownEntities.Where(x => x.IsSelected).Select(x => x.ValueItem);

                var measureResultDeep = _databaseStorageService.LoadExportData(measureResult);
                foreach (var auditTrailEntry in measureResultDeep.AuditTrailEntries.OrderByDescending(ate => ate.DateChanged.UtcDateTime))
                {
                    if (filterActions.Any() && !filterActions.Contains(auditTrailEntry.Action)) continue;
                    if (filterEntities.Any() && !filterEntities.Any(x => auditTrailEntry.EntityName.StartsWith(x))) continue;

                    _auditTrailEntries.Add(new AuditTrailEntityViewModel(auditTrailEntry, _localizationService, _databaseStorageService, _environmentService));
                }

                NotifyOfPropertyChange("AuditTrailEntries");
            });
        }

        public void LoadAuditTrailEntries(MeasureSetup template)
        {
            _auditedObject = template;

            Application.Current.Dispatcher.Invoke(() =>
            {
                MeasurementNameAndOrigin =
                    $"Template: {template.Name}";
                MeasuredAtAndSerial = "";

                _auditTrailEntries.Clear();

                var filterActions = KnownActions.Where(x => x.IsSelected).Select(x => x.ValueItem);
                var filterEntities = KnownEntities.Where(x => x.IsSelected).Select(x => x.ValueItem);

                var templateDeep = _databaseStorageService.LoadExportData(template);
                foreach (var auditTrailEntry in templateDeep.AuditTrailEntries.OrderByDescending(ate => ate.DateChanged.UtcDateTime))
                {
                    if (filterActions.Any() && !filterActions.Contains(auditTrailEntry.Action)) continue;
                    if (filterEntities.Any() && !filterEntities.Any(x => auditTrailEntry.EntityName.StartsWith(x))) continue;

                    _auditTrailEntries.Add(new AuditTrailEntityViewModel(auditTrailEntry, _localizationService, _databaseStorageService, _environmentService));
                }

                NotifyOfPropertyChange("AuditTrailEntries");
            });
        }

        public void OnImportsSatisfied()
        {
            Title = _localizationService.GetLocalizedString("AuditTrailView_Title");

            KnownActions = new ObservableCollection<ComboBoxItemWrapperViewModel<string>>()
            {
                new ComboBoxItemWrapperViewModel<string>("Added")
                {
                    DisplayItem = _localizationService.GetLocalizedString("AuditTrailEntry_Action_Added"),
                    OnIsSelectedChanged = () => ReloadAuditTrailEntries()
                },
                new ComboBoxItemWrapperViewModel<string>("Deleted")
                {
                    DisplayItem = _localizationService.GetLocalizedString("AuditTrailEntry_Action_Deleted"),
                    OnIsSelectedChanged = () => ReloadAuditTrailEntries()
                },
                new ComboBoxItemWrapperViewModel<string>("Modified")
                {
                    DisplayItem = _localizationService.GetLocalizedString("AuditTrailEntry_Action_Modified"),
                    OnIsSelectedChanged = () => ReloadAuditTrailEntries()
                }
            };

            KnownEntities = new ObservableCollection<ComboBoxItemWrapperViewModel<string>>()
            {
                new ComboBoxItemWrapperViewModel<string>("MeasureResultEntity")
                {
                    DisplayItem = _localizationService.GetLocalizedString("AuditTrailEntry_Name_Measurement"),
                    OnIsSelectedChanged = () => ReloadAuditTrailEntries()
                },
                new ComboBoxItemWrapperViewModel<string>("Cursor")
                {
                    DisplayItem = _localizationService.GetLocalizedString("AuditTrailEntry_Name_Range"),
                    OnIsSelectedChanged = () => ReloadAuditTrailEntries()
                },
                new ComboBoxItemWrapperViewModel<string>("MeasureSetupEntity")
                {
                    DisplayItem = _localizationService.GetLocalizedString("AuditTrailEntry_Name_Template"),
                    OnIsSelectedChanged = () => ReloadAuditTrailEntries()
                }
            };
        }

        public string MeasurementNameAndOrigin
        {
            get => _measurementNameAndOrigin;
            set
            {
                if (value == _measurementNameAndOrigin) return;
                _measurementNameAndOrigin = value;
                NotifyOfPropertyChange();
            }
        }

        public string MeasuredAtAndSerial
        {
            get => _measuredAtAndSerial;
            set
            {
                if (value == _measuredAtAndSerial) return;
                _measuredAtAndSerial = value;
                NotifyOfPropertyChange();
            }
        }

        public ICommand PrintCommand => new OmniDelegateCommand(OnPrint);

        private void OnPrint()
        {
            var renderer = new PdfDocumentRenderer(false);

            var tableDocument = new AuditTrailDocument(_localizationService, _authenticationService, _documentSettingsManager, _environmentService);
            renderer.Document = tableDocument.CreateDocument(_auditTrailEntries, MeasurementNameAndOrigin, MeasuredAtAndSerial);

            var name = _auditedObject is MeasureResult
                ? ((MeasureResult) _auditedObject).Name
                : ((MeasureSetup) _auditedObject).Name;
            var fileName = $"AuditTrail_{name}_{DateTime.Now:yyyy-dd-M--HH-mm-ss}.pdf";

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
