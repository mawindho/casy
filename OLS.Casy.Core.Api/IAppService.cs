namespace OLS.Casy.Core.Api
{
    /// <summary>
	///     The IAppService Service implements the system extensibility to extend the system by product specific logic.
	/// </summary>
	public interface IAppService : ICoreService
    {
        /// <summary>
        ///     Getter and Setter for product name.
        /// </summary>
        string ProductName { get; }

        /// <summary>
        ///     Getter and Setter for product version.
        /// </summary>
        string Version { get; }
    }
}
