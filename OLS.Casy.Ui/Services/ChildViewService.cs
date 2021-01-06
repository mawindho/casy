using OLS.Casy.Core.Api;
using OLS.Casy.Ui.Api;
using System;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Forms;

namespace OLS.Casy.Ui.Services
{
    /// <summary>
	///     Provides a way to show a child view from a view model without knowing the view. This keeps the MVVM pattern.
	/// </summary>
	[PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IChildViewService))]
    public class ChildViewService : IChildViewService
    {
        private readonly ICompositionFactory _compositionFactory;

        [ImportingConstructor]
        public ChildViewService(ICompositionFactory compositionFactory)
        {
            _compositionFactory = compositionFactory;
        }

        #region IChildViewService Members

        /// <summary>
        ///     Sows a child view
        /// </summary>
        /// <param name="childViewIdentifier">Idetifier of the child view to be shown (MEF IChildView Export parameter)</param>
        /// <param name="viewModel">The viewmodel to be used as datacontext of the view</param>
        /// <param name="onDialogClose">Action to be performed when the dialog was closed</param>
        /// <param name="doModal">Flag to indicate whether showing the view modal or not</param>
        /// <param name="windowStyle">WindowStyle</param>
        /// <param name="inputData">Input parameter for the view model.</param>
        /// <typeparam name="TViewModel">OperatorUsage of the view model. Must implement IChildViewOwner</typeparam>
        /// <returns>Custom dialog result for the child view, or null.</returns>
        public bool? ShowChildView<TViewModel>(string childViewIdentifier, TViewModel viewModel = default(TViewModel),
                                                Action<TViewModel> onDialogClose = null, bool doModal = false,
                                                WindowStyle windowStyle = WindowStyle.SingleBorderWindow, Object inputData = null) where TViewModel : IChildViewOwner
        {
            var childView = _compositionFactory.GetComposite<IChildView>(childViewIdentifier);

            if (viewModel != null)
            {
                childView.DataContext = viewModel;
                viewModel.SetInputData(inputData);
            }

            if (onDialogClose != null)
            {
                childView.Closed += (sender, e) => onDialogClose(viewModel);
            }

            if (doModal)
            {
                var window = childView as Window;
                if (window != null)
                {
                    window.Topmost = true;
                    window.ShowInTaskbar = false;
                    window.WindowStyle = WindowStyle.None;

                    if (viewModel != null)
                    {
                        this.CenterDialogOnScreen(window, viewModel.WindowWidth, viewModel.WindowHeight);
                    }
                }

                childView.ShowDialog();
                return childView.ChildViewDialogResult;
            }

            childView.Show();
            return null;
        }

        #endregion

        private void CenterDialogOnScreen(Window childView, double? windowWidth, double? windowHeight)
        {
            if (windowWidth != null && windowHeight != null)
            {
                double screenWidth = SystemParameters.PrimaryScreenWidth;
                double screenHeight = SystemParameters.PrimaryScreenHeight;

                childView.WindowStartupLocation = WindowStartupLocation.Manual;

                //todo konfigurierbar machen? verschieben ins Model?Screen secondaryScreen = Screen.AllScreens.FirstOrDefault(item => !item.Equals(primaryScreen));
                Screen secondaryScreen = Screen.AllScreens.FirstOrDefault(item => !item.Equals(Screen.PrimaryScreen));
                if (secondaryScreen == null)
                {
                    childView.Left = (screenWidth / 2) - ((double)windowWidth / 2);
                    childView.Top = (screenHeight / 2) - ((double)windowHeight / 2);
                }
                else
                {
                    Rectangle secondaryScreenRect = secondaryScreen.WorkingArea;
                    screenWidth = secondaryScreenRect.Width;
                    screenHeight = secondaryScreenRect.Height;

                    // X and Y are holding a negative or psitive screen offset
                    childView.Left = secondaryScreenRect.X + (screenWidth / 2) - ((double)windowWidth / 2);
                    childView.Top = secondaryScreenRect.Y + (screenHeight / 2) - ((double)windowHeight / 2);
                }
            }
        }
    }
}
