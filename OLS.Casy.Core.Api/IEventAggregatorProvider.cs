using Prism.Events;

namespace OLS.Casy.Core.Api
{
    /// <summary>
    /// Interface providing a wrapper for the applications <see cref="IEventAggregator"/> implementation.
    /// Te IEventAggregator implementation of PRISM isn't MEF compatible, so the wrapper class is neccessary.
    /// </summary>
    public interface IEventAggregatorProvider
    {
        /// <summary>
        /// Returns the singleton instance of <see cref="IEventAggregator"/>
        /// </summary>
        IEventAggregator Instance { get; }
    }
}
