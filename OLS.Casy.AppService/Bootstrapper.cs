using MahApps.Metro.Controls;
using OLS.Casy.AppService.ViewModels;
using OLS.Casy.AppService.Views;
using OLS.Casy.Core;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Core.Logging.Api;
using OLS.Casy.Core.Runtime.Api;
using OLS.Casy.Ui.Api;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace OLS.Casy.AppService
{
    /// <summary>
    /// Bootstrapper of the Casy application.
    /// Initializes core services and views while showing a splash screen before presenting the main window
    /// </summary>
    public class Bootstrapper : IDisposable
    {
        private SplashScreenView _splashScreen;
        private SplashScreenViewModel _splashScreenViewModel;
        private IMainViewModel _mainViewModel;
        private IProgress<string> _splashProgrss;
        private ILocalizationService _localizationService;
        private IAuthenticationService _authenticationService;
        private IRuntimeService _runtimeService;

        //private IBluetoothService _bluetoothService;

        private CancelEventHandler _cancelEventHandler;

        /// <summary>
        /// Starts the bootstrapping of the application
        /// </summary>
        /// <param name="excludedFiles">List of files not to be included in MEF container</param>
        public async Task Startup(CancelEventHandler cancelEventHandler)
        {
            _cancelEventHandler = cancelEventHandler;

            ImageSource icon = null;
            try
            {
                icon = BitmapFrame.Create(new Uri("pack://application:,,,/OLS.Casy.Ui;component/Assets/Icons/logo.png", UriKind.RelativeOrAbsolute));
            }
            catch (IOException)
            {
                //this._logger.Error("Applciation icon not found.", () => this.DoShowWindow());
            }

            // FIrstly show splash screen to inform the user about progress
            this._splashScreenViewModel = new SplashScreenViewModel();
            this._splashScreen = new SplashScreenView
            {
                DataContext = this._splashScreenViewModel,
                Icon = icon
            };
            // Progress will be displayed in spash screen
            this._splashProgrss = new Progress<string>(this.OnSplashProgress);

            this._splashScreen.Show();

            // Start initialization procress in different thread to not freeze application
            await Task.Factory.StartNew(() =>
                this.BeforeShowWindows()//.ContinueWith(this.DoShowWindow())
                );//async task =>
                //{
                    //await Application.Current.Dispatcher.BeginInvoke((Action)delegate ()
                    //{
                  //      this.DoShowWindow();
                    //});
                //}));
        }

        private void LoadReferencedAssembly(Assembly assembly)
        {
            foreach (AssemblyName name in assembly.GetReferencedAssemblies())
            {
                if (!AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName == name.FullName))
                {
                    this.LoadReferencedAssembly(Assembly.Load(name));
                }
            }
        }

        private async Task BeforeShowWindows()
        {
            //this._bluetoothService = new BluetoothService();
            //Task.Factory.StartNew(async () =>
            //{
            //    await _bluetoothService.InitializeServiceAsync();
            //});

            // Initialize IoC-Container


            // Initialize localization service because it's already neccessary for splash screen
            try
            {
                //foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                //{
//                    this.LoadReferencedAssembly(assembly);
                //}

                FunctionsAssemblyResolver.RedirectAssembly();

                _localizationService = GlobalCompositionContainerFactory.GetExport<ILocalizationService>().Value;
                _splashProgrss.Report(_localizationService.GetLocalizedString("SplashScreen_Message_StartUpApplication"));

                await CleanUpInstallation();

                // Get all asset providers to register assemblies ressources in application resources
                IEnumerable<Lazy<IAssetsProvider>> assetsProviders =
                    GlobalCompositionContainerFactory.GetExports<IAssetsProvider>();

                foreach (Lazy<IAssetsProvider> assetsProvider in assetsProviders)
                {
                    foreach (string uiString in assetsProvider.Value.AssetUris)
                    {
                        var rd = new ResourceDictionary
                        {
                            Source = new Uri(uiString)
                        };
                        Application.Current.Resources.MergedDictionaries.Add(rd);
                    }
                }

                this._splashScreenViewModel.LocalizationService = _localizationService;
                this._splashScreenViewModel.CompositionFactory = GlobalCompositionContainerFactory.GetExport<ICompositionFactory>().Value;

                Globals.SplashProgress = _splashProgrss;
                Globals.ShowSplashCustomDialogDelegate = (object wrapper) =>
                    this._splashScreenViewModel.ShowCustomDialog(wrapper as ShowCustomDialogWrapper);
                Globals.ShowSplashMessageDialogDelegate = (object wrapper) =>
                    this._splashScreenViewModel.ShowMessageBox(wrapper as ShowMessageBoxDialogWrapper);
                Globals.ShowSplashProgressDialogDelegate = (object wrapper) =>
                    this._splashScreenViewModel.ShowProgress(wrapper as ShowProgressDialogWrapper);

                this._splashProgrss.Report(_localizationService.GetLocalizedString("Preparing application ..."));

                _runtimeService = GlobalCompositionContainerFactory.GetExport<IRuntimeService>().Value;
                _runtimeService.Initialize(_splashProgrss);

                //#if DEBUG
                //#else

                this._splashProgrss.Report(_localizationService.GetLocalizedString("Checking activation status ..."));
                var activationService = GlobalCompositionContainerFactory.GetExport<IActivationService>().Value;
                var isActivated = await activationService.CheckActivation(
                    (object wrapper) => this._splashScreenViewModel.ShowMessageBox(wrapper as ShowMessageBoxDialogWrapper),
                    (object wrapper) => this._splashScreenViewModel.ShowCustomDialog(wrapper as ShowCustomDialogWrapper),
                    _splashProgrss);

                if (isActivated)
                {
                    _mainViewModel = GlobalCompositionContainerFactory.GetExport<IMainViewModel>().Value;

                        Application.Current.Dispatcher.Invoke((Action)delegate () { DoShowWindow(); });
                    
                    //#endif
                    // Initialize all runtime services
                    

                    // Initialize the main view model
                    
                    //#if DEBUG
                    //#else

                }
                else
                {
                    Environment.Exit(1);
                }

                //#endif
            }
            catch(CompositionException cex)
            {
                App.LogUnhandledException(cex);
                MessageBox.Show("CASY installation seems to be broken. Please contact support.");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Application.Current.Shutdown(1);
                });
            }
            catch(Exception ex)
            {
                App.LogUnhandledException(ex);
                MessageBox.Show("An error occured while starting CASY application. Please contact support.");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Application.Current.Shutdown(1);
                });
            }
        }

        private void DoShowWindow()
        {
            ImageSource icon = null;
            try
            {
                icon = BitmapFrame.Create(new Uri("pack://application:,,,/OLS.Casy.Ui;component/Assets/Icons/logo.png", UriKind.RelativeOrAbsolute));
            }
            catch (IOException)
            {
                //this._logger.Error("Applciation icon not found.", () => this.DoShowWindow());
            }

            this._splashProgrss.Report(_localizationService.GetLocalizedString("Starting authentication module ..."));
            this._authenticationService = GlobalCompositionContainerFactory.GetExport<IAuthenticationService>().Value;
            this._authenticationService.AuthenticateCurrentUser();

            // Initialize and show the Main Windows
            var mainView = GlobalCompositionContainerFactory.GetExport<MetroWindow>("MainView").Value;
            Application.Current.MainWindow = mainView;
            Application.Current.MainWindow.DataContext = this._mainViewModel;
            Application.Current.MainWindow.Icon = icon;
            Application.Current.MainWindow.Closing += _cancelEventHandler;
            Application.Current.MainWindow.Show();

            this._splashScreen.Close();
            this._splashScreenViewModel.Dispose();
            this._splashScreen = null;

            if (this.OnFinisched != null)
            {
                OnFinisched.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnSplashProgress(string startupInfo)
        {
            Application.Current.Dispatcher.Invoke(() => this._splashScreenViewModel.CurrentProgressText = startupInfo, DispatcherPriority.Send);
        }

        private async Task CleanUpInstallation()
        {
            this._splashProgrss.Report(_localizationService.GetLocalizedString("Cleaning up installation ..."));

            DirectoryInfo appDirInfo = new DirectoryInfo(System.AppDomain.CurrentDomain.BaseDirectory);
            var oldFiles = appDirInfo.GetFiles("*.old", SearchOption.AllDirectories);

            foreach (var oldFileName in oldFiles)
            {
                if (File.Exists(oldFileName.FullName))
                {
                    File.Delete(oldFileName.FullName);
                }
            }
            
            var tempFiles = appDirInfo.GetFiles("*.temp");
            foreach (var tempFile in tempFiles)
            {
                if (File.Exists(tempFile.FullName))
                {
                    File.Delete(tempFile.FullName);
                }
            }
        }

        internal event EventHandler OnFinisched;

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this._splashScreenViewModel.Dispose();
                }

                disposedValue = true;
            }
        }

        ~Bootstrapper()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
