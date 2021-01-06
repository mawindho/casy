using System;

namespace OLS.Casy.ActivationServer.Cobra.Models
{
    public class Address
    {
        public int Id { get; set; }
        public Guid Guid { get; set; }
        public string Company1 { get; set; }
        public string Company2 { get; set; }
        public string Company3 { get; set; }
        public string Department { get; set; }
        public string Salutation { get; set; }
        public string Title { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Position { get; set; }
        public string Street { get; set; }
        public string Postbox { get; set; }
        public string Country { get; set; }
        public string PostalCode { get; set; }
        public string City { get; set; }
        public string Phone { get; set; }
        public string DirectPhone { get; set; }
        public string MobilePhone { get; set; }
        public string Fax { get; set; }
        public string Email { get; set; }

    }
}
