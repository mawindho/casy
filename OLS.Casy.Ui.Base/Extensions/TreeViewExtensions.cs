using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections;
using System.Windows.Media;
using System.Reflection;
using System.Collections.Specialized;

namespace OLS.Casy.Ui.Base.Extensions
{
    public class TreeViewExtensions : DependencyObject
    {
        private static readonly PropertyInfo IsSelectionChangeActiveProperty = typeof(TreeView).GetProperty("IsSelectionChangeActive", BindingFlags.NonPublic | BindingFlags.Instance);

        public static bool GetEnableMultiSelect(DependencyObject obj)
        {
            return (bool)obj.GetValue(EnableMultiSelectProperty);
        }

        public static void SetEnableMultiSelect(DependencyObject obj, bool value)
        {
            obj.SetValue(EnableMultiSelectProperty, value);
        }

        // Using a DependencyProperty as the backing store for EnableMultiSelect.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EnableMultiSelectProperty =
            DependencyProperty.RegisterAttached("EnableMultiSelect", typeof(bool), typeof(TreeViewExtensions), new FrameworkPropertyMetadata(false)
            {
                PropertyChangedCallback = EnableMultiSelectChanged,
                BindsTwoWayByDefault = true
            });

        public static INotifyCollectionChanged GetSelectedItems(DependencyObject obj)
        {
            return (INotifyCollectionChanged)obj.GetValue(SelectedItemsProperty);
        }

        public static void SetSelectedItems(DependencyObject obj, IList value)
        {
            obj.SetValue(SelectedItemsProperty, value);
        }

        // Using a DependencyProperty as the backing store for SelectedItems.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.RegisterAttached("SelectedItems", typeof(INotifyCollectionChanged), typeof(TreeViewExtensions), new PropertyMetadata(OnSelectedItemsChanged));

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TreeView treeView = d as TreeView;

            if(treeView != null)
            { 

            if(e.OldValue != null)
            {
                INotifyCollectionChanged collection = e.OldValue as INotifyCollectionChanged;
                if(collection != null)
                {
                    collection.CollectionChanged -= (s, args) => OnSelectedItemsCollectionChanged(treeView, args);
                }
            }

            if (e.NewValue != null)
            {
                INotifyCollectionChanged collection = e.NewValue as INotifyCollectionChanged;
                if (collection != null)
                {
                    collection.CollectionChanged += (s, args) => OnSelectedItemsCollectionChanged(treeView, args);
                }
            }
            }
        }

        private static void OnSelectedItemsCollectionChanged(TreeView tree, NotifyCollectionChangedEventArgs e)
        {
            var isSelectionChangeActive = (bool) IsSelectionChangeActiveProperty.GetValue(tree, null);

            if(!isSelectionChangeActive)
            { 
                IsSelectionChangeActiveProperty.SetValue(tree, true, null);

                foreach (var selected in (IList) GetSelectedItems(tree))
                {
                    var treeViewI = TreeViewItemFromItem(tree, selected);
                    if (treeViewI != null)
                    {
                        SetIsSelected(treeViewI, true);
                    }
                }

                IsSelectionChangeActiveProperty.SetValue(tree, false, null);
            };
        }

        static TreeViewItem GetAnchorItem(DependencyObject obj)
        {
            return (TreeViewItem)obj.GetValue(AnchorItemProperty);
        }

        static void SetAnchorItem(DependencyObject obj, TreeViewItem value)
        {
            obj.SetValue(AnchorItemProperty, value);
        }

        // Using a DependencyProperty as the backing store for AnchorItem.  This enables animation, styling, binding, etc...
        static readonly DependencyProperty AnchorItemProperty =
            DependencyProperty.RegisterAttached("AnchorItem", typeof(TreeViewItem), typeof(TreeViewExtensions), new PropertyMetadata(null));



        static void EnableMultiSelectChanged(DependencyObject s, DependencyPropertyChangedEventArgs args)
        {
            TreeView tree = (TreeView)s;
            var wasEnable = (bool)args.OldValue;
            var isEnabled = (bool)args.NewValue;
            if (wasEnable)
            {
                tree.RemoveHandler(TreeViewItem.MouseDownEvent, new MouseButtonEventHandler(ItemClicked));
                //tree.RemoveHandler(TreeView.KeyDownEvent, new KeyEventHandler(KeyDown));
            }
            if (isEnabled)
            {
                tree.AddHandler(TreeViewItem.MouseDownEvent, new MouseButtonEventHandler(ItemClicked), true);
                //tree.AddHandler(TreeView.KeyDownEvent, new KeyEventHandler(KeyDown));
            }
        }

