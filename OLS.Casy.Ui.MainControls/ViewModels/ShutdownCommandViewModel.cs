using OLS.Casy.Com.Api;
using OLS.Casy.Controller.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Core.Api;
using OLS.Casy.Ui.MainControls.Api;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Windows;
using MahApps.Metro.IconPacks;

namespace OLS.Casy.Ui.MainControls.ViewModels
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export("UserMenuCommand", typeof(CommandViewModel))]
    public class ShutdownCommandViewModel : CommandViewModel, IPartImportsSatisfiedNotification
    {
        private readonly ILocalizationService _localizationService;
        private readonly IMeasureResultManager _measureResultManager;
        private readonly ICasySerialPortDriver _casySerialPortDriver;
        private readonly IDatabaseStorageService _databaseStorageService;
        private readonly IServiceController _serviceController;
        private readonly IAuthenticationService _authenticationService;

        [ImportingConstructor]
        public ShutdownCommandViewModel(ILocalizationService localizationService,
            IMeasureResultManager measureResultManager,
            [Import(AllowDefault = true)] ICasySerialPortDriver casySerialPortDriver,
            IDatabaseStorageService databaseStorageService,
            [Import(AllowDefault = true)] IServiceController serviceController,
            IAuthenticationService authenticationService)
        {
            _localizationService = localizationService;
            _measureResultManager = measureResultManager;
            _casySerialPortDriver = casySerialPortDriver;
            _databaseStorageService = databaseStorageService;
            _serviceController = serviceController;
            _authenticationService = authenticationService;

            Order = 12;
            AwesomeGlyph = PackIconFontAwesomeKind.PowerOffSolid;
        }

        private void OnPowerPressed()
        {
            Task.Factory.StartNew(async () =>
            {
                var result = await this._measureResultManager.SaveChangedMeasureResults(isShutDown: true);
                if (result != Casy.Core.Api.ButtonResult.Cancel)
                {
                    if (_casySerialPortDriver != null && _serviceController != null)
                    {
                        var showShutDown = "true";
                        if (_databaseStorageService.GetSettings().TryGetValue($"ShowShutDown-{_authenticationService.LoggedInUser.Name}", out var showShutDownSetting))
                        {
                            showShutDown = showShutDownSetting.Value;
                        }
                        if (showShutDown == "true")
                        {
                            result = _serviceController.StartShutdownWizard() ? Casy.Core.Api.ButtonResult.Ok : Casy.Core.Api.ButtonResult.Cancel;
                        }
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Application.Current.Shutdown();
                    });
                }
            });
        }

        public void OnImportsSatisfied()
        {
            DisplayName = _localizationService.GetLocalizedString("UserMenu_Button_Shutdown");
            Command = new OmniDelegateCommand(OnPowerPressed);
        }
    }
}
