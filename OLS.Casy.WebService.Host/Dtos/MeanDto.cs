using System.Collections.Generic;

namespace OLS.Casy.WebService.Host.Dtos
{
    public class MeanDto : MeasureResultDto
    {
        public int[] MeasureResultIds { get; set; }
        public string ErrorMessage { get; set; }
        public IEnumerable<MeasureResultDto> ParentMeasureResults { get; set; }
    }
}
