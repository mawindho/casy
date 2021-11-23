using OLS.Casy.Core;
using OLS.Casy.Core.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.IO.SQLite.Entities;
using OLS.Casy.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data;
using System.Data.Entity;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using OLS.Casy.Base;
using MeasureResultAccessMapping = OLS.Casy.Models.MeasureResultAccessMapping;

namespace OLS.Casy.IO.SQLite.EF
{
    /// <summary>
    /// Implementation of <see cref="IDatabaseStorageProvider"/> for SQLite database using entity framework 7.
    /// </summary>
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IDatabaseStorageProvider))]

    public class SQLiteDatabaseChannel : IDatabaseStorageProvider, IPartImportsSatisfiedNotification, IDisposable
    {
        private readonly IEnvironmentService _environmentService;
        private readonly IEnumerable<IAuditTrailDecorator> _auditTrailDecorators;
        private IAuditTrailDecorator _activeAuditTrailDecorator;

        private IList<User> _userCache;
        private IList<UserRole> _userRoleCache;
        private readonly IList<UserGroup> _userGroupCache;
        private readonly IList<MeasureResult> _measureResultCache;
        private readonly IList<MeasureSetup> _measureSetupCache;

        private bool _disposedValue = false; // To detect redundant calls

        /// <summary>
        /// MEF importing constructor
        /// </summary>
        [ImportingConstructor]
        public SQLiteDatabaseChannel(IEnvironmentService environmentService,
            [ImportMany] IEnumerable<IAuditTrailDecorator> auditTrailDecorators)
        {
            _environmentService = environmentService;
            _auditTrailDecorators = auditTrailDecorators;

            _userCache = new List<User>();
            _userRoleCache = new List<UserRole>();
            _measureResultCache = new List<MeasureResult>();
            _measureSetupCache = new List<MeasureSetup>();
            _userGroupCache = new List<UserGroup>();
        }

        public int ProviderVersion => 0;
        public bool IsEmpty => !GetErrorDetails().Any();

        /// <summary>
        /// Method will be valled when all MEF imports has been fullfilled
        /// </summary>
        public void OnImportsSatisfied()
        {
            //if (!IsActive)
            //{
                //return;
            //}
            if (_auditTrailDecorators.Any())
            {
                _activeAuditTrailDecorator = _auditTrailDecorators.FirstOrDefault(x => !x.IsStandard);
            }


            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                //if (!File.Exists("casy.standard.db"))
                //{
                    if (Migrations.CheckForMigration(casyContext))
                    {
                        if (File.Exists("casy.db.bak"))
                        {
                            File.Delete("casy.db.bak");
                        }

                        CreateBackupFile("casy.db.bak");
                        Migrations.DoMigration(casyContext);
                    }
                //}

                IsDatabaseReady = true;
                RaiseOnDatabaseReady();
            }
        }        

        public bool IsDatabaseReady { get; private set; }

        public void CreateMigrationDatabase()
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                var destination =
                    new SQLiteConnection(@"Data Source=.\casy.standard.db;Version=3;");

                destination.Open();

                if (casyContext.Database.Connection is SQLiteConnection source)
                {
                    if (source.State != ConnectionState.Open)
                    {
                        source.Open();
                    }

                    source.BackupDatabase(destination, "main", "main", -1, null, 0);

                    source.Close();
                }

                destination.Close();
            }
        }

        /// <summary>
        /// Event is raised when database connection is ready
        /// </summary>
        public event EventHandler OnDatabaseReady;

        private void RaiseOnDatabaseReady()
        {
            OnDatabaseReady?.Invoke(this, EventArgs.Empty);
        }

        public IEnumerable<User> GetUsers()
        {
            var result = new List<User>();
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                var userEntities = casyContext.Users.ToList();

                foreach (var userEntity in userEntities)
                {
                    result.Add(GetUser(casyContext, userEntity.UserEntityId));
                }
                return result;
            }
        }

        public User GetUser(int userId)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                return GetUser(casyContext, userId);
            }
        }

        private User GetUser(CasyContext2 casyContext, int userId)
        {
            var user = _userCache.FirstOrDefault(u => u.Id == userId);

            if (user != null)
            {
                return user;
            }

            var userEntity = casyContext.Users.FirstOrDefault(u => u.UserEntityId == userId);
            if (userEntity == null)
            {
                return null;
            }
            var userRole = GetUserRole(userEntity.UserRoleEntityId);
            var userUserGroupMappings = casyContext.UserUserGroupMappings.Where(m => m.UserEntityId == userId).ToList();

            user = new User(userEntity.Username, userRole)
            {
                Id = userEntity.UserEntityId,
                FirstName = userEntity.FirstName,
                LastName = userEntity.LastName,
                Password = userEntity.Password,
                JobTitle = userEntity.JobTitle,
                CountryRegionName = userEntity.CountryRegionName,
                KeyboardCountryRegionName = userEntity.KeyboardCountryRegionName,
                EmailAddress = userEntity.EmailAddress,
                Image = userEntity.Image,
                ForceCreatePassword = userEntity.ForceCreatePassword,
                //RecentMeasureResultIds = string.IsNullOrEmpty(userEntity.RecentMeasureResultIds) ? new List<int>() : userEntity.RecentMeasureResultIds.Split(';').Select(item => Convert.ToInt32(item, CultureInfo.InvariantCulture)).ToList(),
                LastUsedSetupId = userEntity.LastUsedSetupId,
                IsEmergencyUser = userEntity.IsEmergencyUser,
                RecentTemplateIds = string.IsNullOrEmpty(userEntity.RecentTemplateIds) ? new List<int>() : userEntity.RecentTemplateIds.Split(';').Select(item => Convert.ToInt32(item, CultureInfo.InvariantCulture)).ToList(),
                FavoriteTemplateIds = string.IsNullOrEmpty(userEntity.FavoriteTemplateIds) ? new List<int>() : userEntity.FavoriteTemplateIds.Split(';').Select(item => Convert.ToInt32(item, CultureInfo.InvariantCulture)).ToList(),
                SelectedTableColumns = string.IsNullOrEmpty(userEntity.RecentMeasureResultIds) ? new List<string>() : userEntity.RecentMeasureResultIds.Split(';').ToList()
            };

            foreach(var mapping in userUserGroupMappings)
            {
                user.UserGroups.Add(GetUserGroup(casyContext, mapping.UserGroupEntityId, false));
            }

            _userCache.Add(user);
            return user;
        }

        public User GetUserByName(string name)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                var user = _userCache.FirstOrDefault(u => u.Identity.Name == name);

                if (user != null)
                {
                    return user;
                }

                var userEntity = casyContext.Users.FirstOrDefault(u => u.Username == name);
                if (userEntity == null)
                {
                    return null;
                }
                var userRole = GetUserRole(userEntity.UserRoleEntityId);
                var userUserGroupMappings = casyContext.UserUserGroupMappings.Where(m => m.UserEntityId == userEntity.UserEntityId).ToList();

                user = new User(userEntity.Username, userRole)
                {
                    Id = userEntity.UserEntityId,
                    FirstName = userEntity.FirstName,
                    LastName = userEntity.LastName,
                    Password = userEntity.Password,
                    JobTitle = userEntity.JobTitle,
                    CountryRegionName = userEntity.CountryRegionName,
                    KeyboardCountryRegionName = userEntity.KeyboardCountryRegionName,
                    EmailAddress = userEntity.EmailAddress,
                    Image = userEntity.Image,
                    ForceCreatePassword = userEntity.ForceCreatePassword,
                    //RecentMeasureResultIds = string.IsNullOrEmpty(userEntity.RecentMeasureResultIds) ? new List<int>() : userEntity.RecentMeasureResultIds.Split(';').Select(item => Convert.ToInt32(item, CultureInfo.InvariantCulture)).ToList(),
                    LastUsedSetupId = userEntity.LastUsedSetupId,
                    IsEmergencyUser = userEntity.IsEmergencyUser,
                    RecentTemplateIds = string.IsNullOrEmpty(userEntity.RecentTemplateIds) ? new List<int>() : userEntity.RecentTemplateIds.Split(';').Select(item => Convert.ToInt32(item, CultureInfo.InvariantCulture)).ToList(),
                    FavoriteTemplateIds = string.IsNullOrEmpty(userEntity.FavoriteTemplateIds) ? new List<int>() : userEntity.FavoriteTemplateIds.Split(';').Select(item => Convert.ToInt32(item, CultureInfo.InvariantCulture)).ToList(),
                    SelectedTableColumns = string.IsNullOrEmpty(userEntity.RecentMeasureResultIds) ? new List<string>() : userEntity.RecentMeasureResultIds.Split(';').ToList()
                };

                foreach (var mapping in userUserGroupMappings)
                {
                    user.UserGroups.Add(GetUserGroup(casyContext, mapping.UserGroupEntityId, false));
                }

                _userCache.Add(user);
                return user;
            }
        }

        public void DeleteUser(User user)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                _userCache.Remove(user);

                var userUserGroupMappings = casyContext.UserUserGroupMappings.Where(m => m.UserEntityId == user.Id).ToList();
                foreach (var mapping in userUserGroupMappings)
                {
                    casyContext.UserUserGroupMappings.Remove(mapping);
                }
                var userEntity = casyContext.Users.FirstOrDefault(u => u.UserEntityId == user.Id);
                if (userEntity == null) return;
                casyContext.Users.Remove(userEntity);
                casyContext.SaveChanges();
            }
        }

        public int SaveUser(User user)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                var userEntity = casyContext.Users.Include("UserRoleEntity").FirstOrDefault(u => u.UserEntityId == user.Id);

                var userRoleId = user.UserRole == null || user.UserRole == UserRole.None ? 1 : casyContext.UserRoles.FirstOrDefault(i => i.Name == user.UserRole.Name).UserRoleEntityId;

                if (userEntity == null)
                {
                    userEntity = new UserEntity
                    {
                        FirstName = user.FirstName,
                        Password = user.Password,
                        LastName = user.LastName,
                        Username = user.Identity.Name,
                        UserRoleEntityId = userRoleId,
                        CountryRegionName = user.CountryRegionName,
                        KeyboardCountryRegionName = user.KeyboardCountryRegionName,
                        EmailAddress = user.EmailAddress,
                        Image = user.Image,
                        JobTitle = user.JobTitle,
                        ForceCreatePassword = user.ForceCreatePassword,
                        RecentMeasureResultIds = string.Join(";", user.SelectedTableColumns),
                        LastUsedSetupId = user.LastUsedSetupId,
                        IsEmergencyUser = user.IsEmergencyUser,
                        RecentTemplateIds = string.Join(";", user.RecentTemplateIds),
                        FavoriteTemplateIds = string.Join(";", user.FavoriteTemplateIds)
                    };
                    casyContext.Users.Add(userEntity);

                    casyContext.SaveChanges();
                    user.Id = userEntity.UserEntityId;

                    _userCache.Add(user);
                }
                else
                {
                    userEntity.FirstName = user.FirstName;
                    userEntity.Password = user.Password;
                    userEntity.LastName = user.LastName;
                    userEntity.Username = user.Identity.Name;
                    userEntity.UserRoleEntityId = userRoleId;
                    userEntity.CountryRegionName = user.CountryRegionName;
                    userEntity.KeyboardCountryRegionName = user.KeyboardCountryRegionName;
                    userEntity.EmailAddress = user.EmailAddress;
                    userEntity.Image = user.Image;
                    userEntity.JobTitle = user.JobTitle;
                    userEntity.ForceCreatePassword = user.ForceCreatePassword;
                    userEntity.RecentMeasureResultIds = string.Join(";", user.SelectedTableColumns);
                    userEntity.LastUsedSetupId = user.LastUsedSetupId;
                    userEntity.IsEmergencyUser = user.IsEmergencyUser;
                    userEntity.RecentTemplateIds = string.Join(";", user.RecentTemplateIds);
                    userEntity.FavoriteTemplateIds = string.Join(";", user.FavoriteTemplateIds);

                    casyContext.SaveChanges();
                }

                foreach(var userGroup in user.UserGroups)
                {
                    SaveUserUserGroupMapping(casyContext, user.Id, userGroup.Id);
                }

                return userEntity.UserEntityId;
            }
        }

        private void SaveUserUserGroupMapping(CasyContext2 casyContext, int userId, int userGroupId)
        {
            var userUserGroupMapping = casyContext.UserUserGroupMappings.FirstOrDefault(u => u.UserEntityId == userId && u.UserGroupEntityId == userGroupId);

            if (userUserGroupMapping != null) return;
            userUserGroupMapping = new UserUserGroupMapping
            {
                UserEntityId = userId,
                UserGroupEntityId = userGroupId
            };
            casyContext.UserUserGroupMappings.Add(userUserGroupMapping);

            casyContext.SaveChanges();
        }

        public IEnumerable<UserRole> GetUserRoles()
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                var result = new List<UserRole>();

                var userRoleEntities = casyContext.UserRoles.ToList();

                foreach (var userRoleEntity in userRoleEntities)
                {
                    result.Add(GetUserRole(casyContext, userRoleEntity.UserRoleEntityId));
                }
                return result;
            }
        }

        public UserRole GetUserRole(int userRoleId)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                return GetUserRole(casyContext, userRoleId);
            }
        }

        private UserRole GetUserRole(CasyContext2 casyContext, int userRoleId)
        {
            var userRole = _userRoleCache.FirstOrDefault(ur => ur.Id == userRoleId);

            if (userRole != null)
            {
                return userRole;
            }

            var userRoleEntity = casyContext.UserRoles.FirstOrDefault(ur => ur.UserRoleEntityId == userRoleId);
            if (userRoleEntity == null) return null;
            userRole = new UserRole { Id = userRoleEntity.UserRoleEntityId, Name = userRoleEntity.Name, Priority = userRoleEntity.Priority };
            _userRoleCache.Add(userRole);
            return userRole;
        }

        public int SaveUserRole(UserRole userRole)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                var userRoleEntity = casyContext.UserRoles.FirstOrDefault(ur => ur.UserRoleEntityId == userRole.Id);

                if (userRoleEntity == null)
                {
                    userRoleEntity = new UserRoleEntity
                    {
                        Name = userRole.Name,
                        Priority = userRole.Priority
                    };
                    casyContext.UserRoles.Add(userRoleEntity);
                    casyContext.SaveChanges();

                    userRole.Id = userRoleEntity.UserRoleEntityId;

                    _userRoleCache.Add(userRole);
                }
                else
                {
                    userRoleEntity.Name = userRole.Name;
                    userRoleEntity.Priority = userRole.Priority;

                    casyContext.SaveChanges();
                }

                return userRoleEntity.UserRoleEntityId;
            }
        }

        public IEnumerable<UserGroup> GetUserGroups()
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                var result = new List<UserGroup>();

                var userGroupEntities = casyContext.UserGroups.ToList();

                foreach (var userGroupEntity in userGroupEntities)
                {
                    result.Add(GetUserGroup(casyContext, userGroupEntity.UserGroupEntityId, true));
                }
                return result;
            }
        }

        public UserGroup GetUserGroup(int userGroupId)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                return GetUserGroup(casyContext, userGroupId, true);
            }
        }

        private UserGroup GetUserGroup(CasyContext2 casyContext, int userGroupId, bool fillUser)
        {
            var userGroup = _userGroupCache.FirstOrDefault(ur => ur.Id == userGroupId);

            if (userGroup != null)
            {
                return userGroup;
            }

            var userGroupEntity = casyContext.UserGroups.FirstOrDefault(ur => ur.UserGroupEntityId == userGroupId);
            if (userGroupEntity == null) return null;

            userGroup = new UserGroup { Id = userGroupEntity.UserGroupEntityId, Name = userGroupEntity.Name };

            if(fillUser)
            {
                var userUserGroupMappings = casyContext.UserUserGroupMappings.Where(m => m.UserGroupEntityId == userGroup.Id).ToList();

                foreach(var mapping in userUserGroupMappings)
                {
                    userGroup.Users.Add(GetUser(casyContext, mapping.UserEntityId));
                }
            }

            _userGroupCache.Add(userGroup);
            return userGroup;
        }

        public int SaveUserGroup(UserGroup userGroup)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                var userGroupEntity = casyContext.UserGroups.FirstOrDefault(ug => ug.UserGroupEntityId == userGroup.Id);

                if (userGroupEntity == null)
                {
                    userGroupEntity = new UserGroupEntity()
                    {
                        Name = userGroup.Name
                    };
                    casyContext.UserGroups.Add(userGroupEntity);

                    casyContext.SaveChanges();

                    userGroup.Id = userGroupEntity.UserGroupEntityId;

                    _userGroupCache.Add(userGroup);
                }
                else
                {
                    userGroupEntity.Name = userGroup.Name;

                    casyContext.SaveChanges();
                }

                foreach (var user in userGroup.Users)
                {
                    SaveUserUserGroupMapping(casyContext, user.Id, userGroup.Id);
                }

                return userGroupEntity.UserGroupEntityId;
            }
        }

        public IEnumerable<MeasureSetup> GetPredefinedTemplates()
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                var result = new List<MeasureSetup>();

                var measureSetupEntities = casyContext.MeasureSetups.Where(ms => ms.IsTemplate && ms.IsReadOnly).ToList();

                foreach (var measureSetupEntity in measureSetupEntities)
                {
                    result.Add(GetMeasureSetup(casyContext, measureSetupEntity.MeasureSetupEntityId, true));
                }
                return result;
            }
        }

        public MeasureSetup GetPredefinedTemplate(string name, int capillarySize)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                var measureSetupIds = casyContext.MeasureSetups.Where(ms => ms.Name == name && ms.IsTemplate && ms.IsReadOnly && ms.CapillarySize == capillarySize).Select(ms => ms.MeasureSetupEntityId).ToList();

                return measureSetupIds.Count > 0 ? GetMeasureSetup(casyContext, measureSetupIds.FirstOrDefault(), true) : null;
            }
        }

        public IEnumerable<MeasureSetup> GetMeasureSetupTemplates()
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                var result = new List<MeasureSetup>();

                var measureSetupEntities = casyContext.MeasureSetups.Where(ms => ms.IsTemplate && !ms.IsReadOnly).ToList();

                foreach (var measureSetupEntity in measureSetupEntities)
                {
                    result.Add(GetMeasureSetup(casyContext, measureSetupEntity.MeasureSetupEntityId, true));
                }
                return result;
            }
        }

        public bool MeasureResultExists(string name, string experiment, string group)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                return casyContext.MeasureResults.Count(mr => mr.Name == name && mr.Group == group && mr.Experiment == experiment) > 0;
            }
        }

        public MeasureSetup GetMeasureSetup(int measureSetupId, bool ignoreCache = false)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                return GetMeasureSetup(casyContext, measureSetupId, ignoreCache);
            }
        }

        private MeasureSetup GetMeasureSetup(CasyContext2 casyContext, int measureSetupId, bool ignoreCache = false, bool isDeletedResult = false)
        {
            //TODO: Um gelöschte kümmern

            var measureSetup = _measureSetupCache.FirstOrDefault(ms => ms.MeasureSetupId == measureSetupId);

            if (measureSetup != null && !ignoreCache)
            {
                return measureSetup;
            }

            if (isDeletedResult)
            {
                var measureSetupEntityDeleted = casyContext.MeasureSetupsDeleted.Include("CursorEntities")
                    .Include("DeviationControlItemEntities")
                    .FirstOrDefault(ms => ms.MeasureSetupEntityId == measureSetupId);

                if (measureSetupEntityDeleted == null) return null;

                measureSetup = new MeasureSetup
                {
                    ChannelCount = measureSetupEntityDeleted.ChannelCount,
                    AggregationCalculationMode = measureSetupEntityDeleted.AggregationCalculationMode,
                    CapillarySize = measureSetupEntityDeleted.CapillarySize,
                    DilutionFactor = measureSetupEntityDeleted.DilutionFactor,
                    DilutionSampleVolume = measureSetupEntityDeleted.DilutionSampleVolume,
                    DilutionCasyTonVolume = measureSetupEntityDeleted.DilutionCasyTonVolume,
                    FromDiameter = measureSetupEntityDeleted.FromDiameter,
                    IsDeviationControlEnabled = measureSetupEntityDeleted.IsDeviationControlEnabled,
                    IsSmoothing = measureSetupEntityDeleted.IsSmoothing,
                    IsTemplate = measureSetupEntityDeleted.IsTemplate,
                    ManualAggregationCalculationFactor = measureSetupEntityDeleted.ManualAggrgationCalculationFactor,
                    MeasureMode = measureSetupEntityDeleted.MeasureMode,
                    MeasureSetupId = measureSetupEntityDeleted.MeasureSetupEntityId,
                    Name = measureSetupEntityDeleted.Name,
                    Version = measureSetupEntityDeleted.Version,
                    Repeats = measureSetupEntityDeleted.Repeats,
                    ScalingMaxRange = measureSetupEntityDeleted.ScalingMaxRange,
                    ScalingMode = measureSetupEntityDeleted.ScalingMode,
                    SmoothingFactor = measureSetupEntityDeleted.SmoothingFactor,
                    ToDiameter = measureSetupEntityDeleted.ToDiameter,
                    UnitMode = measureSetupEntityDeleted.UnitMode,
                    Volume = measureSetupEntityDeleted.Volume,
                    VolumeCorrectionFactor = measureSetupEntityDeleted.VolumeCorrectionFactor,
                    IsReadOnly = measureSetupEntityDeleted.IsReadOnly,
                    CreatedAt = DateTimeOffsetExtensions.ParseAny(measureSetupEntityDeleted.CreatedAt),
                    CreatedBy = measureSetupEntityDeleted.CreatedBy,
                    LastModifiedAt = DateTimeOffsetExtensions.ParseAny(measureSetupEntityDeleted.LastModifiedAt),
                    LastModifiedBy = measureSetupEntityDeleted.LastModifiedBy,
                    AutoSaveName = measureSetupEntityDeleted.AutoSaveName,
                    DefaultExperiment = measureSetupEntityDeleted.DefaultExperiment,
                    DefaultGroup = measureSetupEntityDeleted.DefaultGroup,
                    IsAutoSave = measureSetupEntityDeleted.IsAutoSave,
                    IsAutoComment = measureSetupEntityDeleted.IsAutoComment,
                    HasSubpopulations = measureSetupEntityDeleted.HasSubpopulations,
                    IsDeletedSetup = true
                };

                foreach (var cursorEntity in measureSetupEntityDeleted.CursorEntities)
                {
                    measureSetup.Cursors.Add(new Cursor
                    {
                        CursorId = cursorEntity.CursorEntityId,
                        MinLimit = cursorEntity.MinLimit,
                        MaxLimit = cursorEntity.MaxLimit,
                        Color = cursorEntity.Color,
                        Name = cursorEntity.Name,
                        Version = cursorEntity.Version,
                        IsDeadCellsCursor = cursorEntity.IsDeadCellsCursor,
                        MeasureSetup = measureSetup,
                        CreatedAt = DateTimeOffsetExtensions.ParseAny(cursorEntity.CreatedAt),
                        CreatedBy = cursorEntity.CreatedBy,
                        LastModifiedAt = DateTimeOffsetExtensions.ParseAny(cursorEntity.LastModifiedAt),
                        LastModifiedBy = cursorEntity.LastModifiedBy,
                        Subpopulation = cursorEntity.Subpopulation
                    });
                }

                foreach (var deviationControlItemEntity in measureSetupEntityDeleted.DeviationControlItemEntities)
                {
                    measureSetup.AddDeviationControlItem(new DeviationControlItem
                    {
                        DeviationControlItemId = deviationControlItemEntity.DeviationControlItemEntityId,
                        MaxLimit = deviationControlItemEntity.MaxLimit,
                        MeasureResultItemType = deviationControlItemEntity.MeasureResultItemType,
                        MeasureSetup = measureSetup,
                        Version = deviationControlItemEntity.Version,
                        MinLimit = deviationControlItemEntity.MinLimit
                    });
                }
            }
            else
            {
                var measureSetupEntity = casyContext.MeasureSetups.Include("CursorEntities")
                    .Include("DeviationControlItemEntities")
                    .FirstOrDefault(ms => ms.MeasureSetupEntityId == measureSetupId);

                if (measureSetupEntity == null) return null;

                measureSetup = new MeasureSetup
                {
                    ChannelCount = measureSetupEntity.ChannelCount,
                    AggregationCalculationMode = measureSetupEntity.AggregationCalculationMode,
                    CapillarySize = measureSetupEntity.CapillarySize,
                    DilutionFactor = measureSetupEntity.DilutionFactor,
                    DilutionSampleVolume = measureSetupEntity.DilutionSampleVolume,
                    DilutionCasyTonVolume = measureSetupEntity.DilutionCasyTonVolume,
                    FromDiameter = measureSetupEntity.FromDiameter,
                    IsDeviationControlEnabled = measureSetupEntity.IsDeviationControlEnabled,
                    IsSmoothing = measureSetupEntity.IsSmoothing,
                    IsTemplate = measureSetupEntity.IsTemplate,
                    ManualAggregationCalculationFactor = measureSetupEntity.ManualAggrgationCalculationFactor,
                    MeasureMode = measureSetupEntity.MeasureMode,
                    MeasureSetupId = measureSetupEntity.MeasureSetupEntityId,
                    Name = measureSetupEntity.Name,
                    Version = measureSetupEntity.Version,
                    Repeats = measureSetupEntity.Repeats,
                    ScalingMaxRange = measureSetupEntity.ScalingMaxRange,
                    ScalingMode = measureSetupEntity.ScalingMode,
                    SmoothingFactor = measureSetupEntity.SmoothingFactor,
                    ToDiameter = measureSetupEntity.ToDiameter,
                    UnitMode = measureSetupEntity.UnitMode,
                    Volume = measureSetupEntity.Volume,
                    VolumeCorrectionFactor = measureSetupEntity.VolumeCorrectionFactor,
                    IsReadOnly = measureSetupEntity.IsReadOnly,
                    CreatedAt = DateTimeOffsetExtensions.ParseAny(measureSetupEntity.CreatedAt),
                    CreatedBy = measureSetupEntity.CreatedBy,
                    LastModifiedAt = DateTimeOffsetExtensions.ParseAny(measureSetupEntity.LastModifiedAt),
                    LastModifiedBy = measureSetupEntity.LastModifiedBy,
                    AutoSaveName = measureSetupEntity.AutoSaveName,
                    DefaultExperiment = measureSetupEntity.DefaultExperiment,
                    DefaultGroup = measureSetupEntity.DefaultGroup,
                    IsAutoSave = measureSetupEntity.IsAutoSave,
                    IsAutoComment = measureSetupEntity.IsAutoComment,
                    HasSubpopulations = measureSetupEntity.HasSubpopulations
                };

                foreach (var cursorEntity in measureSetupEntity.CursorEntities)
                {
                    measureSetup.Cursors.Add(new Cursor
                    {
                        CursorId = cursorEntity.CursorEntityId,
                        MinLimit = cursorEntity.MinLimit,
                        MaxLimit = cursorEntity.MaxLimit,
                        Color = cursorEntity.Color,
                        Name = cursorEntity.Name,
                        Version = cursorEntity.Version,
                        IsDeadCellsCursor = cursorEntity.IsDeadCellsCursor,
                        MeasureSetup = measureSetup,
                        CreatedAt = DateTimeOffsetExtensions.ParseAny(cursorEntity.CreatedAt),
                        CreatedBy = cursorEntity.CreatedBy,
                        LastModifiedAt = DateTimeOffsetExtensions.ParseAny(cursorEntity.LastModifiedAt),
                        LastModifiedBy = cursorEntity.LastModifiedBy,
                        Subpopulation = cursorEntity.Subpopulation
                    });
                }

                foreach (var deviationControlItemEntity in measureSetupEntity.DeviationControlItemEntities)
                {
                    measureSetup.AddDeviationControlItem(new DeviationControlItem
                    {
                        DeviationControlItemId = deviationControlItemEntity.DeviationControlItemEntityId,
                        MaxLimit = deviationControlItemEntity.MaxLimit,
                        MeasureResultItemType = deviationControlItemEntity.MeasureResultItemType,
                        MeasureSetup = measureSetup,
                        Version = deviationControlItemEntity.Version,
                        MinLimit = deviationControlItemEntity.MinLimit
                    });
                }
            }

            if (!ignoreCache)
            {
                _measureSetupCache.Add(measureSetup);
            }

            return measureSetup;
        }

        public void DeleteMeasureSetup(MeasureSetup measureSetup)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                DeleteMeasureSetup(casyContext, measureSetup);
            }
        }

        private MeasureSetupEntity_Deleted DeleteMeasureSetup(CasyContext2 casyContext, MeasureSetup measureSetup)
        {
            //TODO: Um das löschen kümmern (verschieben in deleted table)
            if (measureSetup == null) return null;

            var loggedInUser = _environmentService.GetEnvironmentInfo("LoggedInUserName") as string;
            var measureSetupEntity = casyContext.MeasureSetups.FirstOrDefault(ms => ms.MeasureSetupEntityId == measureSetup.MeasureSetupId);
            if (measureSetupEntity == null) return null;

            var measureSetupEntityDeleted = new MeasureSetupEntity_Deleted
            {
                CreatedBy = measureSetupEntity.CreatedBy,
                Name = measureSetupEntity.Name,
                Version = measureSetup.Version,
                LastModifiedBy = measureSetupEntity.LastModifiedBy,
                CreatedAt = measureSetupEntity.CreatedAt,
                LastModifiedAt = measureSetupEntity.LastModifiedAt,
                DeletedBy = loggedInUser,
                DeletedAt = DateTimeOffset.UtcNow.ToString(),
                AggregationCalculationMode = measureSetupEntity.AggregationCalculationMode,
                AutoSaveName = measureSetupEntity.AutoSaveName,
                CapillarySize = measureSetupEntity.CapillarySize,
                ChannelCount = measureSetupEntity.ChannelCount,
                DefaultExperiment = measureSetupEntity.DefaultExperiment,
                DefaultGroup = measureSetupEntity.DefaultGroup,
                DilutionCasyTonVolume = measureSetupEntity.DilutionCasyTonVolume,
                DilutionFactor = measureSetupEntity.DilutionFactor,
                DilutionSampleVolume = measureSetupEntity.DilutionSampleVolume,
                FromDiameter = measureSetupEntity.FromDiameter,
                HasSubpopulations = measureSetupEntity.HasSubpopulations,
                IsAutoComment = measureSetupEntity.IsAutoComment,
                IsAutoSave = measureSetupEntity.IsAutoSave,
                IsDeviationControlEnabled = measureSetupEntity.IsDeviationControlEnabled,
                IsReadOnly = measureSetupEntity.IsReadOnly,
                IsSmoothing = measureSetupEntity.IsSmoothing,
                IsTemplate = measureSetupEntity.IsTemplate,
                ManualAggrgationCalculationFactor = measureSetupEntity.ManualAggrgationCalculationFactor,
                MeasureMode = measureSetupEntity.MeasureMode,
                Repeats = measureSetupEntity.Repeats,
                ScalingMaxRange = measureSetupEntity.ScalingMaxRange,
                ScalingMode = measureSetupEntity.ScalingMode,
                SmoothingFactor = measureSetupEntity.SmoothingFactor,
                ToDiameter = measureSetupEntity.ToDiameter,
                UnitMode = measureSetupEntity.UnitMode,
                Volume = measureSetupEntity.Volume,
                VolumeCorrectionFactor = measureSetupEntity.VolumeCorrectionFactor
            };

            casyContext.MeasureSetupsDeleted.Add(measureSetupEntityDeleted);

            var cursorsToDelete = new List<CursorEntity>();
            foreach (var cursor in measureSetup.Cursors)
            {
                var cursorEntity = casyContext.Cursors.Include("MeasureSetupEntity").FirstOrDefault(c => c.CursorEntityId == cursor.CursorId);

                if (cursorEntity != null)
                {
                    var cursorEntityDeleted = new CursorEntity_Deleted
                    {
                        Color = cursorEntity.Color,
                        CreatedAt = cursorEntity.CreatedAt,
                        CreatedBy = cursorEntity.CreatedBy,
                        DeletedAt = DateTimeOffset.UtcNow.ToString(),
                        DeletedBy = loggedInUser,
                        Version = cursor.Version,
                        IsDeadCellsCursor = cursorEntity.IsDeadCellsCursor,
                        LastModifiedAt = cursorEntity.LastModifiedAt,
                        LastModifiedBy = cursorEntity.LastModifiedBy,
                        MaxLimit = cursorEntity.MaxLimit,
                        MinLimit = cursorEntity.MinLimit,
                        Name = cursorEntity.Name,
                        Subpopulation = cursorEntity.Subpopulation
                    };
                    measureSetupEntityDeleted.CursorEntities.Add(cursorEntityDeleted);
                    casyContext.CursorsDeleted.Add(cursorEntityDeleted);
                    cursorsToDelete.Add(cursorEntity);
                    //casyContext.Cursors.Remove(cursorEntity);
                }
            }

            var deviationControlItemsToDelete = new List<DeviationControlItemEntity>();
            foreach (var deviationControlItem in measureSetup.DeviationControlItems)
            {
                var deviationControlItemEntity = casyContext.DeviationControlItems.Include("MeasureSetupEntity").FirstOrDefault(d => d.DeviationControlItemEntityId == deviationControlItem.DeviationControlItemId);

                if (deviationControlItemEntity != null)
                {
                    var deviationControlItemEntityDeleted = new DeviationControlItemEntity_Deleted
                    {
                        CreatedAt = deviationControlItemEntity.CreatedAt,
                        CreatedBy = deviationControlItemEntity.CreatedBy,
                        DeletedAt = DateTimeOffset.UtcNow.ToString(),
                        DeletedBy = loggedInUser,
                        LastModifiedAt = deviationControlItemEntity.LastModifiedAt,
                        Version = deviationControlItem.Version,
                        LastModifiedBy = deviationControlItemEntity.LastModifiedBy,
                        MaxLimit = deviationControlItemEntity.MaxLimit,
                        MeasureResultItemType = deviationControlItemEntity.MeasureResultItemType,
                        MinLimit = deviationControlItemEntity.MinLimit
                    };

                    measureSetupEntityDeleted.DeviationControlItemEntities
                        .Add(deviationControlItemEntityDeleted);
                    casyContext.DeviationControlItemsDeleted.Add(deviationControlItemEntityDeleted);
                    deviationControlItemsToDelete.Add(deviationControlItemEntity);
                    //casyContext.DeviationControlItems.Remove(deviationControlItemEntity);
                }
            }

            var auditTrailsToDelete = new List<AuditTrailEntryEntity>();
            foreach (var auditTrail in measureSetup.AuditTrailEntries)
            {
                var entity = casyContext.AuditTrailEntries.FirstOrDefault(d => d.AuditTrailEntryEntityId == auditTrail.AuditTrailEntryId);

                var auditTrailDeleted = new AuditTrailEntryEntity_Deleted()
                {
                    Action = entity.Action,
                    ComputerName = entity.ComputerName,
                    DateChanged = entity.DateChanged,
                    EntityName = entity.EntityName,
                    NewValue = entity.NewValue,
                    OldValue = entity.OldValue,
                    PrimaryKeyValue = entity.PrimaryKeyValue,
                    PropertyName = entity.PropertyName,
                    SoftwareVersion = entity.SoftwareVersion,
                    UserChanged = entity.UserChanged
                };
                measureSetupEntityDeleted.AuditTrailEntrieEntities.Add(auditTrailDeleted);
                //casyContext.AuditTrailEntries.Remove(entity);
                casyContext.AuditTrailEntriesDeleted.Add(auditTrailDeleted);
                auditTrailsToDelete.Add(entity);
            }

            //casyContext.SaveChanges();

            _measureSetupCache.Remove(measureSetup);

            
            foreach (var cursor in cursorsToDelete)
            {
                casyContext.Cursors.Remove(cursor);
            }

            foreach (var deviationControlItem in deviationControlItemsToDelete)
            {
                casyContext.DeviationControlItems.Remove(deviationControlItem);
            }

            foreach (var auditTrail in auditTrailsToDelete)
            {
                casyContext.AuditTrailEntries.Remove(auditTrail);
            }

            casyContext.MeasureSetups.Remove(measureSetupEntity);

            casyContext.SaveChanges();

            return measureSetupEntityDeleted;
        }

        private void RemoveDeletedMeasureSetup(CasyContext2 casyContext, int deletedmeasureSetupId)
        {
            //if (measureSetup == null) return null;

            var measureSetupEntityDeleted = casyContext.MeasureSetupsDeleted.Include("CursorEntities")
                .Include("DeviationControlItemEntities")
                .Include("AuditTrailEntrieEntities")
                .FirstOrDefault(ms => ms.MeasureSetupEntityId == deletedmeasureSetupId);
            /*var measureSetupEntity = new MeasureSetupEntity
            {
                LastModifiedBy = measureSetupEntityDeleted.LastModifiedBy,
                CreatedBy = measureSetupEntityDeleted.CreatedBy,
                CreatedAt = measureSetupEntityDeleted.CreatedAt,
                LastModifiedAt = measureSetupEntityDeleted.LastModifiedAt,
                AggregationCalculationMode = measureSetupEntityDeleted.AggregationCalculationMode,
                AutoSaveName = measureSetupEntityDeleted.AutoSaveName,
                CapillarySize = measureSetupEntityDeleted.CapillarySize,
                ChannelCount = measureSetupEntityDeleted.ChannelCount,
                DefaultExperiment = measureSetupEntityDeleted.DefaultExperiment,
                DefaultGroup = measureSetupEntityDeleted.DefaultGroup,
                DilutionCasyTonVolume = measureSetupEntityDeleted.DilutionCasyTonVolume,
                DilutionFactor = measureSetupEntityDeleted.DilutionFactor,
                DilutionSampleVolume = measureSetupEntityDeleted.DilutionSampleVolume,
                FromDiameter = measureSetupEntityDeleted.FromDiameter,
                HasSubpopulations = measureSetupEntityDeleted.HasSubpopulations,
                IsAutoComment = measureSetupEntityDeleted.IsAutoComment,
                IsAutoSave = measureSetupEntityDeleted.IsAutoSave,
                IsDeviationControlEnabled = measureSetupEntityDeleted.IsDeviationControlEnabled,
                IsReadOnly = measureSetupEntityDeleted.IsReadOnly,
                IsSmoothing = measureSetupEntityDeleted.IsSmoothing,
                IsTemplate = measureSetupEntityDeleted.IsTemplate,
                ManualAggrgationCalculationFactor = measureSetupEntityDeleted.ManualAggrgationCalculationFactor,
                MeasureMode = measureSetupEntityDeleted.MeasureMode,
                Name = measureSetupEntityDeleted.Name,
                Version = measureSetupEntityDeleted.Version,
                Repeats = measureSetupEntityDeleted.Repeats,
                ScalingMaxRange = measureSetupEntityDeleted.ScalingMaxRange,
                ScalingMode = measureSetupEntityDeleted.ScalingMode,
                SmoothingFactor = measureSetupEntityDeleted.SmoothingFactor,
                ToDiameter = measureSetupEntityDeleted.ToDiameter,
                UnitMode = measureSetupEntityDeleted.UnitMode,
                Volume = measureSetupEntityDeleted.Volume,
                VolumeCorrectionFactor = measureSetupEntityDeleted.VolumeCorrectionFactor
            };*/
            casyContext.MeasureSetupsDeleted.Remove(measureSetupEntityDeleted);
            //casyContext.MeasureSetups.Add(measureSetupEntity);

            foreach (var cursor in measureSetupEntityDeleted.CursorEntities)
            {
                //var cursorEntityDeleted = casyContext.CursorsDeleted.FirstOrDefault(c => c.CursorEntityId == cursor.CursorId);

                /*var cursorEntity = new CursorEntity
                {
                    LastModifiedAt = cursorEntityDeleted.LastModifiedAt,
                    LastModifiedBy = cursorEntityDeleted.LastModifiedBy,
                    CreatedAt = cursorEntityDeleted.CreatedAt,
                    CreatedBy = cursorEntityDeleted.CreatedBy,
                    Name = cursorEntityDeleted.Name,
                    Color = cursorEntityDeleted.Color,
                    Version = cursorEntityDeleted.Version,
                    IsDeadCellsCursor = cursorEntityDeleted.IsDeadCellsCursor,
                    MaxLimit = cursorEntityDeleted.MaxLimit,
                    MinLimit = cursorEntityDeleted.MinLimit,
                    Subpopulation = cursorEntityDeleted.Subpopulation
                };*/

                casyContext.CursorsDeleted.Remove(cursor);
                //casyContext.Cursors.Add(cursorEntity);

                //measureSetupEntity.CursorEntities.Add(cursorEntity);
            }

            foreach (var deviationControlItem in measureSetupEntityDeleted.DeviationControlItemEntities)
            {
                //var deviationControlItemEntityDeleted = casyContext.DeviationControlItemsDeleted.FirstOrDefault(d => d.DeviationControlItemEntityId == deviationControlItem.DeviationControlItemId);

                /*var deviationControlItemEntity = new DeviationControlItemEntity
                {
                    CreatedAt = deviationControlItemEntityDeleted.CreatedAt,
                    CreatedBy = deviationControlItemEntityDeleted.CreatedBy,
                    LastModifiedBy = deviationControlItemEntityDeleted.LastModifiedBy,
                    LastModifiedAt = deviationControlItemEntityDeleted.LastModifiedAt,
                    Version = deviationControlItemEntityDeleted.Version,
                    MaxLimit = deviationControlItemEntityDeleted.MaxLimit,
                    MeasureResultItemType = deviationControlItemEntityDeleted.MeasureResultItemType,
                    MinLimit = deviationControlItemEntityDeleted.MinLimit
                };*/
                casyContext.DeviationControlItemsDeleted.Remove(deviationControlItem);
                //casyContext.DeviationControlItems.Add(deviationControlItemEntity);
                //measureSetupEntity.DeviationControlItemEntities.Add(deviationControlItemEntity);
            }

            foreach (var auditTrail in measureSetupEntityDeleted.AuditTrailEntrieEntities)
            {
                //var auditTrailEntityDeleted =
                    //casyContext.AuditTrailEntriesDeleted.FirstOrDefault(x =>
                        //x.AuditTrailEntryEntityId == auditTrail.AuditTrailEntryId);

                /*var auditTrailEntity = new AuditTrailEntryEntity()
                {
                    Action = auditTrailEntityDeleted.Action,
                    ComputerName = auditTrailEntityDeleted.ComputerName,
                    DateChanged = auditTrailEntityDeleted.DateChanged,
                    EntityName = auditTrailEntityDeleted.EntityName,
                    NewValue = auditTrailEntityDeleted.NewValue,
                    OldValue = auditTrailEntityDeleted.OldValue,
                    PrimaryKeyValue = auditTrailEntityDeleted.PrimaryKeyValue,
                    PropertyName = auditTrailEntityDeleted.PropertyName,
                    SoftwareVersion = auditTrailEntityDeleted.SoftwareVersion,
                    UserChanged = auditTrailEntityDeleted.UserChanged
                };*/

                casyContext.AuditTrailEntriesDeleted.Remove(auditTrail);
                //casyContext.AuditTrailEntries.Add(auditTrailEntity);
                //measureSetupEntity.AuditTrailEntrieEntities.Add(auditTrailEntity);
            }

            //_measureSetupCache.Remove(measureSetup);

            casyContext.SaveChanges();

            //return GetMeasureSetup(measureSetupEntity.MeasureSetupEntityId);
        }

        public int SaveMeasureSetup(MeasureSetup measureSetup, bool ignoreAuditTrail)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                return SaveMeasureSetup(casyContext, measureSetup, ignoreAuditTrail).MeasureSetupEntityId;
            }
        }

        private MeasureSetupEntity SaveMeasureSetup(CasyContext2 casyContext, MeasureSetup measureSetup, bool ignoreAuditTrail)
        {
            var query = casyContext.MeasureSetups.Include("CursorEntities").Include("DeviationControlItemEntities").Where(ms => ms.MeasureSetupEntityId == measureSetup.MeasureSetupId);
            var measureSetupEntity = query.FirstOrDefault();

            if (measureSetupEntity == null)
            {
                measureSetupEntity = new MeasureSetupEntity
                {
                    AggregationCalculationMode = measureSetup.AggregationCalculationMode,
                    CapillarySize = measureSetup.CapillarySize,
                    DilutionFactor = measureSetup.DilutionFactor,
                    DilutionSampleVolume = measureSetup.DilutionSampleVolume,
                    DilutionCasyTonVolume = measureSetup.DilutionCasyTonVolume,
                    FromDiameter = measureSetup.FromDiameter,
                    IsDeviationControlEnabled = measureSetup.IsDeviationControlEnabled,
                    IsSmoothing = measureSetup.IsSmoothing,
                    IsTemplate = measureSetup.IsTemplate,
                    ManualAggrgationCalculationFactor = measureSetup.ManualAggregationCalculationFactor,
                    MeasureMode = measureSetup.MeasureMode,
                    Name = measureSetup.Name,
                    Repeats = measureSetup.Repeats,
                    ScalingMaxRange = measureSetup.ScalingMaxRange,
                    ScalingMode = measureSetup.ScalingMode,
                    SmoothingFactor = measureSetup.SmoothingFactor,
                    ToDiameter = measureSetup.ToDiameter,
                    UnitMode = measureSetup.UnitMode,
                    Volume = measureSetup.Volume,
                    VolumeCorrectionFactor = measureSetup.VolumeCorrectionFactor,
                    //ResultItemTypes = measureSetup.ResultItemTypes,
                    IsReadOnly = measureSetup.IsReadOnly,
                    AutoSaveName = measureSetup.AutoSaveName,
                    IsAutoSave = measureSetup.IsAutoSave,
                    DefaultExperiment = measureSetup.DefaultExperiment,
                    DefaultGroup = measureSetup.DefaultGroup,
                    IsAutoComment = measureSetup.IsAutoComment,
                    ChannelCount = measureSetup.ChannelCount,
                    HasSubpopulations = measureSetup.HasSubpopulations
                };

                casyContext.MeasureSetups.Add(measureSetupEntity);
                casyContext.SaveChanges(ignoreAuditTrail);
                measureSetup.MeasureSetupId = measureSetupEntity.MeasureSetupEntityId;

                if (!ignoreAuditTrail)
                {
                    measureSetup.CreatedAt = DateTimeOffsetExtensions.ParseAny(measureSetupEntity.CreatedAt);
                    measureSetup.CreatedBy = measureSetupEntity.CreatedBy;
                    measureSetup.LastModifiedBy = measureSetupEntity.LastModifiedBy;
                    measureSetup.LastModifiedAt = DateTimeOffsetExtensions.ParseAny(measureSetupEntity.LastModifiedAt);
                    measureSetup.Version = measureSetupEntity.Version;
                }

                _measureSetupCache.Add(measureSetup);
            }
            else
            {
                measureSetupEntity.AggregationCalculationMode = measureSetup.AggregationCalculationMode;
                measureSetupEntity.CapillarySize = measureSetup.CapillarySize;
                measureSetupEntity.DilutionFactor = measureSetup.DilutionFactor;
                measureSetupEntity.DilutionSampleVolume = measureSetup.DilutionSampleVolume;
                measureSetupEntity.DilutionCasyTonVolume = measureSetup.DilutionCasyTonVolume;
                measureSetupEntity.FromDiameter = measureSetup.FromDiameter;
                measureSetupEntity.IsDeviationControlEnabled = measureSetup.IsDeviationControlEnabled;
                measureSetupEntity.IsSmoothing = measureSetup.IsSmoothing;
                measureSetupEntity.IsTemplate = measureSetup.IsTemplate;
                measureSetupEntity.ManualAggrgationCalculationFactor = measureSetup.ManualAggregationCalculationFactor;
                measureSetupEntity.MeasureMode = measureSetup.MeasureMode;
                measureSetupEntity.Name = measureSetup.Name;
                measureSetupEntity.Repeats = measureSetup.Repeats;
                measureSetupEntity.ScalingMaxRange = measureSetup.ScalingMaxRange;
                measureSetupEntity.ScalingMode = measureSetup.ScalingMode;
                measureSetupEntity.SmoothingFactor = measureSetup.SmoothingFactor;
                measureSetupEntity.ToDiameter = measureSetup.ToDiameter;
                measureSetupEntity.UnitMode = measureSetup.UnitMode;
                measureSetupEntity.Volume = measureSetup.Volume;
                measureSetupEntity.VolumeCorrectionFactor = measureSetup.VolumeCorrectionFactor;
                measureSetupEntity.AssociatedMeasureResultEntityId = measureSetup.MeasureResult?.MeasureResultId ?? -1;
                //measureSetupEntity.ResultItemTypes = measureSetup.ResultItemTypes;
                measureSetupEntity.IsReadOnly = measureSetup.IsReadOnly;
                measureSetupEntity.AutoSaveName = measureSetup.AutoSaveName;
                measureSetupEntity.IsAutoSave = measureSetup.IsAutoSave;
                measureSetupEntity.DefaultExperiment = measureSetup.DefaultExperiment;
                measureSetupEntity.DefaultGroup = measureSetup.DefaultGroup;
                measureSetupEntity.IsAutoComment = measureSetup.IsAutoComment;
                measureSetupEntity.ChannelCount = measureSetup.ChannelCount;
                measureSetupEntity.HasSubpopulations = measureSetup.HasSubpopulations;

                casyContext.SaveChanges(ignoreAuditTrail);

                measureSetup.LastModifiedAt = DateTimeOffsetExtensions.ParseAny(measureSetupEntity.LastModifiedAt);
                measureSetup.LastModifiedBy = measureSetupEntity.LastModifiedBy;
                measureSetup.Version = measureSetupEntity.Version;
            }

            var entitiesToDelete = measureSetupEntity.CursorEntities.Where(mse => /*!mse.IsDelete &&*/ measureSetup.Cursors.All(c => c.CursorId != mse.CursorEntityId)).ToList();

            if (entitiesToDelete.Any())
            {
                foreach (var cursorEntity in entitiesToDelete)
                {
                    cursorEntity.AssociatedMeasureResultEntityId = measureSetup.MeasureResult?.MeasureResultId ?? -1;
                    casyContext.Cursors.Remove(cursorEntity);
                }
                casyContext.SaveChanges(ignoreAuditTrail);
            }

            foreach (var cursor in measureSetup.Cursors)
            {
                SaveCursor(casyContext, cursor, measureSetupEntity, ignoreAuditTrail);
            }

            var deviationEntitiesToDelete = measureSetupEntity.DeviationControlItemEntities.Where(item => /*!item.IsDelete &&*/ measureSetup.DeviationControlItems.All(dci => dci.DeviationControlItemId != item.DeviationControlItemEntityId)).ToList();
            if (deviationEntitiesToDelete.Any())
            {
                foreach (var deviationControlItemEntity in deviationEntitiesToDelete)
                {
                    casyContext.DeviationControlItems.Remove(deviationControlItemEntity);
                }
                casyContext.SaveChanges(ignoreAuditTrail);
            }

            foreach (var deviationControlItem in measureSetup.DeviationControlItems)
            {
                SaveDeviationControlItem(casyContext, deviationControlItem, measureSetupEntity, ignoreAuditTrail);
            }
            return measureSetupEntity;
        }

        private static void SaveCursor(CasyContext2 casyContext, Cursor cursor, MeasureSetupEntity measureSetupEntity,
            bool ignoreAuditTrail = false)
        {
            var cursorEntity = casyContext.Cursors.FirstOrDefault(c => c.CursorEntityId == cursor.CursorId);

            if (cursorEntity == null)
            {
                cursorEntity = new CursorEntity
                {
                    MaxLimit = cursor.MaxLimit,
                    MinLimit = cursor.MinLimit,
                    Name = cursor.Name,
                    Color = cursor.Color,
                    IsDeadCellsCursor = cursor.IsDeadCellsCursor,
                    MeasureSetupEntityId = cursor.MeasureSetup.MeasureSetupId,
                    MeasureSetupEntity = measureSetupEntity,
                    Subpopulation = cursor.Subpopulation
                };

                if (cursor.MeasureSetup.IsTemplate)
                {
                    cursorEntity.AssociatedMeasureResultEntityId = -1;
                }
                else if(cursor.MeasureSetup.MeasureResult != null)
                {
                    cursorEntity.AssociatedMeasureResultEntityId = cursor.MeasureSetup.MeasureResult.MeasureResultId;
                }
                else
                {
                    cursorEntity.AssociatedMeasureResultEntityId = -1;
                }

                casyContext.Cursors.Add(cursorEntity);
                casyContext.SaveChanges(ignoreAuditTrail);
                cursor.CursorId = cursorEntity.CursorEntityId;

                if (!ignoreAuditTrail) return;
                cursor.CreatedAt = DateTimeOffsetExtensions.ParseAny(cursorEntity.CreatedAt);
                cursor.CreatedBy = cursorEntity.CreatedBy;
                cursor.LastModifiedBy = cursorEntity.LastModifiedBy;
                cursor.LastModifiedAt = DateTimeOffsetExtensions.ParseAny(cursorEntity.LastModifiedAt);
                cursor.Version = cursorEntity.Version;
            }
            else
            {
                cursorEntity.MinLimit = cursor.MinLimit;
                cursorEntity.MaxLimit = cursor.MaxLimit;
                cursorEntity.Name = cursor.Name;
                cursorEntity.Color = cursor.Color;
                cursorEntity.IsDeadCellsCursor = cursor.IsDeadCellsCursor;
                cursorEntity.MeasureSetupEntityId = cursor.MeasureSetup.MeasureSetupId;
                cursorEntity.MeasureSetupEntity = measureSetupEntity;
                cursorEntity.Subpopulation = cursor.Subpopulation;

                if (cursor.MeasureSetup.IsTemplate)
                {
                    cursorEntity.AssociatedMeasureResultEntityId = -1;
                }
                else if (cursor.MeasureSetup.MeasureResult != null)
                {
                    cursorEntity.AssociatedMeasureResultEntityId = cursor.MeasureSetup.MeasureResult.MeasureResultId;
                }
                else
                {
                    cursorEntity.AssociatedMeasureResultEntityId = -1;
                }

                casyContext.SaveChanges(ignoreAuditTrail);

                //if (!ignoreAuditTrail) return;
                cursor.LastModifiedAt = DateTimeOffsetExtensions.ParseAny(cursorEntity.LastModifiedAt);
                cursor.LastModifiedBy = cursorEntity.LastModifiedBy;
                cursor.Version = cursorEntity.Version;
            }
        }

        private static void SaveDeviationControlItem(CasyContext2 casyContext, DeviationControlItem deviationControlItem, MeasureSetupEntity measureSetupEntity, bool ignoreAuditTrail = false)
        {
            var deviationControlItemEntity = casyContext.DeviationControlItems.FirstOrDefault(d => d.DeviationControlItemEntityId == deviationControlItem.DeviationControlItemId);

            if (deviationControlItemEntity == null)
            {
                deviationControlItemEntity = new DeviationControlItemEntity
                {
                    MeasureResultItemType = deviationControlItem.MeasureResultItemType,
                    MaxLimit = deviationControlItem.MaxLimit,
                    MinLimit = deviationControlItem.MinLimit,
                    MeasureSetupEntityId = deviationControlItem.MeasureSetup.MeasureSetupId,
                    MeasureSetupEntity = measureSetupEntity
                };

                casyContext.DeviationControlItems.Add(deviationControlItemEntity);
                casyContext.SaveChanges(ignoreAuditTrail);
                deviationControlItem.DeviationControlItemId = deviationControlItemEntity.DeviationControlItemEntityId;
            }
            else
            {
                deviationControlItemEntity.MinLimit = deviationControlItem.MinLimit;
                deviationControlItemEntity.MaxLimit = deviationControlItem.MaxLimit;
                deviationControlItemEntity.MeasureResultItemType = deviationControlItem.MeasureResultItemType;
                deviationControlItemEntity.MeasureSetupEntityId = deviationControlItem.MeasureSetup.MeasureSetupId;
                deviationControlItemEntity.MeasureSetupEntity = measureSetupEntity;

                casyContext.SaveChanges(ignoreAuditTrail);
            }
        }

        public IEnumerable<Tuple<string, int, int>> GetExperiments(string filter = "", bool includeDeleted = false)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                var result = new Dictionary<string, Tuple<string, int, int>>();
                var experimentQuery = casyContext.MeasureResults.Where(x => !x.IsTemporary).AsQueryable();

                if(!string.IsNullOrEmpty(filter))
                {
                    experimentQuery = experimentQuery.Where(x => x.Name.ToLower().Contains(filter.ToLower()));
                }

                var experiments = experimentQuery.Select(x =>  new { x.Experiment, x.Group, x.MeasureResultEntityId }).GroupBy(x => x.Experiment).ToList();

                if (includeDeleted)
                {
                    var deletedExperimentQuery = casyContext.MeasureResultsDeleted.Where(x => !x.IsTemporary).AsQueryable();

                    experiments = experiments.Union(deletedExperimentQuery.Select(x => new { x.Experiment, x.Group, x.MeasureResultEntityId }).GroupBy(x => x.Experiment).ToList()).ToList();
                }

                foreach (var expGroup in experiments)
                {
                    var grpGroup = expGroup.GroupBy(x => x.Group);

                    var key = expGroup.Key == null ? string.Empty : expGroup.Key;
                    if (result.TryGetValue(key, out var existing))
                    {
                        result[key] = new Tuple<string, int, int>(expGroup.Key, existing.Item2 + expGroup.Count(), existing.Item3 + grpGroup.Count());
                    }
                    else
                    {
                        result.Add(key, new Tuple<string, int, int>(expGroup.Key, expGroup.Count(), grpGroup.Count()));
                    }
                }

                return result.Values.AsEnumerable();
            }
        }

        public IEnumerable<Tuple<string, int>> GetGroups(string experiment, string filter = "", bool includeDeleted = false)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                var result = new Dictionary<string, Tuple<string, int>>();

                var groupQuery = casyContext.MeasureResults.Where(x => !x.IsTemporary);

                if (!string.IsNullOrEmpty(filter))
                {
                    groupQuery = groupQuery.Where(x => x.Name.ToLower().Contains(filter.ToLower()));
                }

                if (string.IsNullOrEmpty(experiment))
                {
                    groupQuery = groupQuery.Where(x => string.IsNullOrEmpty(x.Experiment));
                }
                else
                {
                    groupQuery = groupQuery.Where(x => x.Experiment == experiment);
                }

                var groups = groupQuery.Select(x => new { x.Group, x.MeasureResultEntityId }).GroupBy(x => x.Group).ToList();

                if (includeDeleted)
                {
                    var groupQueryDeleted = casyContext.MeasureResultsDeleted.Where(x => !x.IsTemporary);

                    if (string.IsNullOrEmpty(experiment))
                    {
                        groupQueryDeleted = groupQueryDeleted.Where(x => string.IsNullOrEmpty(x.Experiment));
                    }
                    else
                    {
                        groupQueryDeleted = groupQueryDeleted.Where(x => x.Experiment == experiment);
                    }

                    groups = groups.Union(groupQueryDeleted.Select(x => new { x.Group, x.MeasureResultEntityId }).GroupBy(x => x.Group).ToList()).ToList();
                }

                foreach (var grpGroup in groups)
                {
                    var key = grpGroup.Key == null ? string.Empty : grpGroup.Key;
                    if (result.TryGetValue(key, out var existing))
                    {
                        result[key] = new Tuple<string, int>(grpGroup.Key, existing.Item2 + grpGroup.Count());
                    }
                    else
                    {
                        result.Add(key, new Tuple<string, int>(grpGroup.Key, grpGroup.Count()));
                    }
                }

                return result.Values.AsEnumerable();
            }
        }

        public IEnumerable<MeasureResult> GetTemporaryMeasureResults(User loggedInUser)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                casyContext.Database.Log = Console.WriteLine;
                var result = new List<MeasureResult>();

                var measureResultEntities = casyContext
                    .MeasureResults
                    .Include("MeasureResultAnnotationEntities")
                    .Include("MeasureResultAnnotationEntities.AnnotationTypeEntity")
                    .Include("MeasureResultAccessMappings")
                    .Where(x => x.IsTemporary);

                foreach (var measureResultEntity in measureResultEntities)
                {
                    result.Add(EntityToModel(measureResultEntity));
                }

                return result;
            }
        }

        public IEnumerable<MeasureResult> GetMeasureResults(string experiment, string group, string filter = "", bool includeDeleted = false, bool nullAsNoValue = false, int maxItems = -1)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                var result = new List<MeasureResult>();

                var measureResultEntities = casyContext
                    .MeasureResults
                    .Include("MeasureResultAnnotationEntities")
                    .Include("MeasureResultAnnotationEntities.AnnotationTypeEntity")
                    .Include("MeasureResultAccessMappings").AsQueryable();

                measureResultEntities = measureResultEntities.Where(x => !x.IsTemporary);

                if (!string.IsNullOrEmpty(filter))
                {
                    measureResultEntities = measureResultEntities.Where(x => x.Name.ToLower().Contains(filter.ToLower()));
                }

                if (nullAsNoValue)
                {
                    if (experiment != null)
                    {
                        measureResultEntities = measureResultEntities.Where(x => x.Experiment == experiment);
                    }
                    if (group != null)
                    {
                        measureResultEntities = measureResultEntities.Where(x => x.Group == group);
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(experiment))
                    {
                        measureResultEntities = measureResultEntities.Where(x => x.Experiment == experiment);
                    }
                    else
                    {
                        measureResultEntities = measureResultEntities.Where(x => string.IsNullOrEmpty(x.Experiment));
                    }

                    if (!string.IsNullOrEmpty(group))
                    {
                        measureResultEntities = measureResultEntities.Where(x => x.Group == group);
                    }
                    else
                    {
                        measureResultEntities = measureResultEntities.Where(x => string.IsNullOrEmpty(x.Group));
                    }
                }

                if(maxItems > -1)
                {
                    measureResultEntities = measureResultEntities.Take(maxItems);
                }

                foreach (var measureResultEntity in measureResultEntities.ToList())
                {
                    result.Add(EntityToModel(measureResultEntity));
                }

                if (includeDeleted && result.Count < maxItems)
                {
                    var measureResultEntitiesDeleted = casyContext
                        .MeasureResultsDeleted.AsQueryable();


                    //if (nullAsNoValue)
                    //{
                        //measureResultEntitiesDeleted = measureResultEntitiesDeleted.Where(x => !x.IsTemporary && x.Experiment == experiment && x.Group == group);
                    //}
                    //else
                    //{
                        measureResultEntitiesDeleted = measureResultEntitiesDeleted.Where(x => !x.IsTemporary);
                        if (!string.IsNullOrEmpty(experiment))
                        {
                            measureResultEntitiesDeleted = measureResultEntitiesDeleted.Where(x => x.Experiment == experiment);
                        }
                        else
                        {
                            measureResultEntitiesDeleted = measureResultEntitiesDeleted.Where(x => string.IsNullOrEmpty(x.Experiment));
                        }

                        if (!string.IsNullOrEmpty(group))
                        {
                            measureResultEntitiesDeleted = measureResultEntitiesDeleted.Where(x => x.Group == group);
                        }
                        else
                        {
                            measureResultEntitiesDeleted = measureResultEntitiesDeleted.Where(x => string.IsNullOrEmpty(x.Group));
                        }
                    //}

                    measureResultEntitiesDeleted = measureResultEntitiesDeleted.Take(maxItems - result.Count);

                        foreach (var measureResultEntity in measureResultEntitiesDeleted)
                    {
                        result.Add(EntityToModel(measureResultEntity));
                    }
                }

                return result;
            }
        }

        public MeasureResult GetMeasureResult(int measureResultId, bool ignoreCache = false, bool isDeleted = false)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                var measureResult = _measureResultCache.FirstOrDefault(mr => mr.MeasureResultId == measureResultId);

                if (measureResult != null && !ignoreCache)
                {
                    return measureResult;
                }

                if (!isDeleted)
                {
                    var measureResultEntity = casyContext
                        .MeasureResults
                        .Include("MeasureResultAnnotationEntities")
                        .Include("MeasureResultAnnotationEntities.AnnotationTypeEntity")
                        .Include("MeasureResultAccessMappings")
                        .FirstOrDefault(mr => mr.MeasureResultEntityId == measureResultId);

                    if (measureResultEntity == null)
                    {
                        //measureResult = EntityToModel(measureResultEntity);
                        return null;
                    }
                    measureResult = EntityToModel(measureResultEntity);
                }
                else
                {
                    var measureResultEntityDeleted = casyContext
                        .MeasureResultsDeleted
                        //.Include("MeasureResultAnnotationEntities")
                        //.Include("MeasureResultAnnotationEntities.AnnotationTypeEntity")
                        //.Include("MeasureResultAccessMappings")
                        .FirstOrDefault(mr => mr.MeasureResultEntityId == measureResultId);

                    if (measureResultEntityDeleted == null)
                    {
                        return null;
                    }
                    measureResult = EntityToModel(measureResultEntityDeleted);
                }

                if (measureResult != null && !ignoreCache)
                {
                    _measureResultCache.Add(measureResult);
                }

                return measureResult;
            }
        }

        public MeasureResult GetMeasureResultByGuid(Guid guid)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                var measureResult = _measureResultCache.FirstOrDefault(mr => mr.MeasureResultGuid == guid);

                if (measureResult != null)
                {
                    return measureResult;
                }

                var measureResultEntity = casyContext
                    .MeasureResults
                    .Include("MeasureResultAnnotationEntities")
                    .Include("MeasureResultAnnotationEntities.AnnotationTypeEntity")
                    .Include("MeasureResultAccessMappings").ToList()
                    .FirstOrDefault(mr => mr.MeasureResultEntityGuid == guid);

                if (measureResultEntity == null)
                {
                    var measureResultEntityDeleted = casyContext
                        .MeasureResultsDeleted
                        //.Include("MeasureResultAnnotationEntities")
                        //.Include("MeasureResultAnnotationEntities.AnnotationTypeEntity")
                        //.Include("MeasureResultAccessMappings")
                        .ToList()
                        .FirstOrDefault(mr => mr.MeasureResultEntityGuid == guid);

                    if (measureResultEntityDeleted == null)
                    {
                        return null;
                    }

                    measureResult = EntityToModel(measureResultEntityDeleted);
                }
                else
                {
                    measureResult = EntityToModel(measureResultEntity);
                }

                if (measureResult != null)
                {
                    _measureResultCache.Add(measureResult);
                }

                return measureResult;
            }
        }

        private static MeasureResult EntityToModel(MeasureResultEntity measureResultEntity)
        {
            if (measureResultEntity == null) return null;

            var measureResult = new MeasureResult
            {
                Comment = measureResultEntity.Comment,
                IsTemporary = measureResultEntity.IsTemporary,
                MeasureResultGuid = measureResultEntity.MeasureResultEntityGuid,
                MeasureResultId = measureResultEntity.MeasureResultEntityId,
                Name = measureResultEntity.Name,
                SerialNumber = measureResultEntity.SerialNumber,
                CreatedAt = DateTimeOffsetExtensions.ParseAny(measureResultEntity.CreatedAt),
                CreatedBy = measureResultEntity.CreatedBy,
                LastModifiedAt = DateTimeOffsetExtensions.ParseAny(measureResultEntity.LastModifiedAt),
                LastModifiedBy = measureResultEntity.LastModifiedBy,
                //IsDelete = measureResultEntity.IsDelete,
                Version = measureResultEntity.Version,
                Experiment = measureResultEntity.Experiment,
                Group = measureResultEntity.Group,
                Color = measureResultEntity.Color,
                MeasuredAt = measureResultEntity.MeasuredAt,
                Origin = measureResultEntity.Origin,
                MeasuredAtTimeZone = string.IsNullOrEmpty(measureResultEntity.MeasuredAtTimeZone)
                    ? TimeZoneInfo.Local
                    : TimeZoneInfo.FindSystemTimeZoneById(measureResultEntity.MeasuredAtTimeZone),
                IsCfr = measureResultEntity.IsCfr,
                LastWeeklyClean = string.IsNullOrEmpty(measureResultEntity.LastWeeklyClean) ? null : new DateTimeOffset?(DateTimeOffsetExtensions.ParseAny(measureResultEntity.LastWeeklyClean))
            };

            foreach (var measureResultAnnotationEntity in measureResultEntity.MeasureResultAnnotationEntities)
            {
                var measureResultAnnotation = EntityToModel(measureResultAnnotationEntity);
                if (measureResultAnnotation == null) continue;
                measureResultAnnotation.MeasureResult = measureResult;
                measureResult.MeasureResultAnnotations.Add(measureResultAnnotation);
            }

            foreach (var entity in measureResultEntity.MeasureResultAccessMappings)
            {
                var accessMapping = new MeasureResultAccessMapping
                {
                    MeasureResultAccessMappingId = entity.MeasureResultAccessMappingId,
                    CanRead = entity.CanRead,
                    CanWrite = entity.CanWrite,
                    MeasureResult = measureResult,
                    UserGroupId = entity.UserGroupEntityId,
                    UserId = entity.UserEntityId
                };

                measureResult.AccessMappings.Add(accessMapping);
            }

            return measureResult;

        }

        private static MeasureResult EntityToModel(MeasureResultEntity_Deleted measureResultEntityDeleted)
        {
            if (measureResultEntityDeleted == null) return null;

            var measureResult = new MeasureResult
            {
                Comment = measureResultEntityDeleted.Comment,
                IsTemporary = measureResultEntityDeleted.IsTemporary,
                MeasureResultGuid = measureResultEntityDeleted.MeasureResultEntityGuid,
                MeasureResultId = measureResultEntityDeleted.MeasureResultEntityId,
                Name = measureResultEntityDeleted.Name,
                SerialNumber = measureResultEntityDeleted.SerialNumber,
                CreatedAt = DateTimeOffsetExtensions.ParseAny(measureResultEntityDeleted.CreatedAt),
                CreatedBy = measureResultEntityDeleted.CreatedBy,
                LastModifiedAt = DateTimeOffsetExtensions.ParseAny(measureResultEntityDeleted.LastModifiedAt),
                LastModifiedBy = measureResultEntityDeleted.LastModifiedBy,
                Version = measureResultEntityDeleted.Version,
                Experiment = measureResultEntityDeleted.Experiment,
                Group = measureResultEntityDeleted.Group,
                Color = measureResultEntityDeleted.Color,
                MeasuredAt = measureResultEntityDeleted.MeasuredAt,
                Origin = measureResultEntityDeleted.Origin,
                MeasuredAtTimeZone = string.IsNullOrEmpty(measureResultEntityDeleted.MeasuredAtTimeZone)
                    ? TimeZoneInfo.Local
                    : TimeZoneInfo.FindSystemTimeZoneById(measureResultEntityDeleted.MeasuredAtTimeZone),
                IsCfr = measureResultEntityDeleted.IsCfr,
                IsDeletedResult = true
            };

            //foreach (var measureResultAnnotationEntity in measureResultEntityDeleted.MeasureResultAnnotationEntities)
            //{
                //var measureResultAnnotation = EntityToModel(measureResultAnnotationEntity);
                //if (measureResultAnnotation == null) continue;
                //measureResultAnnotation.MeasureResult = measureResult;
                //measureResult.MeasureResultAnnotations.Add(measureResultAnnotation);
            //}

            //foreach (var entity in measureResultEntityDeleted.MeasureResultAccessMappings)
            //{
                //var accessMapping = new MeasureResultAccessMapping
                //{
                    //MeasureResultAccessMappingId = entity.MeasureResultAccessMappingId,
                    //CanRead = entity.CanRead,
                    //CanWrite = entity.CanWrite,
                    //MeasureResult = measureResult,
                    //UserGroupId = entity.UserGroupEntityId,
                    //UserId = entity.UserEntityId
                //};

                //measureResult.AccessMappings.Add(accessMapping);
            //}

            return measureResult;

        }

        private static MeasureResultAnnotation EntityToModel(MeasureResultAnnotationEntity measureResultAnnotationEntity)
        {
            if (measureResultAnnotationEntity == null) return null;

            var measureResultAnnotation = new MeasureResultAnnotation
            {
                MeasureResultAnnotationId = measureResultAnnotationEntity.MeasureResultAnnotationEntityId,
                Value = measureResultAnnotationEntity.Value
            };

            if (measureResultAnnotationEntity.AnnotationTypeEntity != null)
            {
                measureResultAnnotation.AnnotationType =
                    EntityToModel(measureResultAnnotationEntity.AnnotationTypeEntity);
            }
            return measureResultAnnotation;
        }

        private static AnnotationType EntityToModel(AnnotationTypeEntity annotationTypeEntity)
        {
            if (annotationTypeEntity != null)
            {
                return new AnnotationType
                {
                    AnnotationTypeId = annotationTypeEntity.AnnotationTypeEntityId,
                    AnnottationTypeName = annotationTypeEntity.AnnottationTypeName
                };
            }
            return null;
        }

        public int GetMeasureResultsCount()
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                return casyContext.MeasureResults.Count() + casyContext.MeasureResultsDeleted.Count();
            }
        }

        public MeasureResult LoadDisplayData(MeasureResult measureResult)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                var id = measureResult.MeasureResultId;
                int measureSetupId, origMeasureSetupId;

                if (!measureResult.IsDeletedResult)
                {
                    var measureResultEntity = casyContext.MeasureResults.Where(mr => mr.MeasureResultEntityId == id)
                        .Include("MeasureResultDataEntities").Include("MeasureSetupEntity")
                        .Include("OriginalMeasureSetupEntity").Include("AuditTrailEntrieEntities").FirstOrDefault();

                    if (measureResultEntity == null) return measureResult;
                    lock (((ICollection) measureResult.MeasureResultDatas).SyncRoot)
                    {
                        foreach (var measureResultDataEntity in measureResultEntity.MeasureResultDataEntities)
                        {
                            if (measureResult.MeasureResultDatas.Any(mrd =>
                                mrd.MeasureResultDataId == measureResultDataEntity.MeasureResultDataEntityId)) continue;

                            var measureResultData = GetMeasureResultData(casyContext,
                                measureResultDataEntity.MeasureResultDataEntityId);
                            measureResultData.MeasureResult = measureResult;
                            measureResult.MeasureResultDatas.Add(measureResultData);
                        }
                    }

                    lock (((ICollection)measureResult.AuditTrailEntries).SyncRoot)
                    {
                        measureResult.AuditTrailEntries.Clear();
                        foreach (var auditTrailEntryEntity in measureResultEntity.AuditTrailEntrieEntities)
                        {
                            var auditTrailEntry = GetAuditTrailEntry(casyContext, auditTrailEntryEntity.AuditTrailEntryEntityId);
                            auditTrailEntry.MeasureResult = measureResult;
                            measureResult.AuditTrailEntries.Add(auditTrailEntry);
                        }
                    }

                    measureSetupId = measureResultEntity.MeasureSetupEntity.MeasureSetupEntityId;
                    origMeasureSetupId = measureResultEntity.OriginalMeasureSetupEntity.MeasureSetupEntityId;
                }
                else
                {
                    var measureResultEntity = casyContext.MeasureResultsDeleted.Where(mr => mr.MeasureResultEntityId == id)
                        .Include("MeasureResultDataEntities").Include("MeasureSetupEntity")
                        .Include("OriginalMeasureSetupEntity").Include("AuditTrailEntrieEntities").FirstOrDefault();

                    if (measureResultEntity == null) return measureResult;
                    lock (((ICollection)measureResult.MeasureResultDatas).SyncRoot)
                    {
                        foreach (var measureResultDataEntity in measureResultEntity.MeasureResultDataEntities)
                        {
                            if (measureResult.MeasureResultDatas.Any(mrd =>
                                mrd.MeasureResultDataId == measureResultDataEntity.MeasureResultDataEntityId)) continue;

                            var measureResultData = GetMeasureResultData(casyContext,
                                measureResultDataEntity.MeasureResultDataEntityId, isDeleted: true);
                            measureResultData.MeasureResult = measureResult;
                            measureResult.MeasureResultDatas.Add(measureResultData);
                        }
                    }

                    lock (((ICollection)measureResult.AuditTrailEntries).SyncRoot)
                    {
                        measureResult.AuditTrailEntries.Clear();
                        foreach (var auditTrailEntryEntity in measureResultEntity.AuditTrailEntrieEntities)
                        {
                            var auditTrailEntry = GetAuditTrailEntry(casyContext, auditTrailEntryEntity.AuditTrailEntryEntityId);
                            auditTrailEntry.MeasureResult = measureResult;
                            measureResult.AuditTrailEntries.Add(auditTrailEntry);
                        }
                    }

                    measureSetupId = measureResultEntity.MeasureSetupEntity.MeasureSetupEntityId;
                    origMeasureSetupId = measureResultEntity.OriginalMeasureSetupEntity.MeasureSetupEntityId;
                }

                measureResult.MeasureSetup = GetMeasureSetup(casyContext, measureSetupId, isDeletedResult: measureResult.IsDeletedResult);
                measureResult.MeasureSetup.MeasureResult = measureResult;

                measureResult.OriginalMeasureSetup = GetMeasureSetup(casyContext, origMeasureSetupId, isDeletedResult: measureResult.IsDeletedResult);
                measureResult.OriginalMeasureSetup.MeasureResult = measureResult;

                return measureResult;
            }
        }

        public MeasureResult LoadExportData(MeasureResult measureResult)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                var id = measureResult.MeasureResultId;

                if (!measureResult.IsDeletedResult)
                {
                    var measureResultEntity = casyContext.MeasureResults.Include("AuditTrailEntrieEntities")
                        .FirstOrDefault(mr => mr.MeasureResultEntityId == id);

                    if (measureResultEntity == null) return measureResult;

                    lock (((ICollection) measureResult.AuditTrailEntries).SyncRoot)
                    {
                        measureResult.AuditTrailEntries.Clear();
                        foreach (var auditTrailEntryEntity in measureResultEntity.AuditTrailEntrieEntities)
                        {
                            var auditTrailEntry =
                                GetAuditTrailEntry(casyContext, auditTrailEntryEntity.AuditTrailEntryEntityId);
                            auditTrailEntry.MeasureResult = measureResult;
                            measureResult.AuditTrailEntries.Add(auditTrailEntry);
                        }
                    }
                }
                else
                {
                    var measureResultEntity = casyContext.MeasureResultsDeleted.Include("AuditTrailEntrieEntities")
                        .FirstOrDefault(mr => mr.MeasureResultEntityId == id);

                    if (measureResultEntity == null) return measureResult;

                    lock (((ICollection)measureResult.AuditTrailEntries).SyncRoot)
                    {
                        measureResult.AuditTrailEntries.Clear();
                        foreach (var auditTrailEntryEntity in measureResultEntity.AuditTrailEntrieEntities)
                        {
                            var auditTrailEntry =
                                GetAuditTrailEntry(casyContext, auditTrailEntryEntity.AuditTrailEntryEntityId);
                            auditTrailEntry.MeasureResult = measureResult;
                            measureResult.AuditTrailEntries.Add(auditTrailEntry);
                        }
                    }
                }

                return measureResult;
            }
        }

        public MeasureSetup LoadExportData(MeasureSetup template)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                var id = template.MeasureSetupId;

                if (template.IsDeletedSetup)
                {
                    var templateEntity = casyContext.MeasureSetupsDeleted.Include("AuditTrailEntrieEntities")
                        .FirstOrDefault(mr => mr.MeasureSetupEntityId == id);

                    if (templateEntity == null) return template;

                    lock (((ICollection)template.AuditTrailEntries).SyncRoot)
                    {
                        template.AuditTrailEntries.Clear();
                        foreach (var auditTrailEntryEntity in templateEntity.AuditTrailEntrieEntities)
                        {
                            var auditTrailEntry =
                                GetAuditTrailEntry(casyContext, auditTrailEntryEntity.AuditTrailEntryEntityId);
                            auditTrailEntry.Template = template;
                            template.AuditTrailEntries.Add(auditTrailEntry);
                        }
                    }
                }
                else
                {
                    var templateEntity = casyContext.MeasureSetups.Include("AuditTrailEntrieEntities")
                        .FirstOrDefault(mr => mr.MeasureSetupEntityId == id);

                    if (templateEntity == null) return template;

                    lock (((ICollection) template.AuditTrailEntries).SyncRoot)
                    {
                        template.AuditTrailEntries.Clear();
                        foreach (var auditTrailEntryEntity in templateEntity.AuditTrailEntrieEntities)
                        {
                            var auditTrailEntry =
                                GetAuditTrailEntry(casyContext, auditTrailEntryEntity.AuditTrailEntryEntityId);
                            auditTrailEntry.Template = template;
                            template.AuditTrailEntries.Add(auditTrailEntry);
                        }
                    }
                }

                return template;
            }
        }

        public void DeleteMeasureResults(IList<MeasureResult> measureResults)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                var loggedInUser = _environmentService.GetEnvironmentInfo("LoggedInUserName") as string;

                foreach (var measureResult in measureResults)
                {
                    if (measureResult == null) continue;
                   
                    //if (measureResultEntity == null) continue;
                    var measureResultEntityDeleted = new MeasureResultEntity_Deleted()
                    {
                        DeletedAt = DateTimeOffset.UtcNow.ToString(),
                        DeletedBy = loggedInUser,
                    };

                    
                    //casyContext.MeasureResults.Remove(measureResultEntity);

                    var deletedSetup = DeleteMeasureSetup(casyContext, measureResult.MeasureSetup);
                    if (deletedSetup != null)
                    {
                        measureResultEntityDeleted.MeasureSetupEntity = deletedSetup;
                    }

                    var deletedOrigSetup = DeleteMeasureSetup(casyContext, measureResult.OriginalMeasureSetup);
                    if (deletedOrigSetup != null)
                    {
                        measureResultEntityDeleted.OriginalMeasureSetupEntity = deletedOrigSetup;
                    }

                    var measureResultEntity = casyContext.MeasureResults.FirstOrDefault(mr => mr.MeasureResultEntityId == measureResult.MeasureResultId);

                    if(measureResultEntity == null) continue;
                    measureResultEntityDeleted.Color = measureResultEntity.Color;
                    measureResultEntityDeleted.Comment = measureResultEntity.Comment;
                    measureResultEntityDeleted.CreatedAt = measureResultEntity.CreatedAt;
                    measureResultEntityDeleted.CreatedBy = measureResultEntity.CreatedBy;
                    measureResultEntityDeleted.Experiment = measureResultEntity.Experiment;
                    measureResultEntityDeleted.Group = measureResultEntity.Group;
                    measureResultEntityDeleted.IsCfr = measureResultEntity.IsCfr;
                    measureResultEntityDeleted.IsTemporary = measureResultEntity.IsTemporary;
                    measureResultEntityDeleted.Version = measureResultEntity.Version;
                    measureResultEntityDeleted.LastModifiedAt = measureResultEntity.LastModifiedAt;
                    measureResultEntityDeleted.LastModifiedBy = measureResultEntity.LastModifiedBy;
                    measureResultEntityDeleted.MeasureResultEntityGuid = measureResultEntity.MeasureResultEntityGuid;
                    measureResultEntityDeleted.MeasuredAt = measureResultEntity.MeasuredAt;
                    measureResultEntityDeleted.MeasuredAtTimeZone = measureResultEntity.MeasuredAtTimeZone;
                    measureResultEntityDeleted.Name = measureResultEntity.Name;
                    measureResultEntityDeleted.Origin = measureResultEntity.Origin;
                    measureResultEntityDeleted.SerialNumber = measureResultEntity.SerialNumber;
                    
                    casyContext.MeasureResultsDeleted.Add(measureResultEntityDeleted);

                    var measureResultDataToDelete = new List<MeasureResultDataEntity>();
                    foreach (var measureResultData in measureResult.MeasureResultDatas)
                    {
                        var measureResultDataEntity = casyContext.MeasureResultData.FirstOrDefault(mrd => mrd.MeasureResultDataEntityId == measureResultData.MeasureResultDataId);
                        if(measureResultDataEntity == null) continue;

                        var measureResultDataDeleted = new MeasureResultDataEntity_Deleted()
                        {
                            DeletedAt = DateTimeOffset.UtcNow.ToString(),
                            DeletedBy = loggedInUser,
                            LastModifiedAt = measureResultDataEntity.LastModifiedAt,
                            LastModifiedBy = measureResultDataEntity.LastModifiedBy,
                            CreatedBy = measureResultDataEntity.CreatedBy,
                            CreatedAt = measureResultDataEntity.CreatedAt,
                            Color = measureResultDataEntity.Color,
                            AboveCalibrationLimitCount = measureResultDataEntity.AboveCalibrationLimitCount,
                            BelowCalibrationLimitCount = measureResultDataEntity.BelowCalibrationLimitCount,
                            BelowMeasureLimtCount = measureResultDataEntity.BelowMeasureLimtCount,
                            ConcentrationTooHigh = measureResultDataEntity.ConcentrationTooHigh,
                            DataBlock = measureResultDataEntity.DataBlock,
                            MeasureResultEntity = measureResultEntityDeleted
                        };
                        casyContext.MeasureResultDataDeleted.Add(measureResultDataDeleted);
                        //casyContext.MeasureResultData.Remove(measureResultDataEntity);
                        measureResultDataToDelete.Add(measureResultDataEntity);
                    }

                    var auditTrailToDelete = new List<AuditTrailEntryEntity>();
                    foreach (var auditTrail in measureResult.AuditTrailEntries)
                    {
                        var entity = casyContext.AuditTrailEntries.FirstOrDefault(d => d.AuditTrailEntryEntityId == auditTrail.AuditTrailEntryId);

                        if(entity == null) continue;
                        
                        var auditTrailDeleted = new AuditTrailEntryEntity_Deleted()
                        {
                            Action = entity.Action,
                            ComputerName = entity.ComputerName,
                            DateChanged = entity.DateChanged,
                            EntityName = entity.EntityName,
                            NewValue = entity.NewValue,
                            OldValue = entity.OldValue,
                            PrimaryKeyValue = entity.PrimaryKeyValue,
                            PropertyName = entity.PropertyName,
                            SoftwareVersion = entity.SoftwareVersion,
                            UserChanged = entity.UserChanged
                        };
                        measureResultEntityDeleted.AuditTrailEntrieEntities.Add(auditTrailDeleted);
                        //casyContext.AuditTrailEntries.Remove(entity);
                        casyContext.AuditTrailEntriesDeleted.Add(auditTrailDeleted);

                        auditTrailToDelete.Add(entity);
                    }

                    //casyContext.SaveChanges();
                    foreach (var measureResultData in measureResultDataToDelete)
                    {
                        casyContext.MeasureResultData.Remove(measureResultData);
                    }

                    foreach (var auditTrail in auditTrailToDelete)
                    {
                        casyContext.AuditTrailEntries.Remove(auditTrail);
                    }

                    casyContext.MeasureResults.Remove(measureResultEntity);

                    casyContext.SaveChanges();

                    _measureResultCache.Remove(measureResult);
                }
            }
        }

        public void RemoveDeletedMeasureResult(int deletedMeasureResultId)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                //if (measureResult == null) return null;
                var measureResultEntityDeleted = casyContext.MeasureResultsDeleted.Include(x => x.MeasureResultDataEntities).Include(x => x.AuditTrailEntrieEntities).FirstOrDefault(mr => mr.MeasureResultEntityId == deletedMeasureResultId);

                if (measureResultEntityDeleted == null) return;

                /*var measureResultEntity = new MeasureResultEntity
                {
                    Color = measureResultEntityDeleted.Color,
                    Comment = measureResultEntityDeleted.Comment,
                    CreatedAt = measureResultEntityDeleted.CreatedAt,
                    CreatedBy = measureResultEntityDeleted.CreatedBy,
                    Experiment = measureResultEntityDeleted.Experiment,
                    Group = measureResultEntityDeleted.Group,
                    IsCfr = measureResultEntityDeleted.IsCfr,
                    Version = measureResultEntityDeleted.Version,
                    IsTemporary = measureResultEntityDeleted.IsTemporary,
                    LastModifiedAt = measureResultEntityDeleted.LastModifiedAt,
                    LastModifiedBy = measureResultEntityDeleted.LastModifiedBy,
                    MeasureResultEntityGuid = measureResultEntityDeleted.MeasureResultEntityGuid,
                    MeasuredAt = measureResultEntityDeleted.MeasuredAt,
                    MeasuredAtTimeZone = measureResultEntityDeleted.MeasuredAtTimeZone,
                    Name = measureResultEntityDeleted.Name,
                    Origin = measureResultEntityDeleted.Origin,
                    SerialNumber = measureResultEntityDeleted.SerialNumber
                };*/

                casyContext.MeasureResultsDeleted.Remove(measureResultEntityDeleted);
                //casyContext.MeasureResults.Add(measureResultEntity);

                if (measureResultEntityDeleted != null && measureResultEntityDeleted.MeasureSetupEntity != null)
                {
                    RemoveDeletedMeasureSetup(casyContext,
                        measureResultEntityDeleted.MeasureSetupEntity.MeasureSetupEntityId);
                }

                if (measureResultEntityDeleted != null && measureResultEntityDeleted.OriginalMeasureSetupEntity != null)
                {
                    RemoveDeletedMeasureSetup(casyContext,
                        measureResultEntityDeleted.OriginalMeasureSetupEntity.MeasureSetupEntityId);
                }

                if (measureResultEntityDeleted != null && measureResultEntityDeleted.MeasureResultDataEntities != null)
                {
                    foreach (var measureResultDataEntityDeleted in measureResultEntityDeleted.MeasureResultDataEntities)
                    {
                        //var measureResultDataEntityDeleted = casyContext.MeasureResultDataDeleted.FirstOrDefault(mrd => mrd.MeasureResultDataEntityId == measureResultData.MeasureResultDataId);

                        /*var measureResultDataEntity = new MeasureResultDataEntity
                        {
                            CreatedBy = measureResultDataEntityDeleted.CreatedBy,
                            LastModifiedAt = measureResultDataEntityDeleted.LastModifiedAt,
                            LastModifiedBy = measureResultDataEntityDeleted.LastModifiedBy,
                            CreatedAt = measureResultDataEntityDeleted.CreatedAt,
                            Color = measureResultDataEntityDeleted.Color,
                            AboveCalibrationLimitCount = measureResultDataEntityDeleted.AboveCalibrationLimitCount,
                            BelowCalibrationLimitCount = measureResultDataEntityDeleted.BelowCalibrationLimitCount,
                            BelowMeasureLimtCount = measureResultDataEntityDeleted.BelowMeasureLimtCount,
                            ConcentrationTooHigh = measureResultDataEntityDeleted.ConcentrationTooHigh,
                            DataBlock = measureResultDataEntityDeleted.DataBlock
                        };*/
                        casyContext.MeasureResultDataDeleted.Remove(measureResultDataEntityDeleted);
                        //casyContext.MeasureResultData.Add(measureResultDataEntity);
                        //measureResultEntity.MeasureResultDataEntities.Add(measureResultDataEntity);
                    }
                }

                if (measureResultEntityDeleted != null && measureResultEntityDeleted.AuditTrailEntrieEntities != null)
                {
                    foreach (var auditTrailEntityDeleted in measureResultEntityDeleted.AuditTrailEntrieEntities)
                    {
                        //var auditTrailEntityDeleted =
                        //casyContext.AuditTrailEntriesDeleted.FirstOrDefault(x =>
                        //x.AuditTrailEntryEntityId == auditTrail.AuditTrailEntryId);

                        /*var auditTrailEntity = new AuditTrailEntryEntity()
                        {
                            Action = auditTrailEntityDeleted.Action,
                            ComputerName = auditTrailEntityDeleted.ComputerName,
                            DateChanged = auditTrailEntityDeleted.DateChanged,
                            EntityName = auditTrailEntityDeleted.EntityName,
                            NewValue = auditTrailEntityDeleted.NewValue,
                            OldValue = auditTrailEntityDeleted.OldValue,
                            PrimaryKeyValue = auditTrailEntityDeleted.PrimaryKeyValue,
                            PropertyName = auditTrailEntityDeleted.PropertyName,
                            SoftwareVersion = auditTrailEntityDeleted.SoftwareVersion,
                            UserChanged = auditTrailEntityDeleted.UserChanged
                        };*/

                        casyContext.AuditTrailEntriesDeleted.Remove(auditTrailEntityDeleted);
                        //casyContext.AuditTrailEntries.Add(auditTrailEntity);
                        //measureResultEntity.AuditTrailEntrieEntities.Add(auditTrailEntity);
                    }
                }

                casyContext.SaveChanges();

                //_measureResultCache.Remove(measureResult);

                //var result = GetMeasureResult(measureResultEntity.MeasureResultEntityId);
                //result = LoadDisplayData(result);
                //result = LoadExportData(result);
                //return result;
            }
        }

        public int SaveMeasureResult(MeasureResult measureResult, bool ignoreAuditTrail = false, bool storeExistingAuditTrail = false)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                var measureResultEntity = casyContext.MeasureResults.Include("MeasureResultDataEntities").Include("MeasureSetupEntity").Include("OriginalMeasureSetupEntity").FirstOrDefault(mr => mr.MeasureResultEntityId == measureResult.MeasureResultId);

                if (measureResultEntity == null)
                {
                    var measureSetupEntity = SaveMeasureSetup(casyContext, measureResult.MeasureSetup, ignoreAuditTrail);
                    var origMeasureSetupEntity = SaveMeasureSetup(casyContext, measureResult.OriginalMeasureSetup, ignoreAuditTrail);

                    measureResultEntity = new MeasureResultEntity
                    {
                        Comment = measureResult.Comment,
                        IsTemporary = measureResult.IsTemporary,
                        MeasureResultEntityGuid = measureResult.MeasureResultGuid,
                        MeasureResultEntityId = measureResult.MeasureResultId,
                        Name = measureResult.Name,
                        SerialNumber = measureResult.SerialNumber,
                        MeasureSetupEntity = measureSetupEntity,
                        OriginalMeasureSetupEntity = origMeasureSetupEntity,
                        Experiment = measureResult.Experiment,
                        Group = measureResult.Group,
                        Color = measureResult.Color,
                        MeasuredAt = measureResult.MeasuredAt,
                        Origin = measureResult.Origin,
                        MeasuredAtTimeZone = measureResult.MeasuredAtTimeZone == null ? TimeZoneInfo.Local.Id : measureResult.MeasuredAtTimeZone.Id,
                        IsCfr = measureResult.IsCfr
                    };

                    casyContext.MeasureResults.Add(measureResultEntity);
                    casyContext.SaveChanges(ignoreAuditTrail);

                    measureResult.MeasureResultId = measureResultEntity.MeasureResultEntityId;

                    if (!ignoreAuditTrail)
                    {
                        measureResult.CreatedAt = DateTimeOffsetExtensions.ParseAny(measureResultEntity.CreatedAt);
                        measureResult.CreatedBy = measureResultEntity.CreatedBy;
                        measureResult.LastModifiedBy = measureResultEntity.LastModifiedBy;
                        measureResult.LastModifiedAt = DateTimeOffsetExtensions.ParseAny(measureResultEntity.LastModifiedAt);
                        measureResult.Version = measureResultEntity.Version;
                    }

                    _measureResultCache.Add(measureResult);
                }
                else
                {
                    measureResultEntity.Comment = measureResult.Comment;
                    measureResultEntity.IsTemporary = measureResult.IsTemporary;
                    measureResultEntity.MeasureResultEntityGuid = measureResult.MeasureResultGuid;
                    measureResultEntity.Name = measureResult.Name;
                    measureResultEntity.SerialNumber = measureResult.SerialNumber;
                    measureResultEntity.Experiment = measureResult.Experiment;
                    measureResultEntity.Group = measureResult.Group;
                    measureResultEntity.Color = measureResult.Color;
                    measureResultEntity.MeasuredAt = measureResult.MeasuredAt;
                    measureResultEntity.Origin = measureResult.Origin;
                    measureResultEntity.MeasuredAtTimeZone = measureResult.MeasuredAtTimeZone == null ? TimeZoneInfo.Local.Id : measureResult.MeasuredAtTimeZone.Id;
                    measureResultEntity.IsCfr = measureResult.IsCfr;

                    casyContext.SaveChanges(ignoreAuditTrail);

                    if (!ignoreAuditTrail)
                    {
                        measureResult.LastModifiedAt = DateTimeOffsetExtensions.ParseAny(measureResultEntity.LastModifiedAt);
                        measureResult.LastModifiedBy = measureResultEntity.LastModifiedBy;
                        measureResult.Version = measureResultEntity.Version;
                    }
                }

                foreach (var measureResultData in measureResult.MeasureResultDatas)
                {
                    SaveMeasureResultData(casyContext, measureResultData);
                }

                var annotationEntitiesToDelete = measureResultEntity.MeasureResultAnnotationEntities.Where(entity => measureResult.MeasureResultAnnotations.All(mra => mra.MeasureResultAnnotationId != entity.MeasureResultAnnotationEntityId)).ToList();
                if (annotationEntitiesToDelete.Any())
                {
                    foreach (var annotationsEntity in annotationEntitiesToDelete)
                    {
                        casyContext.MeasureResultAnnotations.Remove(annotationsEntity);
                    }
                    casyContext.SaveChanges(ignoreAuditTrail);
                }

                foreach (var measureResultAnnotation in measureResult.MeasureResultAnnotations)
                {
                    SaveMeasureResultAnnotation(casyContext, measureResultAnnotation, ignoreAuditTrail);
                }

                foreach(var accessMapping in measureResult.AccessMappings)
                {
                    SaveMeasureResultAccessMapping(casyContext, accessMapping);
                }

                //SaveMeasureSetup(casyContext, measureResult.OriginalMeasureSetup, ignoreAuditTrail);

                if (!storeExistingAuditTrail) return measureResultEntity.MeasureResultEntityId;
                foreach (var auditTrailEntry in measureResult.AuditTrailEntries)
                {
                    SaveAuditTrailEntry(casyContext, auditTrailEntry);
                }

                return measureResultEntity.MeasureResultEntityId;
            }
        }

        private static void SaveMeasureResultAccessMapping(CasyContext2 casyContext,
            MeasureResultAccessMapping measureResultAccessMapping)
        {
            var accessMappingEntity = casyContext.MeasureResultAccessMappings.FirstOrDefault(mr => mr.MeasureResultAccessMappingId == measureResultAccessMapping.MeasureResultAccessMappingId);

            if (accessMappingEntity == null)
            {
                var measureResultEntity = casyContext.MeasureResults.FirstOrDefault(x =>
                    x.MeasureResultEntityId == measureResultAccessMapping.MeasureResult.MeasureResultId);

                accessMappingEntity = new Entities.MeasureResultAccessMapping()
                {
                    MeasureResultAccessMappingId = measureResultAccessMapping.MeasureResultAccessMappingId,
                    CanRead = measureResultAccessMapping.CanRead,
                    CanWrite = measureResultAccessMapping.CanWrite,
                    MeasureResultEntityId = measureResultEntity.MeasureResultEntityId,
                    UserEntityId = measureResultAccessMapping.UserId,
                    UserGroupEntityId = measureResultAccessMapping.UserGroupId
                };
                casyContext.MeasureResultAccessMappings.Add(accessMappingEntity);
                casyContext.SaveChanges();
                measureResultAccessMapping.MeasureResultAccessMappingId = accessMappingEntity.MeasureResultAccessMappingId;
            }
            else
            {
                accessMappingEntity.CanRead = measureResultAccessMapping.CanRead;
                accessMappingEntity.CanWrite = measureResultAccessMapping.CanWrite;
                //accessMappingEntity.MeasureResultEntityId = measureResultAccessMapping.MeasureResult.MeasureResultId;
                accessMappingEntity.MeasureResultAccessMappingId = measureResultAccessMapping.MeasureResultAccessMappingId;
                accessMappingEntity.UserEntityId = measureResultAccessMapping.UserId;
                accessMappingEntity.UserGroupEntityId = measureResultAccessMapping.UserGroupId;

                casyContext.SaveChanges();
            }
        }

        private static void SaveAuditTrailEntry(CasyContext2 casyContext, AuditTrailEntry auditTrailEntry)
        {
            var auditTrailEntryEntity = new AuditTrailEntryEntity
            {
                Action = auditTrailEntry.Action,
                ComputerName = auditTrailEntry.ComputerName,
                DateChanged = auditTrailEntry.DateChanged.ToString(),
                EntityName = auditTrailEntry.EntityName,
                NewValue = auditTrailEntry.NewValue,
                OldValue = auditTrailEntry.OldValue,
                PrimaryKeyValue = auditTrailEntry.PrimaryKeyValue,
                PropertyName = auditTrailEntry.PropertyName,
                SoftwareVersion = auditTrailEntry.SoftwareVersion,
                UserChanged = auditTrailEntry.UserChanged,
                MeasureResultEntityId = auditTrailEntry.MeasureResult.MeasureResultId
            };

            casyContext.AuditTrailEntries.Add(auditTrailEntryEntity);
            casyContext.SaveChanges();
            auditTrailEntry.AuditTrailEntryId = auditTrailEntryEntity.AuditTrailEntryEntityId;
        }

        private AnnotationType GetAnnotationType(CasyContext2 casyContext, int annotationTypeId)
        {
            var annotationTypeEntity = casyContext.AnnotationTypes.FirstOrDefault(at => at.AnnotationTypeEntityId == annotationTypeId);
            if (annotationTypeEntity != null)
            {
                return new AnnotationType
                {
                    AnnotationTypeId = annotationTypeEntity.AnnotationTypeEntityId,
                    AnnottationTypeName = annotationTypeEntity.AnnottationTypeName
                };
            }
            return null;
        }

        private static MeasureResultData GetMeasureResultData(CasyContext2 casyContext, int measureResultDataId, bool isDeleted = false)
        {
            if (!isDeleted)
            {
                var measureResultDataEntity =
                    casyContext.MeasureResultData.FirstOrDefault(mrd =>
                        mrd.MeasureResultDataEntityId == measureResultDataId);
                if (measureResultDataEntity != null)
                {
                    return new MeasureResultData()
                    {
                        AboveCalibrationLimitCount = measureResultDataEntity.AboveCalibrationLimitCount,
                        BelowCalibrationLimitCount = measureResultDataEntity.BelowCalibrationLimitCount,
                        BelowMeasureLimtCount = measureResultDataEntity.BelowMeasureLimtCount,
                        ConcentrationTooHigh = measureResultDataEntity.ConcentrationTooHigh,
                        InternalDataBlock = measureResultDataEntity.DataBlock,
                        MeasureResultDataId = measureResultDataEntity.MeasureResultDataEntityId,
                        Color = measureResultDataEntity.Color,
                        CreatedAt = DateTimeOffsetExtensions.ParseAny(measureResultDataEntity.CreatedAt),
                        CreatedBy = measureResultDataEntity.CreatedBy,
                        LastModifiedAt = DateTimeOffsetExtensions.ParseAny(measureResultDataEntity.LastModifiedAt),
                        LastModifiedBy = measureResultDataEntity.LastModifiedBy
                    };
                }

                return null;
            }
            else
            {
                var measureResultDataEntity =
                    casyContext.MeasureResultDataDeleted.FirstOrDefault(mrd =>
                        mrd.MeasureResultDataEntityId == measureResultDataId);
                if (measureResultDataEntity != null)
                {
                    return new MeasureResultData()
                    {
                        AboveCalibrationLimitCount = measureResultDataEntity.AboveCalibrationLimitCount,
                        BelowCalibrationLimitCount = measureResultDataEntity.BelowCalibrationLimitCount,
                        BelowMeasureLimtCount = measureResultDataEntity.BelowMeasureLimtCount,
                        ConcentrationTooHigh = measureResultDataEntity.ConcentrationTooHigh,
                        InternalDataBlock = measureResultDataEntity.DataBlock,
                        MeasureResultDataId = measureResultDataEntity.MeasureResultDataEntityId,
                        Color = measureResultDataEntity.Color,
                        CreatedAt = DateTimeOffsetExtensions.ParseAny(measureResultDataEntity.CreatedAt),
                        CreatedBy = measureResultDataEntity.CreatedBy,
                        LastModifiedAt = DateTimeOffsetExtensions.ParseAny(measureResultDataEntity.LastModifiedAt),
                        LastModifiedBy = measureResultDataEntity.LastModifiedBy
                    };
                }

                return null;
            }
        }

        private static void SaveMeasureResultData(CasyContext2 casyContext, MeasureResultData measureResultData)
        {
            var measureResultDataEntity = casyContext.MeasureResultData.FirstOrDefault(mr => mr.MeasureResultDataEntityId == measureResultData.MeasureResultDataId);

            if (measureResultDataEntity == null)
            {
                measureResultDataEntity = new MeasureResultDataEntity
                {
                    AboveCalibrationLimitCount = measureResultData.AboveCalibrationLimitCount,
                    BelowCalibrationLimitCount = measureResultData.BelowCalibrationLimitCount,
                    BelowMeasureLimtCount = measureResultData.BelowMeasureLimtCount,
                    ConcentrationTooHigh = measureResultData.ConcentrationTooHigh,
                    DataBlock = measureResultData.InternalDataBlock,
                    MeasureResultEntityId = measureResultData.MeasureResult.MeasureResultId
                };
                casyContext.MeasureResultData.Add(measureResultDataEntity);
                casyContext.SaveChanges();
                measureResultData.MeasureResultDataId = measureResultDataEntity.MeasureResultDataEntityId;
                measureResultData.CreatedAt = DateTimeOffsetExtensions.ParseAny(measureResultDataEntity.CreatedAt);
                measureResultData.CreatedBy = measureResultDataEntity.CreatedBy;
                measureResultData.LastModifiedBy = measureResultDataEntity.LastModifiedBy;
                measureResultData.LastModifiedAt = DateTimeOffsetExtensions.ParseAny(measureResultDataEntity.LastModifiedAt);
            }
            else
            {
                measureResultDataEntity.AboveCalibrationLimitCount = measureResultData.AboveCalibrationLimitCount;
                measureResultDataEntity.BelowCalibrationLimitCount = measureResultData.BelowCalibrationLimitCount;
                measureResultDataEntity.BelowMeasureLimtCount = measureResultData.BelowMeasureLimtCount;
                measureResultDataEntity.ConcentrationTooHigh = measureResultData.ConcentrationTooHigh;
                measureResultDataEntity.DataBlock = measureResultData.InternalDataBlock;
                measureResultDataEntity.MeasureResultEntityId = measureResultData.MeasureResult.MeasureResultId;

                casyContext.SaveChanges();

                measureResultData.LastModifiedAt = DateTimeOffsetExtensions.ParseAny(measureResultDataEntity.LastModifiedAt);
                measureResultData.LastModifiedBy = measureResultDataEntity.LastModifiedBy;
            }
        }

        private static AuditTrailEntry GetAuditTrailEntry(CasyContext2 casyContext, int auditTrailEntryId)
        {
            var auditTrailEntryEntity = casyContext.AuditTrailEntries.FirstOrDefault(ate => ate.AuditTrailEntryEntityId == auditTrailEntryId);
            if (auditTrailEntryEntity != null)
            {
                return new AuditTrailEntry
                {
                    Action = auditTrailEntryEntity.Action,
                    AuditTrailEntryId = auditTrailEntryEntity.AuditTrailEntryEntityId,
                    ComputerName = auditTrailEntryEntity.ComputerName,
                    DateChanged = DateTimeOffsetExtensions.ParseAny(auditTrailEntryEntity.DateChanged),
                    EntityName = auditTrailEntryEntity.EntityName,
                    NewValue = auditTrailEntryEntity.NewValue,
                    OldValue = auditTrailEntryEntity.OldValue,
                    PrimaryKeyValue = auditTrailEntryEntity.PrimaryKeyValue,
                    PropertyName = auditTrailEntryEntity.PropertyName,
                    SoftwareVersion = auditTrailEntryEntity.SoftwareVersion,
                    UserChanged = auditTrailEntryEntity.UserChanged
                };
            }
            else
            {
                var auditTrailEntryEntityDeleted = casyContext.AuditTrailEntriesDeleted.FirstOrDefault(ate => ate.AuditTrailEntryEntityId == auditTrailEntryId);
                if(auditTrailEntryEntityDeleted != null)
                {
                    return new AuditTrailEntry
                    {
                        Action = auditTrailEntryEntityDeleted.Action,
                        AuditTrailEntryId = auditTrailEntryEntityDeleted.AuditTrailEntryEntityId,
                        ComputerName = auditTrailEntryEntityDeleted.ComputerName,
                        DateChanged = DateTimeOffsetExtensions.ParseAny(auditTrailEntryEntityDeleted.DateChanged),
                        EntityName = auditTrailEntryEntityDeleted.EntityName,
                        NewValue = auditTrailEntryEntityDeleted.NewValue,
                        OldValue = auditTrailEntryEntityDeleted.OldValue,
                        PrimaryKeyValue = auditTrailEntryEntityDeleted.PrimaryKeyValue,
                        PropertyName = auditTrailEntryEntityDeleted.PropertyName,
                        SoftwareVersion = auditTrailEntryEntityDeleted.SoftwareVersion,
                        UserChanged = auditTrailEntryEntityDeleted.UserChanged
                    };
                }
            }
            return null;
        }

        public Cursor GetCursor(int cursorId)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                return GetCursor(casyContext, cursorId);
            }
        }

        private static Cursor GetCursor(CasyContext2 casyContext, int cursorId)
        {
            var cursorEntity = casyContext.Cursors.FirstOrDefault(c => c.CursorEntityId == cursorId);
            if (cursorEntity != null)
            {
                return new Cursor
                {
                    CursorId = cursorEntity.CursorEntityId,
                    MinLimit = cursorEntity.MinLimit,
                    MaxLimit = cursorEntity.MaxLimit,
                    Color = cursorEntity.Color,
                    Name = cursorEntity.Name,
                    Version = cursorEntity.Version,
                    IsDeadCellsCursor = cursorEntity.IsDeadCellsCursor,
                    CreatedAt = DateTimeOffsetExtensions.ParseAny(cursorEntity.CreatedAt),
                    CreatedBy = cursorEntity.CreatedBy,
                    LastModifiedAt = DateTimeOffsetExtensions.ParseAny(cursorEntity.LastModifiedAt),
                    LastModifiedBy = cursorEntity.LastModifiedBy
                };
            }
            return null;
        }

        public void DeleteCursor(Cursor cursor)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                var cursorEntity = casyContext.Cursors.Include("MeasureSetupEntity").FirstOrDefault(c => c.CursorEntityId == cursor.CursorId);
                if (cursorEntity == null) return;
                casyContext.Cursors.Remove(cursorEntity);
                casyContext.SaveChanges(false, trackDeleted: true);
            }
        }

        public IEnumerable<ErrorDetails> GetErrorDetails()
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                var result = new List<ErrorDetails>();

                var errorDetailsEntities = casyContext.ErrorDetails.ToList();

                foreach (var errorDetailsEntity in errorDetailsEntities)
                {
                    var errorDetails = new ErrorDetails
                    {
                        Description = $"Error_{errorDetailsEntity.ErrorCode.Replace("-", "_")}_Description",
                        ErrorCode = errorDetailsEntity.ErrorCode,
                        ErrorDetailsId = errorDetailsEntity.ErrorDetailsEntityId,
                        ErrorNumber = errorDetailsEntity.ErrorNumber,
                        Information = $"Error_{errorDetailsEntity.ErrorCode.Replace("-", "_")}_Information",
                        Notice = $"Error_{errorDetailsEntity.ErrorCode.Replace("-", "_")}_Notice",
                        DeviceErrorName = $"Error_{errorDetailsEntity.ErrorCode.Replace("-", "_")}_ErrorNumber"
                    };

                    result.Add(errorDetails);
                }

                return result;
            }
        }

        public void SaveErrorDetails(ErrorDetails errorDetails)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                var errorDetailsEntity = casyContext.ErrorDetails.Local.FirstOrDefault(ed => ed.ErrorDetailsEntityId == errorDetails.ErrorDetailsId);

                if (errorDetailsEntity == null)
                {
                    errorDetailsEntity = new ErrorDetailsEntity
                    {
                        ErrorCode = errorDetails.ErrorCode,
                        ErrorNumber = errorDetails.ErrorNumber
                    };

                    casyContext.ErrorDetails.Add(errorDetailsEntity);
                    casyContext.SaveChanges();

                    errorDetails.ErrorDetailsId = errorDetailsEntity.ErrorDetailsEntityId;
                }
                else
                {
                    errorDetailsEntity.ErrorCode = errorDetails.ErrorCode;
                    errorDetailsEntity.ErrorNumber = errorDetails.ErrorNumber;

                    casyContext.SaveChanges();
                }
            }
        }

        public IEnumerable<AnnotationType> GetAnnotationTypes()
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                var result = new List<AnnotationType>();

                var annotationTypeEntities = casyContext.AnnotationTypes.ToList();

                foreach (var annotationTypeEntity in annotationTypeEntities)
                {
                    result.Add(EntityToModel(annotationTypeEntity));
                }

                return result;
            }
        }

        public void SaveAnnotationType(AnnotationType annotationType)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                var annotationTypeEntity = casyContext.AnnotationTypes.FirstOrDefault(at => at.AnnotationTypeEntityId == annotationType.AnnotationTypeId);

                if (annotationTypeEntity == null)
                {
                    annotationTypeEntity = new AnnotationTypeEntity()
                    {
                        AnnottationTypeName = annotationType.AnnottationTypeName
                    };

                    casyContext.AnnotationTypes.Add(annotationTypeEntity);
                    casyContext.SaveChanges();

                    annotationType.AnnotationTypeId = annotationTypeEntity.AnnotationTypeEntityId;
                }
                else
                {
                    annotationTypeEntity.AnnottationTypeName = annotationType.AnnottationTypeName;

                    casyContext.SaveChanges();
                }
            }
        }

        public void DeleteAnnotationType(AnnotationType annotationType)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                if (annotationType == null) return;
                var annotationTypeEntity = casyContext.AnnotationTypes.FirstOrDefault(at => at.AnnotationTypeEntityId == annotationType.AnnotationTypeId);

                if (annotationTypeEntity != null)
                {
                    var measureResultAnnotations = casyContext.MeasureResultAnnotations.Where(mra => mra.AnnotationTypeEntityId == annotationTypeEntity.AnnotationTypeEntityId).ToList();
                    foreach (var entity in measureResultAnnotations)
                    {
                        casyContext.MeasureResultAnnotations.Remove(entity);
                    }                    
                    casyContext.AnnotationTypes.Remove(annotationTypeEntity);
                }
                casyContext.SaveChanges();
            }
        }

        public void SaveMeasureResultAnnotation(MeasureResultAnnotation measureResultAnnotation, bool ignoreAuditTrail)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                SaveMeasureResultAnnotation(casyContext, measureResultAnnotation, ignoreAuditTrail);
            }
        }

        private static void SaveMeasureResultAnnotation(CasyContext2 casyContext, MeasureResultAnnotation measureResultAnnotation, bool ignoreAuditTrail)
        {
            var measureResultAnnotationEntity = casyContext.MeasureResultAnnotations.FirstOrDefault(mra => mra.MeasureResultAnnotationEntityId == measureResultAnnotation.MeasureResultAnnotationId);

            if (measureResultAnnotationEntity == null)
            {
                var measureResultEntity = casyContext.MeasureResults.FirstOrDefault(x =>
                    x.MeasureResultEntityId == measureResultAnnotation.MeasureResult.MeasureResultId);

                measureResultAnnotationEntity = new MeasureResultAnnotationEntity
                {
                    AnnotationTypeEntityId = measureResultAnnotation.AnnotationType.AnnotationTypeId,
                    MeasureResultEntityId = measureResultEntity.MeasureResultEntityId,
                    Value = measureResultAnnotation.Value
                };

                casyContext.MeasureResultAnnotations.Add(measureResultAnnotationEntity);
                casyContext.SaveChanges(ignoreAuditTrail);

                measureResultAnnotation.MeasureResultAnnotationId = measureResultAnnotationEntity.MeasureResultAnnotationEntityId;
            }
            else
            {
                measureResultAnnotationEntity.AnnotationTypeEntityId = measureResultAnnotation.AnnotationType.AnnotationTypeId;
                measureResultAnnotationEntity.Value = measureResultAnnotation.Value;

                casyContext.SaveChanges(ignoreAuditTrail);
            }
        }

        public Dictionary<string, Setting> GetSettings()
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                var result = new Dictionary<string, Setting>();

                var settingsEntities = casyContext.Settings.ToList();

                foreach (var settingsEntity in settingsEntities)
                {
                    result.Add(settingsEntity.Key, new Setting { Id = settingsEntity.Id, Value = settingsEntity.Value, Key = settingsEntity.Key, BlobValue = settingsEntity.BlobValue });
                }

                return result;
            }
        }

        public void SaveSetting(string key, string value)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                var settingEntity = casyContext.Settings.FirstOrDefault(setting => setting.Key == key);

                if (settingEntity == null)
                {
                    settingEntity = new SettingsEntity()
                    {
                        Key = key,
                        Value = value
                    };

                    casyContext.Settings.Add(settingEntity);
                    casyContext.SaveChanges();
                }
                else
                {
                    settingEntity.Value = value;

                    casyContext.SaveChanges();
                }
            }
        }

        public void SaveSetting(string key, byte[] value)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                var settingEntity = casyContext.Settings.FirstOrDefault(setting => setting.Key == key);

                if (settingEntity == null)
                {
                    settingEntity = new SettingsEntity
                    {
                        Key = key,
                        BlobValue = value
                    };

                    casyContext.Settings.Add(settingEntity);
                    casyContext.SaveChanges();
                }
                else
                {
                    settingEntity.BlobValue = value;

                    casyContext.SaveChanges();
                }
            }
        }

        public void CreateBackupFile(string fileName)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                var destination = new SQLiteConnection("Data Source=" + fileName + ";Version=3;Password=th1s1sc4sy;");

                destination.Open();

                //var connection = casyContext.Database.GetDbConnection() as SqliteConnection;
                //if (connection != null)
                //{
                    //connection.BackupDatabase(destination);
                //}

                if (casyContext.Database.Connection is SQLiteConnection source)
                {
                    source.Open();
                    source.BackupDatabase(destination, "main", "main", -1, null, 0);

                    source.Close();
                }

                destination.Close();
            }
        }

        public bool RestoreBackupFile(string fileName)
        {
            try
            {
                var testConnection = new SQLiteConnection("Data Source=" + fileName + ";Version=3;Password=th1s1sc4sy;");
                testConnection.Open();
                var command = new SQLiteCommand(@"SELECT * FROM SettingsEntity", testConnection);
                command.ExecuteNonQuery();

                testConnection.Close();

                _userCache.Clear();
                _userRoleCache.Clear();
                _measureResultCache.Clear();
                _measureSetupCache.Clear();

                File.Copy(fileName, @".\casy.db", true);

                RaiseOnDatabaseReady();
                return true;
            }
            catch
            {
                // ignored
            }

            return false;
        }

