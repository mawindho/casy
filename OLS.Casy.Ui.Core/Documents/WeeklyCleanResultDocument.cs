using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Models;
using OLS.Casy.Models.Enums;
using OLS.Casy.Ui.Core.Api;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OLS.Casy.Core.Api;

namespace OLS.Casy.Ui.Core.Documents
{
    public class WeeklyCleanResultDocument : DocumentBase
    {
        private MeasureResult _measureResult;

        private Paragraph _experimentGroupSection;
        private Paragraph _templateSection;
        private Paragraph _commentSection;
        private Table _paramTable;
        private Table _resultsTable;
        private Paragraph _measuredAtSection;
        private Paragraph _measuredBySection;

        public WeeklyCleanResultDocument(ILocalizationService localizationService, IAuthenticationService authenticationService,
            IDocumentSettingsManager documentSettingsManager, IEnvironmentService environmentService)
            : base(localizationService, authenticationService, documentSettingsManager, environmentService)
        {
        }

        public Document CreateDocument(MeasureResult measureResult)
        {
            _measureResult = measureResult;

            Document = new Document();

            DefineStyles();
            CreatePage(Orientation.Portrait);
            FillContent();

            return Document;
        }

        protected void CreatePage(Orientation orientation)
        {
            base.CreatePage(orientation, _measureResult.CreatedBy);

            _experimentGroupSection = Section.AddParagraph();
            _experimentGroupSection.Format.Font.Color = Colors.Black;
            _experimentGroupSection.Format.Font.Name = "Dosis-Light";
            _experimentGroupSection.Format.Font.Size = 9;
            _experimentGroupSection.Format.LineSpacing = 10.8;
            _experimentGroupSection.Format.LineSpacingRule = LineSpacingRule.AtLeast;

            _templateSection = Section.AddParagraph();
            _templateSection.Format.Font.Color = Colors.Black;
            _templateSection.Format.Font.Name = "Dosis-Light";
            _templateSection.Format.Font.Size = 9;
            _templateSection.Format.LineSpacing = 10.8;
            _templateSection.Format.LineSpacingRule = LineSpacingRule.AtLeast;

            _measuredAtSection = Section.AddParagraph();
            _measuredAtSection.Format.Font.Color = Colors.Black;
            _measuredAtSection.Format.Font.Name = "Dosis-Light";
            _measuredAtSection.Format.Font.Size = 9;
            _measuredAtSection.Format.LineSpacing = 10.8;
            _measuredAtSection.Format.LineSpacingRule = LineSpacingRule.AtLeast;

            _measuredBySection = Section.AddParagraph();
            _measuredBySection.Format.Font.Color = Colors.Black;
            _measuredBySection.Format.Font.Name = "Dosis-Light";
            _measuredBySection.Format.Font.Size = 9;
            _measuredBySection.Format.LineSpacing = 10.8;
            _measuredBySection.Format.LineSpacingRule = LineSpacingRule.AtLeast;

            var paragraph = Section.AddParagraph();
            paragraph.Format.Font.Color = Colors.Black;
            paragraph.Format.Font.Name = "Dosis-Regular";
            paragraph.Format.Font.Size = 9;
            paragraph.Format.LineSpacing = 10.8;
            paragraph.AddText(LocalizationService.GetLocalizedString("SingleMeasureResultDocument_Heading_Comment"));

            _commentSection = Section.AddParagraph();
            _commentSection.Format.Font.Color = Colors.Black;
            _commentSection.Format.Font.Name = "Dosis-Light";
            _commentSection.Format.Font.Size = 9;
            _commentSection.Format.LineSpacing = 10.8;
            _commentSection.Format.SpaceAfter = "1cm";

            _paramTable = Section.AddTable();
            _paramTable.Format.Font.Name = "Dosis-Light";
            _paramTable.Format.Font.Size = 7;
            _paramTable.Borders.Width = "0.01cm";
            _paramTable.Borders.Color = Colors.Black;

            var column = _paramTable.AddColumn("3cm");
            column.Format.LeftIndent = "0.2cm";
            column.Format.Alignment = ParagraphAlignment.Left;
            column = _paramTable.AddColumn("1.5cm");
            column.Format.Alignment = ParagraphAlignment.Center;
            column = _paramTable.AddColumn("1.5cm");
            column.Format.Alignment = ParagraphAlignment.Center;

            var row = _paramTable.AddRow();
            row.HeadingFormat = true;
            row.Format.Font.Name = "Dosis-Regular";
            row.Height = "0.45cm";
            row.Cells[0].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[0].AddParagraph(LocalizationService.GetLocalizedString("SingleMeasureResultDocument_Heading_Parameter"));
            row.Cells[1].MergeRight = 1;
            row.Cells[1].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[1].AddParagraph(_measureResult.Name.ToUpper());

            paragraph = Section.AddParagraph();
            paragraph.AddSpace(1);

            _resultsTable = Section.AddTable();
            _resultsTable.Format.Font.Name = "Dosis-Light";
            _resultsTable.Format.Font.Size = 7;
            _resultsTable.Borders.Width = "0.01cm";
            _resultsTable.Borders.Color = Colors.Black;

            column = _resultsTable.AddColumn("3cm");
            column.Format.LeftIndent = "0.2cm";
            column.Format.Alignment = ParagraphAlignment.Left;
            column = _resultsTable.AddColumn("3cm");
            column.Format.Alignment = ParagraphAlignment.Center;

            row = _resultsTable.AddRow();
            row.HeadingFormat = true;
            row.Format.Font.Name = "Dosis-Regular";
            row.Height = "0.45cm";
            row.Cells[0].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[0].AddParagraph(LocalizationService.GetLocalizedString("SingleMeasureResultDocument_Heading_Results"));
            row.Cells[1].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[1].AddParagraph(_measureResult.Name.ToUpper());
        }

