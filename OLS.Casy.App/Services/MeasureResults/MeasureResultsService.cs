using OLS.Casy.App.Dto;
using OLS.Casy.App.Models;
using OLS.Casy.App.Services.RequestProvider;
using OLS.Casy.App.Services.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using OLS.Casy.App.Extensions;
using OLS.Casy.App.Models.Enums;
using MeasureResult = OLS.Casy.App.Models.MeasureResult;
using OLS.Casy.App.Exceptions;
using System.Net.Http;
using OLS.Casy.App.Services.Dialog;
using System.Diagnostics;

namespace OLS.Casy.App.Services.MeasureResults
{
    public class MeasureResultsService : IMeasureResultsService
    {
        private readonly IRequestProvider _requestProvider;
        private readonly ISettingsService _settingsService;
        private readonly List<MeasureResult> _selectedMeasureResults;
        private readonly IDialogService _dialogService;

        public MeasureResultsService(IRequestProvider requestProvider,
            ISettingsService settingsService, IDialogService dialogService)
        {
            _requestProvider = requestProvider;
            _settingsService = settingsService;
            _dialogService = dialogService;

            _selectedMeasureResults = new List<MeasureResult>();
        }

        public IEnumerable<MeasureResult> SelectedMeasureResults => _selectedMeasureResults.AsEnumerable();

        public async Task<IEnumerable<Experiment>> GetExperiments()
        {
            var casyUrl = _settingsService.CasyEndpointBase;
            if (!casyUrl.EndsWith("/"))
            {
                casyUrl += "/";
            }

            IEnumerable<string> experimentDtos = null;
            try
            { 
                experimentDtos = await _requestProvider.GetAsync<IEnumerable<string>>($"{casyUrl}measureresults", "casy", "c4sy");
            }
            catch (ServiceAuthenticationException sae)
            {
                await _dialogService.ShowAlertAsync("Authentification failed. Incorrect user name and/or password.", "Authentification failed", "Ok");
                return new List<Experiment>();
            }
            catch (HttpRequestExceptionEx hre)
            {
                await _dialogService.ShowAlertAsync("Unable to reach CASY service. Please check URL in settings.", "Service not reachable", "Ok");
                return new List<Experiment>();
            }
            catch (HttpRequestException re)
            {
                await _dialogService.ShowAlertAsync("Unable to reach CASY service. Please check URL in settings.", "Service not reachable", "Ok");
                return new List<Experiment>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MeasureResultService error: {ex}");
                return new List<Experiment>();
            }

            List<Experiment> experiments = new List<Experiment>();
            foreach (var experimentDto in experimentDtos)
            {
                var experimentName = experimentDto;
                if (experimentName == null)
                {
                    experimentName = "(No Experiment)";
                }
                experiments.Add(new Experiment { Name = experimentName });
            }

            return experiments;
        }

        public async Task<IEnumerable<Group>> GetGroups(string experiment)
        {
            var casyUrl = _settingsService.CasyEndpointBase;
            if (!casyUrl.EndsWith("/"))
            {
                casyUrl += "/";
            }

            IEnumerable<string> groupDtos = new List<string>();
            try
            {
                groupDtos = await _requestProvider.GetAsync<IEnumerable<string>>($"{casyUrl}measureresults/{Uri.EscapeUriString(experiment)}", "casy", "c4sy");
            }
            catch (ServiceAuthenticationException sae)
            {
                await _dialogService.ShowAlertAsync("Authentification failed. Incorrect user name and/or password.", "Authentification failed", "Ok");
                return new List<Group>();
            }
            catch (HttpRequestExceptionEx hre)
            {
                await _dialogService.ShowAlertAsync("Unable to reach CASY service. Please check URL in settings.", "Service not reachable", "Ok");
                return new List<Group>();
            }
            catch (HttpRequestException re)
            {
                await _dialogService.ShowAlertAsync("Unable to reach CASY service. Please check URL in settings.", "Service not reachable", "Ok");
                return new List<Group>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MeasureResultService error: {ex}");
                return new List<Group>();
            }

            List<Group> groups = new List<Group>();
            foreach (var groupDto in groupDtos)
            {
                var groupName = groupDto;
                if (groupName == null)
                {
                    groupName = "(No Group)";
                }
                groups.Add(new Group { Name = groupName });
            }

            return groups;
        }

