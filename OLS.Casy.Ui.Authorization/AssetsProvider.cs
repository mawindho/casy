using OLS.Casy.Core.Api;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace OLS.Casy.Ui.Authorization
{
    /// <summary>
    /// Implementation of <see cref="IAssetsProvider"/> to provide assets defined in the OLS.Casy.Ui.MainControls assembly 
    /// </summary>
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IAssetsProvider))]
    public class AssetsProvider : IAssetsProvider
    {
        /// <summary>
        /// Poperty for a list of Uri-strings pointing the assmbly specific assets (e.g. ResourceDictionaries)
        /// </summary>
        public IEnumerable<string> AssetUris
        {
            get
            {
                return new[]
                    {
                        @"pack://application:,,,/OLS.Casy.Ui.Authorization;component\Assets\ViewModelViewMappings.xaml"
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
