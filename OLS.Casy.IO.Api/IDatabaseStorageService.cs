using OLS.Casy.Core.Api;
using OLS.Casy.Models;
using System;
using System.Collections.Generic;

namespace OLS.Casy.IO.Api
{
    /// <summary>
    /// Interface providing I/O operation with a database
    /// </summary>
    public interface IDatabaseStorageService : ICoreService
    {
        IEnumerable<User> GetUsers();
        User GetUser(int userId);
        User GetUserByName(string name);
        void DeleteUser(User user);
        void SaveUser(User user);

        IEnumerable<UserRole> GetUserRoles();
        void SaveUserRole(UserRole userRole);
        UserRole GetUserRole(int userRoleId);

        UserGroup GetUserGroup(int userGroupId);
        IEnumerable<UserGroup> GetUserGroups();
        void SaveUserGroup(UserGroup userGroup);

        IEnumerable<MeasureSetup> GetMeasureSetupTemplates();
        IEnumerable<MeasureSetup> GetPredefinedTemplates();
        MeasureSetup GetPredefinedTemplate(string name, int capillarySize);

        MeasureSetup GetMeasureSetup(int measureSetupId, bool ignoreCache = false);
        void DeleteMeasureSetup(MeasureSetup measureSetup);
        void SaveMeasureSetup(MeasureSetup measureSetup, bool ignoreAuditTrail = false);

        IEnumerable<Tuple<string, int, int>> GetExperiments(string filter = "", bool includeDeleted = false);
        IEnumerable<Tuple<string, int>> GetGroups(string experiment, string filter = "", bool includeDeleted = false);
        IEnumerable<MeasureResult> GetMeasureResults(string experiment, string group, string filter = "", bool includeDeleted = false, bool nullAsNoValue = false, int maxItems = -1);
        MeasureResult GetMeasureResult(int measureResultId, bool ignoreCache = false, bool isDeleted = false);
        MeasureResult GetMeasureResultByGuid(Guid guid);
        IEnumerable<MeasureResult> GetTemporaryMeasureResults(User loggedInUser);

        bool MeasureResultExists(string name, string experiment, string group);
        void DeleteMeasureResults(IList<MeasureResult> measureResults);
        void SaveMeasureResult(MeasureResult measureResult, bool ignoreAuditTrail = false, bool storeExistingAuditTrail = false);
        void RemoveDeletedMeasureResult(int deletedMeasureResultId);

        MeasureResult LoadDisplayData(MeasureResult measureResult);
        MeasureResult LoadExportData(MeasureResult measureResult);
        MeasureSetup LoadExportData(MeasureSetup measureSetup);

        Cursor GetCursor(int cursorId);
        void DeleteCursor(Cursor cursor);

        IEnumerable<ErrorDetails> GetErrorDetails();
        void SaveErrorDetails(ErrorDetails errorDetails);

        void SaveAnnotationType(AnnotationType annotationType);
        IEnumerable<AnnotationType> GetAnnotationTypes();
        void DeleteAnnotationType(AnnotationType annotationType);

        void SaveMeasureResultAnnotation(MeasureResultAnnotation measureResultAnnotation, bool ignoreAuditTrail = false);

        Dictionary<string, Setting> GetSettings();
        void SaveSetting(string key, string value);
        void SaveSetting(string key, byte[] value);

        event EventHandler OnDatabaseReadyEvent;

        bool IsDatabaseReady { get; }

        void CreateBackupFile(string fileName);
        bool RestoreBackupFile(string fileName);

        void SaveAuditTrailEntry(AuditTrailEntry auditTrailEntry);

        void Cleanup(int olderThanDays);

