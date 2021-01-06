using OLS.Casy.Models;
using OLS.Casy.Ui.Base.DyamicUiHelper;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace OLS.Casy.Ui.Base.Authorization
{
    internal class MarkupAuthorizationRule : Rule
    {
        private readonly string _defaultTarget;

        public MarkupAuthorizationRule()
            : this(string.Empty)
        {
        }

        public MarkupAuthorizationRule(string defaultTarget)
        {
            this._defaultTarget = defaultTarget;
        }

        private string DefaultTarget
        {
            get { return this._defaultTarget; }
        }

        public override IEnumerable<AttributeBase> GetAttributes(object target)
        {
            var attributes = new List<AttributeBase>();

            attributes.AddRange(target.GetType().GetCustomAttributes(true).OfType<AuthorizationAttribute>());

            var targetDependencyObject = target as DependencyObject;
            if (targetDependencyObject != null)
            {
                UserRole minRequiredRole = Authorization.GetMinRequiredRole(targetDependencyObject);
                //UserRoles maxRequiredRole = Authorization.GetMaxRequiredRole(targetDependencyObject);
                attributes.Add(new RequiresRoleAttribute(minRequiredRole)); //, maxRequiredRole));
            }

            return attributes;
        }

        public override IEnumerable<BehaviorBase> GetBehaviors(object target)
        {
            var behaviors = new List<BehaviorBase>();

            var targetDependencyObject = target as DependencyObject;

            if (targetDependencyObject != null)
            {
                IEnumerable<string> targetProperties = Authorization.GetTargetProperties(targetDependencyObject);

                if (targetProperties != null)
                {
                    behaviors.AddRange(
                        targetProperties.Select(propertyName => new PropertyBindingBehavior<AuthorizationConverter>(propertyName)));
                }
            }

            if (!behaviors.Any() && !string.IsNullOrEmpty(this.DefaultTarget))
            {
                behaviors.Add(new PropertyBindingBehavior<AuthorizationConverter>(this.DefaultTarget));
            }

            return behaviors;
        }
    }
}
