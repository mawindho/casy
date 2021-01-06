using DevExpress.Mvvm;
using OLS.Casy.ActivationServer.Data;
using OLS.Casy.Ui.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Windows;
using System.Windows.Input;
using OLS.Casy.Activation.KeyGenerator.ViewModels;
using OLS.Casy.Activation.KeyGenerator.Views;
using OLS.Casy.ActivationServer.Cobra;
using Microsoft.EntityFrameworkCore;

namespace OLS.Casy.Activation.KeyGenerator
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(MainViewModel))]
    public class MainViewModel : ValidationViewModelBase
    {
        private readonly Random _rand = new Random();
        private ActivationKeyViewModel _selectedActivationKey;
        private CustomerViewModel _customerViewModel;

        private bool _isConnected;
        private string _currentVersion;

        [ImportingConstructor]
        public MainViewModel()
        {
            ActivationKeys = new ObservableCollection<ActivationKeyViewModel>();
            CustomerViewModels = new ObservableCollection<CustomerViewModel>();

            UpdateCustomer();
            OnReset();
        }

        private void UpdateActivationKeys()
        {
            try
            {
                using (var context = new ActivationServer.Data.ActivationContext())
                {
                    ActivationKeys.Clear();
                    //context.Database.Connection.Open();
                    //if (context.Database.Connection.State != System.Data.ConnectionState.Open) return;

                    var keys = context.ActivationKey.Include("Customer").Include("ProductType")
                        .Include("ActivationKeyProductAddOns").Include("ActivatedMachine").ToList();
                    foreach (var key in keys)
                    {
                        var viewModel = new ActivationKeyViewModel
                        {
                            Id = key.Id,
                            CustomerName = key.Customer.Name,
                            IsAdAuth = key.ActivationKeyProductAddOns.Any(x => x.ProductAddOnId == 1),
                            IsCfr = key.ActivationKeyProductAddOns.Any(x => x.ProductAddOnId == 7),
                            IsLocalAuth = key.ActivationKeyProductAddOns.Any(x => x.ProductAddOnId == 4),
                            IsSimulator = key.ActivationKeyProductAddOns.Any(x => x.ProductAddOnId == 5),
                            IsTrial = key.ActivationKeyProductAddOns.Any(x => x.ProductAddOnId == 9),
                            IsTtSwitch = key.ActivationKeyProductAddOns.Any(x => x.ProductAddOnId == 6),
                            IsAccess = key.ActivationKeyProductAddOns.Any(x => x.ProductAddOnId == 10),
                            MaxNumActivations = key.MaxNumActivations,
                            SerialNumber = key.SerialNumbers,
                            ValidFrom = key.ValidFrom,
                            ValidTo = key.ValidTo,
                            Value = key.Value,
                            IsControl = key.ActivationKeyProductAddOns.Any(x => x.ProductAddOnId == 2),
                            IsCounter = key.ActivationKeyProductAddOns.Any(x => x.ProductAddOnId == 3),
                            Responsible = key.Responsible
                        };

                        foreach (var activatedMachine in key.ActivatedMachine)
                        {
                            viewModel.ActivatedMachines.Add(new ActivatedMachineViewModel(viewModel)
                            {
                                MacAddress = activatedMachine.MacAdress,
                                ActivatedOn = activatedMachine.ActivatedOn,
                                ComputerName = activatedMachine.ComputerName,
                                CurrentVersion = activatedMachine.CurrentVersion,
                                LastUpdatedAt = activatedMachine.LastUpdatedAt,
                                SerialNumber = activatedMachine.SerialNumber,
                                Id = activatedMachine.Id
                            });
                        }

                        ActivationKeys.Add(viewModel);

                        IsConnected = true;
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void UpdateCustomer()
        {
            try
            {
                CustomerViewModels.Clear();
                using (var context = new ActivationServer.Data.ActivationContext())
                using(var cobraContext = new CobraContext())
                {
                    //context.Database.Connection.Open();
                    //cobraContext.Database.Connection.Open();
                    //if (context.Database.Connection.State == System.Data.ConnectionState.Open)
                    //{
                        KnownCustomer = new List<string>();
                        foreach (var customer in context.Customer)
                        {
                            KnownCustomer.Add(customer.Name);
                            var customerViewModel = new CustomerViewModel
                            {
                                Name = customer.Name,
                                Id = customer.Id,
                                Mail = customer.Mail,
                                UpdateGuid = customer.UpdateGuid,
                                CobraAddressGuid = customer.CobraAddressGuid
                            };

                            if (!string.IsNullOrWhiteSpace(customer.CobraAddressGuid))
                            {
                                var guid = Guid.Parse(customer.CobraAddressGuid);

                                customerViewModel.CobraAddress = cobraContext.Addresses.FirstOrDefault(x => x.Guid == guid);
                            }

                            CustomerViewModels.Add(customerViewModel);
                        }
                    //}
                }

                NotifyOfPropertyChange("KnownCustomer");
            }
            catch (Exception e)
            {
                // ignored
            }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (_isConnected == value) return;

                _isConnected = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsDesktop => _selectedActivationKey?.IsControl ?? false;

        public ICommand<object> DeleteRowCommand => new DelegateCommand<object>(OnDeleteRow);

        private void OnDeleteRow(object obj)
        {
            using (var context = new ActivationServer.Data.ActivationContext())
            {
                if (!(obj is ActivatedMachineViewModel activatedMachineViewModel)) return;

                var toDelete = context.ActivatedMachine.FirstOrDefault(x => x.Id == activatedMachineViewModel.Id);

                if (toDelete == null) return;

                context.ActivatedMachine.Remove(toDelete);
                context.SaveChanges();

                ActivationKeys.FirstOrDefault(x => x.Id == activatedMachineViewModel.Parent.Id)?.ActivatedMachines.Remove(activatedMachineViewModel);
            }
        }

        public ObservableCollection<ActivationKeyViewModel> ActivationKeys { get; }
        public ObservableCollection<CustomerViewModel> CustomerViewModels { get; }

        public ActivationKeyViewModel SelectedActivationKey
        {
            get => _selectedActivationKey;
            set
            {
                if (value == _selectedActivationKey) return;

                _selectedActivationKey = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange("ActivationKey");
                NotifyOfPropertyChange("ValidTo");
                NotifyOfPropertyChange("MaxNumActivations");
                NotifyOfPropertyChange("ValidSerialNumber");
                NotifyOfPropertyChange("CustomerName");
                NotifyOfPropertyChange("IsLocalAuth");
                NotifyOfPropertyChange("IsAdAuth");
                NotifyOfPropertyChange("IsControl");
                NotifyOfPropertyChange("IsSimulator");
                NotifyOfPropertyChange("IsTrial");
                NotifyOfPropertyChange("IsAccess");
                NotifyOfPropertyChange("IsCfr");
                NotifyOfPropertyChange("IsTtSwitch");
                NotifyOfPropertyChange("Responsible");
                NotifyOfPropertyChange("CanGenerateActivationKey");
                NotifyOfPropertyChange("ButtonText");
            }
        }

        public CustomerViewModel SelectedCustomer
        {
            get => _customerViewModel;
            set
            {
                _customerViewModel = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange("SelectedCustomerCobraMappingId");
                NotifyOfPropertyChange("SelectedCustomerCompany");
                NotifyOfPropertyChange("SelectedCustomerContact");
                NotifyOfPropertyChange("SelectedCustomerAddress");
                NotifyOfPropertyChange("SelectedCustomerMail");
                NotifyOfPropertyChange("CanGenerateActivationKey");
            }
        }

        public string CustomerName
        {
            get => _selectedActivationKey?.CustomerName;
            set
            {
                if (_selectedActivationKey != null)
                {
                    _selectedActivationKey.CustomerName = value;
                    NotifyOfPropertyChange();
                    NotifyOfPropertyChange("CanGenerateActivationKey");
                }
            }
        }

        public string ActivationKey
        {
            get => _selectedActivationKey?.Value;
            set
            {
                if (_selectedActivationKey != null)
                {
                    _selectedActivationKey.Value = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        [Required]
        public DateTime ValidTo
        {
            get => _selectedActivationKey == null ? DateTime.Now : _selectedActivationKey.ValidTo;
            set
            {
                if (_selectedActivationKey != null)
                {
                    if (value == _selectedActivationKey.ValidTo) return;

                    _selectedActivationKey.ValidTo = value;
                    NotifyOfPropertyChange();
                    NotifyOfPropertyChange("CanGenerateActivationKey");
                }
            }
        }

        [Range(1, 31)]
        public int MaxNumActivations
        {
            get => _selectedActivationKey == null ? 1 : _selectedActivationKey.MaxNumActivations;
            set
            {
                if (_selectedActivationKey != null)
                {
                    if (value == _selectedActivationKey.MaxNumActivations) return;

                    _selectedActivationKey.MaxNumActivations = value;
                    NotifyOfPropertyChange();
                    NotifyOfPropertyChange("CanGenerateActivationKey");
                }
            }
        }

        public string ValidSerialNumber
        {
            get => _selectedActivationKey?.SerialNumber;
            set
            {
                if (_selectedActivationKey != null)
                {
                    if (value == _selectedActivationKey.SerialNumber) return;

                    _selectedActivationKey.SerialNumber = value;
                    NotifyOfPropertyChange();
                    NotifyOfPropertyChange("CanGenerateActivationKey");
                }
            }
        }

        [Required]
        [RegularExpression(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}")]
        public string Responsible
        {
            get => _selectedActivationKey?.Responsible;
            set
            {
                if (_selectedActivationKey != null)
                {
                    if (value == _selectedActivationKey.Responsible) return;

                    _selectedActivationKey.Responsible = value;
                    NotifyOfPropertyChange();
                    NotifyOfPropertyChange("CanGenerateActivationKey");
                }
            }
        }

        public string SelectedCustomerCobraMappingId
        {
            get => _customerViewModel?.CobraAddressGuid;
        }

        public string SelectedCustomerCompany
        {
            get => $"{_customerViewModel?.CobraAddress?.Company1}";
        }

        public string SelectedCustomerContact
        {
            get => $"{_customerViewModel?.CobraAddress?.Salutation} {_customerViewModel?.CobraAddress?.Title} {_customerViewModel?.CobraAddress?.Firstname} {_customerViewModel?.CobraAddress?.Lastname}";
        }

        public string SelectedCustomerAddress
        {
            get => $"{_customerViewModel?.CobraAddress?.Street}\n{_customerViewModel?.CobraAddress?.PostalCode} {_customerViewModel?.CobraAddress?.City}\n{_customerViewModel?.CobraAddress?.Country}";
        }

        public string SelectedCustomerMail
        {
            get => $"{_customerViewModel?.CobraAddress?.Email}";
        }

        public List<string> KnownCustomer { get; private set; }

        public bool IsLocalAuth
        {
            get => _selectedActivationKey?.IsLocalAuth ?? false;
            set
            {
                if (value == _selectedActivationKey.IsLocalAuth) return;

                _selectedActivationKey.IsLocalAuth = value;
                NotifyOfPropertyChange();

                if(value)
                {
                    IsAdAuth = false;
                }
            }
        }

        public bool IsAdAuth
        {
            get => _selectedActivationKey?.IsAdAuth ?? false;
            set
            {
                if (value == _selectedActivationKey.IsAdAuth) return;

                _selectedActivationKey.IsAdAuth = value;
                NotifyOfPropertyChange();

                if (value)
                {
                    IsLocalAuth = false;
                }
            }
        }

        public bool IsControl
        {
            get => _selectedActivationKey?.IsControl ?? false;
            set
            {
                if (value == _selectedActivationKey.IsControl) return;

                _selectedActivationKey.IsControl = value;
                NotifyOfPropertyChange();

                if(!value)
                {
                    IsTtSwitch = false;
                }
                else
                {
                    IsSimulator = false;
                }
            }
        }

        public bool IsSimulator
        {
            get => _selectedActivationKey?.IsSimulator ?? false;
            set
            {
                if (value == _selectedActivationKey.IsSimulator) return;

                _selectedActivationKey.IsSimulator = value;
                NotifyOfPropertyChange();

                if(value)
                {
                    IsControl = false;
                }
            }
        }

        public bool IsCounter
        {
            get => _selectedActivationKey?.IsCounter ?? false;
            set
            {
                if (value == _selectedActivationKey.IsCounter) return;
                _selectedActivationKey.IsCounter = value;
                NotifyOfPropertyChange();

                if (value)
                {
                    IsControl = true;
                }
            }
        }

        public bool IsCfr
        {
            get => _selectedActivationKey?.IsCfr ?? false;
            set
            {
                if (value == _selectedActivationKey.IsCfr) return;
                _selectedActivationKey.IsCfr = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsTtSwitch
        {
            get => _selectedActivationKey?.IsTtSwitch ?? false;
            set
            {
                if (value == _selectedActivationKey.IsTtSwitch) return;
                _selectedActivationKey.IsTtSwitch = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsTrial
        {
            get => _selectedActivationKey?.IsTrial ?? false;
            set
            {
                if (value == _selectedActivationKey.IsTrial) return;
                _selectedActivationKey.IsTrial = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsAccess
        {
            get => _selectedActivationKey?.IsAccess ?? false;
            set
            {
                if (value == _selectedActivationKey.IsAccess) return;
                _selectedActivationKey.IsAccess = value;
                NotifyOfPropertyChange();

                if(value)
                {
                    IsLocalAuth = true;
                }
            }
        }

        public bool CanReset => true;

        public ICommand ResetCommand => new DelegateCommand(OnReset);

        private void OnReset()
        {
            UpdateActivationKeys();
            _selectedActivationKey = new ActivationKeyViewModel()
            {
                Id = 0,
                MaxNumActivations = 1,
                ValidTo = DateTime.UtcNow.AddYears(1)
            };
            NotifyOfPropertyChange("ButtonText");
        }

        public bool CanGenerateActivationKey
        {
            get
            {
                IList<ValidationResult> result = new List<ValidationResult>();
                return Validator.TryValidateObject(this, new ValidationContext(this, null, null), result);
            }
        }

        public ICommand GenerateOrUpdateActivationKeyCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    var id = GenerateOrUpdateActivationKey();
                    UpdateCustomer();
                    UpdateActivationKeys();

                    SelectedActivationKey = ActivationKeys.FirstOrDefault(ak => ak.Id == id);
                });
            }
        }

        public string ButtonText => SelectedActivationKey?.Id == 0 ? "Generieren" : "Änderungen speichern";

        public ICommand SaveCustomerCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    using (var context = new ActivationServer.Data.ActivationContext())
                    {
                        if (_customerViewModel == null) return;
                        var customerEntity = context.Customer.FirstOrDefault(x => x.Id == _customerViewModel.Id);
                        if (customerEntity != null)
                        {
                            customerEntity.Name = _customerViewModel.Name;
                            customerEntity.Mail = _customerViewModel.Mail;
                            customerEntity.CobraAddressGuid = _customerViewModel.CobraAddressGuid;
                        }

                        context.SaveChanges();
                    }

                    UpdateActivationKeys();
                });
            }
        }

        public ICommand CloseCommand
        {
            get
            {
                return new DelegateCommand(() => Application.Current.Shutdown());
            }
        }

        public string CurrentVersion
        {
            get => _currentVersion;
            set
            {
                if (value == _currentVersion) return;
                _currentVersion = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange("CanGenerateUsbUpdate");
            }
        }

        public ICommand SearchCobraUserCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    SearchCobraViewModel viewModel = new SearchCobraViewModel();
                    var view = new SearchCobraView();
                    view.DataContext = viewModel;

                    var result = view.ShowDialog();

                    if (viewModel.SelectedCobraAddress != null)
                    {
                        SelectedCustomer.CobraAddressGuid = viewModel.SelectedCobraAddress.Guid.ToString();
                        SelectedCustomer.CobraAddress = viewModel.SelectedCobraAddress.AssociatedObject;
                        SelectedCustomer.Mail = viewModel.SelectedCobraAddress.Email;
                        SelectedCustomer.Name = viewModel.SelectedCobraAddress.Company;

                        NotifyOfPropertyChange("SelectedCustomerCobraMappingId");
                        NotifyOfPropertyChange("SelectedCustomerCompany");
                        NotifyOfPropertyChange("SelectedCustomerContact");
                        NotifyOfPropertyChange("SelectedCustomerAddress");
                        NotifyOfPropertyChange("SelectedCustomerMail");
                    }
                });
            }
        }

        public bool CanGenerateUsbUpdate => !string.IsNullOrWhiteSpace(_currentVersion) && Version.TryParse(_currentVersion, out _);

        public ICommand GenerateUsbUpdateCommand
        {
            get { return new DelegateCommand(() =>
            {
                if (MessageBox.Show("Wollen Sie wirklich eine USB Update Information an alle Kunden schicken?",
                        "Bitte bestätigen", MessageBoxButton.OK) != MessageBoxResult.OK) return;
                OnGenerateUsbUpdate();
                UpdateCustomer();
            });}
        }

        private void OnGenerateUsbUpdate()
        {
            using (var context = new ActivationServer.Data.ActivationContext())
            {
                var currentVersion = new Version(CurrentVersion);
                var activationKeys = context.ActivationKey.Include(x => x.Customer).Include("ProductType")
                    .Include("ProductAddOn").Include("ActivatedMachine").ToList();

                var updatedCustomerIds = new List<Tuple<int, string>>();

                foreach (var activationKey in activationKeys)
                {
                    if(!(activationKey.ValidFrom < DateTime.Now && activationKey.ValidTo > DateTime.Now)) continue;
                    
                    if(string.IsNullOrWhiteSpace(activationKey.Customer.Mail)) continue;

                    if (!(from activatedMachine in activationKey.ActivatedMachine
                            where !string.IsNullOrWhiteSpace(activatedMachine.CurrentVersion)
                            select new Version(activatedMachine.CurrentVersion))
                        .Any(version => version < currentVersion)) continue;
                    if (updatedCustomerIds.All(x => x.Item1 != activationKey.CustomerId))
                    {
                        activationKey.Customer.UpdateGuid = Guid.NewGuid().ToString("N");
                        SendUsbUpdateMail(activationKey);
                        updatedCustomerIds.Add(new Tuple<int, string>(activationKey.CustomerId, activationKey.SerialNumbers));
                    }
                    else if (!updatedCustomerIds.Any(x =>
                        x.Item1 == activationKey.CustomerId && x.Item2 == activationKey.SerialNumbers))
                    {
                        SendUsbUpdateMail(activationKey);
                        updatedCustomerIds.Add(new Tuple<int, string>(activationKey.CustomerId, activationKey.SerialNumbers));
                    }
                }

                context.SaveChanges();
            }
        }

        private void SendUsbUpdateMail(ActivationKey activationKey)
        {
            try
            {
                var mailAddress = new MailAddress(activationKey.Customer.Mail);
                using (var message = new MailMessage
                {
                    From = new MailAddress("maik.windhorst@ols-bio.de", "Maik Windhorst (OMNI Life Science GmbH & Co KG)"),
                    Sender = new MailAddress("maik.windhorst@ols-bio.de", "Maik Windhorst (OMNI Life Science GmbH & Co KG)")
                })
                { 
                    message.ReplyToList.Add(new MailAddress("maik.windhorst@ols-bio.de", "Maik Windhorst (OMNI Life Science GmbH & Co KG)"));
                    message.To.Add(mailAddress);
                    //message.To.Add("maik.windhorst@ols-bio.de");
                    message.Bcc.Add(new MailAddress("maik.windhorst@ols-bio.de"));
                    message.Subject = $"Easter / Corona Update for all users of NEW CASY Software {_currentVersion}";
                    message.IsBodyHtml = true;
                    message.Body = "<p>Dear customer,</p>"
+ "<p>The new CASY Software has seen many improvements and new features like START UP and SHUT DOWN Wizard.<br>"
+ "The update to version 1.0.2.2 focuses on bug fixing and stability improvments.Furthermore we've implemented a new feature requested by many users:"
+ "<ul>"
+ "<li>“Print all“ with a single click (printing icon in the top menu, which allows to create PDF of all open measurements)</li>"
+ "</ul>"
+ "<p><b>We recommend all users to update to this version.</b></p>"
+ "<p>Please note, that all customers have access to updates during the first 12 month.You can check the expiration date of your update guarantee in CASY software information screen.<br>"
+ "To extend the period, please contact your local CASY partner for a quote.</p>"
+ "<p><strong><u>How to update:</u></strong></p>"
+ "<p>CASY control units connected to internet will have automatic access to the update server when you start the software next time. Otherwise, you can install the update via USB stick as described below.</p>"
+ "<p><b>UPDATE by USB Stick:</b></p>"
+ "<ol>"
+ "<li>The update USB stick must be prepared on a Windows PC!</li>"
+ $"<li>Download the ZIP file using this link: <a href=\"http://update.ols-bio.de/api/update/usbUpdate/{activationKey.Customer.UpdateGuid}/{activationKey.SerialNumbers}\">http://update.ols-bio.de/api/update/usbUpdate/{activationKey.Customer.UpdateGuid}/{activationKey.SerialNumbers}</a><br>"
+ "<b>Please note:</b>It may be neccessary to accept the security protocol of our german OLS CASY update server </li>"
+ "<li>Unzip the file with the free tool 7Zip(<a href=\"https://www.7-zip.org/download.html\">https://www.7-zip.org/download.html</a>) to an empty USB stick</li>"
+ "<li>Plug in the USB stick while CASY software is running and you're logged in with an account with supervisor privileges. Update will be detected automatically.</li>"
+ "</ol><p>Feel free to contact me directly if you need further assistance or have further questions.</p>"
+ "<p>------</p>"
+ "<p>Best regards</p>"
+ "<p>Maik Windhorst | OLS - OMNI Life Science GmbH &amp;Co KG<br>"
+ "Head of Software Development<br>"
+ "T +49 421 276 169 0 | M +49 151 56 23 1234 <br>"
+ "<a href=\"mailto:maik.windhorst@ols-bio.de\">maik.windhorst@ols-bio.de</a> | <a href=\"http://www.ols-bio.de\">www.ols-bio.de</a></p>"
+ "<p>Karl-Ferdinand-Braun-Stra&szlig;e 2 | 28359 Bremen | Germany | Gesch&auml;ftsf&uuml;hrer: Dagmar J&uuml;rgens | Amtsgericht Bremen, HRA 23428</p>";

                    ServicePointManager.ServerCertificateValidationCallback =
                        (s, certificate, chain, sslPolicyErrors) => true;

                    using (var client = new SmtpClient
                    {
                        Host = "192.168.110.3",
                        Port = 587,
                        EnableSsl = true,
                        Credentials = new NetworkCredential("mwindhorst", "ZcM53211")
                    })
                    {
                        client.Send(message);
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private int GenerateOrUpdateActivationKey()
        {
            using (var context = new ActivationServer.Data.ActivationContext())
            {
                var customer = context.Customer.FirstOrDefault(c => c.Name == CustomerName);
                if (customer == null)
                {
                    customer = new Customer
                    {
                        Name = CustomerName
                    };
                    context.Customer.Add(customer);
                    context.SaveChanges();
                }

                ActivationKey activationKeyEntity = null;

                if (_selectedActivationKey.Id == 0)
                {
                    var alreadyExists = true;

                    var key = new char[19];
                    string activationKey;
                    while (alreadyExists)
                    {
                        for (var i = 0; i < key.Length; i++)
                        {
                            if (i == 4 || i == 9 || i == 14)
                            {
                                key[i] = '-';
                            }
                            else
                            {
                                key[i] = GetLetter();
                            }
                        }
                        activationKey = new string(key);

                        activationKeyEntity = context.ActivationKey.FirstOrDefault(ak => ak.Value == activationKey);

                        if (activationKeyEntity != null) continue;

                        alreadyExists = false;

                        activationKeyEntity = new ActivationKey
                        {
                            CustomerId = customer.Id,
                            MaxNumActivations = MaxNumActivations,
                            SerialNumbers = ValidSerialNumber,
                            ProductTypeId = IsCounter ? 3 : (IsControl ? 1 : 2),
                            ValidFrom = DateTime.UtcNow,
                            ValidTo = ValidTo,
                            Value = activationKey,
                            Responsible = Responsible
                        };

                        context.ActivationKey.Add(activationKeyEntity);
                    }
                }
                else
                {
                    activationKeyEntity = context.ActivationKey.FirstOrDefault(ak => ak.Id == _selectedActivationKey.Id);
                    if (activationKeyEntity != null)
                    {
                        activationKeyEntity.MaxNumActivations = MaxNumActivations;
                        activationKeyEntity.SerialNumbers = ValidSerialNumber;
                        activationKeyEntity.ProductTypeId = IsCounter ? 3 : (IsControl ? 1 : 2);
                        activationKeyEntity.ValidTo = ValidTo;
                        activationKeyEntity.Responsible = Responsible;
                        activationKeyEntity.CustomerId = customer.Id;
                    }
                }

                if (activationKeyEntity == null) return -1;
                activationKeyEntity.ActivationKeyProductAddOns.Clear();

                if (IsAdAuth)
                {
                    activationKeyEntity.ActivationKeyProductAddOns.Add(new ActivationKey_ProductAddOns_Mappings()
                    {
                        ActivationKeyId = activationKeyEntity.Id,
                        ProductAddOnId = 1
                    });
                }

                if (IsControl)
                {
                    activationKeyEntity.ActivationKeyProductAddOns.Add(new ActivationKey_ProductAddOns_Mappings()
                    {
                        ActivationKeyId = activationKeyEntity.Id,
                        ProductAddOnId = 2
                    });
                }

                if (IsCounter)
                {
                    activationKeyEntity.ActivationKeyProductAddOns.Add(new ActivationKey_ProductAddOns_Mappings()
                    {
                        ActivationKeyId = activationKeyEntity.Id,
                        ProductAddOnId = 3
                    });
                }

                if (IsLocalAuth)
                {
                    activationKeyEntity.ActivationKeyProductAddOns.Add(new ActivationKey_ProductAddOns_Mappings()
                    {
                        ActivationKeyId = activationKeyEntity.Id,
                        ProductAddOnId = 4
                    });
                }

                if (IsSimulator)
                {
                    activationKeyEntity.ActivationKeyProductAddOns.Add(new ActivationKey_ProductAddOns_Mappings()
                    {
                        ActivationKeyId = activationKeyEntity.Id,
                        ProductAddOnId = 5
                    });
                }

                if (IsTtSwitch)
                {
                    activationKeyEntity.ActivationKeyProductAddOns.Add(new ActivationKey_ProductAddOns_Mappings()
                    {
                        ActivationKeyId = activationKeyEntity.Id,
                        ProductAddOnId = 6
                    });
                }

                if (IsCfr)
                {
                    activationKeyEntity.ActivationKeyProductAddOns.Add(new ActivationKey_ProductAddOns_Mappings()
                    {
                        ActivationKeyId = activationKeyEntity.Id,
                        ProductAddOnId = 7
                    });
                }

                if (IsTrial)
                {
                    activationKeyEntity.ActivationKeyProductAddOns.Add(new ActivationKey_ProductAddOns_Mappings()
                    {
                        ActivationKeyId = activationKeyEntity.Id,
                        ProductAddOnId = 9
                    });
                }

                if (IsAccess)
                {
                    activationKeyEntity.ActivationKeyProductAddOns.Add(new ActivationKey_ProductAddOns_Mappings()
                    {
                        ActivationKeyId = activationKeyEntity.Id,
                        ProductAddOnId = 10
                    });
                }

                context.SaveChanges();

                return activationKeyEntity.Id;
            }
        }

        private char GetLetter()
        {
            var chars = "abcdefghijklmnopqrstuvwxyz1234567890ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var num = _rand.Next(0, chars.Length - 1);
            return chars[num];
        }
    }
}
