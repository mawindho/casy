using System.Collections.Generic;
using System.Globalization;

namespace OLS.Casy.Core.Api
{
    /// <summary>
    /// Interface for implementations providing specialized assets of their assemblies
    /// </summary>
    public interface IAssetsProvider
    {
        /// <summary>
        /// Poperty for a list of Uri-strings pointing the assmbly specific assets (e.g. ResourceDictionaries)
        /// </summary>
        IEnumerable<string> AssetUris { get; }

        /// <summary>
        /// Property to set current thread culture of the assembly according to current application culture from outside
        /// </summary>
        void SetCurrentCulture(CultureInfo cutureInfo);

        //void SetCurrentKeybordCulture(CultureInfo cultureInfo);
    }
}
