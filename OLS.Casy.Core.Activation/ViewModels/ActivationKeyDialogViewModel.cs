using OLS.Casy.Ui.Base;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OLS.Casy.Core.Activation.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(ActivationKeyDialogViewModel))]
    public class ActivationKeyDialogViewModel : DialogModelBase
    {
        private bool _isInternetConnected;
        private bool _everyOneHasWriteAccess;
        private bool _isCasyConnected;
        private string _activationKey;
        private string _serialNumber;
        private bool _canSubmit = false;

        [ImportingConstructor]
        public ActivationKeyDialogViewModel()
        {
        }

        public bool CanSubmit
        {
            get
            {
                return _canSubmit;
            }
        }

        public bool IsInternetConnected
        {
            get { return _isInternetConnected; }
            set
            {
                if (value != _isInternetConnected)
                {
                    this._isInternetConnected = value;
                    NotifyOfPropertyChange();

                    CheckCanSubmit();
                }
            }
        }

        public bool EveryOneHasWriteAccess
        {
            get { return _everyOneHasWriteAccess; }
            set
            {
                if (value != _everyOneHasWriteAccess)
                {
                    this._everyOneHasWriteAccess = value;
                    NotifyOfPropertyChange();

                    CheckCanSubmit();
                }
            }
        }

        public bool IsCasyConnected
        {
            get { return _isCasyConnected; }
            set
            {
                if (value != _isCasyConnected)
                {
                    this._isCasyConnected = value;
                    NotifyOfPropertyChange();

                    CheckCanSubmit();
                }
            }
        }

        public string ActivationKey
        {
            get { return _activationKey; }
            set
            {
                if(value != this._activationKey)
                {
                    this._activationKey = value;
                    NotifyOfPropertyChange();

                    CheckCanSubmit();
                }
            }
        }

        public string SerialNumber
        {
            get { return _serialNumber; }
            set
            {
                if(value != _serialNumber)
                {
                    this._serialNumber = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        private void CheckCanSubmit()
        {
            Task.Factory.StartNew(() =>
            {
                this._canSubmit = /*this._isInternetConnected && */ this._everyOneHasWriteAccess && (!this._isCasyConnected || !string.IsNullOrEmpty(this._serialNumber)) && !string.IsNullOrEmpty(this._activationKey);
                NotifyOfPropertyChange("CanSubmit");
            });
        }
    }
}
