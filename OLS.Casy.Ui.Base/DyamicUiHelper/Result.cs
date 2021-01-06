namespace OLS.Casy.Ui.Base.DyamicUiHelper
{
    /// <summary>
	///     Base implementation representing the result of an dyamic ui szenario. (E.g. does a rule match)
	/// </summary>
	public class Result
    {
        private readonly string _errorMessage;

        /// <summary>
        ///     Constructor
        /// </summary>
        public Result()
        {
        }

        /// <summary>
        ///     Constructor accepting an error message
        /// </summary>
        /// <param name="errorMessage"></param>
        public Result(string errorMessage)
        {
            this._errorMessage = errorMessage;
        }

        /// <summary>
        ///     Property for an error message
        /// </summary>
        public string ErrorMessage
        {
            get { return this._errorMessage; }
        }
    }
}
