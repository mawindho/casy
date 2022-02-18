using OLS.Casy.Core;
using OLS.Casy.Core.Config.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.IO.Api;
using OLS.Casy.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace OLS.Casy.IO
{
    /// <summary>
    /// Implementation of <see cref="IDatabaseStorageService"/>.
    /// Only acts as wrapper passing the commands to currently active <see cref="IDatabaseStorageProvider"/> 
    /// </summary>
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IDatabaseStorageService))]
    public class DatabaseStorageService : AbstractService, IDatabaseStorageService, IPartImportsSatisfiedNotification
    {
        private readonly IEnumerable<IDatabaseStorageProvider> _databaseStorageProviders;
        private IDatabaseStorageProvider _activeDatabaseStorageProvider;

        /// <summary>
        /// MEF importing constructor
        /// </summary>
        /// <param name="databaseStorageProviders">One instance of all known implementations of <see cref="IDatabaseStorageProvider"/> </param>
        [ImportingConstructor]
        public DatabaseStorageService(IConfigService configService, 
            [ImportMany]IEnumerable<IDatabaseStorageProvider> databaseStorageProviders)
            :base(configService)
        {
            this._databaseStorageProviders = databaseStorageProviders;
        }

        /// <summary>
        /// Method is called after all MEF imports has been fullfilled
        /// </summary>
        public void OnImportsSatisfied()
        {
            _activeDatabaseStorageProvider = _databaseStorageProviders.First(x => x.ProviderVersion == _databaseStorageProviders.Max(y => y.ProviderVersion));

            if (_activeDatabaseStorageProvider.IsEmpty && _databaseStorageProviders.Count() > 1)
            {
                var prevProvider = _databaseStorageProviders.FirstOrDefault(x =>
                    x.ProviderVersion == _activeDatabaseStorageProvider.ProviderVersion - 1);

                if (prevProvider == null || prevProvider.IsEmpty)
                {
                    throw new Exception("Unable to migrate database to current provider version.");
                }

                //prevProvider.CreateMigrationDatabase();

                var showProgressWrapper = new ShowProgressDialogWrapper
                {
                    Title = "Database migration required",
                    Message = "Migrating database. This might take some time ...{0}{1}{2}{3}{4}{5}{6}"
                };

                showProgressWrapper.MessageParameter = new[] { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };
                showProgressWrapper.IsFinished = false;

                Globals.ShowSplashProgressDialogDelegate(showProgressWrapper);

                var errorDetails = prevProvider.GetErrorDetails();
                var errorDetailsCount = errorDetails.Count();
                showProgressWrapper.MessageParameter[0] = $"\nMigrating Error Details (0 of {errorDetailsCount} entities)";
                Globals.ShowSplashProgressDialogDelegate(showProgressWrapper);

                int i = 0;
                foreach (var errorDetail in errorDetails)
                {
                    _activeDatabaseStorageProvider.SaveErrorDetails(errorDetail);
                    i++;
                    showProgressWrapper.MessageParameter[0] = $"\nMigrating Error Details ({i} of {errorDetailsCount} entities)";
                    Globals.ShowSplashProgressDialogDelegate(showProgressWrapper);
                }

                var annotationTypes = prevProvider.GetAnnotationTypes();
                var annotationTypesCount = annotationTypes.Count();
                showProgressWrapper.MessageParameter[1] = $"\nMigrating Annotation Types (0 of {annotationTypesCount} entities)";
                Globals.ShowSplashProgressDialogDelegate(showProgressWrapper);

                i = 0;
                foreach (var annotationType in annotationTypes)
                {
                    _activeDatabaseStorageProvider.SaveAnnotationType(annotationType);
                    i++;

                    showProgressWrapper.MessageParameter[1] = $"\nMigrating Annotation Types ({i} of {annotationTypesCount} entities)";
                    Globals.ShowSplashProgressDialogDelegate(showProgressWrapper);
                }

                var settings = prevProvider.GetSettings();
                var settingsCount = settings.Count();
                showProgressWrapper.MessageParameter[2] = $"\nMigrating Settings (0 of {settingsCount} entities)";
                Globals.ShowSplashProgressDialogDelegate(showProgressWrapper);

                i = 0;
                foreach (var setting in prevProvider.GetSettings())
                {
                    if (string.IsNullOrEmpty(setting.Value.Value))
                    {
                        _activeDatabaseStorageProvider.SaveSetting(setting.Key, setting.Value.BlobValue);
                    }
                    else
                    {
                        _activeDatabaseStorageProvider.SaveSetting(setting.Key, setting.Value.Value);
                    }

                    i++;
                    showProgressWrapper.MessageParameter[2] = $"\nMigrating Settings ({i} of {settingsCount} entities)";
                    Globals.ShowSplashProgressDialogDelegate(showProgressWrapper);
                }

                var userRoles = prevProvider.GetUserRoles();
                var userRolesCount = userRoles.Count();
                showProgressWrapper.MessageParameter[3] = $"\nMigrating User Roles (0 of {userRolesCount} entities)";
                Globals.ShowSplashProgressDialogDelegate(showProgressWrapper);

                i = 0;
                foreach (var userRole in userRoles)
                {
                    _activeDatabaseStorageProvider.SaveUserRole(userRole);
                    i++;
                    showProgressWrapper.MessageParameter[3] = $"\nMigrating User Roles ({i} of {userRolesCount} entities)";
                    Globals.ShowSplashProgressDialogDelegate(showProgressWrapper);
                }

                var users = prevProvider.GetUsers();
                var usersCount = users.Count();
                showProgressWrapper.MessageParameter[4] = $"\nMigrating Users (0 of {usersCount} entities)";
                Globals.ShowSplashProgressDialogDelegate(showProgressWrapper);

                i = 0;
                foreach (var user in prevProvider.GetUsers())
                {
                    _activeDatabaseStorageProvider.SaveUser(user);
                    i++;
                    showProgressWrapper.MessageParameter[4] = $"\nMigrating Users ({i} of {usersCount} entities)";
                    Globals.ShowSplashProgressDialogDelegate(showProgressWrapper);
                }

                int measureResultsCount = prevProvider.GetMeasureResultsCount();
                showProgressWrapper.MessageParameter[6] = $"\nMigrating Measure Results (0 of {measureResultsCount} entities)";
                Globals.ShowSplashProgressDialogDelegate(showProgressWrapper);

                i = 0;

                var experiments = prevProvider.GetExperiments(includeDeleted: true);
                foreach (var experiment in experiments)
                {
                    var groups = prevProvider.GetGroups(experiment.Item1, includeDeleted: true);

                    foreach (var @group in groups)
                    {
                        var results = prevProvider.GetMeasureResults(experiment.Item1, group.Item1, includeDeleted: true);

                        foreach (var result in results)
                        {
                            
                            var result2 = prevProvider.LoadDisplayData(result);
                            result2 = prevProvider.LoadExportData(result2);
                            if (result2 != null && result2.MeasureSetup != null)
                            {
                                var entityId = _activeDatabaseStorageProvider.SaveMeasureResult(result2,
                                    storeExistingAuditTrail: true, ignoreAuditTrail: true);

                                if (result2.IsDeletedResult)
                                {
                                    var toDelete = _activeDatabaseStorageProvider.GetMeasureResult(entityId, true);
                                    toDelete = _activeDatabaseStorageProvider.LoadDisplayData(toDelete);
                                    toDelete = _activeDatabaseStorageProvider.LoadExportData(toDelete);

                                    _activeDatabaseStorageProvider.DeleteMeasureResults(new[] {toDelete});
                                }
                            }

                            i++;
                            showProgressWrapper.MessageParameter[6] = $"\nMigrating Measure Results ({i} of {measureResultsCount} entities)";
                            Globals.ShowSplashProgressDialogDelegate(showProgressWrapper);
                        }
                    }
                }

                var predefinedTemplates = prevProvider.GetPredefinedTemplates();
                var templates = prevProvider.GetMeasureSetupTemplates();

                var templatesCount = predefinedTemplates.Count() + templates.Count();
                showProgressWrapper.MessageParameter[5] = $"\nMigrating Templates (0 of {templatesCount} entities)";
                Globals.ShowSplashProgressDialogDelegate(showProgressWrapper);

                i = 0;
                foreach (var predefinded in prevProvider.GetPredefinedTemplates())
                {
                    _activeDatabaseStorageProvider.SaveMeasureSetup(predefinded, ignoreAuditTrail: true);
                    i++;
                    showProgressWrapper.MessageParameter[5] = $"\nMigrating Templates ({i} of {templatesCount} entities)";
                    Globals.ShowSplashProgressDialogDelegate(showProgressWrapper);
                }

                foreach (var template in prevProvider.GetMeasureSetupTemplates())
                {
                    _activeDatabaseStorageProvider.SaveMeasureSetup(template, ignoreAuditTrail: true);
                    i++;
                    showProgressWrapper.MessageParameter[5] = $"\nMigrating Templates ({i} of {templatesCount} entities)";
                    Globals.ShowSplashProgressDialogDelegate(showProgressWrapper);
                }

                showProgressWrapper.IsFinished = true;
                Globals.ShowSplashProgressDialogDelegate(showProgressWrapper);
            }

            this._activeDatabaseStorageProvider.OnDatabaseReady += OnDatabaseReady;
        }

        private void OnDatabaseReady(object sender, EventArgs e)
        {
            if (OnDatabaseReadyEvent != null)
            {
                OnDatabaseReadyEvent.Invoke(this, EventArgs.Empty);
            }
        }

        public IEnumerable<User> GetUsers()
        {
            return _activeDatabaseStorageProvider.GetUsers();
        }

        public User GetUser(int userId)
        {
            return _activeDatabaseStorageProvider.GetUser(userId);
        }

        public User GetUserByName(string name)
        {
            return this._activeDatabaseStorageProvider.GetUserByName(name);
        }

        public void DeleteUser(User user)
        {
            _activeDatabaseStorageProvider.DeleteUser(user);
        }

        public void SaveUser(User user)
        {
            _activeDatabaseStorageProvider.SaveUser(user);
        }

        public IEnumerable<UserRole> GetUserRoles()
        {
            return _activeDatabaseStorageProvider.GetUserRoles();
        }

        public UserGroup GetUserGroup(int userGroupId)
        {
            return this._activeDatabaseStorageProvider.GetUserGroup(userGroupId);
        }

        public IEnumerable<UserGroup> GetUserGroups()
        {
            return this._activeDatabaseStorageProvider.GetUserGroups();
        }

        public void SaveUserGroup(UserGroup userGroup)
        {
            this._activeDatabaseStorageProvider.SaveUserGroup(userGroup);
        }

        public void Initialize(IProgress<string> progress)
        {
        }

        public void SaveUserRole(UserRole userRole)
        {
            _activeDatabaseStorageProvider.SaveUserRole(userRole);
        }

        public UserRole GetUserRole(int userRoleId)
        {
            return _activeDatabaseStorageProvider.GetUserRole(userRoleId);
        }

        public IEnumerable<MeasureSetup> GetMeasureSetupTemplates()
        {
            return _activeDatabaseStorageProvider.GetMeasureSetupTemplates();
        }

        public IEnumerable<MeasureSetup> GetPredefinedTemplates()
        {
            return _activeDatabaseStorageProvider.GetPredefinedTemplates();
        }

        public MeasureSetup GetPredefinedTemplate(string name, int capillarySize)
        {
            return _activeDatabaseStorageProvider.GetPredefinedTemplate(name, capillarySize);
        }

        public MeasureSetup GetMeasureSetup(int measureSetupId, bool ignoreCache = false)
        {
            return _activeDatabaseStorageProvider.GetMeasureSetup(measureSetupId, ignoreCache);
        }

        public void DeleteMeasureSetup(MeasureSetup measureSetup)
        {
            _activeDatabaseStorageProvider.DeleteMeasureSetup(measureSetup);
        }

        public void SaveMeasureSetup(MeasureSetup measureSetup, bool ignoreAuditTrail = false)
        {
            _activeDatabaseStorageProvider.SaveMeasureSetup(measureSetup, ignoreAuditTrail);
        }

        public IEnumerable<Tuple<string, int, int>> GetExperiments(string filter = "", bool includeDeleted = false)
        {
            return _activeDatabaseStorageProvider.GetExperiments(filter, includeDeleted);
        }

        public IEnumerable<Tuple<string, int>> GetGroups(string experiment, string filter = "", bool includeDeleted = false)
        {
            return _activeDatabaseStorageProvider.GetGroups(experiment, filter, includeDeleted);
        }

        public Dictionary<string, List<string>> GetExperimentGroupMappings(bool includeDeleted = false)
        {
            return _activeDatabaseStorageProvider.GetExperimentGroupMappings(includeDeleted);
        }

        public IEnumerable<MeasureResult> GetMeasureResults(string experiment, string group, string filter = "", bool includeDeleted = false, bool nullAsNoValue = false, int maxItems = -1)
        {
            return _activeDatabaseStorageProvider.GetMeasureResults(experiment, group, filter, includeDeleted, nullAsNoValue, maxItems);
        }

        public IEnumerable<MeasureResult> GetTemporaryMeasureResults(User loggedInUser)
        {
            return _activeDatabaseStorageProvider.GetTemporaryMeasureResults(loggedInUser);
        }

        public MeasureResult GetMeasureResult(int measureResultId, bool ignoreCache = false, bool isDeleted = false)
        {
            return _activeDatabaseStorageProvider.GetMeasureResult(measureResultId, ignoreCache, isDeleted);
        }

        public MeasureResult GetMeasureResultByGuid(Guid guid)
        {
            return _activeDatabaseStorageProvider.GetMeasureResultByGuid(guid);
        }

        public void DeleteMeasureResults(IList<MeasureResult> measureResults)
        {
            _activeDatabaseStorageProvider.DeleteMeasureResults(measureResults);
        }

        public void SaveMeasureResult(MeasureResult measureResult, bool ignoreAuditTrail = false, bool storeExistingAuditTrail = false)
        {
            _activeDatabaseStorageProvider.SaveMeasureResult(measureResult, ignoreAuditTrail, storeExistingAuditTrail);
        }

        public void RemoveDeletedMeasureResult(int deletedMeasureResultId)
        {
            _activeDatabaseStorageProvider.RemoveDeletedMeasureResult(deletedMeasureResultId);
        }

        public Cursor GetCursor(int cursorId)
        {
            return this._activeDatabaseStorageProvider.GetCursor(cursorId);
        }

        public void DeleteCursor(Cursor cursor)
        {
            _activeDatabaseStorageProvider.DeleteCursor(cursor);
        }

        public IEnumerable<ErrorDetails> GetErrorDetails()
        {
            return _activeDatabaseStorageProvider.GetErrorDetails();
        }

        public void SaveErrorDetails(ErrorDetails errorDetails)
        {
            _activeDatabaseStorageProvider.SaveErrorDetails(errorDetails);
        }

        public IEnumerable<AnnotationType> GetAnnotationTypes()
        {
            return _activeDatabaseStorageProvider.GetAnnotationTypes();
        }

        public void DeleteAnnotationType(AnnotationType annotationType)
        {
            this._activeDatabaseStorageProvider.DeleteAnnotationType(annotationType);
        }

        public MeasureResult LoadDisplayData(MeasureResult measureResult)
        {
            return _activeDatabaseStorageProvider.LoadDisplayData(measureResult);
        }

        public MeasureResult LoadExportData(MeasureResult measureResult)
        {
            return _activeDatabaseStorageProvider.LoadExportData(measureResult);
        }

        public MeasureSetup LoadExportData(MeasureSetup template)
        {
            return _activeDatabaseStorageProvider.LoadExportData(template);
        }
        public void SaveAnnotationType(AnnotationType annotationType)
        {
            _activeDatabaseStorageProvider.SaveAnnotationType(annotationType);
        }

        public void SaveMeasureResultAnnotation(MeasureResultAnnotation measureResultAnnotation, bool ignoreAuditTrail = false)
        {
            _activeDatabaseStorageProvider.SaveMeasureResultAnnotation(measureResultAnnotation, ignoreAuditTrail);
        }

        public bool IsDatabaseReady
        {
            get { return _activeDatabaseStorageProvider.IsDatabaseReady; }
        }

        public void CreateBackupFile(string fileName)
        {
            this._activeDatabaseStorageProvider.CreateBackupFile(fileName);
        }

        public bool RestoreBackupFile(string fileName)
        {
            return this._activeDatabaseStorageProvider.RestoreBackupFile(fileName);
        }

        public Dictionary<string, Setting> GetSettings()
        {
            return this._activeDatabaseStorageProvider.GetSettings();
        }

        public void SaveSetting(string key, string value)
        {
            this._activeDatabaseStorageProvider.SaveSetting(key, value);
        }

        public void SaveSetting(string key, byte[] value)
        {
            _activeDatabaseStorageProvider.SaveSetting(key, value);
        }

        public bool MeasureResultExists(string name, string experiment, string group)
        {
            return this._activeDatabaseStorageProvider.MeasureResultExists(name, experiment, group);
        }

        public void SaveAuditTrailEntry(AuditTrailEntry auditTrailEntry)
        {
            this._activeDatabaseStorageProvider.SaveAuditTrailEntry(auditTrailEntry);
        }

        public void Cleanup(int olderThanDays)
        {
            _activeDatabaseStorageProvider.Cleanup(olderThanDays);
        }

        public event EventHandler OnDatabaseReadyEvent;
    }
}
