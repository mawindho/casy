using OLS.Casy.IO.Api;
using OLS.Casy.Models.Enums;
using System;

namespace OLS.Casy.IO.SQLite.Entities
{
    public class DeviationControlItemEntity : IAuditedEntity
    {
        public DeviationControlItemEntity()
        {
            CreatedAt = DateTimeOffset.MinValue.ToString();
            LastModifiedAt = DateTimeOffset.MinValue.ToString();
        }

        public int Version { get; set; }
        public int DeviationControlItemEntityId { get; set; }
        public MeasureResultItemTypes MeasureResultItemType { get; set; }
        public double? MinLimit { get; set; }
        public double? MaxLimit { get; set; }
        public int MeasureSetupEntityId { get; set; }
        public virtual MeasureSetupEntity MeasureSetupEntity { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedAt { get; set; }
        public string LastModifiedBy { get; set; }
        public string LastModifiedAt { get; set; }
        public int AssociatedMeasureResultEntityId { get; set; }
        public int AssociatedTemplateEntityId => MeasureSetupEntityId;
    }
}
