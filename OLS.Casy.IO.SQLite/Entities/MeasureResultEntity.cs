using OLS.Casy.IO.Api;
using System;
using System.Collections.Generic;

namespace OLS.Casy.IO.SQLite.Entities
{
    public class MeasureResultEntity : IAuditedEntity//, IDeletedFlagEntity
    {
        public MeasureResultEntity()
        {
            MeasureResultDataEntities = new List<MeasureResultDataEntity>();
            MeasureResultAnnotationEntities = new List<MeasureResultAnnotationEntity>();
            AuditTrailEntrieEntities = new List<AuditTrailEntryEntity>();
            MeasureResultAccessMappings = new List<MeasureResultAccessMapping>();
            IsTemporary = true;

            CreatedAt = DateTimeOffset.MinValue.ToString();
            LastModifiedAt = DateTimeOffset.MinValue.ToString();
        }

        public int MeasureResultEntityId { get; set; }
        public int Version { get; set; }
        public Guid MeasureResultEntityGuid { get; set; }
        public string SerialNumber { get; set; }
        public string Comment { get; set; }
        public string Name { get; set; }
        public string Experiment { get; set; }
        public string Group { get; set; }
        public string Color { get; set; }

        //[ForeignKey("MeasureSetupEntityId")]
        public virtual MeasureSetupEntity MeasureSetupEntity { get; set; }
        //public int MeasureSetupEntityId { get; set; }
        
        //[ForeignKey("OriginalMeasureSetupEntityId")]
        public virtual MeasureSetupEntity OriginalMeasureSetupEntity { get; set; }
        //public int OriginalMeasureSetupEntityId { get; set; }
        public ICollection<MeasureResultDataEntity> MeasureResultDataEntities { get; set; }
        public ICollection<MeasureResultAnnotationEntity> MeasureResultAnnotationEntities { get; set; }
        public ICollection<AuditTrailEntryEntity> AuditTrailEntrieEntities { get; set; }
        public ICollection<MeasureResultAccessMapping> MeasureResultAccessMappings { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedAt { get; set; }
        public string LastModifiedBy { get; set; }
        public string LastModifiedAt { get; set; }

        [IgnoreAuditTrail]
        public bool IsTemporary { get; set; }
        public int AssociatedMeasureResultEntityId => this.MeasureResultEntityId;
        public int AssociatedTemplateEntityId => -1;

        public DateTime MeasuredAt { get; set; }
        public string MeasuredAtTimeZone { get; set; }
        public string Origin { get; set; }
        public bool IsCfr { get; set; }
        public string LastWeeklyClean { get; set; }
    }
}
