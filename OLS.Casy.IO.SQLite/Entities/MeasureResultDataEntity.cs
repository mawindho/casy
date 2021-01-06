using System;

namespace OLS.Casy.IO.SQLite.Entities
{
    [Serializable]
    public class MeasureResultDataEntity
    {
        public MeasureResultDataEntity()
        {
            CreatedAt = DateTimeOffset.MinValue.ToString();
            LastModifiedAt = DateTimeOffset.MinValue.ToString();
        }

        public int MeasureResultDataEntityId { get; set; }
        public string DataBlock { get; set; }
        public int BelowMeasureLimtCount { get; set; }
        public int BelowCalibrationLimitCount { get; set; }
        public int AboveCalibrationLimitCount { get; set; }
        public bool ConcentrationTooHigh { get; set; }
        public string Color { get; set; }
        public int MeasureResultEntityId { get; set; }
        public virtual MeasureResultEntity MeasureResultEntity { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedAt { get; set; }
        public string LastModifiedBy { get; set; }
        public string LastModifiedAt { get; set; }
    }
}
