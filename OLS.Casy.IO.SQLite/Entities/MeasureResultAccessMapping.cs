namespace OLS.Casy.IO.SQLite.Entities
{
    public class MeasureResultAccessMapping
    {
        public int MeasureResultAccessMappingId { get; set; }
        //public int MeasureResultEntityId { get; set; }
        public int? UserEntityId { get; set; }
        public int? UserGroupEntityId { get; set; }
        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }
        public int? MeasureResultEntityId { get; set; }
        //public virtual MeasureResultEntity MeasureResultEntity { get; set; }
        //public virtual MeasureResultEntity_Deleted MeasureResultEntityDeleted { get; set; }
    }
}
