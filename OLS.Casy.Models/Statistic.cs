using System;
using System.Collections.Generic;

namespace OLS.Casy.Models
{
    public class Statistic
    {
        public Statistic()
        {
            this.CapillaryStatistics = new List<CapillaryStatistic>();
            this.ErrorStatistics = new List<ErrorStatistic>();
        }

        public List<CapillaryStatistic> CapillaryStatistics { get; private set; } 
        public List<ErrorStatistic> ErrorStatistics { get; private set; }

        public DateTime? LastPowerOn { get; set; }
        public DateTime? LastUpdateWorkingCounter { get; set; }
        public DateTime? LastResetStatistics { get; set; }
        public TimeSpan PowerUpTime { get; set; }

        public uint PowerOnCount { get; set; }
        public uint AveragePowerOnTime { get; set; }
        public uint AveragePowerOffTime { get; set; }
    }
}
