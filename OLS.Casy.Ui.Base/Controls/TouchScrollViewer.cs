using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Shapes;

namespace OLS.Casy.Ui.Base.Controls
{
    public class TouchScrollViewer : ScrollViewer
    {
        private bool isManipulationInertiaStarted = false;
        private bool isManipulationInertiaBreaked = false;
        private bool isManipulationHorizontally = false;
        private bool isManipulationVertically = false;
        static TouchScrollViewer()
        {
            Control.TemplateProperty.OverrideMetadata(typeof(TouchScrollViewer), new FrameworkPropertyMetadata(CreateDefaultControlTemplate()));
        }

        private static void OnManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            var s = sender as ScrollContentPresenter;
            if (s != null)
            {
                var sv = s.TemplatedParent as TouchScrollViewer;
                if (sv != null)
                {
                    if (sv.isManipulationInertiaStarted && !sv.isManipulationInertiaBreaked)
                        sv.isManipulationInertiaBreaked = true;
                    e.IsSingleTouchEnabled = true;
                    if (s.CanVerticallyScroll)
                    {
                        if (s.CanHorizontallyScroll)
                        {
                            e.Mode = ManipulationModes.Translate;
                            e.ManipulationContainer = sv;
                            sv.isManipulationHorizontally = true;
                            sv.isManipulationVertically = true;
                        }
                        else
                        {
                            e.Mode = ManipulationModes.TranslateY;
                            e.ManipulationContainer = sv;
                            sv.isManipulationHorizontally = false;
                            sv.isManipulationVertically = true;
                        }
                    }
                    else
                    {
                        if (s.CanHorizontallyScroll)
                        {
                            e.Mode = ManipulationModes.TranslateX;
                            e.ManipulationContainer = sv;
                            sv.isManipulationHorizontally = true;
                            sv.isManipulationVertically = false;
                        }
                        else
                        {
                            e.Cancel();
                        }
                    }
                    e.Handled = false;
                }
            }
        }
        private static void OnManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            var sv = e.ManipulationContainer as TouchScrollViewer;
            if (sv != null)
            {
                bool outHO = !sv.isManipulationHorizontally, outVO = !sv.isManipulationVertically;
                double newHO = e.DeltaManipulation.Translation.X, newVO = e.DeltaManipulation.Translation.Y;
                if (newHO != 0.0)
                {
                    newHO = sv.HorizontalOffset - newHO;
                    if (newHO < 0.0)
                    {
                        newHO = 0.0;
                        outHO = true;
                    }
                    else if (newHO > sv.ScrollableWidth)
                    {
                        newHO = sv.ScrollableWidth;
                        outHO = true;
                    }
                    sv.ScrollToHorizontalOffset(newHO);
                }
                if (newVO != 0.0)
                {
                    newVO = sv.VerticalOffset - newVO;
                    if (newVO < 0.0)
                    {
                        newVO = 0.0;
                        outVO = true;
                    }
                    else if (newVO > sv.ScrollableHeight)
                    {
                        newVO = sv.ScrollableHeight;
                        outVO = true;
                    }
                    sv.ScrollToVerticalOffset(newVO);
                }
                if (sv.isManipulationInertiaStarted)
                {
                    if (sv.isManipulationInertiaBreaked || (outHO && outVO))
                    {
                        e.Complete();
                    }
                }
                e.Handled = false;
            }
        }
        private static void OnManipulationInertiaStarting(object sender, ManipulationInertiaStartingEventArgs e)
        {
            var sv = e.ManipulationContainer as TouchScrollViewer;
            if (sv != null)
            {
                e.TranslationBehavior.DesiredDeceleration = 10.0 * 96.0 / (1000.0 * 1000.0);
                sv.isManipulationInertiaStarted = true;
                sv.isManipulationInertiaBreaked = false;
                e.Handled = false;
            }
        }
        private static void OnManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            var sv = e.ManipulationContainer as TouchScrollViewer;
            if (sv != null)
            {
                sv.isManipulationInertiaStarted = false;
                sv.isManipulationInertiaBreaked = false;
            }
        }
        private static ControlTemplate CreateDefaultControlTemplate()
        {
            FrameworkElementFactory fGrid = new FrameworkElementFactory(typeof(Grid), "Grid");
            FrameworkElementFactory fGridColOne = new FrameworkElementFactory(typeof(ColumnDefinition), "ColumnDefinitionOne");
            FrameworkElementFactory fGridColTwo = new FrameworkElementFactory(typeof(ColumnDefinition), "ColumnDefinitionTwo");
            FrameworkElementFactory fGridRowOne = new FrameworkElementFactory(typeof(RowDefinition), "RowDefinitionOne");
            FrameworkElementFactory fGridRowTwo = new FrameworkElementFactory(typeof(RowDefinition), "RowDefinitionTwo");
            FrameworkElementFactory fVerScrollB = new FrameworkElementFactory(typeof(ScrollBar), "PART_VerticalScrollBar");
            FrameworkElementFactory fHorScrollB = new FrameworkElementFactory(typeof(ScrollBar), "PART_HorizontalScrollBar");
            FrameworkElementFactory fScrollConP = new FrameworkElementFactory(typeof(ScrollContentPresenter), "PART_ScrollContentPresenter");
            FrameworkElementFactory fDownCorner = new FrameworkElementFactory(typeof(System.Windows.Shapes.Rectangle), "Corner");
            Binding bHorizontalOffset = new Binding("HorizontalOffset");
            bHorizontalOffset.Mode = BindingMode.OneWay;
            bHorizontalOffset.RelativeSource = RelativeSource.TemplatedParent;
            Binding bVerticalOffset = new Binding("VerticalOffset");
            bVerticalOffset.Mode = BindingMode.OneWay;
            bVerticalOffset.RelativeSource = RelativeSource.TemplatedParent;
            fGrid.SetValue(Panel.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
            fGrid.AppendChild(fGridColOne);
            fGrid.AppendChild(fGridColTwo);
            fGrid.AppendChild(fGridRowOne);
            fGrid.AppendChild(fGridRowTwo);
            fGrid.AppendChild(fDownCorner);
            fGrid.AppendChild(fScrollConP);
            fGrid.AppendChild(fVerScrollB);
            fGrid.AppendChild(fHorScrollB);

            fGridColOne.SetValue(ColumnDefinition.WidthProperty, new GridLength(1.0, GridUnitType.Star));
            fGridColTwo.SetValue(ColumnDefinition.WidthProperty, new GridLength(1.0, GridUnitType.Auto));

            fGridRowOne.SetValue(RowDefinition.HeightProperty, new GridLength(1.0, GridUnitType.Star));
            fGridRowTwo.SetValue(RowDefinition.HeightProperty, new GridLength(1.0, GridUnitType.Auto));

            fScrollConP.SetValue(Grid.ColumnProperty, 0);
            fScrollConP.SetValue(Grid.RowProperty, 0);
            fScrollConP.SetValue(FrameworkElement.MarginProperty, new TemplateBindingExtension(Control.PaddingProperty));
            fScrollConP.SetValue(ContentControl.ContentProperty, new TemplateBindingExtension(ContentControl.ContentProperty));
            fScrollConP.SetValue(ContentControl.ContentTemplateProperty, new TemplateBindingExtension(ContentControl.ContentTemplateProperty));
            fScrollConP.SetValue(ScrollViewer.CanContentScrollProperty, new TemplateBindingExtension(ScrollViewer.CanContentScrollProperty));
            fScrollConP.SetValue(UIElement.IsManipulationEnabledProperty, true);
            fScrollConP.AddHandler(UIElement.ManipulationStartingEvent, new EventHandler<ManipulationStartingEventArgs>(TouchScrollViewer.OnManipulationStarting));
            fScrollConP.AddHandler(UIElement.ManipulationDeltaEvent, new EventHandler<ManipulationDeltaEventArgs>(TouchScrollViewer.OnManipulationDelta));
            fScrollConP.AddHandler(UIElement.ManipulationInertiaStartingEvent, new EventHandler<ManipulationInertiaStartingEventArgs>(TouchScrollViewer.OnManipulationInertiaStarting));
            fScrollConP.AddHandler(UIElement.ManipulationCompletedEvent, new EventHandler<ManipulationCompletedEventArgs>(TouchScrollViewer.OnManipulationCompleted));

            fHorScrollB.SetValue(ScrollBar.OrientationProperty, Orientation.Horizontal);
            fHorScrollB.SetValue(Grid.ColumnProperty, 0);
            fHorScrollB.SetValue(Grid.RowProperty, 1);
            fHorScrollB.SetValue(RangeBase.MinimumProperty, 0.0);
            fHorScrollB.SetValue(RangeBase.MaximumProperty, new TemplateBindingExtension(ScrollViewer.ScrollableWidthProperty));
            fHorScrollB.SetValue(ScrollBar.ViewportSizeProperty, new TemplateBindingExtension(ScrollViewer.ViewportWidthProperty));
            fHorScrollB.SetBinding(RangeBase.ValueProperty, bHorizontalOffset);
            fHorScrollB.SetValue(UIElement.VisibilityProperty, new TemplateBindingExtension(ScrollViewer.ComputedHorizontalScrollBarVisibilityProperty));
            fHorScrollB.SetValue(FrameworkElement.CursorProperty, Cursors.Arrow);
            fHorScrollB.SetValue(AutomationProperties.AutomationIdProperty, "HorizontalScrollBar");

            fVerScrollB.SetValue(Grid.ColumnProperty, 1);
            fVerScrollB.SetValue(Grid.RowProperty, 0);
            fVerScrollB.SetValue(RangeBase.MinimumProperty, 0.0);
            fVerScrollB.SetValue(RangeBase.MaximumProperty, new TemplateBindingExtension(ScrollViewer.ScrollableHeightProperty));
            fVerScrollB.SetValue(ScrollBar.ViewportSizeProperty, new TemplateBindingExtension(ScrollViewer.ViewportHeightProperty));
            fVerScrollB.SetBinding(RangeBase.ValueProperty, bVerticalOffset);
            fVerScrollB.SetValue(UIElement.VisibilityProperty, new TemplateBindingExtension(ScrollViewer.ComputedVerticalScrollBarVisibilityProperty));
            fVerScrollB.SetValue(FrameworkElement.CursorProperty, Cursors.Arrow);
            fVerScrollB.SetValue(AutomationProperties.AutomationIdProperty, "VerticalScrollBar");

            fDownCorner.SetValue(Grid.ColumnProperty, 1);
            fDownCorner.SetValue(Grid.RowProperty, 1);
            fDownCorner.SetResourceReference(Shape.FillProperty, System.Windows.SystemColors.ControlBrushKey);

            ControlTemplate controlTemplate = new ControlTemplate(typeof(ScrollViewer));
            controlTemplate.VisualTree = fGrid;
            controlTemplate.Seal();
            return controlTemplate;
        }
    }

}
