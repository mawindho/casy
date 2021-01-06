using System.Collections.Generic;

namespace OLS.Casy.Ui.Base.DyamicUiHelper
{
    /// <summary>
	///     Abstract base implementation of a rule used for dyamic ui szenarios
	/// </summary>
	public abstract class Rule
    {
        /// <summary>
        ///     Returns the <see cref="IEnumerable{T}" /> of <see cref="AttributeBase" /> associated with the rule targeting the passed object.
        /// </summary>
        /// <param name="target">the object the rule is executed on</param>
        /// <returns>
        ///     <see cref="IEnumerable{T}" /> of <see cref="AttributeBase" /> associated with the rule targeting the passed object
        /// </returns>
        public virtual IEnumerable<AttributeBase> GetAttributes(object target)
        {
            return new AttributeBase[0];
        }

        /// <summary>
        ///     Returns the <see cref="IEnumerable{T}" /> of <see cref="BehaviorBase" /> associated with the rule targeting the passed object.
        /// </summary>
        /// <param name="target">the object the rule is executed on</param>
        /// <returns>
        ///     <see cref="IEnumerable{T}" /> of <see cref="BehaviorBase" /> associated with the rule targeting the passed object
        /// </returns>
        public virtual IEnumerable<BehaviorBase> GetBehaviors(object target)
        {
            return new BehaviorBase[0];
        }
    }
}
