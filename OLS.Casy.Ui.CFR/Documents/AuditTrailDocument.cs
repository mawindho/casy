using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Ui.AuditTrail.ViewModel;
using System.Collections.Generic;
using OLS.Casy.Core.Api;
using OLS.Casy.Ui.Base.Documents;
using OLS.Casy.Ui.Core.Api;

namespace OLS.Casy.Ui.AuditTrail.Documents
{
    public class AuditTrailDocument : DocumentBase
    {
        private Table _table;
        private IEnumerable<AuditTrailEntityViewModel> _data;
        private string _measurementNameAndOrigin;
        private string _measuredAtAndSerial;

        private Paragraph _measuredAtSection;
        private Paragraph _nameAndOriginSection;

        public AuditTrailDocument(ILocalizationService localizationService,
           IAuthenticationService authenticationService, IDocumentSettingsManager documentSettingsManager,
           IEnvironmentService environmentService)
           : base(localizationService, authenticationService, documentSettingsManager, environmentService)
        {
        }

        public Document CreateDocument(IEnumerable<AuditTrailEntityViewModel> data, string measurementNameAndOrigin, string measuredAtAndSerial)
        {
            _data = data;
            _measurementNameAndOrigin = measurementNameAndOrigin;
            _measuredAtAndSerial = measuredAtAndSerial;

            Document = new Document();

            DefineStyles();
            CreatePage(Orientation.Landscape, AuthenticationService.LoggedInUser.Identity.Name);
            FillContent();

            return Document;
        }

        protected override void CreatePage(Orientation orientation, string createdBy)
        {
            base.CreatePage(orientation, createdBy);

            _nameAndOriginSection = Section.AddParagraph();
            _nameAndOriginSection.Format.Font.Color = Colors.Black;
            _nameAndOriginSection.Format.Font.Name = "Dosis-Light";
            _nameAndOriginSection.Format.Font.Size = 9;
            _nameAndOriginSection.Format.LineSpacing = 10.8;
            _nameAndOriginSection.Format.LineSpacingRule = LineSpacingRule.AtLeast;

            _measuredAtSection = Section.AddParagraph();
            _measuredAtSection.Format.Font.Color = Colors.Black;
            _measuredAtSection.Format.Font.Name = "Dosis-Light";
            _measuredAtSection.Format.Font.Size = 9;
            _measuredAtSection.Format.LineSpacing = 10.8;
            _measuredAtSection.Format.LineSpacingRule = LineSpacingRule.AtLeast;
            _measuredAtSection.Format.SpaceAfter = "2cm";

            _table = Section.AddTable();
            _table.Format.Font.Name = "Dosis-Light";
            _table.Format.Font.Size = 7;
            _table.Borders.Width = "0.01cm";
            _table.Borders.Color = Colors.Black;

            const double columnWidth = 27d / 9;
            for (var i = 0; i < 9; i++)
            {
                var column = _table.AddColumn($"{columnWidth}cm");
                column.Format.Alignment = ParagraphAlignment.Left;
            }
        }

        private void FillContent()
        {
            var paragraph = TitleFrame.AddParagraph();
            paragraph.Format.Font.Name = "Dosis-Regular";
            paragraph.Format.Font.Size = 9;
            paragraph.Format.Font.Color = Colors.White;
            paragraph.Format.Alignment = ParagraphAlignment.Center;
            paragraph.AddText("AUDIT TRAIL");

            _nameAndOriginSection.AddText(_measurementNameAndOrigin);
            _measuredAtSection.AddText(_measuredAtAndSerial);

            var row = _table.AddRow();
            row.Height = "0.45cm";
            row.Format.Font.Bold = true;
            row.Format.Font.Size = 9;

            row.Cells[0].Format.Font.Bold = true;
            row.Cells[0].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[0].AddParagraph("Timestamp");

            row.Cells[1].Format.Font.Bold = true;
            row.Cells[1].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[1].AddParagraph("Object");

            row.Cells[2].Format.Font.Bold = true;
            row.Cells[2].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[2].AddParagraph("Action");

            row.Cells[3].Format.Font.Bold = true;
            row.Cells[3].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[3].AddParagraph("Property");

            row.Cells[4].Format.Font.Bold = true;
            row.Cells[4].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[4].AddParagraph("Previous");

            row.Cells[5].Format.Font.Bold = true;
            row.Cells[5].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[5].AddParagraph("New");

            row.Cells[6].Format.Font.Bold = true;
            row.Cells[6].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[6].AddParagraph("User");

            row.Cells[7].Format.Font.Bold = true;
            row.Cells[7].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[7].AddParagraph("Computer");

            row.Cells[8].Format.Font.Bold = true;
            row.Cells[8].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[8].AddParagraph("Version");

            foreach (var data in _data)
            {
                if (data == null)
                    continue;

                var row2 = _table.AddRow();
                row2.Height = "0.45cm";

                row2.Cells[0].Format.Alignment = ParagraphAlignment.Left;
                row2.Cells[0].VerticalAlignment = VerticalAlignment.Center;
                row2.Cells[0].AddParagraph(data.DateChanged.ToString("G"));

                row2.Cells[1].Format.Alignment = ParagraphAlignment.Left;
                row2.Cells[1].VerticalAlignment = VerticalAlignment.Center;
                row2.Cells[1].AddParagraph(data.EntityName ?? "");

                row2.Cells[2].Format.Alignment = ParagraphAlignment.Left;
                row2.Cells[2].VerticalAlignment = VerticalAlignment.Center;
                row2.Cells[2].AddParagraph(data.Action ?? "");

                row2.Cells[3].Format.Alignment = ParagraphAlignment.Left;
                row2.Cells[3].VerticalAlignment = VerticalAlignment.Center;
                row2.Cells[3].AddParagraph(data.PropertyName ?? "");

                row2.Cells[4].Format.Alignment = ParagraphAlignment.Left;
                row2.Cells[4].VerticalAlignment = VerticalAlignment.Center;
                row2.Cells[4].AddParagraph(data.OldValue ?? "");

                row2.Cells[5].Format.Alignment = ParagraphAlignment.Left;
                row2.Cells[5].VerticalAlignment = VerticalAlignment.Center;
                row2.Cells[5].AddParagraph(data.NewValue ?? "");

                row2.Cells[6].Format.Alignment = ParagraphAlignment.Left;
                row2.Cells[6].VerticalAlignment = VerticalAlignment.Center;
                row2.Cells[6].AddParagraph(data.UserChanged ?? "");

                row2.Cells[7].Format.Alignment = ParagraphAlignment.Left;
                row2.Cells[7].VerticalAlignment = VerticalAlignment.Center;
                row2.Cells[7].AddParagraph(data.ComputerName ?? "");

                row2.Cells[8].Format.Alignment = ParagraphAlignment.Left;
                row2.Cells[8].VerticalAlignment = VerticalAlignment.Center;
                row2.Cells[8].AddParagraph(data.SoftwareVersion ?? "");
            }
        }
    }
}
