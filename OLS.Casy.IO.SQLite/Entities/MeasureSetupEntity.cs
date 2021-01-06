using System;
using OLS.Casy.IO.Api;
using System.Collections.Generic;
using OLS.Casy.Models.Enums;

namespace OLS.Casy.IO.SQLite.Entities
{
    public class MeasureSetupEntity : IAuditedEntity
    {
        private int _associatedMeasureResultEntityId;

        public MeasureSetupEntity()
        {
            CursorEntities = new List<CursorEntity>();
            DeviationControlItemEntities = new List<DeviationControlItemEntity>();
            AuditTrailEntrieEntities = new List<AuditTrailEntryEntity>();

            CreatedAt = DateTimeOffset.MinValue.ToString();
            LastModifiedAt = DateTimeOffset.MinValue.ToString();
        }

        //[Key]
        public int MeasureSetupEntityId { get; set; }
        public string Name { get; set; }
        public int Version { get; set; }

        //[Required]
        public MeasureModes MeasureMode { get; set; }

        //[Required]
        public int CapillarySize { get; set; }

        //[Required]
        public int FromDiameter { get; set; }

        //[Required]
        public int ToDiameter { get; set; }

        //[Required]
        public Volumes Volume { get; set; }

        //[Required]
        public double VolumeCorrectionFactor { get; set; }

        //[Required]
        public int Repeats { get; set; }

        //[Required]
        public double DilutionFactor { get; set; }
        public double DilutionSampleVolume { get; set; }
        public double DilutionCasyTonVolume { get; set; }

        //[Required]
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

        //[Required]
        public int ChannelCount { get; set; }
        public bool HasSubpopulations { get; set; }
        public ICollection<CursorEntity> CursorEntities { get; set; }
        public ICollection<DeviationControlItemEntity> DeviationControlItemEntities { get; }
        public string CreatedBy { get; set; }
        public string CreatedAt { get; set; }
        public string LastModifiedBy { get; set; }
        public string LastModifiedAt { get; set; }
        public ICollection<AuditTrailEntryEntity> AuditTrailEntrieEntities { get; set; }

        public int AssociatedMeasureResultEntityId
        {
            get => IsTemplate ? -1 : _associatedMeasureResultEntityId;
            set => _associatedMeasureResultEntityId = value;
        }

        public int AssociatedTemplateEntityId => MeasureSetupEntityId;
    }
}