        public async Task<IEnumerable<MeasureResult>> GetMeasureResults(string experiment, string group)
        {
            var casyUrl = _settingsService.CasyEndpointBase;
            if (!casyUrl.EndsWith("/"))
            {
                casyUrl += "/";
            }

            IEnumerable<MeasureResulltInfoDto> measureResultInfoDtos = null;
            try
            { 
                measureResultInfoDtos = await _requestProvider.GetAsync<IEnumerable<MeasureResulltInfoDto>>($"{casyUrl}measureresults/{Uri.EscapeUriString(experiment)}/{Uri.EscapeUriString(group)}", "casy", "c4sy");
            }
            catch (ServiceAuthenticationException sae)
            {
                await _dialogService.ShowAlertAsync("Authentification failed. Incorrect user name and/or password.", "Authentification failed", "Ok");
                return new List<MeasureResult>();
            }
            catch (HttpRequestExceptionEx hre)
            {
                await _dialogService.ShowAlertAsync("Unable to reach CASY service. Please check URL in settings.", "Service not reachable", "Ok");
                return new List<MeasureResult>();
            }
            catch (HttpRequestException re)
            {
                await _dialogService.ShowAlertAsync("Unable to reach CASY service. Please check URL in settings.", "Service not reachable", "Ok");
                return new List<MeasureResult>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MeasureResultService error: {ex}");
                return new List<MeasureResult>();
            }

            List<MeasureResult> measureResults = new List<MeasureResult>();
            foreach (var measureResultInfoDto in measureResultInfoDtos)
            {
                measureResults.Add(new MeasureResult
                {
                    Id = measureResultInfoDto.Id,
                    Name = measureResultInfoDto.Name,
                    Experiment = experiment,
                    Group = group
                });
            }

            return measureResults;
        }

        public async Task AddSelectedMeasureResult(LastSelected lastSelected)
        {
            var casyUrl = _settingsService.CasyEndpointBase;
            if (!casyUrl.EndsWith("/"))
            {
                casyUrl += "/";
            }

            MeasureResulltInfoDto measureResultInfoDto = null;
            try
            {
                measureResultInfoDto = await _requestProvider.GetAsync<MeasureResulltInfoDto>($"{casyUrl}measureresults/byid/{lastSelected.Id}", "casy", "c4sy");
            }
            catch (ServiceAuthenticationException sae)
            {
                await _dialogService.ShowAlertAsync("Authentification failed. Incorrect user name and/or password.", "Authentification failed", "Ok");
                return;
            }
            catch (HttpRequestExceptionEx hre)
            {
                await _dialogService.ShowAlertAsync("Unable to reach CASY service. Please check URL in settings.", "Service not reachable", "Ok");
                return;
            }
            catch (HttpRequestException re)
            {
                await _dialogService.ShowAlertAsync("Unable to reach CASY service. Please check URL in settings.", "Service not reachable", "Ok");
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MeasureResultService error: {ex}");
                return;
            }

            await AddSelectedMeasureResult(new MeasureResult
            {
                Id = measureResultInfoDto.Id,
                Name = measureResultInfoDto.Name,
                Experiment = measureResultInfoDto.Experiment,
                Group = measureResultInfoDto.Group
            });
        }

