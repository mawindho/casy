using System;
using Xamarin.Forms;
using UI = Windows.UI;

namespace OLS.Casy.App.UWP.Converters
{
    public class TabIconConverter : UI.Xaml.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            //if (value != null && value is Xamarin.Forms.FileImageSource)
            //return ((Xamarin.Forms.FileImageSource)value).File;

            //return null;

            if (value is FileImageSource)
            {
                return string.Format("ms-appx:///{0}", ((FileImageSource)value).File);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }
}
