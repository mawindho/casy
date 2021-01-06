using Microsoft.Xaml.Behaviors;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

namespace OLS.Casy.Ui.Base.Behaviors
{
    public class DoubleTouchBehavior : Behavior<UIElement>
    {
        private readonly Stopwatch _doubleTapStopwatch = new Stopwatch();
        private Point? _lastTapLocation;

        public static readonly DependencyProperty DoubleClickCommandProperty =
            DependencyProperty.Register("DoubleClickCommand", typeof(ICommand), typeof(DoubleTouchBehavior), new UIPropertyMetadata(null));

        public ICommand DoubleClickCommand
        {
            get { return (ICommand)GetValue(DoubleClickCommandProperty); }
            set { SetValue(DoubleClickCommandProperty, value); }
        }

        public static readonly DependencyProperty DoubleClickCommandParameterProperty =
            DependencyProperty.Register("DoubleClickCommandParameter", typeof(object), typeof(DoubleTouchBehavior), new UIPropertyMetadata(null));

        public object DoubleClickCommandParameter
        {
            get { return (object)GetValue(DoubleClickCommandParameterProperty); }
            set { SetValue(DoubleClickCommandParameterProperty, value); }
        }

        protected override void OnAttached()
        {
            /*
            this.AssociatedObject.AddHandler(System.Windows.Controls.Control.PreviewMouseDoubleClickEvent, new MouseButtonEventHandler((target, args) =>
            {
                if (this.DoubleClickCommand != null)
                {
                    this.DoubleClickCommand.Execute(DoubleClickCommandParameter);
                }
                args.Handled = true;
            }));
            */

            this.AssociatedObject.AddHandler(UIElement.PreviewTouchDownEvent, new EventHandler<TouchEventArgs>((target, args) =>
            {
                if (IsDoubleTap(args))
                {
                    if (this.DoubleClickCommand != null)
                    {
                        this.DoubleClickCommand.Execute(DoubleClickCommandParameter);
                    }
                    args.Handled = true;
                }
                else
                {
                    args.Handled = false;
                }
            }));
        }

        public static double GetDistanceBetweenPoints(Point p, Point q)
        {
            double a = p.X - q.X;
            double b = p.Y - q.Y;
            double distance = Math.Sqrt(a * a + b * b);
            return distance;
        }
        private bool IsDoubleTap(TouchEventArgs e)
        {
            Point currentTapPosition = e.GetTouchPoint(this.AssociatedObject).Position;
            bool tapsAreCloseInDistance = false;
            if (_lastTapLocation != null)
            {
                tapsAreCloseInDistance = GetDistanceBetweenPoints(currentTapPosition, _lastTapLocation.Value) < 70;
            }
            _lastTapLocation = currentTapPosition;

            TimeSpan elapsed = _doubleTapStopwatch.Elapsed;
            _doubleTapStopwatch.Restart();
            bool tapsAreCloseInTime = (elapsed != TimeSpan.Zero && elapsed < TimeSpan.FromMilliseconds(SystemInformation.DoubleClickTime));

            if (tapsAreCloseInTime && tapsAreCloseInDistance)
            {
                _lastTapLocation = null;
            }
            return tapsAreCloseInDistance && tapsAreCloseInTime;
        }
    }
}
