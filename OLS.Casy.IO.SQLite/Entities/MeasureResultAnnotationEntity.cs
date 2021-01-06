namespace OLS.Casy.IO.SQLite.Entities
{
    public class MeasureResultAnnotationEntity
    {
        public int MeasureResultAnnotationEntityId { get; set; }

        public string Value { get; set; }

        public int AnnotationTypeEntityId { get; set; }
        public virtual AnnotationTypeEntity AnnotationTypeEntity { get; set; }
        public int? MeasureResultEntityId { get; set; }
        //public virtual MeasureResultEntity MeasureResultEntity { get; set; }
        //public virtual MeasureResultEntity_Deleted MeasureResultEntityDeleted { get; set; }
    }
}
