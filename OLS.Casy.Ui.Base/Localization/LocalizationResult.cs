using OLS.Casy.Ui.Base.DyamicUiHelper;

namespace OLS.Casy.Ui.Base.Localization
{
    public sealed class LocalizationResult : Result
    {
        private readonly object _localizedObject;

        internal LocalizationResult(object localizedObject)
        {
            this._localizedObject = localizedObject;
        }

        internal object LocalizedObject
        {
            get { return this._localizedObject; }
        }
    }
}
