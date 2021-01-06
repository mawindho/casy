using ControlzEx.Theming;
using DevExpress.Xpf.Editors;
using MahApps.Metro;
using MahApps.Metro.Theming;
using OLS.Casy.Core;
using OLS.Casy.Core.Runtime.Api;
using OLS.Casy.Ui.Api;
using OLS.Casy.Ui.Base.Controls;
using OLS.Casy.Ui.Core.TipTap;
using OLS.Casy.Ui.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace OLS.Casy.AppService
{
    /// <summary>
    /// This is the entry-point of the Casy application.
    /// Other than a normal WPF application it's starting a bootstrapper and shows a splash screen
    /// before launching the main window.
    /// </summary>
    public partial class App : Application
    {
        private Bootstrapper _bootstrapper;
        private bool _isDeinitialized;

        public App()
        {
            //Set a default language (English) of UI and dispatcher thread. 
            //Will be overwritten by localization service
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

            Current.Dispatcher.Thread.CurrentCulture = Thread.CurrentThread.CurrentCulture;
            Current.Dispatcher.Thread.CurrentUICulture = Thread.CurrentThread.CurrentUICulture;

            DevExpress.Xpf.Core.DXGridDataController.DisableThreadingProblemsDetection = true;

            var useTipTap = ConfigurationManager.AppSettings["UseTipTap"];
            if (string.IsNullOrEmpty(useTipTap)) return;
            if (!bool.TryParse(useTipTap, out var useTipTapBool)) return;
            if (!useTipTapBool) return;
            TabTipAutomation.IgnoreHardwareKeyboard = HardwareKeyboardIgnoreOptions.IgnoreAll;
            TabTipAutomation.BindTo<OmniTextBox>();
            TabTipAutomation.BindTo<PasswordBox>();
            TabTipAutomation.BindTo<TextEdit>();
            TabTipAutomation.BindTo<ComboBoxEdit>();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            //ThemeManager.AddAccent("CasyAccent", new Uri("pack://application:,,,/OLS.Casy.AppService;component/Assets/CasyAccent.xaml"));
            //Tuple<AppTheme, Accent> theme = ThemeManager.DetectAppStyle(Current);
            //ThemeManager.ChangeAppStyle(Current, ThemeManager.GetAccent("CasyAccent"), theme.Item1);

            var theme = ThemeManager.Current.AddLibraryTheme(new LibraryTheme(new Uri("pack://application:,,,/OLS.Casy.AppService;component/Assets/CasyAccent.xaml"), MahAppsLibraryThemeProvider.DefaultInstance));
            ThemeManager.Current.ChangeTheme(this, theme);

            base.OnStartup(e);

            DispatcherUnhandledException += OnDispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            Current.DispatcherUnhandledException += OnDispatcherUnhandledException;

            // Check for another instance
            var process = Process.GetProcessesByName("OLS.Casy.AppService");
            if (process.Length > 1)
            {
                MessageBox.Show("Another instance is already running. Please shut down the other instance.");
                Environment.Exit(1);
            }
            else
            {
                IList<string> excludedFiles = new List<string>
                {
                    "OLS.Casy.Core.Update.Ui.exe", "OLS.Casy.Authorization.ActiveDirectory.SetGroupsUtil.exe"
                };

                try
                {
                    GlobalCompositionContainerFactory.AssembleComponents(excludedFiles: excludedFiles);
                }
                catch(Exception ex)
                {
                    MessageBox.Show("CASY installation seems to be broken. Please contact support.");
                    LogUnhandledException(ex);
                }

                //Constuct an start up the app's bootstrapper
                _bootstrapper = new Bootstrapper();
                await _bootstrapper.Startup(MainWindowClosing);
                //Application.Current.MainWindow.Closing += this.MainWindowClosing;
                _bootstrapper = null;
            }
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogUnhandledException(e.ExceptionObject as Exception);
        }

        private static void OnUnobservedException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            LogUnhandledException(e.Exception);
        }

        private void MainWindowClosing(object sender, CancelEventArgs e)
        {
            if (!(sender is MainView)) return;

            if (Current.MainWindow != null)
                Current.MainWindow.Closing -= MainWindowClosing;
            DispatcherUnhandledException -= OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
            Current.DispatcherUnhandledException -= OnDispatcherUnhandledException;
            Deinitialize();
            Current.Shutdown();
        }

        private void Deinitialize()
        {
            if (_isDeinitialized) return;

            _isDeinitialized = true;

            var runtimeServiceExport = GlobalCompositionContainerFactory.GetExport<IRuntimeService>();
            var runtimeService = runtimeServiceExport.Value;
            runtimeService?.Deinitialize(new Progress<string>());

            GlobalCompositionContainerFactory.Dispose();
        }

        private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs dispatcherUnhandledExceptionEventArgs)
        {
            LogUnhandledException(dispatcherUnhandledExceptionEventArgs.Exception);
        }

        public static void LogUnhandledException(Exception exception)
        {
            var unhandledExceptionText = new StringBuilder();

            unhandledExceptionText.AppendLine($"Error Report {DateTime.Now:yyyy-MM-dd HH':'mm':'ss}:");
            unhandledExceptionText.AppendLine("Last user activities:");
            foreach (var entry in InteractionLogProvider.InteractionLog)
            {
                unhandledExceptionText.AppendLine(entry);
            }

            if (exception != null)
            {

                unhandledExceptionText.AppendLine(
                    $"{DateTime.Now:yyyy-MM-dd HH':'mm':'ss}: Unhandled exception caught: {exception.Message}; {exception.StackTrace};");

                if (exception is ReflectionTypeLoadException le1)
                {
                    foreach (var e in le1.LoaderExceptions)
                    {
                        unhandledExceptionText.AppendLine(
                            $"{DateTime.Now:yyyy-MM-dd HH':'mm':'ss}: Unhandled loader exception: {e.Message}; {e.StackTrace};");
                    }
                }

                exception = exception.InnerException;
                while (exception != null)
                {
                    unhandledExceptionText.AppendLine(
                        $"{DateTime.Now:yyyy-MM-dd HH':'mm':'ss}: Unhandled inner exception: {exception.Message}; {exception.StackTrace};");
                    exception = exception.InnerException;

                    if (!(exception is ReflectionTypeLoadException)) continue;
                    var le = (ReflectionTypeLoadException) exception;

                    foreach (var e in le.LoaderExceptions)
                    {
                        unhandledExceptionText.AppendLine(
                            $"{DateTime.Now:yyyy-MM-dd HH':'mm':'ss}: Unhandled inner loader exception {e.Message}; {e.StackTrace};");
                    }
                }
            }
            else
            {
                unhandledExceptionText.AppendLine("AppService has unhandled exception catched.");
            }

            var path = @"UnhandledException.txt";
            //if (!File.Exists(path))
            //{
            File.WriteAllText(path, unhandledExceptionText + Environment.NewLine);

            Process.Start("OLS.Casy.ErrorReport.Ui.exe");
            //}
            //else
            //{
            //  File.AppendAllText(path, unhandledExceptionText + Environment.NewLine);
            //}
        }
    }
}
