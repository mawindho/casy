using OLS.Casy.Core;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Core.Logging.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Models;
using OLS.Casy.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using OLS.Casy.Base;
using OfficeOpenXml;

namespace OLS.Casy.IO.ImportExport.TT
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(ITTImportExportProvider))]
    public class TTImportExportProvider : ITTImportExportProvider
    {
        private readonly IFileSystemStorageService _fileSystemStorageService;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly IEventAggregatorProvider _eventAggregatorProvider;

        [ImportingConstructor]
        public TTImportExportProvider(IFileSystemStorageService fileSystemStorageService,
            ILogger logger,
            ILocalizationService localizationService,
            IEventAggregatorProvider eventAggregatorProvider)
        {
            this._fileSystemStorageService = fileSystemStorageService;
            this._logger = logger;
            this._localizationService = localizationService;
            this._eventAggregatorProvider = eventAggregatorProvider;
        }

        public async Task<IEnumerable<MeasureResult>> ImportAsync(string filePath, int lastColorIndex)
        {
            List<MeasureResult> result = new List<MeasureResult>();
            var fileBytes = await _fileSystemStorageService.ReadFileAsync(filePath);

            using (MemoryStream ms = new MemoryStream(fileBytes))
            using (StreamReader sr = new StreamReader(ms))
            {
                // Line 1:  Cylcle count
                string measurementLine = sr.ReadLine();
                while (measurementLine != null)
                {
                    var colorName = "ChartColor" + (lastColorIndex % 10 == 0 ? 1 : 1 + (lastColorIndex % 10));

                    var measureResult = new MeasureResult()
                    {
                        MeasureResultGuid = Guid.NewGuid(),
                        IsTemporary = false,
                        Color = ((SolidColorBrush)Application.Current.Resources[colorName]).Color.ToString(),
                        Name = measurementLine,
                        Origin = "TT-Import"
                    };

                    lastColorIndex++;

                    var measureResultData = new MeasureResultData();
                    measureResult.MeasureResultDatas.Add(measureResultData);
                    measureResultData.MeasureResult = measureResult;

                    var measureSetup = new MeasureSetup()
                    {
                        MeasureSetupId = -1,
                        IsTemplate = false,
                        MeasureMode = Models.Enums.MeasureModes.MultipleCursor
                    };
                    measureSetup.ChannelCount = 400;
                    measureSetup.MeasureResult = measureResult;
                    measureResult.MeasureSetup = measureSetup;

                    var origMeasureSetup = new MeasureSetup()
                    {
                        MeasureSetupId = -1,
                        IsTemplate = false,
                        MeasureMode = Models.Enums.MeasureModes.MultipleCursor
                    };
                    origMeasureSetup.ChannelCount = 400;
                    origMeasureSetup.MeasureResult = measureResult;
                    measureResult.OriginalMeasureSetup = origMeasureSetup;

                    result.Add(measureResult);
                    
                    // Line 2: Measurement name
                    string commentLine = sr.ReadLine();
                    if (!string.IsNullOrEmpty(commentLine))
                    {
                        measureResult.Comment = commentLine;
                    }

                    // Line 3: Serial number
                    string serialNoLine = sr.ReadLine();
                    if (!string.IsNullOrEmpty(serialNoLine))
                    {
                        measureResult.SerialNumber = serialNoLine;
                    }

                    // Line 4: Template name (ignore)
                    string templateName = sr.ReadLine();
                    if (!string.IsNullOrEmpty(templateName))
                    {
                        measureSetup.Name = templateName;
                        origMeasureSetup.Name = templateName;
                    }

                    // Line 5: Setup Number (ignore)
                    sr.ReadLine();

                    // Line 6: Time
                    var timeLine = sr.ReadLine();

                    // Line 7: Date
                    var dateLine = sr.ReadLine();
                    var dateCreated = _localizationService.ParseString(string.Format("{0} {1}", dateLine.Replace("\"", ""), timeLine.Replace("\"", "")));
                    measureResult.CreatedAt = DateTime.UtcNow;
                    measureResult.MeasuredAt = dateCreated;
                    measureResult.MeasuredAtTimeZone = TimeZoneInfo.Local;

                    // Line 8: Dilution
                    var dilutionLine = sr.ReadLine();
                    measureSetup.DilutionFactor = double.Parse(dilutionLine.Replace('.', ','), CultureInfo.InvariantCulture);
                    origMeasureSetup.DilutionFactor = double.Parse(dilutionLine.Replace('.', ','), CultureInfo.InvariantCulture);

                    // Line 9: Repeats
                    var cyclesLine = sr.ReadLine();
                    measureSetup.Repeats = int.Parse(cyclesLine);
                    origMeasureSetup.Repeats = int.Parse(cyclesLine);

                    // Line 10: Sample Volume
                    var sampleVolumeLine = sr.ReadLine().Replace("\"", "");
                    double sampleVolume = double.Parse(sampleVolumeLine.Replace(',', '.'), CultureInfo.InvariantCulture);
                    measureSetup.Volume = int.Parse(sampleVolumeLine[0].ToString()) > 2 ? Volumes.FourHundred : Volumes.TwoHundred;
                    measureSetup.VolumeCorrectionFactor = (sampleVolume / (double)measureSetup.Volume) * 10000d;


                    //origMeasureSetup.VolumeCorrectionFactor = sampleVolume;
                    origMeasureSetup.Volume = int.Parse(sampleVolumeLine[0].ToString()) > 2 ? Volumes.FourHundred : Volumes.TwoHundred;
                    origMeasureSetup.VolumeCorrectionFactor = (sampleVolume / (double)measureSetup.Volume) * 10000d;


                    // Line 11: Mode of aggregation
                    var aggregationModeLine = sr.ReadLine();
                    switch (aggregationModeLine)
                    {
                        case "0":
                            measureSetup.AggregationCalculationMode = Models.Enums.AggregationCalculationModes.Off;
                            origMeasureSetup.AggregationCalculationMode = Models.Enums.AggregationCalculationModes.Off;
                            break;
                        case "1":
                            measureSetup.AggregationCalculationMode = Models.Enums.AggregationCalculationModes.Manual;
                            origMeasureSetup.AggregationCalculationMode = Models.Enums.AggregationCalculationModes.Manual;
                            break;
                        case "2":
                            measureSetup.AggregationCalculationMode = Models.Enums.AggregationCalculationModes.On;
                            origMeasureSetup.AggregationCalculationMode = Models.Enums.AggregationCalculationModes.On;
                            break;
                    }

                    // Line 12: Manual aggregation factor
                    var manualAggregationFactorLine = sr.ReadLine();
                    measureSetup.ManualAggregationCalculationFactor = double.Parse(manualAggregationFactorLine.Replace(',', '.'), CultureInfo.InvariantCulture);
                    origMeasureSetup.ManualAggregationCalculationFactor = double.Parse(manualAggregationFactorLine.Replace(',', '.'), CultureInfo.InvariantCulture);

                    // Line 13: Percentage mode (obsolet)
                    sr.ReadLine();

                    // Line 14: Debris Mode (obsolete)
                    sr.ReadLine();

                    // Line 15: Capillary
                    var capillaryLine = sr.ReadLine();
                    if (!string.IsNullOrEmpty(capillaryLine))
                    {
                        measureSetup.CapillarySize = int.Parse(capillaryLine);
                        origMeasureSetup.CapillarySize = int.Parse(capillaryLine);
                    }

                    // Line 16: ToDiameter
                    var toDiameterLine = sr.ReadLine();
                    if (!string.IsNullOrEmpty(toDiameterLine))
                    {
                        measureSetup.ToDiameter = int.Parse(toDiameterLine);
                        origMeasureSetup.ToDiameter = int.Parse(toDiameterLine);
                    }

                    // Line 17: Norm Cursor Left
                    var normCursorLeftLine = sr.ReadLine();

                    // Line 18: Norm Cursor Right
                    var normCursorRightLine = sr.ReadLine();

                    // Line 19: Eval Cursor Left
                    var evalCursorLeftLine = sr.ReadLine();

                    // Line 20: Eval Cursor Right
                    var evalCursorRightLine = sr.ReadLine();

                    // Line 21: Y-Scale
                    var yScaleLine = sr.ReadLine();
                    if (yScaleLine == "Auto")
                    {
                        measureSetup.ScalingMode = Models.Enums.ScalingModes.Auto;
                        origMeasureSetup.ScalingMode = Models.Enums.ScalingModes.Auto;
                    }
                    else
                    {
                        measureSetup.ScalingMode = Models.Enums.ScalingModes.MaxRange;
                        measureSetup.ScalingMaxRange = int.Parse(yScaleLine);

                        origMeasureSetup.ScalingMode = Models.Enums.ScalingModes.MaxRange;
                        origMeasureSetup.ScalingMaxRange = int.Parse(yScaleLine);
                    }

                    double[] measValues = new double[400];
                    for (int i = 0; i < 400; i++)
                    {
                        measValues[i] = int.Parse(sr.ReadLine());
                    }

                    //var dataBlock = Interpolations.LinearInterpolation(0, 20, 1024, measValues);
                    //var dataBlock = Interpolations.CubicInterpolation(0, 20, 1024, measValues);
                    measureResultData.DataBlock = measValues;//measValues.Select(d => d < 0d ? 0d : Math.Round(d, 0)).ToArray();

                    var cursor = new Cursor();
                    cursor.Name = "Range 1";
                    //cursor.MinLimit = Calculations.CalcChannel(0, measureSetup.ToDiameter, double.Parse(evalCursorLeftLine.Replace("\"", "")), measureSetup.ChannelCount);
                    cursor.MinLimit = double.Parse(evalCursorLeftLine.Replace("\"", "").Replace(',', '.'), CultureInfo.InvariantCulture);
                    cursor.MaxLimit = measureSetup.ToDiameter;
                    cursor.MeasureSetup = measureSetup;
                    measureResult.MeasureSetup.AddCursor(cursor);

                    var origCursor = new Cursor();
                    origCursor.Name = "Range 1";
                    //cursor.MinLimit = Calculations.CalcChannel(0, measureSetup.ToDiameter, double.Parse(evalCursorLeftLine.Replace("\"", "")), measureSetup.ChannelCount);
                    origCursor.MinLimit = double.Parse(evalCursorLeftLine.Replace("\"", "").Replace(',', '.'), CultureInfo.InvariantCulture);
                    origCursor.MaxLimit = origMeasureSetup.ToDiameter;
                    origCursor.MeasureSetup = origMeasureSetup;
                    measureResult.OriginalMeasureSetup.AddCursor(cursor);

                    var cursor2 = new Cursor();
                    cursor2.Name = "Range 2";
                    //cursor2.MinLimit = Calculations.CalcChannel(0, measureSetup.ToDiameter, double.Parse(normCursorLeftLine.Replace("\"", "")), measureSetup.ChannelCount);
                    //cursor2.MaxLimit = measureSetup.ChannelCount - 1;
                    cursor2.MinLimit = double.Parse(normCursorLeftLine.Replace("\"", "").Replace(',', '.'), CultureInfo.InvariantCulture);
                    cursor2.MaxLimit = measureSetup.ToDiameter;
                    cursor2.MeasureSetup = measureSetup;
                    measureResult.MeasureSetup.AddCursor(cursor2);

                    var origCursor2 = new Cursor();
                    origCursor2.Name = "Range 2";
                    //cursor2.MinLimit = Calculations.CalcChannel(0, measureSetup.ToDiameter, double.Parse(normCursorLeftLine.Replace("\"", "")), measureSetup.ChannelCount);
                    //cursor2.MaxLimit = measureSetup.ChannelCount - 1;
                    origCursor2.MinLimit = double.Parse(normCursorLeftLine.Replace("\"", "").Replace(',', '.'), CultureInfo.InvariantCulture);
                    origCursor2.MaxLimit = origMeasureSetup.ToDiameter;
                    origCursor2.MeasureSetup = origMeasureSetup;
                    measureResult.OriginalMeasureSetup.AddCursor(origCursor2);

                    // Line 422: Counts Eval Range
                    var countsEvelRangeLine = sr.ReadLine();

                    // Line 423: Concentration ??
                    var concentrationLine = sr.ReadLine();
                    measureResultData.ConcentrationTooHigh = concentrationLine == "1";

                    // Line 424: TODO: Ist per ML
                    var countsAboveLine = sr.ReadLine();
                    var effectiveMl = Calculations.CalcEffectiveMl((int)measureResult.MeasureSetup.Volume, measureResult.MeasureSetup.VolumeCorrectionFactor, measureResult.MeasureSetup.DilutionFactor, measureResult.MeasureSetup.Repeats);
                    measureResultData.AboveCalibrationLimitCount = (int) (double.Parse(countsAboveLine.Replace("\"", "").Replace(',', '.')) / effectiveMl);

                    // Line 425: Aggr. Factor
                    var aggrFactorLine = sr.ReadLine();
                    if (measureSetup.AggregationCalculationMode == Models.Enums.AggregationCalculationModes.Manual)
                    {
                        measureSetup.ManualAggregationCalculationFactor = double.Parse(aggrFactorLine.Replace("\"", "").Replace(',', '.'), CultureInfo.InvariantCulture);
                    }

                    if (origMeasureSetup.AggregationCalculationMode == Models.Enums.AggregationCalculationModes.Manual)
                    {
                        origMeasureSetup.ManualAggregationCalculationFactor = double.Parse(aggrFactorLine.Replace("\"", "").Replace(',', '.'), CultureInfo.InvariantCulture);
                    }

                    // Line 426: Counts/ml Eval
                    var countsMlEvalLine = sr.ReadLine();

                    // Line 427: Total Counts/ml Norm
                    var totalCountsMlNormLine = sr.ReadLine();

                    // Line 428: Viable Cells/ml Eval
                    var viableCellsLine = sr.ReadLine();

                    // Line 429: Total Cells/ml Norm
                    var totalCellsNormLine = sr.ReadLine();

                    // Line 430: Total Cells/ml Norm
                    var debrisLine = sr.ReadLine();

                    // Line 431: Vol/ml Eval
                    var volMlEvalLine = sr.ReadLine();

                    // Line 432: Total Vol/ml Norm
                    var totalVolMlNormLine = sr.ReadLine();

                    // Line 433: % Counts Eval
                    var countsPercEvalLine = sr.ReadLine();

                    // Line 434: % Viability Eval
                    var viabilityEvalLine = sr.ReadLine();
                    if(viabilityEvalLine != "not calculated")
                    {
                        measureSetup.MeasureMode = MeasureModes.Viability;
                        origMeasureSetup.MeasureMode = MeasureModes.Viability;
                    }

                    var orderedCursor = measureResult.MeasureSetup.Cursors.OrderBy(c => c.MinLimit).ToArray();
                    if (orderedCursor.Length == 1)
                    {
                        var single = orderedCursor.FirstOrDefault();
                        single.Name = "Range 1";
                    }
                    else if (orderedCursor.Length == 2)
                    {
                        if (orderedCursor[0].MinLimit == orderedCursor[1].MinLimit && orderedCursor[0].MaxLimit == orderedCursor[1].MaxLimit)
                        {
                            var single = orderedCursor[0];
                            single.Name = "Range 1";

                            measureResult.MeasureSetup.RemoveCursor(orderedCursor[1]);
                        }
                        else
                        {
                            if (measureResult.MeasureSetup.MeasureMode == MeasureModes.Viability)
                            {
                                orderedCursor[0].Name = "Cursor_DeadCells_Name";
                                orderedCursor[0].IsDeadCellsCursor = true;
                                orderedCursor[1].Name = "Cursor_VitalCells_Name";
                            }
                            orderedCursor[0].MaxLimit = orderedCursor[1].MinLimit - 0.01;
                        }
                    }

                    var orderedOrigCursor = measureResult.OriginalMeasureSetup.Cursors.OrderBy(c => c.MinLimit).ToArray();
                    if (orderedOrigCursor.Length == 1)
                    {
                        var single = orderedOrigCursor.FirstOrDefault();
                        single.Name = "Range 1";
                    }
                    else if (orderedOrigCursor.Length == 2)
                    {
                        if (orderedOrigCursor[0].MinLimit == orderedOrigCursor[1].MinLimit && orderedOrigCursor[0].MaxLimit == orderedOrigCursor[1].MaxLimit)
                        {
                            var single = orderedOrigCursor[0];
                            single.Name = "Range 1";

                            measureResult.OriginalMeasureSetup.RemoveCursor(orderedOrigCursor[1]);
                        }
                        else
                        {
                            if (measureResult.OriginalMeasureSetup.MeasureMode == MeasureModes.Viability)
                            {
                                orderedOrigCursor[0].Name = "Cursor_DeadCells_Name";
                                orderedOrigCursor[0].IsDeadCellsCursor = true;
                                orderedOrigCursor[1].Name = "Cursor_VitalCells_Name";
                            }
                            orderedOrigCursor[0].MaxLimit = orderedOrigCursor[1].MinLimit - 0.01;
                        }
                    }

                    // Line 435: % Volume Eval
                    var volPercEvalLine = sr.ReadLine();

                    // Line 436: Mean VOlume
                    var meanVolLine = sr.ReadLine();

                    // Line 437: Peak VOlume
                    var peakVolLine = sr.ReadLine();

                    // Line 438: Mean Diameter
                    var meanDiaLine = sr.ReadLine();

                    // Line 439: Peak Diameter
                    var peakDiaLine = sr.ReadLine();

                    //Line 440 Color ???
                    sr.ReadLine();

                    List<MeasureResultItemTypes> types = new List<MeasureResultItemTypes>();
                    foreach (var type in Enum.GetNames(typeof(MeasureResultItemTypes)))
                    {
                        types.Add((MeasureResultItemTypes)Enum.Parse(typeof(MeasureResultItemTypes), type));
                    }

                    measureSetup.ResultItemTypes = string.Join(";", types);
                    origMeasureSetup.ResultItemTypes = string.Join(";", types);

                    if (measureSetup.DilutionFactor > 1d)
                    {
                        measureSetup.DilutionCasyTonVolume = 10d;
                        measureSetup.DilutionSampleVolume = (1000 * measureResult.MeasureSetup.DilutionCasyTonVolume) / (measureResult.MeasureSetup.DilutionFactor - 1);
                    }

                    if (origMeasureSetup.DilutionFactor > 1d)
                    {
                        origMeasureSetup.DilutionCasyTonVolume = 10d;
                        origMeasureSetup.DilutionSampleVolume = (1000 * measureResult.MeasureSetup.DilutionCasyTonVolume) / (measureResult.MeasureSetup.DilutionFactor - 1);
                    }

                    _logger.Info(LogCategory.ImportExport, string.Format("TT-Import successful. File: {0}. Measure Result Name: {1}", filePath, measureResult.Name));

                    measurementLine = sr.ReadLine();
                }
            }

            return result;
        }

        public async Task<IEnumerable<MeasureResult>> ImportXlsxAsync(string filePath, int lastColorIndex)
        {
            MeasureResult[] measureResults = null;

            try
            {
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault(x => x.Name == "CASY TT Daten");
                    if(worksheet != null)
                    {
                        var start = worksheet.Dimension.Start;
                        var end = worksheet.Dimension.End;

                            string[] times = null;
                        double[][] dataBlocks = null;
                        Cursor cursor = null;

                        int rowCount = 0;
                        for (int row = start.Row; row <= end.Row; row++)
                        {
                            var col0Value = worksheet.Cells[row, 1].Text.TrimEnd();

                            int vol0Int;
                            if (int.TryParse(col0Value, out vol0Int))
                            {
                                for (int i = 0; i < measureResults.Length; i++)
                                {
                                    dataBlocks[i][vol0Int - 1] = int.Parse(worksheet.Cells[row, i + 2].Value.ToString().Replace(".", "").TrimEnd());
                                }

                                if (vol0Int == 400)
                                {
                                    break;
                                }
                            }
                            else
                            {
                                Console.WriteLine(col0Value);
                                switch (col0Value)
                                {
                                    case "Sample name":
                                        measureResults = new MeasureResult[end.Column - 1];
                                        times = new string[end.Column - 1];
                                        dataBlocks = new double[end.Column - 1][];
                                        for (int i = 0; i < measureResults.Length; i++)
                                        {
                                            var colorName = "ChartColor" + (lastColorIndex % 10 == 0 ? 1 : 1 + (lastColorIndex % 10));

                                            measureResults[i] = new MeasureResult()
                                            {
                                                MeasureResultGuid = Guid.NewGuid(),
                                                IsTemporary = false,
                                                Color = ((SolidColorBrush)Application.Current.Resources[colorName]).Color.ToString(),
                                                Name = worksheet.Cells[row, i + 2].Value.ToString().TrimEnd()
                                            };

                                            lastColorIndex++;

                                            dataBlocks[i] = new double[400];

                                            var measureResultData = new MeasureResultData();
                                            measureResults[i].MeasureResultDatas.Add(measureResultData);
                                            measureResultData.MeasureResult = measureResults[i];

                                            var measureSetup = new MeasureSetup()
                                            {
                                                IsTemplate = false,
                                                MeasureMode = Models.Enums.MeasureModes.MultipleCursor,
                                            };
                                            measureSetup.ChannelCount = 400;
                                            measureSetup.MeasureResult = measureResults[i];
                                            measureResults[i].MeasureSetup = measureSetup;
                                            var origMeasureSetup = new MeasureSetup()
                                            {
                                                IsTemplate = false,
                                                MeasureMode = Models.Enums.MeasureModes.MultipleCursor
                                            };
                                            origMeasureSetup.ChannelCount = 400;
                                            origMeasureSetup.MeasureResult = measureResults[i];
                                            measureResults[i].OriginalMeasureSetup = origMeasureSetup;

                                        }
                                        break;
                                    case "Comments  1":
                                    case "Comments 1":
                                        for (int i = 0; i < measureResults.Length; i++)
                                        { 
                                            var value = worksheet.Cells[row, i + 2].Value;
                                            if (value != null)
                                            {
                                                measureResults[i].Comment += value.ToString().TrimEnd();
                                            }
                                        }
                                        break;
                                    case "Comments  2":
                                    case "Comments  3":
                                        for (int i = 0; i < measureResults.Length; i++)
                                        {
                                            var value = worksheet.Cells[row, i + 2].Value;
                                            if (value != null)
                                            {
                                                measureResults[i].Comment += "\n" + value.ToString().TrimEnd();
                                            }
                                        }
                                        break;
                                    case "SerialNo.":
                                        for (int i = 0; i < measureResults.Length; i++)
                                        {
                                            measureResults[i].SerialNumber = worksheet.Cells[row, i + 2].Value.ToString().TrimEnd();
                                        }
                                        break;
                                    case "Time (24h format)":
                                        for (int i = 0; i < measureResults.Length; i++)
                                        {
                                            times[i] = worksheet.Cells[row, i + 2].Value.ToString().TrimEnd();
                                        }
                                        break;
                                    case "Date":
                                        for (int i = 0; i < measureResults.Length; i++)
                                        {
                                            var date = worksheet.Cells[row, i + 2].Value.ToString().TrimEnd();

                                            DateTime outDate = _localizationService.ParseString(string.Format("{0} {1}", date, times[i]));
                                            measureResults[i].CreatedAt = DateTime.UtcNow;
                                            measureResults[i].MeasuredAt = outDate;
                                            measureResults[i].MeasuredAtTimeZone = TimeZoneInfo.Local;
                                        }
                                        break;
                                    case "Capillary µm":
                                        for (int i = 0; i < measureResults.Length; i++)
                                        {
                                            measureResults[i].MeasureSetup.CapillarySize = int.Parse(worksheet.Cells[row, i + 2].Value.ToString().TrimEnd());
                                            measureResults[i].OriginalMeasureSetup.CapillarySize = measureResults[i].MeasureSetup.CapillarySize;
                                        }
                                        break;
                                    case "Size scale µm":
                                        for (int i = 0; i < measureResults.Length; i++)
                                        {
                                            measureResults[i].MeasureSetup.ToDiameter = int.Parse(worksheet.Cells[row, i + 2].Value.ToString().TrimEnd());
                                            measureResults[i].OriginalMeasureSetup.ToDiameter = measureResults[i].MeasureSetup.ToDiameter;
                                        }
                                        break;
                                    case "Measure repeats (1 - 10)":
                                    case "Repeat measures (1 - 10)":
                                        for (int i = 0; i < measureResults.Length; i++)
                                        {
                                            measureResults[i].MeasureSetup.Repeats = int.Parse(worksheet.Cells[row, i + 2].Value.ToString().TrimEnd());
                                            measureResults[i].OriginalMeasureSetup.Repeats = measureResults[i].MeasureSetup.Repeats;
                                        }
                                        break;
                                    case "Sample vol. µl":
                                        for (int i = 0; i < measureResults.Length; i++)
                                        {
                                            measureResults[i].MeasureSetup.Volume = (Volumes)int.Parse(worksheet.Cells[row, i + 2].Value.ToString().TrimEnd());
                                            measureResults[i].OriginalMeasureSetup.Volume = measureResults[i].MeasureSetup.Volume;
                                        }
                                        break;
                                    case "Volume calibration":
                                        for (int i = 0; i < measureResults.Length; i++)
                                        {
                                            measureResults[i].MeasureSetup.VolumeCorrectionFactor = (double.Parse(worksheet.Cells[row, i + 2].Value.ToString().TrimEnd().Replace(',', '.'), CultureInfo.InvariantCulture)) * 10000d;
                                            measureResults[i].OriginalMeasureSetup.VolumeCorrectionFactor = measureResults[i].MeasureSetup.VolumeCorrectionFactor;
                                        }
                                        break;
                                    case "Conc. check":
                                        for (int i = 0; i < measureResults.Length; i++)
                                        {
                                            measureResults[i].MeasureResultDatas[0].ConcentrationTooHigh = worksheet.Cells[row, i + 2].Value.ToString().TrimEnd() != "Ok";
                                        }
                                        break;
                                    case "Dilution":
                                        for (int i = 0; i < measureResults.Length; i++)
                                        {
                                            var value = double.Parse(worksheet.Cells[row, i + 2].Value.ToString().TrimEnd().Replace(',', '.'), CultureInfo.InvariantCulture);
                                            if (rowCount < 45)
                                            {
                                                measureResults[i].OriginalMeasureSetup.DilutionFactor = value;
                                            }
                                            else
                                            {
                                                measureResults[i].MeasureSetup.DilutionFactor = value;
                                            }
                                        }
                                        break;
                                    case "Aggr.mode":
                                        for (int i = 0; i < measureResults.Length; i++)
                                        {
                                            var value = worksheet.Cells[row, i + 2].Value.ToString().TrimEnd();
                                            if (rowCount < 45)
                                            {
                                                switch (value)
                                                {
                                                    case "Off":
                                                        measureResults[i].OriginalMeasureSetup.AggregationCalculationMode = AggregationCalculationModes.Off;
                                                        break;
                                                    case "On":
                                                    case "Auto":
                                                        measureResults[i].OriginalMeasureSetup.AggregationCalculationMode = AggregationCalculationModes.On;
                                                        break;
                                                    default:
                                                        measureResults[i].OriginalMeasureSetup.AggregationCalculationMode = AggregationCalculationModes.Manual;
                                                        break;
                                                }
                                            }
                                            else
                                            {
                                                switch(value)
                                                {
                                                    case "Off":
                                                        measureResults[i].MeasureSetup.AggregationCalculationMode = AggregationCalculationModes.Off;
                                                        break;
                                                    case "On":
                                                    case "Auto":
                                                        measureResults[i].MeasureSetup.AggregationCalculationMode = AggregationCalculationModes.On;
                                                        break;
                                                    default:
                                                        measureResults[i].MeasureSetup.AggregationCalculationMode = AggregationCalculationModes.Manual;
                                                        break;
                                                }
                                            }
                                        }
                                        break;
                                    case "Man.peak vol. fl":
                                        for (int i = 0; i < measureResults.Length; i++)
                                        {
                                            var value = double.Parse(worksheet.Cells[row, i + 2].Value.ToString().TrimEnd().Replace(',', '.'), CultureInfo.InvariantCulture);
                                            if (rowCount < 45)
                                            {
                                                measureResults[i].OriginalMeasureSetup.ManualAggregationCalculationFactor = value;
                                            }
                                            else
                                            {
                                                measureResults[i].MeasureSetup.ManualAggregationCalculationFactor = value;
                                            }
                                        }
                                        break;
                                    case "Y-Scale":
                                    case "Y-scale":
                                        for (int i = 0; i < measureResults.Length; i++)
                                        {
                                            var value = worksheet.Cells[row, i + 2].Value.ToString().TrimEnd();
                                            if (rowCount < 45)
                                            {
                                                if (value == "Auto")
                                                {
                                                    measureResults[i].OriginalMeasureSetup.ScalingMode = ScalingModes.Auto;
                                                }
                                                else
                                                {
                                                    measureResults[i].OriginalMeasureSetup.ScalingMode = ScalingModes.MaxRange;
                                                    measureResults[i].OriginalMeasureSetup.ScalingMaxRange = int.Parse(value);
                                                }
                                            }
                                            else
                                            {
                                                if (value == "Auto")
                                                {
                                                    measureResults[i].MeasureSetup.ScalingMode = ScalingModes.Auto;
                                                }
                                                else
                                                {
                                                    measureResults[i].MeasureSetup.ScalingMode = ScalingModes.MaxRange;
                                                    measureResults[i].MeasureSetup.ScalingMaxRange = int.Parse(value);
                                                }
                                            }
                                        }
                                        break;
                                    case "L.Norm.Cursor µm":
                                        for (int i = 0; i < measureResults.Length; i++)
                                        {
                                            MeasureSetup measureSetup;
                                            if (rowCount < 45)
                                            {
                                                measureSetup = measureResults[i].OriginalMeasureSetup;
                                            }
                                            else
                                            {
                                                measureSetup = measureResults[i].MeasureSetup;
                                            }
                                            IEnumerable<Cursor> cursors = measureSetup.Cursors;

                                            var value = double.Parse(worksheet.Cells[row, i + 2].Value.ToString().TrimEnd().Replace(',', '.'), CultureInfo.InvariantCulture);

                                            cursor = cursors.FirstOrDefault(c => c.Name == "Range 1");
                                            if (cursor == null)
                                            {
                                                cursor = new Cursor
                                                {
                                                    Name = "Range 1",
                                                    Color = ((SolidColorBrush) Application.Current.Resources[
                                                        "StripBorderColor1"]).Color.ToString(),
                                                    MeasureSetup = measureSetup
                                                };
                                                measureSetup.AddCursor(cursor);
                                            }
                                            //cursor.MinLimit = Calculations.CalcChannel(0, measureResults[i].MeasureSetup.ToDiameter, value, measureResults[i].MeasureSetup.ChannelCount);
                                            cursor.MinLimit = value;
                                        }
                                        break;
                                    case "R.Norm.Cursor µm":
                                        for (int i = 0; i < measureResults.Length; i++)
                                        {
                                            MeasureSetup measureSetup;
                                            if (rowCount < 45)
                                            {
                                                measureSetup = measureResults[i].OriginalMeasureSetup;
                                            }
                                            else
                                            {
                                                measureSetup = measureResults[i].MeasureSetup;
                                            }
                                            IEnumerable<Cursor> cursors = measureSetup.Cursors;

                                            var value = double.Parse(worksheet.Cells[row, i + 2].Value.ToString().TrimEnd().Replace(',', '.'), CultureInfo.InvariantCulture);

                                            cursor = cursors.FirstOrDefault(c => c.Name == "Range 1");
                                            if (cursor == null)
                                            {
                                                cursor = new Cursor
                                                {
                                                    Name = "Range 1",
                                                    Color = ((SolidColorBrush)Application.Current.Resources["StripBorderColor1"]).Color.ToString(),
                                                    MeasureSetup = measureSetup
                                                };
                                                measureSetup.AddCursor(cursor);
                                            }
                                            //cursor.MaxLimit = Calculations.CalcChannel(0, measureResults[i].MeasureSetup.ToDiameter, value, measureResults[i].MeasureSetup.ChannelCount);
                                            cursor.MaxLimit = value;
                                        }
                                        break;
                                    case "L.Eval.Cursor µm":
                                        for (int i = 0; i < measureResults.Length; i++)
                                        {
                                            MeasureSetup measureSetup;
                                            if (rowCount < 45)
                                            {
                                                measureSetup = measureResults[i].OriginalMeasureSetup;
                                            }
                                            else
                                            {
                                                measureSetup = measureResults[i].MeasureSetup;
                                            }
                                            IEnumerable<Cursor> cursors = measureSetup.Cursors;

                                            var value = double.Parse(worksheet.Cells[row, i + 2].Value.ToString().TrimEnd().Replace(',', '.'), CultureInfo.InvariantCulture);

                                            cursor = cursors.FirstOrDefault(c => c.Name == "Range 2");
                                            if (cursor == null)
                                            {
                                                cursor = new Cursor();
                                                cursor.Name = "Range 2";
                                                cursor.Color = ((SolidColorBrush)Application.Current.Resources["StripBorderColor1"]).Color.ToString();
                                                cursor.MeasureSetup = measureSetup;
                                                measureSetup.AddCursor(cursor);
                                            }
                                            //cursor.MinLimit = Calculations.CalcChannel(0, measureResults[i].MeasureSetup.ToDiameter, value, measureResults[i].MeasureSetup.ChannelCount);
                                            cursor.MinLimit = value;
                                        }
                                        break;
                                    case "R.Eval.Cursor µm":
                                        for (int i = 0; i < measureResults.Length; i++)
                                        {
                                            MeasureSetup measureSetup;
                                            if (rowCount < 45)
                                            {
                                                measureSetup = measureResults[i].OriginalMeasureSetup;
                                            }
                                            else
                                            {
                                                measureSetup = measureResults[i].MeasureSetup;
                                            }
                                            IEnumerable<Cursor> cursors = measureSetup.Cursors;

                                            var value = double.Parse(worksheet.Cells[row, i + 2].Value.ToString().TrimEnd().Replace(',', '.'), CultureInfo.InvariantCulture);

                                            cursor = cursors.FirstOrDefault(c => c.Name == "Range 2");
                                            if (cursor == null)
                                            {
                                                cursor = new Cursor();
                                                cursor.Name = "Range 2";
                                                cursor.Color = ((SolidColorBrush)Application.Current.Resources["StripBorderColor1"]).Color.ToString();
                                                cursor.MeasureSetup = measureSetup;
                                                measureSetup.AddCursor(cursor);
                                            }
                                            //cursor.MaxLimit = Calculations.CalcChannel(0, measureResults[i].MeasureSetup.ToDiameter, value, measureResults[i].MeasureSetup.ChannelCount);
                                            cursor.MaxLimit = value;
                                        }
                                        break;
                                    case "Counts > Scale /ml":
                                        for (int i = 0; i < measureResults.Length; i++)
                                        {
                                            if (rowCount > 45)
                                            {
                                                var effectiveMl = Calculations.CalcEffectiveMl((int)measureResults[i].MeasureSetup.Volume, measureResults[i].MeasureSetup.VolumeCorrectionFactor, measureResults[i].MeasureSetup.DilutionFactor, measureResults[i].MeasureSetup.Repeats);
                                                measureResults[i].MeasureResultDatas[0].AboveCalibrationLimitCount = (int)((double)Decimal.Parse(worksheet.Cells[row, i + 2].Value.ToString().TrimEnd().Replace(",", "."), System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture) / effectiveMl);
                                            }
                                        }
                                        break;
                                    case "%Viability (Eval.range)":
                                        for (int i = 0; i < measureResults.Length; i++)
                                        {
                                            MeasureSetup measureSetup;
                                            if (rowCount < 45)
                                            {
                                                measureSetup = measureResults[i].OriginalMeasureSetup;
                                            }
                                            else
                                            {
                                                measureSetup = measureResults[i].MeasureSetup;
                                            }

                                            bool isFreeRange = worksheet.Cells[row, i + 2].Value.ToString().TrimEnd() == "not calculated";
                                            measureSetup.MeasureMode = isFreeRange ? MeasureModes.MultipleCursor : MeasureModes.Viability;
                                        }
                                        break;
                                    case "Setup":
                                        for (int i = 0; i < measureResults.Length; i++)
                                        {
                                            measureResults[i].MeasureSetup.Name = worksheet.Cells[row, i + 2].Value.ToString();
                                            measureResults[i].OriginalMeasureSetup.Name = worksheet.Cells[row, i + 2].Value.ToString();
                                        }
                                        break;
                                }
                            }
                            rowCount++;
                        }

                        if (measureResults != null)
                        {
                            for (int i = 0; i < measureResults.Length; i++)
                            {
                                //var dataBlock = Interpolations.LinearInterpolation(0, 20, 1024, dataBlocks[i]);
                                measureResults[i].MeasureResultDatas[0].DataBlock = dataBlocks[i];//dataBlock.Select(d => d < 0d ? 0d : Math.Round(d, 0)).ToArray();;

                                var orderedCursor = measureResults[i].MeasureSetup.Cursors.OrderBy(c => c.MinLimit).ToArray();
                                if (orderedCursor.Length == 1)
                                {
                                    var single = orderedCursor.FirstOrDefault();
                                    single.Name = "Range 1";
                                    measureResults[i].MeasureSetup.MeasureMode = MeasureModes.MultipleCursor;
                                }
                                else if (orderedCursor.Length == 2)
                                {
                                    if (orderedCursor[0].MinLimit == orderedCursor[1].MinLimit && orderedCursor[0].MaxLimit == orderedCursor[1].MaxLimit)
                                    {
                                        measureResults[i].MeasureSetup.MeasureMode = MeasureModes.MultipleCursor;
                                        var single = orderedCursor[0];
                                        single.Name = "Range 1";

                                        measureResults[i].MeasureSetup.RemoveCursor(orderedCursor[1]);
                                        
                                    }
                                    else
                                    {
                                        if (measureResults[i].MeasureSetup.MeasureMode == MeasureModes.Viability)
                                        {
                                            orderedCursor[0].Name = "Cursor_DeadCells_Name";
                                            orderedCursor[0].IsDeadCellsCursor = true;
                                            orderedCursor[1].Name = "Cursor_VitalCells_Name";
                                        }
                                        orderedCursor[0].MaxLimit = orderedCursor[1].MinLimit - 0.01;
                                    }
                                }

                                orderedCursor = measureResults[i].OriginalMeasureSetup.Cursors.OrderBy(c => c.MinLimit).ToArray();
                                if (orderedCursor.Length == 1)
                                {
                                    var single = orderedCursor.FirstOrDefault();
                                    single.Name = "Range 1";
                                    measureResults[i].OriginalMeasureSetup.MeasureMode = MeasureModes.MultipleCursor;
                                }
                                else if (orderedCursor.Length == 2)
                                {
                                    if (orderedCursor[0].MinLimit == orderedCursor[1].MinLimit && orderedCursor[0].MaxLimit == orderedCursor[1].MaxLimit)
                                    {
                                        var single = orderedCursor[0];
                                        single.Name = "Range 1";

                                        measureResults[i].OriginalMeasureSetup.MeasureMode = MeasureModes.MultipleCursor;
                                        measureResults[i].OriginalMeasureSetup.RemoveCursor(orderedCursor[1]);
                                    }
                                    else
                                    {
                                        if (measureResults[i].OriginalMeasureSetup.MeasureMode == MeasureModes.Viability)
                                        {
                                            orderedCursor[0].Name = "Cursor_DeadCells_Name";
                                            orderedCursor[0].IsDeadCellsCursor = true;
                                            orderedCursor[1].Name = "Cursor_VitalCells_Name";
                                        }
                                        orderedCursor[0].MaxLimit = orderedCursor[1].MinLimit - 0.01;
                                    }
                                }

                                List<MeasureResultItemTypes> types = new List<MeasureResultItemTypes>();
                                foreach (var type in Enum.GetNames(typeof(MeasureResultItemTypes)))
                                {
                                    types.Add((MeasureResultItemTypes)Enum.Parse(typeof(MeasureResultItemTypes), type));
                                }

                                measureResults[i].MeasureSetup.ResultItemTypes = string.Join(";", types);
                                measureResults[i].OriginalMeasureSetup.ResultItemTypes = string.Join(";", types);

                                //foreach (var c in measureResults[i].MeasureSetup.Cursors)
                                //{
                                //    c.MeasureSetup = measureResults[i].MeasureSetup;
                                //}

                                //foreach (var c in measureResults[i].OriginalMeasureSetup.Cursors)
                                //{
                                //    c.MeasureSetup = measureResults[i].OriginalMeasureSetup;
                                //}

                                if (measureResults[i].MeasureSetup.DilutionFactor > 1d)
                                {
                                    measureResults[i].MeasureSetup.DilutionCasyTonVolume = 10d;
                                    measureResults[i].MeasureSetup.DilutionSampleVolume = (1000 * measureResults[i].MeasureSetup.DilutionCasyTonVolume) / (measureResults[i].MeasureSetup.DilutionFactor - 1);
                                }

                                if (measureResults[i].OriginalMeasureSetup.DilutionFactor > 1d)
                                {
                                    measureResults[i].OriginalMeasureSetup.DilutionCasyTonVolume = 10d;
                                    measureResults[i].OriginalMeasureSetup.DilutionSampleVolume = (1000 * measureResults[i].OriginalMeasureSetup.DilutionCasyTonVolume) / (measureResults[i].OriginalMeasureSetup.DilutionFactor - 1);
                                }

                                _logger.Info(LogCategory.ImportExport, string.Format("TT-Import from XSLX successful. File: {0}. Measure Result Name: {1}", filePath, measureResults[i].Name));
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                await Task.Factory.StartNew(() =>
                {
                    var awaiter = new ManualResetEvent(false);

                    ShowMessageBoxDialogWrapper messageBoxWrapper = new ShowMessageBoxDialogWrapper()
                    {
                        Awaiter = awaiter,
                        Title = "ImportTTError_Title",
                        Message = "ImportTTError_Content"
                    };

                    _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>().Publish(messageBoxWrapper);

                    awaiter.WaitOne();
                });
            }
            return measureResults;
        }
    }
}
