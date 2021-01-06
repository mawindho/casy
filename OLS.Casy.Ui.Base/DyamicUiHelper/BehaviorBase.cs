namespace OLS.Casy.Ui.Base.DyamicUiHelper
{
    /// <summary>
	///     Abstract base class for behaviors
	/// </summary>
	public abstract class BehaviorBase
    {
        /// <summary>
        ///     Adds the behavior to a target with an dedicatd source
        /// </summary>
        /// <param name="target">The target object of for the behavir</param>
        /// <param name="source">The source o the behavior</param>
        public abstract void AddBehavior(object target, Source source);

        /// <summary>
        ///     Removes the behavior from a target
        /// </summary>
        /// <param name="target">The target object of for the behavir</param>
        public abstract void RemoveBehavior(object target);
    }
}
