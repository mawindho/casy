using OLS.Casy.Core;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Models;
using OLS.Casy.Ui.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace OLS.Casy.Ui.Core.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(MeasureResultDataViewModel))]
    public class MeasureResultDataViewModel : Base.ViewModelBase, IDisposable
    {
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly ICompositionFactory _compositionFactory;
        private ListCollectionView _cursorResultDataViewSource;

        private MeasureResult _measureResult;
        private MeasureSetup _specializedMeasureSetup;

        private string _totalCountsPerMl;
        private string _totalVolPerMl;
        private string _percentageTitle;
        private string _countsTitle;
        private string _toDiameter;
        private bool _isVisible = true;
        private bool _hasSubpopulations;
        private bool _hasSubpopulationE;
        private bool _hasSubpopulationD;
        private bool _hasSubpopulationC;
        private bool _hasSubpopulationB;
        private bool _hasSubpopulationA;
        private GridLength _aggregationFactorColumnWidth;
        private string _totalCountsPerMlA;
        private string _totalCountsPerMlB;
        private string _totalCountsPerMlC;
        private string _totalCountsPerMlD;
        private string _totalCountsPerMlE;
        private string _totalCountsPercentageE;
        private string _totalCountsPercentageD;
        private string _totalCountsPercentageC;
        private string _totalCountsPercentageB;
        private string _totalCountsPercentageA;

        [ImportingConstructor]
        public MeasureResultDataViewModel(IEventAggregatorProvider eventAggregatorProvider,
            ICompositionFactory compositionFactory)
        {
            _eventAggregatorProvider = eventAggregatorProvider;
            _compositionFactory = compositionFactory;

            CursorResultDataViewModels = new SmartCollection<CursorResultDataViewModel>();
            CursorResultDataViewModelExports = new List<Lazy<CursorResultDataViewModel>>();
        }

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (value == _isVisible) return;
                _isVisible = value;
                NotifyOfPropertyChange();
            }
        }

        public MeasureResult MeasureResult
        {
            get => _measureResult;
            set
            {
                if (value == _measureResult) return;
                if (_measureResult != null)
                {
                    _measureResult.PropertyChanged -= OnMeasureResultPropertyChanged;
                }

                _measureResult = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange("Comment");
                NotifyOfPropertyChange("Color");
                NotifyOfPropertyChange("MeasureResultName");

                if(_measureResult != null)
                {
                    _measureResult.PropertyChanged += OnMeasureResultPropertyChanged;
                }
            }
        }

        public MeasureSetup SpecializedMeasureSetup
        {
            get => _specializedMeasureSetup;
            set
            {
                if (value == _specializedMeasureSetup) return;
                _specializedMeasureSetup = value;
                NotifyOfPropertyChange();
            }
        }

        public SmartCollection<CursorResultDataViewModel> CursorResultDataViewModels { get; }

        public ListCollectionView CursorResultDataViewSource
        {
            get
            {
                if (_cursorResultDataViewSource != null) return _cursorResultDataViewSource;
                _cursorResultDataViewSource = CollectionViewSource.GetDefaultView(CursorResultDataViewModels) as ListCollectionView;

                if (_cursorResultDataViewSource == null) return _cursorResultDataViewSource;
                _cursorResultDataViewSource.SortDescriptions.Add(new SortDescription("MinLimit",
                    ListSortDirection.Ascending));
                _cursorResultDataViewSource.IsLiveSorting = true;
                return _cursorResultDataViewSource;
            }
        }

        public List<Lazy<CursorResultDataViewModel>> CursorResultDataViewModelExports { get; }

        public string MeasureResultName => this._measureResult.Name;

        public string Comment => this._measureResult.Comment;

        public bool HasComment => !string.IsNullOrEmpty(this.Comment);

        public string Color => this._measureResult.Color;

        public GridLength AggregationFactorColumnWidth
        {
            get => _aggregationFactorColumnWidth;
            set
            {
                if (value == _aggregationFactorColumnWidth) return;
                _aggregationFactorColumnWidth = value;
                NotifyOfPropertyChange();
            }
        }

        public string PercentageTitle
        {
            get => this._percentageTitle;
            set
            {
                if (value == _percentageTitle) return;
                _percentageTitle = value;
                NotifyOfPropertyChange();
            }
        }

        public string CountsTitle
        {
            get => this._countsTitle;
            set
            {
                if (value == _countsTitle) return;
                _countsTitle = value;
                NotifyOfPropertyChange();
            }
        }

        public string TotalCountsPerMl
        {
            get => _totalCountsPerMl;
            set
            {
                if (value == _totalCountsPerMl) return;
                _totalCountsPerMl = value;
                NotifyOfPropertyChange();
            }
        }

        public string TotalVolPerMl
        {
            get => _totalVolPerMl;
            set
            {
                if (value == _totalVolPerMl) return;
                _totalVolPerMl = value;
                NotifyOfPropertyChange();
            }
        }

        public string ToDiameter
        {
            get => _toDiameter;
            set
            {
                if (value == _toDiameter) return;
                _toDiameter = value;
                NotifyOfPropertyChange();
            }
        }

        public bool HasSubpopulations
        {
            get => _hasSubpopulations;
            set
            {
                if (value == _hasSubpopulations) return;
                _hasSubpopulations = value;
                NotifyOfPropertyChange();
            }
        }

        public string TotalCountsPerMlA
        {
            get => _totalCountsPerMlA;
            set
            {
                if (value == _totalCountsPerMlA) return;
                _totalCountsPerMlA = value;
                NotifyOfPropertyChange();
            }
        }

        public string TotalCountsPerMlB
        {
            get => _totalCountsPerMlB;
            set
            {
                if (value == _totalCountsPerMlB) return;
                _totalCountsPerMlB = value;
                NotifyOfPropertyChange();
            }
        }

        public string TotalCountsPerMlC
        {
            get => _totalCountsPerMlC;
            set
            {
                if (value == _totalCountsPerMlC) return;
                _totalCountsPerMlC = value;
                NotifyOfPropertyChange();
            }
        }

        public string TotalCountsPerMlD
        {
            get => _totalCountsPerMlD;
            set
            {
                if (value == _totalCountsPerMlD) return;
                _totalCountsPerMlD = value;
                NotifyOfPropertyChange();
            }
        }

        public string TotalCountsPerMlE
        {
            get => _totalCountsPerMlE;
            set
            {
                if (value == _totalCountsPerMlE) return;
                _totalCountsPerMlE = value;
                NotifyOfPropertyChange();
            }
        }

        public string TotalCountsPercentageA
        {
            get => _totalCountsPercentageA;
            set
            {
                if (value == _totalCountsPercentageA) return;
                _totalCountsPercentageA = value;
                NotifyOfPropertyChange();
            }
        }

        public string TotalCountsPercentageB
        {
            get => _totalCountsPercentageB;
            set
            {
                if (value == _totalCountsPercentageB) return;
                _totalCountsPercentageB = value;
                NotifyOfPropertyChange();
            }
        }

        public string TotalCountsPercentageC
        {
            get => _totalCountsPercentageC;
            set
            {
                if (value == _totalCountsPercentageC) return;
                _totalCountsPercentageC = value;
                NotifyOfPropertyChange();
            }
        }

        public string TotalCountsPercentageD
        {
            get => _totalCountsPercentageD;
            set
            {
                if (value == _totalCountsPercentageD) return;
                _totalCountsPercentageD = value;
                NotifyOfPropertyChange();
            }
        }

        public string TotalCountsPercentageE
        {
            get => _totalCountsPercentageE;
            set
            {
                if (value == _totalCountsPercentageE) return;
                _totalCountsPercentageE = value;
                NotifyOfPropertyChange();
            }
        }

        public bool HasSubpopulationA
        {
            get => _hasSubpopulationA;
            set
            {
                if (value != _hasSubpopulationA)
                {
                    _hasSubpopulationA = value;
                    NotifyOfPropertyChange();
                }

                if (!_hasSubpopulationA) return;
                foreach (var cursorViewModel in CursorResultDataViewModels)
                {
                    if (cursorViewModel.Cursor.Subpopulation == "A")
                    {
                        cursorViewModel.Subpopulation = "A";
                    }
                }
            }
        }

        public bool HasSubpopulationB
        {
            get => _hasSubpopulationB;
            set
            {
                if (value != _hasSubpopulationB)
                {
                    _hasSubpopulationB = value;
                    NotifyOfPropertyChange();
                }

                if (!_hasSubpopulationB) return;
                foreach (var cursorViewModel in CursorResultDataViewModels)
                {
                    if (cursorViewModel.Cursor.Subpopulation == "B")
                    {
                        cursorViewModel.Subpopulation = "B";
                    }
                }
            }
        }

        public bool HasSubpopulationC
        {
            get => _hasSubpopulationC;
            set
            {
                if (value != _hasSubpopulationC)
                {
                    _hasSubpopulationC = value;
                    NotifyOfPropertyChange();
                }

                if (!_hasSubpopulationC) return;
                foreach (var cursorViewModel in CursorResultDataViewModels)
                {
                    if (cursorViewModel.Cursor.Subpopulation == "C")
                    {
                        cursorViewModel.Subpopulation = "C";
                    }
                }
            }
        }

        public bool HasSubpopulationD
        {
            get => _hasSubpopulationD;
            set
            {
                if (value != _hasSubpopulationD)
                {
                    _hasSubpopulationD = value;
                    NotifyOfPropertyChange();
                }

                if (!_hasSubpopulationD) return;
                foreach (var cursorViewModel in CursorResultDataViewModels)
                {
                    if (cursorViewModel.Cursor.Subpopulation == "D")
                    {
                        cursorViewModel.Subpopulation = "D";
                    }
                }
            }
        }

        public bool HasSubpopulationE
        {
            get => _hasSubpopulationE;
            set
            {
                if (value != _hasSubpopulationE)
                {
                    _hasSubpopulationE = value;
                    NotifyOfPropertyChange();
                }

                if (!_hasSubpopulationE) return;
                foreach (var cursorViewModel in CursorResultDataViewModels)
                {
                    if (cursorViewModel.Cursor.Subpopulation == "E")
                    {
                        cursorViewModel.Subpopulation = "E";
                    }
                }
            }
        }

        public ICommand DeleteCursorCommand => new OmniDelegateCommand<Casy.Models.Cursor>(OnDeleteCursor);

        public ICommand ToggleVisibilityCommand => new OmniDelegateCommand<Casy.Models.Cursor>(OnToggleCursorVisibilty); 

        private async void OnDeleteCursor(Casy.Models.Cursor cursorToDelete)
        {
            if (_measureResult != null)
            {
                await Task.Factory.StartNew(() =>
                {
                    var awaiter = new System.Threading.ManualResetEvent(false);

                    var wrapper = new ShowMessageBoxDialogWrapper()
                    {
                        Awaiter = awaiter,
                        Title = "DeleteCursorDialog_Title",
                        Message = "DeleteCursorDialog_Content"
                    };

                    _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>().Publish(wrapper);

                    if (!awaiter.WaitOne()) return;
                    if (!wrapper.Result) return;

                    _eventAggregatorProvider.Instance.GetEvent<DeleteCursorEvent>().Publish(
                        SpecializedMeasureSetup != null
                            ? new Tuple<object, object>(SpecializedMeasureSetup, cursorToDelete)
                            : new Tuple<object, object>(_measureResult.MeasureSetup, cursorToDelete));
                });
            }
        }

        private void OnToggleCursorVisibilty(Casy.Models.Cursor cursor)
        {
            cursor.IsVisible = !cursor.IsVisible;
        }

        private void OnMeasureResultPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case "Comment":
                    NotifyOfPropertyChange("Comment");
                    NotifyOfPropertyChange("HasComment");
                    break;
                case "Color":
                    NotifyOfPropertyChange("Color");
                    break;
                case "Name":
                    NotifyOfPropertyChange("MeasureResultName");
                    break;
            }
        }

        #region IDisposable Support
        private bool _disposedValue; // To detect redundant calls
        
        protected override void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if(_measureResult != null)
                    {
                        _measureResult.PropertyChanged -= OnMeasureResultPropertyChanged;
                    }

                    lock (CursorResultDataViewModelExports)
                    {
                        var toRemove = CursorResultDataViewModelExports.ToArray();
                        foreach (var export in toRemove)
                        {
                            export.Value.Dispose();
                            _compositionFactory.ReleaseExport(export);
                        }
                    }
                }

                _disposedValue = true;
            }
            base.Dispose(disposing);
        }
        #endregion
    }
}
