using Xamarin.Forms;

namespace OLS.Casy.App.Models
{
    public class Group : BindableObject, ITreeItem
    {
        private bool _isSelected;

        public string Name { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }
}
