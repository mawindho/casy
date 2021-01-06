using System;
using System.Collections.Generic;

namespace OLS.Casy.Core
{
    /// <summary>
	///     Compares to istaces of <see cref="WeakReference" /> for equality
	/// </summary>
	public class WeakReferenceEqualityComparer : IEqualityComparer<WeakReference>
    {
        #region IEqualityComparer<WeakReference> Members

        /// <summary>
        ///     Determines whether both instances are equal
        /// </summary>
        /// <returns>
        ///     true, if passed instaces are equal, false oherwise.
        /// </returns>
        /// <param name="x">
        ///     The first instance to compare of type <see cref="WeakReference" />
        /// </param>
        /// <param name="y">
        ///     The second instance to compare of type <see cref="WeakReference" />.
        /// </param>
        public bool Equals(WeakReference x, WeakReference y)
        {
            return (x.IsAlive == y.IsAlive) && (!x.IsAlive || (x.Target == y.Target));
        }

        /// <summary>
        ///     Returns a hashcode for the passed object
        /// </summary>
        /// <returns>
        ///     A hascode for the passed object
        /// </returns>
        /// <param name="obj">
        ///     The <see cref="WeakReference" /> object the hashcode shall be created
        /// </param>
        public int GetHashCode(WeakReference obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            int hash = 23;
            hash = hash * 37 + (obj.IsAlive ? 1 : 0);
            hash = hash * 37 + (obj.IsAlive ? obj.Target.GetHashCode() : 0);
            return hash;
        }

        #endregion
    }
}
