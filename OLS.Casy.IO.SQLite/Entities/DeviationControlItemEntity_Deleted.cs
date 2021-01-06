using System;
using OLS.Casy.Models.Enums;

namespace OLS.Casy.IO.SQLite.Entities
{
    public class DeviationControlItemEntity_Deleted
    {
        public DeviationControlItemEntity_Deleted()
        {
            DeletedAt = DateTimeOffset.MinValue.ToString();
        }
        public int Version { get; set; }
        public string DeletedBy { get; set; }
        public string DeletedAt { get; set; }
        public int DeviationControlItemEntityId { get; set; }
        public MeasureResultItemTypes MeasureResultItemType { get; set; }
        public double? MinLimit { get; set; }
        public double? MaxLimit { get; set; }
        public int MeasureSetupEntityId { get; set; }
        public virtual MeasureSetupEntity_Deleted MeasureSetupEntity { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedAt { get; set; }
        public string LastModifiedBy { get; set; }
        public string LastModifiedAt { get; set; }
    }
}
