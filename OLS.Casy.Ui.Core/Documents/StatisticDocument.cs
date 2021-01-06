using MigraDoc.DocumentObjectModel;
using OLS.Casy.Controller.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Models;
using OLS.Casy.Ui.Core.Api;
using System.Linq;
using MigraDoc.DocumentObjectModel.Tables;
using OLS.Casy.Core.Api;

namespace OLS.Casy.Ui.Core.Documents
{
    public class StatisticDocument : DocumentBase
    {
        private Statistic _statistic;
        private string _serialNumber;
        private IErrorContoller _errorController;

        private Paragraph[] _capillaryStatisticParagraphs;
        private Table[] _capillaryMeasStatsTable;

        private Table _errorStatisticTable;
        private Paragraph _generalStatisticsParagraph;

        public StatisticDocument(ILocalizationService localizationService, IAuthenticationService authenticationService,
            IDocumentSettingsManager documentSettingsManager, IEnvironmentService environmentService)
            : base(localizationService, authenticationService, documentSettingsManager, environmentService)
        {
        }

        public Document CreateDocument(Statistic statistic, string serialNumber, IErrorContoller errorController)
        {
            _statistic = statistic;
            _serialNumber = serialNumber;
            _errorController = errorController;

            Document = new Document();

            DefineStyles();
            CreatePage(Orientation.Portrait);
            FillContent();

            return Document;
        }

