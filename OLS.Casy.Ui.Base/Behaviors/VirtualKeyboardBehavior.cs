using System.Diagnostics;
using System.Windows;
using Microsoft.Xaml.Behaviors;

namespace OLS.Casy.Ui.Base.Behaviors
{
    public class VirtualKeyboardExtension : Behavior<FrameworkElement>
    {
        private const string TabTipExecPath = @"C:\Program Files\Common Files\microsoft shared\ink\TabTip.exe";

        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.GotFocus += OnGotFocus;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            if (this.AssociatedObject != null)
            {
                this.AssociatedObject.GotFocus -= OnGotFocus;
            }
        }

        private void OnGotFocus(object sender, RoutedEventArgs e)
        {
            Process.Start(TabTipExecPath);
        }
    }
}
