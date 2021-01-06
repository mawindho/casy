using System;
using System.Threading.Tasks;
using OLS.Casy.App.Services;
using OLS.Casy.App.Services.Detection;
using OLS.Casy.App.Services.Navigation;
using OLS.Casy.App.Services.Settings;
using OLS.Casy.App.ViewModels.Base;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace OLS.Casy.App
{
    public partial class App : Application
    {
        private ISettingsService _settingsService;

        public App()
        {
            InitializeComponent();

            InitApp();
            if (Device.RuntimePlatform == Device.UWP)
            {
                InitNavigation();
            }
        }

        private void InitApp()
        {
            _settingsService = ViewModelLocator.Resolve<ISettingsService>();
        }

        private Task InitNavigation()
        {
            var navigationService = ViewModelLocator.Resolve<INavigationService>();
            return navigationService.InitializeAsync();
        }

        protected override async void OnStart()
        {
            // Handle when your app starts
            base.OnStart();

            if (Device.RuntimePlatform != Device.UWP)
            {
                await InitNavigation();
            }

            var detectionService = ViewModelLocator.Resolve<IDetectionService>();
            detectionService.Initialize();

            base.OnResume();
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
            base.OnSleep();
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
            base.OnResume();
        }
    }
}
