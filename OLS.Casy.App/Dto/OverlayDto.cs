using System;
using System.Collections.Generic;
using System.Text;

namespace OLS.Casy.App.Dto
{
    public class OverlayDto : MeasureResultDto
    {
        public int[] MeasureResultIds { get; set; }
        public string ErrorMessage { get; set; }
        public IEnumerable<MeasureResultDto> MeasureResults { get; set; }
    }
}
