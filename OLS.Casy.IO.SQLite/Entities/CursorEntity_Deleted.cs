using System;

namespace OLS.Casy.IO.SQLite.Entities
{
    public class CursorEntity_Deleted
    {
        public CursorEntity_Deleted()
        {
            DeletedAt = DateTimeOffset.MinValue.ToString();
        }
        public int Version { get; set; }
        public string DeletedBy { get; set; }
        public string DeletedAt { get; set; }
        public int CursorEntityId { get; set; }
        public string Name { get; set; }
        public double MinLimit { get; set; }
        public double MaxLimit { get; set; }
        public string Color { get; set; }
        public bool IsDeadCellsCursor { get; set; }
        public int MeasureSetupEntityId { get; set; }
        public virtual MeasureSetupEntity_Deleted MeasureSetupEntity { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedAt { get; set; }
        public string LastModifiedBy { get; set; }
        public string LastModifiedAt { get; set; }
        public string Subpopulation { get; set; }
    }
}
