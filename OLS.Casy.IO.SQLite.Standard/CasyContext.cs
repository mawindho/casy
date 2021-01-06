using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using OLS.Casy.Core.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.IO.SQLite.Entities;

namespace OLS.Casy.IO.SQLite.Standard
{
    public class CasyContext : DbContext
    {
        private readonly IEnvironmentService _environmentService;
        private readonly IAuditTrailDecorator _auditTrailDecorator;
        private readonly IConfiguration _configuration;

        public static readonly Microsoft.Extensions.Logging.LoggerFactory _myLoggerFactory =
            new LoggerFactory(new[] {
                new DebugLoggerProvider()
            });

        public CasyContext(IEnvironmentService environmentService,
            IAuditTrailDecorator auditTrailDecorator, IConfiguration configuration)
        {
            _environmentService = environmentService;
            _auditTrailDecorator = auditTrailDecorator;
            _configuration = configuration;
            if (_auditTrailDecorator != null)
            {
                _auditTrailDecorator.Context = this;
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.EnableSensitiveDataLogging();

            var connection = InitializeSQLiteConnection();
            optionsBuilder.UseSqlite(connection, x => x.SuppressForeignKeyEnforcement()).UseLoggerFactory(_myLoggerFactory);
        }

        public DbSet<AnnotationTypeEntity> AnnotationTypes { get; set; }
        public DbSet<AuditTrailEntryEntity> AuditTrailEntries { get; set; }
        public DbSet<AuditTrailEntryEntity_Deleted> AuditTrailEntriesDeleted { get; set; }
        public DbSet<CursorEntity> Cursors { get; set; }
        public DbSet<CursorEntity_Deleted> CursorsDeleted { get; set; }
        public DbSet<MeasureResultEntity> MeasureResults { get; set; }
        public DbSet<MeasureResultEntity_Deleted> MeasureResultsDeleted { get; set; }
        public DbSet<MeasureResultAnnotationEntity> MeasureResultAnnotations { get; set; }
        public DbSet<MeasureResultDataEntity> MeasureResultData { get; set; }
        public DbSet<MeasureResultDataEntity_Deleted> MeasureResultDataDeleted { get; set; }
        public DbSet<MeasureSetupEntity> MeasureSetups { get; set; }
        public DbSet<MeasureSetupEntity_Deleted> MeasureSetupsDeleted { get; set; }
        public DbSet<UserRoleEntity> UserRoles { get; set; }
        public DbSet<UserEntity> Users { get; set; }
        public DbSet<UserGroupEntity> UserGroups { get; set; }
        public DbSet<UserUserGroupMapping> UserUserGroupMappings { get; set; }
        public DbSet<MeasureResultAccessMapping> MeasureResultAccessMappings { get; set; }
        public DbSet<ErrorDetailsEntity> ErrorDetails { get; set; }
        public DbSet<DeviationControlItemEntity> DeviationControlItems { get; set; }
        public DbSet<DeviationControlItemEntity_Deleted> DeviationControlItemsDeleted { get; set; }
        public DbSet<SettingsEntity> Settings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserUserGroupMapping>(entity =>
            {
                entity.ToTable("UserUserGroupMapping");
                entity.HasKey(u => new
                {
                    u.UserEntityId,
                    u.UserGroupEntityId
                });
            });

            modelBuilder.Entity<MeasureSetupEntity>(entity =>
            {
                entity.ToTable("MeasureSetupEntity");
                entity.HasKey(x => x.MeasureSetupEntityId);
                entity.Ignore(x => x.AssociatedMeasureResultEntityId);
                entity.Ignore(x => x.AssociatedTemplateEntityId);
            });

            modelBuilder.Entity<MeasureSetupEntity_Deleted>(entity =>
            {
                entity.ToTable("MeasureSetupEntity_Deleted");
                entity.HasKey(x => x.MeasureSetupEntityId);
            });
                

            modelBuilder.Entity<AnnotationTypeEntity>(entity =>
            {
                entity.ToTable("AnnotationTypeEntity");
                entity.HasKey(x => x.AnnotationTypeEntityId);
            });

            modelBuilder.Entity<AuditTrailEntryEntity>(entity =>
            {
                entity.ToTable("AuditTrailEntryEntity");
                entity.HasKey(x => x.AuditTrailEntryEntityId);
            });

            modelBuilder.Entity<AuditTrailEntryEntity_Deleted>(entity =>
            {
                entity.ToTable("AuditTrailEntryEntity_Deleted");
                entity.HasKey(x => x.AuditTrailEntryEntityId);
            });

            modelBuilder.Entity<CursorEntity>(entity =>
            {
                entity.ToTable("CursorEntity");
                entity.HasKey(x => x.CursorEntityId);
                entity.Ignore(x => x.AssociatedMeasureResultEntityId);
                entity.Ignore(x => x.AssociatedTemplateEntityId);
            });

            modelBuilder.Entity<CursorEntity_Deleted>(entity =>
            {
                entity.ToTable("CursorEntity_Deleted");
                entity.HasKey(x => x.CursorEntityId);
            });

            modelBuilder.Entity<DeviationControlItemEntity>(entity =>
            {
                entity.ToTable("DeviationControlItemEntity");
                entity.Ignore(x => x.AssociatedMeasureResultEntityId);
                entity.Ignore(x => x.AssociatedTemplateEntityId);
                entity.HasKey(x => x.DeviationControlItemEntityId);
            });

            modelBuilder.Entity<DeviationControlItemEntity_Deleted>(entity =>
            {
                entity.ToTable("DeviationControlItemEntity_Deleted");
                entity.HasKey(x => x.DeviationControlItemEntityId);
            });

            modelBuilder.Entity<ErrorDetailsEntity>(entity =>
            {
                entity.ToTable("ErrorDetailsEntity");
                entity.HasKey(x => x.ErrorDetailsEntityId);
            });

            modelBuilder.Entity<MeasureResultAccessMapping>(entity =>
            {
                entity.ToTable("MeasureResultAccessMapping");
                entity.HasKey(x => x.MeasureResultAccessMappingId);
            });

            modelBuilder.Entity<MeasureResultAnnotationEntity>(entity =>
            {
                entity.ToTable("MeasureResultAnnotationEntity");
                entity.HasKey(x => x.MeasureResultAnnotationEntityId);
                //entity.HasOne(x => x.MeasureResultEntity)
                    //.WithMany()
                    //.HasForeignKey("MeasureResultEntityId");
            });

            modelBuilder.Entity<MeasureResultDataEntity>(entity =>
            {
                entity.ToTable("MeasureResultDataEntity");
                entity.HasKey(x => x.MeasureResultDataEntityId);
            });

            modelBuilder.Entity<MeasureResultDataEntity_Deleted>(entity =>
            {
                entity.ToTable("MeasureResultDataEntity_Deleted");
                entity.HasKey(x => x.MeasureResultDataEntityId);
            });

            modelBuilder.Entity<MeasureResultEntity>(entity =>
            {
                entity.ToTable("MeasureResultEntity");
                entity.HasKey(x => x.MeasureResultEntityId);
                entity.Ignore(x => x.AssociatedMeasureResultEntityId);
                entity.Ignore(x => x.AssociatedTemplateEntityId);
                entity.HasOne(x => x.MeasureSetupEntity)
                    .WithMany()
                    .HasForeignKey("MeasureSetupEntityId");
                entity.HasOne(x => x.OriginalMeasureSetupEntity)
                    .WithMany()
                    .HasForeignKey("OriginalMeasureSetupEntityId");
                entity.HasMany(x => x.MeasureResultAnnotationEntities)
                    //.WithOne(x => x.MeasureResultEntity)
                    .WithOne();
                    //.HasForeignKey("MeasureResultEntityId");
                entity.HasMany(x => x.MeasureResultAccessMappings)
                    //.WithOne(x => x.MeasureResultEntity)
                    .WithOne();
                    //.HasForeignKey("MeasureResultEntityId");
            });

            modelBuilder.Entity<MeasureResultEntity_Deleted>(entity =>
            {
                entity.ToTable("MeasureResultEntity_Deleted");
                entity.HasKey(x => x.MeasureResultEntityId);
                entity.HasOne(x => x.MeasureSetupEntity)
                    .WithMany()
                    .HasForeignKey("MeasureSetupEntityId");
                entity.HasOne(x => x.OriginalMeasureSetupEntity)
                    .WithMany()
                    .HasForeignKey("OriginalMeasureSetupEntityId");
                //entity.HasMany(x => x.MeasureResultAnnotationEntities)
                  //  .WithOne(x => x.MeasureResultEntityDeleted)
                    //.HasForeignKey("MeasureResultEntityId");
                //entity.HasMany(x => x.MeasureResultAccessMappings)
                    //.WithOne(x => x.MeasureResultEntityDeleted)
                    //.WithOne();
                    //.HasForeignKey("MeasureResultEntityId");
                //entity.HasMany(x => x.MeasureResultAnnotationEntities)
                    //.WithOne();
            });

            modelBuilder.Entity<SettingsEntity>(entity =>
            {
                entity.ToTable("SettingsEntity");
                entity.HasKey(x => x.Id);
            });

            modelBuilder.Entity<UserEntity>(entity =>
            {
                entity.ToTable("UserEntity");
                entity.HasKey(x => x.UserEntityId);
            });

            modelBuilder.Entity<UserGroupEntity>(entity =>
            {
                entity.ToTable("UserGroupEntity");
                entity.HasKey(x => x.UserGroupEntityId);
            });

            modelBuilder.Entity<UserRoleEntity>(entity =>
            {
                entity.ToTable("UserRoleEntity");
                entity.HasKey(x => x.UserRoleEntityId);
            });

            base.OnModelCreating(modelBuilder);

            //foreach (IMutableEntityType entity in modelBuilder.Model.GetEntityTypes())
            //{
                //entity.Relational().TableName = entity.DisplayName();
            //}
        }

