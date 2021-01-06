using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Models;
using System.Collections.Generic;
using OLS.Casy.Core.Api;
using OLS.Casy.Ui.Base.Documents;
using OLS.Casy.Ui.Core.Api;

namespace OLS.Casy.Ui.AuditTrail.Documents
{
    public class SystemLogDocument : DocumentBase
    {
        private Table _table;
        private IEnumerable<SystemLogEntry> _data;

        public SystemLogDocument(ILocalizationService localizationService,
           IAuthenticationService authenticationService, IDocumentSettingsManager documentSettingsManager,
           IEnvironmentService environmentService)
           : base(localizationService, authenticationService, documentSettingsManager, environmentService)
        {
        }

        public Document CreateDocument(IEnumerable<SystemLogEntry> data)
        {
            _data = data;

            Document = new Document();

            DefineStyles();
            CreatePage(Orientation.Landscape, AuthenticationService.LoggedInUser.Identity.Name);
            FillContent();

            return Document;
        }

        protected override void CreatePage(Orientation orientation, string createdBy)
        {
            base.CreatePage(orientation, createdBy);

            _table = Section.AddTable();
            _table.Format.Font.Name = "Dosis-Light";
            _table.Format.Font.Size = 7;
            _table.Borders.Width = "0.01cm";
            _table.Borders.Color = Colors.Black;

            var column = _table.AddColumn("3cm");
            column.Format.Alignment = ParagraphAlignment.Left;

            column = _table.AddColumn("3cm");
            column.Format.Alignment = ParagraphAlignment.Left;

            column = _table.AddColumn("3cm");
            column.Format.Alignment = ParagraphAlignment.Left;

            column = _table.AddColumn("18cm");
            column.Format.Alignment = ParagraphAlignment.Left;
        }

        private void FillContent()
        {
            var paragraph = TitleFrame.AddParagraph();
            paragraph.Format.Font.Name = "Dosis-Regular";
            paragraph.Format.Font.Size = 9;
            paragraph.Format.Font.Color = Colors.White;
            paragraph.Format.Alignment = ParagraphAlignment.Center;
            paragraph.AddText("SYSTEM LOG");

            var row = _table.AddRow();
            row.Height = "0.45cm";
            row.Format.Font.Bold = true;
            row.Format.Font.Size = 9;

            row.Cells[0].Format.Font.Bold = true;
            row.Cells[0].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[0].AddParagraph("Timestamp");

            row.Cells[1].Format.Font.Bold = true;
            row.Cells[1].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[1].AddParagraph("Type");

            row.Cells[2].Format.Font.Bold = true;
            row.Cells[2].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[2].AddParagraph("User");

            row.Cells[3].Format.Font.Bold = true;
            row.Cells[3].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[3].AddParagraph("Message");

            foreach (var data in _data)
            {
                var row2 = _table.AddRow();
                row2.Height = "0.45cm";

                row2.Cells[0].Format.Alignment = ParagraphAlignment.Left;
                row2.Cells[0].VerticalAlignment = VerticalAlignment.Center;
                var dateTimeString = EnvironmentService.GetDateTimeString(data.Date.DateTime);
                row2.Cells[0].AddParagraph(data.Date.ToString("G"));

                row2.Cells[1].Format.Alignment = ParagraphAlignment.Left;
                row2.Cells[1].VerticalAlignment = VerticalAlignment.Center;
                row2.Cells[1].AddParagraph(data.Level);

                row2.Cells[2].Format.Alignment = ParagraphAlignment.Left;
                row2.Cells[2].VerticalAlignment = VerticalAlignment.Center;
                row2.Cells[2].AddParagraph(data.User);

                row2.Cells[3].Format.Alignment = ParagraphAlignment.Left;
                row2.Cells[3].VerticalAlignment = VerticalAlignment.Center;
                row2.Cells[3].AddParagraph(data.Message);
            }
        }
    }
}
