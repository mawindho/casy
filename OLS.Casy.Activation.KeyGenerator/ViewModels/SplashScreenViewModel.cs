using MahApps.Metro.Controls.Dialogs;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Base.ViewModels;
using OLS.Casy.Ui.Base.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace OLS.Casy.Activation.KeyGenerator.ViewModels
{
    public class SplashScreenViewModel : ViewModelBase
    {
        private string _currentProgressText;

        public string ProductName
        {
            get { return "CASY ACTIVATION GENERATOR"; }
        }

        /// <summary>
        /// Property for the current progress text
        /// </summary>
        public string CurrentProgressText
        {
            get { return _currentProgressText; }
            set
            {
                if (value != _currentProgressText)
                {
                    this._currentProgressText = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public ICompositionFactory CompositionFactory { get; set; }

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
                    dialog.Title = showMessageBoxDialogWrapper.Title;
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
                dataContext.Message = string.Format(showMessageBoxDialogWrapper.Message, showMessageBoxDialogWrapper.MessageParameter);
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
    }
}