        private SqliteConnection InitializeSQLiteConnection()
        {
            var connectionString = _configuration == null
                ? @"Data Source=casy.standard.db"
                : $"Data Source={_configuration.GetValue<string>("DbConnection")}";

            var test = File.Exists(connectionString);

            var connection = new SqliteConnection(connectionString);
            //connection.ConnectionString =
                //new SqliteConnectionStringBuilder(connection.ConnectionString)
                        //{ Password = "th1s1sc4sy" }
                    //.ToString();

            //connection.Open();
            connection.Open();
            var password = "th1s1sc4sy";
            var command = connection.CreateCommand();
            command.CommandText = "SELECT quote($password);";
            command.Parameters.AddWithValue("$password", password);
            var quotedPassword = (string)command.ExecuteScalar();

            command.CommandText = "PRAGMA key = " + quotedPassword;
            command.Parameters.Clear();
            var result = command.ExecuteNonQuery();
            return connection;
        }

        public override int SaveChanges()
        {
            return SaveChanges(false);
        }

        public int SaveChanges(bool ignoreAuditTrail, bool deletePermanent = false, bool trackDeleted = false)
        {
            //if (!_auditTrailDecorator.IsStandard)
            //{
                //return base.SaveChanges();
            //}

            if (deletePermanent)
            {
                return base.SaveChanges();
            }

            List<EntityEntry> addedAuditedEntries = null;

            var userName = _environmentService.GetEnvironmentInfo("LoggedInUserName") as string;
            var machineName = _environmentService.GetEnvironmentInfo("MachineName") as string;
            var softwareVersion = _environmentService.GetEnvironmentInfo("SoftwareVersion") as string;

            if (!ignoreAuditTrail)
            {
                if (userName == null)
                {
                    userName = "Unknown";
                }

                addedAuditedEntries = ChangeTracker.Entries()
                  .Where(p => p.Entity is IAuditedEntity && p.State == EntityState.Added).ToList();

                var modifiedAuditedEntries = ChangeTracker.Entries()
                  .Where(p => p.Entity is IAuditedEntity && p.State == EntityState.Modified).ToList();

                var deletedAuditedEntries = ChangeTracker.Entries()
                 .Where(p => p.Entity is IAuditedEntity && p.State == EntityState.Deleted).ToList();

                var utcNow = DateTimeOffset.UtcNow;

                foreach (var added in addedAuditedEntries)
                {
                    IAuditedEntity auditedEntity = added.Entity as IAuditedEntity;
                    if (auditedEntity != null)
                    {
                        auditedEntity.CreatedAt = utcNow.ToString();
                        auditedEntity.LastModifiedAt = utcNow.ToString();
                        auditedEntity.CreatedBy = userName;
                        auditedEntity.LastModifiedBy = userName;
                    }
                }

                foreach (var modified in modifiedAuditedEntries)
                {
                    if (_auditTrailDecorator != null && _auditTrailDecorator.IsStandard)
                    {
                        var auditTrailEntries = _auditTrailDecorator.CreateModifiedAuditTrailEntity(modified, userName, machineName, softwareVersion);
                        foreach (var auditTrailEntry in auditTrailEntries)
                        {
                            AuditTrailEntries.Add(auditTrailEntry);
                        }
                    }

                    IAuditedEntity auditedEntity = modified.Entity as IAuditedEntity;
                    if (auditedEntity != null)
                    {
                        auditedEntity.LastModifiedAt = utcNow.ToString();
                        auditedEntity.LastModifiedBy = userName;
                        auditedEntity.Version = auditedEntity.Version + 1;
                    }
                }

                if (trackDeleted)
                {
                    foreach (var deletedEntry in deletedAuditedEntries)
                    {
                        //if (deletedEntry.Entity is IAuditedEntity auditedEntity)
                        //{
                        //auditedEntity.DeletedAt = utcNow;
                        //auditedEntity.DeletedBy = userName;
                        //}
                        if (_auditTrailDecorator != null && _auditTrailDecorator.IsStandard)
                        {
                            var auditTrailEntry =
                                _auditTrailDecorator?.CreateDeletedAuditTrailEntity(deletedEntry, userName, machineName,
                                    softwareVersion);
                            if (auditTrailEntry != null)
                            {
                                AuditTrailEntries.Add(auditTrailEntry);
                            }
                        }
                    }
                }

                //foreach (var deletedEntry in deletedFlagEntries)
                //{
                //((IDeletedFlagEntity)deletedEntry.Entity).IsDelete = true;
                //deletedEntry.State = EntityState.Modified;
                //}
            }

            var result = base.SaveChanges();

            if (ignoreAuditTrail) return result;

            foreach (var added in addedAuditedEntries)
            {
                if (_auditTrailDecorator != null && _auditTrailDecorator.IsStandard)
                {
                    var auditTrailEntry =
                        _auditTrailDecorator?.CreateAddedAuditTrailEntity(added, userName, machineName,
                            softwareVersion);
                    if (auditTrailEntry != null)
                    {
                        AuditTrailEntries.Add(auditTrailEntry);
                    }
                }
            }

            base.SaveChanges();

            return result;
        }
    }
}
