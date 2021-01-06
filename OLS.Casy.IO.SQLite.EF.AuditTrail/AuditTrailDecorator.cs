using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data.Entity.Core;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using OLS.Casy.IO.Api;
using OLS.Casy.IO.SQLite.Entities;
//using System.Data.Entity;
//using System.Data.Entity.Core;
//using System.Data.Entity.Core.Objects;
//using System.Data.Entity.Infrastructure;

namespace OLS.Casy.IO.SQLite.EF.AuditTrail
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IAuditTrailDecorator))]
    public class AuditTrailDecorator : IAuditTrailDecorator
    {
        [ImportingConstructor]
        public AuditTrailDecorator()
        {
            
        }

        public object Context
        {
            get; set;

        }

        public bool IsStandard => false;

        public AuditTrailEntryEntity CreateAddedAuditTrailEntity(object addedEntityParam, string userName, string machineName, string softwareVersion)
        {
            var addedEntity = addedEntityParam as DbEntityEntry;
            int? entityId = null;
            bool isTemplate = false;
            if (((IAuditedEntity)addedEntity.Entity).AssociatedMeasureResultEntityId > 0)
            {
                entityId = ((IAuditedEntity)addedEntity.Entity).AssociatedMeasureResultEntityId;
            }
            else if (((IAuditedEntity)addedEntity.Entity).AssociatedTemplateEntityId > 0)
            {
                entityId = ((IAuditedEntity)addedEntity.Entity).AssociatedTemplateEntityId;
                isTemplate = true;
            }

            if (entityId.HasValue && entityId.Value > 0)
            {
                var entityName = addedEntity.Entity.GetType().Name;
                //var entityKey = GetKey(addedEntity.Entity);
                var entityKey = GetEntityKey(addedEntity.Entity);
                var primaryKey = entityKey == null ? string.Empty : entityKey.EntityKeyValues.Select(kv => kv.Value).Single();

                AuditTrailEntryEntity auditTrailEntry = new AuditTrailEntryEntity()
                {
                    ComputerName = machineName,
                    Action = "Added",
                    DateChanged = DateTimeOffset.UtcNow.ToString(),
                    EntityName = entityName,
                    MeasureResultEntityId = isTemplate ? null : entityId,
                    MeasureSetupEntityId = isTemplate ? entityId : null,
                    PrimaryKeyValue = primaryKey.ToString(),
                    SoftwareVersion = softwareVersion,
                    UserChanged = userName
                };
                return auditTrailEntry;
            }
            return null;
        }

        public List<AuditTrailEntryEntity> CreateModifiedAuditTrailEntity(object modifiedEntityParam, string userName, string machineName, string softwareVersion)
        {
            var modifiedEntity = modifiedEntityParam as DbEntityEntry;
            List<AuditTrailEntryEntity> result = new List<AuditTrailEntryEntity>();

            var entityName = modifiedEntity.Entity.GetType().Name;
            //var entityKey = GetKey(modifiedEntity.Entity);
            var entityKey = GetEntityKey(modifiedEntity.Entity);
            var primaryKey = entityKey == null ? string.Empty : entityKey.EntityKeyValues.Select(kv => kv.Value).Single();

            var properties = modifiedEntity.Entity.GetType().GetProperties();
            int? entityId = null;
            bool isTemplate = false;
            if (((IAuditedEntity) modifiedEntity.Entity).AssociatedMeasureResultEntityId > 0)
            {
                entityId = ((IAuditedEntity) modifiedEntity.Entity).AssociatedMeasureResultEntityId;
            }
            else if (((IAuditedEntity) modifiedEntity.Entity).AssociatedTemplateEntityId > 0)
            {
                entityId = ((IAuditedEntity) modifiedEntity.Entity).AssociatedTemplateEntityId;
                isTemplate = true;
            }

            if (entityId.HasValue && entityId.Value > 0)
            {
                foreach (var property in modifiedEntity.OriginalValues.PropertyNames.ToList())
                {
                    var dbProperty = modifiedEntity.Property(property);

                    var reflProp = properties.FirstOrDefault(p => p.Name == dbProperty.Name);
                    if (!reflProp.GetCustomAttributes(true).Any(attr => attr is IgnoreAuditTrailAttribute))
                    {
                        var originalValue = dbProperty.OriginalValue;
                        var currentValue = dbProperty.CurrentValue;

                        var oldValue = originalValue == null ? null : originalValue.ToString();
                        var newValue = currentValue == null ? null : currentValue.ToString();

                        if (oldValue != newValue) //Only create a log if the value changes
                        {
                            AuditTrailEntryEntity auditTrailEntry = new AuditTrailEntryEntity
                            {
                                EntityName = entityName,
                                PrimaryKeyValue = primaryKey.ToString(),
                                PropertyName = dbProperty.Name,
                                Action = "Modified",
                                OldValue = originalValue == null ? null : originalValue.ToString(),
                                NewValue = currentValue == null ? null : currentValue.ToString(),
                                DateChanged = DateTimeOffset.UtcNow.ToString(),
                                UserChanged = userName,
                                ComputerName = machineName,
                                SoftwareVersion = softwareVersion,
                                MeasureResultEntityId = isTemplate ? null : entityId,
                                MeasureSetupEntityId = isTemplate ? entityId : null
                            };
                            result.Add(auditTrailEntry);
                        }
                    }
                }
            }
            return result;
        }

        public AuditTrailEntryEntity CreateDeletedAuditTrailEntity(object deletedEntityParam, string userName, string machineName, string softwareVersion)
        {
            var deletedEntity = deletedEntityParam as DbEntityEntry;

            int? entityId = null;
            bool isTemplate = false;
            if (((IAuditedEntity)deletedEntity.Entity).AssociatedMeasureResultEntityId > 0)
            {
                entityId = ((IAuditedEntity)deletedEntity.Entity).AssociatedMeasureResultEntityId;
            }
            else if (((IAuditedEntity)deletedEntity.Entity).AssociatedTemplateEntityId > 0)
            {
                entityId = ((IAuditedEntity)deletedEntity.Entity).AssociatedTemplateEntityId;
                isTemplate = true;
            }

            if (entityId.HasValue && entityId.Value > 0)
            {
                var entityName = deletedEntity.Entity.GetType().Name;
                var entityKey = GetEntityKey(deletedEntity.Entity); //GetKey(deletedEntity.Entity);

                if (entityKey != null)
                {
                    var primaryKey = entityKey.EntityKeyValues.Select(kv => kv.Value).Single();

                    if (primaryKey != null)
                    {
                        AuditTrailEntryEntity auditTrailEntry = new AuditTrailEntryEntity()
                        {
                            EntityName = entityName,
                            PrimaryKeyValue = primaryKey.ToString(),
                            Action = "Deleted",
                            DateChanged = DateTimeOffset.UtcNow.ToString(),
                            UserChanged = userName,
                            ComputerName = machineName,
                            SoftwareVersion = softwareVersion,
                            MeasureResultEntityId = isTemplate ? null : entityId,
                            MeasureSetupEntityId = isTemplate ? entityId : null
                        };
                        return auditTrailEntry;
                    }
                }
            }
            return null;
        }

        private object _lock = new object();

        //public int GetKey<T>(T entity)
        //{
            //var keyName = Context.Model.FindEntityType(typeof(T)).FindPrimaryKey().Properties
                //.Select(x => x.Name).Single();

            //return (int)entity.GetType().GetProperty(keyName).GetValue(entity, null);
        //}

        private EntityKey GetEntityKey<T>(T entity)
            where T : class
        {
            lock (_lock)
            {
                if (Context is IObjectContextAdapter objectContextAdapter && objectContextAdapter.ObjectContext != null)
                {
                    var oc = objectContextAdapter.ObjectContext;
                    ObjectStateEntry ose;
                    if (null != entity && oc.ObjectStateManager != null &&
                        oc.ObjectStateManager.TryGetObjectStateEntry(entity, out ose))
                    {
                        return ose.EntityKey;
                    }
                }
            }

            return null;
        }
    }
}