        public async Task AddSelectedMeasureResult(MeasureResult measureResult)
        {
            var casyUrl = _settingsService.CasyEndpointBase;
            if (!casyUrl.EndsWith("/"))
            {
                casyUrl += "/";
            }

            MeasureResultDto measureResultDto = null;
            try
            { 
                measureResultDto = await _requestProvider.GetAsync<MeasureResultDto>($"{casyUrl}measureresults/{Uri.EscapeUriString(measureResult.Experiment)}/{Uri.EscapeUriString(measureResult.Group)}/{Uri.EscapeUriString(measureResult.Name)}", "casy", "c4sy");
            }
            catch (ServiceAuthenticationException sae)
            {
                await _dialogService.ShowAlertAsync("Authentification failed. Incorrect user name and/or password.", "Authentification failed", "Ok");
                return;
            }
            catch (HttpRequestExceptionEx hre)
            {
                await _dialogService.ShowAlertAsync("Unable to reach CASY service. Please check URL in settings.", "Service not reachable", "Ok");
                return;
            }
            catch (HttpRequestException re)
            {
                await _dialogService.ShowAlertAsync("Unable to reach CASY service. Please check URL in settings.", "Service not reachable", "Ok");
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MeasureResultService error: {ex}");
                return;
            }

            measureResult.Comment = measureResultDto.Comment;
            measureResult.SerialNumber = measureResultDto.SerialNumber;
            measureResult.AggregationCalculationMode = (AggregationCalculationModes) Enum.Parse(typeof(AggregationCalculationModes),
                measureResultDto.AggregationCalculationMode);
            measureResult.ManualAggrgationCalculationFactor = measureResultDto.ManualAggrgationCalculationFactor;
            measureResult.AuditTrailItems = new List<AuditTrailItem>(measureResultDto.AuditTrailItems.Select(entity =>
                new AuditTrailItem
                {
                    Action = entity.Action,
                    ComputerName = entity.ComputerName,
                    DateChanged = DateTimeOffsetExtensions.ParseAny(entity.DateChanged),
                    EntityName = entity.EntityName,
                    NewValue = entity.NewValue,
                    OldValue = entity.OldValue,
                    PrimaryKeyValue = entity.PrimaryKeyValue,
                    PropertyName = entity.PropertyName,
                    SoftwareVersion = entity.SoftwareVersion,
                    UserChanged = entity.UserChanged
                }).ToList());
            measureResult.CapillarySize = measureResultDto.CapillarySize;
            measureResult.ChannelCount = measureResultDto.ChannelCount;
            measureResult.CreatedAt = DateTimeOffsetExtensions.ParseAny(measureResultDto.CreatedAt);
            measureResult.CreatedBy = measureResultDto.CreatedBy;
            measureResult.IsDeviationControlEnabled = measureResultDto.IsDeviationControlEnabled;
            measureResult.DeviationConrolItems = new List<DeviationControlItem>(
                measureResultDto.DeviationConrolItems.Select(entity => new DeviationControlItem
                {
                    MaxLimit = entity.MaxLimit,
                    MinLimit = entity.MinLimit,
                    MeasureResultItemType = (MeasureResultItemTypes) Enum.Parse(typeof(MeasureResultItemTypes), entity.MeasureResultItemType)
                }));
            measureResult.DilutionCasyTonVolume = measureResultDto.DilutionCasyTonVolume;
            measureResult.DilutionFactor = measureResultDto.DilutionFactor;
            measureResult.DilutionSampleVolume = measureResultDto.DilutionSampleVolume;
            measureResult.FromDiameter = measureResultDto.FromDiameter;
            measureResult.ToDiameter = measureResultDto.ToDiameter;
            measureResult.HasSubpopulations = measureResultDto.HasSubpopulations;
            measureResult.IsCfr = measureResultDto.IsCfr;
            measureResult.IsSmoothing = measureResultDto.IsSmoothing;
            measureResult.SmoothingFactor = measureResultDto.SmoothingFactor;
            measureResult.LastModifiedAt = DateTimeOffsetExtensions.ParseAny(measureResultDto.LastModifiedAt);
            measureResult.LastModifiedBy = measureResultDto.LastModifiedBy;
            measureResult.MeasuredAt = measureResultDto.MeasuredAt;
            measureResult.MeasuredAtTimeZone = measureResultDto.MeasuredAtTimeZone;
            measureResult.MeasureMode = (MeasureModes) Enum.Parse(typeof(MeasureModes), measureResultDto.MeasureMode);
            measureResult.MeasureResultDataItems = new List<MeasureResultData>(
                measureResultDto.MeasureResultDataItems.Select(entity => new MeasureResultData
                {
                    AboveCalibrationLimitCount = entity.AboveCalibrationLimitCount,
                    BelowCalibrationLimitCount = entity.BelowCalibrationLimitCount,
                    BelowMeasureLimitCount = entity.BelowCalibrationLimitCount,
                    ConcentrationTooHigh = entity.ConcentrationTooHigh,
                    InternalDataBlock = entity.DataBlock
                }));
            measureResult.MeasureResultGuid = measureResultDto.MeasureResultGuid;
            measureResult.Origin = measureResultDto.Origin;
            measureResult.Ranges = new List<Range>(measureResultDto.Ranges.Select(entity => new Range
            {
                CreatedAt = DateTimeOffsetExtensions.ParseAny(entity.CreatedAt),
                CreatedBy = entity.CreatedBy,
                IsDeadCellsCursor = entity.IsDeadCellsCursor,
                LastModifiedAt = DateTimeOffsetExtensions.ParseAny(entity.LastModifiedAt),
                LastModifiedBy = entity.LastModifiedBy,
                MaxLimit = entity.MaxLimit,
                MinLimit = entity.MinLimit,
                Name = entity.Name == "Cursor_DeadCells_Name" ? "Dead" : (entity.Name == "Cursor_VitalCells_Name" ? "Viable" : entity.Name),
                Subpopulation = entity.Subpopulation
            }));
            measureResult.Repeats = measureResultDto.Repeats;
            measureResult.ScalingMaxRange = measureResultDto.ScalingMaxRange;
            measureResult.ScalingMode = (ScalingModes) Enum.Parse(typeof(ScalingModes), measureResultDto.ScalingMode);
            measureResult.TemplateName = measureResultDto.TemplateName;
            measureResult.UnitMode = (UnitModes) Enum.Parse(typeof(UnitModes), measureResultDto.UnitMode);
            measureResult.Volume = (Volumes) Enum.Parse(typeof(Volumes), measureResultDto.Volume);
            measureResult.VolumeCorrectionFactor = measureResultDto.VolumeCorrectionFactor;
            measureResult.MeasureResultCalculations = new List<MeasureResultCalculation>(measureResultDto.MeasureResultCalculations.Select(
                entity => new MeasureResultCalculation
                {
                    AssociatedRange = entity.AssociatedRange == "Cursor_DeadCells_Name" ? "Dead" : (entity.AssociatedRange == "Cursor_VitalCells_Name" ? "Viable" : entity.AssociatedRange),
                    Deviation = entity.Deviation,
                    MeasureResultItemType = (MeasureResultItemTypes) Enum.Parse(typeof(MeasureResultItemTypes), entity.MeasureResultItemType),
                    ResultItemValue = entity.ResultItemValue
                }));
            measureResult.LastWeeklyClean = DateTimeOffsetExtensions.ParseAny(measureResultDto.LastWeeklyClean);
            measureResult.Color = measureResultDto.Color;

            _selectedMeasureResults.Add(measureResult);

            var lastSelected = _settingsService.GetValueOrDefault("LastSelected", string.Empty);
            var lastSelectedList = lastSelected.Split(';').ToList();

            lastSelectedList.Add($"{measureResult.Id.ToString()}|{measureResult.Name}");

            if(lastSelectedList.Count > 14)
            {
                lastSelectedList.RemoveAt(0);
            }
            await _settingsService.AddOrUpdateValue("LastSelected", string.Join(";", lastSelectedList));

            OnSelectedMeasureResultsChanged();
        }

