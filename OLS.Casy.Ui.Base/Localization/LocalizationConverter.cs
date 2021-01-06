using System;
using System.Globalization;
using System.Windows.Data;

namespace OLS.Casy.Ui.Base.Localization
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal class LocalizationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var result = value as LocalizationResult;

            if (result != null)
            {
                if (targetType == typeof(LocalizationResult))
                {
                    return result;
                }

                if (targetType == typeof(string) || targetType == typeof(object))
                {
                    return !string.IsNullOrEmpty(result.ErrorMessage) ? result.ErrorMessage : result.LocalizedObject;
                }

                throw new NotSupportedException(string.Format(culture, "An LocalizationResult cannot be converted to the type '{0}'.", targetType));
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
