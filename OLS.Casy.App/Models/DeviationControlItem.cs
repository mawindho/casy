using OLS.Casy.App.Models.Enums;

namespace OLS.Casy.App.Models
{
    public class DeviationControlItem
    {
        public double? MaxLimit { get; set; }
        public double? MinLimit { get; set; }
        public MeasureResultItemTypes MeasureResultItemType { get; internal set; }
    }
}