        public async Task RemoveSelectedMeasureResult(MeasureResult measureResult)
        {
            _selectedMeasureResults.Remove(measureResult);

            OnSelectedMeasureResultsChanged();
        }

        public async Task<IEnumerable<MeasureResult>> GetOverlay()
        {
            var result = new List<MeasureResult>();
            if (!SelectedMeasureResults.Any()) return result;

            var overlayDto = new OverlayDto()
            {
                MeasureResultIds = SelectedMeasureResults.Select(x => x.Id).ToArray()
            };

            var casyUrl = _settingsService.CasyEndpointBase;
            if (!casyUrl.EndsWith("/"))
            {
                casyUrl += "/";
            }

            try
            { 
                overlayDto = await _requestProvider.PostAsync<OverlayDto, OverlayDto>($"{casyUrl}measureresults/overlay", overlayDto , "casy", "c4sy");
            }
            catch (ServiceAuthenticationException sae)
            {
                await _dialogService.ShowAlertAsync("Authentification failed. Incorrect user name and/or password.", "Authentification failed", "Ok");
                return result;
            }
            catch (HttpRequestExceptionEx hre)
            {
                await _dialogService.ShowAlertAsync("Unable to reach CASY service. Please check URL in settings.", "Service not reachable", "Ok");
                return result;
            }
            catch (HttpRequestException re)
            {
                await _dialogService.ShowAlertAsync("Unable to reach CASY service. Please check URL in settings.", "Service not reachable", "Ok");
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MeasureResultService error: {ex}");
                return result;
            }

            if (!string.IsNullOrEmpty(overlayDto.ErrorMessage))
            {
                return result;
            }
            foreach (var measureResultDto in overlayDto.MeasureResults)
            {
                var measureResult = new MeasureResult()
                {
                    MeasureResultDataItems = new List<MeasureResultData>(
                        measureResultDto.MeasureResultDataItems.Select(entity => new MeasureResultData
                        {
                            AboveCalibrationLimitCount = entity.AboveCalibrationLimitCount,
                            BelowCalibrationLimitCount = entity.BelowCalibrationLimitCount,
                            BelowMeasureLimitCount = entity.BelowCalibrationLimitCount,
                            ConcentrationTooHigh = entity.ConcentrationTooHigh,
                            InternalDataBlock = entity.DataBlock
                        })),
                    MeasureResultCalculations = new List<MeasureResultCalculation>(
                        measureResultDto.MeasureResultCalculations.Select(
                            entity => new MeasureResultCalculation
                            {
                                AssociatedRange = entity.AssociatedRange == "Cursor_DeadCells_Name" ? "Dead" : (entity.AssociatedRange == "Cursor_VitalCells_Name" ? "Viable" : entity.AssociatedRange),
                                Deviation = entity.Deviation,
                                MeasureResultItemType =
                                    (MeasureResultItemTypes) Enum.Parse(typeof(MeasureResultItemTypes),
                                        entity.MeasureResultItemType),
                                ResultItemValue = entity.ResultItemValue
                            })),
                    Ranges = new List<Range>(overlayDto.Ranges.Select(entity => new Range
                    {
                        CreatedAt = DateTimeOffsetExtensions.ParseAny(entity.CreatedAt),
                        CreatedBy = entity.CreatedBy,
                        IsDeadCellsCursor = entity.IsDeadCellsCursor,
                        LastModifiedAt = DateTimeOffsetExtensions.ParseAny(entity.LastModifiedAt),
                        LastModifiedBy = entity.LastModifiedBy,
                        MaxLimit = entity.MaxLimit,
                        MinLimit = entity.MinLimit,
                        Name = entity.Name == "Cursor_DeadCells_Name" ? "Dead" : (entity.Name == "Cursor_VitalCells_Name" ? "Viable" : entity.Name),
                        Subpopulation = entity.Subpopulation
                    })),
                    Color = measureResultDto.Color,
                    Comment = measureResultDto.Comment,
                    Name = measureResultDto.Name,
                    MeasureMode = (MeasureModes)Enum.Parse(typeof(MeasureModes), overlayDto.MeasureMode),
                    ToDiameter = overlayDto.ToDiameter,
                    HasSubpopulations = overlayDto.HasSubpopulations
                };

                result.Add(measureResult);
            }

            return result;
        }

