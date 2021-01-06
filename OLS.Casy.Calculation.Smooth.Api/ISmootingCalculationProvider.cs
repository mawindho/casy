using OLS.Casy.Calculation.Api;

namespace OLS.Casy.Calculation.Smooth.Api
{
    /// <summary>
    /// Implementation of <see cref="IDataCalculationProvider"/> for smooting the data
    /// like the old casy application does it
    /// </summary>
    public interface ISmootingCalculationProvider : IDataCalculationProvider
    {
        /// <summary>
        /// Property for the width used by smooting alogirthm
        /// </summary>
        int Width { get; set; }
    }
}
