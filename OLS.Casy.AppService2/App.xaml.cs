using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using DevExpress.Xpf.Editors;
using OLS.Casy.Ui.Api;
using OLS.Casy.Ui.Base.Controls;
using OLS.Casy.Ui.Core.TipTap;

namespace OLS.Casy.AppService2
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

            Current.Dispatcher.Thread.CurrentCulture = Thread.CurrentThread.CurrentCulture;
            Current.Dispatcher.Thread.CurrentUICulture = Thread.CurrentThread.CurrentUICulture;

            //DevExpress.Xpf.Core.DXGridDataController.DisableThreadingProblemsDetection = true;

            var useTipTap = ConfigurationManager.AppSettings["UseTipTap"];
            if (string.IsNullOrEmpty(useTipTap)) return;
            if (!bool.TryParse(useTipTap, out var useTipTapBool)) return;
            if (!useTipTapBool) return;
            TabTipAutomation.IgnoreHardwareKeyboard = HardwareKeyboardIgnoreOptions.IgnoreAll;
            TabTipAutomation.BindTo<OmniTextBox>();
            TabTipAutomation.BindTo<PasswordBox>();
            TabTipAutomation.BindTo<TextEdit>();
            //TabTipAutomation.BindTo<ComboBoxEdit>();
        }
    }
}
