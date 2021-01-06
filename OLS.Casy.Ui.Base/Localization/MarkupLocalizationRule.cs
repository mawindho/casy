using OLS.Casy.Ui.Base.DyamicUiHelper;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace OLS.Casy.Ui.Base.Localization
{
    internal class MarkupLocalizationRule : Rule
    {
        private readonly string _target;

        public MarkupLocalizationRule()
            : this(string.Empty)
        {

        }

        public MarkupLocalizationRule(string target)
        {
            this._target = target;
        }

        public string Target
        {
            get { return _target; }
        }

        public override IEnumerable<AttributeBase> GetAttributes(object target)
        {
            var attributes = new List<LocalizationAttribute>();

            attributes.AddRange(target.GetType().GetCustomAttributes(true).OfType<LocalizationAttribute>());

            var targetDependencyObject = target as DependencyObject;
            if (targetDependencyObject != null)
            {
                var isLocalized = Localization.GetIsLocalized(targetDependencyObject);

                if (isLocalized)
                {
                    var localizableKey = Localization.GetLocalizeKey(targetDependencyObject);
                    var localizeParameter = Localization.GetLocalizationParameter(targetDependencyObject);
                    var targetProperty = Localization.GetTargetProperty(targetDependencyObject);

                    if (!string.IsNullOrEmpty(localizableKey))
                    {
                        attributes.Add(new LocalizationAttribute(localizableKey, localizeParameter));
                    }
                    else
                    {
                        PropertyInfo fieldInfo = null;

                        if (!string.IsNullOrEmpty(targetProperty))
                        {
                            fieldInfo = targetDependencyObject.GetType().GetProperty(targetProperty);
                        }
                        else if (!string.IsNullOrEmpty(_target))
                        {
                            fieldInfo = targetDependencyObject.GetType().GetProperty(_target);
                        }

                        if (fieldInfo != null)
                        {
                            localizableKey = fieldInfo.GetValue(targetDependencyObject, null) as string;

                            if (localizableKey != null)
                            {
                                attributes.Add(new LocalizationAttribute(localizableKey, localizeParameter));
                                Localization.SetLocalizeKey(targetDependencyObject, localizableKey);
                            }
                        }
                    }
                }
            }

            return attributes;
        }

        /*
		private FrameworkElement GetOwnerView(FrameworkElement frameworkElement)
		{
			if (frameworkElement == null)
			{
				return null;
			}

			var parent = frameworkElement.Parent as FrameworkElement ?? frameworkElement.TemplatedParent as FrameworkElement;

			if (parent is UserControl || parent is Window)
			{
				return parent;
			}

			return GetOwnerView(parent);
		}
		 */

        public override IEnumerable<BehaviorBase> GetBehaviors(object target)
        {
            var behaviors = new List<BehaviorBase>();

            var targetDependencyObject = target as DependencyObject;

            if (targetDependencyObject != null)
            {
                var targetProperty = Localization.GetTargetProperty(targetDependencyObject);

                if (!string.IsNullOrEmpty(targetProperty))
                {
                    behaviors.Add(new PropertyBindingBehavior<LocalizationConverter>(targetProperty));
                }
            }

            if (!string.IsNullOrEmpty(this.Target))
            {
                behaviors.Add(new PropertyBindingBehavior<LocalizationConverter>(this.Target));
            }

            return behaviors;
        }
    }
}
