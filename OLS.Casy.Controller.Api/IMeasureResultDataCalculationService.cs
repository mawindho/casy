using System.Collections.Generic;
using System.Threading.Tasks;
using OLS.Casy.Models;

namespace OLS.Casy.Controller.Api
{
    public interface IMeasureResultDataCalculationService
    {
        Task UpdateMeasureResultDataAsync(MeasureResult measureResult, MeasureSetup measureSetup = null);
        Task<IEnumerable<MeasureResultItem>> GetMeasureResultDataAsync(MeasureResult measureResult, MeasureSetup measureSetup = null);
        Task<double[]> SumMeasureResultDataAsync(MeasureResult measureResult);
        Task UpdateMeanDeviationsAsync(MeasureResult meanMeasureResult, MeasureResult[] measureResults);
    }
}
