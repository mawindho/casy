using Microsoft.Xaml.Behaviors;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace OLS.Casy.Ui.Base.Behaviors
{
    public class DropDownButtonBehavior : Behavior<Button>
    {
        private long _attachedCount;
        private bool _isContextMenuOpen;

        public bool IsOpen
        {
            get { return (bool)GetValue(IsOpenProperty); }
            set { SetValue(IsOpenProperty, value); }
        }

        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.RegisterAttached("IsOpen", typeof(bool), typeof(DropDownButtonBehavior),
                new FrameworkPropertyMetadata(false, OnIsOpenChanged));

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(AssociatedObject_Click), true);
        }

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (d as DropDownButtonBehavior);
            if ((bool)e.NewValue)
            {
                behavior.AssociatedObject_Click((d as DropDownButtonBehavior).AssociatedObject, null);
                behavior.IsOpen = false;
            }
        }

        private void AssociatedObject_Click(object sender, RoutedEventArgs e)
        {
            Button source = sender as Button;

            if (source != null && source.ContextMenu != null)
            {
                // Only open the ContextMenu when it is not already open. If it is already open,
                // when the button is pressed the ContextMenu will lose focus and automatically close.
                if (!_isContextMenuOpen)
                {
                    source.ContextMenu.AddHandler(ContextMenu.ClosedEvent, new RoutedEventHandler(ContextMenu_Closed), true);
                    Interlocked.Increment(ref _attachedCount);
                    // If there is a drop-down assigned to this button, then position and display it 
                    source.ContextMenu.PlacementTarget = source;
                    source.ContextMenu.Placement = PlacementMode.Bottom;
                    source.ContextMenu.IsOpen = true;
                    this.IsOpen = true;
                    _isContextMenuOpen = true;
                }
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.RemoveHandler(ButtonBase.ClickEvent, new RoutedEventHandler(AssociatedObject_Click));
        }

        private void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            _isContextMenuOpen = false;

            var contextMenu = sender as ContextMenu;
            if (contextMenu != null)
            {
                contextMenu.RemoveHandler(ContextMenu.ClosedEvent, new RoutedEventHandler(ContextMenu_Closed));
                Interlocked.Decrement(ref _attachedCount);
            }
        }
    }
}
