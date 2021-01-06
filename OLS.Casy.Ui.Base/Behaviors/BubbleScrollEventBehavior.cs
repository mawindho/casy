using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace OLS.Casy.Ui.Base.Behaviors
{
    public sealed class BubbleScrollEventBehavior : Behavior<UIElement>
    {
        public bool DoBubble
        {
            get { return (bool)GetValue(DoBubbleProperty); }
            set { SetValue(DoBubbleProperty, value); }
        }

        public static readonly DependencyProperty DoBubbleProperty =
            DependencyProperty.RegisterAttached("DoBubble", typeof(bool), typeof(BubbleScrollEventBehavior),
                new FrameworkPropertyMetadata(true));

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewMouseWheel += AssociatedObject_PreviewMouseWheel;
        }

        //private void AssociatedObject_TouchUp(object sender, TouchEventArgs e)
        //{
            //throw new NotImplementedException();
        //}

        //private void AssociatedObject_TouchDown(object sender, TouchEventArgs e)
        //{
            //FrameworkElement scrollView = sender as FrameworkElement;
            //if (scrollView == null)
                //return;
            //scrollView.CaptureTouch(e.TouchDevice);
            //e.Handled = true;
        //}

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewMouseWheel -= AssociatedObject_PreviewMouseWheel;
            base.OnDetaching();
        }

        void AssociatedObject_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if(DoBubble)
            { 
                e.Handled = true;
                var e2 = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                e2.RoutedEvent = UIElement.MouseWheelEvent;
                AssociatedObject.RaiseEvent(e2);
            }
        }
    }
}
