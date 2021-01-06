using System;

namespace OLS.Casy.Ui.Api
{
    /// <summary>
	///     A view model that will be data context of a child view must implement this interface
	/// </summary>
	public interface IChildViewOwner
    {
        /// <summary>
        ///     Width of the child view
        /// </summary>
        double? WindowWidth { get; }

        /// <summary>
        ///     Height of the child view
        /// </summary>
        double? WindowHeight { get; }

        /// <summary>
        ///     Inout data to be processed by the view model.
        /// </summary>
        void SetInputData(object date);

        /// <summary>
        ///     Event can be raised to force the closing of a child view from within the view model
        /// </summary>
        event EventHandler<DialogResultEventArgs> ForceCloseChildWindow;
    }
}
