using System;
using System.Globalization;

namespace OLS.Casy.App.Extensions
{
    public static class DateTimeOffsetExtensions
    {
        public static DateTimeOffset ParseAny(string input)
        {
            if (!DateTimeOffset.TryParse(input, out var result))
            {
                if (!DateTimeOffset.TryParse(input, CultureInfo.GetCultureInfo("de-DE"), DateTimeStyles.AssumeUniversal, out result))
                {
                    DateTimeOffset.TryParse(input, CultureInfo.InstalledUICulture, DateTimeStyles.AssumeUniversal, out result);
                }
            }

            return result;
        }
    }
}
