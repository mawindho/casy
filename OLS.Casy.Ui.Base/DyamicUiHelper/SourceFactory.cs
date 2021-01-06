using System.Collections.Generic;

namespace OLS.Casy.Ui.Base.DyamicUiHelper
{
    /// <summary>
	///     Generic factory class to create <see cref="Source" /> objects
	/// </summary>
	/// <typeparam name="TSource">Concrete type of Source</typeparam>
	public class SourceFactory<TSource> where TSource : Source, new()
    {
        /// <summary>
        ///     Creates a new TSource instance.
        /// </summary>
        /// <param name="attributes">
        ///     <see cref="IEnumerable{T}" /> of <see cref="AttributeBase" /> implmentation passed as attributes for the new source object
        /// </param>
        /// <returns>The new instance of TSource</returns>
        public TSource CreateSource(IEnumerable<AttributeBase> attributes)
        {
            var newSource = new TSource();
            newSource.Attributes = attributes;
            return newSource;
        }
    }
}
