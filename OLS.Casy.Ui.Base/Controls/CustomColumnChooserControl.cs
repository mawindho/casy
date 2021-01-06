using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OLS.Casy.Ui.Base.Controls
{
    public class CustomColumnChooserControl : Control
    {
        private TouchDevice currentDevice = null;

        public static readonly DependencyProperty ViewProperty = DependencyProperty.Register("View", typeof(DataViewBase), typeof(CustomColumnChooserControl), new UIPropertyMetadata(null));

        public static readonly DependencyProperty FilterProperty = DependencyProperty.Register("Filter", typeof(ObservableCollection<string>), typeof(CustomColumnChooserControl), new UIPropertyMetadata(null));

        public CustomColumnChooserControl()
        {
            DefaultStyleKey = typeof(CustomColumnChooserControl);
        }

        public DataViewBase View
        {
            get { return (DataViewBase)GetValue(ViewProperty); }
            set { SetValue(ViewProperty, value); }
        }

        public ObservableCollection<string> Filter
        {
            get { return (ObservableCollection<string>)GetValue(FilterProperty); }
            set { SetValue(FilterProperty, value); }
        }

        internal ColumnChooserControl ColunmChooserControl { get; private set; }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            ColunmChooserControl = (ColumnChooserControl)GetTemplateChild("PART_ColumnChooserControl");
        }

        protected override void OnPreviewTouchDown(TouchEventArgs e)
        {
            // Release any previous capture
            ReleaseCurrentDevice();
            // Capture the new touch
            CaptureCurrentDevice(e);
        }

        protected override void OnPreviewTouchUp(TouchEventArgs e)
        {
            ReleaseCurrentDevice();
        }

        protected override void OnLostTouchCapture(TouchEventArgs e)
        {
            // Only re-capture if the reference is not null
            // This way we avoid re-capturing after calling ReleaseCurrentDevice()
            if (currentDevice != null)
            {
                CaptureCurrentDevice(e);
            }
        }

        private void ReleaseCurrentDevice()
        {
            if (currentDevice != null)
            {
                // Set the reference to null so that we don't re-capture in the OnLostTouchCapture() method
                var temp = currentDevice;
                currentDevice = null;
                ReleaseTouchCapture(temp);
            }
        }

        private void CaptureCurrentDevice(TouchEventArgs e)
        {
            bool gotTouch = CaptureTouch(e.TouchDevice);
            if (gotTouch)
            {
                currentDevice = e.TouchDevice;
            }
        }
    }

    public sealed class CustomColumnChooser : IColumnChooser, IColumnChooserFactory
    {
        private readonly CustomColumnChooserControl _columnChooserControl;

        public CustomColumnChooser(CustomColumnChooserControl columnChooserControl)
        {
            this._columnChooserControl = columnChooserControl;
            this._columnChooserControl.TouchDown += _columnChooserControl_TouchDown;
        }

        private void _columnChooserControl_TouchDown(object sender, System.Windows.Input.TouchEventArgs e)
        {
            e.Handled = false;
        }


        #region IColumnChooser Members
        void IColumnChooser.Show() { }
        void IColumnChooser.Hide() { }
        void IColumnChooser.ApplyState(IColumnChooserState state) { }
        void IColumnChooser.SaveState(IColumnChooserState state) { }
        void IColumnChooser.Destroy() { }
        UIElement IColumnChooser.TopContainer { get { return _columnChooserControl.ColunmChooserControl; } }
        #endregion

        #region IColumnChooserFactory Members
        IColumnChooser IColumnChooserFactory.Create(Control owner) { return this; }
        #endregion
    }
}
