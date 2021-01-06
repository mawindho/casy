using System;
using System.Globalization;
using System.Windows.Data;

namespace OLS.Casy.Ui.Base.Converters
{
    public class HeightConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if(values.Length == 3)
            {
                double containerSize = System.Convert.ToDouble(values[0], CultureInfo.InvariantCulture);
                int totalItems = System.Convert.ToInt32(values[1], CultureInfo.InvariantCulture);
                int visibleItems = System.Convert.ToInt32(values[2], CultureInfo.InvariantCulture);

                var visibleSubgroupSize = containerSize / visibleItems;
                return totalItems * visibleSubgroupSize;
            }
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
