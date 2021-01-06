using OLS.Casy.Core;
using System;
using System.Linq;
using OLS.Casy.Base;

namespace OLS.Casy.IO.SQLite.EF
{
    internal static class Migrations
    {
        private const long RequiredDatabaseVersion = 15;

        internal static bool CheckForMigration(CasyContext2 casyContext)
        {
            //var connection = casyContext.Database.GetDbConnection();
            var connection = casyContext.Database.Connection;
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA user_version;";
                var currentDbVersion = (long)command.ExecuteScalar();

                return currentDbVersion < RequiredDatabaseVersion;
            }
        }

        internal static void DoMigration(CasyContext2 casyContext)
        {
            long currentDbVersion;

            //var connection = casyContext.Database.GetDbConnection();
            var connection = casyContext.Database.Connection;
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA user_version;";
                currentDbVersion = (long)command.ExecuteScalar();
            }

            //casyContext.Database.ExecuteSqlCommand("PRAGMA user_version = 7");

            if (currentDbVersion >= RequiredDatabaseVersion) return;

            switch (currentDbVersion)
            {
                case 0:
                    MigrateTo1(casyContext);
                    break;
                case 1:
                    MigrateTo2(casyContext);
                    break;
                case 2:
                    MigrateTo3(casyContext);
                    break;
                case 3:
                    MigrateTo4(casyContext);
                    break;
                case 4:
                    MigrateTo5(casyContext);
                    break;
                case 5:
                    MigrateTo6(casyContext);
                    break;
                case 6:
                    MigrateTo7(casyContext);
                    break;
                case 7:
                    MigrateTo8(casyContext);
                    break;
                case 8:
                    MigrateTo9(casyContext);
                    break;
                case 9:
                case 10:
                    MigrateTo11(casyContext);
                    break;
                case 11:
                    MigrateTo12(casyContext);
                    break;
                case 12:
                    MigrateTo13(casyContext);
                    break;
                case 13:
                    MigrateTo14(casyContext);
                    break;
                case 14:
                    MigrateTo15(casyContext);
                    break;
            }
        }

        private static void MigrateTo15(CasyContext2 casyContext)
        {
            casyContext.Database.ExecuteSqlCommand(
                "ALTER TABLE MeasureResultEntity ADD COLUMN Version INTEGER DEFAULT 0");
            casyContext.Database.ExecuteSqlCommand(
                "ALTER TABLE MeasureResultEntity_Deleted ADD COLUMN Version INTEGER DEFAULT 0");
            casyContext.Database.ExecuteSqlCommand(
                "ALTER TABLE MeasureResultEntity ADD COLUMN LastWeeklyClean TEXT DEFAULT NULL");
            casyContext.Database.ExecuteSqlCommand(
                "ALTER TABLE MeasureResultEntity_Deleted ADD COLUMN LastWeeklyClean TEXT DEFAULT NULL");
            casyContext.Database.ExecuteSqlCommand(
                "ALTER TABLE MeasureSetupEntity ADD COLUMN Version INTEGER DEFAULT 0");
            casyContext.Database.ExecuteSqlCommand(
                "ALTER TABLE MeasureSetupEntity_Deleted ADD COLUMN Version INTEGER DEFAULT 0");
            casyContext.Database.ExecuteSqlCommand(
                "ALTER TABLE CursorEntity ADD COLUMN Version INTEGER DEFAULT 0");
            casyContext.Database.ExecuteSqlCommand(
                "ALTER TABLE CursorEntity_Deleted ADD COLUMN Version INTEGER DEFAULT 0");
            casyContext.Database.ExecuteSqlCommand(
                "ALTER TABLE DeviationControlItemEntity ADD COLUMN Version INTEGER DEFAULT 0");
            casyContext.Database.ExecuteSqlCommand(
                "ALTER TABLE DeviationControlItemEntity_Deleted ADD COLUMN Version INTEGER DEFAULT 1");
            casyContext.Database.ExecuteSqlCommand("PRAGMA user_version = 15");
            DoMigration(casyContext);
        }

        private static void MigrateTo14(CasyContext2 casyContext)
        {
            //casyContext.Database.ExecuteSqlCommand(@"DROP TABLE _CursorEntity_old");

            casyContext.Database.ExecuteSqlCommand("PRAGMA foreign_keys=off");

            casyContext.Database.ExecuteSqlCommand(
                @"CREATE TABLE MeasureSetupEntity_Deleted (
            MeasureSetupEntityId INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            AggregationCalculationMode INTEGER NOT NULL,
            CapillarySize INTEGER NOT NULL,
            CreatedAt TEXT NOT NULL,
            CreatedBy TEXT,
            DeletedAt TEXT NOT NULL,
            DeletedBy TEXT,
            DilutionFactor REAL NOT NULL,
            FromDiameter INTEGER NOT NULL,
            IsDeviationControlEnabled INTEGER NOT NULL,
            IsReadOnly INTEGER NOT NULL,
            IsSmoothing INTEGER NOT NULL,
            IsTemplate INTEGER NOT NULL,
            LastModifiedAt TEXT,
            LastModifiedBy TEXT,
            ManualAggrgationCalculationFactor REAL NOT NULL,
            MeasureMode INTEGER NOT NULL,
            Name TEXT,
            Repeats INTEGER NOT NULL,
            ScalingMaxRange INTEGER NOT NULL,
            ScalingMode INTEGER NOT NULL,
            SmoothingFactor REAL NOT NULL,
            ToDiameter INTEGER NOT NULL,
            UnitMode INTEGER NOT NULL,
            Volume INTEGER NOT NULL,
            VolumeCorrectionFactor REAL NOT NULL,
            DefaultExperiment TEXT,
            DefaultGroup TEXT,
            IsAutoSave INTEGER NOT NULL,
            AutoSaveName TEXT, 
            DilutionSampleVolume REAL, 
            DilutionCasyTonVolume REAL, 
            IsAutoComment INTEGER NOT NULL, 
            ChannelCount INTEGER NOT NULL, 
            HasSubpopulations INTEGER NOT NULL)");

            casyContext.Database.ExecuteSqlCommand(
                @"INSERT INTO MeasureSetupEntity_Deleted (MeasureSetupEntityId, AggregationCalculationMode,
                CapillarySize, CreatedAt, CreatedBy, DeletedAt, DeletedBy, DilutionFactor, FromDiameter,
                IsDeviationControlEnabled, IsReadOnly, IsSmoothing, IsTemplate, LastModifiedAt, LastModifiedBy,
                ManualAggrgationCalculationFactor, MeasureMode, Name, Repeats, ScalingMaxRange, ScalingMode,
                SmoothingFactor, ToDiameter, UnitMode, Volume, VolumeCorrectionFactor, DefaultExperiment,
                DefaultGroup, IsAutoSave, AutoSaveName, DilutionSampleVolume, DilutionCasyTonVolume, IsAutoComment,
                ChannelCount, HasSubpopulations) 
                SELECT MeasureSetupEntityId, AggregationCalculationMode,
                CapillarySize, CreatedAt, CreatedBy, DeletedAt, DeletedBy, DilutionFactor, FromDiameter,
                IsDeviationControlEnabled, IsReadOnly, IsSmoothing, IsTemplate, LastModifiedAt, LastModifiedBy,
                ManualAggrgationCalculationFactor, MeasureMode, Name, Repeats, ScalingMaxRange, ScalingMode,
                SmoothingFactor, ToDiameter, UnitMode, Volume, VolumeCorrectionFactor, DefaultExperiment,
                DefaultGroup, IsAutoSave, AutoSaveName, DilutionSampleVolume, DilutionCasyTonVolume, IsAutoComment,
                ChannelCount, HasSubpopulations FROM MeasureSetupEntity
                WHERE IsDelete = 1");

            casyContext.Database.ExecuteSqlCommand(
                @"CREATE TABLE CursorEntity_Deleted (
                CursorEntityId INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                CreatedAt TEXT NOT NULL,
                CreatedBy TEXT,
                DeletedAt TEXT NOT NULL,
                DeletedBy TEXT,
                LastModifiedAt TEXT,
                LastModifiedBy TEXT,
                MaxLimit REAL NOT NULL,
                MeasureSetupEntityId INTEGER NOT NULL,
                MinLimit REAL NOT NULL,
                Name TEXT NOT NULL,
                Color TEXT,
                IsDeadCellsCursor INTEGER DEFAULT 0, 
                Subpopulation TEXT DEFAULT '',
                FOREIGN KEY(`MeasureSetupEntityId`) REFERENCES `MeasureSetupEntity_Deleted`(`MeasureSetupEntityId`) ON DELETE CASCADE)");

            casyContext.Database.ExecuteSqlCommand(
                @"INSERT INTO CursorEntity_Deleted (CursorEntityId, CreatedAt,
                CreatedBy, DeletedAt, DeletedBy, LastModifiedBy, LastModifiedAt, MaxLimit, MeasureSetupEntityId,
                MinLimit, Name, Color, IsDeadCellsCursor, Subpopulation) 
                SELECT CursorEntityId, CreatedAt,
                CreatedBy, DeletedAt, DeletedBy, LastModifiedBy, LastModifiedAt, MaxLimit, MeasureSetupEntityId,
                MinLimit, Name, Color, IsDeadCellsCursor, Subpopulation FROM CursorEntity
                WHERE IsDelete = 1");

            casyContext.Database.ExecuteSqlCommand(
                @"CREATE TABLE MeasureResultEntity_Deleted (
                MeasureResultEntityId INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                Comment TEXT,
                CreatedAt TEXT NOT NULL,
                CreatedBy TEXT,
                DeletedAt TEXT NOT NULL,
                DeletedBy TEXT,
                Experiment TEXT,
                [Group] TEXT,
                IsTemporary INTEGER NOT NULL,
                LastModifiedAt TEXT,
                LastModifiedBy TEXT,
                MeasureResultEntityGuid BLOB NOT NULL,
                MeasureSetupEntityId INTEGER NOT NULL,
                Name TEXT,
                OriginalMeasureSetupEntityId INTEGER NOT NULL,
                SerialNumber TEXT,
                Color Text,
                MeasuredAt TEXT NOT NULL,
                Origin TEXT NOT NULL, 
                MeasuredAtTimeZone TEXT NOT NULL, 
                IsCfr INTEGER NOT NULL,
                FOREIGN KEY(MeasureSetupEntityId) REFERENCES MeasureSetupEntity_Deleted(MeasureSetupEntityId) ON DELETE CASCADE,
                FOREIGN KEY(OriginalMeasureSetupEntityId) REFERENCES MeasureSetupEntity_Deleted(MeasureSetupEntityId) ON DELETE CASCADE
                )");

            casyContext.Database.ExecuteSqlCommand(
                @"INSERT INTO MeasureResultEntity_Deleted (MeasureResultEntityId, Comment,
                CreatedAt, CreatedBy, DeletedAt, DeletedBy, Experiment, [Group], IsTemporary,
                LastModifiedAt, LastModifiedBy, MeasureResultEntityGuid, MeasureSetupEntityId, Name,
                OriginalMeasureSetupEntityId, SerialNumber, Color, MeasuredAt, Origin, MeasuredAtTimeZone,
                IsCfr) 
                SELECT MeasureResultEntityId, Comment,
                CreatedAt, CreatedBy, DeletedAt, DeletedBy, Experiment, [Group], IsTemporary,
                LastModifiedAt, LastModifiedBy, MeasureResultEntityGuid, MeasureSetupEntityId, Name,
                OriginalMeasureSetupEntityId, SerialNumber, Color, MeasuredAt, Origin, MeasuredAtTimeZone,
                IsCfr FROM MeasureResultEntity
                WHERE IsDelete = 1");

            casyContext.Database.ExecuteSqlCommand(
                @"CREATE TABLE DeviationControlItemEntity_Deleted (
                DeviationControlItemEntityId INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                CreatedAt TEXT NOT NULL,
                CreatedBy TEXT,
                DeletedAt TEXT NOT NULL,
                DeletedBy TEXT,
                LastModifiedAt TEXT,
                LastModifiedBy TEXT,
                MaxLimit REAL,
                MeasureResultItemType INTEGER NOT NULL,
                MeasureSetupEntityId INTEGER NOT NULL,
                MinLimit REAL,
                FOREIGN KEY(MeasureSetupEntityId) REFERENCES MeasureSetupEntity_Deleted(MeasureSetupEntityId) ON DELETE CASCADE
                )");

            casyContext.Database.ExecuteSqlCommand(
                @"INSERT INTO DeviationControlItemEntity_Deleted (DeviationControlItemEntityId, CreatedAt,
                CreatedBy, DeletedAt, DeletedBy, LastModifiedAt, LastModifiedBy, MaxLimit, MeasureResultItemType,
                MeasureSetupEntityId, MinLimit) 
                SELECT DeviationControlItemEntityId, CreatedAt,
                CreatedBy, DeletedAt, DeletedBy, LastModifiedAt, LastModifiedBy, MaxLimit, MeasureResultItemType,
                MeasureSetupEntityId, MinLimit FROM DeviationControlItemEntity
                WHERE IsDelete = 1");

            casyContext.Database.ExecuteSqlCommand(
                @"CREATE TABLE MeasureResultDataEntity_Deleted (
                MeasureResultDataEntityId INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                AboveCalibrationLimitCount INTEGER NOT NULL,
                BelowCalibrationLimitCount INTEGER NOT NULL,
                BelowMeasureLimtCount INTEGER NOT NULL,
                ConcentrationTooHigh INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                CreatedBy TEXT,
                DataBlock TEXT,
                DeletedAt TEXT NOT NULL,
                DeletedBy TEXT,
                LastModifiedAt TEXT,
                LastModifiedBy TEXT,
                MeasureResultEntityId INTEGER NOT NULL,
                Color TEXT,
                FOREIGN KEY(MeasureResultEntityId) REFERENCES MeasureResultEntity_Deleted(MeasureResultEntityId) ON DELETE CASCADE)");

            casyContext.Database.ExecuteSqlCommand(
                @"INSERT INTO MeasureResultDataEntity_Deleted (MeasureResultDataEntityId, AboveCalibrationLimitCount,
                BelowCalibrationLimitCount, BelowMeasureLimtCount, ConcentrationTooHigh, CreatedAt, CreatedBy,
                DataBlock, DeletedAt, DeletedBy, LastModifiedAt, LastModifiedBy, MeasureResultEntityId, Color) 
                SELECT MeasureResultDataEntityId, AboveCalibrationLimitCount,
                BelowCalibrationLimitCount, BelowMeasureLimtCount, ConcentrationTooHigh, CreatedAt, CreatedBy,
                DataBlock, DeletedAt, DeletedBy, LastModifiedAt, LastModifiedBy, MeasureResultEntityId, Color FROM MeasureResultDataEntity
                WHERE IsDelete = 1");

            casyContext.Database.ExecuteSqlCommand(
                @"CREATE TABLE AuditTrailEntryEntity_Deleted (
                AuditTrailEntryEntityId INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                Action TEXT,
	            ComputerName TEXT,
	            DateChanged TEXT NOT NULL,
	            EntityName TEXT,
	            MeasureResultEntityId INTEGER,
                MeasureSetupEntityId INTEGER,
	            NewValue TEXT,
	            OldValue TEXT,
	            PrimaryKeyValue TEXT,
	            PropertyName TEXT,
	            SoftwareVersion TEXT,
	            UserChanged TEXT,
	            FOREIGN KEY(`MeasureResultEntityId`) REFERENCES `MeasureResultEntity_Deleted`(`MeasureResultEntityId`) ON DELETE CASCADE,
                FOREIGN KEY(`MeasureSetupEntityId`) REFERENCES `MeasureSetupEntity_Deleted`(`MeasureSetupEntityId`) ON DELETE CASCADE)");

            casyContext.Database.ExecuteSqlCommand(
                @"INSERT INTO AuditTrailEntryEntity_Deleted(AuditTrailEntryEntityId, Action, ComputerName, DateChanged,
                EntityName, MeasureResultEntityId, MeasureSetupEntityId, NewValue, OldValue, PrimaryKeyValue, PropertyName,
                SoftwareVersion, UserChanged)                
                SELECT AuditTrailEntryEntityId, Action, ComputerName, DateChanged,
                EntityName, MeasureResultEntityId, MeasureSetupEntityId, NewValue, OldValue, PrimaryKeyValue, PropertyName,
                SoftwareVersion, UserChanged
                FROM AuditTrailEntryEntity
                WHERE MeasureResultEntityId IN (SELECT MeasureResultEntityId FROM MeasureResultEntity_Deleted)");

            casyContext.Database.ExecuteSqlCommand("DELETE FROM MeasureSetupEntity WHERE IsDelete = 1");
            casyContext.Database.ExecuteSqlCommand("DELETE FROM CursorEntity WHERE IsDelete = 1");
            casyContext.Database.ExecuteSqlCommand("DELETE FROM MeasureResultEntity WHERE IsDelete = 1");
            casyContext.Database.ExecuteSqlCommand("DELETE FROM DeviationControlItemEntity WHERE IsDelete = 1");
            casyContext.Database.ExecuteSqlCommand("DELETE FROM MeasureResultDataEntity WHERE IsDelete = 1");

            casyContext.Database.ExecuteSqlCommand(
                @"CREATE TABLE MeasureSetupEntity_old(
            MeasureSetupEntityId INTEGER,
            AggregationCalculationMode INTEGER NOT NULL,
            CapillarySize INTEGER NOT NULL,
            CreatedAt TEXT NOT NULL,
            CreatedBy TEXT,
            DilutionFactor REAL NOT NULL,
            FromDiameter INTEGER NOT NULL,
            IsDeviationControlEnabled INTEGER NOT NULL,
            IsReadOnly INTEGER NOT NULL,
            IsSmoothing INTEGER NOT NULL,
            IsTemplate INTEGER NOT NULL,
            LastModifiedAt TEXT,
            LastModifiedBy TEXT,
            ManualAggrgationCalculationFactor REAL NOT NULL,
            MeasureMode INTEGER NOT NULL,
            Name TEXT,
            Repeats INTEGER NOT NULL,
            ScalingMaxRange INTEGER NOT NULL,
            ScalingMode INTEGER NOT NULL,
            SmoothingFactor REAL NOT NULL,
            ToDiameter INTEGER NOT NULL,
            UnitMode INTEGER NOT NULL,
            Volume INTEGER NOT NULL,
            VolumeCorrectionFactor REAL NOT NULL,
            DefaultExperiment TEXT,
            DefaultGroup TEXT,
            IsAutoSave INTEGER NOT NULL,
            AutoSaveName TEXT, 
            DilutionSampleVolume REAL DEFAULT 0.00, 
            DilutionCasyTonVolume REAL DEFAULT 0.00, 
            IsAutoComment INTEGER NOT NULL DEFAULT 0, 
            ChannelCount INTEGER NOT NULL DEFAULT 1024, 
            HasSubpopulations INTEGER NOT NULL DEFAULT 0);
                INSERT INTO MeasureSetupEntity_old SELECT MeasureSetupEntityId, AggregationCalculationMode,
                CapillarySize, CreatedAt, CreatedBy, DilutionFactor, FromDiameter,
                IsDeviationControlEnabled, IsReadOnly, IsSmoothing, IsTemplate, LastModifiedAt, LastModifiedBy,
                ManualAggrgationCalculationFactor, MeasureMode, Name, Repeats, ScalingMaxRange, ScalingMode,
                SmoothingFactor, ToDiameter, UnitMode, Volume, VolumeCorrectionFactor, DefaultExperiment,
                DefaultGroup, IsAutoSave, AutoSaveName, DilutionSampleVolume, DilutionCasyTonVolume, IsAutoComment,
                ChannelCount, HasSubpopulations FROM MeasureSetupEntity;
                DROP TABLE MeasureSetupEntity;
                CREATE TABLE MeasureSetupEntity(MeasureSetupEntityId INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            AggregationCalculationMode INTEGER NOT NULL,
            CapillarySize INTEGER NOT NULL,
            CreatedAt TEXT NOT NULL,
            CreatedBy TEXT,
            DilutionFactor REAL NOT NULL,
            FromDiameter INTEGER NOT NULL,
            IsDeviationControlEnabled INTEGER NOT NULL,
            IsReadOnly INTEGER NOT NULL,
            IsSmoothing INTEGER NOT NULL,
            IsTemplate INTEGER NOT NULL,
            LastModifiedAt TEXT,
            LastModifiedBy TEXT,
            ManualAggrgationCalculationFactor REAL NOT NULL,
            MeasureMode INTEGER NOT NULL,
            Name TEXT,
            Repeats INTEGER NOT NULL,
            ScalingMaxRange INTEGER NOT NULL,
            ScalingMode INTEGER NOT NULL,
            SmoothingFactor REAL NOT NULL,
            ToDiameter INTEGER NOT NULL,
            UnitMode INTEGER NOT NULL,
            Volume INTEGER NOT NULL,
            VolumeCorrectionFactor REAL NOT NULL,
            DefaultExperiment TEXT,
            DefaultGroup TEXT,
            IsAutoSave INTEGER NOT NULL,
            AutoSaveName TEXT, 
            DilutionSampleVolume REAL DEFAULT 0.00, 
            DilutionCasyTonVolume REAL DEFAULT 0.00, 
            IsAutoComment INTEGER NOT NULL DEFAULT 0, 
            ChannelCount INTEGER NOT NULL DEFAULT 1024, 
            HasSubpopulations INTEGER NOT NULL DEFAULT 0);
                INSERT INTO MeasureSetupEntity SELECT MeasureSetupEntityId, AggregationCalculationMode,
                CapillarySize, CreatedAt, CreatedBy, DilutionFactor, FromDiameter,
                IsDeviationControlEnabled, IsReadOnly, IsSmoothing, IsTemplate, LastModifiedAt, LastModifiedBy,
                ManualAggrgationCalculationFactor, MeasureMode, Name, Repeats, ScalingMaxRange, ScalingMode,
                SmoothingFactor, ToDiameter, UnitMode, Volume, VolumeCorrectionFactor, DefaultExperiment,
                DefaultGroup, IsAutoSave, AutoSaveName, DilutionSampleVolume, DilutionCasyTonVolume, IsAutoComment,
                ChannelCount, HasSubpopulations FROM MeasureSetupEntity_old;
                DROP TABLE MeasureSetupEntity_old;");

            //casyContext.Database.ExecuteSqlCommand(
            //@"CREATE UNIQUE INDEX idx_MeasureSetupEntity 
            //ON MeasureSetupEntity (Name);");

            casyContext.Database.ExecuteSqlCommand(
                @"CREATE TABLE CursorEntity_old (
                CursorEntityId INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                CreatedBy TEXT,
                LastModifiedAt TEXT,
                LastModifiedBy TEXT,
                MaxLimit REAL NOT NULL,
                MeasureSetupEntityId INTEGER NOT NULL,
                MinLimit REAL NOT NULL,
                Name TEXT NOT NULL,
                Color TEXT,
                IsDeadCellsCursor INTEGER DEFAULT 0, 
                Subpopulation TEXT DEFAULT '');
                INSERT INTO CursorEntity_old SELECT CursorEntityId, CreatedAt,
                CreatedBy, LastModifiedBy, LastModifiedAt, MaxLimit, MeasureSetupEntityId,
                MinLimit, Name, Color, IsDeadCellsCursor, Subpopulation FROM CursorEntity;
                DROP TABLE CursorEntity;
                CREATE TABLE CursorEntity(CursorEntityId INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                CreatedAt TEXT NOT NULL,
                CreatedBy TEXT,
                LastModifiedAt TEXT,
                LastModifiedBy TEXT,
                MaxLimit REAL NOT NULL,
                MeasureSetupEntityId INTEGER NOT NULL,
                MinLimit REAL NOT NULL,
                Name TEXT NOT NULL,
                Color TEXT,
                IsDeadCellsCursor INTEGER DEFAULT 0, 
                Subpopulation TEXT DEFAULT '',
                FOREIGN KEY(MeasureSetupEntityId) REFERENCES MeasureSetupEntity(MeasureSetupEntityId) ON DELETE CASCADE);
                INSERT INTO CursorEntity SELECT CursorEntityId, CreatedAt,
                CreatedBy, LastModifiedBy, LastModifiedAt, MaxLimit, MeasureSetupEntityId,
                MinLimit, Name, Color, IsDeadCellsCursor, Subpopulation FROM CursorEntity_old;
                DROP TABLE CursorEntity_old;");

            casyContext.Database.ExecuteSqlCommand(
                @"CREATE TABLE MeasureResultEntity_old(MeasureResultEntityId INTEGER NOT NULL,
                Comment TEXT,
                CreatedAt TEXT NOT NULL,
                CreatedBy TEXT,
                Experiment TEXT,
                [Group] TEXT,
                IsTemporary INTEGER NOT NULL,
                LastModifiedAt TEXT,
                LastModifiedBy TEXT,
                MeasureResultEntityGuid BLOB NOT NULL,
                MeasureSetupEntityId INTEGER NOT NULL,
                Name TEXT,
                OriginalMeasureSetupEntityId INTEGER NOT NULL,
                SerialNumber TEXT,
                Color Text,
                MeasuredAt TEXT NOT NULL,
                Origin TEXT NOT NULL, 
                MeasuredAtTimeZone TEXT NOT NULL, 
                IsCfr INTEGER NOT NULL);
                INSERT INTO MeasureResultEntity_old SELECT MeasureResultEntityId, Comment,
                CreatedAt, CreatedBy, Experiment,[Group], IsTemporary,
                LastModifiedAt, LastModifiedBy, MeasureResultEntityGuid, MeasureSetupEntityId, Name,
                OriginalMeasureSetupEntityId, SerialNumber, Color, MeasuredAt, Origin, MeasuredAtTimeZone,
                IsCfr FROM MeasureResultEntity;
                DROP TABLE MeasureResultEntity;
                CREATE TABLE MeasureResultEntity(MeasureResultEntityId INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                Comment TEXT,
                CreatedAt TEXT NOT NULL,
                CreatedBy TEXT,
                Experiment TEXT,
                [Group] TEXT,
                IsTemporary INTEGER NOT NULL,
                LastModifiedAt TEXT,
                LastModifiedBy TEXT,
                MeasureResultEntityGuid BLOB NOT NULL,
                MeasureSetupEntityId INTEGER NOT NULL,
                Name TEXT,
                OriginalMeasureSetupEntityId INTEGER NOT NULL,
                SerialNumber TEXT,
                Color Text,
                MeasuredAt TEXT NOT NULL,
                Origin TEXT NOT NULL, 
                MeasuredAtTimeZone TEXT NOT NULL, 
                IsCfr INTEGER NOT NULL,
                FOREIGN KEY(MeasureSetupEntityId) REFERENCES MeasureSetupEntity(MeasureSetupEntityId) ON DELETE CASCADE,
                FOREIGN KEY(OriginalMeasureSetupEntityId) REFERENCES MeasureSetupEntity(MeasureSetupEntityId) ON DELETE CASCADE);
                INSERT INTO MeasureResultEntity SELECT MeasureResultEntityId, Comment,
                CreatedAt, CreatedBy, Experiment, [Group], IsTemporary,
                LastModifiedAt, LastModifiedBy, MeasureResultEntityGuid, MeasureSetupEntityId, Name,
                OriginalMeasureSetupEntityId, SerialNumber, Color, MeasuredAt, Origin, MeasuredAtTimeZone,
                IsCfr FROM MeasureResultEntity_old;
                DROP TABLE MeasureResultEntity_old;");

            for (int i = 0; i < 6; i++)
            {
                try
                {
                    casyContext.Database.ExecuteSqlCommand(
                        @"DELETE FROM MeasureResultEntity WHERE MeasureResultEntity.MeasureResultEntityId IN (
                SELECT MeasureResultEntityId
                FROM MeasureResultEntity
                GROUP BY
                Experiment, [Group], Name
                HAVING 
                COUNT(*) > 1)");
                }
                catch
                {

                }
            }

            casyContext.Database.ExecuteSqlCommand(
                @"CREATE UNIQUE INDEX idx_MeasureResultEntity 
                ON MeasureResultEntity (Name, [Group], Experiment);");

            casyContext.Database.ExecuteSqlCommand(
                @"CREATE TABLE DeviationControlItemEntity_old (
                DeviationControlItemEntityId INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                CreatedBy TEXT,
                LastModifiedAt TEXT,
                LastModifiedBy TEXT,
                MaxLimit REAL,
                MeasureResultItemType INTEGER NOT NULL,
                MeasureSetupEntityId INTEGER NOT NULL,
                MinLimit REAL);
                INSERT INTO DeviationControlItemEntity_old SELECT DeviationControlItemEntityId, CreatedAt,
                CreatedBy, LastModifiedAt, LastModifiedBy, MaxLimit, MeasureResultItemType,
                MeasureSetupEntityId, MinLimit FROM DeviationControlItemEntity;
                DROP TABLE DeviationControlItemEntity;
                CREATE TABLE DeviationControlItemEntity(DeviationControlItemEntityId INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                CreatedAt TEXT NOT NULL,
                CreatedBy TEXT,
                LastModifiedAt TEXT,
                LastModifiedBy TEXT,
                MaxLimit REAL,
                MeasureResultItemType INTEGER NOT NULL,
                MeasureSetupEntityId INTEGER NOT NULL,
                MinLimit REAL,
                FOREIGN KEY(MeasureSetupEntityId) REFERENCES MeasureSetupEntity(MeasureSetupEntityId) ON DELETE CASCADE);
                INSERT INTO DeviationControlItemEntity SELECT DeviationControlItemEntityId, CreatedAt,
                CreatedBy, LastModifiedAt, LastModifiedBy, MaxLimit, MeasureResultItemType,
                MeasureSetupEntityId, MinLimit FROM DeviationControlItemEntity_old;
                DROP TABLE DeviationControlItemEntity_old;");

            casyContext.Database.ExecuteSqlCommand(
                @"CREATE TABLE MeasureResultDataEntity_old(MeasureResultDataEntityId INTEGER NOT NULL,
                AboveCalibrationLimitCount INTEGER NOT NULL,
                BelowCalibrationLimitCount INTEGER NOT NULL,
                BelowMeasureLimtCount INTEGER NOT NULL,
                ConcentrationTooHigh INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                CreatedBy TEXT,
                DataBlock TEXT,
                LastModifiedAt TEXT,
                LastModifiedBy TEXT,
                MeasureResultEntityId INTEGER NOT NULL,
                Color TEXT);
                INSERT INTO MeasureResultDataEntity_old SELECT MeasureResultDataEntityId, AboveCalibrationLimitCount,
                BelowCalibrationLimitCount, BelowMeasureLimtCount, ConcentrationTooHigh, CreatedAt, CreatedBy,
                DataBlock, LastModifiedAt, LastModifiedBy, MeasureResultEntityId, Color FROM MeasureResultDataEntity;
                DROP TABLE MeasureResultDataEntity;
                CREATE TABLE MeasureResultDataEntity(MeasureResultDataEntityId INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                AboveCalibrationLimitCount INTEGER NOT NULL,
                BelowCalibrationLimitCount INTEGER NOT NULL,
                BelowMeasureLimtCount INTEGER NOT NULL,
                ConcentrationTooHigh INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                CreatedBy TEXT,
                DataBlock TEXT,
                LastModifiedAt TEXT,
                LastModifiedBy TEXT,
                MeasureResultEntityId INTEGER NOT NULL,
                Color TEXT,
                FOREIGN KEY(MeasureResultEntityId) REFERENCES MeasureResultEntity(MeasureResultEntityId) ON DELETE CASCADE);
                INSERT INTO MeasureResultDataEntity SELECT MeasureResultDataEntityId, AboveCalibrationLimitCount,
                BelowCalibrationLimitCount, BelowMeasureLimtCount, ConcentrationTooHigh, CreatedAt, CreatedBy,
                DataBlock, LastModifiedAt, LastModifiedBy, MeasureResultEntityId, Color FROM MeasureResultDataEntity_old;
                DROP TABLE MeasureResultDataEntity_old;");

            /*
            casyContext.Database.ExecuteSqlCommand(
                @"CREATE TABLE AuditTrailEntryEntity_old(AuditTrailEntryEntityId INTEGER NOT NULL,
                Action TEXT,
	            ComputerName TEXT,
	            DateChanged TEXT NOT NULL,
	            EntityName TEXT,
	            MeasureResultEntityId INTEGER,
                MeasureSetupEntityId INTEGER,
	            NewValue TEXT,
	            OldValue TEXT,
	            PrimaryKeyValue TEXT,
	            PropertyName TEXT,
	            SoftwareVersion TEXT,
	            UserChanged TEXT);
                INSERT INTO AuditTrailEntryEntity_old SELECT AuditTrailEntryEntityId, Action, ComputerName, DateChanged,
                EntityName, MeasureResultEntityId, MeasureSetupEntityId, NewValue, OldValue, PrimaryKeyValue,
                PropertyName, SoftwareVersion, UserChanged FROM AuditTrailEntryEntity;
                DROP TABLE AuditTrailEntryEntity;
                CREATE TABLE AuditTrailEntryEntity(AuditTrailEntryEntityId INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                Action TEXT,
	            ComputerName TEXT,
	            DateChanged TEXT NOT NULL,
	            EntityName TEXT,
	            MeasureResultEntityId INTEGER,
                MeasureSetupEntityId INTEGER,
	            NewValue TEXT,
	            OldValue TEXT,
	            PrimaryKeyValue TEXT,
	            PropertyName TEXT,
	            SoftwareVersion TEXT,
	            UserChanged TEXT,
	            FOREIGN KEY(`MeasureResultEntityId`) REFERENCES `MeasureResultEntity`(`MeasureResultEntityId`),
                FOREIGN KEY(`MeasureSetupEntityId`) REFERENCES `MeasureSetupEntity`(`MeasureSetupEntityId`));
                INSERT INTO AuditTrailEntryEntity SELECT AuditTrailEntryEntityId, Action, ComputerName, DateChanged,
                EntityName, MeasureResultEntityId, MeasureSetupEntityId, NewValue, OldValue, PrimaryKeyValue,
                PropertyName, SoftwareVersion, UserChanged FROM AuditTrailEntryEntity_old;
                DROP TABLE AuditTrailEntryEntity_old;");*/

            casyContext.Database.ExecuteSqlCommand("PRAGMA foreign_keys=on");

            casyContext.Database.ExecuteSqlCommand("PRAGMA user_version = 14");
            DoMigration(casyContext);
        }

        private static void MigrateTo13(CasyContext2 casyContext)
        {
            casyContext.Database.ExecuteSqlCommand("PRAGMA foreign_keys=off");

            casyContext.Database.ExecuteSqlCommand(
                @"CREATE TABLE AuditTrailEntryEntity_old(
                AuditTrailEntryEntityId INTEGER NOT NULL,
	            Action TEXT,
	            ComputerName TEXT,
	            DateChanged TEXT NOT NULL,
	            EntityName TEXT,
	            MeasureResultEntityId INTEGER NOT NULL,
	            NewValue TEXT,
	            OldValue TEXT,
	            PrimaryKeyValue TEXT,
	            PropertyName TEXT,
	            SoftwareVersion TEXT,
	            UserChanged TEXT);
                INSERT INTO AuditTrailEntryEntity_old SELECT AuditTrailEntryEntityId, Action,
                ComputerName, DateChanged, EntityName, MeasureResultEntityId, NewValue,
                OldValue, PrimaryKeyValue, PropertyName, SoftwareVersion, UserChanged FROM AuditTrailEntryEntity;
                DROP TABLE AuditTrailEntryEntity;
                CREATE TABLE AuditTrailEntryEntity(
                AuditTrailEntryEntityId INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                Action TEXT,
	            ComputerName TEXT,
	            DateChanged TEXT NOT NULL,
	            EntityName TEXT,
	            MeasureResultEntityId INTEGER,
                MeasureSetupEntityId INTEGER,
	            NewValue TEXT,
	            OldValue TEXT,
	            PrimaryKeyValue TEXT,
	            PropertyName TEXT,
	            SoftwareVersion TEXT,
	            UserChanged TEXT,
	            FOREIGN KEY(`MeasureResultEntityId`) REFERENCES `MeasureResultEntity`(`MeasureResultEntityId`) ON DELETE CASCADE,
                FOREIGN KEY(`MeasureSetupEntityId`) REFERENCES `MeasureSetupEntity`(`MeasureSetupEntityId`) ON DELETE CASCADE);
                INSERT INTO AuditTrailEntryEntity(AuditTrailEntryEntityId, Action,
                ComputerName, DateChanged, EntityName, MeasureResultEntityId, NewValue,
                OldValue, PrimaryKeyValue, PropertyName, SoftwareVersion, UserChanged) SELECT AuditTrailEntryEntityId, Action,
                ComputerName, DateChanged, EntityName, MeasureResultEntityId, NewValue,
                OldValue, PrimaryKeyValue, PropertyName, SoftwareVersion, UserChanged FROM AuditTrailEntryEntity_old;
                DROP TABLE AuditTrailEntryEntity_old;");

            casyContext.Database.ExecuteSqlCommand("PRAGMA foreign_keys=on");
            casyContext.Database.ExecuteSqlCommand("PRAGMA user_version = 13");
            DoMigration(casyContext);
        }

        private static void MigrateTo12(CasyContext2 casyContext)
        {
            var dateTimeOffsetMin = DateTimeOffset.MinValue.ToString();
            casyContext.Database.ExecuteSqlCommand($"UPDATE CursorEntity SET CreatedAt = '{dateTimeOffsetMin}' WHERE CreatedAt = '0001-01-01 00:00:00'");
            casyContext.Database.ExecuteSqlCommand($"UPDATE CursorEntity SET DeletedAt = '{dateTimeOffsetMin}' WHERE DeletedAt = '0001-01-01 00:00:00'");
            casyContext.Database.ExecuteSqlCommand($"UPDATE CursorEntity SET LastModifiedAt = '{dateTimeOffsetMin}' WHERE LastModifiedAt = '0001-01-01 00:00:00'");

            casyContext.Database.ExecuteSqlCommand($"UPDATE DeviationControlItemEntity SET CreatedAt = '{dateTimeOffsetMin}' WHERE CreatedAt = '0001-01-01 00:00:00'");
            casyContext.Database.ExecuteSqlCommand($"UPDATE DeviationControlItemEntity SET DeletedAt = '{dateTimeOffsetMin}' WHERE DeletedAt = '0001-01-01 00:00:00'");
            casyContext.Database.ExecuteSqlCommand($"UPDATE DeviationControlItemEntity SET LastModifiedAt = '{dateTimeOffsetMin}' WHERE LastModifiedAt = '0001-01-01 00:00:00'");

            casyContext.Database.ExecuteSqlCommand($"UPDATE MeasureResultDataEntity SET CreatedAt = '{dateTimeOffsetMin}' WHERE CreatedAt = '0001-01-01 00:00:00'");
            casyContext.Database.ExecuteSqlCommand($"UPDATE MeasureResultDataEntity SET DeletedAt = '{dateTimeOffsetMin}' WHERE DeletedAt = '0001-01-01 00:00:00'");
            casyContext.Database.ExecuteSqlCommand($"UPDATE MeasureResultDataEntity SET LastModifiedAt = '{dateTimeOffsetMin}' WHERE LastModifiedAt = '0001-01-01 00:00:00'");

            casyContext.Database.ExecuteSqlCommand($"UPDATE MeasureResultEntity SET CreatedAt = '{dateTimeOffsetMin}' WHERE CreatedAt = '0001-01-01 00:00:00'");
            casyContext.Database.ExecuteSqlCommand($"UPDATE MeasureResultEntity SET DeletedAt = '{dateTimeOffsetMin}' WHERE DeletedAt = '0001-01-01 00:00:00'");
            casyContext.Database.ExecuteSqlCommand($"UPDATE MeasureResultEntity SET LastModifiedAt = '{dateTimeOffsetMin}' WHERE LastModifiedAt = '0001-01-01 00:00:00'");

            casyContext.Database.ExecuteSqlCommand($"UPDATE MeasureSetupEntity SET CreatedAt = '{dateTimeOffsetMin}' WHERE CreatedAt = '0001-01-01 00:00:00'");
            casyContext.Database.ExecuteSqlCommand($"UPDATE MeasureSetupEntity SET DeletedAt = '{dateTimeOffsetMin}' WHERE DeletedAt = '0001-01-01 00:00:00'");
            casyContext.Database.ExecuteSqlCommand($"UPDATE MeasureSetupEntity SET LastModifiedAt = '{dateTimeOffsetMin}' WHERE LastModifiedAt = '0001-01-01 00:00:00'");

            casyContext.Database.ExecuteSqlCommand("PRAGMA user_version = 12");
            DoMigration(casyContext);
        }

        private static void MigrateTo11(CasyContext2 casyContext)
        {
            casyContext.Database.ExecuteSqlCommand(
                "ALTER TABLE MeasureSetupEntity ADD COLUMN HasSubpopulations INTEGER NOT NULL DEFAULT 0");
            casyContext.Database.ExecuteSqlCommand("ALTER TABLE CursorEntity ADD COLUMN Subpopulation TEXT DEFAULT ''");
            casyContext.Database.ExecuteSqlCommand("ALTER TABLE SettingsEntity ADD COLUMN BlobValue BLOB");
            casyContext.Database.ExecuteSqlCommand("PRAGMA user_version = 11");
            DoMigration(casyContext);
        }

        private static void MigrateTo9(CasyContext2 casyContext)
        {
            casyContext.Database.ExecuteSqlCommand(
                "ALTER TABLE MeasureResultEntity ADD COLUMN MeasuredAtTimeZone TEXT NOT NULL DEFAULT ''");
            casyContext.Database.ExecuteSqlCommand(
                "ALTER TABLE MeasureResultEntity ADD COLUMN IsCfr INTEGER NOT NULL DEFAULT 0");
            casyContext.Database.ExecuteSqlCommand("PRAGMA user_version = 9");
            DoMigration(casyContext);
        }

        private static void MigrateTo8(CasyContext2 casyContext)
        {
            casyContext.Database.ExecuteSqlCommand(
                "CREATE TABLE UserGroupEntity (UserGroupEntityId INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL)");
            casyContext.Database.ExecuteSqlCommand(
                "CREATE TABLE UserUserGroupMapping (UserEntityId INTEGER NOT NULL, UserGroupEntityId INTEGER NOT NULL, PRIMARY KEY(UserEntityId, UserGroupEntityId))");
            casyContext.Database.ExecuteSqlCommand(
                "CREATE TABLE MeasureResultAccessMapping (MeasureResultAccessMappingId INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, MeasureResultEntityId INTEGER NOT NULL, UserEntityId INTEGER, UserGroupEntityId INTEGER, CanRead INTEGER NOT NULL, CanWrite INTEGER NOT NULL)");
            casyContext.Database.ExecuteSqlCommand("PRAGMA user_version = 8");
            DoMigration(casyContext);
        }

        private static void MigrateTo7(CasyContext2 casyContext)
        {
            casyContext.Database.ExecuteSqlCommand(
                "ALTER TABLE MeasureResultEntity ADD COLUMN MeasuredAt TEXT NOT NULL DEFAULT '0001-01-01 00:00:00'");
            casyContext.Database.ExecuteSqlCommand(
                "ALTER TABLE MeasureResultEntity ADD COLUMN Origin TEXT NOT NULL DEFAULT ''");
            casyContext.Database.ExecuteSqlCommand("PRAGMA user_version = 7");

            DoMigration(casyContext);
        }

        private static void MigrateTo6(CasyContext2 casyContext)
        {
            casyContext.Database.ExecuteSqlCommand("PRAGMA foreign_keys=off");

            casyContext.Database.ExecuteSqlCommand(@"ALTER TABLE CursorEntity RENAME TO _CursorEntity_old;
                        CREATE TABLE CursorEntity (
	CursorEntityId INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
	CreatedAt TEXT NOT NULL,
	CreatedBy TEXT,
	DeletedAt TEXT NOT NULL,
	DeletedBy TEXT,
	IsDelete INTEGER NOT NULL,
	LastModifiedAt TEXT NOT NULL,
	LastModifiedBy TEXT,
	MaxLimit REAL NOT NULL,
	MeasureSetupEntityId INTEGER NOT NULL,
	MinLimit REAL NOT NULL,
	Name TEXT NOT NULL,
    Color TEXT, 
    IsDeadCellsCursor INTEGER DEFAULT 0,
    FOREIGN KEY(`MeasureSetupEntityId`) REFERENCES `MeasureSetupEntity`(`MeasureSetupEntityId`) ON DELETE CASCADE);
                        
    INSERT INTO CursorEntity (CursorEntityId, CreatedAt, CreatedBy, DeletedAt, DeletedBy, IsDelete, LastModifiedAt, LastModifiedBy, MaxLimit, MeasureSetupEntityId, MinLimit, Name, Color, IsDeadCellsCursor)
        SELECT CursorEntityId, CreatedAt, CreatedBy, DeletedAt, DeletedBy, IsDelete, LastModifiedAt, LastModifiedBy, MaxLimit, MeasureSetupEntityId, MinLimit, Name, Color, IsDeadCellsCursor
       FROM _CursorEntity_old;");

            casyContext.Database.ExecuteSqlCommand("PRAGMA foreign_keys=on");
            casyContext.Database.ExecuteSqlCommand(
                "ALTER TABLE MeasureSetupEntity ADD COLUMN ChannelCount INTEGER NOT NULL DEFAULT 1024");
            casyContext.Database.ExecuteSqlCommand("PRAGMA user_version = 6");

            foreach (var cursor in casyContext.Cursors.Include("MeasureSetupEntity").ToList())
            {
                cursor.MinLimit = Calculations.CalcSmoothedDiameter(0, cursor.MeasureSetupEntity.ToDiameter,
                    (int)cursor.MinLimit,
                    cursor.MeasureSetupEntity.ChannelCount == 0 ? 1024 : cursor.MeasureSetupEntity.ChannelCount);
                cursor.MaxLimit = Calculations.CalcSmoothedDiameter(0, cursor.MeasureSetupEntity.ToDiameter,
                    (int)cursor.MaxLimit,
                    cursor.MeasureSetupEntity.ChannelCount == 0 ? 1024 : cursor.MeasureSetupEntity.ChannelCount);
            }

            casyContext.SaveChanges(true);

            DoMigration(casyContext);
        }

        private static void MigrateTo5(CasyContext2 casyContext)
        {
            casyContext.Database.ExecuteSqlCommand(
                "ALTER TABLE MeasureSetupEntity ADD COLUMN IsAutoComment INTEGER NOT NULL DEFAULT 0");
            casyContext.Database.ExecuteSqlCommand("PRAGMA user_version = 5");

            DoMigration(casyContext);
        }

        private static void MigrateTo4(CasyContext2 casyContext)
        {
            casyContext.Database.ExecuteSqlCommand("ALTER TABLE UserEntity ADD COLUMN FavoriteTemplateIds TEXT DEFAULT ''");
            casyContext.Database.ExecuteSqlCommand("PRAGMA user_version = 4");

            DoMigration(casyContext);
        }

        private static void MigrateTo3(CasyContext2 casyContext)
        {
            casyContext.Database.ExecuteSqlCommand(
                "ALTER TABLE UserEntity ADD COLUMN KeyboardCountryRegionName TEXT DEFAULT 'de-DE'");
            casyContext.Database.ExecuteSqlCommand("PRAGMA user_version = 3");

            DoMigration(casyContext);
        }

        private static void MigrateTo2(CasyContext2 casyContext)
        {
            casyContext.Database.ExecuteSqlCommand("ALTER TABLE UserEntity ADD COLUMN RecentTemplateIds TEXT DEFAULT ''");
            casyContext.Database.ExecuteSqlCommand("PRAGMA user_version = 2");

            DoMigration(casyContext);
        }

        private static void MigrateTo1(CasyContext2 casyContext)
        {
            casyContext.Database.ExecuteSqlCommand(
                "ALTER TABLE MeasureSetupEntity ADD COLUMN DilutionSampleVolume REAL DEFAULT 0.00");
            casyContext.Database.ExecuteSqlCommand(
                "ALTER TABLE MeasureSetupEntity ADD COLUMN DilutionCasyTonVolume REAL DEFAULT 0.00");
            casyContext.Database.ExecuteSqlCommand("ALTER TABLE CursorEntity ADD COLUMN IsDeadCellsCursor INTEGER DEFAULT 0");
            casyContext.Database.ExecuteSqlCommand("PRAGMA user_version = 1");

            foreach (var entity in casyContext.MeasureSetups)
            {
                entity.DilutionCasyTonVolume = 0d;
                entity.DilutionSampleVolume = 0d;
            }

            casyContext.SaveChanges();

            var deadCellsCursorEntities = casyContext.Cursors.Where(ce => ce.Name.ToLower() == "dead cells").ToList();
            foreach (var entity in deadCellsCursorEntities)
            {
                entity.IsDeadCellsCursor = true;
            }

            casyContext.SaveChanges();

            DoMigration(casyContext);
        }
    }
}
