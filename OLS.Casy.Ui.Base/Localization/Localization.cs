using OLS.Casy.Core;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Ui.Base.DyamicUiHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

namespace OLS.Casy.Ui.Base.Localization
{
    public static class Localization
    {
        private static Lazy<ILocalizationService> _localizationService;
        private static readonly SourceFactory<LocalizationSource> _sourceFactory = new SourceFactory<LocalizationSource>();
        private static readonly RuleManager _ruleManager = new LocalizationRuleManager();

        public static readonly DependencyProperty LocalizeKeyProperty =
            DependencyProperty.RegisterAttached(
                "LocalizeKey",
                typeof(string),
                typeof(Localization),
                new PropertyMetadata(string.Empty, LocalizeKeyPropertyChanged)
                );

        public static readonly DependencyProperty IsLocalizedProperty =
            DependencyProperty.RegisterAttached(
                "IsLocalized",
                typeof(bool),
                typeof(Localization),
                new PropertyMetadata(false, IsLocalizedPropertyChanged)
                );

        public static readonly DependencyProperty LocalizationParameterProperty =
            DependencyProperty.RegisterAttached(
                "LocalizationParameter",
                typeof(string),
                typeof(Localization),
                new PropertyMetadata(null, LocalizationParameterPropertyChanged)
                );

        public static readonly DependencyProperty TargetPropertyProperty =
            DependencyProperty.RegisterAttached(
                "TargetProperty",
                typeof(string),
                typeof(Localization),
                new PropertyMetadata(string.Empty, TargetPropertyPropertyChanged)
                );

        public static readonly DependencyProperty BehaviorManagerProperty =
            DependencyProperty.RegisterAttached(
                "BehaviorManager",
                typeof(BehaviorManager),
                typeof(Localization),
                new PropertyMetadata(null)
                );

        private static readonly DependencyProperty LocalizationDescriptor =
            DependencyProperty.RegisterAttached("LocalizationDescriptor",
                typeof(DependencyPropertyDescriptor),
                typeof(Localization));

        internal static string GetLocalizeKey(DependencyObject target)
        {
            return target.GetValue(LocalizeKeyProperty) as string;
        }

        internal static void SetLocalizeKey(DependencyObject target, string value)
        {
            target.SetValue(LocalizeKeyProperty, value);
        }

        public static bool GetIsLocalized(DependencyObject target)
        {
            return (bool)target.GetValue(IsLocalizedProperty);
        }

        public static void SetIsLocalized(DependencyObject target, bool value)
        {
            target.SetValue(IsLocalizedProperty, value);
        }

        public static string GetLocalizationParameter(DependencyObject target)
        {
            return (string)target.GetValue(LocalizationParameterProperty);
        }

        public static void SetLocalizationParameter(DependencyObject target, string value)
        {
            target.SetValue(LocalizationParameterProperty, value);
        }

        public static string GetTargetProperty(DependencyObject target)
        {
            return (string)target.GetValue(TargetPropertyProperty);
        }

        public static void SetTargetProperty(DependencyObject target, IEnumerable<string> value)
        {
            target.SetValue(TargetPropertyProperty, value);
        }

        private static BehaviorManager GetBehaviorManager(DependencyObject target)
        {
            return (BehaviorManager)target.GetValue(BehaviorManagerProperty);
        }

        private static void SetBehaviorManager(DependencyObject target, BehaviorManager value)
        {
            target.SetValue(BehaviorManagerProperty, value);
        }

        internal static ILocalizationService LocalizationService
        {
            get
            {
                if (_localizationService == null)
                {
                    _localizationService = GlobalCompositionContainerFactory.GetExport<ILocalizationService>();
                }
                return _localizationService.Value;
            }
        }

        private static void LocalizeKeyPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Localize(sender);
        }

        private static void TargetPropertyPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Localize(sender);
        }

        private static void IsLocalizedPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Localize(sender);
        }

        private static void LocalizationParameterPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Localize(sender);
        }

        private static Result Localize(object target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            var targetDependencyOject = target as DependencyObject;

            //if (!BindingOperations.IsDataBound(targetDependencyOject, IsLocalizedProperty))
            //{
            //    // We are no longer data-bound, so we need to remove the ValueChanged 
            //    //    event listener to avoid a memory leak 
            //    var dpd = (DependencyPropertyDescriptor)targetDependencyOject.GetValue(LocalizationDescriptor);
            //    if (dpd != null)
            //    {
            //        var bm = GetBehaviorManager(targetDependencyOject);
            //        if(bm != null)
            //        {
            //            bm.SetBehaviors(null, null);
            //        }
            //    }
            //    return null;
            //}

            var attributes = _ruleManager.GetAttributes(target);

            if (attributes != null)
            {
                Source source = _sourceFactory.CreateSource(attributes);

                if (source == null)
                {
                    throw new InvalidOperationException("Factory cannot return a null source.");
                }

                if (targetDependencyOject != null)
                {
                    if (GetBehaviorManager(targetDependencyOject) == null)
                    {
                        SetBehaviorManager(targetDependencyOject, new BehaviorManager(targetDependencyOject));
                    }

                    GetBehaviorManager(targetDependencyOject)
                        .SetBehaviors(_ruleManager.GetBehaviors(target), source);
                }

                return source.Result;
            }
            return null;
        }
    }
}
