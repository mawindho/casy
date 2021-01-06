using System;
using System.Collections.Generic;
using System.Text;
using Microcharts;
using OLS.Casy.App.ViewModels.Base;
using Xamarin.Forms;

namespace OLS.Casy.App.ViewModels
{
    public class ChartViewModel : ViewModelBase
    {
        private Chart _chart;
        private string _measurementName;
        public List<RangeViewModel> _rangeViewModels;
        private Color _color;
        private bool _hasComment;
        private string _comment;
        private bool _hasSubpopulations;
        private string _countsTitle;
        private string _percentageTitle;
        private double _subPopAWidth;
        private double _subPopBWidth;
        private double _subPopCWidth;
        private double _subPopDWidth;
        private double _subPopEWidth;
        private string _totalCountsLabel;
        private string _totalCounts;
        private string _totalCountsCursorLabel;
        private string _totalCountsCursor;
        private string _totalCountsCursorState;
        private string _countsAboveDiameterLabel;
        private string _countsAboveDiameter;
        private string _concentration;
        private List<ChartViewModel> _chartViewModels;

        public Chart Chart
        {
            get => _chart;
            set
            {
                _chart = value;
                RaisePropertyChanged(() => Chart);
            }
        }

        public string MeasurementName
        {
            get => _measurementName;
            set
            {
                _measurementName = value;
                RaisePropertyChanged(() => MeasurementName);
            }
        }

        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                RaisePropertyChanged(() => Color);
            }
        }

        public List<RangeViewModel> RangeViewModels
        {
            get => _rangeViewModels;
            set
            {
                _rangeViewModels = value;
                RaisePropertyChanged(() => RangeViewModels);
            }
        }

        public List<ChartViewModel> ChartViewModels
        {
            get => _chartViewModels;
            set
            {
                _chartViewModels = value;
                RaisePropertyChanged(() => ChartViewModels);
            }
        }

        public bool HasComment
        {
            get => _hasComment;
            set
            {
                _hasComment = value;
                RaisePropertyChanged(() => HasComment);
            }
        }

        public string Comment
        {
            get => _comment;
            set
            {
                _comment = value;
                RaisePropertyChanged(() => Comment);
            }
        }

        public string TotalCountsLabel
        {
            get => _totalCountsLabel;
            set
            {
                _totalCountsLabel = value;
                RaisePropertyChanged(() => TotalCountsLabel);
            }
        }

        public string TotalCounts
        {
            get => _totalCounts;
            set
            {
                _totalCounts = value;
                RaisePropertyChanged(() => TotalCounts);
            }
        }

        public string TotalCountsCursorLabel
        {
            get => _totalCountsCursorLabel;
            set
            {
                _totalCountsCursorLabel = value;
                RaisePropertyChanged(() => TotalCountsCursorLabel);
            }
        }

        public string TotalCountsCursor
        {
            get => _totalCountsCursor;
            set
            {
                _totalCountsCursor = value;
                RaisePropertyChanged(() => TotalCountsCursor);
            }
        }

        public string TotalCountsCursorState
        {
            get => _totalCountsCursorState;
            set
            {
                _totalCountsCursorState = value;
                RaisePropertyChanged(() => TotalCountsCursorState);
            }
        }

        public string CountsAboveDiameterLabel
        {
            get => _countsAboveDiameterLabel;
            set
            {
                _countsAboveDiameterLabel = value;
                RaisePropertyChanged(() => CountsAboveDiameterLabel);
            }
        }

        public string CountsAboveDiameter
        {
            get => _countsAboveDiameter;
            set
            {
                _countsAboveDiameter = value;
                RaisePropertyChanged(() => CountsAboveDiameter);
            }
        }

        public string Concentration
        {
            get => _concentration;
            set
            {
                _concentration = value;
                RaisePropertyChanged(() => Concentration);
            }
        }

        public bool HasSubpopulations
        {
            get => _hasSubpopulations;
            set
            {
                _hasSubpopulations = value;
                RaisePropertyChanged(() => HasSubpopulations);
            }
        }

        public string CountsTitle
        {
            get => _countsTitle;
            set
            {
                _countsTitle = value;
                RaisePropertyChanged(() => CountsTitle);
            }
        }

        public string PercentageTitle
        {
            get => _percentageTitle;
            set
            {
                _percentageTitle = value;
                RaisePropertyChanged(() => PercentageTitle);
            }
        }

        public double SubPopAWidth
        {
            get => _subPopAWidth;
            set
            {
                _subPopAWidth = value;
                RaisePropertyChanged(() => SubPopAWidth);
            }
        }

        public double SubPopBWidth
        {
            get => _subPopBWidth;
            set
            {
                _subPopBWidth = value;
                RaisePropertyChanged(() => SubPopBWidth);
            }
        }

        public double SubPopCWidth
        {
            get => _subPopCWidth;
            set
            {
                _subPopCWidth = value;
                RaisePropertyChanged(() => SubPopCWidth);
            }
        }

        public double SubPopDWidth
        {
            get => _subPopDWidth;
            set
            {
                _subPopDWidth = value;
                RaisePropertyChanged(() => SubPopDWidth);
            }
        }

        public double SubPopEWidth
        {
            get => _subPopEWidth;
            set
            {
                _subPopEWidth = value;
                RaisePropertyChanged(() => SubPopEWidth);
            }
        }
    }
}
