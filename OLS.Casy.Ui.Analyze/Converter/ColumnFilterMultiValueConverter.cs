using OLS.Casy.Ui.Base.Controls;
using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace OLS.Casy.Ui.Analyze.Converter
{
    public class ColumnFilterMultiValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] == null) return null;

            var list = values[0] as IList;
            if(list == null)
            {
                return null;
            }

            var columns = list.Cast<CustomGridColumn>();
            string filter = values[1].ToString();
            if (string.IsNullOrEmpty(filter))
            {
                return columns.OrderBy(c => c.Order);
            }
            return columns.Where(col => col.Tag != null && col.Tag.ToString().Equals(filter)).OrderBy(col => col.Order);
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
