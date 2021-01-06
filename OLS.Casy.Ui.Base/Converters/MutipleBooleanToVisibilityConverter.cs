using System;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace OLS.Casy.Ui.Base.Converters
{
    public class MutipleBooleanToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.OfType<bool>().Any(i => i))
            {
                if (parameter != null)
                {
                    Visibility newValue;

                    string stringParameter = parameter as string;
                    if(stringParameter != null)
                    {
                        if (Enum.TryParse(stringParameter, out newValue))
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

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}
