using OLS.Casy.Core.Api;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace OLS.Casy.Ui.Core
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IAssetsProvider))]
    public class AssetsProvider : IAssetsProvider
    {
        public IEnumerable<string> AssetUris
        {
            get
            {
                return new[]
                    {
                        @"pack://application:,,,/OLS.Casy.Ui.Core;component\Assets\ViewModelViewMappings.xaml"
                    };
            }
        }

        /// <summary>
        /// Property to set current thread culture of the assembly according to current application culture from outside
        /// </summary>
        public void SetCurrentCulture(CultureInfo cutureInfo)
        {
            Thread.CurrentThread.CurrentCulture = cutureInfo;
            Thread.CurrentThread.CurrentUICulture = cutureInfo;
        }

        //public void SetCurrentKeybordCulture(CultureInfo cultureInfo)
        //{
        //    Application.Current.Dispatcher.Invoke(() =>
        //    {
        //        InputLanguageManager.Current.CurrentInputLanguage = cultureInfo;
        //    });
        //}
    }
}
