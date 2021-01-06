using System;
using OLS.Casy.App.Controls;
using Xamarin.Forms;
using UI = Windows.UI;

namespace OLS.Casy.App.UWP.Converters
{
    public class TabBadgeTextConverter : UI.Xaml.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (!(value is Page page)) return string.Empty;
            var badgeText = CustomTabbedPage.GetBadgeText(page);

            return badgeText;

        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }
}
