using System;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Shapes;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Ui.Core.Api;

namespace OLS.Casy.Ui.Base.Documents
{
    public abstract class DocumentBase
    {
        protected DocumentBase(ILocalizationService localizationService,
            IAuthenticationService authenticationService,
            IDocumentSettingsManager documentSettingsManager,
            IEnvironmentService environmentService)
        {
            LocalizationService = localizationService;
            AuthenticationService = authenticationService;
            DocumentSettingsManager = documentSettingsManager;
            EnvironmentService = environmentService;
        }

        protected ILocalizationService LocalizationService { get; }
        protected IAuthenticationService AuthenticationService { get; }
        protected IDocumentSettingsManager DocumentSettingsManager { get; }
        protected IEnvironmentService EnvironmentService { get; }

        protected Document Document { get; set; }
        protected Section Section { get; set; }
        protected TextFrame TitleFrame { get; set; }
        protected Paragraph NameSection { get; set; }

        protected bool ShowLastWeeklyClean
        {
            get { return DocumentSettingsManager.ShowLastWeeklyClean; }
        }

        protected void DefineStyles()
        {
            var style = Document.Styles["Normal"];
            style.Font.Name = "Dosis-Regular";
            style.Font.Size = 9;
        }

        protected virtual void CreatePage(Orientation orientation, string createdBy)
        {
            var docCreator = string.Empty;
            if (AuthenticationService.LoggedInUser != null)
            {
                docCreator =
                    $"{AuthenticationService.LoggedInUser.FirstName} {AuthenticationService.LoggedInUser.LastName}";
            }

            Section = Document.AddSection();
            Section.PageSetup.PageFormat = PageFormat.A4;
            Section.PageSetup.Orientation = orientation;
            Section.PageSetup.LeftMargin = "1.8cm";
            Section.PageSetup.TopMargin = "1.0cm";
            Section.PageSetup.RightMargin = "1.0cm";
            Section.PageSetup.FooterDistance = "0.1cm";

            var documentLogoName = DocumentSettingsManager.DocumentLogoName;
            if (documentLogoName != "OLS_Logo.png")
            {
                var customImage = Section.AddImage(@"Resources\" + documentLogoName);
                customImage.Width = "4.0cm";
                customImage.LockAspectRatio = true;
                customImage.RelativeVertical = RelativeVertical.Line;
                customImage.RelativeHorizontal = RelativeHorizontal.Margin;
                customImage.Top = ShapePosition.Top;
                customImage.Left = ShapePosition.Right;
                customImage.WrapFormat.Style = WrapStyle.Through;
            }
            else
            {
                var olsImage = Section.AddImage(@"Resources\Icon_LogomitUnterzeile.png");
                olsImage.Width = "4.0cm";
                olsImage.LockAspectRatio = true;
                olsImage.RelativeVertical = RelativeVertical.Line;
                olsImage.RelativeHorizontal = RelativeHorizontal.Margin;
                olsImage.Top = ShapePosition.Top;
                olsImage.Left = ShapePosition.Right;
                olsImage.WrapFormat.Style = WrapStyle.Through;
            }

            var linesImage = Section.AddImage(@"Resources\Icon_LogoStriche.png");
            linesImage.Width = "0.5cm";
            linesImage.LockAspectRatio = true;
            linesImage.RelativeVertical = RelativeVertical.Margin;
            linesImage.RelativeHorizontal = RelativeHorizontal.Page;
            linesImage.Top = ShapePosition.Top;
            linesImage.Left = ShapePosition.Right;
            linesImage.WrapFormat.Style = WrapStyle.Through;
            linesImage.WrapFormat.DistanceRight = "0.3cm";
            linesImage.WrapFormat.DistanceTop = "0.4cm";

            TitleFrame = this.Section.AddTextFrame();
            TitleFrame.Left = ShapePosition.Left;
            TitleFrame.RelativeHorizontal = RelativeHorizontal.Margin;
            TitleFrame.Top = "2.1cm";
            TitleFrame.RelativeVertical = RelativeVertical.Page;
            TitleFrame.FillFormat.Color = Colors.Black;
            TitleFrame.MarginTop = "0.03cm";
            TitleFrame.Height = "0.5cm";

            NameSection = Section.AddParagraph();
            NameSection.Format.SpaceBefore = "2.6cm";
            NameSection.Format.Font.Color = Colors.Black;
            NameSection.Format.Font.Name = "Dosis-Regular";
            NameSection.Format.Font.Size = 12;
            NameSection.Format.LineSpacing = 14.4;
            NameSection.Format.LineSpacingRule = LineSpacingRule.AtLeast;

            var footerTable = Section.Footers.Primary.AddTable();
            footerTable.AddColumn("6cm");
            footerTable.AddColumn("6cm");
            footerTable.AddColumn("4cm");
            footerTable.AddColumn("2cm");
            
            var row = footerTable.AddRow();

            var footerLeftParagraph = row.Cells[0].AddParagraph();
            footerLeftParagraph.Format.Font.Name = "Dosis-Regular";
            footerLeftParagraph.Format.Font.Size = 8;
            footerLeftParagraph.Format.Borders.Left.Color = Colors.Black;
            footerLeftParagraph.Format.Borders.Left.Width = "0.05cm";
            footerLeftParagraph.Format.Borders.DistanceFromLeft = "0.3cm";
            footerLeftParagraph.Format.LineSpacing = 14;
            footerLeftParagraph.Format.LineSpacingRule = LineSpacingRule.AtLeast;
            footerLeftParagraph.AddText(LocalizationService.GetLocalizedString("DocumentBase_Footer_User"));
            footerLeftParagraph.AddSpace(2);
            footerLeftParagraph.AddFormattedText(docCreator, new Font("Dosis-Light", 8));

            var footerMiddleParagraph = row.Cells[1].AddParagraph();
            footerMiddleParagraph.Format.Font.Name = "Dosis-Regular";
            footerMiddleParagraph.Format.Font.Size = 8;
            footerMiddleParagraph.Format.Borders.Left.Color = Colors.Black;
            footerMiddleParagraph.Format.Borders.Left.Width = "0.05cm";
            footerMiddleParagraph.Format.Borders.DistanceFromLeft = "0.3cm";
            footerMiddleParagraph.Format.LineSpacing = 14;
            footerMiddleParagraph.Format.LineSpacingRule = LineSpacingRule.AtLeast;
            footerMiddleParagraph.AddText(LocalizationService.GetLocalizedString("DocumentBase_Footer_PrintDate"));
            footerMiddleParagraph.AddSpace(2);
            //footerMiddleParagraph.AddFormattedText(DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss \"UTC\"zzz"), new Font("Dosis-Light", 8));
            footerMiddleParagraph.AddFormattedText(EnvironmentService.GetDateTimeString(DateTime.Now), new Font("Dosis-Light", 8));

            var footerRightParagraph = row.Cells[2].AddParagraph();
            footerRightParagraph.Format.Font.Name = "Dosis-Regular";
            footerRightParagraph.Format.Font.Size = 8;
            footerRightParagraph.Format.Borders.Left.Color = Colors.Black;
            footerRightParagraph.Format.Borders.Left.Width = "0.05cm";
            footerRightParagraph.Format.Borders.DistanceFromLeft = "0.3cm";
            footerRightParagraph.Format.LineSpacing = 14;
            footerRightParagraph.Format.LineSpacingRule = LineSpacingRule.AtLeast;

            footerRightParagraph.AddText(LocalizationService.GetLocalizedString("DocumentBase_Footer_Page"));
            footerRightParagraph.AddSpace(2);
            footerRightParagraph.AddPageField();
            footerRightParagraph.AddSpace(1);
            footerRightParagraph.AddFormattedText(LocalizationService.GetLocalizedString("DocumentBase_Footer_Page_Of"));
            footerRightParagraph.AddSpace(1);
            footerRightParagraph.AddNumPagesField();

            var footerImageParagraph = row.Cells[3].AddParagraph();
            var footerImage = footerImageParagraph.AddImage(@"Resources\Icon_LogomitUnterzeile.png");
            footerImage.Width = "2.0cm";
            footerImage.LockAspectRatio = true;
            footerImage.RelativeVertical = RelativeVertical.Line;
            footerImage.RelativeHorizontal = RelativeHorizontal.Margin;
            footerImage.Top = ShapePosition.Top;
            footerImage.Left = ShapePosition.Right;
            footerImage.WrapFormat.Style = WrapStyle.Through;
        }
    }
}
