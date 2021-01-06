using OLS.Casy.Controller.Api;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Models;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OLS.Casy.Models.Enums;
using System.IO;
using System.Threading;
using OLS.Casy.Core.Events;

namespace OLS.Casy.IO.ImportExport.Raw
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IRawDataExportProvider))]
    public class RawDataExportProvider : IRawDataExportProvider
    {
        private readonly IFileSystemStorageService _fileSystemStorageService;
        private readonly IDatabaseStorageService _databaseStorageService;
        private readonly IEnvironmentService _environmentService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IMeasureResultDataCalculationService _measureResultDataCalculationService;
        private readonly IEventAggregatorProvider _eventAggregatorProvider;

        [ImportingConstructor]
        public RawDataExportProvider(IFileSystemStorageService fileSystemStorageService,
            IDatabaseStorageService databaseStorageService,
            IEnvironmentService environmentService,
            IAuthenticationService authenticationService,
            IEventAggregatorProvider eventAggregatorProvider,
            IMeasureResultDataCalculationService measureResultDataCalculationService)
        {
            this._fileSystemStorageService = fileSystemStorageService;
            this._databaseStorageService = databaseStorageService;
            this._environmentService = environmentService;
            this._authenticationService = authenticationService;
            this._measureResultDataCalculationService = measureResultDataCalculationService;
            _eventAggregatorProvider = eventAggregatorProvider;
        }

        public async Task ExportMeasureResultsAsync(IEnumerable<MeasureResult> measureResults, string filePath)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("CASY® 2.5 Raw Data Export File");
            sb.AppendLine(string.Format("Software Version:\t{0}", (string) _environmentService.GetEnvironmentInfo("SoftwareVersion")));
            sb.AppendLine();

            for (int i = 0; i < measureResults.Count(); i++)
            {
                var measureResult = measureResults.ElementAt(i);
                measureResult = _databaseStorageService.LoadDisplayData(measureResult);
                measureResult = _databaseStorageService.LoadExportData(measureResult);

                await _measureResultDataCalculationService.UpdateMeasureResultDataAsync(measureResult, null);

                sb.AppendLine(string.Format("Measurement Name\t{0}", measureResult.Name));
                sb.AppendLine(string.Format("Serial Number\t{0}", measureResult.SerialNumber));
                sb.AppendLine(string.Format("Template\t{0}", measureResult.MeasureSetup.Name));
                sb.AppendLine(string.Format("Calibration\tK000_{0}.{1}", measureResult.MeasureSetup.CapillarySize.ToString().PadLeft(3, '0'), measureResult.MeasureSetup.ToDiameter.ToString().PadLeft(3, '0')));

                var user = this._authenticationService.GetUserByName(measureResult.CreatedBy);
                if (user == null)
                {
                    sb.AppendLine(string.Format("User (Creation)\tUnknown ({0} [Unknown])", measureResult.CreatedBy));
                }
                else
                {
                    sb.AppendLine(string.Format("User (Creation)\t{0} {1} ({2} [{3}])", user.FirstName, user.LastName, user.Identity.Name, user.UserRole.Name));
                }

                var dateTimeString = _environmentService.GetDateTimeString(measureResult.MeasureSetup.CreatedAt.DateTime);
                sb.AppendLine(string.Format("Date (Creation)\t{0}", dateTimeString));

                user = this._authenticationService.GetUserByName(measureResult.LastModifiedBy);
                if (user == null)
                {
                    sb.AppendLine(string.Format("User (Last Modification)\tUnknown ({0} [Unknown])", measureResult.LastModifiedBy));
                }
                else
                {
                    sb.AppendLine(string.Format("User (Last Modification)\t{0} {1} ({2} [{3}])", user.FirstName, user.LastName, user.Identity.Name, user.UserRole.Name));
                }

                dateTimeString = _environmentService.GetDateTimeString(measureResult.MeasureSetup.LastModifiedAt.DateTime);
                sb.AppendLine(string.Format("Date (Last Modification)\t{0}", dateTimeString));

                sb.AppendLine(string.Format("Comment\t{0}", measureResult.Comment));
                sb.AppendLine(string.Format("Dilution\t{0}", measureResult.MeasureSetup.DilutionFactor.ToString("0.000")));
                sb.AppendLine(string.Format("Cycles\t{0}", measureResult.MeasureSetup.Repeats));
                sb.AppendLine(string.Format("Sample Volume (µl)\t{0}", ((int) measureResult.MeasureSetup.Volume).ToString()));
                sb.AppendLine(string.Format("Volume Correction\t{0}", measureResult.MeasureSetup.VolumeCorrectionFactor.ToString("0.000")));
                sb.AppendLine(string.Format("Smoothing\t{0}", measureResult.MeasureSetup.SmoothingFactor));
                sb.AppendLine(string.Format("Mode of Aggregation Correction\t{0}", measureResult.MeasureSetup.AggregationCalculationMode.ToString()));
                sb.AppendLine(string.Format("Manual Aggregation Correction (fl)\t{0}", measureResult.MeasureSetup.ManualAggregationCalculationFactor.ToString("0.0000")));
                sb.AppendLine(string.Format("Capillary Diameter (µm)\t{0}", measureResult.MeasureSetup.CapillarySize.ToString()));
                sb.AppendLine(string.Format("X-Axis Scale (µm)\t{0}", measureResult.MeasureSetup.ToDiameter.ToString()));

                foreach(var cursor in measureResult.MeasureSetup.Cursors)
                {
                    sb.AppendLine(string.Format("Min Limit Range '{0}'\t{1}", cursor.Name, cursor.MinLimit.ToString("0.00"))); //measureResult.MeasureSetup.SmoothedDiameters[cursor.MinLimit].ToString("0.00")));
                    sb.AppendLine(string.Format("Max Limit Range '{0}'\t{1}", cursor.Name, cursor.MaxLimit.ToString("0.00"))); //measureResult.MeasureSetup.SmoothedDiameters[cursor.MaxLimit].ToString("0.00")));
                }

                sb.AppendLine(string.Format("Y-Axis Scale\t{0}", measureResult.MeasureSetup.ScalingMode == Models.Enums.ScalingModes.Auto ? "Auto" : measureResult.MeasureSetup.ScalingMaxRange.ToString()));

                var summedCounts = await _measureResultDataCalculationService.SumMeasureResultDataAsync(measureResult);
                sb.Append("Size Channel\tCounts (Sum)");
                var numRepeats = measureResult.MeasureSetup.Repeats;
                var dataAndRepeatsEqual = numRepeats == measureResult.MeasureResultDatas.Count;
                if(numRepeats > 1 && dataAndRepeatsEqual)
                {
                    for(int j = 0; j < numRepeats; j++)
                    {
                        sb.Append(string.Format("\tCounts (Cycle {0})", j + 1));
                    }
                }
                sb.AppendLine();

                for(int channel = 0; channel < measureResult.MeasureSetup.ChannelCount; channel++)
                {
                    sb.Append(string.Format("{0}\t{1}", measureResult.MeasureSetup.SmoothedDiameters[channel].ToString("#0.000"), summedCounts[channel]));
                    if (numRepeats > 1 && dataAndRepeatsEqual)
                    {
                        for (int j = 0; j < numRepeats; j++)
                        {
                            sb.Append(string.Format("\t{0}", measureResult.MeasureResultDatas[j].DataBlock[channel]));
                        }
                    }
                    sb.AppendLine();
                }
                sb.Append(string.Format("Sum\t{0}", summedCounts.Sum()));
                if (numRepeats > 1 && dataAndRepeatsEqual)
                {
                    for (int j = 0; j < numRepeats; j++)
                    {
                        sb.Append(string.Format("\t{0}", measureResult.MeasureResultDatas[j].DataBlock.Sum()));
                    }
                }
                sb.AppendLine();

                var deviationItem = measureResult.MeasureResultItemsContainers[MeasureResultItemTypes.Deviation].MeasureResultItem;
                sb.AppendLine(string.Format("Standard Deviation (%)\t{0}", deviationItem == null ? "Unknown" : deviationItem.ResultItemValue.ToString("0.0")));
                var concentrationItem = measureResult.MeasureResultItemsContainers[MeasureResultItemTypes.Concentration].MeasureResultItem;
                sb.AppendLine(string.Format("Concentration Range\t{0}", concentrationItem == null ? "Unknown" : concentrationItem.ResultItemValue == 1d ? "TOO HIGH" : "OK"));

                var countsAboveItem = measureResult.MeasureResultItemsContainers[MeasureResultItemTypes.CountsAboveDiameter].MeasureResultItem;
                sb.AppendLine(string.Format("Counts > {0} µm\t{1}", measureResult.MeasureSetup.ToDiameter, countsAboveItem == null ? "Unknown" : countsAboveItem.ResultItemValue.ToString(countsAboveItem.ValueFormat)));

                if (measureResult.MeasureSetup.MeasureMode == MeasureModes.Viability)
                {
                    MeasureResultItem measureResultItem;
                    var viableCursor = measureResult.MeasureSetup.Cursors.FirstOrDefault(x => !x.IsDeadCellsCursor);

                    if(viableCursor == null) continue;

                    measureResultItem = measureResult.MeasureResultItemsContainers[MeasureResultItemTypes.AggregationFactor].CursorItems.FirstOrDefault(x => x.Cursor == viableCursor);
                    var measureResultValue = measureResultItem == null ? "Unknown" : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                    sb.AppendLine(string.Format("Aggregation Factor\t{0}", measureResultValue));

                    measureResultItem = measureResult.MeasureResultItemsContainers[MeasureResultItemTypes.DebrisCount].MeasureResultItem;
                    measureResultValue = measureResultItem == null ? "Unknown" : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                    sb.AppendLine(string.Format("Debris/ml\t{0}", measureResultValue));

                    measureResultItem = measureResult.MeasureResultItemsContainers[MeasureResultItemTypes.ViableCellsPerMl].CursorItems.FirstOrDefault(x => x.Cursor == viableCursor);
                    measureResultValue = measureResultItem == null ? "Unknown" : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                    sb.AppendLine(string.Format("Viable Cells/ml\t{0}", measureResultValue));

                    measureResultItem = measureResult.MeasureResultItemsContainers[MeasureResultItemTypes.Viability].CursorItems.FirstOrDefault(x => x.Cursor == viableCursor);
                    measureResultValue = measureResultItem == null ? "Unknown" : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                    sb.AppendLine(string.Format("% Viability\t{0}", measureResultValue));

                    measureResultItem = measureResult.MeasureResultItemsContainers[MeasureResultItemTypes.VolumePerMl].CursorItems.FirstOrDefault(x => x.Cursor == viableCursor);
                    measureResultValue = measureResultItem == null ? "Unknown" : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                    sb.AppendLine(string.Format("Volume/ml\t{0}", measureResultValue));

                    measureResultItem = measureResult.MeasureResultItemsContainers[MeasureResultItemTypes.MeanDiameter].CursorItems.FirstOrDefault(x => x.Cursor == viableCursor);
                    measureResultValue = measureResultItem == null ? "Unknown" : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                    sb.AppendLine(string.Format("Mean Diameter (µm)\t{0}", measureResultValue));

                    measureResultItem = measureResult.MeasureResultItemsContainers[MeasureResultItemTypes.PeakDiameter].CursorItems.FirstOrDefault(x => x.Cursor == viableCursor);
                    measureResultValue = measureResultItem == null ? "Unknown" : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                    sb.AppendLine(string.Format("Peak Diameter (µm)\t{0}", measureResultValue));

                    measureResultItem = measureResult.MeasureResultItemsContainers[MeasureResultItemTypes.MeanVolume].CursorItems.FirstOrDefault(x => x.Cursor == viableCursor);
                    measureResultValue = measureResultItem == null ? "Unknown" : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                    sb.AppendLine(string.Format("Mean Volume (fl)\t{0}", measureResultValue));

                    measureResultItem = measureResult.MeasureResultItemsContainers[MeasureResultItemTypes.PeakVolume].CursorItems.FirstOrDefault(x => x.Cursor == viableCursor);
                    measureResultValue = measureResultItem == null ? "Unknown" : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                    sb.AppendLine(string.Format("Peak Volume (fl)\t{0}", measureResultValue));
                }
                else
                {
                    foreach (var cursor in measureResult.MeasureSetup.Cursors)
                    {
                        MeasureResultItem measureResultItem;
                        measureResultItem = measureResult.MeasureResultItemsContainers[MeasureResultItemTypes.CountsPerMl].CursorItems.FirstOrDefault(x => x.Cursor == cursor);
                        var measureResultValue = measureResultItem == null ? "Unknown" : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                        sb.AppendLine(string.Format("Cells/ml ({0})\t{1}", cursor.Name, measureResultValue));

                        measureResultItem = measureResult.MeasureResultItemsContainers[MeasureResultItemTypes.CountsPercentage].CursorItems.FirstOrDefault(x => x.Cursor == cursor);
                        measureResultValue = measureResultItem == null ? "Unknown" : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                        sb.AppendLine(string.Format("% Cells ({0})\t{1}", cursor.Name, measureResultValue));

                        measureResultItem = measureResult.MeasureResultItemsContainers[MeasureResultItemTypes.VolumePerMl].CursorItems.FirstOrDefault(x => x.Cursor == cursor);
                        measureResultValue = measureResultItem == null ? "Unknown" : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                        sb.AppendLine(string.Format("Volume/ml ({0})\t{1}", cursor.Name, measureResultValue));

                        measureResultItem = measureResult.MeasureResultItemsContainers[MeasureResultItemTypes.MeanDiameter].CursorItems.FirstOrDefault(x => x.Cursor == cursor);
                        measureResultValue = measureResultItem == null ? "Unknown" : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                        sb.AppendLine(string.Format("Mean Diameter (µm) ({0})\t{1}", cursor.Name, measureResultValue));

                        measureResultItem = measureResult.MeasureResultItemsContainers[MeasureResultItemTypes.PeakDiameter].CursorItems.FirstOrDefault(x => x.Cursor == cursor);
                        measureResultValue = measureResultItem == null ? "Unknown" : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                        sb.AppendLine(string.Format("Peak Diameter (µm) ({0})\t{1}", cursor.Name, measureResultValue));

                        measureResultItem = measureResult.MeasureResultItemsContainers[MeasureResultItemTypes.MeanVolume].CursorItems.FirstOrDefault(x => x.Cursor == cursor);
                        measureResultValue = measureResultItem == null ? "Unknown" : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                        sb.AppendLine(string.Format("Mean Volume (fl) ({0})\t{1}", cursor.Name, measureResultValue));

                        measureResultItem = measureResult.MeasureResultItemsContainers[MeasureResultItemTypes.PeakVolume].CursorItems.FirstOrDefault(x => x.Cursor == cursor);
                        measureResultValue = measureResultItem == null ? "Unknown" : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                        sb.AppendLine(string.Format("Peak Volume (fl) ({0})\t{1}", cursor.Name, measureResultValue));
                    }
                }
                sb.AppendLine();
            }
            try
            {
                await this._fileSystemStorageService.CreateFileAsync(filePath, Encoding.UTF8.GetBytes(sb.ToString()));
            }
            catch (IOException ioException)
            {
                await Task.Factory.StartNew(() =>
                {
                    var awaiter = new ManualResetEvent(false);

                    var messageBoxWrapper = new ShowMessageBoxDialogWrapper
                    {
                        Awaiter = awaiter,
                        Title = "Error while export",
                        Message = $"The system wasn't able to create the selected file '{filePath}'"
                    };

                    _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>().Publish(messageBoxWrapper);

                    awaiter.WaitOne();

                });
            }
            
        }
    }
}
