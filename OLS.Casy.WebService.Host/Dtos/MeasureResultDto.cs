using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OLS.Casy.WebService.Host.Dtos
{
    public class MeasureResultDto
    {
        public string SerialNumber { get; set; }
        public string Comment { get; set; }
        public string Name { get; set; }
        public string Experiment { get; set; }
        public string Group { get; set; }
        public string CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string LastModifiedAt { get; set; }
        public string LastModifiedBy { get; set; }
        public Guid MeasureResultGuid { get; set; }
        public DateTime MeasuredAt { get; set; }
        public TimeZoneInfo MeasuredAtTimeZone { get; set; }
        public string Origin { get; set; }
        public bool IsCfr { get; set; }
        public string TemplateName { get; set; }
        public string MeasureMode { get; set; }
        public int CapillarySize { get; set; }
        public int FromDiameter { get; set; }
        public int ToDiameter { get; set; }
        public string Volume { get; set; }
        public double VolumeCorrectionFactor { get; set; }
        public int Repeats { get; set; }
        public double DilutionFactor { get; set; }
        public double DilutionSampleVolume { get; set; }
        public double DilutionCasyTonVolume { get; set; }
        public string AggregationCalculationMode { get; set; }
        public double ManualAggrgationCalculationFactor { get; set; }
        public bool IsSmoothing { get; set; }
        public double SmoothingFactor { get; set; }
        public bool IsDeviationControlEnabled { get; set; }
        public string ScalingMode { get; set; }
        public int ScalingMaxRange { get; set; }
        public string UnitMode { get; set; }
        public int ChannelCount { get; set; }
        public bool HasSubpopulations { get; set; }
        public string LastWeeklyClean { get; set; }
        public string Color { get; set; }
        public IEnumerable<RangeDto> Ranges { get; set; }
        public IEnumerable<MeasureResultDataDto> MeasureResultDataItems { get;set; }
        public IEnumerable<AuditTrailDto> AuditTrailItems { get; set; }
        public IEnumerable<DeviationControlDto> DeviationConrolItems { get; set; }
        public IEnumerable<MeasureResultCalculationDto> MeasureResultCalculations { get; set; }
    }
}
