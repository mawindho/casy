namespace OLS.Casy.App.Dto
{
    public class MeasureResultCalculationDto
    {
        public string MeasureResultItemType { get; set; }
        public double ResultItemValue { get; set; }
        public double? Deviation { get; set; }
        public string AssociatedRange { get; set; }
    }
}
