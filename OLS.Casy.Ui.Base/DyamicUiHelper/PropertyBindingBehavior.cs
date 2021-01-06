using System;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Data;

namespace OLS.Casy.Ui.Base.DyamicUiHelper
{
    /// <summary>
	///     Abstract base class for a property binding behavior
	/// </summary>
	/// <typeparam name="TConverter">OperatorUsage of the converter used by the behavior</typeparam>
	public class PropertyBindingBehavior<TConverter> : BehaviorBase where TConverter : IValueConverter, new()
    {
        private const string DEPENDENCY_PROPERTY_SUFFIX = "Property";

        private readonly string _propertyName;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="propertyName">Name of the property the behavior is associated with</param>
        /// <exception cref="ArgumentNullException"></exception>
        public PropertyBindingBehavior(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentNullException("propertyName");
            }
            this._propertyName = propertyName;
        }

        private string PropertyName
        {
            get { return this._propertyName; }
        }

        /// <summary>
        ///     Adds the behavior to the passed target object and associates it with te passed <see cref="Source" />
        /// </summary>
        /// <param name="target">the target object the behavior shall be attached</param>
        /// <param name="source">
        ///     The <see cref="Source" /> for the behavior
        /// </param>
        public override void AddBehavior(object target, Source source)
        {
            var targetDependencyObject = target as DependencyObject;
            if (targetDependencyObject != null)
            {
                BindingOperations.SetBinding(targetDependencyObject, this.GetDependencyProperty(target),
                                            new Binding("Result") { Source = source, Converter = new TConverter() });
            }
        }

        /// <summary>
        ///     Removes the behavior from the passed target object.
        /// </summary>
        /// <param name="target">the target object the behavior shall be removed</param>
        public override void RemoveBehavior(object target)
        {
            var targetDependencyObject = target as DependencyObject;
            if (targetDependencyObject != null)
            {
                var binding = BindingOperations.GetBinding(targetDependencyObject, this.GetDependencyProperty(target));
                var source = binding.Source as Source;
                if(source != null)
                {
                    source.Dispose();
                }
                targetDependencyObject.SetValue(this.GetDependencyProperty(target), DependencyProperty.UnsetValue);
            }
        }

        private DependencyProperty GetDependencyProperty(object target)
        {
            string dependencyPropertyName = this.PropertyName + DEPENDENCY_PROPERTY_SUFFIX;

            FieldInfo fieldInfo = target.GetType()
                                        .GetField(dependencyPropertyName, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            if (fieldInfo == null || fieldInfo.FieldType != typeof(DependencyProperty))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                                                                "There is no dependency property with the name '{0}' on the target of type '{1}' to bind to.",
                                                                dependencyPropertyName, target.GetType()));
            }
            return fieldInfo.GetValue(null) as DependencyProperty;
        }
    }
}
