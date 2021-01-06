using System;
using System.Globalization;
using System.Reflection;

namespace OLS.Casy.Ui.Base.DyamicUiHelper
{
    /// <summary>
	///     Abstract base class for attributes used by dyamic ui scenarios
	/// </summary>
	public class AttributeBase : Attribute
    {
        private string _errorMessage;
        private Func<string> _errorMessageAccessor;
        private Type _resourceType;

        /// <summary>
        ///     Returns an accessor to retrieve the error message (in anotherhread e.g.)
        /// </summary>
        public Func<string> ErrorMessageAccessor
        {
            get
            {
                if (this._errorMessageAccessor == null)
                {
                    this._errorMessageAccessor = this.CreateErrorMessageAccessor();
                }
                return this._errorMessageAccessor;
            }
        }

        /// <summary>
        ///     Property of the error message of the attribute
        /// </summary>
        protected string ErrorMessage
        {
            get { return this._errorMessage; }
            set
            {
                this._errorMessage = value;
                this._errorMessageAccessor = null;
            }
        }

        /// <summary>
        ///     Returns the type associated wit the attribute
        /// </summary>
        protected Type RessourceType
        {
            get { return this._resourceType; }
            set
            {
                this._resourceType = value;
                this._errorMessageAccessor = null;
            }
        }

        /// <summary>
        ///     Returns the error message formated
        /// </summary>
        /// <param name="operation"></param>
        /// <returns></returns>
        protected string FormatErrorMessage(string operation)
        {
            string message = this.ErrorMessageAccessor();
            return string.Format(CultureInfo.CurrentCulture, message, operation);
        }

        private Func<string> CreateErrorMessageAccessor()
        {
            if (this.RessourceType == null)
            {
                if (string.IsNullOrEmpty(this.ErrorMessage))
                {
                    //TODO: Austauschen
                    return () => "Default error message";
                }
                return () => this.ErrorMessage;
            }
            return this.CreateErrorMessagePropertyAccessor();
        }

        private Func<string> CreateErrorMessagePropertyAccessor()
        {
            if (string.IsNullOrEmpty(this.ErrorMessage))
            {
                //TODO:
                throw new InvalidOperationException("AutorizationAttribute requires an error message");
            }

            PropertyInfo propertyInfo = this.RessourceType.GetProperty(this.ErrorMessage,
                                                                        BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);

            if (propertyInfo != null)
            {
                MethodInfo propertyGetter = propertyInfo.GetGetMethod(true);

                if (propertyGetter == null || (!propertyGetter.IsAssembly && !propertyGetter.IsPublic))
                {
                    propertyInfo = null;
                }
            }

            if (propertyInfo == null || propertyInfo.PropertyType != typeof(string))
            {
                //TODO
                throw new InvalidOperationException("AutoizationAttribute reuires valid property");
            }

            return () => (string)propertyInfo.GetValue(null, null);
        }
    }
}
