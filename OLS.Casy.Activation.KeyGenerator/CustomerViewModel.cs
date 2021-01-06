using OLS.Casy.ActivationServer.Cobra.Models;
using OLS.Casy.Ui.Base;

namespace OLS.Casy.Activation.KeyGenerator
{
    public class CustomerViewModel : ViewModelBase
    {
        private string _name;
        private string _mail;
        private string _updateGuid;
        private string _cobraAddressGuid;

        public int Id { get; set; }
        public Address CobraAddress { get; set; }
        public string Name
        {
            get => _name;
            set
            {
                if (value == _name) return;

                _name = value;
                NotifyOfPropertyChange();
            }
        }

        public string Mail
        {
            get => _mail;
            set
            {
                if (value == _mail) return;

                _mail = value;
                NotifyOfPropertyChange();
            }
        }

        public string UpdateGuid
        {
            get => _updateGuid;
            set
            {
                if (value == _updateGuid) return;

                _updateGuid = value;
                NotifyOfPropertyChange();
            }
        }

        public string CobraAddressGuid
        {
            get => _cobraAddressGuid;
            set
            {
                if (value == _cobraAddressGuid) return;

                _cobraAddressGuid = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange("IsMapped");
            }
        }

        public bool IsMapped
        {
            get => !string.IsNullOrWhiteSpace(_cobraAddressGuid);
        }
    }
}
