namespace OLS.Casy.App.Dto
{
    public class MeasureResultDataDto
    {
        public string DataBlock { get; set; }
        public int BelowMeasureLimitCount { get; set; }
        public int BelowCalibrationLimitCount { get; set; }
        public int AboveCalibrationLimitCount { get; set; }
        public bool ConcentrationTooHigh { get; set; }
    }
}
