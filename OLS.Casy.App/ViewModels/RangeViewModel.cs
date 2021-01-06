using System;
using System.Collections.Generic;
using System.Text;
using OLS.Casy.App.Controls;
using OLS.Casy.App.ViewModels.Base;

namespace OLS.Casy.App.ViewModels
{
    public class RangeViewModel : ViewModelBase, IBindableGridItem
    {
        private double _widthPercentage;
        private bool _isTransparent;
        private string _rangeName;
        private string _subpopulation;
        private string _countsPerMl;
        private string _aggregationFactor;
        private string _countsPercentage;
        private string _countsPerMlA;
        private string _countsPerMlB;
        private string _countsPerMlC;
        private string _countsPerMlD;
        private string _countsPerMlE;
        private string _countsPercentageA;
        private string _countsPercentageE;
        private string _countsPercentageD;
        private string _countsPercentageC;
        private string _countsPercentageB;
        private string _volumePerMl;
        private string _peakVolume;
        private string _peakDiameter;
        private string _meanDiameter;
        private string _rangeSettings;

        public double WidthPercentage
        {
            get => _widthPercentage;
            set
            {
                _widthPercentage = value;
                RaisePropertyChanged(() => WidthPercentage);
            }
        }

        public bool IsTransparent
        {
            get => _isTransparent;
            set
            {
                _isTransparent = value;
                RaisePropertyChanged(() => IsTransparent);
            }
        }

        public string RangeName
        {
            get => _rangeName;
            set
            {
                _rangeName = value;
                RaisePropertyChanged(() => RangeName);
            }
        }

        public string Subpopulation
        {
            get => _subpopulation;
            set
            {
                _subpopulation = value;
                RaisePropertyChanged(() => Subpopulation);
                RaisePropertyChanged(() => HasSubpopulation);
                RaisePropertyChanged(() => SubPopAWidth);
                RaisePropertyChanged(() => SubPopBWidth);
                RaisePropertyChanged(() => SubPopCWidth);
                RaisePropertyChanged(() => SubPopDWidth);
                RaisePropertyChanged(() => SubPopEWidth);
            }
        }

        public bool HasSubpopulation => !string.IsNullOrEmpty(_subpopulation);
        public double SubPopAWidth => _subpopulation == "A" ? 65d : 0d;
        public double SubPopBWidth => _subpopulation == "B" ? 65d : 0d;
        public double SubPopCWidth => _subpopulation == "C" ? 65d : 0d;
        public double SubPopDWidth => _subpopulation == "D" ? 65d : 0d;
        public double SubPopEWidth => _subpopulation == "E" ? 65d : 0d;

        public string CountsPerMl
        {
            get => _countsPerMl;
            set
            {
                _countsPerMl = value;
                RaisePropertyChanged(() => CountsPerMl);
            }
        }

        public string AggregationFactor
        {
            get => _aggregationFactor;
            set
            {
                _aggregationFactor = value;
                RaisePropertyChanged(() => AggregationFactor);
            }
        }

        public string CountsPercentage
        {
            get => _countsPercentage;
            set
            {
                _countsPercentage = value;
                RaisePropertyChanged(() => CountsPercentage);
            }
        }

        public string CountsPerMlA
        {
            get => _countsPerMlA;
            set
            {
                _countsPerMlA = value;
                RaisePropertyChanged(() => CountsPerMlA);
            }
        }

        public string CountsPerMlB
        {
            get => _countsPerMlB;
            set
            {
                _countsPerMlB = value;
                RaisePropertyChanged(() => CountsPerMlB);
            }
        }

        public string CountsPerMlC
        {
            get => _countsPerMlC;
            set
            {
                _countsPerMlC = value;
                RaisePropertyChanged(() => CountsPerMlC);
            }
        }

        public string CountsPerMlD
        {
            get => _countsPerMlD;
            set
            {
                _countsPerMlD = value;
                RaisePropertyChanged(() => CountsPerMlD);
            }
        }

        public string CountsPerMlE
        {
            get => _countsPerMlE;
            set
            {
                _countsPerMlE = value;
                RaisePropertyChanged(() => CountsPerMlE);
            }
        }

        public string CountsPercentageA
        {
            get => _countsPercentageA;
            set
            {
                _countsPercentageA = value;
                RaisePropertyChanged(() => CountsPercentageA);
            }
        }

        public string CountsPercentageB
        {
            get => _countsPercentageB;
            set
            {
                _countsPercentageB = value;
                RaisePropertyChanged(() => CountsPercentageB);
            }
        }

        public string CountsPercentageC
        {
            get => _countsPercentageC;
            set
            {
                _countsPercentageC = value;
                RaisePropertyChanged(() => CountsPercentageC);
            }
        }

        public string CountsPercentageD
        {
            get => _countsPercentageD;
            set
            {
                _countsPercentageD = value;
                RaisePropertyChanged(() => CountsPercentageD);
            }
        }

        public string CountsPercentageE
        {
            get => _countsPercentageE;
            set
            {
                _countsPercentageE = value;
                RaisePropertyChanged(() => CountsPercentageE);
            }
        }

        public string VolumePerMl
        {
            get => _volumePerMl;
            set
            {
                _volumePerMl = value;
                RaisePropertyChanged(() => VolumePerMl);
            }
        }

        public string PeakVolume
        {
            get => _peakVolume;
            set
            {
                _peakVolume = value;
                RaisePropertyChanged(() => PeakVolume);
            }
        }

        public string PeakDiameter
        {
            get => _peakDiameter;
            set
            {
                _peakDiameter = value;
                RaisePropertyChanged(() => PeakDiameter);
            }
        }

        public string MeanDiameter
        {
            get => _meanDiameter;
            set
            {
                _meanDiameter = value;
                RaisePropertyChanged(() => MeanDiameter);
            }
        }

        public string RangeSettings
        {
            get => _rangeSettings;
            set
            {
                _rangeSettings = value;
                RaisePropertyChanged(() => RangeSettings);
            }
        }
    }
}
