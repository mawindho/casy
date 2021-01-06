using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OLS.Casy.WebService.Host
{
    public static class DateTimeOffsetExtensions
    {
        public static DateTimeOffset ParseAny(string input)
        {
            if(!DateTimeOffset.TryParse(input, out var result))
            {
                if(!DateTimeOffset.TryParse(input, CultureInfo.GetCultureInfo("de-DE"), DateTimeStyles.AssumeUniversal, out result))
                {
                    DateTimeOffset.TryParse(input, CultureInfo.InstalledUICulture, DateTimeStyles.AssumeUniversal, out result);
                }
            }

            return result;
        }
    }
}
