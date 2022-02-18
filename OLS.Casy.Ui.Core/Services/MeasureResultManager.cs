using OLS.Casy.Controller.Api;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Logging.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Models;
using OLS.Casy.Models.Enums;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Core.Api;
using OLS.Casy.Ui.Core.View;
using OLS.Casy.Ui.Core.ViewModels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using Cursor = OLS.Casy.Models.Cursor;
using Timer = System.Timers.Timer;

namespace OLS.Casy.Ui.Core.Services
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IMeasureResultManager))]
    public class MeasureResultManager : IMeasureResultManager, IPartImportsSatisfiedNotification
    {
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly ICompositionFactory _compositionFactory;
        private readonly IUIProjectManager _uiProject;
        private readonly IDatabaseStorageService _databaseStorageService;
        private readonly IMeasureResultStorageService _measureResultStorageService;
        private readonly IMeasureResultDataCalculationService _measureResultDataCalculationService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IEnvironmentService _environmentService;
        private readonly ILogger _logger;
        private MeasureResult _meanMeasureResult;

        private ConcurrentDictionary<string, List<string>> _experimentsGroupsMappings;
        private readonly ConcurrentDictionary<string, List<string>> _experimentsGroupsDeletedMappings;

        private readonly Timer _raiseEventTimer;
        private readonly Timer _updateTimer;
        private Task _updateTask;
        private volatile bool _doUpdate;
        private MeasureSetup _overlayMeasureSetup;

        private readonly ConcurrentDictionary<MeasureSetup, Timer> _rangeModificationTimers;
        
        private readonly List<MeasureResult> _added = new List<MeasureResult>();
        private readonly List<MeasureResult> _removed = new List<MeasureResult>();
        
        readonly object _lock = new object();

        [ImportingConstructor]
        public MeasureResultManager(IEventAggregatorProvider eventAggregatorProvider,
            IUIProjectManager uiProject,
            IDatabaseStorageService databaseStorageService,
            IMeasureResultStorageService measureResultStorageService,
            IMeasureResultDataCalculationService measureResultDataCalculationService,
            ICompositionFactory compositionFactory,
            IAuthenticationService authenticationService,
            IEnvironmentService environmentService,
            ILogger logger)
        {
            _eventAggregatorProvider = eventAggregatorProvider;
            _uiProject = uiProject;
            _databaseStorageService = databaseStorageService;
            _measureResultStorageService = measureResultStorageService;
            _measureResultDataCalculationService = measureResultDataCalculationService;
            _compositionFactory = compositionFactory;
            _authenticationService = authenticationService;
            _environmentService = environmentService;
            _logger = logger;

            SelectedMeasureResults = new List<MeasureResult>();

            _updateTimer = new Timer {Interval = TimeSpan.FromMilliseconds(500).TotalMilliseconds};
            _updateTimer.Elapsed += OnUpdateTimerElapsed;

            _raiseEventTimer = new Timer {Interval = TimeSpan.FromMilliseconds(500).TotalMilliseconds};
            _raiseEventTimer.Elapsed += OnRaiseEventTimerElapsed;

            _rangeModificationTimers = new ConcurrentDictionary<MeasureSetup, Timer>();
            _experimentsGroupsMappings = new ConcurrentDictionary<string, List<string>>();
            _experimentsGroupsDeletedMappings = new ConcurrentDictionary<string, List<string>>();
        }

        public IEnumerable<string> GetExperiments(bool includeDeleted = false)
        {
            return includeDeleted ? 
                _experimentsGroupsDeletedMappings.Keys.AsEnumerable() :
                    _experimentsGroupsMappings.Keys.AsEnumerable();
        }

        public IEnumerable<string> GetGroups(string experiment, bool includeDeleted = false)
        {
            List<string> result;

            if (includeDeleted)
            {
                _experimentsGroupsDeletedMappings.TryGetValue(experiment ?? string.Empty, out result);
            }
            else
            {
                _experimentsGroupsMappings.TryGetValue(experiment ?? string.Empty, out result);
            }

            return result.AsEnumerable();
        }

        public void StartRangeModificationTimer(MeasureSetup measureSetup)
        {
            lock (_rangeModificationTimers)
            {
                if (!_rangeModificationTimers.TryGetValue(measureSetup, out var timer))
                {
                    timer = new Timer { Interval = TimeSpan.FromMilliseconds(500).TotalMilliseconds };
                    timer.Elapsed += (s, e) => { OnRangeModificationTimerElapsed(timer, measureSetup); };
                    _rangeModificationTimers.TryAdd(measureSetup, timer);
                }
                timer.Start();
            }
        }

        public void StopRangeModificationTimer(MeasureSetup measureSetup)
        {
            lock (_rangeModificationTimers)
            {
                if (_rangeModificationTimers.TryGetValue(measureSetup, out var timer))
                {
                    timer.Stop();
                }
            }
        }

        public IList<MeasureResult> SelectedMeasureResults { get; }

        public MeasureResult MeanMeasureResult
        {
            get => _meanMeasureResult;
            set
            {
                if(_meanMeasureResult != null)
                {
                    _meanMeasureResult.MeasureSetup.PropertyChanged -= OnMeasureSetupChanged;
                    _meanMeasureResult.MeasureSetup.Cursors.CollectionChanged -= OnCursorsChanged;

                    foreach (var cursor in _meanMeasureResult.MeasureSetup.Cursors)
                    {
                        cursor.PropertyChanged -= OnCursorChanged;
                    }
                }

                _meanMeasureResult = value;

                if (_meanMeasureResult?.MeasureSetup != null)
                {
                    _meanMeasureResult.MeasureSetup.PropertyChanged += OnMeasureSetupChanged;
                    _meanMeasureResult.MeasureSetup.Cursors.CollectionChanged += OnCursorsChanged;

                    foreach (var cursor in _meanMeasureResult.MeasureSetup.Cursors)
                    {
                        cursor.PropertyChanged += OnCursorChanged;
                    }
                }

                _updateTimer.Stop();
                _updateTimer.Start();
            }
        }

        public MeasureSetup OverlayMeasureSetup
        {
            get => _overlayMeasureSetup;
            set
            {
                if (_overlayMeasureSetup != null)
                {
                    _overlayMeasureSetup.PropertyChanged -= OnMeasureSetupChanged;
                    _overlayMeasureSetup.Cursors.CollectionChanged -= OnCursorsChanged;

                    foreach (var cursor in _overlayMeasureSetup.Cursors)
                    {
                        cursor.PropertyChanged -= OnCursorChanged;
                    }
                }

                _overlayMeasureSetup = value;

                if (_overlayMeasureSetup != null)
                {
                    _overlayMeasureSetup.PropertyChanged += OnMeasureSetupChanged;
                    _overlayMeasureSetup.Cursors.CollectionChanged += OnCursorsChanged;

                    foreach (var cursor in _overlayMeasureSetup.Cursors)
                    {
                        cursor.PropertyChanged += OnCursorChanged;
                    }
                }

                _updateTimer.Stop();
                _updateTimer.Start();
            }
        }

        public async Task<ButtonResult> SaveMeasureResults(IEnumerable<MeasureResult> measureResults, string defaultName = null, string defaultExperiment = null, string defaultGroup = null, bool cloneSetup = false, bool isSaveAllAllowed = false, bool showConfirmationScreen = true, bool storeAuditTrail = false, bool keepAuditTrail = false)
        {
            return await Task.Run(() =>
            {
                var result = ButtonResult.None;

                var enumerable = measureResults as MeasureResult[] ?? measureResults.ToArray();
                foreach (var measureResult in enumerable)
                {
                    if (measureResult == null) return result;
                    var success = !showConfirmationScreen;

                    if (showConfirmationScreen)
                    {
                        var awaiter = new System.Threading.ManualResetEvent(false);

                        var viewModelExport = _compositionFactory.GetExport<SaveMeasurementDialogModel>();
                        var viewModel = viewModelExport.Value;

                        if (measureResult.IsTemporary)
                        {
                            viewModel.OriginalName = string.IsNullOrEmpty(measureResult.Name) ? null : measureResult.Name;
                            viewModel.OriginalGroup = string.IsNullOrEmpty(measureResult.Group) ? null : measureResult.Group;
                            viewModel.OriginalExperiment = string.IsNullOrEmpty(measureResult.Experiment) ? null : measureResult.Experiment;
                            viewModel.IsCommentRequired = measureResult.MeasureSetup.IsAutoComment;
                            viewModel.IsDeleted = measureResult.IsDeletedResult;
                            viewModel.SelectedExperiment = !string.IsNullOrEmpty(defaultExperiment)
                                ? defaultExperiment
                                : measureResult.Experiment;
                            viewModel.SelectedGroup =
                                !string.IsNullOrEmpty(defaultGroup) ? defaultGroup : measureResult.Group;
                            viewModel.MeasureResultName = string.IsNullOrEmpty(measureResult.Name) ? defaultName : measureResult.Name;
                            viewModel.IsSaveAll = isSaveAllAllowed;
                        }
                        else
                        {
                            viewModel.OriginalName = measureResult.Name;
                            viewModel.OriginalGroup = measureResult.Group;
                            viewModel.OriginalExperiment = measureResult.Experiment;
                            viewModel.IsDeleted = measureResult.IsDeletedResult;
                            viewModel.IsCommentRequired = measureResult.MeasureSetup.IsAutoComment;
                            viewModel.Comment = measureResult.Comment;
                            viewModel.SelectedExperiment = measureResult.Experiment;
                            viewModel.SelectedGroup = measureResult.Group;
                            viewModel.MeasureResultName = measureResult.Name;
                            viewModel.IsSaveAll = isSaveAllAllowed;
                        }

                        var wrapper = new ShowCustomDialogWrapper()
                        {
                            Awaiter = awaiter,
                            Title = "SaveMeasureResultDialog_Title",
                            DataContext = viewModel,
                            DialogType = typeof(SaveMeasurementDialog)
                        };

                        _eventAggregatorProvider.Instance.GetEvent<ShowCustomDialogEvent>().Publish(wrapper);

                        if (awaiter.WaitOne())
                        {
                            result = viewModel.ButtonResult;
                            if (result != ButtonResult.Cancel && !string.IsNullOrEmpty(viewModel.MeasureResultName))
                            {
                                success = true;
                                measureResult.IsTemporary = false;
                                measureResult.Name = viewModel.MeasureResultName;
                                measureResult.Comment = viewModel.Comment;
                                measureResult.Experiment = viewModel.SelectedExperiment;
                                measureResult.Group = viewModel.SelectedGroup;
                            }
                        }
                        else
                        {
                            success = true;
                        }

                        _compositionFactory.ReleaseExport(viewModelExport);
                    }

                    if (!success) return result;
                }

                _measureResultStorageService.StoreMeasureResults(enumerable, cloneSetup, keepAuditTrail: keepAuditTrail);

                foreach (var measureResult in measureResults)
                {
                    if (storeAuditTrail)
                    {
                        foreach (var auditTrailEntry in measureResult.AuditTrailEntries)
                        {
                            _databaseStorageService.SaveAuditTrailEntry(auditTrailEntry);
                        }
                    }

                    _logger.Info(LogCategory.Measurement, $"Measurement result '{measureResult.Name}' has been changed and stored.");
                }

                _uiProject.Clear();
                _eventAggregatorProvider.Instance.GetEvent<MeasureResultStoredEvent>().Publish();
                return result;
            });
        }

        public bool MeasureResultHasChanges(MeasureResult measureResult)
        {
            if (measureResult == null || measureResult.IsReadOnly) return false;
            
            var hasChanges = measureResult.IsTemporary;

            if (hasChanges) return true;
            
            hasChanges = _uiProject.IsModified(measureResult);

            if (hasChanges) return true;

            if (measureResult.MeasureSetup == null) return false;

            hasChanges |= _uiProject.IsModified(measureResult.MeasureSetup);

            foreach (var cursor in measureResult.MeasureSetup.Cursors)
            {
                hasChanges |= _uiProject.IsModified(cursor);
            }
            return hasChanges;
        }

        public async Task<bool> DeleteMeasureResults(IList<MeasureResult> measureResults, bool isShutDown = false)
        {
            return await Task.Run(() =>
            {
                _measureResultStorageService.DeleteMeasureResults(measureResults);

                foreach (var measureResult in measureResults)
                {
                    _logger.Info(LogCategory.Measurement, $"Measurement result '{measureResult.Name}' has been deleted.");
                }

                if (isShutDown) return true;
                Task.Run(async () => await RemoveSelectedMeasureResults(measureResults));
                _eventAggregatorProvider.Instance.GetEvent<MeasureResultsDeletedEvent>().Publish();
                return true;
            });
        }

        public async Task<ButtonResult> SaveChangedMeasureResults(IEnumerable<MeasureResult> measureResults = null, bool allowAcceptWithoutChanges = true, bool isShutDown = false)
        {
             var measureResultsInternal = measureResults == null ? SelectedMeasureResults.ToArray() : measureResults.ToArray();
            
            var result = await CheckForTemporaryMeasureResults(measureResultsInternal, allowAcceptWithoutChanges, isShutDown);

            if (result != ButtonResult.Save) return result;
            
            var toStore = new List<MeasureResult>();
            if (!measureResultsInternal.Any())
            {
                foreach (var modifiedObject in _uiProject.GetModifiedObjects())
                {
                    var measureResult = modifiedObject as MeasureResult;
                    if (measureResult == null)
                    {
                        var measureSetup = modifiedObject as MeasureSetup;

                        if (measureSetup == null)
                        {
                            if (modifiedObject is Cursor cursor)
                            {
                                measureSetup = cursor.MeasureSetup;
                            }
                        }

                        if (measureSetup != null)
                        {
                            measureResult = measureSetup.MeasureResult;
                        }
                    }

                    if (measureResult == null) continue;
                    
                    if (measureResult.IsTemporary)
                    {
                        toStore.Add(measureResult);
                        
                    }
                    else if (!measureResult.IsReadOnly)
                    {
                        var hasChanged = _uiProject.IsModified(measureResult);

                        if (!hasChanged)
                        {
                            hasChanged |= _uiProject.IsModified(measureResult.MeasureSetup);

                            foreach (var cursor in measureResult.MeasureSetup.Cursors)
                            {
                                hasChanged |= _uiProject.IsModified(cursor);
                            }
                        }

                        if (hasChanged)
                        {
                            toStore.Add(measureResult);
                        }
                    }

                }
            }
            else
            {
                toStore.AddRange(measureResultsInternal.Where(MeasureResultHasChanges));
            }
                
            if(toStore.Count > 0)
            {
                result = await Task.Factory.StartNew(async () =>
                {
                    var result2 = ButtonResult.Cancel;
                    var canSaveAll = toStore.Count > 1 && !toStore.Any(mr => mr.IsTemporary);

                    var awaiter = new System.Threading.ManualResetEvent(false);

                    var multiButtonMessageBoxWrapper = new ShowMultiButtonMessageBoxDialogWrapper()
                    {
                        Awaiter = awaiter,
                        Title = "ConfirmSaveMeasureResultsOnCloseDialog_Title",
                        Message = "ConfirmSaveMeasureResultsOnCloseDialog_Content",
                        MessageParameter = new[] { string.Join(", ", toStore.Select(mr => mr.Name)) },
                        FirstButtonUse = ButtonResult.Accept,
                        OkButtonUse = ButtonResult.Save,
                        FirstButtonString = "MessageBox_Button_ContinueWithoutSave"
                    };

                    if (canSaveAll)
                    {
                        multiButtonMessageBoxWrapper.SecondButtonUse = ButtonResult.SaveAll;
                        multiButtonMessageBoxWrapper.SecondButtonString = "MessageBox_Button_SaveAll";
                    }
                    _eventAggregatorProvider.Instance.GetEvent<ShowMultiButtonMessageBoxEvent>().Publish(multiButtonMessageBoxWrapper);

                    if (awaiter.WaitOne())
                    {
                        result2 = multiButtonMessageBoxWrapper.Result;
                    }

                    if (result2 == ButtonResult.Cancel)
                    {
                        return result2;
                    }

                    _environmentService.SetEnvironmentInfo("IsBusy", true);

                    toStore = toStore.Distinct().ToList();
                    foreach (var measureResult in toStore)
                    {
                        if (measureResult == null) continue;
                        
                        switch (result2)
                        {
                            case ButtonResult.Accept:
                                if (!isShutDown)
                                {
                                    _uiProject.UndoAll();
                                    _eventAggregatorProvider.Instance.GetEvent<MeasureResultStoredEvent>().Publish();
                                }
                                break;
                            case ButtonResult.Save:
                                var result3 = await SaveMeasureResults(new[] { measureResult });
                                if(result3 != ButtonResult.Ok)
                                {
                                    return ButtonResult.Cancel;
                                }

                                if (!isShutDown)
                                {
                                    _uiProject.Clear();
                                    _eventAggregatorProvider.Instance.GetEvent<MeasureResultStoredEvent>().Publish();
                                }
                                break;
                            case ButtonResult.SaveAll:
                                await SaveMeasureResults(toStore, showConfirmationScreen: false);

                                if (!isShutDown)
                                {
                                    _uiProject.Clear();
                                    _eventAggregatorProvider.Instance.GetEvent<MeasureResultStoredEvent>().Publish();
                                }
                                break;
                        }

                        if (result2 == ButtonResult.SaveAll)
                        {
                            break;
                        }
                    }

                    _environmentService.SetEnvironmentInfo("IsBusy", false);

                    return result2;
                }).Result;
            }
            return result;
        }

        public event EventHandler<MeasureResultDataChangedEventArgs> SelectedMeasureResultDataChangedEvent;
        public event EventHandler ExperimentsGroupsMappingsChangedEvent;

        private async Task<ButtonResult> CheckForTemporaryMeasureResults(IEnumerable<MeasureResult> measureResults = null, bool allowAcceptWithoutChanges = true, bool isShutDown = false)
        {
            //return await Task.Factory.StartNew( () =>
            //{
                var success = ButtonResult.Save;
                var toRemove = new List<MeasureResult>();

                if(measureResults == null)
                {
                    measureResults = SelectedMeasureResults.ToArray();
                }

                foreach (var measureResult in measureResults)
                {
                    if (measureResult != null && measureResult.IsTemporary && !measureResult.IsReadOnly)
                    {
                        success = ButtonResult.Cancel;
                        var awaiter = new System.Threading.ManualResetEvent(false);

                        var multiButtonMessageBoxWrapper = new ShowMultiButtonMessageBoxDialogWrapper()
                        {
                            Awaiter = awaiter,
                            Title = "MessageBox_TemporaryMeasureResultNotSaved_Title",
                            Message = "MessageBox_TemporaryMeasureResultNotSaved_Content",
                            MessageParameter = new[] { measureResult.Name },
                            SecondButtonUse = ButtonResult.Cancel,
                            OkButtonUse = ButtonResult.Save
                        };

                        if(allowAcceptWithoutChanges)
                        {
                            multiButtonMessageBoxWrapper.FirstButtonString = "MessageBox_Button_ContinueWithoutSave";
                            multiButtonMessageBoxWrapper.FirstButtonUse = ButtonResult.Accept;
                        }

                        _eventAggregatorProvider.Instance.GetEvent<ShowMultiButtonMessageBoxEvent>().Publish(multiButtonMessageBoxWrapper);

                        if (awaiter.WaitOne())
                        {
                            success = multiButtonMessageBoxWrapper.Result;
                            switch (success)
                            {
                                case ButtonResult.Accept:
                                    _uiProject.Clear();

                                    toRemove.Add(measureResult);                                    
                                    await Task.Run(async () => await DeleteMeasureResults(new[] { measureResult }, isShutDown));
                                    break;
                                case ButtonResult.Save:
                                    await Task.Run(async () => await SaveMeasureResults(new[] {measureResult}));
                                    _uiProject.Clear();
                                    break;
                            }
                        }
                    }
                    if(success == ButtonResult.Cancel)
                    {
                        break;
                    }
                }

                if (isShutDown) return success;
                
                await Task.Run(async () => await RemoveSelectedMeasureResults(toRemove));

                return success;
            //});
        }

        public void OnImportsSatisfied()
        {
            _eventAggregatorProvider.Instance.GetEvent<ActiveNavigationCategoryChangedEvent>().Subscribe(OnActiveNavigationCategoryChanged);
            _eventAggregatorProvider.Instance.GetEvent<KeyDownEvent>().Subscribe(OnKeyDown);

            _eventAggregatorProvider.Instance.GetEvent<MeasureResultStoredEvent>().Subscribe(UpdateExperimentGroupMappings);
            _authenticationService.UserLoggedIn += OnUserLoggedIn;

            UpdateExperimentGroupMappings();
        }

        private async void OnKeyDown(object e)
        {
            KeyEventArgs keyEventArgs = e as KeyEventArgs;
            if (keyEventArgs.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                var result = await Task.Run(async () => await SaveChangedMeasureResults());
            }
        }

        private async void OnUserLoggedIn(object sender, AuthenticationEventArgs e)
        {
            if (_authenticationService.LoggedInUser != null)
            {
                var tempResults = _databaseStorageService.GetTemporaryMeasureResults(
                    _authenticationService.LoggedInUser.UserRole.Priority <= 2
                        ? _authenticationService.LoggedInUser
                        : null);
                if (tempResults.Any())
                {
                    await AddSelectedMeasureResults(tempResults.ToArray());
                    _eventAggregatorProvider.Instance.GetEvent<NavigateToEvent>()
                        .Publish(new NavigationArgs(NavigationCategory.AnalyseGraph));

                    await Task.Factory.StartNew(() =>
                    {
                        var awaiter = new ManualResetEvent(false);

                        var showMessageBoxEventWrapper = new ShowMessageBoxDialogWrapper
                        {
                            Awaiter = awaiter,
                            Message = "TemporaryMeasurementsFound_Content",
                            Title = "TemporaryMeasurementsFound_Title"
                        };

                        _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>()
                            .Publish(showMessageBoxEventWrapper);

                        if (!awaiter.WaitOne()) return;
                    }).ConfigureAwait(false);
                    return;
                }

                if (!e.DiffersFromLastUser) return;

                OverlayMeasureSetup = null;
                MeanMeasureResult = null;
                var toRemove = SelectedMeasureResults.ToArray();

                await RemoveSelectedMeasureResults(toRemove);

                _eventAggregatorProvider.Instance.GetEvent<NavigateToEvent>()
                    .Publish(new NavigationArgs(NavigationCategory.Dashboard));
            }
        }

        private void OnActiveNavigationCategoryChanged(object argument)
        {
            var navigationCategory = (NavigationCategory)argument;
            Task.Run(async () =>
            {
                var result = await SaveChangedMeasureResults();
                _eventAggregatorProvider.Instance.GetEvent<NavigateToEvent>().Publish(result == ButtonResult.Cancel
                    ? new NavigationArgs(NavigationCategory.Previous)
                    : new NavigationArgs(navigationCategory));
            });
        }

        public double GetMinMeasureLimit(int capillarySize)
        {
            switch (capillarySize)
            {
                case 45:
                    return 0.5;
                case 60:
                    return 1.0;
                case 150:
                    return 3.0;
            }
            return 0;
        }

        private void OnRaiseEventTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _raiseEventTimer.Stop();

            if (SelectedMeasureResultsChanged == null) return;
            
            if(_removed.Count != 0 && _added.Count != 0)
            {
                foreach (var @delegate in SelectedMeasureResultsChanged.GetInvocationList())
                {
                    var receiver = (NotifyCollectionChangedEventHandler) @delegate;
                    receiver.BeginInvoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, _added.ToList(), _removed.ToList()), null, null);
                }
                _removed.Clear();
                _added.Clear();
            }
            else
            if (_removed.Count != 0)
            {
                foreach (var @delegate in SelectedMeasureResultsChanged.GetInvocationList())
                {
                    var receiver = (NotifyCollectionChangedEventHandler) @delegate;
                    receiver.BeginInvoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, _removed.ToList()), null, null);
                }
                _removed.Clear();
            }
            else
            if (_added.Count != 0)
            {
                foreach (var @delegate in SelectedMeasureResultsChanged.GetInvocationList())
                {
                    var receiver = (NotifyCollectionChangedEventHandler) @delegate;
                    receiver.BeginInvoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, _added.ToList()), null, null);
                }
                _added.Clear();
            }

            //_environmentService.SetEnvironmentInfo("IsBusy", false);
        }


        public event NotifyCollectionChangedEventHandler SelectedMeasureResultsChanged;

        public async Task ReplaceSelectedMeasureResult(MeasureResult oldMeasureResult, MeasureResult newMeasureResult)
        {
            await Task.Run(() =>
            {
                if (oldMeasureResult == null || newMeasureResult == null) return;

                _environmentService.SetEnvironmentInfo("IsBusy", true);

                var diplayOrder = oldMeasureResult.DisplayOrder;

                oldMeasureResult.MeasureResultDatas.CollectionChanged -= OnMeasureResultDatasChanged;
                if (oldMeasureResult.MeasureSetup != null)
                {
                    oldMeasureResult.MeasureSetup.PropertyChanged -= OnMeasureSetupChanged;
                    oldMeasureResult.MeasureSetup.Cursors.CollectionChanged -= OnCursorsChanged;

                    foreach (var cursor in oldMeasureResult.MeasureSetup.Cursors)
                    {
                        cursor.PropertyChanged -= OnCursorChanged;
                    }
                }

                SelectedMeasureResults.Remove(oldMeasureResult);
                if (oldMeasureResult.MeasureSetup != null)
                {
                    lock (_lock)
                    {
                        _rangeModificationTimers.TryRemove(oldMeasureResult.MeasureSetup, out _);
                    }
                }

                _removed.Add(oldMeasureResult);

                var measureResultDeep = newMeasureResult.MeasureResultId > -1 ? _measureResultStorageService.GetMeasureResultById(newMeasureResult.MeasureResultId) : newMeasureResult;

                measureResultDeep.MeasureResultDatas.CollectionChanged += OnMeasureResultDatasChanged;
                measureResultDeep.MeasureSetup.PropertyChanged += OnMeasureSetupChanged;
                measureResultDeep.MeasureSetup.Cursors.CollectionChanged += OnCursorsChanged;

                foreach (var cursor in measureResultDeep.MeasureSetup.Cursors)
                {
                    cursor.PropertyChanged += OnCursorChanged;
                }

                measureResultDeep.DisplayOrder = diplayOrder;
                SelectedMeasureResults.Add(measureResultDeep);

                _added.Add(measureResultDeep);

                _raiseEventTimer.Stop();
                _raiseEventTimer.Start();
            });
        }

        public async Task AddSelectedMeasureResults(IList<MeasureResult> measureResults)
        {
            await Task.Run(async () =>
            {
                if (measureResults == null || !measureResults.Any()) return;
                
                _environmentService.SetEnvironmentInfo("IsBusy", true);

                foreach (var measureResult in measureResults)
                {
                    if(measureResult == null) continue;
                    
                    var measureResultDeep = measureResult.MeasureResultId > -1 ? _measureResultStorageService.GetMeasureResultById(measureResult.MeasureResultId, measureResult.IsDeletedResult) : measureResult;

                    if(measureResultDeep == null) continue;

                    if (measureResultDeep.MeasureResultDatas != null)
                    {
                        measureResultDeep.MeasureResultDatas.CollectionChanged += OnMeasureResultDatasChanged;
                    }

                    if (measureResultDeep.MeasureSetup != null)
                    {
                        measureResultDeep.MeasureSetup.PropertyChanged += OnMeasureSetupChanged;
                        if (measureResultDeep.MeasureSetup.Cursors != null)
                        {
                            measureResultDeep.MeasureSetup.Cursors.CollectionChanged += OnCursorsChanged;

                            foreach (var cursor in measureResultDeep.MeasureSetup.Cursors)
                            {
                                if (cursor != null)
                                {
                                    cursor.PropertyChanged += OnCursorChanged;
                                }
                            }
                        }
                    }
                    //}

                    //foreach (var measureResult in measureResults)
                    //{
                    await _measureResultDataCalculationService.UpdateMeasureResultDataAsync(measureResultDeep);

                    measureResultDeep.DisplayOrder = SelectedMeasureResults.Count;
                    SelectedMeasureResults.Add(measureResultDeep);

                    _added.Add(measureResultDeep);
                }
                //this.AddSelectedMeasureResults(measureResults);

                //_added.AddRange(measureResults);
                _raiseEventTimer.Stop();
                _raiseEventTimer.Start();
                //if (SelectedMeasureResultsChanged != null)
                //{
                //foreach (NotifyCollectionChangedEventHandler receiver in SelectedMeasureResultsChanged.GetInvocationList())
                //{
                //receiver.BeginInvoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, measureResults as IList), null, null);
                //}
                //}

                //this._updateTimer.Stop();
                //this._updateTimer.Start();
            });
        }

        public async Task RemoveSelectedMeasureResults(IList<MeasureResult> measureResults)
        {
            await Task.Run(() =>
            {
                if (measureResults == null || !measureResults.Any()) return;
                
                _environmentService.SetEnvironmentInfo("IsBusy", true);

                foreach (var measureResult in measureResults)
                {
                    measureResult.MeasureResultDatas.CollectionChanged -= OnMeasureResultDatasChanged;
                    if (measureResult.MeasureSetup == null) continue;
                    
                    measureResult.MeasureSetup.PropertyChanged -= OnMeasureSetupChanged;
                    measureResult.MeasureSetup.Cursors.CollectionChanged -= OnCursorsChanged;

                    foreach (var cursor in measureResult.MeasureSetup.Cursors)
                    {
                        cursor.PropertyChanged -= OnCursorChanged;
                    }
                }

                foreach (var measureResult in measureResults)
                {
                    var toRemove =
                        SelectedMeasureResults.FirstOrDefault(x => x.MeasureResultId == measureResult.MeasureResultId);
                    SelectedMeasureResults.Remove(toRemove);
                    if (measureResult.MeasureSetup == null) continue;
                    lock (_lock)
                    {
                        _rangeModificationTimers.TryRemove(measureResult.MeasureSetup, out _);
                    }
                }

                _removed.AddRange(measureResults);
                _raiseEventTimer.Stop();
                _raiseEventTimer.Start();

                //if (SelectedMeasureResultsChanged != null)
                //{
                //foreach (NotifyCollectionChangedEventHandler receiver in SelectedMeasureResultsChanged.GetInvocationList())
                //{
                //receiver.BeginInvoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, measureResults as IList), null, null);
                //}
                //}

                //CalculateMeasureResultData();
                this._updateTimer.Stop();
                this._updateTimer.Start();

                //GC.Collect();

                _environmentService.SetEnvironmentInfo("IsBusy", false);
            });
        }

        public void MoveSelectedMeasureResult(MeasureResult itemToMove, MeasureResult movePositionItem, int draggableOverLocation)
        {
            itemToMove.DisplayOrder = movePositionItem.DisplayOrder - draggableOverLocation;
        }

        private void OnMeasureResultDatasChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _updateTimer.Stop();
            _updateTimer.Start();
        }

        private void OnCursorChanged(object sender, PropertyChangedEventArgs e)
        {
            _updateTimer.Stop();
            _updateTimer.Start();
        }

        private void OnCursorsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _updateTimer.Stop();

            if (e.OldItems != null)
            {
                foreach (var cursor in e.OldItems.OfType<Cursor>())
                {
                    cursor.PropertyChanged -= OnCursorChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (var cursor in e.NewItems.OfType<Cursor>())
                {
                    cursor.PropertyChanged += OnCursorChanged;
                }
            }

            _updateTimer.Start();
        }

        private void OnMeasureSetupChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "ResultItemTypes":
                case "ToDiameter":
                case "FromDiameter":
                case "MeasureMode":
                case "Volume":
                case "VolumeCorrectionFactor":
                case "DilutionFactor":
                case "Repeats":
                case "AggregationCalculationMode":
                case "ManualAggregationCalculationFactor":
                case "HasSubpopulations":
                    _updateTimer.Stop();
                    _updateTimer.Start();
                    break;
            }
        }

        private void OnUpdateTimerElapsed(object sender, ElapsedEventArgs args)
        {
            _updateTimer.Stop();

            _doUpdate = true;

            if (!(_updateTask != null && (_updateTask.IsCompleted == false ||
                           _updateTask.Status == TaskStatus.Running ||
                           _updateTask.Status == TaskStatus.WaitingToRun ||
                           _updateTask.Status == TaskStatus.WaitingForActivation)))
            {
                _updateTask = Task.Factory.StartNew(() =>
                {
                    while (_doUpdate)
                    {
                        _doUpdate = false;

                        CalculateMeasureResultData();

                        if (SelectedMeasureResultDataChangedEvent == null) continue;
                        foreach (var @delegate in SelectedMeasureResultDataChangedEvent.GetInvocationList())
                        {
                            var receiver = (EventHandler<MeasureResultDataChangedEventArgs>) @delegate;
                            receiver.BeginInvoke(this, new MeasureResultDataChangedEventArgs(), null, null);
                        }
                    }
                });
            }

        }

        private async Task CalculateMeasureResultData()
        {
            foreach (var measureResult in SelectedMeasureResults)
            {
                await _measureResultDataCalculationService.UpdateMeasureResultDataAsync(measureResult);
            }
            
            if (_meanMeasureResult == null) return;
            
            await _measureResultDataCalculationService.UpdateMeasureResultDataAsync(_meanMeasureResult,
                _meanMeasureResult.MeasureSetup);
            await _measureResultDataCalculationService.UpdateMeanDeviationsAsync(_meanMeasureResult,
                SelectedMeasureResults.Where(mr => mr.IsVisible).ToArray());
        }

        private void OnRangeModificationTimerElapsed(Timer timer, MeasureSetup measureSetup)
        {
            lock (_lock)
            {
                if (!timer.Enabled) return;
                
                timer.Stop();
                _uiProject.StartUndoGroup();

                foreach (var cursor in measureSetup.Cursors)
                {
                    if (cursor.MinLimit != cursor.OldMinLimit)
                    {
                        _uiProject.SendUIEdit(cursor, "MinLimit", cursor.MinLimit, cursor.OldMinLimit);
                        _uiProject.SendUIEdit(cursor, "OldMinLimit", cursor.MinLimit);
                    }

                    if (cursor.MaxLimit == cursor.OldMaxLimit) continue;
                    
                    _uiProject.SendUIEdit(cursor, "MaxLimit", cursor.MaxLimit, cursor.OldMaxLimit);
                    _uiProject.SendUIEdit(cursor, "OldMaxLimit", cursor.MaxLimit);
                }
                _uiProject.SubmitUndoGroup();
            }
        }

        public MeasureSetup CloneTemplate(MeasureSetup template)
        {
            var setup = _databaseStorageService.GetMeasureSetup(template.MeasureSetupId, true);
            setup.MeasureSetupId = -1;
            setup.IsTemplate = false;

            foreach (var cursor in setup.Cursors)
            {
                cursor.CursorId = -1;
            }

            return setup;
        }

        private void UpdateExperimentGroupMappings()
        {
            //_experimentsGroupsMappings.Clear();

            //var experiments = _databaseStorageService.GetExperiments().Select(e => e.Item1)
            //    .Distinct().OrderBy(experiment => experiment).ToList();

            var groupMappings = _databaseStorageService.GetExperimentGroupMappings();

            //foreach (var experiment in experiments)
            //{
            //    var groups = _databaseStorageService.GetGroups(experiment)
            //        .Where(g => !string.IsNullOrEmpty(g.Item1)).Select(g => g.Item1).Distinct()
            //        .OrderBy(group => group).ToList();

            //    _experimentsGroupsMappings.TryAdd(experiment ?? string.Empty, groups);
            //}
            _experimentsGroupsMappings = new ConcurrentDictionary<string, List<string>>(groupMappings);

            if (ExperimentsGroupsMappingsChangedEvent == null) return;
            foreach (var @delegate in ExperimentsGroupsMappingsChangedEvent.GetInvocationList())
            {
                var receiver = (EventHandler)@delegate;
                receiver.BeginInvoke(this, EventArgs.Empty, null, null);
            }
        }

        public string FindMeasurementName(MeasureResult measureResult)
        {
            var tempName = measureResult.Name;

            if (!_measureResultStorageService.MeasureResultExists(tempName, measureResult.Experiment,
                measureResult.Group)) return tempName;

            var count = 1;
            var measurementName = $"{tempName}_{count.ToString()}";
            while (_measureResultStorageService.MeasureResultExists(measurementName, measureResult.Experiment,
                measureResult.Group))
            {
                count++;
                measurementName = string.Format("{0}_{1}", tempName, count.ToString());
            }

            tempName = measurementName;

            return tempName;
        }
    }
}