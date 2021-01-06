using System;

namespace OLS.Casy.Core.Api
{
    /// <summary>
    /// Interface for a service providing information for the current execution environment
    /// </summary>
    public interface IEnvironmentService
    {
        /// <summary>
        /// Sets environment information
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        void SetEnvironmentInfo(string key, object value);

        /// <summary>
        /// Gets environment information for the passed key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Value</returns>
        object GetEnvironmentInfo(string key);
        event EventHandler<string> EnvironmentInfoChangedEvent;

        string GetExecutionPath();
        string GetUniqueId();
        string GetDateTimeString(DateTime dateTime, bool ignoreTimezone = false);
    }
}
