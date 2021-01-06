using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Grid.LookUp;
using OLS.Casy.Ui.Base.DyamicUiHelper;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;

namespace OLS.Casy.Ui.Base.Localization
{
    internal class LocalizationRuleManager : RuleManager
    {
        protected override void DefineDefaultRoles()
        {
            this.AddRule(typeof(DependencyObject), new MarkupLocalizationRule());
            this.AddRule(typeof(Label), new MarkupLocalizationRule("Content"));
            this.AddRule(typeof(TextBox), new MarkupLocalizationRule("Text"));
            this.AddRule(typeof(TextBlock), new MarkupLocalizationRule("Text"));
            this.AddRule(typeof(Button), new MarkupLocalizationRule("Content"));
            this.AddRule(typeof(ToggleButton), new MarkupLocalizationRule("Content"));
            this.AddRule(typeof(DataGridTextColumn), new MarkupLocalizationRule("Header"));
            this.AddRule(typeof(DataGridCheckBoxColumn), new MarkupLocalizationRule("Header"));
            this.AddRule(typeof(HeaderedContentControl), new MarkupLocalizationRule("Header"));
            this.AddRule(typeof(MenuItem), new MarkupLocalizationRule("Header"));
            this.AddRule(typeof(BarButtonItem), new MarkupLocalizationRule("Content"));
            this.AddRule(typeof(GridColumn), new MarkupLocalizationRule("Header"));
            this.AddRule(typeof(BarCheckItem), new MarkupLocalizationRule("Content"));
            this.AddRule(typeof(BarSubItem), new MarkupLocalizationRule("Content"));
            this.AddRule(typeof(LookUpEdit), new MarkupLocalizationRule("NullText"));
            this.AddRule(typeof(Run), new MarkupLocalizationRule("Text"));
            //this.AddRule(typeof(TileGroupHeader), new MarkupLocalizationRule("Header"));
        }
    }
}
