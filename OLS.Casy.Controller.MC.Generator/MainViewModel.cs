using DevExpress.Mvvm;
using OLS.Casy.Core.Authorization.Encryption;
using OLS.Casy.Ui.Base;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace OLS.Casy.Controller.MC.Generator
{
    public class MainViewModel : ValidationViewModelBase
    {
        private string _uniqueKey;
        private DateTime _validTo;
        private string _activationKey;
        private EncryptionProvider _encryptionProvider;
        private int _numCounts;
        private int _identifier;

        public MainViewModel()
        {
            _encryptionProvider = new EncryptionProvider();
            _validTo = DateTime.Now;
        }

        public string UniqueKey
        {
            get { return _uniqueKey; }
            set
            {
                _uniqueKey = value;
                NotifyOfPropertyChange();
            } 
        }

        public string ActivationKey
        {
            get { return _activationKey; }
            set
            {
                _activationKey = value;
                NotifyOfPropertyChange();
            }
        }

        public DateTime ValidTo
        {
            get { return _validTo; }
            set
            {
                if(value != _validTo)
                {
                    _validTo = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public int NumCounts
        {
            get { return _numCounts; }
            set
            {
                if(value!= _numCounts)
                {
                    _numCounts = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        [Range(0, 1000)]
        public int Identifier
        {
            get { return _identifier; }
            set
            {
                if(value != _identifier)
                {
                    _identifier = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public ICommand GenerateUniqueKeyCommand
        {
            get
            {
                return new DelegateCommand(() => this.UniqueKey = GenerateUniqueKey());
            }
        }

        public ICommand GenerateActivationKeyCommand
        {
            get
            {
                return new DelegateCommand(() => this.ActivationKey = GenerateActivationKey());
            }
        }

        public ICommand CloseCommand
        {
            get
            {
                return new DelegateCommand(() => Application.Current.Shutdown());
            }
        }

        private string GenerateUniqueKey()
        {
            string uniqueKey = string.Empty;

            for(int i = 0; i < 3; i++)
            { 
                Guid guid = Guid.NewGuid();
                string guidString = Convert.ToBase64String(guid.ToByteArray());
                guidString = guidString.Replace("=", "");
                guidString = guidString.Replace("+", "");
                uniqueKey += guidString;
            }

            return uniqueKey;
        }

        private string GenerateActivationKey()
        {
            var bytes = Encoding.UTF8.GetBytes(string.Format("{0}|||{1}|||{2}", this.ValidTo.ToUniversalTime().Ticks.ToString(), NumCounts.ToString(), this.Identifier.ToString()));

            var activationKey = Regex.Replace(Convert.ToBase64String(this._encryptionProvider.Encrypt(bytes, this._uniqueKey)), ".{4}", "$0-");

            return activationKey;
        }
    }
}
