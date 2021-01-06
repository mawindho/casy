using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OLS.Casy.Ui.Base.Converters
{
    public class StaticResourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value == null)
            {
                return null;
            }
            var resourceKey = value as string;

            if (resourceKey == null)
            {
                return null;
            }
            if (Application.Current.Resources.Contains(resourceKey))
            {
                return Application.Current.Resources[resourceKey];
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
