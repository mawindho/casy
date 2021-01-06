using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OLS.Casy.Models.Enums;
using OLS.Casy.WebService.Host.Dtos;
using OLS.Casy.WebService.Host.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OLS.Casy.IO.SQLite.Standard;
using OLS.Casy.Models;
using OLS.Casy.WebService.Host.Services;

namespace OLS.Casy.WebService.Host.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class MeasureResultsController : ControllerBase
    {
        private CasyContext _casyContext;
        private readonly MeasureResultDataCalculationService _measureResultDataCalculationService;

        public MeasureResultsController(CasyContext casyContext, MeasureResultDataCalculationService measureResultDataCalculationService)
        {
            _casyContext = casyContext;
            _measureResultDataCalculationService = measureResultDataCalculationService;
        }

        [AllowAnonymous]
        [HttpGet("/ping")]
        public IActionResult Ping()
        {
            return Ok("Pong");
        }

        [HttpGet]
        public async Task<IActionResult> GetExperiments()
        {
            var experiments = DatabaseAccessService.GetExperiments(_casyContext);
            return Ok(experiments);
        }

        [HttpGet]
        [Route("{experiment}")]
        public async Task<IActionResult> GetGroups(string experiment)
        {
            var groups = DatabaseAccessService.GetGroups(_casyContext, Uri.UnescapeDataString(experiment));
            return Ok(groups);
        }

        [HttpGet]
        [Route("{experiment}/{group}")]
        public async Task<IActionResult> GetMeasureResults(string experiment, string group)
        {
            var measureResults = DatabaseAccessService.GetMeasureResults(_casyContext, Uri.UnescapeDataString(experiment), Uri.UnescapeDataString(group));
            return Ok(measureResults.Select(x => new MeasureResulltInfoDto
            {
                Id = x.Item1,
                Name = x.Item2
            }));
        }

        [HttpGet]
        [Route("byid/{id}")]
        public async Task<IActionResult> GetMeasureResultById(int id)
        {
            var measureResult = DatabaseAccessService.GeMeasureResult(_casyContext, id);
            return Ok(new MeasureResulltInfoDto
            {
                Id = measureResult.MeasureResultId,
                Name = measureResult.Name,
                Experiment = measureResult.Experiment,
                Group = measureResult.Group
            });
        }

        [HttpGet]
        [Route("{experiment}/{group}/{name}")]
        public async Task<IActionResult> Get(string experiment, string group, string name)
        {
            var measureResult = DatabaseAccessService.GetMeasureResult(_casyContext, Uri.UnescapeDataString(experiment), Uri.UnescapeDataString(group), Uri.UnescapeDataString(name));

            if (measureResult != null)
            {
                var measureResultData = await _measureResultDataCalculationService.GetMeasureResultDataAsync(measureResult, measureResult.MeasureSetup);

                return Ok(new MeasureResultDto
                {
                    Name = measureResult.Name,
                    Comment = measureResult.Comment,
                    Experiment = measureResult.Experiment,
                    Group = measureResult.Group,
                    SerialNumber = measureResult.SerialNumber,
                    AggregationCalculationMode = Enum.GetName(typeof(AggregationCalculationModes), measureResult.MeasureSetup.AggregationCalculationMode),
                    ManualAggrgationCalculationFactor = measureResult.MeasureSetup.ManualAggregationCalculationFactor,
                    AuditTrailItems = new List<AuditTrailDto>(measureResult.AuditTrailEntries.Select(entity => new AuditTrailDto
                    {
                        Action = entity.Action,
                        ComputerName = entity.ComputerName,
                        DateChanged = entity.DateChanged.UtcDateTime.ToString(),
                        EntityName = entity.EntityName,
                        NewValue = entity.NewValue,
                        OldValue = entity.OldValue,
                        PrimaryKeyValue = entity.PrimaryKeyValue,
                        PropertyName = entity.PropertyName,
                        SoftwareVersion = entity.SoftwareVersion,
                        UserChanged = entity.UserChanged
                    }).ToList()),
                    CapillarySize = measureResult.MeasureSetup.CapillarySize,
                    ChannelCount = measureResult.MeasureSetup.ChannelCount,
                    CreatedAt = measureResult.CreatedAt.ToString(),
                    CreatedBy = measureResult.CreatedBy,
                    IsDeviationControlEnabled = measureResult.MeasureSetup.IsDeviationControlEnabled,
                    DeviationConrolItems = new List<DeviationControlDto>(measureResult.MeasureSetup.DeviationControlItems.Select(entity => new DeviationControlDto
                    {
                        MaxLimit = entity.MaxLimit,
                        MinLimit = entity.MinLimit,
                        MeasureResultItemType = Enum.GetName(typeof(MeasureResultItemTypes), entity.MeasureResultItemType)
                    })),
                    DilutionCasyTonVolume = measureResult.MeasureSetup.DilutionCasyTonVolume,
                    DilutionFactor = measureResult.MeasureSetup.DilutionFactor,
                    DilutionSampleVolume = measureResult.MeasureSetup.DilutionSampleVolume,
                    FromDiameter = measureResult.MeasureSetup.FromDiameter,
                    ToDiameter = measureResult.MeasureSetup.ToDiameter,
                    HasSubpopulations = measureResult.MeasureSetup.HasSubpopulations,
                    IsCfr = measureResult.IsCfr,
                    IsSmoothing = measureResult.MeasureSetup.IsSmoothing,
                    SmoothingFactor = measureResult.MeasureSetup.SmoothingFactor,
                    LastModifiedAt = measureResult.LastModifiedAt.ToString(),
                    LastModifiedBy = measureResult.LastModifiedBy,
                    MeasuredAt = measureResult.MeasuredAt,
                    MeasuredAtTimeZone = measureResult.MeasuredAtTimeZone,
                    MeasureMode = Enum.GetName(typeof(MeasureModes), measureResult.MeasureSetup.MeasureMode),
                    MeasureResultDataItems = new List<MeasureResultDataDto>(measureResult.MeasureResultDatas.Select(entity => new MeasureResultDataDto
                    {
                        AboveCalibrationLimitCount = entity.AboveCalibrationLimitCount,
                        BelowCalibrationLimitCount = entity.BelowCalibrationLimitCount,
                        BelowMeasureLimitCount = entity.BelowMeasureLimtCount,
                        ConcentrationTooHigh = entity.ConcentrationTooHigh,
                        DataBlock = entity.InternalDataBlock
                    })),
                    MeasureResultGuid = measureResult.MeasureResultGuid,
                    Origin = measureResult.Origin,
                    Ranges = new List<RangeDto>(measureResult.MeasureSetup.Cursors.Select(entity => new RangeDto
                    {
                        CreatedAt = entity.CreatedAt.ToString(),
                        CreatedBy = entity.CreatedBy,
                        IsDeadCellsCursor = entity.IsDeadCellsCursor,
                        LastModifiedAt = entity.LastModifiedAt.ToString(),
                        LastModifiedBy = entity.LastModifiedBy,
                        MaxLimit = entity.MaxLimit,
                        MinLimit = entity.MinLimit,
                        Name = entity.Name,
                        Subpopulation = entity.Subpopulation
                    })),
                    Repeats = measureResult.MeasureSetup.Repeats,
                    ScalingMaxRange = measureResult.MeasureSetup.ScalingMaxRange,
                    ScalingMode = Enum.GetName(typeof(ScalingModes), measureResult.MeasureSetup.ScalingMode),
                    TemplateName = measureResult.MeasureSetup.Name,
                    UnitMode = Enum.GetName(typeof(UnitModes), measureResult.MeasureSetup.UnitMode),
                    Volume = Enum.GetName(typeof(Volumes), measureResult.MeasureSetup.Volume),
                    VolumeCorrectionFactor = measureResult.MeasureSetup.VolumeCorrectionFactor,
                    MeasureResultCalculations = new List<MeasureResultCalculationDto>(measureResultData.Select(entity => new MeasureResultCalculationDto
                    {
                        AssociatedRange = entity.Cursor == null ? null : entity.Cursor.Name,
                        Deviation = entity.Deviation,
                        MeasureResultItemType = Enum.GetName(typeof(MeasureResultItemTypes), entity.MeasureResultItemType),
                        ResultItemValue = entity.ResultItemValue
                    })),
                    LastWeeklyClean = measureResult.LastWeeklyClean.ToString(),
                    Color = measureResult.Color
                });
            }

            return NotFound(); 
        }
        
        [HttpPost]
        [Route("overlay")]
        public async Task<IActionResult> GetOverlayResults([FromBody] OverlayDto overlayDto)
        {
            var measureResults = new List<MeasureResult>();
            foreach(var id in overlayDto.MeasureResultIds)
            {
                var measureResult = DatabaseAccessService.GeMeasureResult(_casyContext, id);
                measureResults.Add(measureResult);
            }

            var overlayMeasureSetup = new MeasureSetup();

            if (measureResults.Count > 0)
            {
                var invalidParameters = string.Empty;

                var toDiameters = measureResults.Select(mr => mr.MeasureSetup.ToDiameter).Distinct().ToArray();
                if (toDiameters.Length > 1)
                {
                    invalidParameters +=
                        $"- ToDiameter\n";
                }

                var fromDiameters = measureResults.Select(mr => mr.MeasureSetup.FromDiameter).Distinct().ToArray();
                if (fromDiameters.Length > 1)
                {
                    invalidParameters +=
                        $"- FromDiameter\n";
                }

                //Kapillare
                var capillaries = measureResults.Select(mr => mr.MeasureSetup.CapillarySize).Distinct().ToArray();
                if (capillaries.Length > 1)
                {
                    invalidParameters +=
                        $"- CapillarySize\n";
                }

                if (!string.IsNullOrEmpty(invalidParameters))
                {
                    overlayDto.ErrorMessage = invalidParameters;
                    return BadRequest(overlayDto);
                }

                overlayMeasureSetup.ChannelCount = measureResults.Max(mr => mr.MeasureSetup.ChannelCount);
                overlayDto.ChannelCount = overlayMeasureSetup.ChannelCount;

                overlayMeasureSetup.ToDiameter = toDiameters[0];
                overlayDto.ToDiameter = toDiameters[0];

                overlayMeasureSetup.FromDiameter = fromDiameters[0];
                overlayDto.FromDiameter = fromDiameters[0];

                overlayMeasureSetup.CapillarySize = capillaries[0];
                overlayDto.CapillarySize = capillaries[0];

                overlayMeasureSetup.Repeats = measureResults.Max(mr => mr.MeasureSetup.Repeats);
                overlayDto.Repeats = overlayMeasureSetup.Repeats;

                var volumes = measureResults.Select(mr => mr.MeasureSetup.Volume).Distinct().ToArray();
                overlayMeasureSetup.Volume = volumes.Length > 1 ? Volumes.TwoHundred : volumes[0];
                overlayDto.Volume = Enum.GetName(typeof(Volumes), overlayMeasureSetup.Volume);

                overlayMeasureSetup.VolumeCorrectionFactor = measureResults[0].MeasureSetup.VolumeCorrectionFactor;
                overlayDto.VolumeCorrectionFactor = measureResults[0].MeasureSetup.VolumeCorrectionFactor;

                var measureModes = measureResults.Select(mr => mr.MeasureSetup.MeasureMode).Distinct().ToArray();
                overlayMeasureSetup.MeasureMode = measureModes.Length > 1 ? MeasureModes.MultipleCursor : measureModes[0];
                overlayDto.MeasureMode = Enum.GetName(typeof(MeasureModes), overlayMeasureSetup.MeasureMode);

                var unitModes = measureResults.Select(mr => mr.MeasureSetup.UnitMode).Distinct().ToArray();
                overlayMeasureSetup.UnitMode = unitModes.Length > 1 ? UnitModes.Counts : unitModes[0];
                overlayDto.UnitMode = Enum.GetName(typeof(UnitModes), overlayMeasureSetup.UnitMode);

                overlayMeasureSetup.DilutionFactor = measureResults[0].MeasureSetup.DilutionFactor;
                overlayDto.DilutionFactor = measureResults[0].MeasureSetup.DilutionFactor;

                overlayMeasureSetup.IsSmoothing = measureResults.All(mr => mr.MeasureSetup.IsSmoothing);
                overlayDto.IsSmoothing = overlayMeasureSetup.IsSmoothing;
                if (overlayMeasureSetup.IsSmoothing)
                {
                    var smoothingFactors = measureResults.Select(mr => mr.MeasureSetup.SmoothingFactor).Distinct().ToArray();
                    overlayMeasureSetup.SmoothingFactor = smoothingFactors.Length > 1 ? 0d : smoothingFactors[0];
                    overlayDto.SmoothingFactor = overlayMeasureSetup.SmoothingFactor;
                }

                var scalingModes = measureResults.Select(mr => mr.MeasureSetup.ScalingMode).Distinct().ToArray();
                overlayMeasureSetup.ScalingMode = scalingModes.Length > 1 ? ScalingModes.Auto : scalingModes[0];
                overlayDto.ScalingMode = Enum.GetName(typeof(ScalingModes), overlayMeasureSetup.ScalingMode);

                if (overlayMeasureSetup.ScalingMode == ScalingModes.MaxRange)
                {
                    var scalingMaxRanges = measureResults.Select(mr => mr.MeasureSetup.ScalingMaxRange).Distinct().ToArray();
                    overlayMeasureSetup.ScalingMaxRange = scalingMaxRanges.Length > 1 ? scalingMaxRanges.Max() : scalingMaxRanges[0];
                    overlayDto.ScalingMaxRange = overlayMeasureSetup.ScalingMaxRange;
                }

                var aggregationCalculationModes = measureResults.Select(mr => mr.MeasureSetup.AggregationCalculationMode).Distinct().ToArray();
                overlayMeasureSetup.AggregationCalculationMode = aggregationCalculationModes.Length > 1 ? AggregationCalculationModes.FromParent : aggregationCalculationModes[0];
                overlayDto.AggregationCalculationMode = Enum.GetName(typeof(AggregationCalculationModes), overlayMeasureSetup.AggregationCalculationMode);

                if (overlayMeasureSetup.AggregationCalculationMode == AggregationCalculationModes.Manual)
                {
                    var aggregationCalculationManualValues = measureResults.Select(mr => mr.MeasureSetup.ManualAggregationCalculationFactor).Distinct().ToArray();
                    if (aggregationCalculationManualValues.Length > 1)
                    {
                        overlayMeasureSetup.AggregationCalculationMode = AggregationCalculationModes.FromParent;
                        overlayDto.AggregationCalculationMode = Enum.GetName(typeof(AggregationCalculationModes), AggregationCalculationModes.FromParent);
                    }
                    else
                    {
                        overlayMeasureSetup.ManualAggregationCalculationFactor = aggregationCalculationManualValues[0];
                        overlayDto.ManualAggrgationCalculationFactor = aggregationCalculationManualValues[0];
                    }
                }

                var cursors = measureResults[0].MeasureSetup.Cursors.ToArray();

                var areRangesEqual = measureResults.Select(mr => mr.MeasureSetup.Cursors.Count).Distinct().Count() == 1;

                if (areRangesEqual)
                {
                    foreach (var measureResult in measureResults)
                    {
                        if (measureResult != measureResults[0])
                        {
                            foreach (var cursor in measureResult.MeasureSetup.Cursors)
                            {
                                areRangesEqual &= cursors.Any(c => c.Name == cursor.Name && c.MaxLimit == cursor.MaxLimit && c.MinLimit == cursor.MinLimit);
                                if (!areRangesEqual)
                                {
                                    break;
                                }
                            }
                        }
                        if (!areRangesEqual)
                        {
                            break;
                        }
                    }
                }

                if (areRangesEqual)
                {
                    overlayMeasureSetup.ClearCursors();
                    foreach (var cursor in cursors)
                    {
                        overlayMeasureSetup.AddCursor(new Cursor
                        {
                            Name = cursor.Name,
                            MinLimit = cursor.MinLimit,
                            MaxLimit = cursor.MaxLimit,
                            IsDeadCellsCursor = cursor.IsDeadCellsCursor,
                            Color = cursor.Color,
                            MeasureSetup = overlayMeasureSetup
                        });
                    }
                    overlayDto.Ranges = new List<RangeDto>(cursors.Select(entity => new RangeDto
                    {
                        CreatedAt = entity.CreatedAt.ToString(),
                        CreatedBy = entity.CreatedBy,
                        IsDeadCellsCursor = entity.IsDeadCellsCursor,
                        LastModifiedAt = entity.LastModifiedAt.ToString(),
                        LastModifiedBy = entity.LastModifiedBy,
                        MaxLimit = entity.MaxLimit,
                        MinLimit = entity.MinLimit,
                        Name = entity.Name,
                        Subpopulation = entity.Subpopulation
                    }));
                }
                else
                {
                    overlayDto.ErrorMessage = "Ranges are not equal";
                    return BadRequest(overlayDto);
                }

                var measureResultDtos = new List<MeasureResultDto>();
                foreach (var measureResult in measureResults)
                {
                    var measureResultData =
                        await _measureResultDataCalculationService.GetMeasureResultDataAsync(measureResult,
                            overlayMeasureSetup);
                    measureResultDtos.Add(new MeasureResultDto()
                    {
                        Comment = measureResult.Comment,
                        Name = measureResult.Name,
                        Color = measureResult.Color,
                        MeasureResultDataItems = new List<MeasureResultDataDto>(measureResult.MeasureResultDatas.Select(
                            entity =>
                                new MeasureResultDataDto
                                {
                                    AboveCalibrationLimitCount = entity.AboveCalibrationLimitCount,
                                    BelowCalibrationLimitCount = entity.BelowCalibrationLimitCount,
                                    BelowMeasureLimitCount = entity.BelowMeasureLimtCount,
                                    ConcentrationTooHigh = entity.ConcentrationTooHigh,
                                    DataBlock = entity.InternalDataBlock
                                })),
                        MeasureResultCalculations = new List<MeasureResultCalculationDto>(measureResultData.Select(
                            entity => new MeasureResultCalculationDto
                            {
                                AssociatedRange = entity.Cursor?.Name,
                                Deviation = entity.Deviation,
                                MeasureResultItemType = Enum.GetName(typeof(MeasureResultItemTypes),
                                    entity.MeasureResultItemType),
                                ResultItemValue = entity.ResultItemValue
                            })),
                    });
                }

                overlayDto.MeasureResults = measureResultDtos;

                return Ok(overlayDto);
            }

            overlayDto.ErrorMessage = "No results found";
            return BadRequest(overlayDto);
        }

        [HttpPost]
        [Route("mean")]
        public async Task<IActionResult> GetMeanResult([FromBody] MeanDto meanDto)
        {
            var measureResults = new List<MeasureResult>();
            foreach (var id in meanDto.MeasureResultIds)
            {
                var measureResult = DatabaseAccessService.GeMeasureResult(_casyContext, id);
                measureResults.Add(measureResult);
            }

            var meanMeasureSetup = new MeasureSetup();

            if (measureResults.Count > 0)
            {
                var invalidParameters = string.Empty;

                var toDiameters = measureResults.Select(mr => mr.MeasureSetup.ToDiameter).Distinct().ToArray();
                if (toDiameters.Length > 1)
                {
                    invalidParameters +=
                        $"- ToDiameter\n";
                }

                var fromDiameters = measureResults.Select(mr => mr.MeasureSetup.FromDiameter).Distinct().ToArray();
                if (fromDiameters.Length > 1)
                {
                    invalidParameters +=
                        $"- FromDiameter\n";
                }

                //Kapillare
                var capillaries = measureResults.Select(mr => mr.MeasureSetup.CapillarySize).Distinct().ToArray();
                if (capillaries.Length > 1)
                {
                    invalidParameters +=
                        $"- CapillarySize\n";
                }

                var repeats = measureResults.Select(mr => mr.MeasureSetup.Repeats).Distinct().ToArray();
                if (repeats.Length > 1)
                {
                    invalidParameters += "- Repeats\n";
                }

                var dilutionFactors = measureResults.Select(mr => mr.MeasureSetup.DilutionFactor).Distinct().ToArray();
                if (dilutionFactors.Length > 1)
                {
                    invalidParameters += "- DilutionFactor\n";
                }

                var volumes = measureResults.Select(mr => mr.MeasureSetup.Volume).Distinct().ToArray();
                if (volumes.Length > 1)
                {
                    invalidParameters += "- Volume\n";
                }

                if (!string.IsNullOrEmpty(invalidParameters))
                {
                    meanDto.ErrorMessage = invalidParameters;
                    return BadRequest(meanDto);
                }

                meanMeasureSetup.ChannelCount = measureResults.Max(mr => mr.MeasureSetup.ChannelCount);
                meanDto.ChannelCount = meanMeasureSetup.ChannelCount;

                meanMeasureSetup.ToDiameter = toDiameters[0];
                meanDto.ToDiameter = meanMeasureSetup.ToDiameter;

                meanMeasureSetup.FromDiameter = fromDiameters[0];
                meanDto.FromDiameter = meanMeasureSetup.FromDiameter;

                meanMeasureSetup.Repeats = repeats[0];
                meanDto.Repeats = meanMeasureSetup.Repeats;

                meanMeasureSetup.CapillarySize = capillaries[0];
                meanDto.CapillarySize = meanMeasureSetup.CapillarySize;

                meanMeasureSetup.DilutionFactor = dilutionFactors[0];
                meanDto.DilutionFactor = meanMeasureSetup.DilutionFactor;
                
                meanMeasureSetup.Volume = volumes[0];
                meanDto.Volume = Enum.GetName(typeof(Volumes), meanMeasureSetup.Volume);

                var unitModes = measureResults.Select(mr => mr.MeasureSetup.UnitMode).Distinct().ToArray();
                meanMeasureSetup.UnitMode = unitModes.Length > 1 ? UnitModes.Counts : unitModes[0];
                meanDto.UnitMode = Enum.GetName(typeof(UnitModes), meanMeasureSetup.UnitMode);

                meanMeasureSetup.IsSmoothing = measureResults.All(mr => mr.MeasureSetup.IsSmoothing);
                meanDto.IsSmoothing = meanMeasureSetup.IsSmoothing;

                if (meanMeasureSetup.IsSmoothing)
                {
                    var smoothingFactors = measureResults.Select(mr => mr.MeasureSetup.SmoothingFactor).Distinct().ToArray();
                    meanMeasureSetup.SmoothingFactor = smoothingFactors.Length > 1 ? 5d : smoothingFactors[0];
                    meanDto.SmoothingFactor = meanMeasureSetup.SmoothingFactor;
                }

                var scalingModes = measureResults.Select(mr => mr.MeasureSetup.ScalingMode).Distinct().ToArray();
                meanMeasureSetup.ScalingMode = scalingModes.Count() > 1 ? ScalingModes.Auto : scalingModes[0];
                meanDto.ScalingMode = Enum.GetName(typeof(ScalingModes), meanMeasureSetup.ScalingMode);

                if (meanMeasureSetup.ScalingMode == ScalingModes.MaxRange)
                {
                    var scalingMaxRanges = measureResults.Select(mr => mr.MeasureSetup.ScalingMaxRange).Distinct().ToArray();
                    meanMeasureSetup.ScalingMaxRange = scalingMaxRanges.Length > 1 ? scalingMaxRanges.Max() : scalingMaxRanges[0];
                    meanDto.ScalingMaxRange = meanMeasureSetup.ScalingMaxRange;
                }

                var aggregationCalculationModes = measureResults.Select(mr => mr.MeasureSetup.AggregationCalculationMode).Distinct().ToArray();
                meanMeasureSetup.AggregationCalculationMode = aggregationCalculationModes.Length > 1 ? AggregationCalculationModes.Off : aggregationCalculationModes[0];
                meanDto.AggregationCalculationMode = Enum.GetName(typeof(AggregationCalculationModes),
                    meanMeasureSetup.AggregationCalculationMode);

                if (meanMeasureSetup.AggregationCalculationMode == AggregationCalculationModes.Manual)
                {
                    var aggregationCalculationManualValues = measureResults.Select(mr => mr.MeasureSetup.ManualAggregationCalculationFactor).Distinct().ToArray();
                    meanMeasureSetup.ManualAggregationCalculationFactor = aggregationCalculationManualValues.Length > 1 ? aggregationCalculationManualValues.Max() : aggregationCalculationManualValues[0];
                    meanDto.ManualAggrgationCalculationFactor = meanMeasureSetup.ManualAggregationCalculationFactor;
                }

                var measureModes = measureResults.Select(mr => mr.MeasureSetup.MeasureMode).Distinct().ToArray();
                meanMeasureSetup.MeasureMode = measureModes.Length > 1 ? MeasureModes.MultipleCursor : measureModes[0];
                meanDto.MeasureMode = Enum.GetName(typeof(MeasureModes), meanMeasureSetup.MeasureMode);

                var cursors = measureResults[0].MeasureSetup.Cursors.ToArray();

                var areRangesEqual = measureResults.Select(mr => mr.MeasureSetup.Cursors.Count).Distinct().Count() == 1;

                if (areRangesEqual)
                {
                    foreach (var measureResult in measureResults)
                    {
                        if (measureResult != measureResults[0])
                        {
                            foreach (var cursor in measureResult.MeasureSetup.Cursors)
                            {
                                areRangesEqual &= cursors.Any(c => c.Name == cursor.Name && c.MaxLimit == cursor.MaxLimit && c.MinLimit == cursor.MinLimit);
                                if (!areRangesEqual)
                                {
                                    break;
                                }
                            }
                        }
                        if (!areRangesEqual)
                        {
                            break;
                        }
                    }
                }

                if (areRangesEqual)
                {
                    meanMeasureSetup.ClearCursors();
                    foreach (var cursor in cursors)
                    {
                        meanMeasureSetup.AddCursor(new Cursor
                        {
                            Name = cursor.Name,
                            MinLimit = cursor.MinLimit,
                            MaxLimit = cursor.MaxLimit,
                            IsDeadCellsCursor = cursor.IsDeadCellsCursor,
                            Color = cursor.Color,
                            MeasureSetup = meanMeasureSetup
                        });
                    }
                    meanDto.Ranges = new List<RangeDto>(cursors.Select(entity => new RangeDto
                    {
                        CreatedAt = entity.CreatedAt.ToString(),
                        CreatedBy = entity.CreatedBy,
                        IsDeadCellsCursor = entity.IsDeadCellsCursor,
                        LastModifiedAt = entity.LastModifiedAt.ToString(),
                        LastModifiedBy = entity.LastModifiedBy,
                        MaxLimit = entity.MaxLimit,
                        MinLimit = entity.MinLimit,
                        Name = entity.Name,
                        Subpopulation = entity.Subpopulation
                    }));
                }
                else
                {
                    meanDto.ErrorMessage = "Ranges are not equal";
                    return BadRequest(meanDto);
                }

                meanDto.Name = "Mean Result";
                meanDto.Color = "#FFFF2F34";
                meanMeasureSetup.VolumeCorrectionFactor = measureResults[0].MeasureSetup.VolumeCorrectionFactor;
                double[] meanData = new double[measureResults.First().MeasureResultDatas.First().DataBlock.Length];

                foreach (var measureResult in measureResults)
                {
                    var summedData = Task.Run(async () => await _measureResultDataCalculationService.SumMeasureResultDataAsync(measureResult)).Result;
                    for (int i = 0; i < summedData.Length; i++)
                    {
                        meanData[i] += summedData[i];
                    }
                }

                for (int i = 0; i < meanData.Length; i++)
                {
                    meanData[i] /= measureResults.Count;
                }

                var meanResult = new MeasureResult()
                {
                    MeasureSetup = meanMeasureSetup
                };
                var measureResultDataDtos = new List<MeasureResultDataDto>();
                for (var i = 0; i < meanMeasureSetup.Repeats; i++)
                {
                    MeasureResultDataDto meanMeasureResultDataDto = new MeasureResultDataDto();

                    double[] dataBlock = new double[meanData.Length];
                    for (int j = 0; j < meanData.Length; j++)
                    {
                        dataBlock[j] = meanData[j] / (double)meanMeasureSetup.Repeats;
                    }

                    ushort[] ushortArray = dataBlock.Select(d => (ushort)d).ToArray();
                    var internalDataBlock = string.Join(";", ushortArray);
                    meanMeasureResultDataDto.DataBlock = internalDataBlock;
                    meanMeasureResultDataDto.AboveCalibrationLimitCount = (measureResults.Select(mr => mr.MeasureResultDatas.Sum(mrd => mrd.AboveCalibrationLimitCount)).Sum() / measureResults.Count()) / meanMeasureSetup.Repeats;
                    meanMeasureResultDataDto.BelowCalibrationLimitCount = (measureResults.Select(mr => mr.MeasureResultDatas.Sum(mrd => mrd.BelowCalibrationLimitCount)).Sum() / measureResults.Count()) / meanMeasureSetup.Repeats;
                    meanMeasureResultDataDto.BelowMeasureLimitCount = (measureResults.Select(mr => mr.MeasureResultDatas.Sum(mrd => mrd.BelowMeasureLimtCount)).Sum() / measureResults.Count()) / meanMeasureSetup.Repeats;
                    meanMeasureResultDataDto.ConcentrationTooHigh = measureResults.Any(item => item.MeasureResultDatas.Any(data => data.ConcentrationTooHigh));

                    measureResultDataDtos.Add(meanMeasureResultDataDto);

                    meanResult.MeasureResultDatas.Add(new MeasureResultData
                    {
                        DataBlock = dataBlock,
                        AboveCalibrationLimitCount = (measureResults.Select(mr => mr.MeasureResultDatas.Sum(mrd => mrd.AboveCalibrationLimitCount)).Sum() / measureResults.Count()) / meanMeasureSetup.Repeats,
                        BelowCalibrationLimitCount = (measureResults.Select(mr => mr.MeasureResultDatas.Sum(mrd => mrd.BelowCalibrationLimitCount)).Sum() / measureResults.Count()) / meanMeasureSetup.Repeats,
                        BelowMeasureLimtCount = (measureResults.Select(mr => mr.MeasureResultDatas.Sum(mrd => mrd.BelowMeasureLimtCount)).Sum() / measureResults.Count()) / meanMeasureSetup.Repeats,
                        ConcentrationTooHigh = measureResults.Any(item => item.MeasureResultDatas.Any(data => data.ConcentrationTooHigh))
                });
                }

                meanDto.MeasureResultDataItems = measureResultDataDtos;

                await _measureResultDataCalculationService.UpdateMeasureResultDataAsync(meanResult, meanMeasureSetup);
                await _measureResultDataCalculationService.UpdateMeanDeviationsAsync(meanResult, measureResults.ToArray());

                List<MeasureResultItem> measureResultItems;
                measureResultItems = meanResult.MeasureResultItemsContainers.Select(x => x.Value).Select(y => y.MeasureResultItem).ToList();
                measureResultItems.AddRange(meanResult.MeasureResultItemsContainers.Select(x => x.Value).SelectMany(y => y.CursorItems));

                meanDto.MeasureResultCalculations = new List<MeasureResultCalculationDto>(measureResultItems.Select(
                    entity => new MeasureResultCalculationDto
                    {
                        AssociatedRange = entity.Cursor?.Name,
                        Deviation = entity.Deviation,
                        MeasureResultItemType = Enum.GetName(typeof(MeasureResultItemTypes),
                            entity.MeasureResultItemType),
                        ResultItemValue = entity.ResultItemValue
                    }));

                var parentResults = new List<MeasureResultDto>();
                foreach (var measureResult in measureResults)
                {
                    parentResults.Add(new MeasureResultDto()
                    {
                        MeasureResultDataItems = measureResult.MeasureResultDatas.Select(
                            entity =>
                                new MeasureResultDataDto
                                {
                                    AboveCalibrationLimitCount = entity.AboveCalibrationLimitCount,
                                    BelowCalibrationLimitCount = entity.BelowCalibrationLimitCount,
                                    BelowMeasureLimitCount = entity.BelowMeasureLimtCount,
                                    ConcentrationTooHigh = entity.ConcentrationTooHigh,
                                    DataBlock = entity.InternalDataBlock
                                }),
                        Color = measureResult.Color,
                        ToDiameter = meanMeasureSetup.ToDiameter
                    });
                }

                meanDto.ParentMeasureResults = parentResults;
                return Ok(meanDto);
            }

            meanDto.ErrorMessage = "No results found";
            return BadRequest(meanDto);
        }
    }
}
