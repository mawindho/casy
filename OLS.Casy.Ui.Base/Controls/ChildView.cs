using OLS.Casy.Ui.Api;
using System.Windows;
using System.Windows.Input;

namespace OLS.Casy.Ui.Base.Controls
{
    /// <summary>
	///     Soecialized <see cref="Window" /> implementation used for Child Views in te Neis platform
	/// </summary>
	public class ChildView : Window, IChildView
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        protected ChildView()
        {
            this.Loaded += this.ViewLoaded;
            this.MouseLeftButtonDown += this.ChildViewMouseLeftButtonDown;
        }

        #region IChildView Members

        /// <summary>
        ///     Custom dialog result for the child view.
        ///     The view isn't accessible from ViewModel, so we have to store the value of DialogResult in another location too
        /// </summary>
        public bool? ChildViewDialogResult { get; set; }

        #endregion

        private void ChildViewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void ViewLoaded(object sender, RoutedEventArgs e)
        {
            var childViewOwnerViewModel = this.DataContext as IChildViewOwner;

            if (childViewOwnerViewModel != null)
            {
                childViewOwnerViewModel.ForceCloseChildWindow += this.DoForceCloseChildView;

                if (childViewOwnerViewModel.WindowHeight.HasValue)
                {
                    this.Height = childViewOwnerViewModel.WindowHeight.Value;
                }

                if (childViewOwnerViewModel.WindowWidth.HasValue)
                {
                    this.Width = childViewOwnerViewModel.WindowWidth.Value;
                }
            }
        }

        private void DoForceCloseChildView(object sender, DialogResultEventArgs e)
        {
            if (null != e)
            {
                this.ChildViewDialogResult = e.DialogResult;
            }
            this.Close();
        }
    }
}
