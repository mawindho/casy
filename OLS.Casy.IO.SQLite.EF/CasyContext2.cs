using Microsoft.Extensions.Configuration;
using OLS.Casy.Core.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.IO.SQLite.Entities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Data.SQLite;
using System.Linq;

namespace OLS.Casy.IO.SQLite.EF
{
    public class CasyContext2 : DbContext
    {
        private readonly IEnvironmentService _environmentService;
        private readonly IAuditTrailDecorator _auditTrailDecorator;
        private readonly IConfiguration _configuration;

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
        public DbSet<MeasureResultAccessMapping> MeasureResultAccessMappings { get;set; }
        public DbSet<ErrorDetailsEntity> ErrorDetails { get; set; }
        public DbSet<DeviationControlItemEntity> DeviationControlItems { get; set; }
        public DbSet<DeviationControlItemEntity_Deleted> DeviationControlItemsDeleted { get; set; }
        public DbSet<SettingsEntity> Settings { get; set; }

        public CasyContext2(IEnvironmentService environmentService,
            IAuditTrailDecorator auditTrailDecorator,
            IConfiguration configuration)
            : base(new SQLiteConnection
            {
                ConnectionString = string.Format(@"Data Source={0};Version=3;Password=th1s1sc4sy;", configuration == null ? @".\casy.db" : configuration.GetValue<string>("DbConnection"))
            }, true)
        {
            _environmentService = environmentService;
            if (auditTrailDecorator == null) return;
            _auditTrailDecorator = auditTrailDecorator;
            _configuration = configuration;
            _auditTrailDecorator.Context = this;
            //_auditTrailDecorator.Context = this;
        }

        //protected override void OnConfiguring(DbContextOptionsBuilder options)
        //    => options.UseSqlite(string.Format(@"Data Source={0};Version=3;Password=th1s1sc4sy;", _configuration == null ? @".\casy.db" : _configuration.GetValue<string>("DbConnection")));

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserUserGroupMapping>().HasKey(u => new
            {
                u.UserEntityId,
                u.UserGroupEntityId
            });

            modelBuilder.Entity<MeasureSetupEntity>()
                .HasKey(x => x.MeasureSetupEntityId)
                .Ignore(x => x.AssociatedMeasureResultEntityId)
                .Ignore(x => x.AssociatedTemplateEntityId);

            modelBuilder.Entity<MeasureSetupEntity_Deleted>()
                .HasKey(x => x.MeasureSetupEntityId);

            modelBuilder.Entity<AnnotationTypeEntity>()
                .HasKey(x => x.AnnotationTypeEntityId);

            modelBuilder.Entity<AuditTrailEntryEntity>()
                .HasKey(x => x.AuditTrailEntryEntityId);

            modelBuilder.Entity<AuditTrailEntryEntity_Deleted>()
                .HasKey(x => x.AuditTrailEntryEntityId);

            modelBuilder.Entity<CursorEntity>()
                .HasKey(x => x.CursorEntityId)
                .Ignore(x => x.AssociatedMeasureResultEntityId)
                .Ignore(x => x.AssociatedTemplateEntityId);

            modelBuilder.Entity<CursorEntity_Deleted>()
                .HasKey(x => x.CursorEntityId);

            modelBuilder.Entity<DeviationControlItemEntity>()
                .HasKey(x => x.DeviationControlItemEntityId)
                .Ignore(x => x.AssociatedMeasureResultEntityId)
                .Ignore(x => x.AssociatedTemplateEntityId);

            modelBuilder.Entity<DeviationControlItemEntity_Deleted>()
                .HasKey(x => x.DeviationControlItemEntityId);

            modelBuilder.Entity<ErrorDetailsEntity>()
                .HasKey(x => x.ErrorDetailsEntityId);

            modelBuilder.Entity<MeasureResultAccessMapping>()
                .HasKey(x => x.MeasureResultAccessMappingId);

            modelBuilder.Entity<MeasureResultAnnotationEntity>()
                .HasKey(x => x.MeasureResultAnnotationEntityId);

            modelBuilder.Entity<MeasureResultDataEntity>()
                .HasKey(x => x.MeasureResultDataEntityId);

