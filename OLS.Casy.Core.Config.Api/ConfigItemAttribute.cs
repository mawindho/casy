using System;

namespace OLS.Casy.Core.Config.Api
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ConfigItemAttribute : Attribute
    {
        public ConfigItemAttribute(object defaultValue, string displayName = null)
        {
            this.DefaultValue = defaultValue;
            this.DisplayName = displayName;
        }

        public object DefaultValue { get; set; }
        public string DisplayName { get; set; }
    }
}
