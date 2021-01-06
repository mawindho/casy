using DevExpress.Export;
using DevExpress.Mvvm;
using DevExpress.Xpf.Grid;
using DevExpress.XtraPrinting;
using MigraDoc.Rendering;
using OLS.Casy.Core;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Models;
using OLS.Casy.Ui.Analyze.Documents;
using OLS.Casy.Ui.Analyze.Models;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Base.Models;
using OLS.Casy.Ui.Core.Api;
using OLS.Casy.Ui.MainControls.Api;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Input;

namespace OLS.Casy.Ui.Analyze.ViewModels
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(HostViewModel))]
    public class MeasureResultTableViewModel : HostViewModel, IPartImportsSatisfiedNotification
    {
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly IMeasureResultManager _measureResultManager;
        private readonly IMeasureResultManager _generalMeasureResultManager;
        private readonly ICompositionFactory _compositionFactory;
        private readonly IDatabaseStorageService _databaseStorageService;
        private readonly ILocalizationService _localizationService;
        private readonly Core.Api.ISaveFileDialogService _saveFileDialogService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IEnvironmentService _environmentService;
        private readonly IDocumentSettingsManager _documentSettingsManager;

        private readonly SmartCollection<AdditinalGridColumnInfo> _additionalColumns;

        private readonly List<Lazy<MeasureResultModel>> _selectedDataViewModelExports;
        private SmartCollection<string> _categories;

        private bool _refreshData;
        private bool _isViabilityPreset;
        private bool _isFreeRangePreset;
        private bool _isUserPreset;

        [ImportingConstructor]
        public MeasureResultTableViewModel(
            IEventAggregatorProvider eventAggregatorProvider,
            IMeasureResultManager measureResultManager,
            IMeasureResultManager generalMeasureResultManager,
            ICompositionFactory compositionFactory,
            IDatabaseStorageService databaseStorageService,
            ILocalizationService localizationService,
            Core.Api.ISaveFileDialogService saveFileDialogService,
            IAuthenticationService authenticationService,
            IEnvironmentService environmentService,
            IDocumentSettingsManager documentSettingsManager
        )
        {
            _eventAggregatorProvider = eventAggregatorProvider;
            _measureResultManager = measureResultManager;
            _generalMeasureResultManager = generalMeasureResultManager;
            _compositionFactory = compositionFactory;
            _databaseStorageService = databaseStorageService;
            _localizationService = localizationService;
            _saveFileDialogService = saveFileDialogService;
            _authenticationService = authenticationService;
            _environmentService = environmentService;
            _documentSettingsManager = documentSettingsManager;

            _selectedDataViewModelExports = new List<Lazy<MeasureResultModel>>();
            SelectedDataViewModels = new SmartCollection<MeasureResultModel>();
            _additionalColumns = new SmartCollection<AdditinalGridColumnInfo>();
            SelectedRows = new ObservableCollection<MeasureResultModel>();

            _categories = new SmartCollection<string>();
        }

        public SmartCollection<MeasureResultModel> SelectedDataViewModels { get; }

        public ObservableCollection<AdditinalGridColumnInfo> AdditionalColumns => _additionalColumns;

        public ObservableCollection<MeasureResultModel> SelectedRows { get; }

        public SmartCollection<string> Filter
        {
            get => _categories;
            set
            {
                _categories = value;
                NotifyOfPropertyChange();
            }
        }

        public ICommand DeleteCommand => new OmniDelegateCommand(OnDelete);

        public bool CanDelete => SelectedRows.Count > 0;

        public ICommand ExportCommand => new DelegateCommand<TableView>(OnExport);

        public bool CanExport => SelectedRows.Count > 0;

        public ICommand PrintCommand => new OmniDelegateCommand(OnPrint);

        public bool CanPrint => SelectedRows.Count > 0;

        public bool IsViabilityPreset
        {
            get => _isViabilityPreset;
            set
            {
                if (value == _isViabilityPreset) return;
                _isViabilityPreset = value;
                NotifyOfPropertyChange();

                InitAnnotationTypes();
            }
        }

        public bool IsFreeRangePreset
        {
            get => _isFreeRangePreset;
            set
            {
                if (value == _isFreeRangePreset) return;
                _isFreeRangePreset = value;
                NotifyOfPropertyChange();

                InitAnnotationTypes();
            }
        }

        public bool IsUserPreset
        {
            get => _isUserPreset;
            set
            {
                if (value == _isUserPreset) return;
                _isUserPreset = value;
                NotifyOfPropertyChange();

                InitAnnotationTypes();
            }
        }

        public ICommand SaveSelectionCommand => new OmniDelegateCommand(OnSaveSelection);

        private async void OnSaveSelection()
        {
            _authenticationService.LoggedInUser.SelectedTableColumns.Clear();
            var selectionString = string.Empty;
            foreach (var col in _additionalColumns)
            {
                if (col.IsSelectedRow)
                {
                    _authenticationService.LoggedInUser.SelectedTableColumns.Add(col.Binding);
                }
            }

            _authenticationService.SaveUser(_authenticationService.LoggedInUser);

            await Task.Factory.StartNew(() =>
            {
                var awaiter = new ManualResetEvent(false);

                var messageBoxWrapper = new ShowMessageBoxDialogWrapper()
                {
                    Awaiter = awaiter,
                    Title = "Selection saved",
                    Message =
                        "The current selection of table headers has been saved successfully."
                };

                _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>().Publish(messageBoxWrapper);

                if (awaiter.WaitOne())
                {
                }
            });
        }

        public ICommand ClearSelectionCommand => new OmniDelegateCommand(OnClearSelection);

        private void OnClearSelection()
        {
            _isFreeRangePreset = false;
            _isViabilityPreset = false;
            NotifyOfPropertyChange("IsFreeRangePreset");
            NotifyOfPropertyChange("IsViabilityPreset");

            foreach (var col in _additionalColumns)
            {
                col.IsSelectedRow = false;
            }

            InitAnnotationTypes();
        }

        public void OnImportsSatisfied()
        {
            _eventAggregatorProvider.Instance.GetEvent<NavigateToEvent>().Subscribe(OnNavigateToEvent);
            _eventAggregatorProvider.Instance.GetEvent<KeyDownEvent>().Subscribe(OnKeyDown);
            _eventAggregatorProvider.Instance.GetEvent<ConfigurationChangedEvent>()
                .Subscribe(OnConfigurationChangedEvent);
            _eventAggregatorProvider.Instance.GetEvent<MeasureResultStoredEvent>().Subscribe(OnMeasureResultStored);

            _measureResultManager.SelectedMeasureResultsChanged += OnSelectedMeasureResultsChanged;

            _localizationService.LanguageChanged += OnLanguageChanged;
            OnLanguageChanged(null, null);

            SelectedRows.CollectionChanged += OnSelectedRowsChanged;
        }

        private void OnMeasureResultStored()
        {
            if (IsActive)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var measureResult in _measureResultManager.SelectedMeasureResults)
                    {
                        if (!measureResult.IsTemporary && _selectedDataViewModelExports.All(vm =>
                                vm.Value.AssociatedMeasureResult.MeasureResultId != measureResult.MeasureResultId))
                        {
                            var modelExport = _compositionFactory.GetExport<MeasureResultModel>();
                            var model = modelExport.Value;

                            model.PropertyChanged += OnModelPropertyChanged;
                            model.AssociatedMeasureResult = measureResult;
                            measureResult.PropertyChanged += OnMeasureResultChanged;
                            measureResult.MeasureSetup.Cursors.CollectionChanged += OnCursorsChanged;
                            _selectedDataViewModelExports.Add(modelExport);
                            SelectedDataViewModels.Add(model);
                        }
                    }
                });
            }
        }

        private void OnCursorsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var oldItem in e.OldItems.OfType<Casy.Models.Cursor>())
                {
                    oldItem.PropertyChanged -= OnCursorChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (var newItem in e.NewItems.OfType<Casy.Models.Cursor>())
                {
                    newItem.PropertyChanged += OnCursorChanged;
                }
            }

            InitAnnotationTypes();
        }

        private void OnCursorChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Name":
                    InitAnnotationTypes();
                    break;
            }
        }

        private void OnMeasureResultChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "IsVisible":
                    RefreshData = true;
                    break;
                case "Experiment":
                    NotifyOfPropertyChange("KnownExperiments");
                    break;
                case "Group":
                    NotifyOfPropertyChange("KnownGroups");
                    break;
            }
        }

        private void OnSelectedRowsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyOfPropertyChange("CanPrint");
            NotifyOfPropertyChange("CanExport");
            NotifyOfPropertyChange("CanDelete");
        }

        private void OnLanguageChanged(object sender, LocalizationEventArgs e)
        {
            InitAnnotationTypes();
        }

        private void OnKeyDown(object e)
        {
            if (!IsActive) return;

            var keyEventArgs = e as KeyEventArgs;
            if (keyEventArgs.Key != Key.A ||
                (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control) return;

            SelectedRows.Clear();
            foreach (var data in SelectedDataViewModels)
            {
                SelectedRows.Add(data);
            }

            NotifyOfPropertyChange("SelectedRows");
        }

        public bool RefreshData
        {
            get => _refreshData;
            set
            {
                _refreshData = value;
                NotifyOfPropertyChange();
            }
        }

        private void OnNavigateToEvent(object argument)
        {
            var navigationArgs = (NavigationArgs) argument;
            switch (navigationArgs.NavigationCategory)
            {
                case NavigationCategory.AnalyseTable:
                    InitAnnotationTypes();
                    IsActive = true;
                    break;
                case NavigationCategory.Measurement:
                case NavigationCategory.Analyse:
                case NavigationCategory.MeasureResults:
                case NavigationCategory.Template:
                    break;
                default:
                    IsActive = false;
                    break;
            }
        }

        private void OnConfigurationChangedEvent()
        {
            InitAnnotationTypes();
        }

        private void InitAnnotationTypes()
        {
            var prevSelectedColumns =
                _additionalColumns.Where(ad => ad.IsSelectedRow).Select(ad => ad.Binding).ToList();

            var categories = new List<string>();
            var additionalColumns = new List<AdditinalGridColumnInfo>();

            categories.Add("MeasureResultTableView_Category_General");
            categories.Add("MeasureResultTableView_Category_Annotation");
            categories.Add("MeasureResultTableView_Category_Subpopulation");

            additionalColumns.Add(new AdditinalGridColumnInfo
            {
                Binding = "Name",
                Header = _localizationService.GetLocalizedString("MeasureResultTableView_GridColumn_Name"),
                IsTwoWayBinding = false,
                Category = "MeasureResultTableView_Category_General",
                Order = 0
            });
            prevSelectedColumns.Add("Name");

            additionalColumns.Add(new AdditinalGridColumnInfo
            {
                Binding = "Experiment",
                Header = _localizationService.GetLocalizedString("MeasureResultTebleView_GridColumn_Experiment"),
                Style = "ExperimentColumnStyle",
                IsTwoWayBinding = true,
                Category = "MeasureResultTableView_Category_General",
                Order = 1
            });
            prevSelectedColumns.Add("Experiment");

            additionalColumns.Add(new AdditinalGridColumnInfo
            {
                Binding = "Group",
                Header = _localizationService.GetLocalizedString("MeasureResultTebleView_GridColumn_Group"),
                Style = "GroupColumnStyle",
                IsTwoWayBinding = true,
                Category = "MeasureResultTableView_Category_General",
                Order = 2
            });
            prevSelectedColumns.Add("Group");

            additionalColumns.Add(new AdditinalGridColumnInfo
            {
                Binding = "MeasuredAt",
                Header = _localizationService.GetLocalizedString("AnnotationType_MeasuredAt_Header"),
                IsTwoWayBinding = false,
                Category = "MeasureResultTableView_Category_General",
                Order = 3
            });

            additionalColumns.Add(new AdditinalGridColumnInfo
            {
                Binding = "TemplateName",
                Header = _localizationService.GetLocalizedString("AnnotationType_TemplateName_Header"),
                IsTwoWayBinding = false,
                Category = "MeasureResultTableView_Category_General",
                Order = 4
            });

            additionalColumns.Add(new AdditinalGridColumnInfo
            {
                Binding = "CapillarySize",
                Header = _localizationService.GetLocalizedString("AnnotationType_CapillarySize_Header"),
                IsTwoWayBinding = false,
                Category = "MeasureResultTableView_Category_General",
                Order = 5
            });

            additionalColumns.Add(new AdditinalGridColumnInfo
            {
                Binding = "ToDiameter",
                Header = _localizationService.GetLocalizedString("AnnotationType_ToDiameter_Header"),
                IsTwoWayBinding = false,
                Category = "MeasureResultTableView_Category_General",
                Order = 6
            });

            additionalColumns.Add(new AdditinalGridColumnInfo
            {
                Binding = "Repeats",
                Header = _localizationService.GetLocalizedString("AnnotationType_Repeats_Header"),
                IsTwoWayBinding = false,
                Category = "MeasureResultTableView_Category_General",
                Order = 7
            });

            additionalColumns.Add(new AdditinalGridColumnInfo
            {
                Binding = "Counts__",
                Header = _localizationService.GetLocalizedString("ResultItemType_Counts_Name"),
                IsTwoWayBinding = false,
                Category = "MeasureResultTableView_Category_General",
                Order = 8
            });

            additionalColumns.Add(new AdditinalGridColumnInfo
            {
                Binding = "CountsPerMl__",
                Header = _localizationService.GetLocalizedString("ResultItemType_CountsPerMl_Name"),
                IsTwoWayBinding = false,
                Category = "MeasureResultTableView_Category_General",
                Order = 9
            });

            additionalColumns.Add(new AdditinalGridColumnInfo
            {
                Binding = "TotalCountsPerMl__",
                Header = _localizationService.GetLocalizedString("ResultItemType_TotalCountsPerMl_Name"),
                IsTwoWayBinding = false,
                Category = "MeasureResultTableView_Category_General",
                Order = 10
            });

            additionalColumns.Add(new AdditinalGridColumnInfo
            {
                Binding = "Concentration__",
                Header = _localizationService.GetLocalizedString("ResultItemType_Concentration_Name"),
                IsTwoWayBinding = false,
                Category = "MeasureResultTableView_Category_General",
                Order = 11
            });

            additionalColumns.Add(new AdditinalGridColumnInfo
            {
                Binding = "CountsAboveDiameter__",
                Header = _localizationService.GetLocalizedString("ResultItemType_CountsAboveDiameter_Name",
                        this._localizationService.GetLocalizedString("AnnotationType_ToDiameter_Header"))
                    .Replace(" µm", ""),
                IsTwoWayBinding = false,
                Category = "MeasureResultTableView_Category_General",
                Order = 12
            });

            additionalColumns.Add(new AdditinalGridColumnInfo
            {
                Binding = "DebrisCount__",
                Header = _localizationService.GetLocalizedString("ResultItemType_DebrisCount_Name"),
                IsTwoWayBinding = false,
                Category = "MeasureResultTableView_Category_General",
                Order = 13
            });

            additionalColumns.Add(new AdditinalGridColumnInfo
            {
                Binding = "DilutionFactor",
                Header = _localizationService.GetLocalizedString("AnnotationType_DilutionFactor_Header"),
                IsTwoWayBinding = false,
                Category = "MeasureResultTableView_Category_General",
                Order = 14
            });

            additionalColumns.Add(new AdditinalGridColumnInfo
            {
                Binding = "AggregationFactor__",
                Header = _localizationService.GetLocalizedString("ResultItemType_AggregationFactor_Name"),
                IsTwoWayBinding = false,
                Category = "MeasureResultTableView_Category_General",
                Order = 15
            });

            additionalColumns.Add(new AdditinalGridColumnInfo
            {
                Binding = "AggregationCalculationMode",
                Header = _localizationService.GetLocalizedString("AnnotationType_AggregationCalculationMode_Header"),
                IsTwoWayBinding = false,
                Category = "MeasureResultTableView_Category_General",
                Order = 16
            });

            additionalColumns.Add(new AdditinalGridColumnInfo
            {
                Binding = "VolumePerMl__",
                Header = _localizationService.GetLocalizedString("ResultItemType_VolumePerMl_Name"),
                IsTwoWayBinding = false,
                Category = "MeasureResultTableView_Category_General",
                Order = 17
            });

            additionalColumns.Add(new AdditinalGridColumnInfo
            {
                Binding = "Volume",
                Header = _localizationService.GetLocalizedString("AnnotationType_Volume_Header"),
                IsTwoWayBinding = false,
                Category = "MeasureResultTableView_Category_General",
                Order = 18
            });

            var license = _environmentService.GetEnvironmentInfo("License") as Casy.Core.Activation.License;
            if (license != null && license.AddOns.Contains("cfr"))
            {
                additionalColumns.Add(new AdditinalGridColumnInfo
                {
                    Binding = "CreatedBy",
                    Header = _localizationService.GetLocalizedString("AnnotationType_CreatedBy_Header"),
                    IsTwoWayBinding = false,
                    Category = "MeasureResultTableView_Category_General",
                    Order = 19
                });

                additionalColumns.Add(new AdditinalGridColumnInfo
                {
                    Binding = "CreatedAt",
                    Header = _localizationService.GetLocalizedString("AnnotationType_CreatedAt_Header"),
                    IsTwoWayBinding = false,
                    Category = "MeasureResultTableView_Category_General",
                    Order = 20
                });

                additionalColumns.Add(new AdditinalGridColumnInfo
                {
                    Binding = "LastModifiedBy",
                    Header = _localizationService.GetLocalizedString("AnnotationType_LastModifiedBy_Header"),
                    IsTwoWayBinding = false,
                    Category = "MeasureResultTableView_Category_General",
                    Order = 21
                });

                additionalColumns.Add(new AdditinalGridColumnInfo
                {
                    Binding = "LastModifiedAt",
                    Header = _localizationService.GetLocalizedString("AnnotationType_LastModifiedAt_Header"),
                    IsTwoWayBinding = false,
                    Category = "MeasureResultTableView_Category_General",
                    Order = 22
                });
            }

            var annotationTypes = _databaseStorageService.GetAnnotationTypes();
            foreach (var annotationType in annotationTypes)
            {
                additionalColumns.Add(new AdditinalGridColumnInfo
                {
                    Binding = annotationType.AnnottationTypeName,
                    Header = annotationType.AnnottationTypeName,
                    IsTwoWayBinding = true,
                    Category = "MeasureResultTableView_Category_Annotation"

                });
            }

            foreach (var model in this.SelectedDataViewModels)
            {
                foreach (var cursor in model.AssociatedMeasureResult.MeasureSetup.Cursors)
                {
                    if (!categories.Contains(cursor.Name))
                    {
                        categories.Add(cursor.Name);
                    }

                    var encoded = HttpUtility.UrlEncode(cursor.Name);

                    if (additionalColumns.All(info => info.Binding != "CountsPerMl__" + encoded))
                    {
                        additionalColumns.Add(new AdditinalGridColumnInfo
                        {
                            Binding = "CountsPerMl__" + encoded,
                            Header =
                                $"{_localizationService.GetLocalizedString("ResultItemType_CountsPerMl_Name")} ({_localizationService.GetLocalizedString(cursor.Name)})",
                            IsTwoWayBinding = false,
                            Category = cursor.Name,
                            Order = 23
                        });
                    }

                    if (additionalColumns.All(info => info.Binding != "VolumePerMl__" + encoded))
                    {
                        additionalColumns.Add(new AdditinalGridColumnInfo
                        {
                            Binding = "VolumePerMl__" + encoded,
                            Header =
                                $"{_localizationService.GetLocalizedString("ResultItemType_VolumePerMl_Name")} ({_localizationService.GetLocalizedString(cursor.Name)})",
                            IsTwoWayBinding = false,
                            Category = cursor.Name,
                            Order = 24
                        });
                    }

                    if (additionalColumns.All(info => info.Binding != "MeanDiameter__" + encoded))
                    {
                        additionalColumns.Add(new AdditinalGridColumnInfo
                        {
                            Binding = "MeanDiameter__" + encoded,
                            Header =
                                $"{_localizationService.GetLocalizedString("ResultItemType_MeanDiameter_Name")} ({_localizationService.GetLocalizedString(cursor.Name)})",
                            IsTwoWayBinding = false,
                            Category = cursor.Name,
                            Order = 25
                        });
                    }

                    if (additionalColumns.All(info => info.Binding != "PeakDiameter__" + encoded))
                    {
                        additionalColumns.Add(new AdditinalGridColumnInfo
                        {
                            Binding = "PeakDiameter__" + encoded,
                            Header =
                                $"{_localizationService.GetLocalizedString("ResultItemType_PeakDiameter_Name")} ({_localizationService.GetLocalizedString(cursor.Name)})",
                            IsTwoWayBinding = false,
                            Category = cursor.Name,
                            Order = 26
                        });
                    }

                    if (additionalColumns.All(info => info.Binding != "MeanVolume__" + encoded))
                    {
                        additionalColumns.Add(new AdditinalGridColumnInfo
                        {
                            Binding = "MeanVolume__" + encoded,
                            Header =
                                $"{_localizationService.GetLocalizedString("ResultItemType_MeanVolume_Name")} ({_localizationService.GetLocalizedString(cursor.Name)})",
                            IsTwoWayBinding = false,
                            Category = cursor.Name,
                            Order = 27
                        });
                    }

                    if (additionalColumns.All(info => info.Binding != "PeakVolume__" + encoded))
                    {
                        additionalColumns.Add(new AdditinalGridColumnInfo
                        {
                            Binding = "PeakVolume__" + encoded,
                            Header =
                                $"{_localizationService.GetLocalizedString("ResultItemType_PeakVolume_Name")} ({_localizationService.GetLocalizedString(cursor.Name)})",
                            IsTwoWayBinding = false,
                            Category = cursor.Name,
                            Order = 28
                        });
                    }

                    if (additionalColumns.All(info => info.Binding != "Counts__" + encoded))
                    {
                        additionalColumns.Add(new AdditinalGridColumnInfo
                        {
                            Binding = "Counts__" + encoded,
                            Header =
                                $"{_localizationService.GetLocalizedString("ResultItemType_Counts_Name")} ({_localizationService.GetLocalizedString(cursor.Name)})",
                            IsTwoWayBinding = false,
                            Category = cursor.Name,
                            Order = 29
                        });
                    }

                    if (additionalColumns.All(info => info.Binding != "CountsPercentage__" + encoded))
                    {
                        additionalColumns.Add(new AdditinalGridColumnInfo
                        {
                            Binding = "CountsPercentage__" + encoded,
                            Header =
                                $"{_localizationService.GetLocalizedString("ResultItemType_CountsPercentage_Name")} ({_localizationService.GetLocalizedString(cursor.Name)})",
                            IsTwoWayBinding = false,
                            Category = cursor.Name,
                            Order = 30
                        });
                    }

                    if (cursor.Subpopulation == "A" &&
                        additionalColumns.All(info => info.Binding != "SubpopulationAPercentage__"))
                    {
                        additionalColumns.Add(new AdditinalGridColumnInfo
                        {
                            Binding = "SubpopulationAPercentage__",
                            Header =
                                $"{_localizationService.GetLocalizedString("ResultItemType_SubpopulationAPercentage_Name")}",
                            IsTwoWayBinding = false,
                            Category = "MeasureResultTableView_Category_Subpopulation",
                            Order = 31
                        });

                        additionalColumns.Add(new AdditinalGridColumnInfo
                        {
                            Binding = "SubpopulationACountsPerMl__",
                            Header =
                                $"{_localizationService.GetLocalizedString("ResultItemType_SubpopulationACountsPerMl_Name")}",
                            IsTwoWayBinding = false,
                            Category = "MeasureResultTableView_Category_Subpopulation",
                            Order = 32
                        });
                    }

                    if (cursor.Subpopulation == "B" &&
                        additionalColumns.All(info => info.Binding != "SubpopulationBPercentage__"))
                    {
                        additionalColumns.Add(new AdditinalGridColumnInfo
                        {
                            Binding = "SubpopulationBPercentage__",
                            Header =
                                $"{_localizationService.GetLocalizedString("ResultItemType_SubpopulationBPercentage_Name")}",
                            IsTwoWayBinding = false,
                            Category = "MeasureResultTableView_Category_Subpopulation",
                            Order = 33
                        });

                        additionalColumns.Add(new AdditinalGridColumnInfo
                        {
                            Binding = "SubpopulationBCountsPerMl__",
                            Header =
                                $"{_localizationService.GetLocalizedString("ResultItemType_SubpopulationBCountsPerMl_Name")}",
                            IsTwoWayBinding = false,
                            Category = "MeasureResultTableView_Category_Subpopulation",
                            Order = 34
                        });
                    }

                    if (cursor.Subpopulation == "C" &&
                        additionalColumns.All(info => info.Binding != "SubpopulationCPercentage__"))
                    {
                        additionalColumns.Add(new AdditinalGridColumnInfo
                        {
                            Binding = "SubpopulationCPercentage__",
                            Header =
                                $"{_localizationService.GetLocalizedString("ResultItemType_SubpopulationCPercentage_Name")}",
                            IsTwoWayBinding = false,
                            Category = "MeasureResultTableView_Category_Subpopulation",
                            Order = 34
                        });

                        additionalColumns.Add(new AdditinalGridColumnInfo
                        {
                            Binding = "SubpopulationCCountsPerMl__",
                            Header =
                                $"{_localizationService.GetLocalizedString("ResultItemType_SubpopulationCCountsPerMl_Name")}",
                            IsTwoWayBinding = false,
                            Category = "MeasureResultTableView_Category_Subpopulation",
                            Order = 35
                        });
                    }

                    if (cursor.Subpopulation == "D" &&
                        additionalColumns.All(info => info.Binding != "SubpopulationDPercentage__"))
                    {
                        additionalColumns.Add(new AdditinalGridColumnInfo
                        {
                            Binding = "SubpopulationDPercentage__",
                            Header =
                                $"{_localizationService.GetLocalizedString("ResultItemType_SubpopulationDPercentage_Name")}",
                            IsTwoWayBinding = false,
                            Category = "MeasureResultTableView_Category_Subpopulation",
                            Order = 36
                        });

                        additionalColumns.Add(new AdditinalGridColumnInfo
                        {
                            Binding = "SubpopulationDCountsPerMl__",
                            Header =
                                $"{_localizationService.GetLocalizedString("ResultItemType_SubpopulationDCountsPerMl_Name")}",
                            IsTwoWayBinding = false,
                            Category = "MeasureResultTableView_Category_Subpopulation",
                            Order = 37
                        });
                    }

                    if (cursor.Subpopulation == "E" &&
                        additionalColumns.All(info => info.Binding != "SubpopulationEPercentage__"))
                    {
                        additionalColumns.Add(new AdditinalGridColumnInfo
                        {
                            Binding = "SubpopulationEPercentage__",
                            Header =
                                $"{_localizationService.GetLocalizedString("ResultItemType_SubpopulationEPercentage_Name")}",
                            IsTwoWayBinding = false,
                            Category = "MeasureResultTableView_Category_Subpopulation",
                            Order = 38
                        });

                        additionalColumns.Add(new AdditinalGridColumnInfo
                        {
                            Binding = "SubpopulationECountsPerMl__",
                            Header =
                                $"{_localizationService.GetLocalizedString("ResultItemType_SubpopulationECountsPerMl_Name")}",
                            IsTwoWayBinding = false,
                            Category = "MeasureResultTableView_Category_Subpopulation",
                            Order = 39
                        });
                    }
                }
            }

            prevSelectedColumns.RemoveAll(item => item.StartsWith("CountsPercentage__"));
            prevSelectedColumns.RemoveAll(item => item.StartsWith("PeakDiameter__"));
            prevSelectedColumns.RemoveAll(item => item.StartsWith("CountsPerMl__"));

            if (_isViabilityPreset && _measureResultManager.SelectedMeasureResults.Any(mr =>
                    mr.MeasureSetup.MeasureMode == Casy.Models.Enums.MeasureModes.Viability))
            {
                if (!prevSelectedColumns.Contains("AggregationFactor__"))
                {
                    prevSelectedColumns.Add("AggregationFactor__");
                }

                if (!prevSelectedColumns.Contains("DilutionFactor"))
                {
                    prevSelectedColumns.Add("DilutionFactor");
                }

                foreach (var model in SelectedDataViewModels)
                {
                    if (model.AssociatedMeasureResult.MeasureSetup.MeasureMode !=
                        Casy.Models.Enums.MeasureModes.Viability) continue;

                    foreach (var cursor in model.AssociatedMeasureResult.MeasureSetup.Cursors)
                    {
                        if (cursor.IsDeadCellsCursor) continue;

                        if (!prevSelectedColumns.Contains("CountsPercentage__" + cursor.Name))
                        {
                            prevSelectedColumns.Add("CountsPercentage__" + cursor.Name);
                        }

                        if (!prevSelectedColumns.Contains("PeakDiameter__" + cursor.Name))
                        {
                            prevSelectedColumns.Add("PeakDiameter__" + cursor.Name);
                        }

                        if (!prevSelectedColumns.Contains("CountsPerMl__" + cursor.Name))
                        {
                            prevSelectedColumns.Add("CountsPerMl__" + cursor.Name);
                        }
                    }
                }
            }
            else
            {
                prevSelectedColumns.Remove("AggregationFactor__");
                prevSelectedColumns.Remove("DilutionFactor");
            }

            if (_isFreeRangePreset)
            {
                foreach (var model in SelectedDataViewModels)
                {
                    foreach (var cursor in model.AssociatedMeasureResult.MeasureSetup.Cursors)
                    {
                        if (!prevSelectedColumns.Contains("CountsPercentage__" + cursor.Name))
                        {
                            prevSelectedColumns.Add("CountsPercentage__" + cursor.Name);
                        }

                        if (!prevSelectedColumns.Contains("PeakDiameter__" + cursor.Name))
                        {
                            prevSelectedColumns.Add("PeakDiameter__" + cursor.Name);
                        }

                        if (!prevSelectedColumns.Contains("CountsPerMl__" + cursor.Name))
                        {
                            prevSelectedColumns.Add("CountsPerMl__" + cursor.Name);
                        }
                    }
                }
            }

            if(_isUserPreset && _authenticationService.LoggedInUser != null)
            {
                var userSelection = _authenticationService.LoggedInUser.SelectedTableColumns;

                foreach (var selected in userSelection)
                {
                    if(!prevSelectedColumns.Contains(selected))
                    {
                        prevSelectedColumns.Add(selected);
                    }
                }
            }

            foreach (var col in additionalColumns)
            {
                col.IsSelectedRow = prevSelectedColumns.Contains(col.Binding);
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                _additionalColumns.Reset(additionalColumns.OrderBy(ac => ac.CursorName).ThenBy(ac => ac.Order));
                _categories.Reset(categories);
                NotifyOfPropertyChange("Filter");
            });
        }

        public override bool IsActive
        {
            get => base.IsActive;
            set
            {
                base.IsActive = value;
                OnIsActiveChanged();
            }
        }

        protected void OnIsActiveChanged()
        {
            if (!IsActive) return;

            var toRemove = new List<Lazy<MeasureResultModel>>();
            foreach (var viewModelExport in _selectedDataViewModelExports)
            {
                if(viewModelExport.Value.AssociatedMeasureResult == null) continue;
                
                if (_measureResultManager.SelectedMeasureResults.Any(m => m != null && m.MeasureResultId == viewModelExport.Value.AssociatedMeasureResult.MeasureResultId)
                ) continue;

                viewModelExport.Value.AssociatedMeasureResult.PropertyChanged -= OnMeasureResultChanged;
                viewModelExport.Value.AssociatedMeasureResult.MeasureSetup.Cursors.CollectionChanged -=
                    OnCursorsChanged;
                viewModelExport.Value.PropertyChanged -= OnModelPropertyChanged;
                toRemove.Add(viewModelExport);
            }

            var toAdd = new List<Lazy<MeasureResultModel>>();

            lock (((ICollection) _measureResultManager.SelectedMeasureResults).SyncRoot)
            {
                foreach (var measureResult in _measureResultManager.SelectedMeasureResults)
                {
                    if (_selectedDataViewModelExports.Any(viewModel =>
                        viewModel.Value.AssociatedMeasureResult != null &&
                        viewModel.Value.AssociatedMeasureResult.MeasureResultId == measureResult.MeasureResultId))
                        continue;

                    if (measureResult.IsTemporary) continue;

                    measureResult.PropertyChanged += OnMeasureResultChanged;

                    if (measureResult.MeasureSetup != null)
                    {
                        measureResult.MeasureSetup.Cursors.CollectionChanged += OnCursorsChanged;
                    }

                    var modelExport = _compositionFactory.GetExport<MeasureResultModel>();
                    modelExport.Value.PropertyChanged += OnModelPropertyChanged;
                    modelExport.Value.AssociatedMeasureResult = measureResult;
                    toAdd.Insert(0, modelExport);
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (toRemove.Count == 1)
                {
                    SelectedDataViewModels.Remove(toRemove.First().Value);
                }
                else
                {
                    SelectedDataViewModels.RemoveRange(toRemove.Select(l => l.Value));
                }

                foreach (var remove in toRemove)
                {
                    _selectedDataViewModelExports.Remove(remove);
                    remove.Value.Dispose();
                    _compositionFactory.ReleaseExport(remove);
                }

                SelectedDataViewModels.InsertRange(0, toAdd.Select(l => l.Value));
                _selectedDataViewModelExports.AddRange(toAdd);
            });

            InitAnnotationTypes();
            NotifyOfPropertyChange("KnownExperiments");
            NotifyOfPropertyChange("KnownGroups");
        }

        private void OnSelectedMeasureResultsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (IsActive)
            {
                Task.Factory.StartNew(() =>
                {
                    List<Lazy<MeasureResultModel>> toRemove = null;
                    List<Lazy<MeasureResultModel>> toAdd = null;

                    if (e.OldItems != null)
                    {
                        toRemove = new List<Lazy<MeasureResultModel>>();
                        foreach (var oldItem in e.OldItems.OfType<MeasureResult>())
                        {
                            var oldModel = _selectedDataViewModelExports.FirstOrDefault(item =>
                                item.Value.AssociatedMeasureResult.MeasureResultId == oldItem.MeasureResultId);
                            if (oldModel == null) continue;
                            oldItem.PropertyChanged -= OnMeasureResultChanged;

                            if (oldItem.MeasureSetup != null && oldItem.MeasureSetup.Cursors != null)
                            {
                                oldItem.MeasureSetup.Cursors.CollectionChanged -= OnCursorsChanged;
                            }

                            if (oldModel.IsValueCreated)
                            {
                                oldModel.Value.PropertyChanged -= OnModelPropertyChanged;
                            }

                            toRemove.Add(oldModel);
                        }
                    }

                    if (e.NewItems != null)
                    {
                        toAdd = new List<Lazy<MeasureResultModel>>();
                        foreach (var newItem in e.NewItems.OfType<MeasureResult>())
                        {
                            if (!newItem.IsTemporary && _selectedDataViewModelExports.All(vm =>
                                    vm.Value.AssociatedMeasureResult.MeasureResultId != newItem.MeasureResultId))
                            {
                                newItem.PropertyChanged += OnMeasureResultChanged;

                                if (newItem.MeasureSetup != null && newItem.MeasureSetup.Cursors != null)
                                {
                                    newItem.MeasureSetup.Cursors.CollectionChanged += OnCursorsChanged;
                                }

                                var modelExport = _compositionFactory.GetExport<MeasureResultModel>();
                                modelExport.Value.PropertyChanged += OnModelPropertyChanged;
                                modelExport.Value.AssociatedMeasureResult = newItem;
                                toAdd.Insert(0, modelExport);
                            }
                        }
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (toRemove != null)
                        {
                            SelectedDataViewModels.RemoveRange(toRemove.Select(l => l.Value));
                            foreach (var remove in toRemove)
                            {
                                _selectedDataViewModelExports.Remove(remove);
                                remove.Value.Dispose();
                                _compositionFactory.ReleaseExport(remove);
                            }
                        }

                        if (toAdd == null) return;

                        SelectedDataViewModels.InsertRange(0, toAdd.Select(l => l.Value));
                        _selectedDataViewModelExports.AddRange(toAdd);
                    });

                    RefreshData = true;
                    NotifyOfPropertyChange("KnownExperiments");
                    NotifyOfPropertyChange("KnownGroups");
                });
            }
        }

        private void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Experiment":
                    NotifyOfPropertyChange("KnownExperiments");
                    break;
                case "Group":
                    NotifyOfPropertyChange("KnownGroups");
                    break;
            }
        }

        private async void OnDelete()
        {
            var selectedRows = SelectedRows.ToArray();

            if (selectedRows.Length <= 0) return;

            var success = await Task.Factory.StartNew(() =>
            {
                var awaiter = new ManualResetEvent(false);

                ShowMessageBoxDialogWrapper messageBoxWrapper = new ShowMessageBoxDialogWrapper
                {
                    Awaiter = awaiter,
                    Title = "DeleteMesureResult_ConfirmDialog_Title",
                    Message = "DeleteMesureResult_ConfirmDialog_Content",
                    MessageParameter = new[] {string.Join(", ", selectedRows.Select(item => item.Name))}
                };

                _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>().Publish(messageBoxWrapper);

                if (awaiter.WaitOne())
                {
                    return messageBoxWrapper.Result;
                }

                return false;
            });


            if (!success) return;

            _environmentService.SetEnvironmentInfo("IsBusy", true);

            await Task.Factory.StartNew(async () =>
            {
                await _generalMeasureResultManager.DeleteMeasureResults(selectedRows
                    .Select(mrm => mrm.AssociatedMeasureResult).ToList());

                foreach (var measureResultModel in selectedRows)
                {
                    var measureResult = measureResultModel.AssociatedMeasureResult;

                    if (measureResult == null) continue;
                    var oldModelExport = _selectedDataViewModelExports.FirstOrDefault(item =>
                        item.Value.AssociatedMeasureResult.MeasureResultId == measureResult.MeasureResultId);

                    if (oldModelExport == null) continue;
                    measureResult.PropertyChanged -= OnMeasureResultChanged;

                    if (measureResult.MeasureSetup != null)
                    {
                        measureResult.MeasureSetup.Cursors.CollectionChanged -= OnCursorsChanged;
                    }
                    oldModelExport.Value.PropertyChanged -= OnModelPropertyChanged;

                    _selectedDataViewModelExports.Remove(oldModelExport);
                    SelectedDataViewModels.Remove(oldModelExport.Value);

                    _compositionFactory.ReleaseExport(oldModelExport);
                }

                _environmentService.SetEnvironmentInfo("IsBusy", false);
            });
        }

        private void OnExport(TableView tableView)
        {
            if (_databaseStorageService.GetSettings().TryGetValue("LastImportExportPath", out var lastExportDirectorySetting))
            {
                _saveFileDialogService.InitialDirectory = lastExportDirectorySetting.Value;
            }
         
            _saveFileDialogService.Title = _localizationService.GetLocalizedString("SaveTemplateFileDialog_Title");

            _saveFileDialogService.Filter = "Excel-Arbeitsmappe|*.xlsx|CSV (trennzeichen-getrennt)|*.csv";
            var result = _saveFileDialogService.ShowDialog();
            if (!result.HasValue || !result.Value) return;

            var fileInfo = new FileInfo(_saveFileDialogService.FileName);

            try
            {
                switch (fileInfo.Extension.ToLower())
                {
                    case ".xlsx":
                        ExportSettings.DefaultExportType = ExportType.DataAware;
                        var options = new XlsxExportOptionsEx();
                        options.CustomizeCell += options_CustomizeCell;

                        tableView.ExportToXlsx(_saveFileDialogService.FileName, options);
                        break;
                    case ".csv":
                        tableView.ExportToCsv(_saveFileDialogService.FileName);
                        break;
                }
            }
            catch (IOException ioex)
            {
                var awaiter = new ManualResetEvent(false);

                var messageBoxWrapper = new ShowMessageBoxDialogWrapper
                {
                    Awaiter = awaiter,
                    Title = "File Access Error",
                    Message =
                        $"An error occured while accessing file '{_saveFileDialogService.FileName}'. Maybe the file is blocked by another process."
                };

                _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>()
                    .Publish(messageBoxWrapper);
            }

            _databaseStorageService.SaveSetting("LastImportExportPath", fileInfo.DirectoryName);
        }

        private static void options_CustomizeCell(CustomizeCellEventArgs e)
        {
            if (!(e.Value is string stringValue) || !stringValue.Contains("E+")) return;

            if (stringValue.EndsWith("fl"))
            {
                stringValue = stringValue.Replace(" fl", "");
            }
            var doubleValue = double.Parse(stringValue);
            e.Value = doubleValue.ToString(CultureInfo.CurrentCulture);
            e.Handled = true;
        }

        private void OnPrint()
        {
            var renderer = new PdfDocumentRenderer(false);

            var tableDocument =
                new TableDocument(_localizationService, _authenticationService, _documentSettingsManager, _environmentService);

            var columns = _additionalColumns.Where(c => c.IsSelectedRow).ToList();
            renderer.Document = tableDocument.CreateDocument(SelectedRows, columns);

            var fileName = $"Table_{DateTime.Now:yyyy-dd-M--HH-mm-ss}.pdf";

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
                        MessageParameter = new[] {fileName}
                    };

                    _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>()
                        .Publish(messageBoxDialogWrapper);
                    awaiter2.WaitOne();
                });
            }
        }
    }
}
