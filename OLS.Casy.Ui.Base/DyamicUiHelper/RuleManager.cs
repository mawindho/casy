using OLS.Casy.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OLS.Casy.Ui.Base.DyamicUiHelper
{
    /// <summary>
	///     Abstract base class for a rule manager required by dynamic ui concept implementations
	/// </summary>
	public abstract class RuleManager
    {
        private readonly IDictionary<WeakReference, IList<Rule>> _rules =
            new Dictionary<WeakReference, IList<Rule>>(new WeakReferenceEqualityComparer());

        private readonly object _syncLock = new object();

        private readonly bool _useRollingInitialization;

        /// <summary>
        ///     Constructor
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        protected RuleManager()
        {
            this._useRollingInitialization = false;

            this.DefineDefaultRoles();

            this._useRollingInitialization = true;
        }

        /// <summary>
        ///     Defines default rules managed by the rule manager.
        /// </summary>
        protected abstract void DefineDefaultRoles();

        /// <summary>
        ///     Returns a <see cref="IEnumerable{T}" /> of <see cref="AttributeBase" /> objects associated with the passed object.
        /// </summary>
        /// <param name="target">
        ///     The object its associated <see cref="AttributeBase" /> objects are requested for
        /// </param>
        /// <returns>
        ///     <see cref="IEnumerable{T}" /> of <see cref="AttributeBase" /> objects associated with the passed object
        /// </returns>
        public IEnumerable<AttributeBase> GetAttributes(object target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            var attributes = new List<AttributeBase>();
            foreach (Rule rule in this.GetRules(target))
            {
                attributes.AddRange(rule.GetAttributes(target));
            }

            return attributes;
        }

        /// <summary>
        ///     Returns a <see cref="IEnumerable{T}" /> of <see cref="BehaviorBase" /> objects associated with the passed object.
        /// </summary>
        /// <param name="target">
        ///     The object its associated <see cref="BehaviorBase" /> objects are requested for
        /// </param>
        /// <returns>
        ///     <see cref="IEnumerable{T}" /> of <see cref="BehaviorBase" /> objects associated with the passed object
        /// </returns>
        public IEnumerable<BehaviorBase> GetBehaviors(object target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            var behaviors = new List<BehaviorBase>();
            foreach (Rule rule in this.GetRules(target))
            {
                behaviors.AddRange(rule.GetBehaviors(target));
            }

            return behaviors;
        }

        /// <summary>
        ///     Returns a <see cref="IEnumerable{T}" /> of <see cref="Rule" /> objects associated with the passed object.
        /// </summary>
        /// <param name="target">
        ///     The object its associated <see cref="Rule" /> objects are requested for
        /// </param>
        /// <returns>
        ///     <see cref="IEnumerable{T}" /> of <see cref="Rule" /> objects associated with the passed object
        /// </returns>
        public IEnumerable<Rule> GetRules(object target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }
            return this.GetRulesInteral(target).ToArray();
        }

        /// <summary>
        ///     associates a <see cref="Rule" /> object wit the passed object.
        /// </summary>
        /// <param name="target">
        ///     The object the passed <see cref="Rule" /> shall be associated with
        /// </param>
        /// <param name="rule">
        ///     The <see cref="Rule" /> to be associated
        /// </param>
        public void AddRule(object target, Rule rule)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }
            if (rule == null)
            {
                throw new ArgumentNullException("rule");
            }
            this.GetRulesInteral(target).Add(rule);
        }

        /// <summary>
        ///     Removes the association of a <see cref="Rule" /> object wit the passed object.
        /// </summary>
        /// <param name="target">
        ///     The object the association with the passed <see cref="Rule" /> shall be removed
        /// </param>
        /// <param name="rule">
        ///     The <see cref="Rule" /> to be associated
        /// </param>
        public void RemoveRule(object target, Rule rule)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }
            if (rule == null)
            {
                throw new ArgumentNullException("rule");
            }
            this.GetRulesInteral(target).Remove(rule);
        }

        /// <summary>
        ///     Removes all association to <see cref="Rule" /> object of the passed object.
        /// </summary>
        /// <param name="target">
        ///     The object all <see cref="Rule" /> associations shall be removed
        /// </param>
        public void ClearRules(object target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }
            this.GetRulesInteral(target).Clear();
        }

        /// <summary>
        ///     Purges orphand rules.
        /// </summary>
        public void PurgeOrphanedRules()
        {
            foreach (WeakReference reference in this._rules.Keys.ToArray())
            {
                if (!reference.IsAlive)
                {
                    this._rules.Remove(reference);
                }
            }
        }

        private IList<Rule> GetRulesInteral(object target)
        {
            var reference = new WeakReference(target);
            if (!this._rules.ContainsKey(reference))
            {
                var rules = new List<Rule>();
                this.InitializeRules(target, rules);

                lock (_syncLock)
                {
                    if (!this._rules.ContainsKey(reference))
                    {
                        this._rules[reference] = rules;
                    }
                }
            }
            return this._rules[reference];
        }

        private void InitializeRules(object target, List<Rule> rules)
        {
            if (!this._useRollingInitialization)
            {
                return;
            }

            Type targetType = target as Type;

            var types = new List<Type>();
            if (targetType == null)
            {
                types.Add(target.GetType());
            }
            else
            { 
                if (targetType.BaseType == null)
                {
                    return;
                }

                types.Add(targetType.BaseType);
                types.AddRange(targetType.GetInterfaces());
            }

            foreach (var typeRules in types.Select(this.GetRules))
            {
                rules.AddRange(typeRules);
            }
        }
    }
}
