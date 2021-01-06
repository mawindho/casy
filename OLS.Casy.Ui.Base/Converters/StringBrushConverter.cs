using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace OLS.Casy.Ui.Base.Converters
{
    public class StringBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var stringValue = value as string;
            if(string.IsNullOrEmpty(stringValue))
            {
                stringValue = "#FF009FE3";
            }
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(stringValue));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
