using OLS.Casy.Ui.Api;
using System;
using System.Globalization;
using System.Windows.Data;

namespace OLS.Casy.Ui.Base.Converters
{
    public class ContextMenuStateToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType == typeof(double) && value is ContextMenuItemState)
            {
                return (ContextMenuItemState)value == ContextMenuItemState.Checked ? 1d : 0d;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
