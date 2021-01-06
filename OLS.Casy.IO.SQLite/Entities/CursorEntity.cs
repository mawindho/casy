using OLS.Casy.IO.Api;
using System;

namespace OLS.Casy.IO.SQLite.Entities
{
    public class CursorEntity : IAuditedEntity//, IDeletedFlagEntity
    {
        public CursorEntity()
        {
            CreatedAt = DateTimeOffset.MinValue.ToString();
            LastModifiedAt = DateTimeOffset.MinValue.ToString();
        }

        public int CursorEntityId { get; set; }

        public string Name { get; set; }
        public int Version { get; set; }
        public double MinLimit { get; set; }
        public double MaxLimit { get; set; }
        public string Color { get; set; }
        public bool IsDeadCellsCursor { get; set; }
        public int MeasureSetupEntityId { get; set; }
        public virtual MeasureSetupEntity MeasureSetupEntity { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedAt { get; set; }
        public string LastModifiedBy { get; set; }
        public string LastModifiedAt { get; set; }
        public string Subpopulation { get; set; }
        public int AssociatedMeasureResultEntityId { get; set; }
        public int AssociatedTemplateEntityId => MeasureSetupEntityId;
    }
}
