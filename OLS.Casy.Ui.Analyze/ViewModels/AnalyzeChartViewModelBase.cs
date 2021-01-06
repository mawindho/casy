using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Core.Api;
using System;
using System.Collections;
using OLS.Casy.Core;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using OLS.Casy.Core.Api;

namespace OLS.Casy.Ui.Analyze.ViewModels
{
    public abstract class AnalyzeChartViewModelBase : ViewModelBase, IPartImportsSatisfiedNotification
    {
        private bool _isActive;
        
        private readonly object _lock = new object();

        protected AnalyzeChartViewModelBase(IMeasureResultManager measureResultManager, ICompositionFactory compositionFacotry)
        {
            MeasureResultManager = measureResultManager;
            MeasureResultContainerViewModelExports = new Dictionary<int, Lazy<IMeasureResultContainerViewModel>>();
            MeasureResultContainers = new SmartCollection<IMeasureResultContainerViewModel>();
            CompositionFactory = compositionFacotry;
        }

        protected ICompositionFactory CompositionFactory { get; }

        private Dictionary<int, Lazy<IMeasureResultContainerViewModel>> MeasureResultContainerViewModelExports { get; }

        protected SmartCollection<IMeasureResultContainerViewModel> MeasureResultContainers { get; }

        protected void AddMeasureResultContainer(
            Tuple<Lazy<IMeasureResultContainerViewModel>, IMeasureResultContainerViewModel> measureResultContainerViewModelExport)
        {
            lock (_lock)
            {
                var id = measureResultContainerViewModelExport.Item2.SingleResult == null ? 0 : measureResultContainerViewModelExport.Item2.SingleResult.MeasureResultId;

                if (MeasureResultContainerViewModelExports.ContainsKey(id))
                {
                    MeasureResultContainerViewModelExports[id] = measureResultContainerViewModelExport.Item1;
                }
                else
                {
                    MeasureResultContainerViewModelExports.Add(id, measureResultContainerViewModelExport.Item1);
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    measureResultContainerViewModelExport.Item2.DisplayOrder = MeasureResultContainerViewModelExports.Count;
                    MeasureResultContainers.Add(measureResultContainerViewModelExport.Item2);
                });
            }
        }

        protected void AddMeasureResultContainers(
            IEnumerable<Tuple<Lazy<IMeasureResultContainerViewModel>, IMeasureResultContainerViewModel>> measureResultContainerViewModelExport)
        {
            var values = new List<IMeasureResultContainerViewModel>();

            lock (((ICollection)measureResultContainerViewModelExport).SyncRoot)
            {
                //MeasureResultContainerViewModelExports.AddRange(resultContainerViewModelExportInternal);
                var count = MeasureResultContainerViewModelExports.Count + measureResultContainerViewModelExport.Count();
                foreach (var item in measureResultContainerViewModelExport)
                {
                    var value = item.Item2;
                    value.DisplayOrder = count++;
                    values.Add(value);

                    MeasureResultContainerViewModelExports.Add(value.SingleResult == null ? 0 : value.SingleResult.MeasureResultId, item.Item1);
                } 
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                MeasureResultContainers.AddRange(values);
            });
        }

        protected void RemoveMeasureResultContainer(IMeasureResultContainerViewModel measureResultContainerViewModel)
        {
            lock (_lock)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MeasureResultContainers.Remove(measureResultContainerViewModel);
                });

                var id = measureResultContainerViewModel.SingleResult == null ? 0 : measureResultContainerViewModel.SingleResult.MeasureResultId;
                if (MeasureResultContainerViewModelExports.TryGetValue(id, out var export))
                {
                    MeasureResultContainerViewModelExports.Remove(id);
                    CompositionFactory.ReleaseExport(export);
                }
                
                measureResultContainerViewModel.Dispose();

                
            }
        }
        
        protected void RemoveMeasureResultContainers(IEnumerable</*Lazy<*/IMeasureResultContainerViewModel/*>*/> measureResultContainerViewModels)
        {
            lock (_lock)
            {
                //var values = measureResultContainerViewModelExport.Select(item => item.Value);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MeasureResultContainers.RemoveRange(measureResultContainerViewModels);
                });

                foreach (var value in measureResultContainerViewModels)
                {
                    var id = value.SingleResult == null ? 0 : value.SingleResult.MeasureResultId;
                    if (MeasureResultContainerViewModelExports.TryGetValue(id, out var export))
                    {
                        MeasureResultContainerViewModelExports.Remove(id);
                        CompositionFactory.ReleaseExport(export);
                    }

                    value.Dispose();
                }       
            }
        }

        public bool IsActive
        {
            protected get => _isActive;
            set
            {
                if (value == _isActive) return;
                _isActive = value;
                NotifyOfPropertyChange();
                OnIsActiveChanged();
            }
        }

        protected virtual void OnIsActiveChanged()
        {
        }

        public virtual void OnImportsSatisfied()
        {
        }

        protected IMeasureResultManager MeasureResultManager { get; }
    }
}