#if NET47
        [Export("StoreCounts")]
#endif
        public void StoreCounts(int counts)
        {
            SaveSetting("Count", counts.ToString());
        }

#if NET47
        [Export("GetCounts")]
#endif
        public int GetCounts()
        {
            var countSetting = GetSettings().Where(setting => setting.Key == "Count").ToArray();

            return countSetting.Length != 1 ? 0 : int.Parse(countSetting[0].Value.Value);
        }

#if NET47
        [Export("CheckIdentifier")]
#endif
        public bool CheckIdentifier(int identifier)
        {
            var identifiersSetting = GetSettings().Where(setting => setting.Key == "Identifiers").ToArray();

            if (identifiersSetting.Length < 1)
            {
                SaveSetting("Identifiers", identifier.ToString());
                return true;
            }

            var value = identifiersSetting[0].Value;
            var split = value.Value.Split(';');

            var contains = split.Contains(identifier.ToString());

            if (contains) return false;
            SaveSetting("Identifiers", value + ";" + identifier);
            return true;
        }

        public void SaveAuditTrailEntry(AuditTrailEntry auditTrailEntry)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                SaveAuditTrailEntry(casyContext, auditTrailEntry);
            }
        }

        public void Cleanup(int olderThanDays)
        {
            using (var casyContext = new CasyContext2(_environmentService, _activeAuditTrailDecorator, null))
            {
                var timeToCheck = DateTimeOffset.UtcNow.AddDays(olderThanDays*(-1));

                var measurementsToDelete = casyContext.MeasureResultsDeleted
                    .Select(mr => new
                    {
                        mr.MeasureResultEntityId, mr.DeletedAt
                    }).ToList();

                foreach (var tuple in measurementsToDelete)
                {
                    if (DateTimeOffsetExtensions.ParseAny(tuple.DeletedAt) > timeToCheck)
                    {
                        continue;
                    }

                    var measureResultEntity = casyContext.MeasureResultsDeleted
                        .FirstOrDefault(x => x.MeasureResultEntityId == tuple.MeasureResultEntityId);

                    var toRemove = _measureResultCache.FirstOrDefault(x =>
                        x.MeasureResultId == measureResultEntity.MeasureResultEntityId);
                    if (toRemove != null)
                    {
                        _measureResultCache.Remove(toRemove);
                    }
                    casyContext.MeasureResultsDeleted.Remove(measureResultEntity);
                }

                var setupsToDelete = casyContext.MeasureSetupsDeleted
                    .Select(mr => new
                    {
                        mr.MeasureSetupEntityId,
                        mr.DeletedAt
                    }).ToList();

                foreach (var tuple in setupsToDelete)
                {
                    if (DateTimeOffsetExtensions.ParseAny(tuple.DeletedAt) > timeToCheck)
                    {
                        continue;
                    }

                    var measureSetupEntity = casyContext.MeasureSetupsDeleted
                        .FirstOrDefault(x => x.MeasureSetupEntityId == tuple.MeasureSetupEntityId);

                    var toRemove = _measureSetupCache.FirstOrDefault(x =>
                        x.MeasureSetupId == measureSetupEntity.MeasureSetupEntityId);
                    if (toRemove != null)
                    {
                        _measureSetupCache.Remove(toRemove);
                    }
                    casyContext.MeasureSetupsDeleted.Remove(measureSetupEntity);
                }

                var cursorToDelete = casyContext.CursorsDeleted
                    .Select(mr => new
                    {
                        mr.CursorEntityId,
                        mr.DeletedAt
                    }).ToList();

                foreach (var tuple in cursorToDelete)
                {
                    if (DateTimeOffsetExtensions.ParseAny(tuple.DeletedAt) > timeToCheck)
                    {
                        continue;
                    }

                    var cursorEntity = casyContext.CursorsDeleted
                        .FirstOrDefault(x => x.CursorEntityId == tuple.CursorEntityId);

                    casyContext.CursorsDeleted.Remove(cursorEntity);
                }

                var deviationControlItemsToDelete = casyContext.DeviationControlItemsDeleted
                    .Select(mr => new
                    {
                        mr.DeviationControlItemEntityId,
                        mr.DeletedAt
                    }).ToList();

                foreach (var tuple in deviationControlItemsToDelete)
                {
                    if (DateTimeOffsetExtensions.ParseAny(tuple.DeletedAt) > timeToCheck)
                    {
                        continue;
                    }

                    var deviationControlItemEntity = casyContext.DeviationControlItemsDeleted
                        .FirstOrDefault(x => x.DeviationControlItemEntityId == tuple.DeviationControlItemEntityId);

                    casyContext.DeviationControlItemsDeleted.Remove(deviationControlItemEntity);
                }

                casyContext.SaveChanges(false, deletePermanent: true);

                
                var datasToDelete = casyContext.MeasureResultDataDeleted
                    .Select(mr => new
                    {
                        mr.MeasureResultDataEntityId,
                        mr.MeasureResultEntityId
                    }).ToList();

                foreach (var tuple in datasToDelete)
                {
                    if (casyContext.MeasureResultsDeleted.FirstOrDefault(mr =>
                            mr.MeasureResultEntityId == tuple.MeasureResultEntityId) == null)
                    {
                        var dataEntity = casyContext.MeasureResultDataDeleted
                            .FirstOrDefault(x => x.MeasureResultDataEntityId == tuple.MeasureResultDataEntityId);
                        casyContext.MeasureResultDataDeleted.Remove(dataEntity);
                    }
                }

                var auditTrailsToDelete = casyContext.AuditTrailEntriesDeleted
                    .Select(mr => new
                    {
                        mr.AuditTrailEntryEntityId,
                        mr.MeasureResultEntityId,
                        mr.MeasureSetupEntityId
                    }).ToList();

                foreach (var tuple in auditTrailsToDelete)
                {
                    if (tuple.MeasureResultEntityId.HasValue && casyContext.MeasureResultsDeleted.FirstOrDefault(mr =>
                            mr.MeasureResultEntityId == tuple.MeasureResultEntityId.Value) == null)
                    {
                        var auditTrailEntity = casyContext.AuditTrailEntriesDeleted
                            .FirstOrDefault(x => x.AuditTrailEntryEntityId == tuple.AuditTrailEntryEntityId);
                        casyContext.AuditTrailEntriesDeleted.Remove(auditTrailEntity);
                    }
                    else if (tuple.MeasureSetupEntityId.HasValue && casyContext.MeasureSetupsDeleted.FirstOrDefault(mr =>
                                 mr.MeasureSetupEntityId == tuple.MeasureSetupEntityId.Value) == null)
                    {
                        var auditTrailEntity = casyContext.AuditTrailEntriesDeleted
                            .FirstOrDefault(x => x.AuditTrailEntryEntityId == tuple.AuditTrailEntryEntityId);
                        casyContext.AuditTrailEntriesDeleted.Remove(auditTrailEntity);
                    }
                }

                casyContext.SaveChanges(false, deletePermanent: true);
            }
        }

#region IDisposable Support


        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue) return;

            if (disposing)
            {
            }

            _userCache = null;
            _userRoleCache = null;

            _disposedValue = true;
        }

        ~SQLiteDatabaseChannel() {
          // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
          Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }

#endregion
    }
}
