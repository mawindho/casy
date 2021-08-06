using DevExpress.Mvvm;
using OLS.Casy.Controller.Api;
using OLS.Casy.Core;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Core.Api;
using OLS.Casy.Ui.MainControls.Api;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OLS.Casy.Ui.MainControls.ViewModels
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(TopMenuViewModel))]
    public class TopMenuViewModel : Base.ViewModelBase, IPartImportsSatisfiedNotification
    {
        private readonly ILocalizationService _localizationService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly IMeasureController _measureController;
        private readonly IMeasureResultManager _measureResultManager;
        private readonly ICompositionFactory _compositionFactory;
        private readonly IServiceController _serviceController;
        private readonly IUIProjectManager _uiProject;
        private readonly Casy.Core.Notification.Api.INotificationService _notificationService;
        private readonly IMeasureCounter _measureCounter;
        private readonly IEnvironmentService _environmentService;

        private readonly IEnumerable<CommandViewModel> _userMenuCommandsInternal;
        private bool _isUndoEnabled;
        private bool _isRedoEnabled;
        private bool _isNotificationsOpen;
        private bool _isCasyConnected;
        private bool _isVisible;
        private bool _isLoading;
        private bool _isPrintAllEnabled;
        private NavigationCategory _navigationCategory;

        [ImportingConstructor]
        public TopMenuViewModel(ILocalizationService localizationService,
            IAuthenticationService authenticationService,
            [ImportMany("UserMenuCommand")] IEnumerable<CommandViewModel> userMenuCommands,
            IEventAggregatorProvider eventAggregatorProvider,
            [Import(AllowDefault = true)] IMeasureController measureController,
            IMeasureResultManager measureResultManager,
            ICompositionFactory compositionFactory,
            [Import(AllowDefault = true)] IServiceController serviceController,
            IUIProjectManager uiProject,
            Casy.Core.Notification.Api.INotificationService notificationService,
            [Import(AllowDefault = true)] IMeasureCounter measureCounter,
            IEnvironmentService environmentService)
        {
            _localizationService = localizationService;
            _authenticationService = authenticationService;
            _userMenuCommandsInternal = userMenuCommands;
            _eventAggregatorProvider = eventAggregatorProvider;
            _measureController = measureController;
            _measureResultManager = measureResultManager;
            _compositionFactory = compositionFactory;
            _serviceController = serviceController;
            _uiProject = uiProject;
            _notificationService = notificationService;
            _measureCounter = measureCounter;
            _environmentService = environmentService;

            UserMenuCommands = new ObservableCollection<CommandViewModel>();
            Notifications = new ObservableCollection<NotificationViewModel>();
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsMeasureControllerLoaded
        {
            get => _serviceController != null;
        }

        public ObservableCollection<NotificationViewModel> Notifications { get; }

        public bool HasUnreadNotifications
        {
            get { return _notificationService.Notifications.Any(n => n.IsUnread); }
        }

        public bool IsNotificationsOpen
        {
            get { return _isNotificationsOpen; }
            set
            {
                if(value != _isNotificationsOpen)
                {
                    _isNotificationsOpen = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                _isVisible = value;
                OnNotificationsChanged(null, null);
            }
        }

        public string UserMenuButtonText
        {
            get
            {
                if (_authenticationService.LoggedInUser == null)
                {
                    return string.Empty;
                }

                if (string.IsNullOrEmpty(_authenticationService.LoggedInUser.FirstName) && string.IsNullOrEmpty(_authenticationService.LoggedInUser.LastName))
                {
                    return string.Format(_localizationService.GetLocalizedString("TopMenuView_UserMenu_Content"), _authenticationService.LoggedInUser.Identity.Name);
                }

                return string.Format(_localizationService.GetLocalizedString("TopMenuView_UserMenu_Content"), string.Format("{0} {1}", _authenticationService.LoggedInUser.FirstName, _authenticationService.LoggedInUser.LastName));
            }
        }

        public string UserMenuButtonTextUserNameRole
        {
            get
            {
                if (_authenticationService.LoggedInUser == null)
                {
                    return string.Empty;
                }

                return string.Format(_localizationService.GetLocalizedString("TopMenuView_UserMenu_Role_Content"), _authenticationService.LoggedInUser.UserRole.Name);
            }
        }

        public ImageBrush LoggedInUserImage
        {
            get
            {
                if (_authenticationService.LoggedInUser == null)
                {
                    return null;
                }

                ImageBrush brush = new ImageBrush();
                var bi = LoadImage(_authenticationService.LoggedInUser.Image);
                brush.ImageSource = bi;
                return brush;
            }   
        }

        public ObservableCollection<CommandViewModel> UserMenuCommands { get; }

        public ICommand MeasureCommand
        {
            get { return new OmniDelegateCommand(OnMeasure); }
        }

        public ICommand PurgeCommand
        {
            get { return new OmniDelegateCommand<object>(OnPurge); }
        }

        public ICommand BackgroundCommand
        {
            get { return new OmniDelegateCommand(OnBackground); }
        }

        public ICommand UndoCommand
        {
            get { return new OmniDelegateCommand(OnUndo); }
        }

        public bool IsUndoEnabled
        {
            get { return this._isUndoEnabled; }
            set
            {
                if(value != this._isUndoEnabled)
                {
                    this._isUndoEnabled = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public ICommand RedoCommand
        {
            get { return new DelegateCommand(OnRedo); }
        }

        public bool IsRedoEnabled
        {
            get { return this._isRedoEnabled; }
            set
            {
                if (value != this._isRedoEnabled)
                {
                    this._isRedoEnabled = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public ICommand PrintAllCommand
        {
            get => new DelegateCommand(OnPrintAll);
        }

        private void OnPrintAll()
        {
            _eventAggregatorProvider.Instance.GetEvent<PrintAllMeasurementsEvent>().Publish();
        }

        public bool IsPrintAllEnabled
        {
            get { return this._isPrintAllEnabled && _measureResultManager.SelectedMeasureResults.Any(); }
            set
            {
                if (value != this._isPrintAllEnabled)
                {
                    this._isPrintAllEnabled = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string CountsLeft
        {
            get { return this._measureCounter == null ? string.Empty : _localizationService.GetLocalizedString("TopMenuView_CountsLeft", _measureCounter.GetAvailableCounts().ToString()); }
        }

        public bool HasLessCounts
        {
            get { return this._measureCounter == null ? false : _measureCounter.CountsLeft <= 100 && _measureCounter.CountsLeft > 0; }
        }

        public bool HasNoCounts
        {
            get { return this._measureCounter == null ? false : _measureCounter.CountsLeft <= 0; }
        }

        public bool CanMeasure
        {
            get
            {
                return !HasNoCounts && !_serviceController.IsWeeklyCleanMandatory;
            }
        }

        public bool IsCasyConnected
        {
            get { return _isCasyConnected; }
            set
            {
                if(value != _isCasyConnected)
                {
                    this._isCasyConnected = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        private void OnMeasure()
        {
            Task.Factory.StartNew(async () =>
            {
                var result = await this._measureResultManager.SaveChangedMeasureResults();
                if (result != ButtonResult.Cancel)
                {
                    var args = new NavigationArgs(NavigationCategory.Measurement);
                    this._eventAggregatorProvider.Instance.GetEvent<NavigateToEvent>().Publish(args);
                }
            });
        }

        private void OnPurge(object obj)
        {
            Task.Factory.StartNew(() => _measureController.Clean(int.Parse(obj.ToString())));
        }

        private void OnBackground()
        {
            this._serviceController.StartMeasureBackgroundWizard();
        }

        private void OnUndo()
        {
            if (_uiProject.CanUndo)
            {
                _uiProject.Undo();
            }
        }

        private void OnRedo()
        {
            if (_uiProject.CanRedo)
            {
                _uiProject.Redo();
            }
        }

        private static BitmapImage LoadImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return null;
            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }

        public ICommand ShowNotificationsCommand
        {
            get { return new OmniDelegateCommand(OnShowNotifications); }
        }

        public void OnImportsSatisfied()
        {
            _authenticationService.UserLoggedIn += OnUserLoggedIn;

            var orderedCommands = _userMenuCommandsInternal.Where(command => command.IsVisible).OrderBy(command => command.Order);
            foreach (var command in orderedCommands)
            {
                this.UserMenuCommands.Add(command);
            }

            this._uiProject.UndoRedoStackChanged += OnUndoRedoStackChanged;
            this._notificationService.NotificationsChanged += OnNotificationsChanged;

            if(this._measureCounter != null)
            {
                this._measureCounter.AvailableCountsChanged += OnCountsChanged;
            }

            this._eventAggregatorProvider.Instance.GetEvent<KeyDownEvent>().Subscribe(OnKeyDown);
            this._eventAggregatorProvider.Instance.GetEvent<RemoteCommandEvent>().Subscribe(OnRemoteCommand);
            _eventAggregatorProvider.Instance.GetEvent<NavigateToEvent>().Subscribe(OnNavigateToEvent);
            this._environmentService.EnvironmentInfoChangedEvent += OnEnvironmentInfoChanged;

            OnUndoRedoStackChanged(null, null);
            UpdateNotifications();

            _localizationService.LanguageChanged += OnLanguageChanged;
            
            var isConnected = this._environmentService.GetEnvironmentInfo("IsCasyConnected");
            IsCasyConnected = isConnected != null && (bool) isConnected;

            if (_serviceController != null)
            {
                _serviceController.WeeklyCleanMandatoryChangedEvent += (s, e) => NotifyOfPropertyChange("CanMeasure");
            }
        }

        private void OnNavigateToEvent(object argument)
        {
            var navigationArgs = (NavigationArgs)argument;
            IsPrintAllEnabled = navigationArgs.NavigationCategory != NavigationCategory.AnalyseMean &&
                                navigationArgs.NavigationCategory != NavigationCategory.AnalyseOverlay &&
                                navigationArgs.NavigationCategory != NavigationCategory.Dashboard;
        }

        private void OnLanguageChanged(object sender, LocalizationEventArgs e)
        {
            NotifyOfPropertyChange("UserMenuButtonText");
            NotifyOfPropertyChange("UserMenuButtonTextUserNameRole");
        }

        private void OnShowNotifications()
        {
            this.UpdateNotifications(true);
        }

        private void OnEnvironmentInfoChanged(object sender, string e)
        {
            if (e == "IsCasyConnected")
            {
                this.IsCasyConnected = (bool)this._environmentService.GetEnvironmentInfo("IsCasyConnected");
            }
            else if (e == "IsBusy")
            {
                this.IsLoading = (bool)_environmentService.GetEnvironmentInfo("IsBusy");
            }
        }

        private void OnCountsChanged(object sender, EventArgs e)
        {
            NotifyOfPropertyChange("CountsLeft");
            NotifyOfPropertyChange("HasLessCounts");
            NotifyOfPropertyChange("HasNoCounts");
        }

        private void OnNotificationsChanged(object sender, EventArgs e)
        {
            if (this._isVisible)
            {
                this.UpdateNotifications();
            }
        }

        private void OnUndoRedoStackChanged(object sender, EventArgs e)
        {
            this.IsUndoEnabled = _uiProject.CanUndo;
            this.IsRedoEnabled = _uiProject.CanRedo;
        }

        private void OnUserLoggedIn(object sender, AuthenticationEventArgs e)
        {
            NotifyOfPropertyChange("UserMenuButtonText");
            NotifyOfPropertyChange("UserMenuButtonTextUserNameRole");
            NotifyOfPropertyChange("LoggedInUserImage");
        }

        private void OnKeyDown(object obj)
        {
            var keyEventArgs = obj as KeyEventArgs;
            if (keyEventArgs.Key == Key.F6)
            {
                this.PurgeCommand.Execute(1);
            }
        }

        private void OnRemoteCommand(RemoteCommand remoteCommand)
        {
            if (remoteCommand.Command == "Clean")
            {
                this.PurgeCommand.Execute((int) remoteCommand.Parameter1);
            }

        }


    private void UpdateNotifications(bool forceShowNotifications = false)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var toRemoves = this.Notifications.ToArray();
                foreach(var toRemove in toRemoves)
                {
                    this.Notifications.Remove(toRemove);
                    toRemove.Dispose();
                }
                
                this.Notifications.Clear();

                foreach (var notification in _notificationService.Notifications)
                {
                    this.Notifications.Add(new NotificationViewModel(this._notificationService, notification));
                }
                NotifyOfPropertyChange("Notifications");
                NotifyOfPropertyChange("HasUnreadNotifications");

                var hasNotifications = Notifications.Count != 0;
                if(!hasNotifications)
                {
                    Notifications.Add(new NotificationViewModel(this._notificationService, new Casy.Core.NotificationObject(Casy.Core.NotificationType.NoNotifications, _localizationService)
                    {
                        IsUnread = false,
                        Title = "Notification_NoNotifications_Title",
                        Message = "Notification_NoNotifications_Message"
                    }));
                }

                if (hasNotifications || forceShowNotifications)
                {
                    IsNotificationsOpen = true;
                }
            });
        }
    }
}
