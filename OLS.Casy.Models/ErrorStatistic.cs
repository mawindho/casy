using System;

namespace OLS.Casy.Models
{
    public class ErrorStatistic
    {
        public DateTime? FirstOccured { get; set; }
        public DateTime? LastOccured { get; set; }
        public ushort OccurenceCount { get; set; }
    }
}
