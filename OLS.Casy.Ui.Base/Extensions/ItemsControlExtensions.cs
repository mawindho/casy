using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OLS.Casy.Ui.Base.Extensions
{
    public static class ItemsControlExtensions
    {
        public static object GetScrollIntoView(DependencyObject obj)
        {
            return (object)obj.GetValue(ScrollIntoViewProperty);
        }

        public static void SetScrollIntoView(DependencyObject obj, object value)
        {
            obj.SetValue(ScrollIntoViewProperty, value);
        }

        public static readonly DependencyProperty ScrollIntoViewProperty =
            DependencyProperty.RegisterAttached("ScrollIntoView", typeof(object), typeof(ItemsControlExtensions), new PropertyMetadata(null, ScrollIntoViewChanged));

        public static readonly DependencyProperty ScrollToTopProperty =
            DependencyProperty.RegisterAttached("ScrollToTop", typeof(bool), typeof(ItemsControlExtensions), new PropertyMetadata(false, ScrollToTopChanged));

        private static void ScrollToTopChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var itemsControl = obj as ItemsControl;

            if (itemsControl != null && e.NewValue != null)
            {
                var parent = GetParent(itemsControl);
                while (parent != null && !(parent is ScrollViewer))
                {
                    parent = GetParent(parent);
                }

                if (parent != null)
                {
                    ((ScrollViewer)parent).ScrollToVerticalOffset(0);
                }
            }
        }

        private static DependencyObject GetParent(DependencyObject element)
        {
            if (!(element is FrameworkElement frameworkElement)) return null;
            if (frameworkElement.Parent != null)
            {
                return frameworkElement.Parent;
            }

            var parent = VisualTreeHelper.GetParent(frameworkElement);

            return parent;
        }

        public static object GetScrollToTop(DependencyObject obj)
        {
            return (bool)obj.GetValue(ScrollIntoViewProperty);
        }

        public static void SetScrollToTop(DependencyObject obj, object value)
        {
            obj.SetValue(ScrollIntoViewProperty, (bool) value);
        }

        

        public static void ScrollIntoView(ItemsControl control, object item)
        {
            FrameworkElement framework = control.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
            if (framework == null) { return; }
            framework.BringIntoView();
        }

        public static void ScrollIntoView(ItemsControl control)
        {
            int count = control.Items.Count;
            if (count == 0) { return; }
            object item = control.Items[count - 1];
            ScrollIntoView(control, item);
        }

        public static void ScrollIntoViewChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var itemsControl = obj as ItemsControl;

            if (itemsControl != null && e.NewValue != null)
            {
                ScrollIntoView(itemsControl, e.NewValue);
            }
        }
    }
}
