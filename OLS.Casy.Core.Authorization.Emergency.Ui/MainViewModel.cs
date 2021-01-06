using DevExpress.Mvvm;
using OLS.Casy.Authorization.Emergency;
using OLS.Casy.Ui.Base;
using System;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Input;

namespace OLS.Casy.Core.Authorization.Emergency.Ui
{
    public class MainViewModel : ValidationViewModelBase
    {
        private readonly SuperPasswordProvider _superPasswordProvider;

        private string _sessionId;
        private DateTime _dataTime;

        public MainViewModel()
        {
            _dataTime = DateTime.Now;
            _superPasswordProvider = new SuperPasswordProvider();
        }

        public string SessionId
        {
            get { return _sessionId; }
            set
            {
                _sessionId = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange("Password");
            } 
        }

        public string Password
        {
            get
            {
                if (!string.IsNullOrEmpty(_sessionId))
                {
                    return _superPasswordProvider.GenerateSuperPassword(_sessionId, _dataTime);
                }
                return string.Empty;
            }
        }

        [Range(1, 9999)]
        public int Year
        {
            get { return _dataTime.Year; }
            set
            {
                try
                {
                    _dataTime = new DateTime(value, this.Month, this.Day);
                    NotifyOfPropertyChange();
                }
                catch
                {

                }
                NotifyOfPropertyChange("Password");
            }
        }

        [Range(1, 12)]
        public int Month
        {
            get { return _dataTime.Month; }
            set
            {
                try
                {
                    _dataTime = new DateTime(this.Year, value, this.Day);
                    NotifyOfPropertyChange();
                }
                catch
                {

                }
                NotifyOfPropertyChange("Password");
            }
        }

        [Range(1, 31)]
        public int Day
        {
            get { return _dataTime.Day; }
            set
            {
                try
                { 
                    _dataTime = new DateTime(this.Year, this.Month, value);
                    NotifyOfPropertyChange();
                }
                catch
                {

                }
                NotifyOfPropertyChange("Password");
            }
        }

        public ICommand CloseCommand
        {
            get
            {
                return new OmniDelegateCommand(() => Application.Current.Shutdown());
            }
        }
    }
}
