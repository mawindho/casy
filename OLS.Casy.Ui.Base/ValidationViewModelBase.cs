using OLS.Casy.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Collections;
using System.Collections.Concurrent;

namespace OLS.Casy.Ui.Base
{
    public abstract class ValidationViewModelBase : ViewModelBase, IDataErrorInfo, INotifyDataErrorInfo
    {
        protected readonly ConcurrentDictionary<string, ICollection<string>> _validationErrors = new ConcurrentDictionary<string, ICollection<string>>();

        /// <summary>
		///     Not supported and needed in this implementation of <see cref="IDataErrorInfo" />.
		/// </summary>
        public string Error => throw new NotSupportedException();

        public bool HasErrors => _validationErrors.Count > 0;

        /// <summary>
        ///     Returns the error mssage for the property of the passed name.
        /// </summary>
        /// <returns>
        ///     The error message of the property. Default value is empty string ("").
        /// </returns>
        /// <param name="propertyName">The name of the property the error message shall be returned of.</param>
        public string this[string propertyName] => OnValidate(propertyName);

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        protected void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        /// <summary>
        ///     Validates the property with the passed name based on WF stadard validation functionality.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        protected virtual string OnValidate(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentException("Invalid property name", propertyName);
            }

            var error = string.Empty;
            var value = GetValue(propertyName);
            var results = new List<ValidationResult>(1);

            var result = Validator.TryValidateProperty(value, new ValidationContext(this, null, null)
            {
                MemberName = propertyName
            }, results);

            if (result) return error;
            
            var validationResult = results.First();
            error = validationResult.ErrorMessage;

            return error;
        }

        private object GetValue(string propertyName)
        {
            var value = ReflectionUtils.GetValue(this, propertyName);

            if (value == null) return null;
            
            var propertyDescriptor = TypeDescriptor.GetProperties(GetType()).Find(propertyName, false);
            if (propertyDescriptor == null)
            {
                throw new ArgumentException("Invalid property name", propertyName);
            }
            value = propertyDescriptor.GetValue(this);

            return value;
        }

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_validationErrors.ContainsKey(propertyName))
                return null;

            return _validationErrors[propertyName];
        }
    }
}
