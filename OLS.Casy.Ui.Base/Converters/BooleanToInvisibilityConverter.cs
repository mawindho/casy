using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OLS.Casy.Ui.Base.Converters
{
    public class BooleanToInvisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool && targetType == typeof(Visibility))
            {
                if ((bool)value)
                {
                    
                    if(parameter != null)
                    {
                        string stringParameter = parameter as string;
                        if(stringParameter != null)
                        { 
                            Visibility newValue;
                            if(Enum.TryParse(stringParameter, out newValue))
                            {
                                return newValue;
                            }
                        }
                    }
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
