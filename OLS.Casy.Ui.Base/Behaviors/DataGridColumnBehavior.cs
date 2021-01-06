using DevExpress.Xpf.Grid;
using Microsoft.Xaml.Behaviors;
using OLS.Casy.Ui.Base.Api;
using OLS.Casy.Ui.Base.Controls;
using OLS.Casy.Ui.Base.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace OLS.Casy.Ui.Base.Behaviors
{
    public class DataGridColumnBehavior : Behavior<GridControl>
    {
        //public ObservableCollection<AdditinalGridColumnInfo> Columns
        //{
        //    get { return (ObservableCollection<AdditinalGridColumnInfo>)GetValue(ColumnsProperty); }
        //    set { SetValue(ColumnsProperty, value); }
        //}

        //public static readonly DependencyProperty ColumnsProperty =
        //    DependencyProperty.RegisterAttached("Columns", typeof(ObservableCollection<AdditinalGridColumnInfo>), typeof(DataGridColumnBehavior),
        //        new FrameworkPropertyMetadata(null, OnColumnsChanged));


        public ObservableCollection<string> Filter
        {
            get { return (ObservableCollection<string>)GetValue(FilterProperty); }
            set { SetValue(FilterProperty, value); }
        }

        public static readonly DependencyProperty FilterProperty =
            DependencyProperty.RegisterAttached("Filter", typeof(ObservableCollection<string>), typeof(DataGridColumnBehavior),
                new FrameworkPropertyMetadata(null));

        public bool RefreshData
        {
            get { return (bool)GetValue(RefreshDataProperty); }
            set { SetValue(RefreshDataProperty, value); }
        }

        public static readonly DependencyProperty RefreshDataProperty =
            DependencyProperty.RegisterAttached("RefreshData", typeof(bool), typeof(DataGridColumnBehavior),
                new FrameworkPropertyMetadata(false, OnRefreshDataChanged));

        

        public ObservableCollection<AdditinalGridColumnInfo> AdditinalColumns
        {
            get { return (ObservableCollection<AdditinalGridColumnInfo>)GetValue(AdditinalColumnsProperty); }
            set { SetValue(AdditinalColumnsProperty, value); }
        }

        public static readonly DependencyProperty AdditinalColumnsProperty =
            DependencyProperty.RegisterAttached("AdditinalColumns", typeof(ObservableCollection<AdditinalGridColumnInfo>), typeof(DataGridColumnBehavior),
                new FrameworkPropertyMetadata(null, OnAdditionColumnsChanged));

        public bool ShowColumnChooser
        {
            get { return (bool)GetValue(ShowColumnChooserProperty); }
            set { SetValue(ShowColumnChooserProperty, value); }
        }

        public static readonly DependencyProperty ShowColumnChooserProperty =
            DependencyProperty.RegisterAttached("ShowColumnChooser", typeof(bool), typeof(DataGridColumnBehavior),
                new FrameworkPropertyMetadata(false));

        public CustomColumnChooserControl CustomColumnChooser
        {
            get { return (CustomColumnChooserControl)GetValue(CustomColumnChooserProperty); }
            set { SetValue(CustomColumnChooserProperty, value); }
        }

        public static readonly DependencyProperty CustomColumnChooserProperty =
            DependencyProperty.RegisterAttached("CustomColumnChooser", typeof(CustomColumnChooserControl), typeof(DataGridColumnBehavior),
                new FrameworkPropertyMetadata(null, OnCustomColumnChooserProperty));

        protected override void OnAttached()
        {
            base.OnAttached();
            _gridControl = this.AssociatedObject;
            _gridControl.CustomRowFilter += _gridControl_CustomRowFilter;
        }

        protected override void OnDetaching()
        {
            _gridControl.CustomRowFilter -= _gridControl_CustomRowFilter;
            _gridControl = null;
            base.OnDetaching();
        }

        private void _gridControl_CustomRowFilter(object sender, RowFilterEventArgs e)
        {
            var model = e.Source.GetRowByListIndex(e.ListSourceRowIndex) as IFilterable;

            if (model != null)
            {
                e.Visible = model.IsVisible;
            }

            e.Handled = !e.Visible ? true : false;
        }

        private static void OnRefreshDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var boolValue = (bool)e.NewValue;
            if(boolValue)
            {
                (d as DataGridColumnBehavior).AssociatedObject.RefreshData();
                (d as DataGridColumnBehavior).RefreshData = false;
                
            }
        }

        private static void OnCustomColumnChooserProperty(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var behavior = sender as DataGridColumnBehavior;

            if (behavior == null)
            {
                return;
            }

            CustomColumnChooserControl columnChooser = e.NewValue as CustomColumnChooserControl;

            if(columnChooser == null)
            {
                return;
            }

            columnChooser.View = behavior.AssociatedObject.View;
            columnChooser.Filter = behavior.Filter; 
            behavior.AssociatedObject.View.ColumnChooserFactory = new CustomColumnChooser(columnChooser);
        }

        private static void OnAdditionColumnsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var behavior = sender as DataGridColumnBehavior;

            if (behavior == null)
            {
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (e.OldValue != null)
                {
                    var oldCollection = e.OldValue as ObservableCollection<AdditinalGridColumnInfo>;
                    oldCollection.CollectionChanged -= behavior.OnAdditinalColumnsCollectionChanged;
                }

                if (e.NewValue != null)
                {
                    var newCollection = e.NewValue as ObservableCollection<AdditinalGridColumnInfo>;
                    newCollection.CollectionChanged += behavior.OnAdditinalColumnsCollectionChanged;

                    AddColumns(behavior.AssociatedObject, newCollection, false);
                }
            });
        }

        /*
        private static void OnColumnsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var behavior = sender as DataGridColumnBehavior;

            if (behavior == null)
            {
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (e.OldValue != null)
                {
                    var oldCollection = e.OldValue as ObservableCollection<AdditinalGridColumnInfo>;
                    oldCollection.CollectionChanged -= OnColumnsCollectionChanged;
                }

                if (e.NewValue != null)
                {
                    var newCollection = e.NewValue as ObservableCollection<AdditinalGridColumnInfo>;
                    newCollection.CollectionChanged += OnColumnsCollectionChanged;
                    AddColumns(behavior.AssociatedObject, newCollection, true);
                }
            });
        }
        */

        private static GridControl _gridControl;

        private void OnAdditinalColumnsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    _gridControl.Columns.Clear();
                    AddColumns(_gridControl, this.AdditinalColumns, false);
                }
                else
                {
                    if (e.OldItems != null)
                    {
                        foreach (var oldItem in e.OldItems.OfType<AdditinalGridColumnInfo>())
                        {
                            var gridColumn = _gridControl.Columns.FirstOrDefault(column => column.Header.ToString() == oldItem.Header);
                            if (gridColumn != null)
                            {
                                gridColumn.Binding = null;
                                BindingOperations.ClearBinding(gridColumn, CustomGridColumn.VisibleProperty);
                                _gridControl.Columns.Remove(gridColumn);
                            }
                        }
                    }

                    if (e.NewItems != null)
                    {
                        AddColumns(_gridControl, e.NewItems.OfType<AdditinalGridColumnInfo>(), false);
                    }
                }
            });
        }

        private static void OnColumnsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (e.OldItems != null)
                {
                    foreach (var oldItem in e.OldItems.OfType<AdditinalGridColumnInfo>())
                    {
                        var gridColumn = _gridControl.Columns.FirstOrDefault(column => column.Header.ToString() == oldItem.Header);
                        gridColumn.Binding = null;
                        BindingOperations.ClearBinding(gridColumn, CustomGridColumn.VisibleProperty);
                        _gridControl.Columns.Remove(gridColumn);
                    }
                }

                if (e.NewItems != null)
                {
                    AddColumns(_gridControl, e.NewItems.OfType<AdditinalGridColumnInfo>(), true);
                }
            });
        }

        private static void AddColumns(GridControl grid, IEnumerable<AdditinalGridColumnInfo> columnInfos, bool isVisible)
        {
            foreach (var columnInfo in columnInfos.OrderBy(ci => ci.CursorName).ThenBy(ci => ci.Order))
            {
                CustomGridColumn column = new CustomGridColumn();
                column.DataContext = columnInfo;
                if (!string.IsNullOrEmpty(columnInfo.Style))
                {
                    column.Style = Application.Current.FindResource(columnInfo.Style) as Style;
                }
                column.Binding = new Binding(columnInfo.Binding);
                column.Width = new GridColumnWidth(2, GridColumnUnitType.Star);
                var isSelectedRowBinding = new Binding("IsSelectedRow");
                isSelectedRowBinding.Mode = BindingMode.TwoWay;
                column.SetBinding(CustomGridColumn.VisibleProperty, isSelectedRowBinding);
                //column.Visible = isVisible;
                if (columnInfo.IsTwoWayBinding)
                {
                    ((Binding)column.Binding).Mode = BindingMode.TwoWay;
                }
                column.Header = columnInfo.Header;
                column.Tag = columnInfo.Category;
                column.Order = columnInfo.Order;
                grid.Columns.Add(column);
            }
        }
    }
}
