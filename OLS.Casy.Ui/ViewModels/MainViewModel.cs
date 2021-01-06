using MahApps.Metro.Controls.Dialogs;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Ui.Api;
using OLS.Casy.Ui.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using OLS.Casy.Ui.Authorization.Api;
using System.Threading;
using OLS.Casy.Models;
using OLS.Casy.Ui.Base.Views;
using OLS.Casy.Ui.Base.ViewModels;
using System.Threading.Tasks;
using System.ComponentModel.Composition.Primitives;
using DevExpress.Mvvm;
using System.Linq;
using OLS.Casy.Controller.Api;
using System.Windows.Data;

namespace OLS.Casy.Ui.ViewModels
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IMainViewModel))]
    public class MainViewModel : Base.ViewModelBase, IMainViewModel, IPartImportsSatisfiedNotification
    {
        private readonly IEnumerable<IMainContainerViewModel> _mainContainerViewModels;
        private readonly ILoginViewModel _loginViewModel;
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly ILocalizationService _localizationService;
        private readonly IAuthenticationService _authenticationService;
        private readonly ICompositionFactory _compositionFactory;
        private readonly IEnvironmentService _environmentService;
        private readonly ITabTipAutomation _tabTipAutomation;
        private readonly ICasyController _casyController;

        private bool _isMainContainerVisible;
        private bool _isLoginContainerVisible;
        private bool _isInitializationVisible = true;

        private static readonly SemaphoreSlim _slowStuffSemaphore = new SemaphoreSlim(1, 1);

        private readonly List<CasyProgressDialogViewModel> _currentProgressViewModelStack;
        private DialogModelBase _currentDataContext;
        private PdfPreviewViewModel _showPrintPreview;
        private string _currentProgressText = "Preparing device ...";

        //private IDraggable _draggable;
        //private IDroppable _currentDropable;
        //private IDroppable _lastDropable;
        //private DispatcherTimer _touchWaitTimer;
        //private FrameworkElement _grid;
        //private Point? _point;
        //private DependencyObject _currentDepedencyObject;
        private readonly IDragAndDropService _dragAndDropService;
        //private readonly ContextMenuViewModel _contextMenuViewModel;
        //private Point _clickLocation = new Point(-1, -1);
        //private readonly Services.ContextMenuService _contextMenuService;
        //private volatile ProgressDialogController _progressDialogController;
        private readonly ObservableCollection<IDraggable> _dragItems;
        //private Point? _lastPoint;
        //private volatile bool _isPopUpOpen = false;

        private bool _isBusy;

        [ImportingConstructor]
        public MainViewModel(
            [ImportMany] IEnumerable<IMainContainerViewModel> mainContainerViewModels,
            [Import(AllowDefault = true)] ILoginViewModel loginViewModel,
            IEventAggregatorProvider eventAggregatorProvider,
            ILocalizationService localizationService,
            IAuthenticationService authenticationService,
            ICompositionFactory compositionFactory,
            IEnvironmentService environmentService,
            IDragAndDropService dragAndDropService,
            ICasyController casyController
            //ContextMenuViewModel contextMenuViewModel,
            //IContextMenuService contextMenuService,
            //DropActionViewModel dropActionViewModel,
            )
        {
            _mainContainerViewModels = mainContainerViewModels;
            _loginViewModel = loginViewModel;
            _eventAggregatorProvider = eventAggregatorProvider;
            _localizationService = localizationService;
            _authenticationService = authenticationService;
            _compositionFactory = compositionFactory;
            _environmentService = environmentService;
            _dragAndDropService = dragAndDropService;
            _casyController = casyController;

            _dragItems = new ObservableCollection<IDraggable>();
            _currentProgressViewModelStack = new List<CasyProgressDialogViewModel>();
            //this._contextMenuViewModel = contextMenuViewModel;
            //this._contextMenuService = contextMenuService as OLS.Casy.Ui.Services.ContextMenuService;
            //this._dropActionViewModel = dropActionViewModel;
        }

        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                //if(value != _isBusy)
                //{
                    this._isBusy = value;
                    NotifyOfPropertyChange();
                //}
            }
        }

        public ObservableCollection<IMainContainerViewModel> MainContainerViewModels
        {
            get { return new ObservableCollection<IMainContainerViewModel>(this._mainContainerViewModels); }
        }

        public bool IsMainContainerVisible
        {
            get { return _isMainContainerVisible; }
            set
            {
                if (value != _isMainContainerVisible)
                {
                    _isMainContainerVisible = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool IsLoginContainerVisible
        {
            get { return _isLoginContainerVisible; }
            set
            {
                if (value != _isLoginContainerVisible)
                {
                    this._isLoginContainerVisible = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool IsInitializationVisible
        {
            get { return _isInitializationVisible; }
            set
            {
                if (value != _isInitializationVisible)
                {
                    this._isInitializationVisible = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string ProductName
        {
            get { return "CASY"; }
        }

        public string CurrentProgressText
        {
            get => _currentProgressText;
            set
            {
                if (value == _currentProgressText) return;
                _currentProgressText = value;
                NotifyOfPropertyChange();
            }
        }

        public ILoginViewModel LoginViewModel
        {
            get { return this._loginViewModel; }
        }

        public OmniDelegateCommand<EventInformation<KeyEventArgs>> KeyDownCommand
        {
            get { return new OmniDelegateCommand<EventInformation<KeyEventArgs>>(this.OnKeyDown); }
        }

        public OmniDelegateCommand<EventInformation<KeyEventArgs>> KeyUpCommand
        {
            get { return new OmniDelegateCommand<EventInformation<KeyEventArgs>>(this.OnKeyUp); }
        }

        public PdfPreviewViewModel ShowPrintPreview
        {
            get { return _showPrintPreview; }
            set
            {
                if(value != this._showPrintPreview)
                {
                    this._showPrintPreview = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public void OnImportsSatisfied()
        {
            _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>().Subscribe(this.OnShowMessageBox);
            _eventAggregatorProvider.Instance.GetEvent<ShowMultiButtonMessageBoxEvent>().Subscribe(this.ShowMultiButtonMessageBox);
            _eventAggregatorProvider.Instance.GetEvent<ErrorResultEvent>().Subscribe(this.ShowErrorMessageBox);
            _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Subscribe(this.ShowProgress);
            _eventAggregatorProvider.Instance.GetEvent<ShowInputEvent>().Subscribe(this.ShowInput);
            _eventAggregatorProvider.Instance.GetEvent<ShowCustomDialogEvent>().Subscribe(this.ShowCustomDialog);
            _eventAggregatorProvider.Instance.GetEvent<ShowReportEvent>().Subscribe(this.OnShowReport);
            _eventAggregatorProvider.Instance.GetEvent<ShowLoginScreenEvent>().Subscribe(OnShowLoginScren);
            _authenticationService.UserLoggedOut += OnUserLoggedOut;
            _authenticationService.UserLoggedIn += OnUserLoggedIn;
            _environmentService.EnvironmentInfoChangedEvent += OnEnvironmentInfoChanged;

            var isConnected = _casyController.IsConnected; //_environmentService.GetEnvironmentInfo("IsCasyConnected");
            //_environmentService.SetEnvironmentInfo("IsBusy", false);
            if (isConnected || _casyController.ForceCheckIsConnected())
            {
                Task.Run(() => _casyController.StartSelfTest(_loginViewModel != null));
                IsInitializationVisible = true;
                //IsLoginContainerVisible = true;
            }
            else
            {
                IsLoginContainerVisible = true;
            }

            if (_loginViewModel == null)
            {
                /*
                this.IsMainContainerVisible = this._authenticationService.LoggedInUser 

                if (!this.IsMainContainerVisible)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var awaiter = new ManualResetEvent(false);
                        ShowMessageBoxDialogWrapper messageBoxWrapper = new ShowMessageBoxDialogWrapper()
                        {
                            Awaiter = awaiter,
                            Title = "Not authorized",
                            Message = "Sorry! You are not authorized using this application. Please contact your administrator."
                        };

                        _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>().Publish(messageBoxWrapper);
                    });
                }
                */
            }

            //Application.Current.Dispatcher.BeginInvoke((Action)delegate ()
            //{
            //    this._touchWaitTimer = new DispatcherTimer(new TimeSpan(0, 0, 0, 0, 200),
            //                                                DispatcherPriority.Background,
            //                                                this.touchWaitTimer_Tick,
            //                                                Dispatcher.CurrentDispatcher);
            //}, DispatcherPriority.ApplicationIdle);

            _dragAndDropService.ActiveDragableChanged += OnActiveDragableChanged;
        }

        private void OnEnvironmentInfoChanged(object sender, string changedKey)
        {
            if(changedKey == "IsBusy")
            {
                IsBusy = (bool)_environmentService.GetEnvironmentInfo("IsBusy");
            }
        }

        private void OnShowLoginScren()
        {
            if (_loginViewModel == null)
            {
                IsMainContainerVisible = _authenticationService.LoggedInUser != null;
                IsLoginContainerVisible = false;

                /*if (!this.IsMainContainerVisible)
                {
                    Task.Factory.StartNew(() =>
                    {
                        var awaiter = new ManualResetEvent(false);
                        ShowMessageBoxDialogWrapper messageBoxWrapper = new ShowMessageBoxDialogWrapper()
                        {
                            Awaiter = awaiter,
                            Title = "Not authorized",
                            Message = "Sorry! You are not authorized using this application. Please contact your administrator."
                        };

                        _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>().Publish(messageBoxWrapper);
                    });
                }*/
                //else
                if(IsMainContainerVisible)
                {
                    IsInitializationVisible = false;
                }
            }
            else
            {
                if (_isMainContainerVisible) return;
                IsLoginContainerVisible = true;
                IsInitializationVisible = false;
            }
        }

        private void OnShowReport(object obj)
        {
            ShowPrintPreview = obj as PdfPreviewViewModel;
        }

        private void OnShowMessageBox(ShowMessageBoxDialogWrapper showMessageBoxDialogWrapper)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)async delegate ()
            {
                var dialogExport = this._compositionFactory.GetExport<CasyMessageDialog>();
                var dialog = dialogExport.Value;

                var dataContextExport = this._compositionFactory.GetExport<CasyMessageDialogViewModel>();
                var dataContext = dataContextExport.Value;

                if (!string.IsNullOrEmpty(showMessageBoxDialogWrapper.Title))
                {
                    dialog.Title = _localizationService.GetLocalizedString(showMessageBoxDialogWrapper.Title);
                }

                dataContext.CloseHandler = async instance =>
                {
                    try
                    {
                        await DialogCoordinator.Instance.HideMetroDialogAsync(this, dialog);
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        showMessageBoxDialogWrapper.Result = dataContext.DialogResult == ButtonResult.Ok;
                        showMessageBoxDialogWrapper.Awaiter.Set();

                        this._compositionFactory.ReleaseExport(dialogExport);
                        this._compositionFactory.ReleaseExport(dataContextExport);
                    }
                };
                dataContext.Message = _localizationService.GetLocalizedString(showMessageBoxDialogWrapper.Message, showMessageBoxDialogWrapper.MessageParameter);
                dataContext.IsFirstButtonVisibile = false;
                if(!showMessageBoxDialogWrapper.HideCancelButton)
                {
                    dataContext.IsSecondButtonVisibile = true;
                    dataContext.SecondButtonResult = ButtonResult.Cancel;
                }

                dialog.DataContext = dataContext;

                await DialogCoordinator.Instance.ShowMetroDialogAsync(this, dialog);
            }, DispatcherPriority.Normal);
        }

        private void ShowProgress(ShowProgressDialogWrapper showProgressDialogWrapper)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)async delegate ()
            {
                await _slowStuffSemaphore.WaitAsync();

                CasyProgressDialogViewModel progressViewModel =  this._currentProgressViewModelStack.FirstOrDefault(vm => vm.Wrapper == showProgressDialogWrapper);

                if (progressViewModel == null)
                {
                    if (!showProgressDialogWrapper.IsFinished)
                    {
                        var dialogExport = this._compositionFactory.GetExport<CasyProgressDialog>();
                        var dialog = dialogExport.Value;

                        var dataContextExport = this._compositionFactory.GetExport<CasyProgressDialogViewModel>();
                        progressViewModel = dataContextExport.Value;
                        progressViewModel.Wrapper = showProgressDialogWrapper;
                        this._currentProgressViewModelStack.Add(progressViewModel);

                        progressViewModel.ShowCloseButton = false;
                        progressViewModel.IsCancelButtonAvailable = showProgressDialogWrapper.IsCancelButtonAvailable;
                        progressViewModel.CancelAction = () =>
                        {
                            if (showProgressDialogWrapper.CancelAction != null)
                            {
                                this._currentProgressViewModelStack.Remove(progressViewModel);
                                showProgressDialogWrapper.IsFinished = true;
                                showProgressDialogWrapper.CancelAction.Invoke(true);
                            }
                        };

                        progressViewModel.CloseHandler = async instance =>
                        {
                            try
                            {
                                await DialogCoordinator.Instance.HideMetroDialogAsync(this, dialog);
                            }
                            catch (Exception)
                            {
                            }
                            finally
                            {
                                this._compositionFactory.ReleaseExport(dialogExport);
                                this._compositionFactory.ReleaseExport(dataContextExport);
                                //_slowStuffSemaphore.Release();
                            }
                        };

                        var localizedTitle = _localizationService.GetLocalizedString(showProgressDialogWrapper.Title);
                        var localizedMessage = _localizationService.GetLocalizedString(showProgressDialogWrapper.Message, showProgressDialogWrapper.MessageParameter);

                        progressViewModel.Title = localizedTitle;
                        progressViewModel.Message = localizedMessage;

                        dialog.DataContext = progressViewModel;

                        await DialogCoordinator.Instance.ShowMetroDialogAsync(this, dialog);
                    }
                }
                else
                {
                    var localizedTitle = _localizationService.GetLocalizedString(showProgressDialogWrapper.Title);
                    var localizedMessage = _localizationService.GetLocalizedString(showProgressDialogWrapper.Message, showProgressDialogWrapper.MessageParameter);

                    progressViewModel.Title = localizedTitle;
                    progressViewModel.Message = localizedMessage;

                    if (showProgressDialogWrapper.IsFinished)
                    {
                        this._currentProgressViewModelStack.Remove(progressViewModel);
                        progressViewModel.CloseHandler.Invoke(null);
                        //progressViewModel.CancelCommand.Execute(false);
                        //_slowStuffSemaphore.Release();
                    }
                }

                _slowStuffSemaphore.Release();
            }, DispatcherPriority.Send);
        }

        private void ShowErrorMessageBox(ErrorResult errorResult)
        {
            if (errorResult.FatalErrorDetails.Count > 0)
            {
                Application.Current.Dispatcher.BeginInvoke((Action)async delegate ()
                {
                    var dialogExport = this._compositionFactory.GetExport<CasyErrorMessageDialog>();
                    var dialog = dialogExport.Value;

                    var dataContextExport = this._compositionFactory.GetExport<CasyErrorMessageDialogViewModel>();
                    var dataContext = dataContextExport.Value;

                    dialog.Title = _localizationService.GetLocalizedString("ErrorMessageDialogView_Title", errorResult.FatalErrorDetails.Count.ToString());

                    dataContext.CloseHandler = async instance =>
                    {
                        try
                        {
                            await DialogCoordinator.Instance.HideMetroDialogAsync(this, dialog);
                        }
                        catch(Exception)
                        {
                        }
                        finally
                        {
                            this._compositionFactory.ReleaseExport(dialogExport);
                            this._compositionFactory.ReleaseExport(dataContextExport);
                        }
                    };
                    dataContext.SetErrorDetails(errorResult.FatalErrorDetails);

                    dialog.DataContext = dataContext;

                    await DialogCoordinator.Instance.ShowMetroDialogAsync(this, dialog);
                }, DispatcherPriority.Normal);
            }
        }

        private void ShowInput(ShowInputDialogWrapper showInputDialogWrapper)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)async delegate ()
            {
                var dialogExport = this._compositionFactory.GetExport<CasyInputDialog>();
                var dialog = dialogExport.Value;

                var dataContextExport = this._compositionFactory.GetExport<CasyInputDialogViewModel>();
                var dataContext = dataContextExport.Value;

                if (!string.IsNullOrEmpty(showInputDialogWrapper.Title))
                {
                    dialog.Title = _localizationService.GetLocalizedString(showInputDialogWrapper.Title);
                }

                dataContext.CloseHandler = async instance =>
                {
                    try
                    {
                        await DialogCoordinator.Instance.HideMetroDialogAsync(this, dialog);
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        showInputDialogWrapper.Result = dataContext.Input;
                        showInputDialogWrapper.IsCancel = dataContext.IsCancel;
                        showInputDialogWrapper.Awaiter.Set();

                        this._compositionFactory.ReleaseExport(dialogExport);
                        this._compositionFactory.ReleaseExport(dataContextExport);
                    }
                };
                dataContext.Message = _localizationService.GetLocalizedString(showInputDialogWrapper.Message, showInputDialogWrapper.MessageParameter);
                dataContext.IsSecondButtonVisibile = true;
                dataContext.IsFirstButtonVisibile = false;
                dataContext.SecondButtonResult = ButtonResult.Cancel;
                dataContext.Input = showInputDialogWrapper.DefaultText;
                dataContext.InputWatermark = showInputDialogWrapper.Watermark;
                dataContext.CanOkDelegate = showInputDialogWrapper.CanOkDelegate;

                dialog.DataContext = dataContext;

                await DialogCoordinator.Instance.ShowMetroDialogAsync(this, dialog);
            }, DispatcherPriority.Normal);
        }

        private void ShowCustomDialog(ShowCustomDialogWrapper showCustomDialogWrapper)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)async delegate ()
            {
                //await _slowStuffSemaphore.WaitAsync();

                //if (this._currentDataContext == null || showCustomDialogWrapper.DataContext.GetType() != this._currentDataContext.GetType())
                //{
                var dialog = this._compositionFactory.GetExportedValue(showCustomDialogWrapper.DialogType) as BaseMetroDialog;

                this._currentDataContext = showCustomDialogWrapper.DataContext as DialogModelBase;

                if (!string.IsNullOrEmpty(showCustomDialogWrapper.Title))
                {
                    dialog.Title = _localizationService.GetLocalizedString(showCustomDialogWrapper.Title);
                }
                else if (showCustomDialogWrapper.TitleBinding != null)
                {
                    dialog.SetBinding(BaseMetroDialog.TitleProperty, (Binding) showCustomDialogWrapper.TitleBinding);
                }

                this._currentDataContext.CloseHandler = async instance =>
                {
                    try
                    {
                        await DialogCoordinator.Instance.HideMetroDialogAsync(this, dialog);
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        showCustomDialogWrapper.Awaiter.Set();
                    }
                };

                dialog.DataContext = showCustomDialogWrapper.DataContext;
                await DialogCoordinator.Instance.ShowMetroDialogAsync(this, dialog);
                //}
                //else
                //{
                    //showCustomDialogWrapper.Awaiter.Set();
                //}
                //_slowStuffSemaphore.Release();
            }, DispatcherPriority.Send);
        }

        private void ShowMultiButtonMessageBox(ShowMultiButtonMessageBoxDialogWrapper showMultiButtonMessageBoxDialogWrapper)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)async delegate ()
            {
                var dialogExport = this._compositionFactory.GetExport<CasyMessageDialog>();
                var dialog = dialogExport.Value;

                var dataContextExport = this._compositionFactory.GetExport<CasyMessageDialogViewModel>();
                var dataContext = dataContextExport.Value;

                if (!string.IsNullOrEmpty(showMultiButtonMessageBoxDialogWrapper.Title))
                {
                    dialog.Title = _localizationService.GetLocalizedString(showMultiButtonMessageBoxDialogWrapper.Title);
                }

                dataContext.CloseHandler = async instance =>
                {
                    showMultiButtonMessageBoxDialogWrapper.Result = dataContext.DialogResult;

                    try
                    {
                        await DialogCoordinator.Instance.HideMetroDialogAsync(this, dialog);
                    }
                    catch(Exception)
                    {
                    }
                    finally
                    { 
                    //instance --> dialog ViewModel
                        showMultiButtonMessageBoxDialogWrapper.Awaiter.Set();
                        this._compositionFactory.ReleaseExport(dialogExport);
                        this._compositionFactory.ReleaseExport(dataContextExport);
                    }
                };
                dataContext.Message = _localizationService.GetLocalizedString(showMultiButtonMessageBoxDialogWrapper.Message, showMultiButtonMessageBoxDialogWrapper.MessageParameter);

                if (showMultiButtonMessageBoxDialogWrapper.OkButtonUse != ButtonResult.None)
                {
                    string localizationKey;
                    if (string.IsNullOrEmpty(showMultiButtonMessageBoxDialogWrapper.OkButtonString))
                    {
                        localizationKey = string.Format("MessageBox_Button_{0}_Text", Enum.GetName(typeof(ButtonResult), showMultiButtonMessageBoxDialogWrapper.OkButtonUse));
                    }
                    else
                    {
                        localizationKey = showMultiButtonMessageBoxDialogWrapper.OkButtonString;
                    }

                    var buttonText = _localizationService.GetLocalizedString(localizationKey);
                    dataContext.OkButtonText = buttonText;
                    dataContext.OkButtonResult = showMultiButtonMessageBoxDialogWrapper.OkButtonUse;
                }

                if (showMultiButtonMessageBoxDialogWrapper.SecondButtonUse != ButtonResult.None)
                {
                    string localizationKey;
                    if (string.IsNullOrEmpty(showMultiButtonMessageBoxDialogWrapper.SecondButtonString))
                    {
                        localizationKey = string.Format("MessageBox_Button_{0}_Text", Enum.GetName(typeof(ButtonResult), showMultiButtonMessageBoxDialogWrapper.SecondButtonUse));
                    }
                    else
                    {
                        localizationKey = showMultiButtonMessageBoxDialogWrapper.SecondButtonString;
                    }

                    var buttonText = _localizationService.GetLocalizedString(localizationKey);
                    dataContext.SecondButtonText = buttonText;
                    dataContext.SecondButtonResult = showMultiButtonMessageBoxDialogWrapper.SecondButtonUse;
                    dataContext.IsSecondButtonVisibile = true;
                }

                if (showMultiButtonMessageBoxDialogWrapper.FirstButtonUse != ButtonResult.None)
                {
                    string localizationKey;
                    if (string.IsNullOrEmpty(showMultiButtonMessageBoxDialogWrapper.FirstButtonString))
                    {
                        localizationKey = string.Format("MessageBox_Button_{0}_Text", Enum.GetName(typeof(ButtonResult), showMultiButtonMessageBoxDialogWrapper.FirstButtonUse));
                    }
                    else
                    {
                        localizationKey = showMultiButtonMessageBoxDialogWrapper.FirstButtonString;
                    }

                    var buttonText = _localizationService.GetLocalizedString(localizationKey);
                    dataContext.FirstButtonText = buttonText;
                    dataContext.FirstButtonResult = showMultiButtonMessageBoxDialogWrapper.FirstButtonUse;
                    dataContext.IsFirstButtonVisibile = true;
                }

                dialog.DataContext = dataContext;

                await DialogCoordinator.Instance.ShowMetroDialogAsync(this, dialog);
            }, DispatcherPriority.Send);
        }

        private void OnUserLoggedOut(object sender, AuthenticationEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)async delegate ()
            {
                await HideVisibleDialogs();

                this.IsMainContainerVisible = false;
                this.IsLoginContainerVisible = true;
            }, DispatcherPriority.ApplicationIdle);        
        }

        public Task HideVisibleDialogs()
        {
            return Task.Run(async () =>
            {
                await Application.Current.Dispatcher.Invoke(async () =>
                {
                    BaseMetroDialog dialogBeingShow = await DialogCoordinator.Instance.GetCurrentDialogAsync<BaseMetroDialog>(this);

                    while (dialogBeingShow != null)
                    {
                        await DialogCoordinator.Instance.HideMetroDialogAsync(this, dialogBeingShow);
                        dialogBeingShow = await DialogCoordinator.Instance.GetCurrentDialogAsync<BaseMetroDialog>(this);
                    }
                });
            });
        }

        private void OnUserLoggedIn(object sender, AuthenticationEventArgs e)
        {
            if (this._loginViewModel == null)
            {
                //Application.Current.Dispatcher.BeginInvoke((Action)delegate ()
                //{
                this.IsMainContainerVisible = this._authenticationService.LoggedInUser != null;
                this.IsLoginContainerVisible = !this._isMainContainerVisible;

                if (!this.IsMainContainerVisible)
                {
                    Task.Factory.StartNew(() =>
                    { 
                        var awaiter = new ManualResetEvent(false);
                        ShowMessageBoxDialogWrapper messageBoxWrapper = new ShowMessageBoxDialogWrapper()
                        {
                            Awaiter = awaiter,
                            Title = "Not authorized",
                            Message = "Sorry! You are not authorized using this application. Please contact your administrator."
                        };

                        _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>().Publish(messageBoxWrapper);

                        if(awaiter.WaitOne())
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                Application.Current.Shutdown();
                            });
                        }
                    });
                }

                if (this.IsMainContainerVisible)
                {
                    this.IsInitializationVisible = false;
                }
                //});
            }
            else
            { 
                Application.Current.Dispatcher.BeginInvoke((Action)delegate ()
                {
                    this.IsLoginContainerVisible = false;
                    this.IsMainContainerVisible = true;
                });
            }
        }

        private void OnKeyDown(EventInformation<KeyEventArgs> information)
        {
            this._eventAggregatorProvider.Instance.GetEvent<KeyDownEvent>().Publish(information.EventArgs);
            information.EventArgs.Handled = false;
        }

        private void OnKeyUp(EventInformation<KeyEventArgs> information)
        {
            this._eventAggregatorProvider.Instance.GetEvent<KeyUpEvent>().Publish(information.EventArgs);
            information.EventArgs.Handled = false;
        }

        public ObservableCollection<IDraggable> DragItems
        {
            get { return _dragItems; }
        }

        private void OnActiveDragableChanged(object sender, EventArgs e)
        {
            this.DragItems.Clear();
            if(this._dragAndDropService.ActiveDraggable != null)
            {
                this.DragItems.Add(this._dragAndDropService.ActiveDraggable);
            }
        }
    }
}
