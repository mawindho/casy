using System;
using System.Reflection;

namespace OLS.Casy.Core
{
    // <summary>
    /// Helper class providing reflection functionality to get or set properties based on their name
    /// </summary>
    public static class ReflectionUtils
    {
        /// <summary>
        /// Returns the value of the property of the passed name of the passed object
        /// </summary>
        /// <param name="obj">The object, the property of the passed name shall be evaluated</param>
        /// <param name="propertyName">Name of te property to get the value</param>
        /// <returns>The value of the property. null if the property wasn't found</returns>
        public static object GetValue(object obj, string propertyName)
        {
            object propertyValue;
            if (propertyName.IndexOf(".", StringComparison.Ordinal) < 0)
            {
                PropertyInfo propertyInfo = obj.GetType().GetProperty(propertyName, BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public);
                propertyValue = propertyInfo != null ? propertyInfo.GetValue(obj, null) : null;
            }
            else
            {
                propertyValue = obj;
                foreach (var part in propertyName.Split('.'))
                {
                    if (propertyValue == null)
                    {
                        return null;
                    }

                    PropertyInfo propertyInfo = propertyValue.GetType().GetProperty(part, BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public);

                    if (propertyInfo == null)
                    {
                        return null;
                    }

                    propertyValue = propertyInfo.GetValue(propertyValue, null);
                }
            }
            return propertyValue;
        }

        /// <summary>
        /// Sets the value of the property of the passed name of the passed object
        /// </summary>
        /// <param name="obj">The object, the property of the passed name shall be set</param>
        /// <param name="propertyName">Name of the property to set the value</param>
        /// <param name="value">The new value for the property</param>
        public static void SetValue(object obj, string propertyName, object value)
        {
            if (propertyName.IndexOf(".", StringComparison.Ordinal) < 0)
            {
                PropertyInfo propertyInfo = obj.GetType().GetProperty(propertyName, BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public);
                propertyInfo.SetValue(obj, value, null);
            }
            else
            {
                object propertyValue = obj;

                foreach (var part in propertyName.Split('.'))
                {
                    if (propertyValue == null)
                    {
                        return;
                    }

                    PropertyInfo propertyInfo = propertyValue.GetType().GetProperty(part, BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public);

                    if (propertyInfo == null || !propertyInfo.CanWrite)
                    {
                        return;
                    }

                    propertyInfo.SetValue(obj, value, null);
                }
            }
        }
    }
}
