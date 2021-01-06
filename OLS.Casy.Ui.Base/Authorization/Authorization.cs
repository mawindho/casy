using OLS.Casy.Core;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Models;
using OLS.Casy.Ui.Base.Converters;
using OLS.Casy.Ui.Base.DyamicUiHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace OLS.Casy.Ui.Base.Authorization
{
    /// <summary>
	///     Behavior to provide Authorization functionality for WPF dependency objects.
	/// </summary>
	public static class Authorization
    {
        private static Lazy<IAuthenticationService> _authenticationService;
        private static readonly SourceFactory<AuthorizationSource> SOURCE_FACTORY = new SourceFactory<AuthorizationSource>();
        private static readonly RuleManager RULE_MANAGER = new AuthorizationRuleManager();

        /// <summary>
        ///     <see cref="DependencyProperty" /> to define the required role for the <see cref="DependencyObject" />.
        /// </summary>
        public static readonly DependencyProperty MinRequiredRoleProperty =
            DependencyProperty.RegisterAttached(
                "MinRequiredRole",
                typeof(UserRole),
                typeof(Authorization),
                new PropertyMetadata(UserRole.None, RequiredRolePropertyChanged)
                );

        ///// <summary>
        /////     <see cref="DependencyProperty" /> to define the maximal allowed role for the <see cref="DependencyObject" />.
        ///// </summary>
        //public static readonly DependencyProperty MaxRequiredRoleProperty =
        //    DependencyProperty.RegisterAttached(
        //        "MaxRequiredRole",
        //        typeof(UserRole),
        //        typeof(Authorization),
        //        new PropertyMetadata(UserRole.None, RequiredRolePropertyChanged)
        //        );

        /// <summary>
        ///     <see cref="DependencyProperty" /> to define the properties of the <see cref="DependencyObject" /> affected by the authorization check
        /// </summary>
        public static readonly DependencyProperty TargetPropertiesProperty =
            DependencyProperty.RegisterAttached(
                "TargetProperties",
                typeof(IEnumerable<string>),
                typeof(Authorization),
                new PropertyMetadata(new string[0], TargetPropertiesPropertyChanged)
                );

        private static readonly DependencyProperty BehaviorManagerProperty =
            DependencyProperty.RegisterAttached(
                "BehaviorManager",
                typeof(BehaviorManager),
                typeof(Authorization),
                new PropertyMetadata(null)
                );

        internal static IAuthenticationService AuthenticationService
        {
            get
            {
                if (_authenticationService == null)
                {
                    _authenticationService = GlobalCompositionContainerFactory.GetExport<IAuthenticationService>();
                }
                return _authenticationService.Value;
            }
        }

        /// <summary>
        ///     Getter for the <see cref="RequiredRoleProperty" />.
        /// </summary>
        /// <param name="target">
        ///     <see cref="DependencyObject" /> defiinig the <see cref="RequiredRoleProperty" />
        /// </param>
        /// <returns>
        ///     Value of type <see cref="UserRoles" /> of the <see cref="RequiredRoleProperty" />
        /// </returns>
        [TypeConverter(typeof(StringUserRoleConverter))]
        public static UserRole GetMinRequiredRole(DependencyObject target)
        {
            return (UserRole)target.GetValue(MinRequiredRoleProperty);
        }

        /// <summary>
        ///     Setter for the <see cref="RequiredRoleProperty" />.
        /// </summary>
        /// <param name="target">
        ///     <see cref="DependencyObject" /> defiinig the <see cref="RequiredRoleProperty" />
        /// </param>
        /// <param name="value">
        ///     The new value of type <see cref="UserRoles" /> for the <see cref="RequiredRoleProperty" />
        /// </param>
        public static void SetMinRequiredRole(DependencyObject target, UserRole value)
        {
            target.SetValue(MinRequiredRoleProperty, value);
        }

        ///// <summary>
        /////     Getter for the <see cref="MaxRequiredRoleProperty" />.
        ///// </summary>
        ///// <param name="target">
        /////     <see cref="DependencyObject" /> defiinig the <see cref="MaxRequiredRoleProperty" />
        ///// </param>
        ///// <returns>
        /////     Value of type <see cref="UserRoles" /> of the <see cref="MaxRequiredRoleProperty" />
        ///// </returns>
        //[TypeConverter(typeof(StringUserRoleConverter))]
        //public static UserRole GetMaxRequiredRole(DependencyObject target)
        //{
        //    return (UserRole)target.GetValue(MaxRequiredRoleProperty);
        //}

        ///// <summary>
        /////     Setter for the <see cref="MaxRequiredRoleProperty" />.
        ///// </summary>
        ///// <param name="target">
        /////     <see cref="DependencyObject" /> defiinig the <see cref="MaxRequiredRoleProperty" />
        ///// </param>
        ///// <param name="value">
        /////     The new value of type <see cref="UserRoles" /> for the <see cref="MaxRequiredRoleProperty" />
        ///// </param>
        //public static void SetMaxRequiredRole(DependencyObject target, UserRole value)
        //{
        //    target.SetValue(MaxRequiredRoleProperty, value);
        //}

        /// <summary>
        ///     Getter for the <see cref="TargetPropertiesProperty" />.
        /// </summary>
        /// <param name="target">
        ///     <see cref="DependencyObject" /> defiinig the <see cref="TargetPropertiesProperty" />
        /// </param>
        /// <returns>
        ///     Value of type <see cref="IEnumerable{T}" /> of <see cref="string" /> of the <see cref="TargetPropertiesProperty" />
        /// </returns>
        [TypeConverter(typeof(StringArrayConverter))]
        public static IEnumerable<string> GetTargetProperties(DependencyObject target)
        {
            return (IEnumerable<string>)target.GetValue(TargetPropertiesProperty);
        }

        /// <summary>
        ///     Setter for the <see cref="TargetPropertiesProperty" />.
        /// </summary>
        /// <param name="target">
        ///     <see cref="DependencyObject" /> defiinig the <see cref="TargetPropertiesProperty" />
        /// </param>
        /// <param name="value">
        ///     The new value of type <see cref="IEnumerable{T}" /> of <see cref="string" /> for the
        ///     <see
        ///         cref="TargetPropertiesProperty" />
        /// </param>
        public static void SetTargetProperties(DependencyObject target, IEnumerable<string> value)
        {
            target.SetValue(TargetPropertiesProperty, value);
        }

        private static BehaviorManager GetBehaviorManager(DependencyObject target)
        {
            return (BehaviorManager)target.GetValue(BehaviorManagerProperty);
        }

        private static void SetBehaviorManager(DependencyObject target, BehaviorManager value)
        {
            target.SetValue(BehaviorManagerProperty, value);
        }

        private static void RequiredRolePropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Authorize(sender);
        }

        private static void TargetPropertiesPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Authorize(sender);
        }

        private static Result Authorize(object target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            using (Source source = SOURCE_FACTORY.CreateSource(RULE_MANAGER.GetAttributes(target)))
            {
                if (source == null)
                {
                    throw new InvalidOperationException("Factory canot return a null source.");
                }

                var targetDependencyOject = target as DependencyObject;

                if (targetDependencyOject != null)
                {
                    if (DesignerProperties.GetIsInDesignMode(targetDependencyOject))
                    {
                        return AuthorizationResult.Allowed;
                    }

                    if (GetBehaviorManager(targetDependencyOject) == null)
                    {
                        SetBehaviorManager(targetDependencyOject, new BehaviorManager(targetDependencyOject));
                    }

                    GetBehaviorManager(targetDependencyOject).SetBehaviors(RULE_MANAGER.GetBehaviors(target), source);
                }
                var result = source.Result;
                return result;
            }
        }
    }
}
