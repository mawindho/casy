using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Shapes;
using MigraDoc.DocumentObjectModel.Tables;
using OLS.Casy.Controller.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Models;
using OLS.Casy.Models.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using OLS.Casy.Core.Api;
using OLS.Casy.Ui.Core.Api;

namespace OLS.Casy.Ui.Core.Documents
{
    public class MeanDocument : DocumentBase
    {
        private readonly IMeasureResultDataCalculationService _measureResultDataCalculationService;

        private MeasureResult _meanResult;
        private MeasureResult[] _measureResults;

        private byte[] _graphImage;

        private Table _resultNamesTable;

        private Table _paramsTable;
        private Column _paramsTableNameColumn;
        private Row _paramsHeaderRow;
        private Dictionary<Tuple<string, Cursor>, Row> _paramsTableRows;

        private Table _resultsTable;
        private Column _resultTableNameColumn;
        private Column _resultTableMeanResultsColumn;
        private Column _resultTableDeviationsColumn;
        private Row _resultsHeaderRow;
        private Dictionary<Tuple<MeasureResultItemTypes, Cursor>, Row> _resultsTableRows;

        public MeanDocument(ILocalizationService localizationService,
            IMeasureResultDataCalculationService measureResultDataCalculationService,
            IAuthenticationService authenticationService,
            IDocumentSettingsManager documentSettingsManager,
            IEnvironmentService environmentService)
            : base(localizationService, authenticationService, documentSettingsManager, environmentService)
        {
            _measureResultDataCalculationService = measureResultDataCalculationService;
        }

        public Document CreateDocument(MeasureResult meanResult, MeasureResult[] measureResults, byte[] graphImage)
        {
            _meanResult = meanResult;
            _measureResults = measureResults;
            _graphImage = graphImage;

            Document = new Document();

            DefineStyles();
            CreatePage(Orientation.Portrait, _meanResult.CreatedBy);
            FillContent();

            return Document;
        }

