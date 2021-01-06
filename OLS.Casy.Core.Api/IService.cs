using System;

namespace OLS.Casy.Core.Api
{
    /// <summary>
	///     Interface for (applicational) service implementations. This interface defines a protocol of several phases for
	///     startup and shutdown.
	///     See also the <see cref="ICoreService" />. In opposite to this interface the ICoreService is only for not very basically
	///     Services.
	/// </summary>
	public interface IService
    {
        //todo check if the concept of DependenciesReady and OnReady still needed!

        /// <summary>
        ///     Pre-condition: MEF has satisfied all references.
        ///     This  method can be used to initialize the service and perform actions, which do
        ///     not expect other dependent services with OnReady state.
        /// </summary>
        void Prepare(IProgress<string> progress);

        /// <summary>
        ///     The Deinitialize method is for cleaning up, storing and closing resouces.
        /// </summary>
        void Deinitialize(IProgress<string> progress);
    }
}
