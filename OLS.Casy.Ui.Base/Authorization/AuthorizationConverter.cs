using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OLS.Casy.Ui.Base.Authorization
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal class AuthorizationConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var result = value as AuthorizationResult;

            if (targetType == typeof(AuthorizationResult))
            {
                return result;
            }

            if (targetType == typeof(string))
            {
                return result == AuthorizationResult.Allowed ? "Allowed" : (result == null ? "Error" : result.ErrorMessage);
            }

            if (targetType == typeof(Visibility))
            {
                return result == AuthorizationResult.Allowed ? Visibility.Visible : Visibility.Collapsed;
            }

            if (targetType == typeof(bool))
            {
                return result == AuthorizationResult.Allowed ? true : false;
            }

            if (targetType == typeof(object))
            {
                return result;
            }

            throw new NotSupportedException(string.Format(culture,
                                                        "An AuthorizationResult cannot be converted to the type '{0}'.", targetType));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