        protected override void CreatePage(Orientation orientation, string createdBy)
        {
            base.CreatePage(orientation, createdBy);

            _resultNamesTable = Section.AddTable();
            _resultNamesTable.Format.Font.Name = "Dosis-Light";
            _resultNamesTable.Format.Font.Size = 7;

            var column = _resultNamesTable.AddColumn("9cm");
            column.Format.Font.Color = Colors.Black;
            column.Format.Font.Name = "Dosis-Light";
            column.Format.Font.Size = 9;
            column.Format.LineSpacing = 10.8;
            column.Format.LineSpacingRule = LineSpacingRule.AtLeast;

            column = _resultNamesTable.AddColumn("9cm");
            column.Format.Font.Color = Colors.Black;
            column.Format.Font.Name = "Dosis-Light";
            column.Format.Font.Size = 9;
            column.Format.LineSpacing = 10.8;
            column.Format.LineSpacingRule = LineSpacingRule.AtLeast;

            var imagePath = "base64:" + Convert.ToBase64String(_graphImage);

            var graphSection = Section.AddParagraph();
            graphSection.Format.SpaceBefore = "1cm";
            graphSection.Format.SpaceAfter = "1cm";
            var graphImage = graphSection.AddImage(imagePath);
            graphImage.Width = "18cm";
            graphImage.LockAspectRatio = true;
            graphImage.RelativeVertical = RelativeVertical.Paragraph;
            graphImage.RelativeHorizontal = RelativeHorizontal.Margin;
            graphImage.Top = ShapePosition.Top;
            graphImage.Left = ShapePosition.Left;

            _paramsTable = Section.AddTable();
            _paramsTable.Format.Font.Name = "Dosis-Light";
            _paramsTable.Format.Font.Size = 7;
            _paramsTable.Borders.Width = "0.01cm";
            _paramsTable.Borders.Color = Colors.Black;

            _paramsTableNameColumn = _paramsTable.AddColumn("3cm");
            _paramsTableNameColumn.Format.LeftIndent = "0.2cm";
            _paramsTableNameColumn.Format.Alignment = ParagraphAlignment.Left;

            var paragraph = Section.AddParagraph();
            paragraph.AddSpace(1);

            _resultsTable = Section.AddTable();
            _resultsTable.Format.Font.Name = "Dosis-Light";
            _resultsTable.Format.Font.Size = 7;
            _resultsTable.Borders.Width = "0.01cm";
            _resultsTable.Borders.Color = Colors.Black;

            _resultTableNameColumn = _resultsTable.AddColumn("3cm");
            _resultTableNameColumn.Format.LeftIndent = "0.2cm";
            _resultTableNameColumn.Format.Alignment = ParagraphAlignment.Left;

            var columnWidth = 10.5 / _measureResults.Length;
            for (var i = 0; i < _measureResults.Length; i++)
            {
                var resultsColumn = _resultsTable.AddColumn($"{columnWidth}cm");
                resultsColumn.Format.Alignment = ParagraphAlignment.Center;

                var paramsColumn = _paramsTable.AddColumn($"{columnWidth / 2}cm");
                paramsColumn.Format.Alignment = ParagraphAlignment.Center;

                paramsColumn = _paramsTable.AddColumn($"{columnWidth / 2}cm");
                paramsColumn.Format.Alignment = ParagraphAlignment.Center;
            }

            _resultTableMeanResultsColumn = _resultsTable.AddColumn("3cm");
            _resultTableMeanResultsColumn.Format.Alignment = ParagraphAlignment.Center;

            _resultTableDeviationsColumn = _resultsTable.AddColumn("1.5cm");
            _resultTableDeviationsColumn.Format.Alignment = ParagraphAlignment.Center;

            _paramsHeaderRow = _paramsTable.AddRow();
            _paramsHeaderRow.Format.Font.Bold = true;
            _paramsHeaderRow.Format.Font.Size = 9;
            _paramsHeaderRow.Cells[0].VerticalAlignment = VerticalAlignment.Center;
            _paramsHeaderRow.Cells[0].AddParagraph("PARAMETER");

            _resultsHeaderRow = _resultsTable.AddRow();
            _resultsHeaderRow.Format.Font.Bold = true;
            _resultsHeaderRow.Format.Font.Size = 9;
            _resultsHeaderRow.Cells[0].VerticalAlignment = VerticalAlignment.Center;
            _resultsHeaderRow.Cells[0].AddParagraph("RESULTS");
            var j = _measureResults.Length;
            
            _resultsHeaderRow.Cells[j + 1].VerticalAlignment = VerticalAlignment.Center;
            _resultsHeaderRow.Cells[j + 1].AddParagraph("MEAN");
            _resultsHeaderRow.Cells[j + 2].VerticalAlignment = VerticalAlignment.Center;
            _resultsHeaderRow.Cells[j + 2].AddParagraph("\u00B1 CV %");
            
            _paramsTableRows = new Dictionary<Tuple<string, Cursor>, Row>();

            var documentSettings = DocumentSettingsManager.OverlayDocumentSettings;
            var paramSettings = documentSettings.Where(item => item.IsSelected && item.Category == DocumentSettingCategory.Parameter)
                .OrderBy(item => item.DisplayOrder);

            foreach (var paramSetting in paramSettings)
            {
                switch (paramSetting.Type)
                {
                    case DocumentSettingType.SampleVolume:
                    case DocumentSettingType.DilutionFactor:
                    case DocumentSettingType.Capillary:
                    case DocumentSettingType.Smoothing:
                    case DocumentSettingType.AggrCorrection:
                    case DocumentSettingType.SizeScale:
                        AddParametersRow(paramSetting.Name);
                        break;
                    case DocumentSettingType.Ranges:
                        foreach (var cursor in _meanResult.MeasureSetup.Cursors)
                        {
                            AddParametersRow(LocalizationService.GetLocalizedString(cursor.Name), cursor);
                        }
                        break;
                }
            }

            _resultsTableRows = new Dictionary<Tuple<MeasureResultItemTypes, Cursor>, Row>();

            var resultSettingsStack = documentSettings.Where(item => item.IsSelected && item.Category == DocumentSettingCategory.Result)
               .OrderBy(item => item.DisplayOrder).ToList();

            var isViability = _meanResult.MeasureSetup.MeasureMode == MeasureModes.Viability;
            Cursor viableCursor = null, deadCursor = null;
            if (isViability)
            {
                viableCursor = _meanResult.MeasureSetup.Cursors.FirstOrDefault(c => !c.IsDeadCellsCursor);
                deadCursor = _meanResult.MeasureSetup.Cursors.FirstOrDefault(c => c.IsDeadCellsCursor);
            }

            while (resultSettingsStack.Any())
            {
                var resultSettings = new List<DocumentSetting>();
                var currentResultSetting = resultSettingsStack.First();
                resultSettingsStack.Remove(currentResultSetting);
                resultSettings.Add(currentResultSetting);

                if (!isViability && currentResultSetting.IsFreeRanges)
                {
                    if (currentResultSetting.Type != DocumentSettingType.TotalCountsMl)
                    {
                        resultSettings.AddRange(resultSettingsStack.TakeWhile(item =>
                            item.IsFreeRanges && item.Type != DocumentSettingType.TotalCountsMl).ToList());

                        foreach (var cursor in _meanResult.MeasureSetup.Cursors)
                        {
                            foreach (var resultSetting in resultSettings)
                            {
                                switch (resultSetting.Type)
                                {
                                    case DocumentSettingType.CountsMl:
                                        AddResultsRow($"Counts/ml ({LocalizationService.GetLocalizedString(cursor.Name)})",
                                            MeasureResultItemTypes.TotalCountsPerMl, cursor);
                                        break;
                                    case DocumentSettingType.CellsPercentage:
                                        AddResultsRow($"% Cells ({LocalizationService.GetLocalizedString(cursor.Name)})",
                                            MeasureResultItemTypes.CountsPercentage, cursor);
                                        break;
                                    case DocumentSettingType.AggrFactor:
                                        AddResultsRow($"Aggr. Factor ({LocalizationService.GetLocalizedString(cursor.Name)})",
                                            MeasureResultItemTypes.AggregationFactor, cursor);
                                        break;
                                    case DocumentSettingType.VolumeMl:
                                        AddResultsRow($"Volume/ml ({LocalizationService.GetLocalizedString(cursor.Name)})",
                                            MeasureResultItemTypes.VolumePerMl, cursor);
                                        break;
                                    case DocumentSettingType.MeanDiameter:
                                        AddResultsRow($"Mean Diameter ({LocalizationService.GetLocalizedString(cursor.Name)})",
                                            MeasureResultItemTypes.MeanDiameter, cursor);
                                        break;
                                    case DocumentSettingType.PeakDiameter:
                                        AddResultsRow($"Peak Diameter ({LocalizationService.GetLocalizedString(cursor.Name)})",
                                            MeasureResultItemTypes.PeakDiameter, cursor);
                                        break;
                                    case DocumentSettingType.MeanVolume:
                                        AddResultsRow($"Mean Volume ({LocalizationService.GetLocalizedString(cursor.Name)})",
                                            MeasureResultItemTypes.MeanVolume, cursor);
                                        break;
                                    case DocumentSettingType.PeakVolume:
                                        AddResultsRow($"Peak Volume ({LocalizationService.GetLocalizedString(cursor.Name)})",
                                            MeasureResultItemTypes.PeakVolume, cursor);
                                        break;
                                }
                                resultSettingsStack.Remove(resultSetting);
                            }
                        }
                    }
                    else
                    {
                        AddResultsRow("Total Counts/ml",
                            MeasureResultItemTypes.TotalCountsPerMl);
                    }
                }
                else
                {
                    switch (currentResultSetting.Type)
                    {
                        case DocumentSettingType.Concentration:
                            AddResultsRow(currentResultSetting.Name,
                                MeasureResultItemTypes.Concentration);
                            break;
                        case DocumentSettingType.Counts:
                            AddResultsRow(currentResultSetting.Name,
                                MeasureResultItemTypes.Counts);
                            break;
                        case DocumentSettingType.ViableCellsMl:
                            if (isViability)
                            {
                                AddResultsRow(currentResultSetting.Name,
                                    MeasureResultItemTypes.ViableCellsPerMl, viableCursor);
                            }
                            break;
                        case DocumentSettingType.DeadCellsMl:
                            if (isViability)
                            {
                                AddResultsRow(currentResultSetting.Name,
                                    MeasureResultItemTypes.CountsPerMl, deadCursor);
                            }
                            break;
                        case DocumentSettingType.TotalCellsMl:
                            if (isViability)
                            {
                                AddResultsRow(currentResultSetting.Name,
                                    MeasureResultItemTypes.TotalCountsPerMl);
                            }
                            break;
                        case DocumentSettingType.Viability:
                            if (isViability)
                            {
                                AddResultsRow(currentResultSetting.Name,
                                    MeasureResultItemTypes.Viability, viableCursor);
                            }
                            break;
                        case DocumentSettingType.ViableAggrFactor:
                            if (isViability)
                            {
                                AddResultsRow(currentResultSetting.Name,
                                    MeasureResultItemTypes.AggregationFactor, viableCursor);
                            }
                            break;
                        case DocumentSettingType.DebrisMl:
                            if (isViability)
                            {
                                AddResultsRow(currentResultSetting.Name,
                                    MeasureResultItemTypes.DebrisCount);
                            }
                            break;
                        case DocumentSettingType.CountsMlAboveRange:
                            AddResultsRow($"Counts/ml > {_meanResult.MeasureSetup.ToDiameter} µm",
                                MeasureResultItemTypes.CountsAboveDiameter);
                            break;
                        case DocumentSettingType.VolumeMlViable:
                            if (isViability)
                            {
                                AddResultsRow("Volume/ml",
                                    MeasureResultItemTypes.VolumePerMl, viableCursor);
                            }
                            break;
                        case DocumentSettingType.MeanDiameterViable:
                            if (isViability)
                            {
                                AddResultsRow("Mean Diameter",
                                    MeasureResultItemTypes.MeanDiameter, viableCursor);
                            }
                            break;
                        case DocumentSettingType.PeakDiameterViable:
                            if (isViability)
                            {
                                AddResultsRow("Peak Diameter",
                                    MeasureResultItemTypes.PeakDiameter, viableCursor);
                            }
                            break;
                        case DocumentSettingType.MeanVolumeViable:
                            if (isViability)
                            {
                                AddResultsRow("Mean Volume",
                                    MeasureResultItemTypes.MeanVolume, viableCursor);
                            }
                            break;
                        case DocumentSettingType.PeakVolumeViable:
                            if (isViability)
                            {
                                AddResultsRow("Peak Volume",
                                    MeasureResultItemTypes.PeakVolume, viableCursor);
                            }
                            break;
                    }
                }
            }
        }

