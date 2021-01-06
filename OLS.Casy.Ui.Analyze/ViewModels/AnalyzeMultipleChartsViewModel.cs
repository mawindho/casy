using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Windows.Data;
using System;
using System.Collections.Specialized;
using System.Linq;
using OLS.Casy.Models;
using OLS.Casy.Core.Api;
using OLS.Casy.Ui.Core.Api;
using System.Windows;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using OLS.Casy.Core;
using OLS.Casy.Core.Events;
using PdfSharp.Pdf;
using System.IO;
using System.Threading;
using MigraDoc.Rendering;
using PdfSharp.Pdf.IO;

namespace OLS.Casy.Ui.Analyze.ViewModels
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(AnalyzeMultipleChartsViewModel))]
    public class AnalyzeMultipleChartsViewModel : AnalyzeChartViewModelBase
    {
        private readonly IEnvironmentService _environmentService;
        private readonly IEventAggregatorProvider _eventAggregatorProvider;

        private ListCollectionView _analyzeChartViewSource;
        private int _maxHorizontalChartCount = 1;
        private bool _doScrollToTop;
        private object _scrollToItem;
        private readonly object _lock = new object();

        [ImportingConstructor]
        public AnalyzeMultipleChartsViewModel(
            IMeasureResultManager measureResultManager,
            ICompositionFactory compositionFactory,
            Lazy<IMeasureResultContainerViewModel> measureResultContainerViewModel,
            IEnvironmentService environmentService,
            IEventAggregatorProvider eventAggregatorProvider
            ) : base(measureResultManager, compositionFactory)
        {
            _environmentService = environmentService;
            _eventAggregatorProvider = eventAggregatorProvider;
        }

        public ListCollectionView MeasureResultContainerViewSource
        {
            get
            {
                if (_analyzeChartViewSource != null) return _analyzeChartViewSource;
                
                _analyzeChartViewSource = CollectionViewSource.GetDefaultView(MeasureResultContainers) as ListCollectionView;
                if (_analyzeChartViewSource == null) return _analyzeChartViewSource;
                
                _analyzeChartViewSource.SortDescriptions.Add(new SortDescription("DisplayOrder",
                    ListSortDirection.Descending));
                //_analyzeChartViewSource.Filter = item =>
                //{
                //var mrcvm = item as IMeasureResultContainerViewModel;
                //return mrcvm != null && mrcvm.IsVisible;
                //};
                //_analyzeChartViewSource.IsLiveFiltering = true;
                _analyzeChartViewSource.IsLiveSorting = true;
                return _analyzeChartViewSource;
            }
        }

        public int MaxHorizontalChartCount
        {
            get => _maxHorizontalChartCount > MeasureResultContainers.Count() ? (!MeasureResultContainers.Any() ? 1 : MeasureResultContainers.Count()) : _maxHorizontalChartCount;
            set
            {
                if (value == _maxHorizontalChartCount || value > MeasureResultContainers.Count()) return;
                
                _environmentService.SetEnvironmentInfo("IsBusy", true);
                _maxHorizontalChartCount = value;

                //Task.Run(() =>
                //{
                    

                    if (MeasureResultContainers.Count > 1)
                    {
                        foreach (var viewModel in MeasureResultContainers)
                        {
                            viewModel.IsExpandViewCollapsed = MaxHorizontalChartCount > 1;
                            viewModel.IsButtonMenuCollapsed = MaxHorizontalChartCount > 1;
                            Task.Run(async () => await viewModel.UpdateRowHeightAsync());
                        }
                    }
                    else if (MeasureResultContainers.Count > 0)
                    {
                        var firstItem = MeasureResultContainers.FirstOrDefault();

                        if (firstItem != null)
                        {
                            firstItem.IsExpandViewCollapsed = false;
                            firstItem.IsButtonMenuCollapsed = false;
                            Task.Run(async () => await firstItem.UpdateRowHeightAsync());
                    }
                    }

//                    Application.Current.Dispatcher.Invoke(() =>
//                    {
//                        MeasureResultContainerViewSource.Refresh();
//                        
//                    });
                //});
                
                NotifyOfPropertyChange();
                _environmentService.SetEnvironmentInfo("IsBusy", false);
            }
        }

        public bool DoScrollToTop
        {
            get => _doScrollToTop;
            set
            {
                _doScrollToTop = value;
                NotifyOfPropertyChange();
            }
        }

        public object ScrollToItem
        {
            get => _scrollToItem;
            set
            {
                _scrollToItem = value;
                NotifyOfPropertyChange();
            }
        }

        public bool ScrollToTop
        {
            get => _scrollToTop;
            set
            {
                _scrollToTop = value;
                NotifyOfPropertyChange();
            }
        }

        protected override void OnIsActiveChanged()
        {
            if (IsActive)
            {
                //lock (_lock)
                //{
                //Task.Factory.StartNew(() =>
                //{
                _environmentService.SetEnvironmentInfo("IsBusy", true);

                var selectedMeasureResults = MeasureResultManager.SelectedMeasureResults.ToList();
                var visibleMeasureResults = selectedMeasureResults.Where(mr => mr.IsVisible)
                    .OrderBy(mr => mr.DisplayOrder).ToList();

                var toRemoves = new List<IMeasureResultContainerViewModel>();

                var measureResultContainers = MeasureResultContainers.ToList();
                foreach (var container in measureResultContainers)
                {
                    if ((container.SingleResult.IsTemporary && !container.SingleResult.IsDeletedResult) ||
                        visibleMeasureResults.Contains(container.SingleResult)) continue;

                    if (!selectedMeasureResults.Contains(container.SingleResult))
                    {
                        container.SingleResult.PropertyChanged -= OnMeasureResultPropertyChanged;
                    }

                    toRemoves.AddRange(MeasureResultContainers.Where(viewModel =>
                            viewModel.SingleResult.MeasureResultId == container.SingleResult.MeasureResultId)
                        .ToList());
                }

                if (toRemoves.Any())
                {
                    RemoveMeasureResultContainers(toRemoves);
                }

                //GC.Collect();

                //NotifyOfPropertyChange("MaxHorizontalChartCount");
                if (MaxHorizontalChartCount != _maxHorizontalChartCount)
                {
                    MaxHorizontalChartCount = MaxHorizontalChartCount;
                    Application.Current.Dispatcher.Invoke(() => { MeasureResultContainerViewSource.Refresh(); });
                }

                var toAdds = new List<Tuple<Lazy<IMeasureResultContainerViewModel>, IMeasureResultContainerViewModel>>();
                foreach (var measureResult in selectedMeasureResults)
                {
                    if (!MeasureResultContainers.Any(viewModel =>
                        viewModel.SingleResult != null && viewModel.SingleResult.MeasureResultId == measureResult.MeasureResultId))
                    {
                        var measureResultContainerViewModelExport = CompositionFactory.GetExport<IMeasureResultContainerViewModel>();
                        var measureResultContainerViewModel = measureResultContainerViewModelExport.Value;
                        measureResultContainerViewModel.IsVisible = measureResult.IsVisible;
                        measureResultContainerViewModel.AddMeasureResults(new[] {measureResult});
                        //AddMeasureResultContainer(measureResultContainerViewModelExport);
                        toAdds.Add(new Tuple<Lazy<IMeasureResultContainerViewModel>, IMeasureResultContainerViewModel>(measureResultContainerViewModelExport, measureResultContainerViewModel));
                        //

                        //measureResultContainerViewModel.AddMeasureResults(new[] { measureResult });
                        //measureResultContainerViewModel.Order = this.MeasureResultManager.SelectedMeasureResults.IndexOf(newItem);// this.MeasureResultContainerViewModels.Count == 0 ? 0 : this.MeasureResultContainerViewModels.Max(vm => vm.Order) + 1;
                        //Application.Current.Dispatcher.Invoke(() =>
                        //{
//                                this.MeasureResultContainerViewModelExports.Add(measureResultContainerViewModelExport);
                        //                              this.MeasureResultContainers.TryAdd(measureResult.MeasureResultId, measureResultContainerViewModel);
                        //                        });
                        //measureResultContainerViewModel.PropertyChanged += OnPropertyChanged;
                    }

                    measureResult.PropertyChanged += OnMeasureResultPropertyChanged;
                }

                if (toAdds.Any())
                {
                    AddMeasureResultContainers(toAdds);
                }

                if (MeasureResultContainers.Count > 1)
                {
                    foreach (var viewModel in MeasureResultContainers)
                    {
                        if (viewModel.IsExpandViewCollapsed == MaxHorizontalChartCount > 1) continue;
                        viewModel.IsExpandViewCollapsed = MaxHorizontalChartCount > 1;
                        Task.Run(async () => await viewModel.UpdateRowHeightAsync());
                    }
                }
                else if (MeasureResultContainers.Count > 0)
                {
                    if (MeasureResultContainers[0].IsExpandViewCollapsed)
                    {
                        MeasureResultContainers[0].IsExpandViewCollapsed = false;
                        Task.Run(async () => await MeasureResultContainers[0].UpdateRowHeightAsync());
                    }
                }

                NotifyOfPropertyChange("MaxHorizontalChartCount");

                ScrollToTop = true;
                _scrollToTop = false;

                MeasureResultManager.SelectedMeasureResultsChanged += OnSelectedMeasureResultsChanged;

                _environmentService.SetEnvironmentInfo("IsBusy", false);
            }
            else
            {
                MeasureResultManager.SelectedMeasureResultsChanged -= OnSelectedMeasureResultsChanged;
            }
        }

        private void OnSelectedMeasureResultsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!IsActive) return;
            //lock (_lock)
            //{
            lock (_lock)
            {
                _environmentService.SetEnvironmentInfo("IsBusy", true);

            if (e.OldItems != null)
            {
                var toRemoves = new List<IMeasureResultContainerViewModel>();

                foreach (var oldItem in e.OldItems.OfType<MeasureResult>())
                {
                    oldItem.PropertyChanged -= OnMeasureResultPropertyChanged;
                    toRemoves.AddRange(MeasureResultContainers.Where(viewModel => viewModel.SingleResult.MeasureResultId == oldItem.MeasureResultId).ToList());
                }

                if (toRemoves.Any())
                {
                    foreach (var toRemove in toRemoves)
                    {
                        RemoveMeasureResultContainer(toRemove);
                    }

                    NotifyOfPropertyChange("MaxHorizontalChartCount");
                }
            }

            if (e.NewItems != null)
            {
                foreach (var newItem in e.NewItems.OfType<MeasureResult>())
                {
                    if (newItem.IsReadOnly || !MeasureResultContainers.Any(viewModel => viewModel.SingleResult != null && viewModel.SingleResult.MeasureResultId == newItem.MeasureResultId))
                    {
                        var measureResultContainerViewModelExport = CompositionFactory.GetExport<IMeasureResultContainerViewModel>();
                        var measureResultContainerViewModel = measureResultContainerViewModelExport.Value;
                        measureResultContainerViewModel.AddMeasureResults(new[] { newItem });
                        AddMeasureResultContainer(new Tuple<Lazy<IMeasureResultContainerViewModel>, IMeasureResultContainerViewModel>(measureResultContainerViewModelExport, measureResultContainerViewModel));
                        
                        //var measureResultContainerViewModel = measureResultContainerViewModelExport.Value;

                        //measureResultContainerViewModel.AddMeasureResults(new[] { newItem });
                        //measureResultContainerViewModel.Order = this.MeasureResultManager.SelectedMeasureResults.IndexOf(newItem);// this.MeasureResultContainerViewModels.Count == 0 ? 0 : this.MeasureResultContainerViewModels.Max(vm => vm.Order) + 1;
                        //Application.Current.Dispatcher.Invoke(() =>
                        //{
                            //this.MeasureResultContainerViewModelExports.Add(measureResultContainerViewModelExport);
                            //this.MeasureResultContainers.TryAdd(newItem.MeasureResultId, measureResultContainerViewModel);
                        //});
                        //measureResultContainerViewModel.PropertyChanged += OnPropertyChanged;
                    }

                    newItem.PropertyChanged += OnMeasureResultPropertyChanged;
                }
            }

            //toRemoves = new List<Lazy<IMeasureResultContainerViewModel>>();
            //foreach (var container in MeasureResultContainers)
            //{
            //    if (container.SingleResult != null && !container.SingleResult.IsTemporary && !this.MeasureResultManager.SelectedMeasureResults.Contains(container.SingleResult))
            //    {
            //        container.SingleResult.PropertyChanged -= OnMeasureResultPropertyChanged;

            //        toRemoves = this.MeasureResultContainerViewModelExports.Where(viewModel => viewModel.Value.SingleResult.MeasureResultId == container.SingleResult.MeasureResultId).ToList();
            //        if (toRemoves.Any())
            //        {
            //            Application.Current.Dispatcher.Invoke(() =>
            //            {
            //                this.MeasureResultContainers.RemoveRange(toRemoves.Select(l => l.Value));
            //            });

            //            foreach (var toRemove in toRemoves)
            //            {
            //                //this.MeasureResultContainerViewModels.Remove(toRemove);
            //                //});
            //                this.MeasureResultContainerViewModelExports.Remove(toRemove);
            //                toRemove.Value.PropertyChanged -= OnPropertyChanged;
            //                toRemove.Value.Dispose();

            //                this._compositionFactory.ReleaseExport(toRemove);
            //            }
            //        }

            //        GC.Collect();

            //        NotifyOfPropertyChange("MaxHorizontalChartCount");
            //    }
            //}

            if (MeasureResultContainers.Count > 1)
            {
                foreach (var viewModel in MeasureResultContainers)
                {
                    //viewModel.Order = this.MeasureResultManager.SelectedMeasureResults.IndexOf(viewModel.MeasureResults.First());// this.MeasureResultContainerViewModels.Count == 0 ? 0 : this.MeasureResultContainerViewModels.Max(vm => vm.Order) + 1;
                    var newExpansionState = MaxHorizontalChartCount > 1;
                    if (newExpansionState == viewModel.IsExpandViewCollapsed) continue;
                            
                    viewModel.IsExpandViewCollapsed = newExpansionState;
                    Task.Run(async () => await viewModel.UpdateRowHeightAsync());
                }
            }
            else if (MeasureResultContainers.Count > 0)
            {
                if (MeasureResultContainers[0].IsExpandViewCollapsed)
                {
                    MeasureResultContainers[0].IsExpandViewCollapsed = false;
                    Task.Run(async () => await MeasureResultContainers[0].UpdateRowHeightAsync());
                }
            }

                //NotifyOfPropertyChange("MaxHorizontalChartCount");
                //this.DoScrollToTop = true;
                //ScrollToItem = MeasureResultContainers.LastOrDefault();
                ScrollToTop = true;
                _scrollToTop = false;


                Application.Current.Dispatcher.Invoke(() =>
            {
                if (MaxHorizontalChartCount != _maxHorizontalChartCount)
                {
                    MaxHorizontalChartCount = MaxHorizontalChartCount;
                }

                //MeasureResultContainerViewSource.Refresh();
            });
            _environmentService.SetEnvironmentInfo("IsBusy", false);
            }
            //});
        }

        private volatile bool _ignoreOrderChanges = false;
        private bool _scrollToTop;

        private void OnMeasureResultPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "DisplayOrder":
                {
                    if (!_ignoreOrderChanges)
                    {
                        if (MeasureResultManager.SelectedMeasureResults != null)
                        {
                            _ignoreOrderChanges = true;

                            var measureResults = MeasureResultManager.SelectedMeasureResults.OrderBy(mr => mr.DisplayOrder).ToArray();

                            if (sender is MeasureResult movedMeasureResult)
                            {
                                var movedMeasureResultContainer = MeasureResultContainers.FirstOrDefault(viewModel => viewModel.SingleResult != null && viewModel.SingleResult.MeasureResultId == movedMeasureResult.MeasureResultId);

                                if (movedMeasureResultContainer != null)
                                {
                                    movedMeasureResultContainer.DisplayOrder = movedMeasureResult.DisplayOrder;

                                    //var other = measureResults.FirstOrDefault(mr => mr.DisplayOrder == movedMeasureResult.DisplayOrder && mr.MeasureResultId != movedMeasureResult.MeasureResultId);

                                    //if (other != null)
                                    //{
                                    var order = 1;
                                    foreach (var curMeasureResult in measureResults)
                                    {
                                        if (curMeasureResult.MeasureResultId == movedMeasureResult.MeasureResultId)
                                            continue;
                                        
                                        var result = curMeasureResult;
                                        var curMeasureResultContainer = MeasureResultContainers.FirstOrDefault(viewModel => viewModel.SingleResult != null && viewModel.SingleResult.MeasureResultId == result.MeasureResultId);

                                        if (curMeasureResultContainer != null)
                                        {
                                            if (curMeasureResult.DisplayOrder != movedMeasureResult.DisplayOrder)
                                            {
                                                curMeasureResult.DisplayOrder = order;
                                                curMeasureResultContainer.DisplayOrder = order;
                                            }
                                            else if (curMeasureResult.DisplayOrder == movedMeasureResult.DisplayOrder)
                                            {
                                                curMeasureResult.DisplayOrder = ++order;
                                                curMeasureResultContainer.DisplayOrder = order;
                                            }
                                        }
                                        order++;
                                    }
                                }
                            }
                            //}

                            OnIsActiveChanged();

                            _ignoreOrderChanges = false;
                        }
                    }

                    //Application.Current.Dispatcher.Invoke(() =>
                    //{
                    //    this.MeasureResultContainerViewSource.Refresh();
                    //    foreach (var measureResultContainerViewModel in MeasureResultContainers)
                    //    {
                    //       measureResultContainerViewModel.ForceUpdate();
                    //    }
                    //});
                    break;
                }
                //this.MeasureResultContainerViewSource.Refresh();
                //this.OnIsActiveChanged();
                case "IsVisible" when MeasureResultManager.SelectedMeasureResults == null:
                    return;
                case "IsVisible":
                {
                    if (!(sender is MeasureResult measureResult)) return;
                
                    var measureResultContainer = MeasureResultContainers.FirstOrDefault(viewModel =>
                        viewModel.SingleResult != null && viewModel.SingleResult.MeasureResultId ==
                        measureResult.MeasureResultId);

                    if (measureResultContainer == null) return;
                    measureResultContainer.IsVisible = measureResult.IsVisible;
                    break;
                }
            }
        }

        /*
        protected override void OnReadOnlyMeasureResultChanged(object sender, EventArgs e)
        {
            var measureResultContainerViewModel = _compositionFactory.GetComposite<IMeasureResultContainerViewModel>();
            measureResultContainerViewModel.PropertyChanged += OnPropertyChanged;
            measureResultContainerViewModel.AddMeasureResult(this.MeasureResultManager.ReadOnlyMeasureResult);
            this.MeasureResultContainerViewModels.Insert(0, measureResultContainerViewModel);
        }
        */

        //private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        //{
        //    if (e.PropertyName == "IsVisible")
        //    {
        //        //this.MeasureResultContainerViewSource.Refresh();
        //        this.OnIsActiveChanged();
        //    }
        //}

        public override void OnImportsSatisfied()
        {
            base.OnImportsSatisfied();
            _eventAggregatorProvider.Instance.GetEvent<PrintAllMeasurementsEvent>().Subscribe(OnPrintAllMeasurements);
        }

        private async void OnPrintAllMeasurements()
        {
            if (this.IsActive)
            {
                var result = await Task.Run(async () => await MeasureResultManager.SaveChangedMeasureResults());
                if (result == ButtonResult.Cancel)
                {
                    return;
                }

                _environmentService.SetEnvironmentInfo("IsBusy", true);

                foreach (var container in MeasureResultContainers)
                {
                    container.CaptureImage();
                }

                List<PdfDocument> documents = new List<PdfDocument>();
                foreach (var container in MeasureResultContainers)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        if (container.CurrentDocument == null)
                        {
                            await Task.Delay(1000);
                        }
                        else
                        {
                            documents.Add((PdfDocument) container.CurrentDocument);
                            container.CurrentDocument = null;
                            break;
                        }
                    }
                }

                PdfDocument outputDocument = new PdfDocument();
                foreach (var document in documents)
                {
                    var renderer = new PdfDocumentRenderer(false);
                    renderer.PdfDocument = document;

                    using (MemoryStream ms = new MemoryStream())
                    {
                        renderer.Save(ms, false);
                        ms.Seek(0, SeekOrigin.Begin);

                        PdfDocument inputDocument = PdfReader.Open(ms, PdfDocumentOpenMode.Import);

                        int count = inputDocument.PageCount;
                        for (int idx = 0; idx < count; idx++)
                        {
                            PdfPage page = inputDocument.Pages[idx];
                            outputDocument.AddPage(page);
                        }
                    }
                }

                _environmentService.SetEnvironmentInfo("IsBusy", false);

                var appDataFolder = Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData), "Casy", "temp");

                if (!Directory.Exists(appDataFolder))
                {
                    Directory.CreateDirectory(appDataFolder);
                }

                var fileName = $"MultipleMeasurements_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.pdf";

                fileName = Path.Combine(appDataFolder, fileName);

                
                outputDocument.Save(fileName);

                try
                {
                    Process.Start(fileName);
                }
                catch (Exception)
                {
                    await Task.Factory.StartNew(() =>
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
}
