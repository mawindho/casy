using Microsoft.Xaml.Behaviors;
using OLS.Casy.Core;
using OLS.Casy.Ui.Api;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace OLS.Casy.Ui.Base.Behaviors
{
    public class OmniDragAndDropManager : Behavior<ListView>
    {
        private FrameworkElement _currentFrameworkElement;
        private IDraggable _currentDraggable;
        private IDroppable _currentDroppable;
        private IDroppable _lastDroppable;
        private readonly DispatcherTimer _touchWaitTimer;
        private Point? _lastDownPosition;

        private Lazy<IDragAndDropService> _dragAndDropService;

        public OmniDragAndDropManager()
        {
            _touchWaitTimer = new DispatcherTimer(new TimeSpan(0, 0, 0, 0, 200),
                                                            DispatcherPriority.Background,
                                                            this.touchWaitTimer_Tick,
                                                            Dispatcher.CurrentDispatcher);
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
            AssociatedObject.PreviewTouchDown += OnTouchDown;
            AssociatedObject.PreviewMouseLeftButtonUp += OnMouseLeftButtonUp;
            AssociatedObject.PreviewTouchUp += OnTouchUp;
            AssociatedObject.PreviewMouseMove += OnMouseMove;
            AssociatedObject.PreviewTouchMove += OnTouchMove;

            _dragAndDropService = GlobalCompositionContainerFactory.GetExport<IDragAndDropService>();
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewMouseLeftButtonDown -= OnMouseLeftButtonDown;
            AssociatedObject.PreviewTouchDown -= OnTouchDown;
            AssociatedObject.PreviewMouseLeftButtonUp -= OnMouseLeftButtonUp;
            AssociatedObject.PreviewTouchUp -= OnTouchUp;
            AssociatedObject.PreviewMouseMove -= OnMouseMove;
            AssociatedObject.PreviewTouchMove -= OnTouchMove;

            base.OnDetaching();
        }

        private void OnTouchDown(object sender, TouchEventArgs e)
        {
            var listView = sender as ListView;
            OnDown(listView, e.GetTouchPoint(listView).Position);

            e.Handled = false;
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var listView = sender as ListView;
            OnDown(listView, e.GetPosition(listView));

            e.Handled = false;
        }

        private void OnTouchUp(object sender, TouchEventArgs e)
        {
            var listView = sender as ListView;
            OnUp(listView, e.GetTouchPoint(listView).Position);

            e.Handled = false;
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var listView = sender as ListView;
            OnUp(listView, e.GetPosition(listView));

            e.Handled = false;
        }

        private void OnTouchMove(object sender, TouchEventArgs e)
        {
            var listView = sender as ListView;
            OnMove(listView, e.GetTouchPoint(listView).Position);

            e.Handled = false;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            var listView = sender as ListView;
            OnMove(listView, e.GetPosition(listView));

            e.Handled = false;
        }

        private void OnDown(ListView listView, Point screenPosition)
        {
            _lastDownPosition = screenPosition;
            VisualTreeHelper.HitTest(listView, HitTestVisibleFilterCallback, OnDownResultCallback,
                                    new PointHitTestParameters(screenPosition));
        }

        private void OnUp(ListView listView, Point screenPosition)
        {
            _lastDownPosition = null;
            _currentFrameworkElement = null;

            if(_dragAndDropService.Value.ActiveDraggable != null)
            {
                _dragAndDropService.Value.ActiveDraggable.IsDragging = false;
            }

            _dragAndDropService.Value.ActiveDraggable = null;
            _touchWaitTimer.Stop();

            if (_currentDraggable == null)
            {
                return;
            }

            VisualTreeHelper.HitTest(listView, HitTestVisibleFilterCallback, OnUpResultCallback,
                                        new PointHitTestParameters(screenPosition));

            if(_currentFrameworkElement != null)
            {
                _currentDroppable?.PerformDrop(_currentDraggable);
            }

            if(_currentDroppable != null)
            {
                _currentDroppable.DraggableOverLocation = 0;
            }
            if (_dragAndDropService.Value.ActiveDraggable != null)
            {
                _dragAndDropService.Value.ActiveDraggable.IsDragging = false;
            }

            _currentDraggable = null;
            _dragAndDropService.Value.ActiveDraggable = null;
            _currentDroppable = null;
            _lastDroppable = null;
         }

        private HitTestFilterBehavior HitTestVisibleFilterCallback(DependencyObject potentialHitTestTarget)
        {
            if (!(potentialHitTestTarget is UIElement uiElement))
            {
                return HitTestFilterBehavior.ContinueSkipSelfAndChildren;
            }

            return uiElement.Visibility != Visibility.Visible ? HitTestFilterBehavior.ContinueSkipSelfAndChildren : HitTestFilterBehavior.Continue;
        }

        private HitTestResultBehavior OnDownResultCallback(HitTestResult result)
        {
            if (result.VisualHit == null) return HitTestResultBehavior.Stop;
            _currentFrameworkElement = result.VisualHit as FrameworkElement;

            _touchWaitTimer.Start();
            return HitTestResultBehavior.Stop;
        }

        private HitTestResultBehavior OnUpResultCallback(HitTestResult result)
        {
            if (result == null) return HitTestResultBehavior.Continue;
            _currentFrameworkElement = result.VisualHit as FrameworkElement;
            return HitTestResultBehavior.Stop;
        }

        private void OnMove(ListView listView, Point screenPosition)
        {
            if (_currentDraggable == null) return;

            var position = System.Windows.Forms.Control.MousePosition;
            _currentDraggable.DragPositionLeft = position.X;
            _currentDraggable.DragPositionTop = position.Y;

            VisualTreeHelper.HitTest(listView, HitTestVisibleFilterCallback, OnMoveResultCallback,
                new PointHitTestParameters(screenPosition));
        }

        private HitTestResultBehavior OnMoveResultCallback(HitTestResult result)
        {
            if (result == null) return HitTestResultBehavior.Continue;
            var currentDepedencyObject = result.VisualHit;

            _currentDroppable = GetTopMostContextElement<IDroppable>(currentDepedencyObject);

            if (_currentDroppable != null)
            {
                var index = _currentDroppable.DisplayOrder;
                var dragItemIndex = _currentDraggable.DisplayOrder;

                var draggableOverLocation = index > dragItemIndex ? -1 : 1;

                if (_lastDroppable == _currentDroppable &&
                    _currentDroppable.DraggableOverLocation == draggableOverLocation) return HitTestResultBehavior.Stop;

                if (_lastDroppable != _currentDroppable && _lastDroppable != null)
                {
                    _lastDroppable.DraggableOverLocation = 0;
                }
                
                _currentDroppable.DraggableOverLocation = draggableOverLocation;

                _lastDroppable = _currentDroppable;
            }
            else
            {
                if (_lastDroppable != null)
                {
                    _lastDroppable.DraggableOverLocation = 0;
                }
            }
            return HitTestResultBehavior.Stop;
        }

        private void touchWaitTimer_Tick(object sender, EventArgs e)
        {
            _touchWaitTimer.Stop();

            if (_currentFrameworkElement != null && _lastDownPosition != null)
            {
                _currentDraggable = GetTopMostContextElement<IDraggable>(_currentFrameworkElement);

                if (_currentDraggable == null) return;

                _currentDraggable.IsDragging = true;
                var position = System.Windows.Forms.Control.MousePosition;
                _currentDraggable.DragPositionLeft = position.X;
                _currentDraggable.DragPositionTop = position.Y;
                _dragAndDropService.Value.ActiveDraggable = _currentDraggable;
            }
            else
            {
                if (_dragAndDropService.Value.ActiveDraggable != null)
                {
                    _dragAndDropService.Value.ActiveDraggable.IsDragging = false;
                }
                _currentDraggable = null;
                _dragAndDropService.Value.ActiveDraggable = null;
            }
        }

        private TContext GetTopMostContextElement<TContext>(DependencyObject element) where TContext : class
        {
            if (element is FrameworkElement frameworkElement && frameworkElement.DataContext != null)
            {
                var context = frameworkElement.DataContext as TContext;

                if (!(context is IDraggable draggable) || _currentDraggable != draggable)
                {
                    return context;
                }
            }

            var parent = GetParent(element);
            return parent != null ? GetTopMostContextElement<TContext>(parent) : null;
        }
    

        private DependencyObject GetParent(DependencyObject element)
        {
            if (!(element is FrameworkElement frameworkElement)) return null;
            if (frameworkElement.Parent != null)
            {
                return frameworkElement.Parent;
            }

            var parent = VisualTreeHelper.GetParent(frameworkElement);

            return parent;
        }
    }
}