using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace OLS.Casy.Ui.Base.Converters
{
    /// <summary>
	///     Converts a comma seperated string to an array of strings
	/// </summary>
	public class StringArrayConverter : TypeConverter
    {
        /// <summary>
        ///     Indicates if the converter is able to convert the passed type.
        /// </summary>
        /// <returns>
        ///     True, if the converter is able to procceed the conversion, false otherwise
        /// </returns>
        /// <param name="context">
        ///     An <see cref="T:System.ComponentModel.ITypeDescriptorContext" />-interface providing the formatting context
        /// </param>
        /// <param name="sourceType">
        ///     A <see cref="T:System.OperatorUsage" />-class represeting the initial type of the conversion
        /// </param>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return (sourceType == typeof(string));
        }

        /// <summary>
        ///     Converts the passed object with the help of the passed context ad culture information to the type of the converter.
        /// </summary>
        /// <returns>
        ///     An <see cref="T:System.Object" /> representing the voncerted value.
        /// </returns>
        /// <param name="context">
        ///     A <see cref="T:System.ComponentModel.ITypeDescriptorContext" />-interface providing a formatting context.
        /// </param>
        /// <param name="culture">The currently used culture.</param>
        /// <param name="value">
        ///     The <see cref="T:System.Object" />-class to be converted.
        /// </param>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var valueString = value as string;
            return string.IsNullOrEmpty(valueString) ? new string[0] : valueString.Split(',').Select(s => s.Trim()).ToArray();
        }
    }
}
