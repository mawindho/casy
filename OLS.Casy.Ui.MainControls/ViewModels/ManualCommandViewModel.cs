using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MahApps.Metro.IconPacks;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.MainControls.Api;

namespace OLS.Casy.Ui.MainControls.ViewModels
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export("UserMenuCommand", typeof(CommandViewModel))]
    public class ManualCommandViewModel : CommandViewModel, IPartImportsSatisfiedNotification
    {
        private const string ManualFileName = @"Data\OLS_ CASY_TTT-OperatorsGuide_2018-8.pdf";
        private readonly ILocalizationService _localizationService;

        [ImportingConstructor]
        public ManualCommandViewModel(ILocalizationService localizationService)
        {
            _localizationService = localizationService;

            Order = 3;
            AwesomeGlyph = PackIconFontAwesomeKind.BookOpenSolid;
        }

        private void OnManualPressed()
        {
            Process.Start(ManualFileName);
        }

        public void OnImportsSatisfied()
        {
            DisplayName = _localizationService.GetLocalizedString("UserMenu_Button_Manual");
            Command = new OmniDelegateCommand(OnManualPressed);
        }
    }
}
