using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace OLS.Casy.Ui.Base.Extensions
{
    public class UpdateOnLostFocus : DependencyObject
    {
        public static readonly DependencyProperty ActiveProperty = DependencyProperty.RegisterAttached(
            "Active",
            typeof(bool),
            typeof(UpdateOnLostFocus),
            new PropertyMetadata(false, ActivePropertyChanged));

        private static void ActivePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox)
            {
                TextBox TextBox = d as TextBox;
                if ((e.NewValue as bool?).GetValueOrDefault(false))
                {
                    TextBox.LostFocus += OnLostFocus;
                }
                else
                {
                    TextBox.LostFocus -= OnLostFocus;
                }
            }
            else if (d is NumericUpDown)
            {
                NumericUpDown numericUpDown = d as NumericUpDown;
                if ((e.NewValue as bool?).GetValueOrDefault(false))
                {
                    numericUpDown.LostFocus += OnLostFocus;
                }
                else
                {
                    numericUpDown.LostFocus -= OnLostFocus;
                }
            }
        }

        private static void OnLostFocus(object sender, RoutedEventArgs e)
        {
            //DependencyObject dependencyObject = GetParentFromVisualTree<OmniTextBox>(e.OriginalSource);

            var TextBox = sender as TextBox;
            if (TextBox != null)
            {
                BindingOperations.GetBindingExpression(TextBox, TextBox.TextProperty).UpdateTarget();
            }

            //dependencyObject = GetParentFromVisualTree<NumericUpDown>(e.OriginalSource);

            var numericUpDown = sender as NumericUpDown;
            if (numericUpDown != null)
            {
                BindingOperations.GetBindingExpression(numericUpDown, NumericUpDown.ValueProperty).UpdateTarget();
            }
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

        [AttachedPropertyBrowsableForChildrenAttribute(IncludeDescendants = false)]
        [AttachedPropertyBrowsableForType(typeof(TextBox))]
        [AttachedPropertyBrowsableForType(typeof(NumericUpDown))]
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
