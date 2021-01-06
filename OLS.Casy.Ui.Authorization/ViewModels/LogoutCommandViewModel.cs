using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Core.Api;
using OLS.Casy.Ui.MainControls.Api;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System;
using OLS.Casy.Core.Authorization.Local;
using DevExpress.Mvvm;
using MahApps.Metro.IconPacks;

namespace OLS.Casy.Ui.Authorization.ViewModels
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export("UserMenuCommand", typeof(CommandViewModel))]
    public class LogoutCommandViewModel : CommandViewModel, IPartImportsSatisfiedNotification
    {
        private readonly ILocalizationService _localizationService;
        private readonly LocalAuthenticationService _authenticationService;
        private readonly IMeasureResultManager _measureResultManager;

        [ImportingConstructor]
        public LogoutCommandViewModel(ILocalizationService localizationService,
            LocalAuthenticationService authenticationService,
            IMeasureResultManager measureResultManager)
        {
            this._localizationService = localizationService;
            this._authenticationService = authenticationService;
            this._measureResultManager = measureResultManager;

            this.Order = 4;
            this.AwesomeGlyph = PackIconFontAwesomeKind.SignOutAltSolid;
        }

        private void OnPowerPressed()
        {
            Task.Factory.StartNew(async () =>
            {
                var result = await this._measureResultManager.SaveChangedMeasureResults();
                if (result != Casy.Core.Api.ButtonResult.Cancel)
                {
                    _authenticationService.LogOut();
                }
            });
        }

        public void OnImportsSatisfied()
        {
            this.DisplayName = _localizationService.GetLocalizedString("UserMenu_Button_Logout");
            this.Command = new OmniDelegateCommand(OnPowerPressed);

            this._authenticationService.AutoLogOffRaised += OnAutoLogOffRaised;
        }

        private void OnAutoLogOffRaised(object sender, EventArgs e)
        {
            Task.Factory.StartNew(async () =>
            {
                await _measureResultManager.SaveChangedMeasureResults();
            });
        }
    }
}
