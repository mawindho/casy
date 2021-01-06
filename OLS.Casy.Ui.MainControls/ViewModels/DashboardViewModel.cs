using OLS.Casy.Controller.Api;
using OLS.Casy.Core.Activation;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.MainControls.Api;
using OLS.Casy.Ui.MainControls.Views;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OLS.Casy.Ui.MainControls.ViewModels
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(HostViewModel))]
    public class DashboardViewModel : HostViewModel, IPartImportsSatisfiedNotification
    {
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly IAppService _appService;
        private readonly IEnumerable<IDashboardPageViewModel> _dashboardPagesInternal;
        private IDashboardPageViewModel _selectedItem;
        private readonly IAuthenticationService _authenticationService;
        private readonly ICasyController _casyController;
        private readonly IEnvironmentService _environmentService;
        private readonly ICompositionFactory _compositionFactory;

        [ImportingConstructor]
        public DashboardViewModel(
            IEventAggregatorProvider eventAggregatorProvider,
             IAppService appService,
             IAuthenticationService authenticationService,
             ICasyController casyController,
             IEnvironmentService environmentService,
             ICompositionFactory compositionFactory,
            [ImportMany(typeof(IDashboardPageViewModel))] IEnumerable<IDashboardPageViewModel> dashboardPageViewModels)
        {
            _eventAggregatorProvider = eventAggregatorProvider;
            _appService = appService;
            _dashboardPagesInternal = dashboardPageViewModels;
            _authenticationService = authenticationService;
            _casyController = casyController;
            _environmentService = environmentService;
            _compositionFactory = compositionFactory;
        }

        public override bool IsActive
        {
            get => base.IsActive;

            set
            {
                base.IsActive = value;
                NotifyOfPropertyChange();
            }
        }

        public List<IDashboardPageViewModel> DashboardPages => new List<IDashboardPageViewModel>(_dashboardPagesInternal.OrderBy(x => x.Order));

        public IDashboardPageViewModel SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                NotifyOfPropertyChange();

            }
        }

        public string VersionNumber => this._appService.Version;

        public string SerialNumberAndModules
        {
            get
            {
                string serialNumber = string.Empty;
                if (_casyController.IsConnected)
                {
                    serialNumber = _casyController.GetSerialNumber();
                }

                var license = _environmentService.GetEnvironmentInfo("License") as License;
                return
                    $"{(serialNumber == null ? "" : $"{serialNumber} -")}{(license == null ? string.Empty : string.Join(";", license.AddOns))}";
            }
        }

        public string UserName => _authenticationService.LoggedInUser == null ? string.Empty : _authenticationService.LoggedInUser.FirstName.ToUpper();

        public ICommand AboutCommand => new OmniDelegateCommand(OnAbout);

        private void OnAbout()
        {
            Task.Factory.StartNew(() =>
            {
                var awaiter = new ManualResetEvent(false);
                var viewModelExport = this._compositionFactory.GetExport<AboutViewModel>();
                var viewModel = viewModelExport.Value;

                ShowCustomDialogWrapper wrapper = new ShowCustomDialogWrapper
                {
                    Awaiter = awaiter,
                    DataContext = viewModel,
                    DialogType = typeof(AboutDialog)
                };

                _eventAggregatorProvider.Instance.GetEvent<ShowCustomDialogEvent>().Publish(wrapper);

                if (awaiter.WaitOne())
                {
                }
            });
        }

        public void OnImportsSatisfied()
        {
            _eventAggregatorProvider.Instance.GetEvent<NavigateToEvent>().Subscribe(OnNavigateToEvent);
            _authenticationService.UserLoggedIn += OnUserLoggedIn;
            IsActive = true;
            _casyController.OnIsConnectedChangedEvent += (s, e) => NotifyOfPropertyChange("SerialNumberAndModules");
        }

        private void OnUserLoggedIn(object sender, AuthenticationEventArgs e)
        {
            NotifyOfPropertyChange("UserName");
        }

        private void OnNavigateToEvent(object argument)
        {
            var navigationArgs = (NavigationArgs)argument;
            switch (navigationArgs.NavigationCategory)
            {
                case NavigationCategory.Dashboard:
                    IsActive = true;
                    break;
                case NavigationCategory.Analyse:
                    if (IsActive && navigationArgs.Parameter != null)
                    {
                        _eventAggregatorProvider.Instance.GetEvent<NavigateToEvent>().Publish(new NavigationArgs(NavigationCategory.AnalyseGraph));
                    }
                    break;
                case NavigationCategory.MeasureResults:
                case NavigationCategory.Template:
                    break;
                default:
                    IsActive = false;
                    break;
            }
        }
    }
}