        protected void CreatePage(Orientation orientation)
        {
            base.CreatePage(orientation,
                $"{AuthenticationService.LoggedInUser.FirstName} {AuthenticationService.LoggedInUser.LastName} ({AuthenticationService.LoggedInUser.Identity.Name})");

            _generalStatisticsParagraph = Section.AddParagraph();
            _generalStatisticsParagraph.Format.Font.Color = Colors.Black;
            _generalStatisticsParagraph.Format.Font.Name = "Dosis-Light";
            _generalStatisticsParagraph.Format.Font.Size = 7;
            _generalStatisticsParagraph.Format.LineSpacing = 10.8;
            _generalStatisticsParagraph.Format.SpaceBefore = "0.2cm";

            var capillariesHeaderParagraph = Section.AddParagraph();
            capillariesHeaderParagraph.Format.Font.Color = Colors.Black;
            capillariesHeaderParagraph.Format.Font.Name = "Dosis-Light";
            capillariesHeaderParagraph.Format.Font.Size = 10;
            capillariesHeaderParagraph.Format.LineSpacing = 10.8;
            capillariesHeaderParagraph.Format.SpaceBefore = "1cm";
            capillariesHeaderParagraph.AddText("Capillary statistics");

            _capillaryStatisticParagraphs = new Paragraph[_statistic.CapillaryStatistics.Count];
            _capillaryMeasStatsTable = new Table[_statistic.CapillaryStatistics.Count * 2];

            Column column;
            Row row;

            for (int i = 0, j = 0; i < _statistic.CapillaryStatistics.Count; i++, j+=2)
            {
                var headerParagraph = Section.AddParagraph();
                headerParagraph.Format.Font.Color = Colors.Black;
                headerParagraph.Format.Font.Name = "Dosis-Light";
                headerParagraph.Format.Font.Size = 9;
                headerParagraph.Format.LineSpacing = 10.8;
                headerParagraph.Format.SpaceBefore = "0.2cm";
                headerParagraph.AddText($"Capillary {i + 1}");

                _capillaryStatisticParagraphs[i] = Section.AddParagraph();
                _capillaryStatisticParagraphs[i].Format.Font.Color = Colors.Black;
                _capillaryStatisticParagraphs[i].Format.Font.Name = "Dosis-Light";
                _capillaryStatisticParagraphs[i].Format.Font.Size = 7;
                _capillaryStatisticParagraphs[i].Format.LineSpacing = 10.8;
                _capillaryStatisticParagraphs[i].Format.SpaceBefore = "0.2cm";

                var paragraph = Section.AddParagraph();
                paragraph.Format.Font.Color = Colors.Black;
                paragraph.Format.Font.Name = "Dosis-Light";
                paragraph.Format.Font.Size = 7;
                paragraph.Format.LineSpacing = 10.8;
                paragraph.Format.SpaceBefore = "0.1cm";
                paragraph.AddText("Measurement protocols for 200 µl measurements:");
                paragraph.Format.SpaceAfter = "0.1cm";

                _capillaryMeasStatsTable[j] = Section.AddTable();
                _capillaryMeasStatsTable[j].Format.Font.Name = "Dosis-Light";
                _capillaryMeasStatsTable[j].Format.Font.Size = 7;
                _capillaryMeasStatsTable[j].Borders.Width = "0.01cm";
                _capillaryMeasStatsTable[j].Borders.Color = Colors.Black;

                column = _capillaryMeasStatsTable[j].AddColumn("0.5cm");
                column.Format.LeftIndent = "0.2cm";
                column.Format.Alignment = ParagraphAlignment.Center;

                column = _capillaryMeasStatsTable[j].AddColumn("4.5cm");
                column.Format.LeftIndent = "0.2cm";
                column.Format.Alignment = ParagraphAlignment.Center;

                column = _capillaryMeasStatsTable[j].AddColumn("4.5cm");
                column.Format.LeftIndent = "0.2cm";
                column.Format.Alignment = ParagraphAlignment.Center;

                column = _capillaryMeasStatsTable[j].AddColumn("3cm");
                column.Format.LeftIndent = "0.2cm";
                column.Format.Alignment = ParagraphAlignment.Center;

                column = _capillaryMeasStatsTable[j].AddColumn("3cm");
                column.Format.LeftIndent = "0.2cm";
                column.Format.Alignment = ParagraphAlignment.Center;

                column = _capillaryMeasStatsTable[j].AddColumn("2.5cm");
                column.Format.LeftIndent = "0.2cm";
                column.Format.Alignment = ParagraphAlignment.Center;

                row = _capillaryMeasStatsTable[j].AddRow();
                row.Height = "0.45cm";
                row.Cells[1].VerticalAlignment = VerticalAlignment.Center;
                row.Cells[1].AddParagraph("Errors");
                row.Cells[2].VerticalAlignment = VerticalAlignment.Center;
                row.Cells[2].AddParagraph("Date/Time");
                row.Cells[3].VerticalAlignment = VerticalAlignment.Center;
                row.Cells[3].AddParagraph("Time 2");
                row.Cells[4].VerticalAlignment = VerticalAlignment.Center;
                row.Cells[4].AddParagraph("Time 3");
                row.Cells[5].VerticalAlignment = VerticalAlignment.Center;
                row.Cells[5].AddParagraph("Bubble Time");

                paragraph = Section.AddParagraph();
                paragraph.Format.Font.Color = Colors.Black;
                paragraph.Format.Font.Name = "Dosis-Light";
                paragraph.Format.Font.Size = 7;
                paragraph.Format.LineSpacing = 10.8;
                paragraph.Format.SpaceBefore = "0.1cm";
                paragraph.AddText("Measurement protocols for 400 µl measurements:");
                paragraph.Format.SpaceAfter = "0.1cm";

                _capillaryMeasStatsTable[j+1] = Section.AddTable();
                _capillaryMeasStatsTable[j+1].Format.Font.Name = "Dosis-Light";
                _capillaryMeasStatsTable[j+1].Format.Font.Size = 7;
                _capillaryMeasStatsTable[j+1].Borders.Width = "0.01cm";
                _capillaryMeasStatsTable[j+1].Borders.Color = Colors.Black;

                column = _capillaryMeasStatsTable[j + 1].AddColumn("0.5cm");
                column.Format.LeftIndent = "0.2cm";
                column.Format.Alignment = ParagraphAlignment.Center;

                column = _capillaryMeasStatsTable[j + 1].AddColumn("4.5cm");
                column.Format.LeftIndent = "0.2cm";
                column.Format.Alignment = ParagraphAlignment.Center;

                column = _capillaryMeasStatsTable[j + 1].AddColumn("4.5cm");
                column.Format.LeftIndent = "0.2cm";
                column.Format.Alignment = ParagraphAlignment.Center;

                column = _capillaryMeasStatsTable[j + 1].AddColumn("3cm");
                column.Format.LeftIndent = "0.2cm";
                column.Format.Alignment = ParagraphAlignment.Center;

                column = _capillaryMeasStatsTable[j + 1].AddColumn("3cm");
                column.Format.LeftIndent = "0.2cm";
                column.Format.Alignment = ParagraphAlignment.Center;

                column = _capillaryMeasStatsTable[j + 1].AddColumn("2.5cm");
                column.Format.LeftIndent = "0.2cm";
                column.Format.Alignment = ParagraphAlignment.Center;

                row = _capillaryMeasStatsTable[j + 1].AddRow();
                row.Height = "0.45cm";
                row.Cells[1].VerticalAlignment = VerticalAlignment.Center;
                row.Cells[1].AddParagraph("Errors");
                row.Cells[2].VerticalAlignment = VerticalAlignment.Center;
                row.Cells[2].AddParagraph("Date/Time");
                row.Cells[3].VerticalAlignment = VerticalAlignment.Center;
                row.Cells[3].AddParagraph("Time 2");
                row.Cells[4].VerticalAlignment = VerticalAlignment.Center;
                row.Cells[4].AddParagraph("Time 3");
                row.Cells[5].VerticalAlignment = VerticalAlignment.Center;
                row.Cells[5].AddParagraph("Bubble Time");
            }

            var errorsHeaderParagraph = Section.AddParagraph();
            errorsHeaderParagraph.Format.Font.Color = Colors.Black;
            errorsHeaderParagraph.Format.Font.Name = "Dosis-Light";
            errorsHeaderParagraph.Format.Font.Size = 10;
            errorsHeaderParagraph.Format.LineSpacing = 10.8;
            errorsHeaderParagraph.Format.SpaceBefore = "1cm";
            errorsHeaderParagraph.AddText("Error statistics");

            _errorStatisticTable = Section.AddTable();
            _errorStatisticTable.Format.Font.Name = "Dosis-Light";
            _errorStatisticTable.Format.Font.Size = 7;
            _errorStatisticTable.Borders.Width = "0.01cm";
            _errorStatisticTable.Borders.Color = Colors.Black;

            column = _errorStatisticTable.AddColumn("0.5cm");
            column.Format.LeftIndent = "0.2cm";
            column.Format.Alignment = ParagraphAlignment.Center;

            column = _errorStatisticTable.AddColumn("2cm");
            column.Format.LeftIndent = "0.2cm";
            column.Format.Alignment = ParagraphAlignment.Center;

            column = _errorStatisticTable.AddColumn("4cm");
            column.Format.LeftIndent = "0.2cm";
            column.Format.Alignment = ParagraphAlignment.Center;

            column = _errorStatisticTable.AddColumn("4.5cm");
            column.Format.LeftIndent = "0.2cm";
            column.Format.Alignment = ParagraphAlignment.Center;

            column = _errorStatisticTable.AddColumn("4.5cm");
            column.Format.LeftIndent = "0.2cm";
            column.Format.Alignment = ParagraphAlignment.Center;

            column = _errorStatisticTable.AddColumn("2.5cm");
            column.Format.LeftIndent = "0.2cm";
            column.Format.Alignment = ParagraphAlignment.Center;

            row = _errorStatisticTable.AddRow();
            row.Height = "0.45cm";
            row.Cells[1].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[1].AddParagraph("Error Number");
            row.Cells[2].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[2].AddParagraph("Error Code");
            row.Cells[3].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[3].AddParagraph("First occurence");
            row.Cells[4].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[4].AddParagraph("Last occurence");
            row.Cells[5].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[5].AddParagraph("Count");
        }

