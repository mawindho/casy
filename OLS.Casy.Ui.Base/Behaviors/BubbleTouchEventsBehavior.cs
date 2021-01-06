using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace OLS.Casy.Ui.Base.Behaviors
{
    public sealed class BubbleTouchEventsBehavior : Behavior<ScrollViewer>
    {
        public bool DoBubble
        {
            get { return (bool)GetValue(DoBubbleProperty); }
            set { SetValue(DoBubbleProperty, value); }
        }

        public static readonly DependencyProperty DoBubbleProperty =
            DependencyProperty.RegisterAttached("DoBubble", typeof(bool), typeof(BubbleTouchEventsBehavior),
                new FrameworkPropertyMetadata(true));

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.TouchDown += AssociatedObject_OnTouchDown;
            AssociatedObject.TouchUp += AssociatedObject_OnTouchUp; 
        }

        protected override void OnDetaching()
        {
            AssociatedObject.TouchDown -= AssociatedObject_OnTouchDown;
            AssociatedObject.TouchUp -= AssociatedObject_OnTouchUp;
            base.OnDetaching();
        }

        private void AssociatedObject_OnTouchDown(object sender, TouchEventArgs e)
        {
            if (DoBubble)
            {
                FrameworkElement scrollView = sender as FrameworkElement;
                if (scrollView == null)
                    return;
                scrollView.CaptureTouch(e.TouchDevice);
                e.Handled = false;
            }
        }

        private void AssociatedObject_OnTouchUp(object sender, TouchEventArgs e)
        {
            if (DoBubble)
            {
                FrameworkElement scrollView = sender as FrameworkElement;
                if (scrollView == null)
                    return;
                scrollView.ReleaseTouchCapture(e.TouchDevice);
                e.Handled = false;
            }
        }
    }
}
