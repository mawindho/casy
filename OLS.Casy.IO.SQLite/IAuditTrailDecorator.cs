using OLS.Casy.IO.SQLite.Entities;
using System.Collections.Generic;

namespace OLS.Casy.IO.SQLite
{
    public interface IAuditTrailDecorator
    {
        object Context { get; set; }
        bool IsStandard { get; }
        AuditTrailEntryEntity CreateAddedAuditTrailEntity(object addedEntity, string userName, string machineName, string softwareVersion);
        List<AuditTrailEntryEntity> CreateModifiedAuditTrailEntity(object modifiedEntity, string userName, string machineName, string softwareVersion);
        AuditTrailEntryEntity CreateDeletedAuditTrailEntity(object deletedEntity, string userName, string machineName, string softwareVersion);
    }
}
