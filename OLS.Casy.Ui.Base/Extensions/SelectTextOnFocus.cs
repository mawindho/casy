using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OLS.Casy.Ui.Base.Extensions
{
    public class SelectTextOnFocus : DependencyObject
    {
        public static readonly DependencyProperty ActiveProperty = DependencyProperty.RegisterAttached(
            "Active",
            typeof(bool),
            typeof(SelectTextOnFocus),
            new PropertyMetadata(false, ActivePropertyChanged));

        private static void ActivePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox)
            {
                TextBox TextBox = d as TextBox;
                if ((e.NewValue as bool?).GetValueOrDefault(false))
                {
                    TextBox.GotKeyboardFocus += OnKeyboardFocusSelectText;
                    TextBox.PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
                }
                else
                {
                    TextBox.GotKeyboardFocus -= OnKeyboardFocusSelectText;
                    TextBox.PreviewMouseLeftButtonDown -= OnMouseLeftButtonDown;
                }
            }
            else if (d is NumericUpDown)
            {
                NumericUpDown numericUpDown = d as NumericUpDown;
                if((e.NewValue as bool?).GetValueOrDefault(false))
                {
                    numericUpDown.GotKeyboardFocus += OnKeyboardFocusSelectText;
                    numericUpDown.PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
                }
                else
                {
                    numericUpDown.GotKeyboardFocus -= OnKeyboardFocusSelectText;
                    numericUpDown.PreviewMouseLeftButtonDown -= OnMouseLeftButtonDown;
                }
            }
        }

        private static void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DependencyObject dependencyObject = GetParentFromVisualTree<TextBox>(e.OriginalSource);

            //if (dependencyObject == null)
            //{
            //    return;
            //}
            var TextBox = dependencyObject as TextBox;
            if (TextBox != null && !TextBox.IsKeyboardFocusWithin)
            {
                TextBox.Focus();
                e.Handled = true;
            }

            //dependencyObject = GetParentFromVisualTree<NumericUpDown>(e.OriginalSource);

//            var numericUpDown = dependencyObject as NumericUpDown;
  //          if(numericUpDown != null && !numericUpDown.IsKeyboardFocusWithin)
    //        {
      //          numericUpDown.Focus();
        //        e.Handled = true;
          //  }
        }

        private static DependencyObject GetParentFromVisualTree<T>(object source) where T : class
        {
            DependencyObject parent = source as UIElement;
            while (parent != null && !(parent is T))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            return parent;
        }

        private static void OnKeyboardFocusSelectText(object sender, KeyboardFocusChangedEventArgs e)
        {
            TextBox OmniTextBox = e.OriginalSource as TextBox;
            if (OmniTextBox != null)
            {
                OmniTextBox.SelectAll();
            }

            NumericUpDown numericUpDown = e.OriginalSource as NumericUpDown;
            if (numericUpDown != null)
            {
                numericUpDown.SelectAll();
            }
        }

        [AttachedPropertyBrowsableForChildrenAttribute(IncludeDescendants = false)]
        [AttachedPropertyBrowsableForType(typeof(TextBox))]
        public static bool GetActive(DependencyObject @object)
        {
            return (bool)@object.GetValue(ActiveProperty);
        }

        public static void SetActive(DependencyObject @object, bool value)
        {
            @object.SetValue(ActiveProperty, value);
        }
    }

}
