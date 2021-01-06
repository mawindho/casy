using System;
using System.Windows.Input;

namespace OLS.Casy.Ui.Base
{
    public abstract class DialogModelBase : ValidationViewModelBase
    {
        private string _title;
        private bool _showCloseButton = true;

        public Action<DialogModelBase> CloseHandler { get; set; }
        public bool IsCancel { get; set; }

        public ICommand OkCommand => new OmniDelegateCommand(OnOk);

        public virtual bool CanOk => true;

        protected virtual void OnOk()
        {
            CloseHandler?.Invoke(this);
        }

        public ICommand CancelCommand => new OmniDelegateCommand(OnCancel);

        protected virtual void OnCancel()
        {
            IsCancel = true;
            CloseHandler?.Invoke(this);
        }

        public virtual string Title
        {
            get => _title;
            set
            {
                if (value == _title) return;
                _title = value;
                NotifyOfPropertyChange();
            }
        }

        public bool ShowCloseButton
        {
            get => _showCloseButton;
            set
            {
                if (value == _showCloseButton) return;
                _showCloseButton = value;
                NotifyOfPropertyChange();
            }
        }
    }
}
