namespace OLS.Casy.IO.SQLite.Entities
{
    public class AuditTrailEntryEntity_Deleted
    {
        public int AuditTrailEntryEntityId { get; set; }
        public int? MeasureResultEntityId { get; set; }
        //public virtual MeasureResultEntity MeasureResultEntity { get; set; }
        public int? MeasureSetupEntityId { get; set; }
        //public virtual MeasureSetupEntity MeasureSetupEntity { get; set; }
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
