using DevExpress.Xpf.Charts;
using System.Windows;

namespace OLS.Casy.Ui.Base.Controls
{
    public class OmniRange : Range
    {
        public static readonly DependencyProperty IsAutoRangeProperty =
            DependencyProperty.Register(
                "IsAutoRange",
                typeof(bool),
                typeof(OmniRange),
                new PropertyMetadata(false, OnIsAutoRangeChanged)
                );

        public static bool GetIsAutoRange(DependencyObject target)
        {
            return (bool)target.GetValue(IsAutoRangeProperty);
        }

        /// <summary>
        ///     Setter used by XAMl code.
        /// </summary>
        public static void SetIsAutoRange(DependencyObject target, bool value)
        {
            target.SetValue(IsAutoRangeProperty, value);
        }

        private static void OnIsAutoRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is OmniRange range)) return;
            if (!(bool) e.NewValue) return;
            
            range.SetAuto();
            SetIsAutoRange(range, false);
        }
    }
}
