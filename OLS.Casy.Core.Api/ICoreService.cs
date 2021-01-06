using System;

namespace OLS.Casy.Core.Api
{
    /// <summary>
	///     This interface isdefined for very basically services.
	/// </summary>
	public interface ICoreService
    {
        /// <summary>
        ///     Pre-condition: MEF has satisfied all references.
        ///     The Initialize phase is for preparations.
        /// </summary>
        void Initialize(IProgress<string> progress);

        /// <summary>
        ///     Pre-condition: Operational phase is done.
        ///     The Deinitialize phase is for cleaning up, storing and closing resouces.
        /// </summary>
        void Deinitialize(IProgress<string> progress);
    }
}
