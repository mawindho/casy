using System;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace OLS.Casy.Ui.Base.Behaviors
{
    public class BindableSelectedItemsBehavior : Behavior<TreeView>
    {
        private static readonly PropertyInfo IsSelectionChangeActiveProperty = typeof(TreeView).GetProperty("IsSelectionChangeActive", BindingFlags.NonPublic | BindingFlags.Instance);

        public IList SelectedItems
        {
            get { return (IList)GetValue(SelectedItemsProperty); }
            set { SetValue(SelectedItemsProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.RegisterAttached("SelectedItems", typeof(IList), typeof(BindableSelectedItemsBehavior),
                new FrameworkPropertyMetadata(null, OnSelectedItemsChanged));

        public bool AlwaysSelectChildItems
        {
            get { return (bool)GetValue(AlwaysSelectChildItemsProperty); }
            set { SetValue(AlwaysSelectChildItemsProperty, value); }
        }

        public static readonly DependencyProperty AlwaysSelectChildItemsProperty =
            DependencyProperty.RegisterAttached("AlwaysSelectChildItems", typeof(bool), typeof(BindableSelectedItemsBehavior),
                new FrameworkPropertyMetadata(false));

        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.SelectedItemChanged += OnSelectedItemChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            if (this.AssociatedObject != null)
            {
                this.AssociatedObject.SelectedItemChanged -= OnSelectedItemChanged;
            }
        }

        private static void OnSelectedItemsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var behavior = sender as BindableSelectedItemsBehavior;

            if (behavior == null)
            {
                return;
            }

            if (e.OldValue != null)
            {
                var oldCollection = e.OldValue as INotifyCollectionChanged;
                if(oldCollection != null)
                { 
                    oldCollection.CollectionChanged -= (s, args) => OnSelectedItemsCollectionChanged(behavior, args);
                }
            }

            if (e.NewValue != null)
            {
                var newCollection = e.NewValue as INotifyCollectionChanged;
                if(newCollection != null)
                { 
                    newCollection.CollectionChanged += (s, args) => OnSelectedItemsCollectionChanged(behavior, args);
                }
            }
        }

        private static void OnSelectedItemsCollectionChanged(BindableSelectedItemsBehavior behavior, NotifyCollectionChangedEventArgs e)
        {
            var treeView = behavior.AssociatedObject;

            var isSelectionChangeActive = IsSelectionChangeActiveProperty.GetValue(treeView, null);

            IsSelectionChangeActiveProperty.SetValue(treeView, true, null);

            if (e.OldItems != null)
            {
                foreach (var oldItem in e.OldItems)
                {
                    SetSelectionState(treeView, oldItem, (x) => false);
                }
            }

            if (e.NewItems != null)
            {
                foreach (var newItem in e.NewItems)
                {
                    SetSelectionState(treeView, newItem, (x) => true);
                }
            }
            IsSelectionChangeActiveProperty.SetValue(treeView, isSelectionChangeActive, null);
        }

        private void OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {

            var selectedItem = this.AssociatedObject.SelectedItem;
            if (selectedItem == null) return;

            var selectedTreeViewItem = TreeViewItemFromItem(this.AssociatedObject, selectedItem) as TreeViewItem;

            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                // suppress selection change notification
                // select all selected items
                // then restore selection change notifications
                var isSelectionChangeActive = IsSelectionChangeActiveProperty.GetValue(this.AssociatedObject, null);

                IsSelectionChangeActiveProperty.SetValue(this.AssociatedObject, true, null);
                foreach(var item in this.SelectedItems)
                {
                    SetSelectionState(this.AssociatedObject, item, (x) => true);
                    //var treeViewItem = this.AssociatedObject.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                    //treeViewItem.IsSelected = true;
                }

                IsSelectionChangeActiveProperty.SetValue(this.AssociatedObject, isSelectionChangeActive, null);
            }
            else
            {
                foreach (var item in this.SelectedItems)
                {
                    SetSelectionState(this.AssociatedObject, item, (other) => other == selectedItem);
                    //var treeViewItem = this.AssociatedObject.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                    //treeViewItem.IsSelected = treeViewItem == selectedTreeViewItem;
                }
                int count = this.SelectedItems.Count;
                for (int i = count; i > 0; i--)
                {
                    this.SelectedItems.RemoveAt(i - 1);
                }
                
            }

            if (!this.SelectedItems.Contains(selectedItem))
            {
                this.SelectedItems.Add(selectedItem);
                //SetSelectionState(this.AssociatedObject, selectedItem, (x) => true);

                if(this.AlwaysSelectChildItems)
                {
                    var isSelectionChangeActive = IsSelectionChangeActiveProperty.GetValue(this.AssociatedObject, null);
                    IsSelectionChangeActiveProperty.SetValue(this.AssociatedObject, true, null);
                    SelectTreeViewItems(selectedTreeViewItem.Items);
                    IsSelectionChangeActiveProperty.SetValue(this.AssociatedObject, isSelectionChangeActive, null);
                }

                //selectedTreeViewItem.IsSelected = true;
            }
            else
            {
                // deselect if already selected
                //selectedTreeViewItem.IsSelected = false;
                //SetSelectionState(this.AssociatedObject, selectedItem, (x) => false);
                this.SelectedItems.Remove(selectedItem);

                if (this.AlwaysSelectChildItems)
                {
                    var isSelectionChangeActive = IsSelectionChangeActiveProperty.GetValue(this.AssociatedObject, null);
                    IsSelectionChangeActiveProperty.SetValue(this.AssociatedObject, true, null);
                    foreach (var item in selectedTreeViewItem.Items)
                    {
                        this.SelectedItems.Remove(item);
                        //SetSelectionState(this.AssociatedObject, item, (x) => false);
                    }
                    IsSelectionChangeActiveProperty.SetValue(this.AssociatedObject, isSelectionChangeActive, null);
                }
            }
        }

        private void SelectTreeViewItems(ItemCollection itemsCollection)
        {
            foreach (var item in itemsCollection)
            {
                if (!this.SelectedItems.Contains(item))
                {
                    this.SelectedItems.Add(item);
                    //SetSelectionState(this.AssociatedObject, item, (x) => true);

                    var treeViewItem = TreeViewItemFromItem(this.AssociatedObject, item) as TreeViewItem;
                    if (treeViewItem != null)
                    {
                        SelectTreeViewItems(treeViewItem.Items);
                    }
                }
            }
        }

        private static TreeViewItem TreeViewItemFromItem(ItemsControl parent, object item)
        {
            if (parent == null || item == null)
            {
                return null;
            }

            TreeViewItem childNode = parent.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;

            if (childNode != null)
            {
                //childNode.Focus();
                return childNode;
            }

            if (parent.Items.Count > 0)
            {
                foreach (var childItem in parent.Items)
                {
                    ItemsControl childControl = parent.ItemContainerGenerator.ContainerFromItem(childItem) as ItemsControl;
                    childNode = TreeViewItemFromItem(childControl, item);
                    if(childNode != null)
                    {
                        return childNode;
                    }
                }
            }
            return null;
        }

        private static bool SetSelectionState(ItemsControl parent, object child, Func<object, bool> condition)
        {
            if (parent == null || child == null)
            {
                return false;
            }

            TreeViewItem childNode = parent.ItemContainerGenerator.ContainerFromItem(child) as TreeViewItem;

            if (childNode != null)
            {
                //childNode.Focus();
                return childNode.IsSelected = condition(child);
            }

            if (parent.Items.Count > 0)
            {
                foreach (var childItem in parent.Items)
                {
                    parent.UpdateLayout();
                    ItemsControl childControl = parent.ItemContainerGenerator.ContainerFromItem(childItem) as ItemsControl;
                    if (SetSelectionState(childControl, child, condition))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
