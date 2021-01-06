using OLS.Casy.Models;

namespace OLS.Casy.Calculation.Api
{
    /// <summary>
    /// Implementation of <see cref="IDataCalculationProvider"/> for transforming counts to volume
    /// </summary>
    public interface IVolumeCalculationProvider
    {
        double[] TransformMeasureResultDataBlock(MeasureSetup measureSetup, double[] dataBlock);
    }
}