        private void FillContent()
        {
            var measureSetup = _measureResult?.MeasureSetup;

            var paragraph = TitleFrame.AddParagraph();
            paragraph.Format.Font.Name = "Dosis-Regular";
            paragraph.Format.Font.Size = 9;
            paragraph.Format.Font.Color = Colors.White;
            paragraph.Format.Alignment = ParagraphAlignment.Center;
            paragraph.AddText("WEEKLY CLEAN RESULT");

            NameSection.AddText(_measureResult.Name);

            var experiment = string.IsNullOrEmpty(_measureResult.Experiment) ? LocalizationService.GetLocalizedString("MeasureResultTreeView_GroupNode_NoExperiment") : _measureResult.Experiment;
            var group = string.IsNullOrEmpty(_measureResult.Group) ? LocalizationService.GetLocalizedString("MeasureResultTreeView_GroupNode_NoGroup") : _measureResult.Group;
            var experimentAndGroup = $"Experiment: {experiment} / Group: {group}";
            _experimentGroupSection.AddText(experimentAndGroup);

            var templateName =
                $"Template: {(string.IsNullOrEmpty(measureSetup.Name) ? "Individual measurement" : measureSetup.Name)}";
            _templateSection.AddText(templateName);

            var timeZone = _measureResult.MeasuredAtTimeZone == null ? TimeZoneInfo.Local : _measureResult.MeasuredAtTimeZone;

            var isDaylightSaving = timeZone.IsDaylightSavingTime(_measureResult.MeasuredAt);

            if (isDaylightSaving)
            {
                var split = timeZone.DisplayName.Split(':');

                if (split[0].Contains("+"))
                {
                    var hours = (int.Parse(split[0].Split('+')[1]) + 1).ToString("D2");
                    var timeZoneString = $"(UTC+{hours}:{split[1]}";
                    //_measuredAtSection.AddText($"Measurement Date: {_measureResult.MeasuredAt:dd.MM.yyyy HH:mm:ss} {timeZoneString}");
                    _measuredAtSection.AddText($"Measurement Date: {EnvironmentService.GetDateTimeString(_measureResult.MeasuredAt, true)} {timeZoneString}");
                }
                else
                {
                    var hours = (int.Parse(split[0].Split('-')[1]) + 1).ToString("D2");
                    var timeZoneString = $"(UTC-{hours}:{split[1]}";
                    //_measuredAtSection.AddText($"Measurement Date: {_measureResult.MeasuredAt:dd.MM.yyyy HH:mm:ss} {timeZoneString}");
                    _measuredAtSection.AddText($"Measurement Date: {EnvironmentService.GetDateTimeString(_measureResult.MeasuredAt, true)} {timeZoneString}");
                }
            }
            else
            {
                //_measuredAtSection.AddText($"Measurement Date: {_measureResult.MeasuredAt:dd.MM.yyyy HH:mm:ss} {timeZone.DisplayName}");
                _measuredAtSection.AddText($"Measurement Date: {EnvironmentService.GetDateTimeString(_measureResult.MeasuredAt, true)} {timeZone.DisplayName}");
            }

            _measuredBySection.AddText($"Measured by: {_measureResult.CreatedBy}");

            var comment = string.IsNullOrEmpty(_measureResult.Comment) ? "No Comment" : _measureResult.Comment;
            _commentSection.AddText(comment);

            var documentSettings = DocumentSettingsManager.SingleDocumentSettings;
            var paramSettings = documentSettings.Where(item => item.IsSelected && item.Category == DocumentSettingCategory.Parameter)
                .OrderBy(item => item.DisplayOrder);
            foreach (var paramSetting in paramSettings)
            {
                switch (paramSetting.Type)
                {
                    case DocumentSettingType.SampleVolume:
                        AddParametersRow(paramSetting.Name, $"{measureSetup.Repeats} x {(int)measureSetup.Volume} µl");
                        break;
                    case DocumentSettingType.DilutionFactor:
                        AddParametersRow(paramSetting.Name, $"1 : {measureSetup.DilutionFactor}");
                        break;
                    case DocumentSettingType.Capillary:
                        AddParametersRow(paramSetting.Name, $"{measureSetup.CapillarySize} µm");
                        break;
                    case DocumentSettingType.Smoothing:
                        if (!_measureResult.IsBackground)
                        {
                            AddParametersRow(paramSetting.Name, measureSetup.IsSmoothing ? measureSetup.SmoothingFactor.ToString(CultureInfo.InvariantCulture) : "Off");
                        }
                        break;
                    case DocumentSettingType.AggrCorrection:
                        if (!_measureResult.IsBackground)
                        {
                            AddParametersRow(paramSetting.Name, measureSetup.AggregationCalculationMode == AggregationCalculationModes.Manual ? $"Manual: {measureSetup.ManualAggregationCalculationFactor} fl"
                                : measureSetup.AggregationCalculationMode.ToString());
                        }
                        break;
                    case DocumentSettingType.SizeScale:
                        AddParametersRow(paramSetting.Name, $"{measureSetup.FromDiameter.ToString()} µm", $"{measureSetup.ToDiameter.ToString()} µm");
                        break;
                    case DocumentSettingType.Ranges:
                        var index = 0;
                        foreach (var cursor in measureSetup.Cursors)
                        {
                            var row = AddParametersRow(LocalizationService.GetLocalizedString(cursor.Name), $"{cursor.MinLimit:0.00} µm", $"{cursor.MaxLimit:0.00} µm");
                            var cursorBrush = System.Windows.Application.Current.Resources[$"StripBorderColor{index + 1}"] as System.Windows.Media.SolidColorBrush;
                            row.Cells[0].Format.Font.Color = Color.FromArgb(cursorBrush.Color.A, cursorBrush.Color.R, cursorBrush.Color.G, cursorBrush.Color.B);
                            index++;
                        }
                        break;
                }
            }

            var isViability = _measureResult.MeasureSetup.MeasureMode == MeasureModes.Viability;
            Cursor viableCursor = null, deadCursor = null;
            if (isViability)
            {
                viableCursor = measureSetup.Cursors.FirstOrDefault(c => !c.IsDeadCellsCursor);
                deadCursor = measureSetup.Cursors.FirstOrDefault(c => c.IsDeadCellsCursor);
            }

            var resultSettingsStack = documentSettings.Where(item => item.IsSelected && item.Category == DocumentSettingCategory.Result)
                .OrderBy(item => item.DisplayOrder).ToList();

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

                        foreach (var cursor in measureSetup.Cursors)
                        {
                            foreach (var resultSetting in resultSettings)
                            {
                                switch (resultSetting.Type)
                                {
                                    case DocumentSettingType.CountsMl:
                                        var measureResultItem = _measureResult
                                            .MeasureResultItemsContainers[MeasureResultItemTypes.CountsPerMl]
                                            .CursorItems
                                            .FirstOrDefault(x => Equals(x.Cursor, cursor));
                                        var measureResultValue = measureResultItem == null
                                            ? "---"
                                            : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                                        AddResultsRow(
                                            $"Counts/ml ({LocalizationService.GetLocalizedString(cursor.Name)})",
                                            measureResultValue);
                                        break;
                                    case DocumentSettingType.CellsPercentage:
                                        measureResultItem = _measureResult
                                            .MeasureResultItemsContainers[MeasureResultItemTypes.CountsPercentage]
                                            .CursorItems.FirstOrDefault(x => Equals(x.Cursor, cursor));
                                        measureResultValue = measureResultItem == null
                                            ? "---"
                                            : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                                        AddResultsRow($"% Cells ({cursor.Name})", measureResultValue);
                                        break;
                                    case DocumentSettingType.AggrFactor:
                                        measureResultItem = _measureResult
                                            .MeasureResultItemsContainers[MeasureResultItemTypes.AggregationFactor]
                                            .CursorItems.FirstOrDefault(x => Equals(x.Cursor, cursor));
                                        measureResultValue = measureResultItem == null
                                            ? "---"
                                            : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                                        AddResultsRow($"Aggr. Factor ({cursor.Name})", measureResultValue);
                                        break;
                                    case DocumentSettingType.VolumeMl:
                                        measureResultItem = _measureResult
                                            .MeasureResultItemsContainers[MeasureResultItemTypes.VolumePerMl]
                                            .CursorItems
                                            .FirstOrDefault(x => Equals(x.Cursor, cursor));
                                        measureResultValue = measureResultItem == null
                                            ? "---"
                                            : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                                        AddResultsRow($"Volume/ml ({cursor.Name})", measureResultValue);
                                        break;
                                    case DocumentSettingType.MeanDiameter:
                                        measureResultItem = _measureResult
                                            .MeasureResultItemsContainers[MeasureResultItemTypes.MeanDiameter]
                                            .CursorItems
                                            .FirstOrDefault(x => Equals(x.Cursor, cursor));
                                        measureResultValue = measureResultItem == null
                                            ? "---"
                                            : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                                        AddResultsRow($"Mean Diameter ({cursor.Name})", measureResultValue);
                                        break;
                                    case DocumentSettingType.PeakDiameter:
                                        measureResultItem = _measureResult
                                            .MeasureResultItemsContainers[MeasureResultItemTypes.PeakDiameter]
                                            .CursorItems
                                            .FirstOrDefault(x => Equals(x.Cursor, cursor));
                                        measureResultValue = measureResultItem == null
                                            ? "---"
                                            : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                                        AddResultsRow($"Peak Diameter ({cursor.Name})", measureResultValue);
                                        break;
                                    case DocumentSettingType.MeanVolume:
                                        measureResultItem = _measureResult
                                            .MeasureResultItemsContainers[MeasureResultItemTypes.MeanVolume].CursorItems
                                            .FirstOrDefault(x => Equals(x.Cursor, cursor));
                                        measureResultValue = measureResultItem == null
                                            ? "---"
                                            : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                                        AddResultsRow($"Mean Volume ({cursor.Name})", measureResultValue);
                                        break;
                                    case DocumentSettingType.PeakVolume:
                                        measureResultItem = _measureResult
                                            .MeasureResultItemsContainers[MeasureResultItemTypes.PeakVolume].CursorItems
                                            .FirstOrDefault(x => Equals(x.Cursor, cursor));
                                        measureResultValue = measureResultItem == null
                                            ? "---"
                                            : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                                        AddResultsRow($"Peak Volume ({cursor.Name})", measureResultValue);
                                        break;
                                }
                                resultSettingsStack.Remove(resultSetting);
                            }
                        }
                    }
                    else
                    {
                        var measureResultItem = _measureResult
                            .MeasureResultItemsContainers[MeasureResultItemTypes.CountsPerMl].MeasureResultItem;
                        var measureResultValue = measureResultItem == null
                            ? "---"
                            : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                        AddResultsRow("Total Counts/ml", measureResultValue);
                    }
                }
                else
                {
                    switch (currentResultSetting.Type)
                    {
                        case DocumentSettingType.Concentration:
                            var measureResultItem = _measureResult
                                .MeasureResultItemsContainers[MeasureResultItemTypes.Concentration]
                                .MeasureResultItem;
                            var measureResultValue = measureResultItem == null ? "---" :
                                measureResultItem.ResultItemValue == 0d ? "OK" : "TOO HIGH";
                            AddResultsRow(currentResultSetting.Name, measureResultValue);
                            break;
                        case DocumentSettingType.Counts:
                            measureResultItem = _measureResult
                                .MeasureResultItemsContainers[MeasureResultItemTypes.Counts].MeasureResultItem;
                            measureResultValue = measureResultItem == null
                                ? "---"
                                : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                            AddResultsRow(currentResultSetting.Name, measureResultValue);
                            break;
                        case DocumentSettingType.ViableCellsMl:
                            if (isViability)
                            {
                                measureResultItem = _measureResult
                                    .MeasureResultItemsContainers[MeasureResultItemTypes.ViableCellsPerMl]
                                    .CursorItems
                                    .FirstOrDefault(x => Equals(x.Cursor, viableCursor));
                                measureResultValue = measureResultItem == null
                                    ? "---"
                                    : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                                AddResultsRow(currentResultSetting.Name, measureResultValue);
                            }

                            break;
                        case DocumentSettingType.DeadCellsMl:
                            if (isViability)
                            {
                                measureResultItem = _measureResult
                                    .MeasureResultItemsContainers[MeasureResultItemTypes.CountsPerMl].CursorItems
                                    .FirstOrDefault(x => Equals(x.Cursor, deadCursor));
                                measureResultValue = measureResultItem == null
                                    ? "---"
                                    : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                                AddResultsRow(currentResultSetting.Name, measureResultValue);
                            }

                            break;
                        case DocumentSettingType.TotalCellsMl:
                            if (isViability)
                            {
                                measureResultItem = _measureResult
                                    .MeasureResultItemsContainers[MeasureResultItemTypes.TotalCountsPerMl]
                                    .MeasureResultItem;
                                measureResultValue = measureResultItem == null
                                    ? "---"
                                    : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                                AddResultsRow(currentResultSetting.Name, measureResultValue);
                            }

                            break;
                        case DocumentSettingType.Viability:
                            if (isViability)
                            {
                                measureResultItem = _measureResult
                                    .MeasureResultItemsContainers[MeasureResultItemTypes.Viability].CursorItems
                                    .FirstOrDefault(x => Equals(x.Cursor, viableCursor));
                                measureResultValue = measureResultItem == null
                                    ? "---"
                                    : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                                AddResultsRow(currentResultSetting.Name, measureResultValue);
                            }

                            break;
                        case DocumentSettingType.ViableAggrFactor:
                            if (isViability)
                            {
                                measureResultItem = _measureResult
                                    .MeasureResultItemsContainers[MeasureResultItemTypes.AggregationFactor]
                                    .CursorItems
                                    .FirstOrDefault(x => Equals(x.Cursor, viableCursor));
                                measureResultValue = measureResultItem == null
                                    ? "---"
                                    : (measureResultItem.ResultItemValue == 1.0
                                        ? "Off"
                                        : measureResultItem.ResultItemValue.ToString(measureResultItem
                                            .ValueFormat));
                                AddResultsRow(currentResultSetting.Name, measureResultValue);
                            }

                            break;
                        case DocumentSettingType.DebrisMl:
                            if (isViability)
                            {
                                measureResultItem = _measureResult
                                    .MeasureResultItemsContainers[MeasureResultItemTypes.DebrisCount]
                                    .MeasureResultItem;
                                measureResultValue = measureResultItem == null
                                    ? "---"
                                    : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                                AddResultsRow(currentResultSetting.Name, measureResultValue);
                            }

                            break;


                        case DocumentSettingType.CountsMlAboveRange:
                            measureResultItem = _measureResult
                                .MeasureResultItemsContainers[MeasureResultItemTypes.CountsAboveDiameter]
                                .MeasureResultItem;
                            measureResultValue = measureResultItem == null
                                ? "---"
                                : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                            AddResultsRow($"Counts/ml > {measureSetup.ToDiameter} µm", measureResultValue);
                            break;
                        case DocumentSettingType.VolumeMlViable:
                            if (isViability)
                            {
                                measureResultItem = _measureResult
                                    .MeasureResultItemsContainers[MeasureResultItemTypes.VolumePerMl].CursorItems
                                    .FirstOrDefault(x => Equals(x.Cursor, viableCursor));
                                measureResultValue = measureResultItem == null
                                    ? "---"
                                    : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                                AddResultsRow("Volume/ml", measureResultValue);
                            }

                            break;
                        case DocumentSettingType.MeanDiameterViable:
                            if (isViability)
                            {
                                measureResultItem = _measureResult
                                    .MeasureResultItemsContainers[MeasureResultItemTypes.MeanDiameter].CursorItems
                                    .FirstOrDefault(x => Equals(x.Cursor, viableCursor));
                                measureResultValue = measureResultItem == null
                                    ? "---"
                                    : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                                AddResultsRow("Mean Diameter", measureResultValue);
                            }

                            break;
                        case DocumentSettingType.PeakDiameterViable:
                            if (isViability)
                            {
                                measureResultItem = _measureResult
                                    .MeasureResultItemsContainers[MeasureResultItemTypes.PeakDiameter].CursorItems
                                    .FirstOrDefault(x => Equals(x.Cursor, viableCursor));
                                measureResultValue = measureResultItem == null
                                    ? "---"
                                    : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                                AddResultsRow("Peak Diameter", measureResultValue);
                            }

                            break;
                        case DocumentSettingType.MeanVolumeViable:
                            if (isViability)
                            {
                                measureResultItem = _measureResult
                                    .MeasureResultItemsContainers[MeasureResultItemTypes.MeanVolume].CursorItems
                                    .FirstOrDefault(x => Equals(x.Cursor, viableCursor));
                                measureResultValue = measureResultItem == null
                                    ? "---"
                                    : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                                AddResultsRow("Mean Volume", measureResultValue);
                            }

                            break;
                        case DocumentSettingType.PeakVolumeViable:
                            if (isViability)
                            {
                                measureResultItem = _measureResult
                                    .MeasureResultItemsContainers[MeasureResultItemTypes.PeakVolume].CursorItems
                                    .FirstOrDefault(x => Equals(x.Cursor, viableCursor));
                                measureResultValue = measureResultItem == null
                                    ? "---"
                                    : measureResultItem.ResultItemValue.ToString(measureResultItem.ValueFormat);
                                AddResultsRow("Peak Volume", measureResultValue);
                            }

                            break;
                    }
                }
            }
        }

        private Row AddParametersRow(string cell0Value, string cell1Value, string cell2Value = null)
        {
            var row = _paramTable.AddRow();
            row.Height = "0.45cm";
            row.Cells[0].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[1].VerticalAlignment = VerticalAlignment.Center;

            row.Cells[0].AddParagraph(cell0Value);
            row.Cells[1].AddParagraph(cell1Value);

            if (string.IsNullOrEmpty(cell2Value))
            {
                row.Cells[1].MergeRight = 1;
            }
            else
            {
                row.Cells[2].VerticalAlignment = VerticalAlignment.Center;
                row.Cells[2].AddParagraph(cell2Value);
            }

            return row;
        }

        private void AddResultsRow(string cell0Value, string cell1Value)
        {
            var row = _resultsTable.AddRow();
            row.Height = "0.45cm";
            row.Cells[0].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[1].VerticalAlignment = VerticalAlignment.Center;

            row.Cells[0].AddParagraph(cell0Value);
            row.Cells[1].AddParagraph(cell1Value);
        }
    }
}
