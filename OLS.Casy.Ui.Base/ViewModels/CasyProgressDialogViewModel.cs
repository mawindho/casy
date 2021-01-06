using OLS.Casy.Core.Events;
using System;
using System.ComponentModel.Composition;
using System.Windows.Input;

namespace OLS.Casy.Ui.Base.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(CasyProgressDialogViewModel))]
    public class CasyProgressDialogViewModel : DialogModelBase
    {
        private string _message;
        private bool _isCancelButtonAvailable;

        public string Message
        {
            get { return _message; }
            set
            {
                if (value != _message)
                {
                    this._message = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool IsCancelButtonAvailable
        {
            get { return _isCancelButtonAvailable; }
            set
            {
                if(value != _isCancelButtonAvailable)
                {
                    this._isCancelButtonAvailable = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public Action CancelAction { get; set; }

        public ShowProgressDialogWrapper Wrapper { get; set; }

        protected override void OnCancel()
        {
            if(CancelAction != null)
            {
                this.CancelAction.Invoke();
            }
            base.OnCancel();
        }
    }
}
