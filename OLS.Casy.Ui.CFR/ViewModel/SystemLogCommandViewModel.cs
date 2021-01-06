using System.ComponentModel.Composition;
using System.Threading.Tasks;
using MahApps.Metro.IconPacks;
using OLS.Casy.Controller.Api;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Ui.AuditTrail.Views;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.MainControls.Api;

namespace OLS.Casy.Ui.AuditTrail.ViewModel
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export("UserMenuCommand", typeof(CommandViewModel))]
    public class SystemToolCommandViewModel : CommandViewModel, IPartImportsSatisfiedNotification
    {
        private readonly ILocalizationService _localizationService;
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly IAuthenticationService _authenticationService;
        private readonly ICompositionFactory _compositionFactory;
        private readonly ICalibrationController _calibrationController;

        [ImportingConstructor]
        public SystemToolCommandViewModel(
            ILocalizationService localizationService,
             IEventAggregatorProvider eventAggregatorProvider,
             IAuthenticationService authenticationService,
             ICompositionFactory compositionFactory,
             [Import(AllowDefault = true)] ICalibrationController calibrationController
            )
        {
            _localizationService = localizationService;
            _eventAggregatorProvider = eventAggregatorProvider;
            _authenticationService = authenticationService;
            _calibrationController = calibrationController;
            _compositionFactory = compositionFactory;

            AwesomeGlyph = PackIconFontAwesomeKind.ListUlSolid;
            Order = 4;
        }

        public virtual void OnImportsSatisfied()
        {
            MinRequiredRole = _authenticationService.GetRoleByName("Operator");

            OnLanguageChanged(null, null);
            Command = new OmniDelegateCommand(OnShowSystemLog);

            _localizationService.LanguageChanged += OnLanguageChanged;

            IsVisible = _calibrationController != null;
        }

        private void OnLanguageChanged(object sender, LocalizationEventArgs e)
        {
            DisplayName = _localizationService.GetLocalizedString("UserMenu_Button_SystemLog");
        }

        private void OnShowSystemLog()
        {
            Task.Factory.StartNew(() =>
            {
                var awaiter = new System.Threading.ManualResetEvent(false);

                var viewModelExport = _compositionFactory.GetExport<SystemLogViewModel>();
                var viewModel = viewModelExport.Value;

                var wrapper = new ShowCustomDialogWrapper
                {
                    Awaiter = awaiter,
                    DataContext = viewModel,
                    DialogType = typeof(SystemLogView)
                };

                _eventAggregatorProvider.Instance.GetEvent<ShowCustomDialogEvent>().Publish(wrapper);

                if (awaiter.WaitOne())
                {
                }

                _compositionFactory.ReleaseExport(viewModelExport);
            });
        }
    }
}
