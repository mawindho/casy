using OLS.Casy.Models;
using OLS.Casy.Ui.Base;
using System.Windows.Input;

namespace OLS.Casy.Ui.MainControls.ViewModels
{
    public class ShortcutViewModel : ViewModelBase
    {
        private bool _isCasyConnected;
        private bool _isRed;
        private bool _isOrange;
        private string _header2;

        public string Header { get; set; }
        public string Header2
        {
            get { return _header2; }
            set
            {
                if(value != _header2)
                {
                    this._header2 = value;
                    NotifyOfPropertyChange();
                }
            }
        }
        public string ImagePath { get; set; }
        public ICommand Command { get; set; }
        public object CommandParameter { get; set; }
        public int Order { get; set; }
        public UserRole MinRequiredRole { get; set; }
        public bool IsOrange
        {
            get { return _isOrange; }
            set
            {
                if(value != _isOrange)
                {
                    this._isOrange = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool IsRed
        {
            get { return _isRed; }
            set
            {
                if(value != _isRed)
                {
                    this._isRed = value;
                    NotifyOfPropertyChange();
                }
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
    }
}
