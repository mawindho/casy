using System;
using System.Collections.Generic;
using System.Windows;

namespace OLS.Casy.Ui.Base.DyamicUiHelper
{
    /// <summary>
	///     Object to manage available bahaviors. Must be used by each concrete dynamic UI behaviors.
	/// </summary>
	public class BehaviorManager
    {
        private readonly DependencyObject _target;

        private IEnumerable<BehaviorBase> _behaviors;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="target">Depeendecy object the manager is associated to</param>
        public BehaviorManager(DependencyObject target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }
            this._target = target;
        }

        /// <summary>
        ///     Property for all behavior bases associated with the manager
        /// </summary>
        public IEnumerable<BehaviorBase> Behaviors
        {
            get { return this._behaviors; }
        }

        /// <summary>
        ///     Sets the behavior of the rule behavior
        /// </summary>
        /// <param name="behaviors">The behavior the target sall be assiciated with</param>
        /// <param name="source">The source for the behavior</param>
        public void SetBehaviors(IEnumerable<BehaviorBase> behaviors, Source source)
        {
            if (this._behaviors != behaviors)
            {
                if (this._behaviors != null)
                {
                    foreach (BehaviorBase behavior in this._behaviors)
                    {
                        behavior.RemoveBehavior(this._target);
                    }
                }

                this._behaviors = behaviors;

                if (this._behaviors != null)
                {
                    foreach (BehaviorBase behavior in this._behaviors)
                    {
                        behavior.AddBehavior(this._target, source);
                    }
                }
            }
        }
    }
}
