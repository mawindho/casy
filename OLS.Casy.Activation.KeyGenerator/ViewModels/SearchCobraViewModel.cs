using OLS.Casy.ActivationServer.Cobra;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DevExpress.Mvvm;
using MahApps.Metro.Controls;

namespace OLS.Casy.Activation.KeyGenerator.ViewModels
{
    public class SearchCobraViewModel
    {
        public SearchCobraViewModel()
        {
            CobraAddresses = new ObservableCollection<CobraAddressViewModel>();

            LoadAddresses();
        }

        public ObservableCollection<CobraAddressViewModel> CobraAddresses { get; }

        public CobraAddressViewModel SelectedCobraAddress { get; set; }

        public ICommand OkCommand
        {
            get { return new DelegateCommand<MetroWindow>(o => ((MetroWindow)o).Close()); }
        }

        private void LoadAddresses()
        {
            try
            {
                using (var context = new CobraContext())
                {
                    CobraAddresses.Clear();
                    //context.Database.Connection.Open();
                    //if (context.Database.Connection.State != System.Data.ConnectionState.Open) return;

                    foreach (var address in context.Addresses)
                    {
                        CobraAddresses.Add(new CobraAddressViewModel
                        {
                            AssociatedObject = address,
                            Company = address.Company1,
                            Guid = address.Guid,
                            Position = address.Position,
                            Street = address.Street,
                            Firstname = address.Firstname,
                            Email = address.Email,
                            Lastname = address.Lastname,
                            PostalCode = address.PostalCode,
                            City = address.City,
                            Country = address.Country,
                            Company2 = address.Company2,
                            Company3 = address.Company3,
                            Department = address.Department
                        });
                    }
                }
            }
            catch (Exception e)
            {
                // ignored
            }
        }
    }
}
