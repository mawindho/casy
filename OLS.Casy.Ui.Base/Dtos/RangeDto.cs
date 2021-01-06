using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OLS.Casy.Ui.Base.Dtos
{
    public class RangeDto
    {
        public string Name { get; set; }
        public string CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string LastModifiedAt { get; set; }
        public string LastModifiedBy { get; set; }
        public double MinLimit { get; set; }
        public double MaxLimit { get; set; }
        public bool IsDeadCellsCursor { get; set; }
        public string Subpopulation { get; set; }
    }
}
