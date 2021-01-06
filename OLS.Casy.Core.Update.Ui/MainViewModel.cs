using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OLS.Casy.Core.Update.Ui
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _progressText;
        private double _progressValue;

        public string ProgressText
        {
            get { return _progressText; }
            set
            {
                if(value != _progressText)
                {
                    this._progressText = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public double ProgressValue
        {
            get { return _progressValue; }
            set
            {
                if (value != _progressValue)
                {
                    this._progressValue = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyOfPropertyChange([CallerMemberName] string callerMemberName = "")
        {
            this.NotifyOfPropertyChangeInternal(callerMemberName);
        }

        private void NotifyOfPropertyChangeInternal(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
