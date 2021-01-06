using DevExpress.Xpf.Grid;
using Microsoft.Xaml.Behaviors;
using System.Collections;
using System.Windows;
using System.Windows.Input;

namespace OLS.Casy.Ui.Base.Behaviors
{
    public class ExpandBehavior : Behavior<TableView>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewKeyDown += AssociatedObject_PreviewKeyDown;
        }

        public static readonly DependencyProperty ExpandedItemProperty =
            DependencyProperty.Register(
                "ExpandedItem",
                typeof(object),
                typeof(ExpandBehavior),
                new PropertyMetadata(null, OnExpandedItemChanged)
                );

        public static object GetExpandedItem(DependencyObject target)
        {
            return (double)target.GetValue(ExpandedItemProperty);
        }

        public static void SetExpandedItem(DependencyObject target, object value)
        {
            target.SetValue(ExpandedItemProperty, value);
        }

        private static void OnExpandedItemChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if(e.NewValue == null)
            {
                return;
            }

            if (!(sender is ExpandBehavior expandBehavior)) return;

            if (!(expandBehavior.AssociatedObject.Grid.ItemsSource is ICollection items)) return;

            var index = 0;
            
            foreach (var item in items)
            {
                if (item == e.NewValue)
                {
                    expandBehavior.AssociatedObject.Grid.ExpandMasterRow(index);
                }

                index++;
            }
        }

        static void AssociatedObject_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var view = sender as TableView;

            // Avoid key processing when focus is within detail views
            // or when a groupo row is focused
            if (view != null && (!view.IsFocusedView || view.FocusedRowHandle < 0))
                return;

            // Process CTRL+* key combination
            if (e.Key != Key.Multiply || (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control) return;

            var finalExpandedState = view != null && !view.Grid.IsMasterRowExpanded(view.FocusedRowHandle);
            view?.Grid.SetMasterRowExpanded(view.FocusedRowHandle, finalExpandedState);
            e.Handled = true;
        }

        #region ExpandAllCommand

        private OmniDelegateCommand _expandAllCommand;

        public OmniDelegateCommand ExpandAllCommand => _expandAllCommand ?? (_expandAllCommand = new OmniDelegateCommand(ExpandAllCommandExecute));

        protected void ExpandAllCommandExecute()
        {
            var dataRowCount = ((ICollection) AssociatedObject.Grid.ItemsSource).Count;
            for (var rowHandle = 0; rowHandle < dataRowCount; rowHandle++)
                AssociatedObject.Grid.ExpandMasterRow(rowHandle);
        }

        #endregion ExpandAllCommand

        #region CollapseAllButThisCommand

        private OmniDelegateCommand _collapseAllButThisCommand;

        public OmniDelegateCommand CollapseAllButThisCommand =>
            _collapseAllButThisCommand ?? (_collapseAllButThisCommand =
                new OmniDelegateCommand(CollapseAllButThisCommandExecute));

        protected void CollapseAllButThisCommandExecute()
        {
            var dataRowCount = ((ICollection) AssociatedObject.Grid.ItemsSource).Count;
            if (AssociatedObject.FocusedRowHandle >= 0)
                AssociatedObject.Grid.ExpandMasterRow(AssociatedObject.FocusedRowHandle);
            for (var rowHandle = 0; rowHandle < dataRowCount; rowHandle++)
            {
                if (rowHandle != AssociatedObject.FocusedRowHandle)
                    AssociatedObject.Grid.CollapseMasterRow(rowHandle);
            }
        }

        #endregion CollapseAllButThisCommand
    }
}
