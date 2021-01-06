using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OLS.Casy.ActivationServer.Cobra.Models;
using OLS.Casy.Ui.Base;

namespace OLS.Casy.Activation.KeyGenerator
{
    public class CobraAddressViewModel : ViewModelBase
    {
        private string _company;
        private string _company2;
        private string _company3;
        private string _department;
        private string _firstname;
        private string _lastname;
        private string _position;
        private string _street;
        private string _postalCode;
        private string _city;
        private string _country;
        private string _email;

        public Guid Guid { get; set; }

        public Address AssociatedObject { get; set; }

        public string Company
        {
            get => _company;
            set
            {
                if (value == _company) return;

                _company = value;
                NotifyOfPropertyChange();
            }
        }

        public string Company2
        {
            get => _company2;
            set
            {
                if (value == _company2) return;

                _company2 = value;
                NotifyOfPropertyChange();
            }
        }

        public string Company3
        {
            get => _company3;
            set
            {
                if (value == _company3) return;

                _company3 = value;
                NotifyOfPropertyChange();
            }
        }

        public string Department
        {
            get => _department;
            set
            {
                if (value == _department) return;

                _department = value;
                NotifyOfPropertyChange();
            }
        }

        public string Firstname
        {
            get => _firstname;
            set
            {
                if (value == _firstname) return;

                _firstname = value;
                NotifyOfPropertyChange();
            }
        }

        public string Lastname
        {
            get => _lastname;
            set
            {
                if (value == _lastname) return;

                _lastname = value;
                NotifyOfPropertyChange();
            }
        }

        public string Position
        {
            get => _position;
            set
            {
                if (value == _position) return;

                _position = value;
                NotifyOfPropertyChange();
            }
        }

        public string Street
        {
            get => _street;
            set
            {
                if (value == _street) return;

                _street = value;
                NotifyOfPropertyChange();
            }
        }

        public string PostalCode
        {
            get => _postalCode;
            set
            {
                if (value == _postalCode) return;

                _postalCode = value;
                NotifyOfPropertyChange();
            }
        }

        public string City
        {
            get => _city;
            set
            {
                if (value == _city) return;

                _city = value;
                NotifyOfPropertyChange();
            }
        }

        public string Country
        {
            get => _country;
            set
            {
                if (value == _country) return;

                _country = value;
                NotifyOfPropertyChange();
            }
        }

        public string Email
        {
            get => _email;
            set
            {
                if (value == _email) return;

                _email = value;
                NotifyOfPropertyChange();
            }
        }
    }
}
