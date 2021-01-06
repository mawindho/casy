using OLS.Casy.Models.Enums;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace OLS.Casy.Models
{
    public class MeasureResultItemsContainer
    {
        public MeasureResultItemsContainer(MeasureResultItemTypes measureResultItemType)
        {
            MeasureResultItemType = measureResultItemType;
            MeasureResultItem = new MeasureResultItem(measureResultItemType);
            CursorItems = new List<MeasureResultItem>();
            ValueFormat = MeasureResultItem.ValueFormats[measureResultItemType];
    }

        public MeasureResultItem MeasureResultItem { get; }
        public MeasureResultItemTypes MeasureResultItemType { get; }
        public string ValueFormat { get; }
        public MeasureResult MeasureResult { get; set; }
        public List<MeasureResultItem> CursorItems { get; }
    }
}
