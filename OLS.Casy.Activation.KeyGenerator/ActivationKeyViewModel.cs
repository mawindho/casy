using OLS.Casy.Ui.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace OLS.Casy.Activation.KeyGenerator
{
    public class ActivationKeyViewModel : ViewModelBase
    {
        private string _value;
        private DateTime _validFrom;
        private DateTime _validTo;
        private int _maxNumActivations;
        private string _customerName;
        private string _serialNumber;
        private string _responsible;
        private bool _isAdAuth;
        private bool _isLocalAuth;
        private bool _isSimulator;
        private bool _isCfr;
        private bool _isTrial;
        private bool _isControl;
        private bool _isCounter;
        private bool _isTtSwitch;
        private bool _isAccess;

        public ActivationKeyViewModel()
        {
            ActivatedMachines = new ObservableCollection<ActivatedMachineViewModel>();
        }

        public ObservableCollection<ActivatedMachineViewModel> ActivatedMachines { get; }

        public int Id { get; set; }
        public string Value
        {
            get => _value;
            set
            {
                if (value == _value) return;

                _value = value;
                NotifyOfPropertyChange();
            }
        }
        public DateTime ValidFrom
        {
            get => _validFrom;
            set
            {
                if (value == _validFrom) return;

                _validFrom = value;
                NotifyOfPropertyChange();
            }
        }
        public DateTime ValidTo
        {
            get => _validTo;
            set
            {
                if (value != _validTo)
                {
                    this._validTo = value;
                    NotifyOfPropertyChange();
                }
            }
        }
        public int MaxNumActivations
        {
            get => _maxNumActivations;
            set
            {
                if (value == _maxNumActivations) return;

                _maxNumActivations = value;
                NotifyOfPropertyChange();
            }
        }
        public string CustomerName
        {
            get => _customerName;
            set
            {
                if (value == _customerName) return;

                _customerName = value;
                NotifyOfPropertyChange();
            }
        }
        public string SerialNumber
        {
            get => _serialNumber;
            set
            {
                if (value == _serialNumber) return;

                _serialNumber = value;
                NotifyOfPropertyChange();
            }
        }

        public string Responsible
        {
            get => _responsible;
            set
            {
                if (value == _responsible) return;

                _responsible = value;
                NotifyOfPropertyChange();
            }
        }
        public bool IsAdAuth
        {
            get => _isAdAuth;
            set
            {
                if (value == _isAdAuth) return;

                _isAdAuth = value;
                NotifyOfPropertyChange();
            }
        }
        public bool IsLocalAuth
        {
            get => _isLocalAuth;
            set
            {
                if (value == _isLocalAuth) return;

                _isLocalAuth = value;
                NotifyOfPropertyChange();
            }
        }
        public bool IsSimulator
        {
            get => _isSimulator;
            set
            {
                if (value == _isSimulator) return;

                _isSimulator = value;
                NotifyOfPropertyChange();
            }
        }
        public bool IsCfr
        {
            get => _isCfr;
            set
            {
                if (value == _isCfr) return;

                _isCfr = value;
                NotifyOfPropertyChange();
            }
        }
        public bool IsTrial
        {
            get => _isTrial;
            set
            {
                if (value == _isTrial) return;

                _isTrial = value;
                NotifyOfPropertyChange();
            }
        }
        public bool IsControl
        {
            get => _isControl;
            set
            {
                if (value == _isControl) return;

                _isControl = value;
                NotifyOfPropertyChange();
            }
        }
        public bool IsCounter
        {
            get => _isCounter;
            set
            {
                if (value == _isCounter) return;

                _isCounter = value;
                NotifyOfPropertyChange();
            }
        }
        public bool IsTtSwitch
        {
            get => _isTtSwitch;
            set
            {
                if (value == _isTtSwitch) return;

                _isTtSwitch = value;
                NotifyOfPropertyChange();
            }
        }
        public bool IsAccess
        {
            get => _isAccess;
            set
            {
                if (value == _isAccess) return;

                _isAccess = value;
                NotifyOfPropertyChange();
            }
        }
    }
}
