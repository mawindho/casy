using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OLS.Casy.Ui.Base.Dtos
{
    public class DeviationControlDto
    {
        public string MeasureResultItemType { get; set; }
        public double? MinLimit { get; set; }
        public double? MaxLimit { get; set; }
    }
}
