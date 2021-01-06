namespace OLS.Casy.Calculation.Api
{
    /// <summary>
    /// Interface fo data calulation providers available in the current application
    /// </summary>
    public interface IDataCalculationProvider
    {
        /// <summary>
        /// Starts the calulation progress and transforms the passed data block
        /// </summary>
        /// <param name="dataBlock">Original data block to be transformed by the data calculation provider</param>
        /// <returns>Transformed data block</returns>
        double[] TransformMeasureResultDataBlock(double[] dataBlock);
    }
}
