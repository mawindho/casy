using OLS.Casy.IO.SQLite.Entities;
using OLS.Casy.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using OLS.Casy.IO.SQLite.Standard;
using TimeZoneConverter;

namespace OLS.Casy.WebService.Host.Handlers
{
    public class DatabaseAccessService
    {
        public static IEnumerable<string> GetExperiments(CasyContext casyContext)
        {
            var result = new List<string>();

            var experimentQuery = casyContext.MeasureResults.Where(x => !x.IsTemporary).AsQueryable();
            var experiments = experimentQuery.Select(x => new { x.Experiment, x.Group, x.MeasureResultEntityId }).GroupBy(x => x.Experiment).ToList();

            foreach (var expGroup in experiments)
            {
                result.Add(expGroup.Key);
            }

            return result;
        }

        public static IEnumerable<string> GetGroups(CasyContext casyContext, string experiment)
        {
            var result = new List<string>();
            var temp1 = casyContext.MeasureResults.ToList();
            var groupQuery = casyContext.MeasureResults.Where(x => !x.IsTemporary).Where(x => x.Experiment == (experiment == "null" ? "" : experiment));
            var temp = groupQuery.ToList();
            var groups = groupQuery.Select(x => new { x.Group, x.MeasureResultEntityId }).GroupBy(x => x.Group).ToList();

            foreach (var grpGroup in groups)
            {
                result.Add(grpGroup.Key);
            }

            return result;
        }

        public static IEnumerable<Tuple<int, string>> GetMeasureResults(CasyContext casyContext, string experiment, string group)
        {
            var result = new List<Tuple<int, string>>();

            var measureResultEntities = casyContext.MeasureResults
                .Where(x => !x.IsTemporary && 
                            x.Experiment == (experiment == "null" ? "" : experiment) && 
                            x.Group == (group == "null" ? "" : group)).ToList();

            foreach(var measureResult in measureResultEntities)
            {
                result.Add(new Tuple<int, string>(measureResult.MeasureResultEntityId, measureResult.Name));
            }
            return result;
        }

        public static MeasureResult GeMeasureResult(CasyContext casyContext, int id)
        {
            var measureResultEntity = casyContext.MeasureResults
                .Include("MeasureResultAnnotationEntities")
                .Include("MeasureResultAnnotationEntities.AnnotationTypeEntity")
                .Include("MeasureResultDataEntities")
                .Include("MeasureSetupEntity")
                .Include("AuditTrailEntrieEntities")
                .FirstOrDefault(x => x.MeasureResultEntityId == id);

            MeasureResult measureResult = null;
            if (measureResultEntity != null)
            {
                measureResult = EntityToModel(casyContext, measureResultEntity);
            }
            return measureResult;
        }

        public static MeasureResult GetMeasureResult(CasyContext casyContext, string experiment, string group, string name)
        {
            var measureResultEntity = casyContext
                .MeasureResults
                .Include("MeasureResultAnnotationEntities")
                .Include("MeasureResultAnnotationEntities.AnnotationTypeEntity")
                .Include("MeasureResultDataEntities")
                .Include("MeasureSetupEntity")
                .Include("AuditTrailEntrieEntities")
                .FirstOrDefault(x => x.Experiment == (experiment == "null" ? "" : experiment) &&
                                    x.Group == (group == "null" ? "" : group) &&
                                    x.Name == name);

            MeasureResult measureResult = null;
            if (measureResultEntity != null)
            {
                measureResult = EntityToModel(casyContext, measureResultEntity);
            }
            return measureResult;
        }

        private static MeasureResult EntityToModel(CasyContext casyContext, MeasureResultEntity measureResultEntity)
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
                Experiment = measureResultEntity.Experiment,
                Group = measureResultEntity.Group,
                Color = measureResultEntity.Color,
                MeasuredAt = measureResultEntity.MeasuredAt,
                Origin = measureResultEntity.Origin,
                MeasuredAtTimeZone = string.IsNullOrEmpty(measureResultEntity.MeasuredAtTimeZone)
                    ? TimeZoneInfo.Local
                    : TZConvert.GetTimeZoneInfo(measureResultEntity.MeasuredAtTimeZone),
                IsCfr = measureResultEntity.IsCfr
            };

            foreach (var measureResultAnnotationEntity in measureResultEntity.MeasureResultAnnotationEntities)
            {
                var measureResultAnnotation = EntityToModel(measureResultAnnotationEntity);
                if (measureResultAnnotation == null) continue;
                measureResultAnnotation.MeasureResult = measureResult;
                measureResult.MeasureResultAnnotations.Add(measureResultAnnotation);
            }

            foreach (var measureResultDataEntity in measureResultEntity.MeasureResultDataEntities)
            {
                if (measureResult.MeasureResultDatas.Any(mrd =>
                    mrd.MeasureResultDataId == measureResultDataEntity.MeasureResultDataEntityId)) continue;

                var measureResultData = new MeasureResultData()
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
                measureResultData.MeasureResult = measureResult;
                measureResult.MeasureResultDatas.Add(measureResultData);
            }

            foreach (var auditTrailEntryEntity in measureResultEntity.AuditTrailEntrieEntities)
            {
                var auditTrailEntry = new AuditTrailEntry
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
                auditTrailEntry.MeasureResult = measureResult;
                measureResult.AuditTrailEntries.Add(auditTrailEntry);
            }

            measureResult.MeasureSetup = GetMeasureSetup(casyContext, measureResultEntity.MeasureSetupEntity.MeasureSetupEntityId);
            measureResult.MeasureSetup.MeasureResult = measureResult;

            return measureResult;

        }

        private static MeasureSetup GetMeasureSetup(CasyContext casyContext, int measureSetupId)
        {
            //TODO: Um gelöschte kümmern

            var measureSetupEntity = casyContext.MeasureSetups.Include("CursorEntities")
                .Include("DeviationControlItemEntities")
                .FirstOrDefault(ms => ms.MeasureSetupEntityId == measureSetupId);

            if (measureSetupEntity == null) return null;

            var measureSetup = new MeasureSetup
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
                    MinLimit = deviationControlItemEntity.MinLimit
                });
            }

            return measureSetup;
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
    }
}
