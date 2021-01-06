using OLS.Casy.App.Models.Enums;

namespace OLS.Casy.App.Models
{
    public class MeasureResultCalculation
    {
        public string AssociatedRange { get; internal set; }
        public double? Deviation { get; internal set; }
        public MeasureResultItemTypes MeasureResultItemType { get; internal set; }
        public double ResultItemValue { get; internal set; }
    }
}
