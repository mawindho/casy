using System;
using Xamarin.Forms;

namespace OLS.Casy.App.Models
{
    public class AuditTrailItem : BindableObject
    {
        public string Action { get; internal set; }
        public string ComputerName { get; set; }
        public DateTimeOffset DateChanged { get; internal set; }
        public string EntityName { get; internal set; }
        public string NewValue { get; internal set; }
        public string OldValue { get; internal set; }
        public string PrimaryKeyValue { get; internal set; }
        public string PropertyName { get; internal set; }
        public string SoftwareVersion { get; internal set; }
        public string UserChanged { get; internal set; }
    }
}
