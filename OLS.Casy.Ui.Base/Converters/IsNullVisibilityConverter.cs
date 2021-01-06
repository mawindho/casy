using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OLS.Casy.Ui.Base.Converters
{
    public class IsNullVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value == null) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException("IsNullVisibilityConverter can only be used OneWay.");
        }
    }
}
