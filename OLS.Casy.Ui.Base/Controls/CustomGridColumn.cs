using DevExpress.Xpf.Grid;
using System.Windows;

namespace OLS.Casy.Ui.Base.Controls
{
    public class CustomGridColumn : GridColumn
    {
        public static readonly DependencyProperty OrderProperty = DependencyProperty.Register("Order", typeof(int), typeof(CustomGridColumn), new UIPropertyMetadata(0));

        public int Order
        {
            get { return (int)GetValue(OrderProperty); }
            set { SetValue(OrderProperty, value); }
        }
    }
}
