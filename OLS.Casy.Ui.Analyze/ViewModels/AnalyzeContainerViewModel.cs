using OLS.Casy.Core.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.MainControls.Api;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;

namespace OLS.Casy.Ui.Analyze.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(HostViewModel))]
    public class AnalyzeContainerViewModel : HostViewModel, IPartImportsSatisfiedNotification
    {
        private readonly IEventAggregatorProvider _eventAggregatorProvider;

        private readonly AnalyzeMultipleChartsViewModel _analyzeMultipleChartsViewModel;
        private readonly AnalyzeOverlayChartViewModel _analyzeOverlayChartViewModel;
        private readonly AnalyzeMeanChartViewModel _analyzeMeanChartViewModel;
        //private readonly AnalyzeAdvancedChartViewModel _analyzeAdvancedChartViewModel;

        private bool _isOverlayActive;
        private bool _isMeanActive;
        //private bool _isAdvancedActive;

        [ImportingConstructor]
        public AnalyzeContainerViewModel(
            IEventAggregatorProvider eventAggregatorProvider,
            AnalyzeMultipleChartsViewModel analyzeMultipleChartsViewModel,
            AnalyzeOverlayChartViewModel analyzeOverlayChartViewModel,
            AnalyzeMeanChartViewModel analyzeMeanChartViewModel
            //AnalyzeAdvancedChartViewModel analyzeAdvancedChartViewModel,
            )
        {
            _eventAggregatorProvider = eventAggregatorProvider;

            _analyzeMultipleChartsViewModel = analyzeMultipleChartsViewModel;
            _analyzeOverlayChartViewModel = analyzeOverlayChartViewModel;
            _analyzeMeanChartViewModel = analyzeMeanChartViewModel;
            //this._analyzeAdvancedChartViewModel = analyzeAdvancedChartViewModel;
            
            AnalyzeChartViewModels = new ObservableCollection<AnalyzeChartViewModelBase>();
        }

        public bool IsOverlayActive
        {
            get => _isOverlayActive;
            set
            {
                if (value == _isOverlayActive) return;
                _isOverlayActive = value;
                NotifyOfPropertyChange();

                if(value)
                {
                    _analyzeMultipleChartsViewModel.IsActive = false;
                    //this._analyzeAdvancedChartViewModel.IsActive = false;
                    _analyzeMeanChartViewModel.IsActive = false;

                    //this._isAdvancedActive = false;
                    //NotifyOfPropertyChange("IsAdvancedActive");
                    _isMeanActive = false;
                    NotifyOfPropertyChange("IsMeanActive");

                    _analyzeOverlayChartViewModel.IsActive = true;
                }
                else
                {
                    _analyzeMultipleChartsViewModel.IsActive = true;
                    _analyzeOverlayChartViewModel.IsActive = false;
                    _analyzeMeanChartViewModel.IsActive = false;
                    //this._analyzeAdvancedChartViewModel.IsActive = false;
                }
            }
        }

        public bool IsMeanActive
        {
            get => _isMeanActive;
            set
            {
                if (value == _isMeanActive) return;
                
                _isMeanActive = value;
                NotifyOfPropertyChange();

                if (value)
                {
                    _analyzeMultipleChartsViewModel.IsActive = false;
                    //this._analyzeAdvancedChartViewModel.IsActive = false;
                    _analyzeOverlayChartViewModel.IsActive = false;

                    //this._isAdvancedActive = false;
                    //NotifyOfPropertyChange("IsAdvancedActive");
                    _isOverlayActive = false;
                    NotifyOfPropertyChange("IsOverlayActive");

                    _analyzeMeanChartViewModel.IsActive = true;
                }
                else
                {
                    _analyzeMultipleChartsViewModel.IsActive = true;
                    _analyzeOverlayChartViewModel.IsActive = false;
                    _analyzeMeanChartViewModel.IsActive = false;
                    //this._analyzeAdvancedChartViewModel.IsActive = false;
                }
            }
        }

        //public bool IsAdvancedActive
        //{
        //    get { return _isAdvancedActive; }
        //    set
        //    {
        //        if (value != _isAdvancedActive)
        //        {
        //            _isAdvancedActive = value;
        //            NotifyOfPropertyChange();

        //            if (value)
        //            {
        //                this._analyzeMultipleChartsViewModel.IsActive = false;
        //                this._analyzeOverlayChartViewModel.IsActive = false;
        //                this._analyzeMeanChartViewModel.IsActive = false;

        //                this._isOverlayActive = false;
        //                NotifyOfPropertyChange("IsOverlayActive");
        //                this._isMeanActive = false;
        //                NotifyOfPropertyChange("IsMeanActive");

        //                this._analyzeAdvancedChartViewModel.IsActive = true;
        //            }
        //            else
        //            {
        //                this._analyzeMultipleChartsViewModel.IsActive = true;
        //                this._analyzeOverlayChartViewModel.IsActive = false;
        //                this._analyzeMeanChartViewModel.IsActive = false;
        //                this._analyzeAdvancedChartViewModel.IsActive = false;
        //            }
        //        }
        //    }
        //}

        public ObservableCollection<AnalyzeChartViewModelBase> AnalyzeChartViewModels { get; }

        public void OnImportsSatisfied()
        {
            _eventAggregatorProvider.Instance.GetEvent<NavigateToEvent>().Subscribe(OnNavigateToEvent);

            AnalyzeChartViewModels.Add(_analyzeMultipleChartsViewModel);
            AnalyzeChartViewModels.Add(_analyzeOverlayChartViewModel);
            AnalyzeChartViewModels.Add(_analyzeMeanChartViewModel);
            //this._analyzeChartViewModels.Add(this._analyzeAdvancedChartViewModel);
        }

        private void OnNavigateToEvent(object argument)
        {
            var navigationArgs = (NavigationArgs)argument;

            switch (navigationArgs.NavigationCategory)
            {
                case NavigationCategory.Measurement:
                case NavigationCategory.AnalyseGraph:
                    IsActive = true;
                    _analyzeMultipleChartsViewModel.IsActive = true;
                    IsOverlayActive = false;
                    IsMeanActive = false;
                    break;
                case NavigationCategory.AnalyseOverlay:
                    IsActive = true;
                    _analyzeMultipleChartsViewModel.IsActive = false;
                    IsOverlayActive = true;
                    IsMeanActive = false;
                    break;
                case NavigationCategory.AnalyseMean:
                    IsActive = true;
                    _analyzeMultipleChartsViewModel.IsActive = false;
                    IsOverlayActive = false;
                    IsMeanActive = true;
                    break;
                case NavigationCategory.AnalyseTable:
                    IsActive = false;
                    _analyzeMultipleChartsViewModel.IsActive = false;
                    IsOverlayActive = false;
                    IsMeanActive = false;
                    break;
                case NavigationCategory.Analyse:
                case NavigationCategory.MeasureResults:
                case NavigationCategory.Template:
                    break;
                default:
                    IsActive = false;
                    _analyzeMultipleChartsViewModel.IsActive = false;
                    IsOverlayActive = false;
                    IsMeanActive = false;
                    break;
            }           
        }
    }
}
