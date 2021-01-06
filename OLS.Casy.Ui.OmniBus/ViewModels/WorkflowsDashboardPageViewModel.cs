using DevExpress.Mvvm;
using OLS.Casy.Com.OmniBus;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.MainControls.Api;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace OLS.Casy.Ui.OmniBus.ViewModels
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IDashboardPageViewModel))]
    public class WorkflowsDashboardPageViewModel : Base.ViewModelBase, IDashboardPageViewModel, IPartImportsSatisfiedNotification
    {
        private readonly IOmniBusDataService _omniBusDataService;

        private List<WorkflowInstanceViewModel> _workflowInstanceViewModels;
        
         [ImportingConstructor]
        public WorkflowsDashboardPageViewModel(IOmniBusDataService omniBusDataService)
        {
            this._omniBusDataService = omniBusDataService;
            this._workflowInstanceViewModels = new List<WorkflowInstanceViewModel>();
        }

        public List<WorkflowInstanceViewModel> WorkflowInstanceViewModels
        {
            get { return _workflowInstanceViewModels; }
        }

        //public ListCollectionView WorkflowInstanceViewSource
        //{
        //    get
        //    {
        //        if (_worflowInstanceViewSource == null)
        //        {
        //            _worflowInstanceViewSource = CollectionViewSource.GetDefaultView(this._workflowInstanceViewModels) as ListCollectionView;
        //            _worflowInstanceViewSource.SortDescriptions.Add(new SortDescription("Order", ListSortDirection.Descending));
        //            //_analyzeChartViewSource.Filter = item =>
        //            //{
        //            //var mrcvm = item as IMeasureResultContainerViewModel;
        //            //return mrcvm != null && mrcvm.IsVisible;
        //            //};
        //            //_analyzeChartViewSource.IsLiveFiltering = true;
        //            _worflowInstanceViewSource.IsLiveSorting = true;
        //        }
        //        return _worflowInstanceViewSource;
        //    }
        //}

        public ICommand TileSelectedCommand
        {
            get { return new OmniDelegateCommand<WorkflowInstanceViewModel>(OnTileSelected); }
        }

        private void OnTileSelected(WorkflowInstanceViewModel selectedTile)
        {
            foreach(var item in this._workflowInstanceViewModels)
            {
                if(item == selectedTile)
                {
                    item.IsMaximized = true;
                }
                else
                {
                    item.IsMaximized = false;
                }
            }
        }

        public async void OnImportsSatisfied()
        {
            var workflowInstances = await this._omniBusDataService.GetWorkflowInstances();

            if(workflowInstances != null)
            {
                foreach(var instance in workflowInstances)
                {
                    this._workflowInstanceViewModels.Add(new WorkflowInstanceViewModel()
                    {
                        Header = instance.Workflow.Name
                    });
                }
            }

            //this._workflowInstanceViewModels.Add(new WorkflowInstanceViewModel()
            //{
            //    Header = "Test 1"
            //});
            //this._workflowInstanceViewModels.Add(new WorkflowInstanceViewModel()
            //{
            //    Header = "Test 2"
            //});
            //this._workflowInstanceViewModels.Add(new WorkflowInstanceViewModel()
            //{
            //    Header = "Test 3"
            //});
            //this._workflowInstanceViewModels.Add(new WorkflowInstanceViewModel()
            //{
            //    Header = "Test 4"
            //});
            //this._workflowInstanceViewModels.Add(new WorkflowInstanceViewModel()
            //{
            //    Header = "Test 5"
            //});
            //this._workflowInstanceViewModels.Add(new WorkflowInstanceViewModel()
            //{
            //    Header = "Test 6"
            //});
            //this._workflowInstanceViewModels.Add(new WorkflowInstanceViewModel()
            //{
            //    Header = "Test 7"
            //});
            //this._workflowInstanceViewModels.Add(new WorkflowInstanceViewModel()
            //{
            //    Header = "Test 8"
            //});
            //this._workflowInstanceViewModels.Add(new WorkflowInstanceViewModel()
            //{
            //    Header = "Test 9"
            //});
            //this._workflowInstanceViewModels.Add(new WorkflowInstanceViewModel()
            //{
            //    Header = "Test 10"
            //});
            //this._workflowInstanceViewModels.Add(new WorkflowInstanceViewModel()
            //{
            //    Header = "Test 11"
            //});
            //this._workflowInstanceViewModels.Add(new WorkflowInstanceViewModel()
            //{
            //    Header = "Test 12"
            //});
            //this._workflowInstanceViewModels.Add(new WorkflowInstanceViewModel()
            //{
            //    Header = "Test 13"
            //});
            //this._workflowInstanceViewModels.Add(new WorkflowInstanceViewModel()
            //{
            //    Header = "Test 14"
            //});
            //this._workflowInstanceViewModels.Add(new WorkflowInstanceViewModel()
            //{
            //    Header = "Test 15"
            //});
        }
    }
}
