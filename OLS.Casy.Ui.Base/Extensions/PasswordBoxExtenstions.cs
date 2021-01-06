using System.Windows;
using System.Windows.Controls;

namespace OLS.Casy.Ui.Base.Extensions
{
    /// <summary>
	///     Helper to update properties of the class System.Windows.Controls.PasswordBox.
	/// </summary>
	public static class PasswordBoxExtenstions
    {
        /// <summary>
        ///     Definition of dependency property 'Password'.
        /// </summary>
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.RegisterAttached("Password",
                                                typeof(string), typeof(PasswordBoxExtenstions),
                                                new FrameworkPropertyMetadata(string.Empty, OnPasswordPropertyChanged));

        /// <summary>
        ///     Definition of dependency property 'Attach'.
        /// </summary>
        public static readonly DependencyProperty AttachProperty =
            DependencyProperty.RegisterAttached("Attach",
                                                typeof(bool), typeof(PasswordBoxExtenstions), new PropertyMetadata(false, Attach));

        private static readonly DependencyProperty IsUpdatingProperty =
            DependencyProperty.RegisterAttached("IsUpdating", typeof(bool),
                                                typeof(PasswordBoxExtenstions));

        /// <summary>
        ///     Definition of dependency property 'PasswordChar'.
        /// </summary>
        public static readonly DependencyProperty PasswordChar =
            DependencyProperty.RegisterAttached("PasswordChar", typeof(char),
                                                typeof(PasswordBoxExtenstions));

        /// <summary>
        ///     Setter for property 'Attach'.
        /// </summary>
        public static void SetAttach(DependencyObject dp, bool value)
        {
            dp.SetValue(AttachProperty, value);
        }

        /// <summary>
        ///     Getter for property 'Attach'.
        /// </summary>
        public static bool GetAttach(DependencyObject dp)
        {
            return (bool)dp.GetValue(AttachProperty);
        }

        /// <summary>
        ///     Getter for property 'Password'.
        /// </summary>
        public static string GetPassword(DependencyObject dp)
        {
            return (string)dp.GetValue(PasswordProperty);
        }

        /// <summary>
        ///     Setter for property 'Password'.
        /// </summary>
        public static void SetPassword(DependencyObject dp, string value)
        {
            dp.SetValue(PasswordProperty, value);
        }

        private static bool GetIsUpdating(DependencyObject dp)
        {
            return (bool)dp.GetValue(IsUpdatingProperty);
        }

        private static void SetIsUpdating(DependencyObject dp, bool value)
        {
            dp.SetValue(IsUpdatingProperty, value);
        }

        /// <summary>
        ///     Getter for property 'PasswordChar'.
        /// </summary>
        public static char GetPasswordChar(DependencyObject dp)
        {
            return (char)dp.GetValue(PasswordChar);
        }

        /// <summary>
        ///     Setter for property 'PasswordChar'.
        /// </summary>
        public static void SetPasswordChar(DependencyObject dp, char value)
        {
            dp.SetValue(PasswordChar, value);
        }

        private static void OnPasswordPropertyChanged(DependencyObject sender,
                                                    DependencyPropertyChangedEventArgs e)
        {
            var passwordBox = sender as PasswordBox;
            passwordBox.PasswordChanged -= PasswordChanged;

            if (!GetIsUpdating(passwordBox))
            {
                passwordBox.Password = (string)e.NewValue;
            }

            passwordBox.PasswordChanged += PasswordChanged;
        }

        private static void Attach(DependencyObject sender,
                                    DependencyPropertyChangedEventArgs e)
        {
            var passwordBox = sender as PasswordBox;

            if (passwordBox == null)
            {
                return;
            }

            if ((bool)e.OldValue)
            {
                passwordBox.PasswordChanged -= PasswordChanged;
            }

            if ((bool)e.NewValue)
            {
                passwordBox.PasswordChanged += PasswordChanged;
            }
        }

        private static void PasswordChanged(object sender, RoutedEventArgs e)
        {
            var passwordBox = sender as PasswordBox;
            SetIsUpdating(passwordBox, true);
            SetPassword(passwordBox, passwordBox.Password);
            SetIsUpdating(passwordBox, false);
        }
    }
}
