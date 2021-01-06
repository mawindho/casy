using System.Collections.Generic;

namespace OLS.Casy.Models
{
    public class CapillaryStatistic
    {
        public CapillaryStatistic()
        {
            this.Measure200Statistics = new List<MeasureStatistic>();
            this.Measure400Statistics = new List<MeasureStatistic>();
        }

        public ushort Diameter { get; set; }
        public ushort LastPosition200 { get; set; }
        public ushort LastPosition400 { get; set; }
        public uint CleanCount { get; set; }
        public uint MeasureCount { get; set; }
        public List<MeasureStatistic> Measure200Statistics { get; private set; }
        public List<MeasureStatistic> Measure400Statistics { get; private set; }
    }
}
