using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OLS.Casy.Ui.Base.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool && targetType == typeof(Visibility))
            {
                if ((bool)value)
                {
                    return Visibility.Visible;
                }
                else
                {
                    if(parameter != null && parameter.ToString() == "Hidden")
                    {
                        return Visibility.Hidden;
                    }
                    return Visibility.Collapsed;
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is Visibility && targetType == typeof(bool))
            {
                if((Visibility)value == Visibility.Visible)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