        static TreeView GetTree(TreeViewItem item)
        {
            Func<DependencyObject, DependencyObject> getParent = (o) => VisualTreeHelper.GetParent(o);
            FrameworkElement currentItem = item;
            while (!(getParent(currentItem) is TreeView))
                currentItem = (FrameworkElement)getParent(currentItem);
            return (TreeView)getParent(currentItem);
        }



        static void RealSelectedChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            TreeViewItem item = (TreeViewItem)sender;
            var selectedItems = (IList) GetSelectedItems(GetTree(item));
            if (selectedItems != null)
            {
                var isSelected = GetIsSelected(item);
                if (isSelected)
                    try
                    {
                        if(!selectedItems.Contains(item.DataContext))
                        { 
                            selectedItems.Add(item.DataContext);
                        }
                        item.IsSelected = true;
                    }
                    catch (ArgumentException)
                    {
                    }
                else
                { 
                    selectedItems.Remove(item.DataContext);
                    item.IsSelected = false;
                }
            }
        }

        //static void KeyDown(object sender, KeyEventArgs e)
        //{
        //    TreeView tree = (TreeView)sender;
        //    /*
        //    if (e.Key == Key.A && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
        //    {
        //        foreach (var item in GetTreeViewItems(tree))
        //        {
        //            SetIsSelected(item, true);
        //        }
        //        e.Handled = true;
        //    }
        //    */
        //}

        static void ItemClicked(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem item = FindTreeViewItem(e.OriginalSource);
            if (item == null)
                return;
            TreeView tree = (TreeView)sender;

            var isSelectionChangeActive = IsSelectionChangeActiveProperty.GetValue(tree, null);

            var mouseButton = e.ChangedButton;
            if (mouseButton != MouseButton.Left)
            {
                if ((mouseButton == MouseButton.Right) && ((Keyboard.Modifiers & (ModifierKeys.Shift | ModifierKeys.Control)) == ModifierKeys.None))
                {
                    if (GetIsSelected(item))
                    {
                        UpdateAnchorAndActionItem(tree, item);
                        return;
                    }
                    MakeSingleSelection(tree, item);
                }
                return;
            }
            if (mouseButton != MouseButton.Left)
            {
                if ((mouseButton == MouseButton.Right) && ((Keyboard.Modifiers & (ModifierKeys.Shift | ModifierKeys.Control)) == ModifierKeys.None))
                {
                    if (GetIsSelected(item))
                    {
                        UpdateAnchorAndActionItem(tree, item);
                        return;
                    }
                    MakeSingleSelection(tree, item);
                }
                return;
            }
            if ((Keyboard.Modifiers & (ModifierKeys.Shift | ModifierKeys.Control)) != (ModifierKeys.Shift | ModifierKeys.Control))
            {
                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    
                    IsSelectionChangeActiveProperty.SetValue(tree, true, null);

                    MakeToggleSelection(tree, item);

                    DeepSelect(item, GetIsSelected(item));

                    foreach (var selected in (IList) GetSelectedItems(tree))
                    {
                        var treeViewI = TreeViewItemFromItem(tree, selected);
                        if(treeViewI != null)
                        { 
                            treeViewI.IsSelected = true;
                        }
                    }

                    IsSelectionChangeActiveProperty.SetValue(tree, isSelectionChangeActive, null);
                    return;
                }
                if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                {
                    MakeAnchorSelection(tree, item, true);
                    return;
                }

                IsSelectionChangeActiveProperty.SetValue(tree, true, null);
                MakeSingleSelection(tree, item);
                
                DeepSelect(item, GetIsSelected(item));

                foreach (var selected in (IList) GetSelectedItems(tree))
                {
                    var treeViewI = TreeViewItemFromItem(tree, selected);
                    if(treeViewI != null)
                    { 
                        treeViewI.IsSelected = true;
                    }
                }
                IsSelectionChangeActiveProperty.SetValue(tree, isSelectionChangeActive, null);

                return;
            }
            //MakeAnchorSelection(item, false);


            //SetIsSelected(tree.SelectedItem
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
                    if (childNode != null)
                    {
                        return childNode;
                    }
                }
            }
            return null;
        }

        private static TreeViewItem FindTreeViewItem(object obj)
        {
            DependencyObject dpObj = obj as DependencyObject;
            if (dpObj == null)
                return null;

            TreeViewItem treeViewItem = dpObj as TreeViewItem;
            if (treeViewItem != null)
                return treeViewItem;

            return FindTreeViewItem(VisualTreeHelper.GetParent(dpObj));
        }



        private static IEnumerable<TreeViewItem> GetTreeViewItems(ItemsControl tree)
        {
            for (int i = 0; i < tree.Items.Count; i++)
            {
                var item = (TreeViewItem)tree.ItemContainerGenerator.ContainerFromIndex(i);
                if (item == null)
                    continue;
                yield return item;
                //if (item.IsExpanded)
                foreach (var subItem in GetTreeViewItems(item))
                    yield return subItem;
            }
        }

        private static void MakeAnchorSelection(TreeView tree, TreeViewItem actionItem, bool clearCurrent)
        {
            if (GetAnchorItem(tree) == null)
            {
                var selectedItems = GetSelectedTreeViewItems(tree);
                if (selectedItems.Count > 0)
                {
                    SetAnchorItem(tree, selectedItems[selectedItems.Count - 1]);
                }
                else
                {
                    SetAnchorItem(tree, GetTreeViewItems(tree).Skip(3).FirstOrDefault());
                }
                if (GetAnchorItem(tree) == null)
                {
                    return;
                }
            }

            var anchor = GetAnchorItem(tree);

            var items = GetTreeViewItems(tree);
            bool betweenBoundary = false;
            //bool end = false;
            foreach (var item in items)
            {
                bool isBoundary = item == anchor || item == actionItem;
                if (isBoundary)
                {
                    betweenBoundary = !betweenBoundary;
                }
                if (betweenBoundary || isBoundary)
                    SetIsSelected(item, true);
                else
                    if (clearCurrent)
                    SetIsSelected(item, false);
                else
                    break;

            }
        }

        private static List<TreeViewItem> GetSelectedTreeViewItems(TreeView tree)
        {
            return GetTreeViewItems(tree).Where(i => GetIsSelected(i)).ToList();
        }

        private static void MakeSingleSelection(TreeView tree, TreeViewItem item)
        {
            foreach (TreeViewItem selectedItem in GetTreeViewItems(tree))
            {
                if (selectedItem == null)
                    continue;
                if (selectedItem != item)
                { 
                    SetIsSelected(selectedItem, false);
                    //DeepSelect(selectedItem, false);
                }
                else
                {
                    SetIsSelected(selectedItem, true);
                }
            }
            UpdateAnchorAndActionItem(tree, item);
        }

        private static void MakeToggleSelection(TreeView tree, TreeViewItem item)
        {
            var newState = !GetIsSelected(item);

            SetIsSelected(item, newState);
            UpdateAnchorAndActionItem(tree, item);

            //DeepSelect(item, newState);
        }

        private static void DeepSelect(TreeViewItem item, bool newState)
        {
            if (item.HasItems)
            {
                foreach (var childItem in item.Items)
                {
                    var treeViewChildItem = TreeViewItemFromItem(item, childItem);
                    SetIsSelected(treeViewChildItem, newState);

                    DeepSelect(treeViewChildItem, newState);
                }
            }
        }

        private static void UpdateAnchorAndActionItem(TreeView tree, TreeViewItem item)
        {
            SetAnchorItem(tree, item);
        }




        public static bool GetIsSelected(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsSelectedProperty);
        }

        public static void SetIsSelected(DependencyObject obj, bool value)
        {
            obj.SetValue(IsSelectedProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsSelected.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.RegisterAttached("IsSelected", typeof(bool), typeof(TreeViewExtensions), new PropertyMetadata(false)
            {
                PropertyChangedCallback = RealSelectedChanged
            });


    }
}