using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace OLS.Casy.Ui.Base.Extensions
{
    public static class ScrollViewerExtensions
    {
        public static bool GetScrollToBottom(DependencyObject obj)
        {
            return (bool)obj.GetValue(ScrollToBottomProperty);
        }

        public static void SetScrollToBottom(DependencyObject obj, bool value)
        {
            obj.SetValue(ScrollToBottomProperty, value);
        }

        public static readonly DependencyProperty ScrollToBottomProperty =
            DependencyProperty.RegisterAttached("ScrollToBottom", typeof(bool), typeof(ScrollViewerExtensions), new PropertyMetadata(false, ScrollToBottomPropertyChanged));

        public static bool GetScrollToTop(DependencyObject obj)
        {
            return (bool)obj.GetValue(ScrollToTopProperty);
        }

        public static void SetScrollToTop(DependencyObject obj, bool value)
        {
            obj.SetValue(ScrollToTopProperty, value);
        }

        public static readonly DependencyProperty ScrollToTopProperty =
            DependencyProperty.RegisterAttached("ScrollToTop", typeof(bool), typeof(ScrollViewerExtensions), new PropertyMetadata(false, ScrollToTopPropertyChanged));

        public static void ScrollToBottomPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var scrollViewer = obj as ScrollViewer;

            if (scrollViewer != null && (bool)e.NewValue)
            {
                scrollViewer.ScrollToBottom();
                SetScrollToBottom(obj, false);
            }
        }

        public static void ScrollToTopPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var scrollViewer = obj as ScrollViewer;

            if (scrollViewer != null && (bool)e.NewValue)
            {
                scrollViewer.ScrollToTop();
                SetScrollToTop(obj, false);
            }
            
        }
    }
}
