using DevExpress.Mvvm;
using OLS.Casy.Controller.Api;
using OLS.Casy.Core;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Models;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Core.Api;
using OLS.Casy.Ui.MainControls.Api;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace OLS.Casy.Ui.Core.ViewModels
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(ITreeViewModel))]
    public class MeasureResultTreeViewModel : Base.ViewModelBase, ITreeViewModel, IPartImportsSatisfiedNotification
    {
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly ILocalizationService _localizationService;
        private readonly IMeasureResultStorageService _measureResultStorageService;
        private readonly IMeasureResultManager _measureResultManager;
        private readonly ICompositionFactory _compositionFactory;
        private readonly IBinaryImportExportProvider _binaryImportExportProvider;
        private readonly ICRFImportExportProvider _crfImportExportProvider;
        private readonly ITTImportExportProvider _ttImportExportProvider;
        private readonly Api.IOpenFileDialogService _openFileDialogService;
        private readonly IMeasureController _measureController;
        private readonly IAuthenticationService _authenticationService;
        private readonly IActivationService _activationService;
        private readonly IMeasureResultDataCalculationService _measureResultDataCalculationService;
        private readonly IDatabaseStorageService _databaseStorageService;
        private readonly ITemplateManager _templateManager;
        private readonly IEnvironmentService _environmentService;
        private string _selectedExperiment;
        private string _selectedGroup;
        private string _filterName;

        private bool _isExpandViewCollapsed;
        private GridLength _expandViewHeight;
        private bool _isShowDeleted;

        private Lazy<MeasureResultSubTreeViewModel> _groupLevelTreeViewModelExport;
        private Lazy<MeasureResultSubTreeViewModel> _measureResultLevelTreeViewModelExport;

        [ImportingConstructor]
        public MeasureResultTreeViewModel(
            IEventAggregatorProvider eventAggregatorProvider,
            ILocalizationService localizationService,
            IMeasureResultStorageService measureResultStorageService,
            ISelectedMeasureResultsTreeViewModel selectedMeasureResultsTreeViewModel,
            IMeasureResultManager measureResultManager,
            ICompositionFactory compositionFactory,
            IBinaryImportExportProvider binaryImportExportProvider,
            Api.IOpenFileDialogService openFileDialogService,
            ICRFImportExportProvider crfImportExportProvider,
            ITTImportExportProvider ttImportExportProvider,
            Api.ISaveFileDialogService saveFileDialogService,
            IMeasureController measureController,
            IRawDataExportProvider rawDataExportProvider,
            IAuthenticationService authenticationService,
            IActivationService activationService,
            IMeasureResultDataCalculationService measureResultDataCalculationService,
            IDatabaseStorageService databaseStorageService,
            ITemplateManager templateManager,
            IEnvironmentService environmentService
            )
        {
            _eventAggregatorProvider = eventAggregatorProvider;
            _localizationService = localizationService;
            _measureResultStorageService = measureResultStorageService;
            SelectedMeasureResultsTreeViewModel = selectedMeasureResultsTreeViewModel;
            _measureResultManager = measureResultManager;
            _compositionFactory = compositionFactory;
            _binaryImportExportProvider = binaryImportExportProvider;
            _openFileDialogService = openFileDialogService;
            _crfImportExportProvider = crfImportExportProvider;
            _ttImportExportProvider = ttImportExportProvider;
            _measureController = measureController;
            _authenticationService = authenticationService;
            _activationService = activationService;
            _measureResultDataCalculationService = measureResultDataCalculationService;
            _databaseStorageService = databaseStorageService;
            _templateManager = templateManager;
            _environmentService = environmentService;

            MeasureResultTreeItemViewModels = new SmartCollection<MeasureResultTreeItemViewModel>();

            _expandViewHeight = new GridLength(1, GridUnitType.Star);
        }

        public ICommand NavigateBackCommand => new OmniDelegateCommand<object>(OnBack);

        public ICommand SelectAllGroupsCommand => new OmniDelegateCommand(OnSelectAllGroups);

        public ICommand SelectAllMeasureResultsCommand => new OmniDelegateCommand(OnSelectAllMeasureResults);

        public string NavigateBackButtonText => _localizationService.GetLocalizedString("MeasureResultTreeView_BackButton_DefaultText");

        public SmartCollection<MeasureResultTreeItemViewModel> MeasureResultTreeItemViewModels { get; }

        public string FilterName
        {
            get { return _filterName; }
            set
            {
                if (value != _filterName)
                {
                    this._filterName = value;
                    NotifyOfPropertyChange();
                    UpdateMeasureResultTreeItems();
                }
            }
        }

        public void SetSelectedExperiment(string experiment)
        {
            if(_measureResultLevelTreeViewModelExport != null)
            {
                _eventAggregatorProvider.Instance.GetEvent<RemoveMainControlsOverlayEvent>().Publish(_measureResultLevelTreeViewModelExport.Value);
                _compositionFactory.ReleaseExport(_measureResultLevelTreeViewModelExport);
                _measureResultLevelTreeViewModelExport = null;
            }

            _selectedExperiment = experiment;

            if(_groupLevelTreeViewModelExport == null)
            {
                _groupLevelTreeViewModelExport = _compositionFactory.GetExport<MeasureResultSubTreeViewModel>();
                _groupLevelTreeViewModelExport.Value.MeasureResultTreeItemType = MeasureResultTreeItemType.Group;
                _groupLevelTreeViewModelExport.Value.NavigateBackCommand = NavigateBackCommand;
                _groupLevelTreeViewModelExport.Value.SelectAllCommand = SelectAllGroupsCommand;
            }
            _groupLevelTreeViewModelExport.Value.NavigateBackButtonText = string.IsNullOrEmpty(_selectedExperiment) ? _localizationService.GetLocalizedString("MeasureResultTreeView_GroupNode_NoExperiment") : _selectedExperiment;

            UpdateMeasureResultTreeItems();
            _eventAggregatorProvider.Instance.GetEvent<AddMainControlsOverlayEvent>().Publish(_groupLevelTreeViewModelExport.Value);
        }

        public void SetSelectedGroup(string group)
        {
            _selectedGroup = group;

            if(_measureResultLevelTreeViewModelExport == null)
            { 
                _measureResultLevelTreeViewModelExport = _compositionFactory.GetExport<MeasureResultSubTreeViewModel>();
                _measureResultLevelTreeViewModelExport.Value.MeasureResultTreeItemType = MeasureResultTreeItemType.MeasureResult;
                _measureResultLevelTreeViewModelExport.Value.NavigateBackCommand = NavigateBackCommand;
                _measureResultLevelTreeViewModelExport.Value.SelectAllCommand = SelectAllMeasureResultsCommand;
            }

            _measureResultLevelTreeViewModelExport.Value.NavigateBackButtonText = string.IsNullOrEmpty(_selectedGroup) ? _localizationService.GetLocalizedString("MeasureResultTreeView_GroupNode_NoGroup") : _selectedGroup;
            UpdateMeasureResultTreeItems();

            _eventAggregatorProvider.Instance.GetEvent<AddMainControlsOverlayEvent>().Publish(_measureResultLevelTreeViewModelExport.Value);
        }

        //private static SemaphoreSlim _slowStuffSemaphore = new SemaphoreSlim(1, 1);
        

        public void ToggleSelection(bool isSelected, MeasureResult measureResult)
        {
            Task.Factory.StartNew(async () =>
            {
                if (!isSelected)
                {
                    await this._measureResultManager.RemoveSelectedMeasureResults(new[] { measureResult });
                }
                else
                {
                    if (this._measureResultManager.SelectedMeasureResults.All(mr => mr.MeasureResultId != measureResult.MeasureResultId))
                    {
                        this._eventAggregatorProvider.Instance.GetEvent<NavigateToEvent>().Publish(new NavigationArgs(NavigationCategory.AnalyseGraph)
                        {
                            Parameter = true
                        });
                        await this._measureResultManager.AddSelectedMeasureResults(new[] { measureResult });
                        
                    }
                }

                //_slowStuffSemaphore.Release();
            });
        }


        public ICommand ExpandButtonCommand
        {
            get
            {
                return new OmniDelegateCommand(DoExpand);
            }
        }

        public GridLength ExpandViewHeight
        {
            get { return _expandViewHeight; }
            set
            {
                if (value != _expandViewHeight)
                {
                    _expandViewHeight = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool IsExpandViewCollapsed
        {
            get { return _isExpandViewCollapsed; }
            set
            {
                if (value != _isExpandViewCollapsed)
                {
                    this._isExpandViewCollapsed = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public ICommand RemoveAllFromSelectionCommand
        {
            get { return new OmniDelegateCommand(OnRemoveAllFromSelection); }
        }

        public ISelectedMeasureResultsTreeViewModel SelectedMeasureResultsTreeViewModel { get; }

        public ICommand ImportCommand
        {
            get { return new OmniDelegateCommand(OnImport); }
        }

        public ICommand ExportCommand
        {
            get { return new OmniDelegateCommand(OnExport); }
        }

        public bool CanShowDeleted
        {
            get { return this._activationService.IsModuleEnabled("cfr") && this._authenticationService.LoggedInUser != null && this._authenticationService.LoggedInUser.UserRole.Priority > 2; }
        }

        public bool IsShowDeleted
        {
            get { return _isShowDeleted; }
            set
            {
                if(value != _isShowDeleted)
                {
                    this._isShowDeleted = value;
                    NotifyOfPropertyChange();

                    UpdateMeasureResultTreeItems();
                }
            }
        }

        public void OnImportsSatisfied()
        {
            this._measureResultManager.SelectedMeasureResultsChanged += OnSelectedMeasureResultsChanged;
            this._measureResultStorageService.MeasureResultsChangedEvent += (s, e) => UpdateMeasureResultTreeItems();

            this._eventAggregatorProvider.Instance.GetEvent<MeasureResultStoredEvent>().Subscribe(OnMeasureResultStored);
            this._eventAggregatorProvider.Instance.GetEvent<MeasureResultsDeletedEvent>().Subscribe(OnMeasureResultsDeleted);

            NotifyOfPropertyChange("NavigateBackButtonText");

            this._localizationService.LanguageChanged += OnLanguageChanged;
            UpdateMeasureResultTreeItems();

            this._authenticationService.UserLoggedIn += OnUserLoggedIn;
        }

        private void OnUserLoggedIn(object sender, AuthenticationEventArgs e)
        {
            UpdateMeasureResultTreeItems();
            NotifyOfPropertyChange("CanShowDeleted");
        }

        private void OnLanguageChanged(object sender, LocalizationEventArgs e)
        {
            UpdateMeasureResultTreeItems();
        }

        private void OnBack(object viewModel)
        {
            if(viewModel == this)
            {
                this._selectedGroup = null;
                this._selectedExperiment = null;
                this._eventAggregatorProvider.Instance.GetEvent<RemoveMainControlsOverlayEvent>().Publish(null);
                this._eventAggregatorProvider.Instance.GetEvent<NavigateToEvent>().Publish(new NavigationArgs(NavigationCategory.MeasureResults));
            }
            else if(viewModel == this._groupLevelTreeViewModelExport.Value)
            {
                this._selectedExperiment = null;
                this._selectedGroup = null;

                if (this._measureResultLevelTreeViewModelExport != null)
                {
                    this._eventAggregatorProvider.Instance.GetEvent<RemoveMainControlsOverlayEvent>().Publish(this._measureResultLevelTreeViewModelExport.Value);
                    _compositionFactory.ReleaseExport(this._measureResultLevelTreeViewModelExport);
                }
                 
                if(this._groupLevelTreeViewModelExport != null)
                {
                    this._eventAggregatorProvider.Instance.GetEvent<RemoveMainControlsOverlayEvent>().Publish(this._groupLevelTreeViewModelExport.Value);
                    this._compositionFactory.ReleaseExport(this._groupLevelTreeViewModelExport);
                }
            }
            else if(viewModel == this._measureResultLevelTreeViewModelExport.Value)
            {
                //this._selectedExperiment = null;
                this._selectedGroup = null;

                if (this._measureResultLevelTreeViewModelExport != null)
                {
                    this._eventAggregatorProvider.Instance.GetEvent<RemoveMainControlsOverlayEvent>().Publish(this._measureResultLevelTreeViewModelExport.Value);
                    this._compositionFactory.ReleaseExport(this._measureResultLevelTreeViewModelExport);
                }
            }
            UpdateMeasureResultTreeItems();
        }

        private async void OnSelectAllGroups()
        {
            var groups = _databaseStorageService.GetGroups(_selectedExperiment);

            var toAdd = new List<MeasureResult>();
            foreach (var group in groups)
            {
                var measureResults =
                    _measureResultStorageService.GetMeasureResults(_selectedExperiment, group.Item1);

                foreach (var measureResult in measureResults)
                {
                    if (_measureResultManager.SelectedMeasureResults.Any(mr => mr.MeasureResultId == measureResult.MeasureResultId)) continue;

                    toAdd.Add(measureResult);

                    var viewModel = _measureResultLevelTreeViewModelExport?.Value.MeasureResultTreeItemViewModels.FirstOrDefault(vm => vm.AssociatedObject is MeasureResult && ((MeasureResult)vm.AssociatedObject).MeasureResultId == measureResult.MeasureResultId);
                    if (viewModel != null)
                    {
                        viewModel.IsSelected = true;
                    }
                }
            }

            await _measureResultManager.AddSelectedMeasureResults(toAdd);
            _eventAggregatorProvider.Instance.GetEvent<NavigateToEvent>().Publish(new NavigationArgs(NavigationCategory.AnalyseGraph)
            {
                Parameter = true
            });
        }

        private async void OnSelectAllMeasureResults()
        {
            var measureResults = _databaseStorageService.GetMeasureResults(_selectedExperiment, _selectedGroup, includeDeleted: this.IsShowDeleted).ToList();

            List<MeasureResult> toAdd = new List<MeasureResult>();
            foreach (var measureResult in measureResults)
            {
                if (_measureResultManager.SelectedMeasureResults.Any(mr =>
                    mr.MeasureResultId == measureResult.MeasureResultId)) continue;

                toAdd.Add(measureResult);

                var viewModel = _measureResultLevelTreeViewModelExport?.Value.MeasureResultTreeItemViewModels.FirstOrDefault(vm => vm.AssociatedObject is MeasureResult && ((MeasureResult)vm.AssociatedObject).MeasureResultId == measureResult.MeasureResultId);
                if (viewModel != null)
                {
                    viewModel.IsSelected = true;
                }
            }

            await this._measureResultManager.AddSelectedMeasureResults(toAdd);
            _eventAggregatorProvider.Instance.GetEvent<NavigateToEvent>().Publish(new NavigationArgs(NavigationCategory.AnalyseGraph)
            {
                Parameter = true
            });
        }

        private void DoExpand()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsExpandViewCollapsed = !_isExpandViewCollapsed;

                if (_isExpandViewCollapsed)
                {
                    ExpandViewHeight = new GridLength(0);
                }
                else
                {
                    ExpandViewHeight = new GridLength(1, GridUnitType.Star);
                }
            });
        }

        
        private void OnSelectedMeasureResultsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if(e.OldItems != null)
            {
                foreach (var oldItem in e.OldItems.OfType<MeasureResult>())
                {
                    var viewModel = _measureResultLevelTreeViewModelExport?.Value.MeasureResultTreeItemViewModels.FirstOrDefault(vm => vm.AssociatedObject is MeasureResult && ((MeasureResult)vm.AssociatedObject).MeasureResultId == oldItem.MeasureResultId);
                    if (viewModel != null)
                    {
                        viewModel.IsSelected = false;
                    }
                }
            }

            if (e.NewItems != null)
            {
                foreach (var newItem in e.NewItems.OfType<MeasureResult>())
                {
                    var viewModel = _measureResultLevelTreeViewModelExport?.Value.MeasureResultTreeItemViewModels.FirstOrDefault(vm => vm.AssociatedObject is MeasureResult && ((MeasureResult)vm.AssociatedObject).MeasureResultId == newItem.MeasureResultId);
                    if (viewModel != null)
                    {
                        viewModel.IsSelected = true;
                    }
                }
            }
        }
        

        private void OnMeasureResultsDeleted()
        {
            UpdateMeasureResultTreeItems();
        }
        
        private void UpdateMeasureResultTreeItems()
        {
            if (_authenticationService.LoggedInUser != null)
            {
                Task.Factory.StartNew(() =>
                {
                    var measureResultTreeItemViewModels = new List<MeasureResultTreeItemViewModel>();
                    var groupLevelTreeViewModels = new List<MeasureResultTreeItemViewModel>();
                    var measureResultLevelTreeViewModels = new List<MeasureResultTreeItemViewModel>();

                    //var isSupervisor = _authenticationService.LoggedInUser.UserRole.Priority == 3;
                    //var userId = _authenticationService.LoggedInUser.Id;
                    //var groupIds = _authenticationService.LoggedInUser.UserGroups.Select(g => g.Id);

                    var experiments = _databaseStorageService.GetExperiments(FilterName, IsShowDeleted);

                    foreach (var experiment in experiments)
                    {
                        MeasureResultTreeItemViewModel measureResultTreeItemViewModel;

                        var groupCount = experiment.Item2;
                        var resultCount = experiment.Item3;

                        if (string.IsNullOrEmpty(experiment.Item1) && measureResultTreeItemViewModels.All(x => x.AssociatedObject != null))
                        {
                            measureResultTreeItemViewModel = new MeasureResultTreeItemViewModel(
                                this, MeasureResultTreeItemType.Experiment, _localizationService.GetLocalizedString("MeasureResultTreeView_GroupNode_NoExperiment"), groupCount, resultCount, null);
                        }
                        else
                        {
                            measureResultTreeItemViewModel = new MeasureResultTreeItemViewModel(
                                this, MeasureResultTreeItemType.Experiment, experiment.Item1, groupCount, resultCount, experiment.Item1);
                        }

                        measureResultTreeItemViewModels.Add(measureResultTreeItemViewModel);
                    }

                    if (_groupLevelTreeViewModelExport != null)
                    {
                        var groups = _databaseStorageService.GetGroups(_selectedExperiment, FilterName, IsShowDeleted);

                        foreach (var group in groups)
                        {
                            MeasureResultTreeItemViewModel measureResultTreeItemViewModel;

                            var measureResultCount = group.Item2;

                            if (string.IsNullOrEmpty(group.Item1) && groupLevelTreeViewModels.All(mrtivm => mrtivm.AssociatedObject != null))
                            {
                                measureResultTreeItemViewModel = new MeasureResultTreeItemViewModel(this, MeasureResultTreeItemType.Group, _localizationService.GetLocalizedString("MeasureResultTreeView_GroupNode_NoGroup"), measureResultCount, null)
                                {
                                    AssociatedExperiment = _selectedExperiment
                                };
                            }
                            else
                            {
                                measureResultTreeItemViewModel = new MeasureResultTreeItemViewModel(
                                    this, MeasureResultTreeItemType.Group, group.Item1, measureResultCount, group.Item1);
                            }

                            groupLevelTreeViewModels.Add(measureResultTreeItemViewModel);
                        }
                    }

                    if (_measureResultLevelTreeViewModelExport != null)
                    {
                        var measureResults = _measureResultStorageService.GetMeasureResults(_selectedExperiment, _selectedGroup, FilterName, IsShowDeleted);

                        foreach (var measureResult in measureResults)
                        {
                            var measureResultTreeItemViewModel = new MeasureResultTreeItemViewModel(
                                this, MeasureResultTreeItemType.MeasureResult, measureResult.Name, 0, measureResult);

                            if (_measureResultManager.SelectedMeasureResults.ToList().Any(mr => mr.MeasureResultId == measureResult.MeasureResultId))
                            {
                                measureResultTreeItemViewModel.IsSelected = true;
                            }
                            measureResultLevelTreeViewModels.Add(measureResultTreeItemViewModel);
                        }
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var oldItems = MeasureResultTreeItemViewModels.ToArray();

                        MeasureResultTreeItemViewModels.Reset(measureResultTreeItemViewModels.OrderBy(item => item.ButtonText));

                        foreach (var oldItem in oldItems)
                        {
                            oldItem.Dispose();
                        }

                        if (_groupLevelTreeViewModelExport != null)
                        {
                            oldItems = _groupLevelTreeViewModelExport.Value.MeasureResultTreeItemViewModels.ToArray();
                            _groupLevelTreeViewModelExport.Value.MeasureResultTreeItemViewModels.Reset(groupLevelTreeViewModels.OrderBy(item => item.ButtonText));

                            foreach (var oldItem in oldItems)
                            {
                                oldItem.Dispose();
                            }
                        }

                        if (_measureResultLevelTreeViewModelExport == null) return;

                        oldItems = _measureResultLevelTreeViewModelExport.Value.MeasureResultTreeItemViewModels.ToArray();
                        _measureResultLevelTreeViewModelExport.Value.MeasureResultTreeItemViewModels.Reset(measureResultLevelTreeViewModels.OrderBy(item => item.ButtonText));

                        foreach (var oldItem in oldItems)
                        {
                            oldItem.Dispose();
                        }
                    });
                });
            }
        }

        private void OnMeasureResultStored()
        {
            UpdateMeasureResultTreeItems();
        }

        private void OnImport()
        {
            _openFileDialogService.Title = _localizationService.GetLocalizedString("OpenImportMeasureResultDialog_Title");
            _openFileDialogService.Filter = "CASY Measure Result|*.csy|CRF Files|*.crf|TT Files|*.tt;*.xlsx|Alle Dateien|*.*";

            _databaseStorageService.GetSettings().TryGetValue("LastImportExportPath", out var importPath);

            if (importPath != null && !string.IsNullOrEmpty(importPath.Value))
            {
                this._openFileDialogService.InitialDirectory = importPath.Value;
            }
            
            _openFileDialogService.Multiselect = true;
            var result = _openFileDialogService.ShowDialog();
            if (result.HasValue && result.Value)
            {
                this._eventAggregatorProvider.Instance.GetEvent<NavigateToEvent>().Publish(new NavigationArgs(NavigationCategory.AnalyseGraph)
                {
                    Parameter = true
                });

                Task.Factory.StartNew(async () =>
                {
                    _environmentService.SetEnvironmentInfo("IsBusy", true);

                    bool isSaveAll = false;

                    foreach (var fileName in _openFileDialogService.FileNames)
                    {
                        FileInfo fileInfo = new FileInfo(fileName);

                        switch (fileInfo.Extension.ToLower())
                        {
                            case ".csy":
                                isSaveAll = await this.OpenBinaryFile(fileInfo, isSaveAll);
                                break;
                            case ".crf":
                                isSaveAll = await this.ImportCRFFile(fileInfo, isSaveAll);
                                break;
                            case ".tt":
                                isSaveAll = await this.ImportTTFile(fileInfo, false, isSaveAll);
                                break;
                            case ".xlsx":
                                isSaveAll = await this.ImportTTFile(fileInfo, true, isSaveAll);
                                break;
                        }

                        _databaseStorageService.SaveSetting("LastImportExportPath", fileInfo.DirectoryName);
                    }

                    _environmentService.SetEnvironmentInfo("IsBusy", false);
                });
            }
        }

        private async Task<bool> ImportCRFFile(FileInfo fileInfo, bool isSaveAll)
        {
            var lastColorIndex = _measureController.LastColorIndex;
            var colorName = "ChartColor" + (lastColorIndex % 10 == 0 ? 1 : 1 + (lastColorIndex % 10));

            var measureResult = await _crfImportExportProvider.ImportAsync(fileInfo.FullName, ((SolidColorBrush)Application.Current.Resources[colorName]).Color.ToString());
            measureResult.MeasureResultGuid = Guid.NewGuid();
            measureResult.Name = Path.GetFileNameWithoutExtension(fileInfo.Name);
            measureResult.MeasureSetup.Name = string.Format("{0} Setup", measureResult.Name);

            _measureController.LastColorIndex++;

            //await _measureResultDataCalculationService.UpdateMeasureResultDataAsync(measureResult, null);

            var result = await this._measureResultManager.SaveMeasureResults(new[] { measureResult }, storeAuditTrail: true, isSaveAllAllowed: true, showConfirmationScreen: !isSaveAll);
            if (result != ButtonResult.Cancel)
            {
                await this._measureResultManager.AddSelectedMeasureResults(new[] {measureResult});
                return result == ButtonResult.SaveAll;
            }

            return false;
        }

        private async Task<bool> ImportTTFile(FileInfo fileInfo, bool isXlsx, bool isSaveAll)
        {
            var lastColorIndex = _measureController.LastColorIndex;
            IEnumerable<MeasureResult> measureResults;

            try
            {
                if (isXlsx)
                {
                    measureResults = await _ttImportExportProvider.ImportXlsxAsync(fileInfo.FullName, lastColorIndex);
                }
                else
                {
                    measureResults = await _ttImportExportProvider.ImportAsync(fileInfo.FullName, lastColorIndex);
                }
            }
            catch (FormatException)
            {
                await Task.Factory.StartNew(() =>
                {
                    var awaiter = new ManualResetEvent(false);

                    var messageBoxWrapper = new ShowMessageBoxDialogWrapper()
                    {
                        Awaiter = awaiter,
                        Title = "Invalid file format",
                        Message =
                            $"The imported file '{fileInfo.Name}' seems to contain invalid content and coulnd't be parsed by the system."
                    };

                    _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>().Publish(messageBoxWrapper);

                    if (awaiter.WaitOne())
                    {
                    }
                });
                return false;
            }

            //bool canSaveAll = measureResults.Count() > 1;
            var buttonResult = ButtonResult.None;
            List<MeasureResult> stored = new List<MeasureResult>();

            string lastExp = string.Empty, lastGrp = string.Empty, lastMeasurementName = string.Empty;

            foreach (var measureResult in measureResults)
            {
                var tempName = _measureResultManager.FindMeasurementName(measureResult);

                measureResult.MeasureResultGuid = Guid.NewGuid();
                measureResult.Name = tempName;
                measureResult.MeasureSetup.Name = string.Format("{0} Setup", tempName);

                _measureController.LastColorIndex++;

                //await _measureResultDataCalculationService.UpdateMeasureResultDataAsync(measureResult, null);

                if (buttonResult == ButtonResult.SaveAll)
                {
                    tempName = _measureResultManager.FindMeasurementName(measureResult);
                    measureResult.Name = tempName;
                    measureResult.Experiment = lastExp;
                    measureResult.Group = lastGrp;
                    
                    stored.Add(measureResult);
                }
                else
                {
                    buttonResult = await _measureResultManager.SaveMeasureResults(new[] {measureResult }, defaultExperiment: lastExp, defaultGroup: lastGrp, isSaveAllAllowed: true, showConfirmationScreen: !isSaveAll);
                    lastExp = measureResult.Experiment;
                    lastGrp = measureResult.Group;
                    lastMeasurementName = measureResult.Name;

                    if (buttonResult != ButtonResult.Cancel)
                    {
                        stored.Add(measureResult);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (buttonResult == ButtonResult.Cancel)
            {
                return false;
            }

            ButtonResult result = ButtonResult.Cancel;
            if (buttonResult == ButtonResult.SaveAll)
            {
                result = await this._measureResultManager.SaveMeasureResults(stored, defaultName: lastMeasurementName, defaultExperiment: lastExp, defaultGroup: lastGrp, showConfirmationScreen: false);
            }

            if (result != ButtonResult.Cancel)
            {
                await this._measureResultManager.AddSelectedMeasureResults(stored.ToList());
            }

            return buttonResult == ButtonResult.SaveAll;
        }

        private async Task<bool> OpenBinaryFile(FileInfo fileInfo, bool isSaveAll)
        {
            var isCfrVersion = this._activationService.IsModuleEnabled("cfr");

            var measureResults = (await _binaryImportExportProvider.ImportMeasureResultsAsync(fileInfo.FullName)).ToList();

            foreach (var measureResult in measureResults)
            {
                if(object.ReferenceEquals(measureResult.MeasureSetup, measureResult.OriginalMeasureSetup))
                {
                    var newSetup = new MeasureSetup();
                    _templateManager.CloneSetup(measureResult.MeasureSetup, ref newSetup);
                    measureResult.OriginalMeasureSetup = newSetup;
                }

                if (!isSaveAll)
                {
                    if (measureResult.IsCfr && !isCfrVersion)
                    {
                        var awaiter = new ManualResetEvent(false);

                        ShowMultiButtonMessageBoxDialogWrapper eventWrapper = new ShowMultiButtonMessageBoxDialogWrapper()
                        {
                            Awaiter = awaiter,
                            Message = "ImportingCfrInNonCfrVersionDialog_Content",
                            Title = "ImportingCfrInNonCfrVersionDialog_Title",
                            MessageParameter = new[] { measureResult.Name },
                            SecondButtonUse = ButtonResult.SaveAll,
                            SecondButtonString = "MessageBox_Button_SaveAll"
                        };

                        _eventAggregatorProvider.Instance.GetEvent<ShowMultiButtonMessageBoxEvent>().Publish(eventWrapper);

                        if (awaiter.WaitOne())
                        {
                            if (eventWrapper.Result == ButtonResult.Cancel)
                            {
                                continue;
                            }
                            else if (eventWrapper.Result == ButtonResult.SaveAll)
                            {
                                isSaveAll = true;
                            }

                            measureResult.AuditTrailEntries.Clear();
                        }
                    }
                    else if (isCfrVersion && !measureResult.IsCfr)
                    {
                        var awaiter = new ManualResetEvent(false);

                        ShowMultiButtonMessageBoxDialogWrapper eventWrapper = new ShowMultiButtonMessageBoxDialogWrapper()
                        {
                            Awaiter = awaiter,
                            Message = "ImportingNonCfrInCfrVersionDialog_Content",
                            Title = "ImportingNonCfrInCfrVersionDialog_Title",
                            MessageParameter = new[] { measureResult.Name },
                            SecondButtonUse = ButtonResult.SaveAll,
                            SecondButtonString = "MessageBox_Button_SaveAll"
                        };

                        _eventAggregatorProvider.Instance.GetEvent<ShowMultiButtonMessageBoxEvent>().Publish(eventWrapper);

                        if (awaiter.WaitOne())
                        {
                            if (eventWrapper.Result == ButtonResult.Cancel)
                            {
                                continue;
                            }
                            else if (eventWrapper.Result == ButtonResult.SaveAll)
                            {
                                isSaveAll = true;
                            }

                            measureResult.Origin = "Non CFR CSY";
                            measureResult.IsCfr = true;
                        }
                    }
                    else
                    {
                        isSaveAll = true;
                    }
                }

                if (isSaveAll)
                {
                    if (measureResult.IsCfr && !isCfrVersion)
                    {
                        measureResult.AuditTrailEntries.Clear();
                    }
                    else if (isCfrVersion && !measureResult.IsCfr)
                    {
                        measureResult.Origin = "Non CFR CSY";
                        measureResult.IsCfr = true;
                    }
                }

                string tempName = null;
                //TODO: Hier muss anhand der GUID überprüft werden, ob es ein reimport ist und entsprechend behandelt werden
                // - Zu klären: Parallele Bearbeitung auf zwei Geräten

                var existing = _databaseStorageService.GetMeasureResultByGuid(measureResult.MeasureResultGuid);
                if (existing != null && !existing.IsDeletedResult)
                {
                    var awaiter = new ManualResetEvent(false);

                    ShowMultiButtonMessageBoxDialogWrapper showMessageBoxEventWrapper = new ShowMultiButtonMessageBoxDialogWrapper()
                    {
                        Awaiter = awaiter,
                        Message = "MeasureResultWithGuidExistsDialog_Content",
                        Title = "MeasureResultWithGuidExistsDialog_Title",
                        OkButtonUse = ButtonResult.Replace,
                        FirstButtonUse = ButtonResult.SaveAsCopy,
                        SecondButtonUse = ButtonResult.Cancel
                    };

                    _eventAggregatorProvider.Instance.GetEvent<ShowMultiButtonMessageBoxEvent>().Publish(showMessageBoxEventWrapper);

                    if (awaiter.WaitOne())
                    {
                        if (showMessageBoxEventWrapper.Result != ButtonResult.Cancel)
                        {
                            if (existing.MeasureSetup == null)
                            {
                                _databaseStorageService.LoadDisplayData(existing);
                            }

                            if (showMessageBoxEventWrapper.Result == ButtonResult.Replace)
                            {
                                await this._measureResultManager.RemoveSelectedMeasureResults(new[] { existing });
                                //_measureResultManager.SelectedMeasureResults.Remove(existing);
                                _measureResultStorageService.DeleteMeasureResults(new[] { existing });
                                //measureResult.MeasureResultId = existing.MeasureResultId;
                            }
                            else
                            {
                                tempName = measureResult.Name;

                                if (_measureResultStorageService.MeasureResultExists(tempName, measureResult.Experiment, measureResult.Group))
                                {
                                    int count = 1;
                                    var measurementName = string.Format("{0}_{1}", tempName, count.ToString());
                                    while (_measureResultStorageService.MeasureResultExists(measurementName, measureResult.Experiment, measureResult.Group))
                                    {
                                        count++;
                                        measurementName = string.Format("{0}_{1}", tempName, count.ToString());
                                    }

                                    tempName = measurementName;
                                }

                                measureResult.MeasureResultGuid = Guid.NewGuid();
                            }

                            measureResult.MeasureResultId = -1;
                            measureResult.IsVisible = true;

                            foreach (var annotation in measureResult.MeasureResultAnnotations)
                            {
                                annotation.MeasureResultAnnotationId = -1;
                            }

                            foreach (var auditTrailEntry in measureResult.AuditTrailEntries)
                            {
                                auditTrailEntry.AuditTrailEntryId = -1;
                            }

                            foreach (var measureResultData in measureResult.MeasureResultDatas)
                            {
                                measureResultData.MeasureResultDataId = -1;
                            }

                            measureResult.MeasureSetup.MeasureSetupId = -1;
                            foreach (var cursor in measureResult.MeasureSetup.Cursors)
                            {
                                cursor.CursorId = -1;
                                cursor.IsVisible = true;
                            }

                            foreach (var deviationControlItem in measureResult.MeasureSetup.DeviationControlItems)
                            {
                                deviationControlItem.DeviationControlItemId = -1;
                            }

                            measureResult.OriginalMeasureSetup.MeasureSetupId = -1;
                            foreach (var cursor in measureResult.OriginalMeasureSetup.Cursors)
                            {
                                cursor.CursorId = -1;
                                cursor.IsVisible = true;
                            }

                            foreach (var deviationControlItem in measureResult.OriginalMeasureSetup.DeviationControlItems)
                            {
                                deviationControlItem.DeviationControlItemId = -1;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    measureResult.MeasureResultId = -1;
                    measureResult.IsVisible = true;

                    foreach (var annotation in measureResult.MeasureResultAnnotations)
                    {
                        annotation.MeasureResultAnnotationId = -1;
                    }

                    foreach (var auditTrailEntry in measureResult.AuditTrailEntries)
                    {
                        auditTrailEntry.AuditTrailEntryId = -1;
                    }

                    foreach (var measureResultData in measureResult.MeasureResultDatas)
                    {
                        measureResultData.MeasureResultDataId = -1;
                    }

                    measureResult.MeasureSetup.MeasureSetupId = -1;
                    foreach (var cursor in measureResult.MeasureSetup.Cursors)
                    {
                        cursor.CursorId = -1;
                        cursor.IsVisible = true;
                    }

                    foreach (var deviationControlItem in measureResult.MeasureSetup.DeviationControlItems)
                    {
                        deviationControlItem.DeviationControlItemId = -1;
                    }

                    measureResult.OriginalMeasureSetup.MeasureSetupId = -1;
                    foreach (var cursor in measureResult.OriginalMeasureSetup.Cursors)
                    {
                        cursor.CursorId = -1;
                        cursor.IsVisible = true;
                    }

                    foreach (var deviationControlItem in measureResult.OriginalMeasureSetup.DeviationControlItems)
                    {
                        deviationControlItem.DeviationControlItemId = -1;
                    }
                }

                measureResult.MeasureSetup.CalcSmoothedDiametersAndVolumeMapping();

                if (!string.IsNullOrEmpty(tempName))
                {
                    measureResult.Name = tempName;
                }

                //await _measureResultDataCalculationService.UpdateMeasureResultDataAsync(measureResult, null);
                //this._measureResultManager.SelectedMeasureResults.Add(measureResult);
            }

            this._measureResultStorageService.StoreMeasureResults(measureResults, keepAuditTrail: true);

            await this._measureResultManager.AddSelectedMeasureResults(measureResults);

            this._eventAggregatorProvider.Instance.GetEvent<MeasureResultStoredEvent>().Publish();

            return true;
        }

        private void OnExport()
        {
            Task.Factory.StartNew(() =>
            {
                var awaiter = new ManualResetEvent(false);
                var viewModelExport = this._compositionFactory.GetExport<IExportDialogModel>();
                var viewModel = viewModelExport.Value;

                ShowCustomDialogWrapper wrapper = new ShowCustomDialogWrapper()
                {
                    Awaiter = awaiter,
                    DataContext = viewModel,
                    DialogType = typeof(IExportDialog)
                };

                this._eventAggregatorProvider.Instance.GetEvent<ShowCustomDialogEvent>().Publish(wrapper);

                awaiter.WaitOne();

                this._compositionFactory.ReleaseExport(viewModelExport);
            });
            /*
            IEnumerable<MeasureResult> _measureResultToSave = _measureResultManager.SelectedMeasureResults.ToArray();

            if (_measureResultToSave != null)
            {
                _saveFileDialogService.Title = _localizationService.GetLocalizedString("ExportMeasurementxDialog_Title");
                if(!string.IsNullOrEmpty(this._lastExportPath))
                {
                    this._saveFileDialogService.InitialDirectory = this._lastExportPath;
                }
                this._saveFileDialogService.Filter = "CASY Measure Result|*.csy|CASY RAW Data|*.raw";
                var result = _saveFileDialogService.ShowDialog();
                if (result.HasValue && result.Value)
                {
                    FileInfo fileInfo = new FileInfo(this._saveFileDialogService.FileName);

                    switch (fileInfo.Extension.ToLower())
                    {
                        case ".csy":
                            _binaryImportExportProvider.ExportMeasureResultsAsync(_measureResultToSave, fileInfo.FullName);
                            break;
                        case ".raw":
                            _rawDataExportProvider.ExportMeasureResultsAsync(_measureResultToSave, fileInfo.FullName);
                            break;
                    }
                    this._lastExportPath = fileInfo.DirectoryName;
                }
            }
            */
        }

        private void OnRemoveAllFromSelection()
        {
            this.SelectedMeasureResultsTreeViewModel.RemoveAllFromSelection();
        }
    }
}
