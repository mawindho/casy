using OLS.Casy.Core.Config.Api;
using System.Linq;
using System.Reflection;

namespace OLS.Casy.Core.Config
{
    /// <summary>
    /// Wrapper class for extension methods used for configuration 
    /// </summary>
    internal static class Extensions
    {
        internal static bool ContainsConfigItemAttributes(this object instanceWithConfigItemAttributes)
        {
            PropertyInfo[] props = instanceWithConfigItemAttributes.GetType().GetProperties();
            return props.Any(property => property.GetCustomAttributes(true).Any(customAttribute => customAttribute is ConfigItemAttribute));
        }
    }
}
