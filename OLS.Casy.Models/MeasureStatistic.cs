using System;

namespace OLS.Casy.Models
{
    public class MeasureStatistic
    {
        public ushort[] ErrorCode { get; set; }
        public DateTime? Timestamp { get; set; }
        public ushort Time2 { get; set; }
        public ushort Time3 { get; set; }
        public ushort BubbleTime { get; set; }
    }
}