        public async Task<Tuple<MeasureResult, IEnumerable<MeasureResult>>> GetMean()
        {
            var result = new Tuple<MeasureResult, IEnumerable<MeasureResult>>(null, null);
            if (!SelectedMeasureResults.Any()) return result;

            var meanDto = new MeanDto()
            {
                MeasureResultIds = SelectedMeasureResults.Select(x => x.Id).ToArray()
            };

            var casyUrl = _settingsService.CasyEndpointBase;
            if (!casyUrl.EndsWith("/"))
            {
                casyUrl += "/";
            }

            try
            {
                meanDto = await _requestProvider.PostAsync<MeanDto, MeanDto>($"{casyUrl}measureresults/mean", meanDto, "casy", "c4sy");
            }
            catch (ServiceAuthenticationException sae)
            {
                await _dialogService.ShowAlertAsync("Authentification failed. Incorrect user name and/or password.", "Authentification failed", "Ok");
                return result;
            }
            catch (HttpRequestExceptionEx hre)
            {
                await _dialogService.ShowAlertAsync("Unable to reach CASY service. Please check URL in settings.", "Service not reachable", "Ok");
                return result;
            }
            catch (HttpRequestException re)
            {
                await _dialogService.ShowAlertAsync("Unable to reach CASY service. Please check URL in settings.", "Service not reachable", "Ok");
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MeasureResultService error: {ex}");
                return result;
            }

            if (!string.IsNullOrEmpty(meanDto.ErrorMessage))
            {
                return result;
            }

            var meanResult = new MeasureResult()
            {
                MeasureResultDataItems = new List<MeasureResultData>(
                    meanDto.MeasureResultDataItems.Select(entity => new MeasureResultData
                        {
                            AboveCalibrationLimitCount = entity.AboveCalibrationLimitCount,
                            BelowCalibrationLimitCount = entity.BelowCalibrationLimitCount,
                            BelowMeasureLimitCount = entity.BelowCalibrationLimitCount,
                            ConcentrationTooHigh = entity.ConcentrationTooHigh,
                            InternalDataBlock = entity.DataBlock
                        })),
                MeasureResultCalculations = new List<MeasureResultCalculation>(
                    meanDto.MeasureResultCalculations.Select(
                            entity => new MeasureResultCalculation
                            {
                                AssociatedRange = entity.AssociatedRange == "Cursor_DeadCells_Name" ? "Dead" : (entity.AssociatedRange == "Cursor_VitalCells_Name" ? "Viable" : entity.AssociatedRange),
                                Deviation = entity.Deviation,
                                MeasureResultItemType =
                                    (MeasureResultItemTypes)Enum.Parse(typeof(MeasureResultItemTypes),
                                        entity.MeasureResultItemType),
                                ResultItemValue = entity.ResultItemValue
                            })),
                Ranges = new List<Range>(meanDto.Ranges.Select(entity => new Range
                {
                    CreatedAt = DateTimeOffsetExtensions.ParseAny(entity.CreatedAt),
                    CreatedBy = entity.CreatedBy,
                    IsDeadCellsCursor = entity.IsDeadCellsCursor,
                    LastModifiedAt = DateTimeOffsetExtensions.ParseAny(entity.LastModifiedAt),
                    LastModifiedBy = entity.LastModifiedBy,
                    MaxLimit = entity.MaxLimit,
                    MinLimit = entity.MinLimit,
                    Name = entity.Name == "Cursor_DeadCells_Name" ? "Dead" : (entity.Name == "Cursor_VitalCells_Name" ? "Viable" : entity.Name),
                    Subpopulation = entity.Subpopulation
                })),
                Color = meanDto.Color,
                Comment = meanDto.Comment,
                Name = meanDto.Name,
                MeasureMode = (MeasureModes)Enum.Parse(typeof(MeasureModes), meanDto.MeasureMode),
                ToDiameter = meanDto.ToDiameter,
                HasSubpopulations = meanDto.HasSubpopulations
            };

            var parentResults = new List<MeasureResult>();
            foreach (var parentDto in meanDto.ParentMeasureResults)
            {
                parentResults.Add(new MeasureResult
                {
                    MeasureResultDataItems = new List<MeasureResultData>(
                        parentDto.MeasureResultDataItems.Select(entity => new MeasureResultData
                        {
                            AboveCalibrationLimitCount = entity.AboveCalibrationLimitCount,
                            BelowCalibrationLimitCount = entity.BelowCalibrationLimitCount,
                            BelowMeasureLimitCount = entity.BelowCalibrationLimitCount,
                            ConcentrationTooHigh = entity.ConcentrationTooHigh,
                            InternalDataBlock = entity.DataBlock
                        })),
                    Color = parentDto.Color,
                    ToDiameter = parentDto.ToDiameter
                });
            }

            return new Tuple<MeasureResult, IEnumerable<MeasureResult>>(meanResult, parentResults);
        }

        public event EventHandler SelectedMeasureResultsChanged;

        private void OnSelectedMeasureResultsChanged()
        {
            if (SelectedMeasureResultsChanged == null) return;
            foreach (var @delegate in SelectedMeasureResultsChanged.GetInvocationList())
            {
                var receiver = (EventHandler)@delegate;
                receiver.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
