using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OLS.Casy.Ui.Base.Dtos
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
