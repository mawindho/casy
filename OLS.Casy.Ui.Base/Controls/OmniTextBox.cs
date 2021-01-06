using System;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace OLS.Casy.Ui.Base.Controls
{
    /// <summary>
	///		Custom <see cref="TextBox" /> control offering the ability to directly trigger a property changed event
	///		after each key pressed.
	/// </summary>
	public class OmniTextBox : TextBox
    {
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            if (!this.IsReadOnly)
            {
                return new FrameworkElementAutomationPeer(this);
            }
            return base.OnCreateAutomationPeer();
        }
        /*
        private DispatcherTimer _delayTimer;

        /// <summary>
        ///		Flag to enable the direct update source forcing a property changed evet after eac pressed key
        /// </summary>
        public static readonly DependencyProperty DirectlyUpdateSourceProperty =
            DependencyProperty.Register(
                "DirectlyUpdateSource",
                typeof(bool),
                typeof(OmniOmniTextBox),
                new PropertyMetadata(false)
                );

        /// <summary>
        ///     Getter used by XAMl code.
        /// </summary>
        public static bool GetDirectlyUpdateSource(DependencyObject target)
        {
            return (bool)target.GetValue(DirectlyUpdateSourceProperty);
        }

        /// <summary>
        ///     Setter used by XAMl code.
        /// </summary>
        public static void SetDirectlyUpdateSource(DependencyObject target, bool value)
        {
            target.SetValue(DirectlyUpdateSourceProperty, value);
        }

        /// <summary>
        /// Overrides the <see cref="UIElement.KeyUp"/> event of the control.
        /// If the directly update source featre is eabled, the binding of the text property will be cecked every key press.
        /// </summary>
        protected override void OnKeyUp(KeyEventArgs e)
        {
            if ((bool)this.GetValue(DirectlyUpdateSourceProperty))
            {
                if (_delayTimer == null)
                {
                    _delayTimer = new DispatcherTimer();
                    _delayTimer.Interval = TimeSpan.FromMilliseconds(200);
                    _delayTimer.Tick += (sender, args) =>
                    {
                        var binding = this.GetBindingExpression(TextProperty);
                        if (binding != null)
                        {
                            binding.UpdateSource();
                        }
                        base.OnKeyUp(e);
                        this.Focus();
                        _delayTimer.Stop();
                    };
                    _delayTimer.Start();
                }
                _delayTimer.Stop();
                _delayTimer.Start();
            }
        }

        /// <summary>
        /// Overrides the <see cref="UIElement.GotMouseCapture"/> event of the control.
        /// </summary>
        protected override void OnGotMouseCapture(MouseEventArgs e)
        {
            this.SelectAll();

            base.OnGotMouseCapture(e);
        }
        */
    }
}
