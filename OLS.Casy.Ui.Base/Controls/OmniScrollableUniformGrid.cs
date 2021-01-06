using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OLS.Casy.Ui.Base.Controls
{
    public class OmniScrollableUniformGrid : Panel
    {
        //private int ComputedRows { get; set; }

        private int ComputedColumns { get; set; }

        public int Columns
        {
            get => (int)GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        public static readonly DependencyProperty ColumnsProperty =
                DependencyProperty.Register(
                "Columns",
                typeof(int),
                typeof(OmniScrollableUniformGrid),
                new PropertyMetadata(0, OnColumnsPropertyChanged));

        private static void OnColumnsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(e.NewValue is int) || (int)e.NewValue < 0)
            {
                d.SetValue(e.Property, e.OldValue);
            }

            var panel = d as Panel;
            panel?.InvalidateVisual();
        }

        //public int Rows
        //{
        //    get { return (int)GetValue(RowsProperty); }
        //    set { SetValue(RowsProperty, value); }
        //}

        //public static readonly DependencyProperty RowsProperty =
        //        DependencyProperty.Register(
        //        "Rows",
        //        typeof(int),
        //        typeof(OmniScrollableUniformGrid),
        //        new PropertyMetadata(0, OnIntegerDependencyPropertyChanged));

        /*
    private static void OnIntegerDependencyPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
            // Silently coerce the value back to >= 0 if negative.
            if (!(e.NewValue is int) || (int)e.NewValue < 0)
        {
            o.SetValue(e.Property, e.OldValue);
        }
    }


    private static void OnBooleanDependencyPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
            // Silently coerce the value back to >= 0 if negative.
            if (!(e.NewValue is bool))
        {
            o.SetValue(e.Property, e.OldValue);
        }
    }
    */

        protected override Size MeasureOverride(Size constraint)
        {
            UpdateComputedValues();

            //if(Children.Count < 1)
            //{
            //  return constraint;
            //}

            var parentGrid = FindVisualParent<Grid>(this);
            var outerUniformGrid = FindVisualParent<Grid>(this, "outerUniformGrid");

            if (parentGrid == null)
            {
                throw new ArgumentNullException("parentGrid");
            }

            var childConstraint = new Size(parentGrid.DesiredSize.Width / ComputedColumns, (outerUniformGrid?.ActualHeight ?? parentGrid.ActualHeight) / ComputedColumns /*ComputedRows*/);
            //double maxChildDesiredWidth = 0.0;
            //double maxChildDesiredHeight = 0.0;

            //  Measure each child, keeping track of max desired width & height.
            var children = GetVisibleChildren();
            for (int i = 0, count = children.Count; i < count; ++i)
            {
                var child = children[i];
                child.Measure(childConstraint);
                //Size childDesiredSize = child.DesiredSize;
            //    if (maxChildDesiredWidth < childDesiredSize.Width)
            //    {
            //        maxChildDesiredWidth = childDesiredSize.Width;
            //    }
                //if (maxChildDesiredHeight < childDesiredSize.Height)
                //{
                    //maxChildDesiredHeight = childDesiredSize.Height;
                //}
            }

            var size = new Size(0, 0)
            {
                Width = double.IsPositiveInfinity(childConstraint.Width)
                    ? parentGrid.DesiredSize.Width
                    : childConstraint.Width,
                Height = childConstraint.Height * (int) Math.Ceiling(children.Count / (double) ComputedColumns)
            };
            //size.Height = maxChildDesiredHeight * (int)Math.Ceiling((double)Children.Count / (double)ComputedColumns);
            //size.Height = maxChildDesiredHeight < parentGrid.DesiredSize.Height ? parentGrid.DesiredSize.Height : maxChildDesiredHeight * (int)Math.Ceiling((double)Children.Count / (double)ComputedColumns);

            return size;
        }
        
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            if(Children.Count < 1)
            {
                return arrangeSize;
            }


            var children = GetVisibleChildren();
            //var children = Children.OfType<UIElement>().Where(elem => elem.Visibility != Visibility.Collapsed && (!(elem is ContentControl) && (UIElement)((ContentControl)elem).Content)).Vis.ToList();
            var test = (int)Math.Ceiling(children.Count / (double)ComputedColumns);
            test = test < ComputedColumns ? ComputedColumns : test;
            var childBounds = new Rect(0, 0, arrangeSize.Width / ComputedColumns, arrangeSize.Height / test/*ComputedColumns*/ /*ComputedRows*/);
            var xStep = childBounds.Width;
            var xBound = arrangeSize.Width - 1.0;
            //childBounds.X += 0;//childBounds.Width;// * FirstColumn;

            // Arrange and Position each child to the same cell size
            foreach (var child in children)
            {
                child.Arrange(childBounds);
                if (child.Visibility == Visibility.Collapsed) continue;
                childBounds.X += xStep;
                
                if (!(childBounds.X >= xBound)) continue;
                childBounds.Y += childBounds.Height;
                childBounds.X = 0;
            }

            return arrangeSize;
        }

        private List<UIElement> GetVisibleChildren()
        {
            return Children.OfType<UIElement>().Where(child => child.Visibility != Visibility.Collapsed).ToList();
        }

        private void UpdateComputedValues()
        {
            ComputedColumns = Columns;
            //ComputedRows = Rows;

            if ((ComputedColumns != 0)) return;
            
            var nonCollapsedCount = 0;
            var children = GetVisibleChildren();
            for (int i = 0, count = children.Count; i < count; ++i)
            {
                var child = children[i];
                if (child.Visibility != Visibility.Collapsed)
                {
                    nonCollapsedCount++;
                }
            }
            if (nonCollapsedCount == 0)
            {
                nonCollapsedCount = 1;
            }
            /*
                if (ComputedRows == 0)
                {
                    if (ComputedColumns > 0)
                    {
                        ComputedRows = (nonCollapsedCount + (ComputedColumns - 1)) / ComputedColumns;
                    }
                    else
                    {
                        ComputedRows = (int)Math.Sqrt(nonCollapsedCount);
                        if ((ComputedRows * ComputedRows) < nonCollapsedCount)
                        {
                            ComputedRows++;
                        }
                        ComputedColumns = ComputedRows;
                    }
                }
                else*/
            if (ComputedColumns != 0) return;
            
            ComputedColumns = (int)Math.Sqrt(nonCollapsedCount);
            if ((ComputedColumns * ComputedColumns) < nonCollapsedCount)
            {
                ComputedColumns++;
            }
            //ComputedColumns = (nonCollapsedCount + (ComputedRows - 1)) / ComputedRows;
        }

        private static T FindVisualParent<T>(DependencyObject obj, string name = null) where T : FrameworkElement
        {
            while (true)
            {
                switch (obj)
                {
                    case null:
                        return null;
                    case T instance when name == null:
                        return instance;
                    case T instance when name == instance.Name:
                        return instance;
                    default:
                        obj = VisualTreeHelper.GetParent(obj);
                        continue;
                }
            }
        }
    }
}
