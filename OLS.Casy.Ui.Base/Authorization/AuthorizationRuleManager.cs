using MahApps.Metro.Controls;
using OLS.Casy.Ui.Base.DyamicUiHelper;
using System.Windows;
using System.Windows.Controls;

namespace OLS.Casy.Ui.Base.Authorization
{
    internal class AuthorizationRuleManager : RuleManager
    {
        protected override void DefineDefaultRoles()
        {
            this.AddRule(typeof(DependencyObject), new MarkupAuthorizationRule());
            this.AddRule(typeof(TabItem), new MarkupAuthorizationRule("Visibility"));
            this.AddRule(typeof(Button), new MarkupAuthorizationRule("IsEnabled"));
            this.AddRule(typeof(Slider), new MarkupAuthorizationRule("IsEnabled"));
            this.AddRule(typeof(TextBox), new MarkupAuthorizationRule("IsEnabled"));
            this.AddRule(typeof(ComboBox), new MarkupAuthorizationRule("IsEnabled"));
            this.AddRule(typeof(StackPanel), new MarkupAuthorizationRule("IsEnabled"));
            this.AddRule(typeof(Slider), new MarkupAuthorizationRule("IsEnabled"));
            this.AddRule(typeof(CheckBox), new MarkupAuthorizationRule("IsEnabled"));
            this.AddRule(typeof(Grid), new MarkupAuthorizationRule("IsEnabled"));
            this.AddRule(typeof(NumericUpDown), new MarkupAuthorizationRule("IsEnabled"));
        }
    }
}
