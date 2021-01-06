using System.Collections.Generic;
using System.Linq;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Ui.Analyze.Models;
using OLS.Casy.Ui.Base.Documents;
using OLS.Casy.Ui.Base.Models;
using OLS.Casy.Ui.Core.Api;

namespace OLS.Casy.Ui.Analyze.Documents
{
    public class TableDocument : DocumentBase
    {
        private IEnumerable<MeasureResultModel> _selectedRows;
        private IEnumerable<AdditinalGridColumnInfo> _selectedColumns;

        private Table _table;

        public TableDocument(ILocalizationService localizationService,
           IAuthenticationService authenticationService, IDocumentSettingsManager documentSettingsManager,
           IEnvironmentService environmentService)
           : base(localizationService, authenticationService, documentSettingsManager, environmentService)
        {
        }

        public Document CreateDocument(IEnumerable<MeasureResultModel> selectedRows, IEnumerable<AdditinalGridColumnInfo> selectedColumns)
        {
            _selectedRows = selectedRows;
            _selectedColumns = selectedColumns;

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

            var columnWidth = 27d / _selectedColumns.Count();
            foreach (var unused in _selectedColumns)
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
            paragraph.AddText("TABLE VIEW");

            var row = _table.AddRow();
            row.Height = "0.45cm";
            row.Format.Font.Bold = true;
            row.Format.Font.Size = 9;

            for (var i = 0; i < _selectedColumns.Count(); i++)
            {
                var selectedColumn = _selectedColumns.ElementAt(i);
                row.Cells[i].Format.Font.Bold = true;
                row.Cells[i].VerticalAlignment = VerticalAlignment.Center;
                row.Cells[i].AddParagraph(selectedColumn.Header);
            }

            foreach (var selectedRow in _selectedRows)
            {
                var row2 = _table.AddRow();
                row2.Height = "0.45cm";

                for (var i = 0; i < _selectedColumns.Count(); i++)
                {
                    var selectedColumn = _selectedColumns.ElementAt(i);

                    row2.Cells[i].Format.Alignment = ParagraphAlignment.Left;
                    row2.Cells[i].VerticalAlignment = VerticalAlignment.Center;
                    row2.Cells[i].AddParagraph(selectedRow.GetValue(selectedColumn.Binding).ToString());
                }
            }
        }
    }
}
