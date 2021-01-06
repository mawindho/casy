using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.MainControls.Api;
using OLS.Casy.Ui.MainControls.Views;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using MahApps.Metro.IconPacks;

namespace OLS.Casy.Ui.MainControls.ViewModels
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export("UserMenuCommand", typeof(CommandViewModel))]
    public class SettingsCommandViewModel : CommandViewModel, IPartImportsSatisfiedNotification
    {
        private readonly ILocalizationService _localizationService;
        private readonly ICompositionFactory _compositionFactory;
        private readonly IEventAggregatorProvider _eventAggregatorProvider;

        [ImportingConstructor]
        public SettingsCommandViewModel(
            ILocalizationService localizationService, 
            ICompositionFactory compositionFactory,
            IEventAggregatorProvider eventAggregatorProvider,
            IAuthenticationService authenticationService)
        {
            _localizationService = localizationService;
            _compositionFactory = compositionFactory;
            _eventAggregatorProvider = eventAggregatorProvider;

            Order = 0;
            AwesomeGlyph = PackIconFontAwesomeKind.CogsSolid;
        }

        public virtual void OnImportsSatisfied()
        {
            OnLanguageChanged(null, null);
            Command = new OmniDelegateCommand(OnSettings);

            _localizationService.LanguageChanged += OnLanguageChanged;
            _eventAggregatorProvider.Instance.GetEvent<ShowSettingsEvent>().Subscribe(OnSettings);
        }

        private void OnSettings()
        {
            Task.Factory.StartNew(() =>
            {
                var awaiter = new System.Threading.ManualResetEvent(false);
                var viewModelExport = _compositionFactory.GetExport<SettingsContainerViewModel>();
                var viewModel = viewModelExport.Value;

                var wrapper = new ShowCustomDialogWrapper
                {
                    Awaiter = awaiter,
                    DataContext = viewModel,
                    DialogType = typeof(SettingsContainerView)
                };

                _eventAggregatorProvider.Instance.GetEvent<ShowCustomDialogEvent>().Publish(wrapper);
                if (awaiter.WaitOne())
                {
                }

                _compositionFactory.ReleaseExport(viewModelExport);
            });
        }

        private void OnLanguageChanged(object sender, LocalizationEventArgs e)
        {
            DisplayName = _localizationService.GetLocalizedString("UserMenu_Button_Settings");
        }
    }
}
