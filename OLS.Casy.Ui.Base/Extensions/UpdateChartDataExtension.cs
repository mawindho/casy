using DevExpress.Xpf.Charts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OLS.Casy.Ui.Base.Extensions
{
    public static class UpdateChartDataExtension
    {
        public static bool GetForceUpdate(DependencyObject obj)
        {
            return (bool)obj.GetValue(ForceUpdateProperty);
        }

        public static void SetForceUpdate(DependencyObject obj, bool value)
        {
            obj.SetValue(ForceUpdateProperty, value);
        }

        public static readonly DependencyProperty ForceUpdateProperty =
            DependencyProperty.RegisterAttached("ForceUpdate", typeof(bool), typeof(UpdateChartDataExtension), new PropertyMetadata(false, ForceUpdatePropertyChanged));

        public static void ForceUpdatePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var chartControl = obj as ChartControl;
            bool boolValue = (bool) e.NewValue;

            if(chartControl != null && boolValue)
            {
                chartControl.UpdateData();
                SetForceUpdate(chartControl, false);
            }
        }
    }
}
