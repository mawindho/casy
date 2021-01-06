using System;

namespace OLS.Casy.App.Models
{
    public class Range
    {
        public DateTimeOffset CreatedAt { get; internal set; }
        public string CreatedBy { get; set; }
        public bool IsDeadCellsCursor { get; internal set; }
        public DateTimeOffset LastModifiedAt { get; internal set; }
        public string LastModifiedBy { get; set; }
        public double MaxLimit { get; internal set; }
        public double MinLimit { get; set; }
        public string Name { get; internal set; }
        public string Subpopulation { get; internal set; }
    }
}
