using DevExpress.Mvvm;
using OLS.Casy.Ui.Base;
using System;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Input;

namespace OLS.Casy.Controller.Service.PinGenerator.Ui
{
    public class MainViewModel : ValidationViewModelBase
    {
        private string _serialNumber = string.Empty;
        private DateTime _dataTime;

        public MainViewModel()
        {
            _dataTime = DateTime.Now;
        }

        public string SerialNumber
        {
            get { return _serialNumber; }
            set
            {
                _serialNumber = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange("ServicePIN");
            } 
        }

        public string ServicePIN
        {
            get
            {
                if (_serialNumber != null)
                {
                    return GenerateServicePIN();
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
                NotifyOfPropertyChange("ServicePIN");
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
                NotifyOfPropertyChange("ServicePIN");
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
                NotifyOfPropertyChange("ServicePIN");
            }
        }

        public ICommand CloseCommand
        {
            get
            {
                return new OmniDelegateCommand(() => Application.Current.Shutdown());
            }
        }

        private string GenerateServicePIN()
        {
            char[] serialNumber = _serialNumber.ToCharArray();
            byte[] result = new byte[2];

            if(serialNumber.Length < 2)
            {
                result[0] = 0;
                result[1] = 0;
            }
            else
            {
                result[0] = Convert.ToByte(serialNumber[serialNumber.Length - 2]);
                result[1] = Convert.ToByte(serialNumber[serialNumber.Length - 1]);
            }

            var yearString = _dataTime.Year.ToString();
            var year = int.Parse(yearString.Substring(2));

            result[0] ^= (byte)(_dataTime.Day ^ year);
            result[1] ^= (byte)((_dataTime.Day << 3) ^ _dataTime.Month);

            return BitConverter.ToUInt16(result, 0).ToString();
        }
    }
}