        /*
        /// <summary>
        /// Save all changes to the database
        /// </summary>
        void SaveChanges();

        /// <summary>
        /// Check if the passed entity has been changed compared with the version currently stored in the database
        /// </summary>
        /// <typeparam name="TEntity">Type of the entity to be checked</typeparam>
        /// <param name="entity">Entity to check</param>
        /// <returns>True, if there are changes for the passed entity, false otherwise</returns>
        bool HasChanges<TEntity>(TEntity entity) where TEntity : class;

        IEnumerable<object> GetEntitiesWithChanges();

        /// <summary>
        /// Returns the entity of passed entity type with passed entity id stored in the database or NULL if no entity could be found.
        /// </summary>
        /// <typeparam name="TEntity">Type of the entity</typeparam>
        /// <param name="entityId">Id of the entity</param>
        /// <returns>Entity of the passed type with passed id stored in the database or NULL</returns>
        TEntity GetEntityById<TEntity>(int entityId, bool asNoTracking = false) where TEntity : class;

        TEntity GetEntityById<TEntity, TProperty>(int entityId, Expression<Func<TEntity, TProperty>> includeFunctionExpression, bool asNoTracking = false) where TEntity : class;

        TEntity GetEntityById<TEntity, TProperty, TProperty2>(int entityId, Expression<Func<TEntity, TProperty>> includeFunctionExpression, Expression<Func<TEntity, TProperty2>> includeFunctionExpression2, bool asNoTracking = false) where TEntity : class;

        TEntity GetEntityById<TEntity, TProperty, TProperty2, TProperty3>(int entityId, Expression<Func<TEntity, TProperty>> includeFunctionExpression, Expression<Func<TEntity, TProperty2>> includeFunctionExpression2, Expression<Func<TEntity, TProperty3>> includeFunctionExpression3, bool asNoTracking = false) where TEntity : class;

        TEntity GetEntityById<TEntity, TProperty, TProperty2, TProperty3, TProperty4>(int entityId, Expression<Func<TEntity, TProperty>> includeFunctionExpression, Expression<Func<TEntity, TProperty2>> includeFunctionExpression2, Expression<Func<TEntity, TProperty3>> includeFunctionExpression3, Expression<Func<TEntity, TProperty4>> includeFunctionExpression4, bool asNoTracking = false) where TEntity : class;

        //TEntity LoadInclude<TEntity, TProperty>(TEntity result, Expression<Func<TEntity, TProperty>> includeFunctionExpression);

        /// <summary>
        /// Returns all entities of the passed entity type stored in the database.
        /// </summary>
        /// <typeparam name="TEntity">Type of the entities</typeparam>
        /// <returns>List of all entities of the passed type stored in the database</returns>
        IList<TEntity> GetAllEntities<TEntity>(Expression<Func<TEntity, bool>> whereClause = null) where TEntity : class;

        /// <summary>
        /// Returns all entities of the passed entity type stored in the database and also fills navigation properties of the passed property type by resolving their foreign key relations
        /// </summary>
        /// <typeparam name="TEntity">Type of the entities</typeparam>
        /// <typeparam name="TProperty">Type of the navigation properties</typeparam>
        /// <param name="includeFunction"><see cref="Func{T, TResult}"/> representing the property access to the navigation property</param>
        /// <returns>List of all entities of the passed type with filled navigation properties of passed type stored in the database</returns>
        IList<TEntity> GetAllEntities<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> includeFunctionExpression, Expression<Func<TEntity, bool>> whereClause = null) where TEntity : class;

        /// <summary>
        /// Adds the passed entity to the database.
        /// </summary>
        /// <typeparam name="TEntity">Type of the eneity to store</typeparam>
        /// <param name="entity">Entity to store</param>
        void AddEntity<TEntity>(TEntity entity) where TEntity : class;

        /// <summary>
        /// Removes the passed entity from the database.
        /// </summary>
        /// <typeparam name="TEntity">Type of the entity to be removed</typeparam>
        /// <param name="entity">Entity to be removed</param>
        void RemoveEntity<TEntity>(TEntity entity) where TEntity : class;

        

        void RollBack<TEntity>() where TEntity : class;
        */
    }
}
