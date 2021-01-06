using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace OLS.Casy.Ui.Base.Converters
{
    public class BooleanOrMultiValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                return values.Any(v =>
                {
                    var b = System.Convert.ToBoolean(v);
                    return b;
                });
            }
            catch (FormatException)
            {
                return false;
            }
            catch (InvalidCastException)
            {
                return false;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
