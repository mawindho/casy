using OLS.Casy.Models.Enums;
using System;
using System.Collections.Generic;

namespace OLS.Casy.IO.SQLite.Entities
{
    public class MeasureSetupEntity_Deleted
    {
        public MeasureSetupEntity_Deleted()
        {
            CursorEntities = new List<CursorEntity_Deleted>();
            DeviationControlItemEntities = new List<DeviationControlItemEntity_Deleted>();
            AuditTrailEntrieEntities = new List<AuditTrailEntryEntity_Deleted>();

            DeletedAt = DateTimeOffset.MinValue.ToString();
        }
        public string DeletedBy { get; set; }
        public string DeletedAt { get; set; }
        public int MeasureSetupEntityId { get; set; }
        public string Name { get; set; }
        public int Version { get; set; }
        public MeasureModes MeasureMode { get; set; }
        public int CapillarySize { get; set; }
        public int FromDiameter { get; set; }
        public int ToDiameter { get; set; }
        public Volumes Volume { get; set; }
        public double VolumeCorrectionFactor { get; set; }
        public int Repeats { get; set; }
        public double DilutionFactor { get; set; }
        public double DilutionSampleVolume { get; set; }
        public double DilutionCasyTonVolume { get; set; }
        public AggregationCalculationModes AggregationCalculationMode { get; set; }
        public double ManualAggrgationCalculationFactor { get; set; }
        public bool IsSmoothing { get; set; }
        public double SmoothingFactor { get; set; }
        public bool IsDeviationControlEnabled { get; set; }
        public ScalingModes ScalingMode { get; set; }
        public int ScalingMaxRange { get; set; }
        public UnitModes UnitMode { get; set; }
        public bool IsTemplate { get; set; }
        public bool IsReadOnly { get; set; }
        public string DefaultExperiment { get; set; }
        public string DefaultGroup { get; set; }
        public bool IsAutoSave { get; set; }
        public string AutoSaveName { get; set; }
        public bool IsAutoComment { get; set; }
        public int ChannelCount { get; set; }
        public bool HasSubpopulations { get; set; }
        public ICollection<CursorEntity_Deleted> CursorEntities { get; set; }
        public ICollection<DeviationControlItemEntity_Deleted> DeviationControlItemEntities { get; }
        public string CreatedBy { get; set; }
        public string CreatedAt { get; set; }
        public string LastModifiedBy { get; set; }
        public string LastModifiedAt { get; set; }
        public ICollection<AuditTrailEntryEntity_Deleted> AuditTrailEntrieEntities { get; set; }
    }
}
