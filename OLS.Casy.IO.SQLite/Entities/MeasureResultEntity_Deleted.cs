using System;
using System.Collections.Generic;

namespace OLS.Casy.IO.SQLite.Entities
{
    public class MeasureResultEntity_Deleted
    {
        public MeasureResultEntity_Deleted()
        {
            MeasureResultDataEntities = new List<MeasureResultDataEntity_Deleted>();
            //MeasureResultAnnotationEntities = new List<MeasureResultAnnotationEntity>();
            AuditTrailEntrieEntities = new List<AuditTrailEntryEntity_Deleted>();
            //MeasureResultAccessMappings = new List<MeasureResultAccessMapping>();

            DeletedAt = DateTimeOffset.MinValue.ToString();
        }

        public int Version { get; set; }
        public string DeletedBy { get; set; }
        public string DeletedAt { get; set; }
        public int MeasureResultEntityId { get; set; }
        public Guid MeasureResultEntityGuid { get; set; }
        public string SerialNumber { get; set; }
        public string Comment { get; set; }
        public string Name { get; set; }
        public string Experiment { get; set; }
        public string Group { get; set; }
        public string Color { get; set; }
        
        //[ForeignKey("MeasureSetupEntityId")]
        public virtual MeasureSetupEntity_Deleted MeasureSetupEntity { get; set; }
        //public int MeasureSetupEntityId { get; set; }

        //[ForeignKey("OriginalMeasureSetupEntityId")]
        public virtual MeasureSetupEntity_Deleted OriginalMeasureSetupEntity { get; set; }
        //public int OriginalMeasureSetupEntityId { get; set; }

        public ICollection<MeasureResultDataEntity_Deleted> MeasureResultDataEntities { get; set; }
        //public ICollection<MeasureResultAnnotationEntity> MeasureResultAnnotationEntities { get; set; }
        public ICollection<AuditTrailEntryEntity_Deleted> AuditTrailEntrieEntities { get; set; }
        //public ICollection<MeasureResultAccessMapping> MeasureResultAccessMappings { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedAt { get; set; }
        public string LastModifiedBy { get; set; }
        public string LastModifiedAt { get; set; }
        public bool IsTemporary { get; set; }
        public DateTime MeasuredAt { get; set; }
        public string MeasuredAtTimeZone { get; set; }
        public string Origin { get; set; }
        public bool IsCfr { get; set; }
        public string LastWeeklyClean { get; set; }
    }
}
