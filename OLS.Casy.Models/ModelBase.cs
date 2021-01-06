using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OLS.Casy.Models
{
    [Serializable]
    public class ModelBase : INotifyPropertyChanged
    {
        [field:NonSerialized]
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
