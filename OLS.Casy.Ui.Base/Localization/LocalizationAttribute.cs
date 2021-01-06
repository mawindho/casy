using OLS.Casy.Ui.Base.DyamicUiHelper;
using System;
using System.Reflection;

namespace OLS.Casy.Ui.Base.Localization
{
    public class LocalizationAttribute : AttributeBase
    {
        private readonly string _localizationKey;
        private readonly string _localizationParameter;
        private Func<object> _localizedObjectAccessor;
        private object _localizedObject;

        internal LocalizationAttribute(string localizationKey, string localizationParameter)
        {
            //if (string.IsNullOrEmpty(localizationKey))
            //{
            //    throw new ArgumentNullException("localizationKey");
            //}

            this._localizationKey = localizationKey;
            this._localizationParameter = localizationParameter;
        }

        internal LocalizationResult Localize()
        {
            object localizedObject;
            if (Localization.LocalizationService.TryGetLocalizedObject(_localizationKey, out localizedObject))
            {
                string localizedString = localizedObject as string;

                if(localizedString != null && !string.IsNullOrEmpty(_localizationParameter))
                {
                    localizedObject = string.Format(localizedString, _localizationParameter);
                }

                return new LocalizationResult(localizedObject);
            }

            return new LocalizationResult(string.Format("Unkown localization key '{0}'", _localizationKey));
        }

        public Func<object> LocalizedObjectAccessor
        {
            get
            {
                if (this._localizedObjectAccessor == null)
                {
                    this._localizedObjectAccessor = this.CreateLocalizedObjectAccessor();
                }
                return this._localizedObjectAccessor;
            }
        }

        internal object LocalizedObject
        {
            get { return _localizedObject; }
            set
            {
                this._localizedObject = value;
                this._localizedObjectAccessor = null;
            }
        }

        private Func<object> CreateLocalizedObjectAccessor()
        {
            return () => this.LocalizedObject;
        }
    }
}
