using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OLS.Casy.Ui.Base.Controls
{
    public class OmniScrollableGrid : System.Windows.Controls.Panel
    {
        protected override Size MeasureOverride(Size constraint)
        {
            //UpdateComputedValues();

            var parentGrid = FindVisualParent<Grid>(this);
            //var outerUniformGrid = FindVisualParent<Grid>(this, "outerUniformGrid");

            if (parentGrid == null)
            {
                throw new ArgumentNullException("parentGrid");
            }

            var childConstraint = new Size(parentGrid.DesiredSize.Width, parentGrid.ActualHeight);
            //double maxChildDesiredWidth = 0.0;
            //double maxChildDesiredHeight = 0.0;

            //  Measure each child, keeping track of max desired width & height.

            double height = 0d;
            var children = GetVisibleChildren();
            for (int i = 0, count = children.Count; i < count; ++i)
            {
                var child = children[i];
                child.Measure(childConstraint);
                Size childDesiredSize = child.DesiredSize;

                height += childDesiredSize.Height;

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
                Width = parentGrid.DesiredSize.Width,
                Height = height
            };
            //size.Height = maxChildDesiredHeight * (int)Math.Ceiling((double)Children.Count / (double)ComputedColumns);
            //size.Height = maxChildDesiredHeight < parentGrid.DesiredSize.Height ? parentGrid.DesiredSize.Height : maxChildDesiredHeight * (int)Math.Ceiling((double)Children.Count / (double)ComputedColumns);

            return size;
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            if (Children.Count < 1)
            {
                return arrangeSize;
            }


            var children = GetVisibleChildren();
            //var children = Children.OfType<UIElement>().Where(elem => elem.Visibility != Visibility.Collapsed && (!(elem is ContentControl) && (UIElement)((ContentControl)elem).Content)).Vis.ToList();
            //var test = (int)Math.Ceiling(children.Count / (double)ComputedColumns);
            //test = test < ComputedColumns ? ComputedColumns : test;
            var childBounds = new Rect(0, 0, arrangeSize.Width, arrangeSize.Height);
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

        /*
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
            
            if (ComputedColumns != 0) return;

            ComputedColumns = (int)Math.Sqrt(nonCollapsedCount);
            if ((ComputedColumns * ComputedColumns) < nonCollapsedCount)
            {
                ComputedColumns++;
            }
            //ComputedColumns = (nonCollapsedCount + (ComputedRows - 1)) / ComputedRows;
        }
        */

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