            modelBuilder.Entity<MeasureResultDataEntity_Deleted>()
                .HasKey(x => x.MeasureResultDataEntityId);

            modelBuilder.Entity<MeasureResultEntity>()
                .HasKey(x => x.MeasureResultEntityId)
                .Ignore(x => x.AssociatedMeasureResultEntityId)
                .Ignore(x => x.AssociatedTemplateEntityId);

            modelBuilder.Entity<MeasureResultEntity>()
                .HasRequired(x => x.MeasureSetupEntity)
                .WithMany()
                .Map(m => m.MapKey("MeasureSetupEntityId"));

            modelBuilder.Entity<MeasureResultEntity>()
                .HasRequired(x => x.OriginalMeasureSetupEntity)
                .WithMany()
                .Map(m => m.MapKey("OriginalMeasureSetupEntityId"));

            modelBuilder.Entity<MeasureResultEntity>()
                .HasMany(x => x.MeasureResultAnnotationEntities)
                .WithOptional();

            modelBuilder.Entity<MeasureResultEntity>()
                .HasMany(x => x.MeasureResultAccessMappings)
                .WithOptional();

            modelBuilder.Entity<MeasureResultEntity_Deleted>()
                .HasKey(x => x.MeasureResultEntityId);

            modelBuilder.Entity<MeasureResultEntity_Deleted>()
                .HasRequired(x => x.MeasureSetupEntity)
                .WithMany()
                .Map(m => m.MapKey("MeasureSetupEntityId"));

            modelBuilder.Entity<MeasureResultEntity_Deleted>()
                .HasRequired(x => x.OriginalMeasureSetupEntity)
                .WithMany()
                .Map(m => m.MapKey("OriginalMeasureSetupEntityId"));

            //modelBuilder.Entity<MeasureResultEntity_Deleted>()
                //.HasMany(x => x.MeasureResultAnnotationEntities)
                //.WithOptional();

            //modelBuilder.Entity<MeasureResultEntity_Deleted>()
                //.HasMany(x => x.MeasureResultAccessMappings)
                //.WithOptional();

            modelBuilder.Entity<SettingsEntity>()
                .HasKey(x => x.Id);

            modelBuilder.Entity<UserEntity>()
                .HasKey(x => x.UserEntityId);

            modelBuilder.Entity<UserGroupEntity>()
                .HasKey(x => x.UserGroupEntityId);

            modelBuilder.Entity<UserRoleEntity>()
                .HasKey(x => x.UserRoleEntityId);

            base.OnModelCreating(modelBuilder);

            //foreach (IMutableEntityType entity in modelBuilder.Model.GetEntityTypes())
            //{
            //entity.Relational().TableName = entity.DisplayName();
            //}
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }

        public override int SaveChanges()
        {
            return SaveChanges(false);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability",
            "CA1502:AvoidExcessiveComplexity")]
        public int SaveChanges(bool ignoreAuditTrail, bool deletePermanent = false, bool trackDeleted = false)
        {
            if (deletePermanent)
            {
                return base.SaveChanges();
            }

            List<DbEntityEntry> addedAuditedEntries = null;

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

                //var deletedFlagEntries = ChangeTracker.Entries()
                  //.Where(p => p.Entity is IDeletedFlagEntity && p.State == EntityState.Deleted).ToList();

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
                    if (_auditTrailDecorator != null)
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

                if(trackDeleted)
                { 
                foreach (var deletedEntry in deletedAuditedEntries)
                {
                    //if (deletedEntry.Entity is IAuditedEntity auditedEntity)
                    //{
                        //auditedEntity.DeletedAt = utcNow;
                        //auditedEntity.DeletedBy = userName;
                    //}

                    var auditTrailEntry = _auditTrailDecorator?.CreateDeletedAuditTrailEntity(deletedEntry, userName, machineName, softwareVersion);
                    if (auditTrailEntry != null)
                    {
                        AuditTrailEntries.Add(auditTrailEntry);
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
                var auditTrailEntry = _auditTrailDecorator?.CreateAddedAuditTrailEntity(added, userName, machineName, softwareVersion);
                if (auditTrailEntry != null)
                {
                    AuditTrailEntries.Add(auditTrailEntry);
                }
            }

            base.SaveChanges();

            return result;
        }
    }
}
