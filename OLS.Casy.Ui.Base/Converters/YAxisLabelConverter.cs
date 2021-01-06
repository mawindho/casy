using System;
using System.Globalization;
using System.Windows.Data;

namespace OLS.Casy.Ui.Base.Converters
{
    public class YAxisLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            double dValue = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
            return dValue / System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
