using System.Collections.Generic;

namespace OLS.Casy.Ui.Base.DyamicUiHelper
{
    /// <summary>
	///     Abstrace base class for any source implementation for dyamic ui szenarios
	/// </summary>
    public abstract class Source : ViewModelBase
    {
        public Source()
            //: base(false)
        {
        }

        private IEnumerable<AttributeBase> _attributes;
        private Result _result;

        /// <summary>
        ///     Property for the <see cref="IEnumerable{T}" /> of <see cref="AttributeBase" /> implementations used and required by the source implementation-
        /// </summary>
        public IEnumerable<AttributeBase> Attributes
        {
            protected get { return this._attributes; }
            set
            {
                this._attributes = value;
                this.Initialize();
            }
        }

        /// <summary>
        ///     Result of the check effacting this source object
        /// </summary>
        public Result Result
        {
            get { return this._result; }
            protected set
            {
                if (this._result != value)
                {
                    this._result = value;
                    this.NotifyOfPropertyChange();
                }
            }
        }

        /// <summary>
        ///     Initializes the source object
        /// </summary>
        protected abstract void Initialize();
    }
}