        private void FillContent()
        {
            var paragraph = TitleFrame.AddParagraph();
            paragraph.Format.Font.Name = "Dosis-Regular";
            paragraph.Format.Font.Size = 9;
            paragraph.Format.Font.Color = Colors.White;
            paragraph.Format.Alignment = ParagraphAlignment.Center;
            paragraph.AddText("STATISTIC");

            NameSection.AddText($"Measuring unit {_serialNumber} statistics");

            _generalStatisticsParagraph.AddText(
                $"Time and date of last power on:\t{(_statistic.LastPowerOn == null ? "-" : EnvironmentService.GetDateTimeString(_statistic.LastPowerOn.Value))}\n");
            _generalStatisticsParagraph.AddText(
                $"Last update of power up time:\t{(_statistic.LastUpdateWorkingCounter == null ? "-" : EnvironmentService.GetDateTimeString(_statistic.LastUpdateWorkingCounter.Value))}\n");
            _generalStatisticsParagraph.AddText(
                $"Last reset of statistic:\t\t{(_statistic.LastResetStatistics == null ? "-" : EnvironmentService.GetDateTimeString(_statistic.LastResetStatistics.Value))}\n");
            _generalStatisticsParagraph.AddText(
                $"Total power up time:\t\t{_statistic.PowerUpTime.TotalDays} days {_statistic.PowerUpTime.TotalHours} hours {_statistic.PowerUpTime.TotalMinutes} minutes\n");
            _generalStatisticsParagraph.AddText($"Total count of power ons:\t{_statistic.PowerOnCount}\n");
            _generalStatisticsParagraph.AddText($"Average power on time in minutes:\t{_statistic.AveragePowerOnTime}\n");
            _generalStatisticsParagraph.AddText(
                $"Average power off time in minutes:\t{_statistic.AveragePowerOffTime}\n");

            Row row;

            for (int i = 0, j = 0; i < _statistic.CapillaryStatistics.Count; i++, j += 2)
            {
                var capStat = _statistic.CapillaryStatistics[i];
                _capillaryStatisticParagraphs[i].AddText($"Diameter:\t\t\t{capStat.Diameter}\n");
                _capillaryStatisticParagraphs[i].AddText($"Cleans:\t\t\t{capStat.CleanCount}\n");
                _capillaryStatisticParagraphs[i].AddText($"Measurements:\t\t{capStat.MeasureCount}\n");
                _capillaryStatisticParagraphs[i].AddText(
                    $"Position of last 200 µl measurement:\t{capStat.LastPosition200}\n");
                _capillaryStatisticParagraphs[i].AddText(
                    $"Position of last 400 µl measurement:\t{capStat.LastPosition400}\n");

                var k = 0;
                foreach(var measureStat in capStat.Measure200Statistics)
                {
                    row = _capillaryMeasStatsTable[j].AddRow();
                    row.Height = "0.45cm";
                    row.Cells[0].VerticalAlignment = VerticalAlignment.Center;
                    row.Cells[0].AddParagraph(k.ToString());
                    row.Cells[1].VerticalAlignment = VerticalAlignment.Center;
                    row.Cells[1].AddParagraph(string.Join(" ", measureStat.ErrorCode.Select(us => us.ToString("00"))));
                    row.Cells[2].VerticalAlignment = VerticalAlignment.Center;
                    row.Cells[2].AddParagraph(measureStat.Timestamp == null ? "-" : EnvironmentService.GetDateTimeString(measureStat.Timestamp.Value));
                    row.Cells[3].VerticalAlignment = VerticalAlignment.Center;
                    row.Cells[3].AddParagraph(measureStat.Time2.ToString());
                    row.Cells[4].VerticalAlignment = VerticalAlignment.Center;
                    row.Cells[4].AddParagraph(measureStat.Time3.ToString());
                    row.Cells[5].VerticalAlignment = VerticalAlignment.Center;
                    row.Cells[5].AddParagraph(measureStat.BubbleTime.ToString());

                    k++;
                }

                k = 0;
                foreach (var measureStat in capStat.Measure400Statistics)
                {
                    row = _capillaryMeasStatsTable[j+1].AddRow();
                    row.Height = "0.45cm";
                    row.Cells[0].VerticalAlignment = VerticalAlignment.Center;
                    row.Cells[0].AddParagraph(k.ToString());
                    row.Cells[1].VerticalAlignment = VerticalAlignment.Center;
                    row.Cells[1].AddParagraph(string.Join(" ", measureStat.ErrorCode.Select(us => us.ToString("00"))));
                    row.Cells[2].VerticalAlignment = VerticalAlignment.Center;
                    row.Cells[2].AddParagraph(measureStat.Timestamp == null ? "-" : EnvironmentService.GetDateTimeString(measureStat.Timestamp.Value));
                    row.Cells[3].VerticalAlignment = VerticalAlignment.Center;
                    row.Cells[3].AddParagraph(measureStat.Time2.ToString());
                    row.Cells[4].VerticalAlignment = VerticalAlignment.Center;
                    row.Cells[4].AddParagraph(measureStat.Time3.ToString());
                    row.Cells[5].VerticalAlignment = VerticalAlignment.Center;
                    row.Cells[5].AddParagraph(measureStat.BubbleTime.ToString());

                    k++;
                }
            }

            var errorDetails = _errorController.ErrorDetails.OrderBy(ed => int.Parse(ed.ErrorNumber)).ToArray();
            for(var i = 0; i < errorDetails.Length; i++)
            {
                var errorDetail = errorDetails[i];
                var errorStat = _statistic.ErrorStatistics[i];
                
                row = _errorStatisticTable.AddRow();
                row.Height = "0.45cm";
                row.Cells[0].VerticalAlignment = VerticalAlignment.Center;
                row.Cells[0].AddParagraph(errorDetail.ErrorNumber);
                row.Cells[1].VerticalAlignment = VerticalAlignment.Center;
                row.Cells[1].AddParagraph(LocalizationService.GetLocalizedString(errorDetail.DeviceErrorName));
                row.Cells[2].VerticalAlignment = VerticalAlignment.Center;
                row.Cells[2].AddParagraph(errorDetail.ErrorCode);
                row.Cells[3].VerticalAlignment = VerticalAlignment.Center;
                row.Cells[3].AddParagraph(errorStat.FirstOccured == null ? "-" : EnvironmentService.GetDateTimeString(errorStat.FirstOccured.Value));
                row.Cells[4].VerticalAlignment = VerticalAlignment.Center;
                row.Cells[4].AddParagraph(errorStat.LastOccured == null ? "-" : EnvironmentService.GetDateTimeString(errorStat.LastOccured.Value));
                row.Cells[5].VerticalAlignment = VerticalAlignment.Center;
                row.Cells[5].AddParagraph(errorStat.OccurenceCount.ToString());
            }
        }
    }
}
