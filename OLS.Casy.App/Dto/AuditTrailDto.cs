namespace OLS.Casy.App.Dto
{
    public class AuditTrailDto
    {
        public string EntityName { get; set; }
        public string Action { get; set; }
        public string PropertyName { get; set; }
        public string PrimaryKeyValue { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public string DateChanged { get; set; }
        public string UserChanged { get; set; }
        public string ComputerName { get; set; }
        public string SoftwareVersion { get; set; }
    }
}
