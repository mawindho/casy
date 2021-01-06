using MahApps.Metro.Controls;
using OLS.Casy.Activation.KeyGenerator.ViewModels;
using OLS.Casy.Activation.KeyGenerator.Views;
using OLS.Casy.Core;
using OLS.Casy.Core.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OLS.Casy.Activation.KeyGenerator
{
    public class Bootstrapper
    {
        private SplashScreenView _splashScreen;
        private SplashScreenViewModel _splashScreenViewModel;
        private IProgress<string> _splashProgrss;
        private MainViewModel _mainViewModel;

        public async Task Startup()
        {
            this._splashScreenViewModel = new SplashScreenViewModel();
            this._splashScreen = new SplashScreenView
            {
                DataContext = this._splashScreenViewModel
            };

            this._splashProgrss = new Progress<string>(this.OnSplashProgress);

            this._splashScreen.Show();

            await Task.Factory.StartNew(() =>
                this.BeforeShowWindows());
        }

        private async Task BeforeShowWindows()
        {
            GlobalCompositionContainerFactory.AssembleComponents(excludedFiles: new string[0]);

            this._splashProgrss.Report("Starting application ...");
            this._splashScreenViewModel.CompositionFactory = GlobalCompositionContainerFactory.GetExport<ICompositionFactory>().Value;

            this._splashProgrss.Report("Connecting database ...");
            this._mainViewModel = GlobalCompositionContainerFactory.GetExport<MainViewModel>().Value;

            Application.Current.Dispatcher.Invoke((Action)delegate () { DoShowWindow(); });
        }

        private void DoShowWindow()
        {
            var mainView = GlobalCompositionContainerFactory.GetExport<MetroWindow>("MainView").Value;
            Application.Current.MainWindow = mainView;
            Application.Current.MainWindow.DataContext = this._mainViewModel;
            Application.Current.MainWindow.Show();

            this._splashScreen.Close();
            this._splashScreenViewModel.Dispose();
            this._splashScreen = null;
        }

        private void OnSplashProgress(string startupInfo)
        {
           this._splashScreenViewModel.CurrentProgressText = startupInfo;
        }
    }
}
