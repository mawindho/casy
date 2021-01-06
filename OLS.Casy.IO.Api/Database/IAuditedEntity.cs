using System;

namespace OLS.Casy.IO.Api
{
    public interface IAuditedEntity
    {
        string CreatedBy { get; set; }
        string CreatedAt { get; set; }
        string LastModifiedBy { get; set; }
        string LastModifiedAt { get; set; }
        //string DeletedBy { get; set; }
        //DateTime DeletedAt { get; set; }
        int AssociatedMeasureResultEntityId { get; }
        int AssociatedTemplateEntityId { get; }
        int Version { get; set; }
    }
}
