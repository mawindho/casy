using System;
using System.Collections.Generic;
using System.Globalization;

namespace OLS.Casy.Core.Localization.Api
{
    public interface ILocalizationService
    {
        CultureInfo CurrentCulture { set; get; }
        //CultureInfo CurrentKeyboardCulture { get; set; }
        IEnumerable<CultureInfo> PossibleLanguages { get; }
        bool TryGetLocalizedObject(string localizationKey, out object localizedObject, params object[] parameter);
        string GetLocalizedString(string localizationKey, params string[] parameter);

        event EventHandler<LocalizationEventArgs> LanguageChanged;

        DateTime ParseString(string s);
    }
}
