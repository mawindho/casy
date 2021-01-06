using System;

namespace OLS.Casy.Ui.Api
{
    /// <summary>
	/// Implmentations of this interface ca be show b the child view service out of a view model
	/// </summary>
	public interface IChildView
    {
        /// <summary>
        /// <see cref="Window.DialogResult"/>
        /// </summary>
        bool? DialogResult { get; set; }

        /// <summary>
        /// Custom dialog result for the child view.
        /// The view isn't accessible from ViewModel, so we have to store the value of DialogResult in another location too
        /// </summary>
        bool? ChildViewDialogResult { get; set; }


        /// <summary>
        /// <see cref="FrameworkElement.DataContext"/>
        /// </summary>
        object DataContext { get; set; }

        /// <summary>
        /// <see cref="Window.Closed"/>
        /// </summary>
        event EventHandler Closed;

        /// <summary>
        /// <see cref="Window.Show"/>
        /// </summary>
        void Show();

        /// <summary>
        /// <see cref="Window.ShowDialog"/>
        /// </summary>
        bool? ShowDialog();

        /// <summary>
        /// <see cref="Window.Close"/>
        /// </summary>
        void Close();
    }
}
