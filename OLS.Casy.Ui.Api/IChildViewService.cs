using System;
using System.Windows;

namespace OLS.Casy.Ui.Api
{
    /// <summary>
	/// Provides a way to show a child view from a view model without knowing the view. This keeps the MVVM pattern.
	/// </summary>
	public interface IChildViewService
    {
        /// <summary>
        /// Sows a child view
        /// </summary>
        /// <param name="childViewIdentifier">Idetifier of the child view to be shown (MEF IChildView Export parameter)</param>
        /// <param name="viewModel">The viewmodel to be used as datacontext of the view</param>
        /// <param name="onDialogClose">Action to be performed when the dialog was closed</param>
        /// <param name="doModal">Flag to indicate whether showing the view modal or not</param>
        /// <param name="windowStyle">WindowStyle</param>
        /// <param name="inputData">Input parameter for the view model.</param>
        /// <typeparam name="TViewModel">OperatorUsage of the view model. Must implement IChildViewOwner</typeparam>
        /// <returns></returns>
        bool? ShowChildView<TViewModel>(string childViewIdentifier, TViewModel viewModel, Action<TViewModel> onDialogClose = null,
                                        bool doModal = false, WindowStyle windowStyle = WindowStyle.SingleBorderWindow, object inputData = null) where TViewModel : IChildViewOwner;
    }
}
