using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OLS.Casy.Ui.Base.Extensions
{
    /// <summary>
	///     This class allows to set the keayboard focus to a WPF element.
	/// </summary>
	public static class KeyboardFocusExtension
    {
        private static readonly DependencyProperty ON_PROPERTY;

        /// <summary>
        ///     Class constructor.
        /// </summary>
        static KeyboardFocusExtension()
        {
            ON_PROPERTY = DependencyProperty.RegisterAttached("On", typeof(FrameworkElement), typeof(KeyboardFocusExtension),
                                                            new PropertyMetadata(OnSetCallback));
        }

        /// <summary>
        ///     Setter for focus.
        /// </summary>
        /// <param name="element">UI element.</param>
        /// <param name="value">Framework element.</param>
        public static void SetOn(UIElement element, FrameworkElement value)
        {
            element.SetValue(ON_PROPERTY, value);
        }

        /// <summary>
        ///     Getter for focus.
        /// </summary>
        /// <param name="element">UI element.</param>
        public static FrameworkElement GetOn(UIElement element)
        {
            return (FrameworkElement)element.GetValue(ON_PROPERTY);
        }

        private static void OnSetCallback(DependencyObject dependencyObject,
                                        DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var frameworkElement = (FrameworkElement)dependencyObject;
            FrameworkElement target = GetOn(frameworkElement);

            if (target == null)
            {
                return;
            }

            frameworkElement.Loaded += (s, e) =>
            {
                var TextBox = target as TextBox;
                if (TextBox != null)
                {
                    //if (OmniTextBox.IsFocused) return;
                    TextBox.SelectAll();
                    TextBox.Focus();
                }

                var comboBox = target as ComboBox;
                if (comboBox != null)
                {
                    comboBox.Focus();
                }
                Keyboard.Focus(target);
            };
        }
    }
}
