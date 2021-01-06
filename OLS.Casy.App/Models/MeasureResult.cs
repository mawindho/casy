using System;
using System.Collections.Generic;
using OLS.Casy.App.Models.Enums;
using Xamarin.Forms;

namespace OLS.Casy.App.Models
{
    public class MeasureResult : BindableObject, ITreeItem
    {
        private bool _isSelected;

        public int Id { get; set; }
        public string Name { get; set; }
        public string Group { get; set; }
        public string Experiment { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public string Comment { get; internal set; }
        public string SerialNumber { get; internal set; }
        public double ManualAggrgationCalculationFactor { get; set; }
        public List<AuditTrailItem> AuditTrailItems { get; internal set; }
        public object CapillarySize { get; internal set; }
        public int ChannelCount { get; internal set; }
        public DateTimeOffset CreatedAt { get; internal set; }
        public string CreatedBy { get; internal set; }
        public bool IsDeviationControlEnabled { get; internal set; }
        public List<DeviationControlItem> DeviationConrolItems { get; internal set; }
        public double DilutionCasyTonVolume { get; set; }
        public double DilutionFactor { get; internal set; }
        public double DilutionSampleVolume { get; internal set; }
        public int FromDiameter { get; internal set; }
        public int ToDiameter { get; internal set; }
        public bool HasSubpopulations { get; internal set; }
        public bool IsCfr { get; internal set; }
        public bool IsSmoothing { get; internal set; }
        public double SmoothingFactor { get; internal set; }
        public DateTimeOffset LastModifiedAt { get; internal set; }
        public string LastModifiedBy { get; internal set; }
        public DateTime MeasuredAt { get; internal set; }
        public TimeZoneInfo MeasuredAtTimeZone { get; internal set; }
        public MeasureModes MeasureMode { get; internal set; }
        public List<MeasureResultData> MeasureResultDataItems { get; internal set; }
        public Guid MeasureResultGuid { get; internal set; }
        public string Origin { get; internal set; }
        public List<Range> Ranges { get; internal set; }
        public int Repeats { get; set; }
        public int ScalingMaxRange { get; internal set; }
        public ScalingModes ScalingMode { get; internal set; }
        public string TemplateName { get; internal set; }
        public UnitModes UnitMode { get; internal set; }
        public Volumes Volume { get; internal set; }
        public double VolumeCorrectionFactor { get; internal set; }
        public DateTimeOffset LastWeeklyClean { get; internal set; }
        public string Color { get; internal set; }
        public List<MeasureResultCalculation> MeasureResultCalculations { get; internal set; }
        public AggregationCalculationModes AggregationCalculationMode { get; internal set; }
    }
}
