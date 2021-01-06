using MahApps.Metro.Controls.Dialogs;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Base.ViewModels;
using OLS.Casy.Ui.Base.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace OLS.Casy.AppService.ViewModels
{
    /// <summary>
    /// ViewModel for the spash screen
    /// </summary>
    public class SplashScreenViewModel : ViewModelBase
    {
        private string _currentProgressText;
        private static readonly SemaphoreSlim _slowStuffSemaphore = new SemaphoreSlim(1, 1);
        private readonly List<CasyProgressDialogViewModel> _currentProgressViewModelStack = new List<CasyProgressDialogViewModel>();

        public string ProductName
        {
            get { return "CASY"; }
        }

        /// <summary>
        /// Property for the current progress text
        /// </summary>
        public string CurrentProgressText
        {
            get { return _currentProgressText; }
            set
            {
                if(value != _currentProgressText)
                {
                    this._currentProgressText = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public ICompositionFactory CompositionFactory { get; set; }
        public ILocalizationService LocalizationService { get; set; }

        internal void ShowMessageBox(ShowMessageBoxDialogWrapper showMessageBoxDialogWrapper)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)async delegate ()
            {
                var dialogExport = this.CompositionFactory.GetExport<CasyMessageDialog>();
                var dialog = dialogExport.Value;

                var dataContextExport = this.CompositionFactory.GetExport<CasyMessageDialogViewModel>();
                var dataContext = dataContextExport.Value;

                if (!string.IsNullOrEmpty(showMessageBoxDialogWrapper.Title))
                {
                    dialog.Title = LocalizationService.GetLocalizedString(showMessageBoxDialogWrapper.Title);
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

                        this.CompositionFactory.ReleaseExport(dialogExport);
                        this.CompositionFactory.ReleaseExport(dataContextExport);
                    }
                };
                dataContext.Message = LocalizationService.GetLocalizedString(showMessageBoxDialogWrapper.Message, showMessageBoxDialogWrapper.MessageParameter);
                dataContext.IsFirstButtonVisibile = false;
                if (!showMessageBoxDialogWrapper.HideCancelButton)
                {
                    dataContext.IsSecondButtonVisibile = true;
                    dataContext.SecondButtonResult = ButtonResult.Cancel;
                }

                dialog.DataContext = dataContext;

                await DialogCoordinator.Instance.ShowMetroDialogAsync(this, dialog);
            }, DispatcherPriority.Normal);
        }

        internal void ShowCustomDialog(ShowCustomDialogWrapper showCustomDialogWrapper)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)async delegate ()
            {
                var dialog = this.CompositionFactory.GetExportedValue(showCustomDialogWrapper.DialogType) as BaseMetroDialog;

                ((DialogModelBase)showCustomDialogWrapper.DataContext).CloseHandler = async instance =>
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
            }, DispatcherPriority.Normal);
        }

        internal void ShowProgress(ShowProgressDialogWrapper showProgressDialogWrapper)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)async delegate ()
            {
                await _slowStuffSemaphore.WaitAsync();

                CasyProgressDialogViewModel progressViewModel = this._currentProgressViewModelStack.FirstOrDefault(vm => vm.Wrapper == showProgressDialogWrapper);

                if (progressViewModel == null)
                {
                    if (!showProgressDialogWrapper.IsFinished)
                    {
                        var dialogExport = this.CompositionFactory.GetExport<CasyProgressDialog>();
                        var dialog = dialogExport.Value;

                        var dataContextExport = this.CompositionFactory.GetExport<CasyProgressDialogViewModel>();
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
                                this.CompositionFactory.ReleaseExport(dialogExport);
                                this.CompositionFactory.ReleaseExport(dataContextExport);
                                //_slowStuffSemaphore.Release();
                            }
                        };

                        var localizedTitle = LocalizationService.GetLocalizedString(showProgressDialogWrapper.Title);
                        var localizedMessage = LocalizationService.GetLocalizedString(showProgressDialogWrapper.Message, showProgressDialogWrapper.MessageParameter);

                        progressViewModel.Title = localizedTitle;
                        progressViewModel.Message = localizedMessage;

                        dialog.DataContext = progressViewModel;

                        await DialogCoordinator.Instance.ShowMetroDialogAsync(this, dialog);
                    }
                }
                else
                {
                    var localizedTitle = LocalizationService.GetLocalizedString(showProgressDialogWrapper.Title);
                    var localizedMessage = LocalizationService.GetLocalizedString(showProgressDialogWrapper.Message, showProgressDialogWrapper.MessageParameter);

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
    }
}
