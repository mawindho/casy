using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OLS.Casy.WebService.Host.Dtos
{
    public class MeasureResultCalculationDto
    {
        public string MeasureResultItemType { get; set; }
        public double ResultItemValue { get; set; }
        public double? Deviation { get; set; }
        public string AssociatedRange { get; set; }
    }
}