        private void FillContent()
        {
            var paragraph = TitleFrame.AddParagraph();
            paragraph.Format.Font.Name = "Dosis-Regular";
            paragraph.Format.Font.Size = 9;
            paragraph.Format.Font.Color = Colors.White;
            paragraph.Format.Alignment = ParagraphAlignment.Center;
            paragraph.AddText("MEAN");

            NameSection.AddText($"MEAN, {_measureResults.Length} MEASUREMENTS");

            Row resultNamesTableRow = null;
            var resultNamesCellIndex = 2;
            var resultsCellIndex = 1;
            var paramsCellIndex = 1;

            MeasureResultItem measureResultItem;
            string measureResultValue;
            Cursor viableCursor;

            foreach (var measureResult in _measureResults)
            {
                if (measureResult?.MeasureSetup == null) continue;

                if (resultNamesCellIndex > 1)
                {
                    resultNamesTableRow = _resultNamesTable.AddRow();
                    resultNamesTableRow.Height = "0.45cm";
                    resultNamesCellIndex = 0;
                }

                resultNamesTableRow.Cells[resultNamesCellIndex].VerticalAlignment = VerticalAlignment.Center;
                string color = (measureResult.Color ?? "#FF009FE3").Replace("#", "0x");
                resultNamesTableRow.Cells[resultNamesCellIndex].Format.Font.Color = Color.Parse(color);
                resultNamesTableRow.Cells[resultNamesCellIndex].AddParagraph(
                    $"Pos {resultsCellIndex}: {measureResult.Name}");
                resultNamesCellIndex++;

                _resultsHeaderRow.Cells[resultsCellIndex].VerticalAlignment = VerticalAlignment.Center;
                _resultsHeaderRow.Cells[resultsCellIndex].Format.Alignment = ParagraphAlignment.Center;
                _resultsHeaderRow.Cells[resultsCellIndex].Format.Font.Color = Color.Parse(color);
                _resultsHeaderRow.Cells[resultsCellIndex].AddParagraph(resultsCellIndex.ToString());

                _paramsHeaderRow.Cells[paramsCellIndex].MergeRight = 1;
                _paramsHeaderRow.Cells[paramsCellIndex].VerticalAlignment = VerticalAlignment.Center;
                _paramsHeaderRow.Cells[paramsCellIndex].Format.Alignment = ParagraphAlignment.Center;
                _paramsHeaderRow.Cells[paramsCellIndex].Format.Font.Color = Color.Parse(color);
                _paramsHeaderRow.Cells[paramsCellIndex].AddParagraph(resultsCellIndex.ToString());

                var measureResultItemsWithMeanCursor = Task.Run(async () => await _measureResultDataCalculationService.GetMeasureResultDataAsync(measureResult, _meanResult.MeasureSetup)).Result.ToList();

                var sampleVolume =
                    $"{measureResult.MeasureSetup.Repeats} x {(int) measureResult.MeasureSetup.Volume} µl";
                AddParamsTableEntry("Sample Volume", sampleVolume, null, paramsCellIndex);

                var dilutionFactor = $"1 : {measureResult.MeasureSetup.DilutionFactor}";
                AddParamsTableEntry("Dilution Factor", dilutionFactor, null, paramsCellIndex);

                var capillary = $"{measureResult.MeasureSetup.CapillarySize} µm";
                AddParamsTableEntry("Capillary", capillary, null, paramsCellIndex);

                var smoothing = measureResult.MeasureSetup.IsSmoothing ? measureResult.MeasureSetup.SmoothingFactor.ToString(CultureInfo.InvariantCulture) : "Off";
                AddParamsTableEntry("Smoothing", smoothing, null, paramsCellIndex);

                var aggregationCorrection = measureResult.MeasureSetup.AggregationCalculationMode == AggregationCalculationModes.Manual ? $"Manual: {measureResult.MeasureSetup.ManualAggregationCalculationFactor} fl"
                    : measureResult.MeasureSetup.AggregationCalculationMode.ToString();
                AddParamsTableEntry("Aggr. Correction", aggregationCorrection, null, paramsCellIndex);

                var sizeScaleMin = $"{measureResult.MeasureSetup.FromDiameter.ToString()} µm";
                var sizeScaleMax = $"{measureResult.MeasureSetup.ToDiameter.ToString()} µm";
                AddParamsTableEntry("Size Scale", sizeScaleMin, sizeScaleMax, paramsCellIndex);

                if (_meanResult.MeasureSetup != null)
                {
                    foreach (var cursor in _meanResult.MeasureSetup.Cursors)
                    {
                        var minLimit = $"{cursor.MinLimit:0.00} µm";
                        var maxLimit = $"{cursor.MaxLimit:0.00} µm";
                        AddParamsTableEntry(LocalizationService.GetLocalizedString(cursor.Name), minLimit, maxLimit, paramsCellIndex, cursor);
                    }

                    measureResultItem = measureResult.MeasureResultItemsContainers[MeasureResultItemTypes.Concentration].MeasureResultItem;
                    if(measureResultItem != null)
                    { 
                        measureResultValue = measureResultItem == null ? "---" : measureResultItem.ResultItemValue == 0d ? "OK" : "TOO HIGH";
                        AddResultTableEntry(measureResultItem, resultsCellIndex, measureResultValue);
                    }

                    measureResultItem = measureResult.MeasureResultItemsContainers[MeasureResultItemTypes.Counts].MeasureResultItem;
                    if (measureResultItem != null)
                    {
                        AddResultTableEntry(measureResultItem, resultsCellIndex);
                    }

                    measureResultItem = measureResult.MeasureResultItemsContainers[MeasureResultItemTypes.CountsAboveDiameter].MeasureResultItem;
                    if (measureResultItem != null)
                    {
                        AddResultTableEntry(measureResultItem, resultsCellIndex);
                    }

                    //measureResultItem = measureResult.MeasureResultItemsContainers[MeasureResultItemTypes.TotalCountsPerMl].MeasureResultItem;
                    measureResultItem = measureResultItemsWithMeanCursor.FirstOrDefault(item => item.MeasureResultItemType == MeasureResultItemTypes.TotalCountsPerMl);
                    if (measureResultItem != null)
                    {
                        AddResultTableEntry(measureResultItem, resultsCellIndex);
                    }

                    if (_meanResult.MeasureSetup.MeasureMode == MeasureModes.Viability)
                    {
                        viableCursor = _meanResult.MeasureSetup.Cursors.FirstOrDefault(c => !c.IsDeadCellsCursor);
                        var deadCursor = _meanResult.MeasureSetup.Cursors.FirstOrDefault(c => c.IsDeadCellsCursor);

                        measureResultItem = measureResultItemsWithMeanCursor.FirstOrDefault(item => item.MeasureResultItemType == MeasureResultItemTypes.DebrisCount);
                        if (measureResultItem != null)
                        {
                            AddResultTableEntry(measureResultItem, resultsCellIndex);
                        }

                        measureResultItem = measureResultItemsWithMeanCursor.FirstOrDefault(item => item.Cursor != null && !item.Cursor.IsDeadCellsCursor && item.MeasureResultItemType == MeasureResultItemTypes.AggregationFactor);
                        if (measureResultItem != null)
                        {
                            measureResultValue = measureResultItem == null ? "---" : measureResultItem.ResultItemValue == 1.0 ? "Off" : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                            AddResultTableEntry(measureResultItem, resultsCellIndex, measureResultValue, viableCursor);
                        }

                        measureResultItem = measureResultItemsWithMeanCursor.FirstOrDefault(item => item.Cursor != null && !item.Cursor.IsDeadCellsCursor && item.MeasureResultItemType == MeasureResultItemTypes.ViableCellsPerMl);
                        if (measureResultItem != null)
                        {
                            AddResultTableEntry(measureResultItem, resultsCellIndex, cursor: viableCursor);
                        }

                        measureResultItem = measureResultItemsWithMeanCursor.FirstOrDefault(item => item.Cursor != null && item.Cursor.IsDeadCellsCursor && item.MeasureResultItemType == MeasureResultItemTypes.CountsPerMl);
                        if (measureResultItem != null)
                        {
                            AddResultTableEntry(measureResultItem, resultsCellIndex, cursor: deadCursor);
                        }

                        measureResultItem = measureResultItemsWithMeanCursor.FirstOrDefault(item => item.Cursor != null && !item.Cursor.IsDeadCellsCursor && item.MeasureResultItemType == MeasureResultItemTypes.Viability);
                        if (measureResultItem != null)
                        {
                            AddResultTableEntry(measureResultItem, resultsCellIndex, cursor: viableCursor);
                        }

                        measureResultItem = measureResultItemsWithMeanCursor.FirstOrDefault(item => item.Cursor != null && !item.Cursor.IsDeadCellsCursor && item.MeasureResultItemType == MeasureResultItemTypes.VolumePerMl);
                        if (measureResultItem != null)
                        {
                            AddResultTableEntry(measureResultItem, resultsCellIndex, cursor: viableCursor);
                        }

                        measureResultItem = measureResultItemsWithMeanCursor.FirstOrDefault(item => item.Cursor != null && !item.Cursor.IsDeadCellsCursor && item.MeasureResultItemType == MeasureResultItemTypes.MeanDiameter);
                        if (measureResultItem != null)
                        {
                            AddResultTableEntry(measureResultItem, resultsCellIndex, cursor: viableCursor);
                        }

                        measureResultItem = measureResultItemsWithMeanCursor.FirstOrDefault(item => item.Cursor != null && !item.Cursor.IsDeadCellsCursor && item.MeasureResultItemType == MeasureResultItemTypes.PeakDiameter);
                        if (measureResultItem != null)
                        {
                            AddResultTableEntry(measureResultItem, resultsCellIndex, cursor: viableCursor);
                        }

                        measureResultItem = measureResultItemsWithMeanCursor.FirstOrDefault(item => item.Cursor != null && !item.Cursor.IsDeadCellsCursor && item.MeasureResultItemType == MeasureResultItemTypes.MeanVolume);
                        if (measureResultItem != null)
                        {
                            AddResultTableEntry(measureResultItem, resultsCellIndex, cursor: viableCursor);
                        }

                        measureResultItem = measureResultItemsWithMeanCursor.FirstOrDefault(item => item.Cursor != null && !item.Cursor.IsDeadCellsCursor && item.MeasureResultItemType == MeasureResultItemTypes.PeakVolume);
                        if (measureResultItem != null)
                        {
                            AddResultTableEntry(measureResultItem, resultsCellIndex, cursor: viableCursor);
                        }
                    }
                    else
                    {
                        foreach (var cursor in _meanResult.MeasureSetup.Cursors)
                        {
                            measureResultItem = measureResultItemsWithMeanCursor.FirstOrDefault(item => item.Cursor != null && item.Cursor.Equals(cursor) && item.MeasureResultItemType == MeasureResultItemTypes.AggregationFactor);
                            if (measureResultItem != null)
                            {
                                measureResultValue = measureResultItem == null ? "---" : measureResultItem.ResultItemValue == 1.0 ? "Off" : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                                AddResultTableEntry(measureResultItem, resultsCellIndex, measureResultValue, cursor: cursor);
                            }

                            measureResultItem = measureResultItemsWithMeanCursor.FirstOrDefault(item => item.Cursor != null && item.Cursor.Equals(cursor) && item.MeasureResultItemType == MeasureResultItemTypes.TotalCountsPerMl);
                            if (measureResultItem != null)
                            {
                                AddResultTableEntry(measureResultItem, resultsCellIndex, cursor: cursor);
                            }

                            measureResultItem = measureResultItemsWithMeanCursor.FirstOrDefault(item => item.Cursor != null && item.Cursor.Equals(cursor) && item.MeasureResultItemType == MeasureResultItemTypes.CountsPercentage);
                            if (measureResultItem != null)
                            {
                                AddResultTableEntry(measureResultItem, resultsCellIndex, cursor: cursor);
                            }

                            measureResultItem = measureResultItemsWithMeanCursor.FirstOrDefault(item => item.Cursor != null && item.Cursor.Equals(cursor) && item.MeasureResultItemType == MeasureResultItemTypes.VolumePerMl);
                            if (measureResultItem != null)
                            {
                                AddResultTableEntry(measureResultItem, resultsCellIndex, cursor: cursor);
                            }

                            measureResultItem = measureResultItemsWithMeanCursor.FirstOrDefault(item => item.Cursor != null && item.Cursor.Equals(cursor) && item.MeasureResultItemType == MeasureResultItemTypes.MeanDiameter);
                            if (measureResultItem != null)
                            {
                                AddResultTableEntry(measureResultItem, resultsCellIndex, cursor: cursor);
                            }

                            measureResultItem = measureResultItemsWithMeanCursor.FirstOrDefault(item => item.Cursor != null && item.Cursor.Equals(cursor) && item.MeasureResultItemType == MeasureResultItemTypes.PeakDiameter);
                            if (measureResultItem != null)
                            {
                                AddResultTableEntry(measureResultItem, resultsCellIndex, cursor: cursor);
                            }

                            measureResultItem = measureResultItemsWithMeanCursor.FirstOrDefault(item => item.Cursor != null && item.Cursor.Equals(cursor) && item.MeasureResultItemType == MeasureResultItemTypes.MeanVolume);
                            if (measureResultItem != null)
                            {
                                AddResultTableEntry(measureResultItem, resultsCellIndex, cursor: cursor);
                            }

                            measureResultItem = measureResultItemsWithMeanCursor.FirstOrDefault(item => item.Cursor != null && item.Cursor.Equals(cursor) && item.MeasureResultItemType == MeasureResultItemTypes.PeakVolume);
                            if (measureResultItem != null)
                            {
                                AddResultTableEntry(measureResultItem, resultsCellIndex, cursor: cursor);
                            }
                        }
                    }
                }

                resultsCellIndex++;
                paramsCellIndex += 2;
            }

            //ICollection<MeasureResultItem> meanMeasureResultItems = _meanResult.MeasureResultItems;

            measureResultItem = _meanResult.MeasureResultItemsContainers[MeasureResultItemTypes.Concentration].MeasureResultItem;
            if (measureResultItem != null)
            {
                measureResultValue = measureResultItem == null ? "---" : measureResultItem.ResultItemValue == 0d ? "OK" : "TOO HIGH";
                AddResultTableEntry(measureResultItem, resultsCellIndex, measureResultValue);
            }

            measureResultItem = _meanResult.MeasureResultItemsContainers[MeasureResultItemTypes.Counts].MeasureResultItem;
            if (measureResultItem != null)
            {
                AddResultTableEntry(measureResultItem, resultsCellIndex);
                if (measureResultItem.Deviation.HasValue)
                {
                    measureResultValue = measureResultItem.Deviation.Value.ToString("0.##");
                    AddResultTableEntry(measureResultItem, resultsCellIndex + 1, measureResultValue);
                }
            }

            measureResultItem = _meanResult.MeasureResultItemsContainers[MeasureResultItemTypes.CountsAboveDiameter].MeasureResultItem;
            if (measureResultItem != null)
            {
                AddResultTableEntry(measureResultItem, resultsCellIndex);
                if (measureResultItem.Deviation.HasValue)
                {
                    measureResultValue = measureResultItem.Deviation.Value.ToString("0.##");
                    AddResultTableEntry(measureResultItem, resultsCellIndex + 1, measureResultValue);
                }
            }

            measureResultItem = _meanResult.MeasureResultItemsContainers[MeasureResultItemTypes.TotalCountsPerMl].MeasureResultItem;
            if (measureResultItem != null)
            {
                AddResultTableEntry(measureResultItem, resultsCellIndex);
                if (measureResultItem.Deviation.HasValue)
                {
                    measureResultValue = measureResultItem.Deviation.Value.ToString("0.##");
                    AddResultTableEntry(measureResultItem, resultsCellIndex + 1, measureResultValue);
                }
            }

            if (_meanResult.MeasureSetup.MeasureMode == MeasureModes.Viability)
            {
                viableCursor = _meanResult.MeasureSetup.Cursors.FirstOrDefault(c => !c.IsDeadCellsCursor);
                var deadCursor = _meanResult.MeasureSetup.Cursors.FirstOrDefault(c => c.IsDeadCellsCursor);

                measureResultItem = _meanResult.MeasureResultItemsContainers[MeasureResultItemTypes.DebrisCount].MeasureResultItem;
                if (measureResultItem != null)
                {
                    AddResultTableEntry(measureResultItem, resultsCellIndex);
                    if (measureResultItem.Deviation.HasValue)
                    {
                        measureResultValue = measureResultItem.Deviation.Value.ToString("0.##");
                        AddResultTableEntry(measureResultItem, resultsCellIndex + 1, measureResultValue);
                    }
                }

                measureResultItem = _meanResult.MeasureResultItemsContainers[MeasureResultItemTypes.AggregationFactor]
                    .CursorItems.FirstOrDefault(x => Equals(x.Cursor, viableCursor));
                if (measureResultItem != null)
                {
                    AddResultTableEntry(measureResultItem, resultsCellIndex, cursor: viableCursor);
                    if (measureResultItem.Deviation.HasValue)
                    {
                        measureResultValue = measureResultItem.Deviation.Value.ToString("0.##");
                        AddResultTableEntry(measureResultItem, resultsCellIndex + 1, measureResultValue, viableCursor);
                    }
                }

                measureResultItem = _meanResult.MeasureResultItemsContainers[MeasureResultItemTypes.ViableCellsPerMl]
                    .CursorItems.FirstOrDefault(x => Equals(x.Cursor, viableCursor));
                if (measureResultItem != null)
                {
                    AddResultTableEntry(measureResultItem, resultsCellIndex, cursor: viableCursor);
                    if (measureResultItem.Deviation.HasValue)
                    {
                        measureResultValue = measureResultItem.Deviation.Value.ToString("0.##");
                        AddResultTableEntry(measureResultItem, resultsCellIndex + 1, measureResultValue, viableCursor);
                    }
                }

                measureResultItem = _meanResult.MeasureResultItemsContainers[MeasureResultItemTypes.Viability]
                    .CursorItems.FirstOrDefault(x => Equals(x.Cursor, viableCursor));
                if (measureResultItem != null)
                {
                    AddResultTableEntry(measureResultItem, resultsCellIndex, cursor: viableCursor);
                    if (measureResultItem.Deviation.HasValue)
                    {
                        measureResultValue = measureResultItem.Deviation.Value.ToString("0.##");
                        AddResultTableEntry(measureResultItem, resultsCellIndex + 1, measureResultValue, viableCursor);
                    }
                }

                measureResultItem = _meanResult.MeasureResultItemsContainers[MeasureResultItemTypes.CountsPerMl]
                    .CursorItems.FirstOrDefault(x => Equals(x.Cursor, deadCursor));
                if (measureResultItem != null)
                {
                    AddResultTableEntry(measureResultItem, resultsCellIndex, cursor: deadCursor);
                    if (measureResultItem.Deviation.HasValue)
                    {
                        measureResultValue = measureResultItem.Deviation.Value.ToString("0.##");
                        AddResultTableEntry(measureResultItem, resultsCellIndex + 1, measureResultValue, deadCursor);
                    }
                }

                measureResultItem = _meanResult.MeasureResultItemsContainers[MeasureResultItemTypes.VolumePerMl]
                    .CursorItems.FirstOrDefault(x => Equals(x.Cursor, viableCursor));
                if (measureResultItem != null)
                {
                    AddResultTableEntry(measureResultItem, resultsCellIndex, cursor: viableCursor);
                    if (measureResultItem.Deviation.HasValue)
                    {
                        measureResultValue = measureResultItem.Deviation.Value.ToString("0.##");
                        AddResultTableEntry(measureResultItem, resultsCellIndex + 1, measureResultValue, viableCursor);
                    }
                }

                measureResultItem = _meanResult.MeasureResultItemsContainers[MeasureResultItemTypes.MeanDiameter]
                    .CursorItems.FirstOrDefault(x => Equals(x.Cursor, viableCursor));
                if (measureResultItem != null)
                {
                    AddResultTableEntry(measureResultItem, resultsCellIndex, cursor: viableCursor);
                    if (measureResultItem.Deviation.HasValue)
                    {
                        measureResultValue = measureResultItem.Deviation.Value.ToString("0.##");
                        AddResultTableEntry(measureResultItem, resultsCellIndex + 1, measureResultValue, viableCursor);
                    }
                }

                measureResultItem = _meanResult.MeasureResultItemsContainers[MeasureResultItemTypes.PeakDiameter]
                    .CursorItems.FirstOrDefault(x => Equals(x.Cursor, viableCursor));
                if (measureResultItem != null)
                {
                    AddResultTableEntry(measureResultItem, resultsCellIndex, cursor: viableCursor);
                    if (measureResultItem.Deviation.HasValue)
                    {
                        measureResultValue = measureResultItem.Deviation.Value.ToString("0.##");
                        AddResultTableEntry(measureResultItem, resultsCellIndex + 1, measureResultValue, viableCursor);
                    }
                }

                measureResultItem = _meanResult.MeasureResultItemsContainers[MeasureResultItemTypes.MeanVolume]
                    .CursorItems.FirstOrDefault(x => Equals(x.Cursor, viableCursor));
                if (measureResultItem != null)
                {
                    AddResultTableEntry(measureResultItem, resultsCellIndex, cursor: viableCursor);
                    if (measureResultItem.Deviation.HasValue)
                    {
                        measureResultValue = measureResultItem.Deviation.Value.ToString("0.##");
                        AddResultTableEntry(measureResultItem, resultsCellIndex + 1, measureResultValue, viableCursor);
                    }
                }

                measureResultItem = _meanResult.MeasureResultItemsContainers[MeasureResultItemTypes.PeakVolume]
                    .CursorItems.FirstOrDefault(x => Equals(x.Cursor, viableCursor));
                if (measureResultItem != null)
                {
                    AddResultTableEntry(measureResultItem, resultsCellIndex, cursor: viableCursor);
                    if (measureResultItem.Deviation.HasValue)
                    {
                        measureResultValue = measureResultItem.Deviation.Value.ToString("0.##");
                        AddResultTableEntry(measureResultItem, resultsCellIndex + 1, measureResultValue, viableCursor);
                    }
                }
            }
            else
            {
                foreach (var cursor in _meanResult.MeasureSetup.Cursors)
                {
                    measureResultItem = _meanResult.MeasureResultItemsContainers[MeasureResultItemTypes.AggregationFactor]
                        .CursorItems.FirstOrDefault(x => Equals(x.Cursor, cursor));
                    if (measureResultItem != null)
                    {
                        AddResultTableEntry(measureResultItem, resultsCellIndex, cursor: cursor);
                        if (measureResultItem.Deviation.HasValue)
                        {
                            measureResultValue = measureResultItem.Deviation.Value.ToString("0.##");
                            AddResultTableEntry(measureResultItem, resultsCellIndex + 1, measureResultValue, cursor);
                        }
                    }

                    measureResultItem = _meanResult.MeasureResultItemsContainers[MeasureResultItemTypes.TotalCountsPerMl]
                        .CursorItems.FirstOrDefault(x => Equals(x.Cursor, cursor));
                    if (measureResultItem != null)
                    {
                        AddResultTableEntry(measureResultItem, resultsCellIndex, cursor: cursor);
                        if (measureResultItem.Deviation.HasValue)
                        {
                            measureResultValue = measureResultItem.Deviation.Value.ToString("0.##");
                            AddResultTableEntry(measureResultItem, resultsCellIndex + 1, measureResultValue, cursor);
                        }
                    }

                    measureResultItem = _meanResult.MeasureResultItemsContainers[MeasureResultItemTypes.CountsPercentage]
                        .CursorItems.FirstOrDefault(x => Equals(x.Cursor, cursor));
                    if (measureResultItem != null)
                    {
                        AddResultTableEntry(measureResultItem, resultsCellIndex, cursor: cursor);
                        if (measureResultItem.Deviation.HasValue)
                        {
                            measureResultValue = measureResultItem.Deviation.Value.ToString("0.##");
                            AddResultTableEntry(measureResultItem, resultsCellIndex + 1, measureResultValue, cursor);
                        }
                    }

                    measureResultItem = _meanResult.MeasureResultItemsContainers[MeasureResultItemTypes.MeanDiameter]
                        .CursorItems.FirstOrDefault(x => Equals(x.Cursor, cursor));
                    if (measureResultItem != null)
                    {
                        AddResultTableEntry(measureResultItem, resultsCellIndex, cursor: cursor);
                        if (measureResultItem.Deviation.HasValue)
                        {
                            measureResultValue = measureResultItem.Deviation.Value.ToString("0.##");
                            AddResultTableEntry(measureResultItem, resultsCellIndex + 1, measureResultValue, cursor);
                        }
                    }

                    measureResultItem = _meanResult.MeasureResultItemsContainers[MeasureResultItemTypes.PeakDiameter]
                        .CursorItems.FirstOrDefault(x => Equals(x.Cursor, cursor));
                    if (measureResultItem != null)
                    {
                        AddResultTableEntry(measureResultItem, resultsCellIndex, cursor: cursor);
                        if (measureResultItem.Deviation.HasValue)
                        {
                            measureResultValue = measureResultItem.Deviation.Value.ToString("0.##");
                            AddResultTableEntry(measureResultItem, resultsCellIndex + 1, measureResultValue, cursor);
                        }
                    }

                    measureResultItem = _meanResult.MeasureResultItemsContainers[MeasureResultItemTypes.MeanVolume]
                        .CursorItems.FirstOrDefault(x => Equals(x.Cursor, cursor));
                    if (measureResultItem != null)
                    {
                        AddResultTableEntry(measureResultItem, resultsCellIndex, cursor: cursor);
                        if (measureResultItem.Deviation.HasValue)
                        {
                            measureResultValue = measureResultItem.Deviation.Value.ToString("0.##");
                            AddResultTableEntry(measureResultItem, resultsCellIndex + 1, measureResultValue, cursor);
                        }
                    }

                    measureResultItem = _meanResult.MeasureResultItemsContainers[MeasureResultItemTypes.PeakVolume]
                        .CursorItems.FirstOrDefault(x => Equals(x.Cursor, cursor));
                    if (measureResultItem != null)
                    {
                        AddResultTableEntry(measureResultItem, resultsCellIndex, cursor: cursor);
                        if (measureResultItem.Deviation.HasValue)
                        {
                            measureResultValue = measureResultItem.Deviation.Value.ToString("0.##");
                            AddResultTableEntry(measureResultItem, resultsCellIndex + 1, measureResultValue, cursor);
                        }
                    }
                }
            }
        }

