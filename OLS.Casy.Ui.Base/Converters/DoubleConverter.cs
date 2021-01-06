using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace OLS.Casy.Ui.Base.Converters
{
    public class DoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is double)
            {
                double dValue = (double)value;
                if(double.IsNaN(dValue))
                {
                    return string.Empty;
                }
                return dValue.ToString();
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is string && targetType == typeof(double))
            {
                if (!string.IsNullOrEmpty(value as string))
                {
                    double dValue;
                    if(double.TryParse(value as string, out dValue))
                    {
                        return dValue;
                    }
                    return 1d;
                }
            }
            return null;
        }
    }
}