        private void AddResultTableEntry(MeasureResultItem measureResultItem, int resultsCellIndex, string measureResultItemValue = null, Cursor cursor = null)
        {
            if (measureResultItem == null) return;

            var key = _resultsTableRows.Keys.FirstOrDefault(k => k.Item1 == measureResultItem.MeasureResultItemType && (cursor == null && k.Item2 == null || k.Item2 != null && k.Item2.Equals(cursor)));

            if (key == null) return;

            _resultsTableRows[key].VerticalAlignment = VerticalAlignment.Center;
            _resultsTableRows[key].Cells[resultsCellIndex].Format.Alignment = ParagraphAlignment.Center;

            if (string.IsNullOrEmpty(measureResultItemValue))
            {
                var paragraphText = measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                _resultsTableRows[key].Cells[resultsCellIndex].AddParagraph(paragraphText);
            }
            else
            {
                _resultsTableRows[key].Cells[resultsCellIndex].AddParagraph(measureResultItemValue);
            }
        }

        private void AddParamsTableEntry(string key, string value, string value2, int paramsCellIndex, Cursor cursor = null)
        {
            var paramKey = _paramsTableRows.Keys.FirstOrDefault(k => k.Item1 == key && (cursor == null && k.Item2 == null || k.Item2 != null && k.Item2.Equals(cursor)));

            if (paramKey == null) return;

            _paramsTableRows[paramKey].VerticalAlignment = VerticalAlignment.Center;
            _paramsTableRows[paramKey].Cells[paramsCellIndex].Format.Alignment = ParagraphAlignment.Center;
            _paramsTableRows[paramKey].Cells[paramsCellIndex].AddParagraph(value);

            if (string.IsNullOrEmpty(value2))
            {
                _paramsTableRows[paramKey].Cells[paramsCellIndex].MergeRight = 1;
            }
            else
            {
                _paramsTableRows[paramKey].Cells[paramsCellIndex + 1].Format.Alignment = ParagraphAlignment.Center;
                _paramsTableRows[paramKey].Cells[paramsCellIndex + 1].AddParagraph(value2);
            }
        }

        private void AddParametersRow(string cell0Value, Cursor cursor = null)
        {
            var row = _paramsTable.AddRow();
            row.Cells[0].Format.Alignment = ParagraphAlignment.Left;
            row.Cells[0].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[0].AddParagraph(cell0Value);
            _paramsTableRows.Add(new Tuple<string, Cursor>(cell0Value, cursor), row);
        }

        private void AddResultsRow(string cell0Value, MeasureResultItemTypes measureResultItemType,
            Cursor cursor = null)
        {
            var row = _resultsTable.AddRow();
            row.Cells[0].Format.Alignment = ParagraphAlignment.Left;
            row.Cells[0].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[0].AddParagraph(cell0Value);
            _resultsTableRows.Add(new Tuple<MeasureResultItemTypes, Cursor>(measureResultItemType, cursor), row);
        }
    }
}
