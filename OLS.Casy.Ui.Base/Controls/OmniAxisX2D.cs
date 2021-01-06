using DevExpress.Xpf.Charts;

namespace OLS.Casy.Ui.Base.Controls
{
    public class OmniAxisX2D : AxisX2D
    {
        //private static Dictionary<CursorViewModel, ConstantLine[]> _cursorMapping = new Dictionary<CursorViewModel, ConstantLine[]>();
        //private static Dictionary<ReferenceLineViewModel, ConstantLine> _referenceLineMapping = new Dictionary<ReferenceLineViewModel, ConstantLine>();

        //public static readonly DependencyProperty CursorItemsSourceProperty =
        //    DependencyProperty.Register(
        //        "CursorItemsSource",
        //        typeof(INotifyCollectionChanged),
        //        typeof(OmniAxisX2D),
        //        new PropertyMetadata(null, OnCursorItemsSource)
        //        );

        //private static readonly DependencyProperty CursorMappingsProperty =
        //    DependencyProperty.Register(
        //        "CursorMappings",
        //        typeof(Dictionary<ChartCursorViewModel, ConstantLine[]>),
        //        typeof(OmniAxisX2D));

        //public static INotifyCollectionChanged GetCursorItemsSource(DependencyObject target)
        //{
        //    return (INotifyCollectionChanged)target.GetValue(CursorItemsSourceProperty);
        //}

        ///// <summary>
        /////     Setter used by XAMl code.
        ///// </summary>
        //public static void SetCursorItemsSource(DependencyObject target, INotifyCollectionChanged value)
        //{
        //    target.SetValue(CursorItemsSourceProperty, value);
        //}

        //private static Dictionary<ChartCursorViewModel, ConstantLine[]> GetCursorMappings(DependencyObject target)
        //{
        //    return (Dictionary<ChartCursorViewModel, ConstantLine[]>)target.GetValue(CursorMappingsProperty);
        //}

        //private static void SetCursorMappings(DependencyObject target, Dictionary<ChartCursorViewModel, ConstantLine[]> value)
        //{
        //    target.SetValue(CursorMappingsProperty, value);
        //}

    //    public static readonly DependencyProperty ReferenceLineItemsSourceProperty =
    //        DependencyProperty.Register(
    //            "ReferenceLineItemsSource",
    //            typeof(INotifyCollectionChanged),
    //            typeof(OmniAxisX2D),
    //            new PropertyMetadata(null, OnReferenceLineItemsSource)
    //            );

    //    private static readonly DependencyProperty ReferenceLineMappingsProperty =
    //        DependencyProperty.Register(
    //            "ReferenceLineMappings",
    //            typeof(Dictionary<ReferenceLineViewModel, ConstantLine>),
    //            typeof(OmniAxisX2D));

    //    public static INotifyCollectionChanged GetReferenceLineItemsSource(DependencyObject target)
    //    {
    //        return (INotifyCollectionChanged)target.GetValue(ReferenceLineItemsSourceProperty);
    //    }

    //    /// <summary>
    //    ///     Setter used by XAMl code.
    //    /// </summary>
    //    public static void SetReferenceLineItemsSource(DependencyObject target, INotifyCollectionChanged value)
    //    {
    //        target.SetValue(ReferenceLineItemsSourceProperty, value);
    //    }

    //    private static Dictionary<ReferenceLineViewModel, ConstantLine> GetReferenceLineMappings(DependencyObject target)
    //    {
    //        return (Dictionary<ReferenceLineViewModel, ConstantLine>)target.GetValue(ReferenceLineMappingsProperty);
    //    }

    //    private static void SetReferenceLineMappings(DependencyObject target, Dictionary<ReferenceLineViewModel, ConstantLine> value)
    //    {
    //        target.SetValue(ReferenceLineMappingsProperty, value);
    //    }

    //    private static void OnCursorItemsSource(DependencyObject d, DependencyPropertyChangedEventArgs e)
    //    {
    //        Application.Current.Dispatcher.Invoke(() =>
    //        {
    //            AxisX2D axis = d as AxisX2D;

    //            if (axis != null)
    //            {
    //                //axis.ConstantLinesBehind.Clear();
    //                axis.Strips.Clear();

    //                if (GetCursorMappings(axis) == null)
    //                {
    //                    SetCursorMappings(axis, new Dictionary<ChartCursorViewModel, ConstantLine[]>());
    //                }

    //                if (e.NewValue != null)
    //                {
    //                    var cursorViewModels = e.NewValue as INotifyCollectionChanged;

    //                    //CreateConstantLines(axis, cursorViewModels as IEnumerable<ChartCursorViewModel>);
    //                    CreateStrips(axis, cursorViewModels as IEnumerable<ChartCursorViewModel>);

    //                    cursorViewModels.CollectionChanged += (sender, args) =>
    //                    {
    //                        Application.Current.Dispatcher.Invoke(() =>
    //                        {
    //                            IEnumerable<ChartCursorViewModel> viewModels = sender as IEnumerable<ChartCursorViewModel>;
    //                            axis.Strips.Clear();
    //                            CreateStrips(axis, viewModels);

    //                            //axis.ConstantLinesBehind.Clear();
    //                            //CreateConstantLines(axis, viewModels);

    //                            /*
    //                            if (args.OldItems != null)
    //                            {
    //                                var cursorMappings = GetCursorMappings(axis);

    //                                foreach (var cursorViewModel in args.OldItems.OfType<ChartCursorViewModel>())
    //                                {
    //                                    var mappings = cursorMappings.FirstOrDefault(m => m.Key.Cursor.Equals(cursorViewModel.Cursor));

    //                                    ConstantLine[] toRemove = mappings.Value;
    //                                    //if (cursorMappings.TryGetValue(cursorViewModel, out toRemove))
    //                                    //{
    //                                        axis.ConstantLinesBehind.Remove(toRemove[0]);
    //                                        axis.ConstantLinesBehind.Remove(toRemove[1]);
    //                                    //}
    //                                    cursorMappings.Remove(cursorViewModel);
    //                                }
    //                            }

    //                            if (args.NewItems != null)
    //                            {
    //                                CreateConstantLines(axis, args.NewItems.OfType<ChartCursorViewModel>());
    //                            }
    //                            */
    //                        });
    //                    };
    //                }
    //            }
    //        });
    //    }

    //    private static void OnReferenceLineItemsSource(DependencyObject d, DependencyPropertyChangedEventArgs e)
    //    {
    //        Application.Current.Dispatcher.Invoke(() =>
    //        {
    //            AxisX2D axis = d as AxisX2D;

    //            if (axis != null)
    //            {
    //                if (GetReferenceLineMappings(axis) == null)
    //                {
    //                    SetReferenceLineMappings(axis, new Dictionary<ReferenceLineViewModel, ConstantLine>());
    //                }

    //                if (e.NewValue != null)
    //                {
    //                    var referenceLineViewModels = e.NewValue as INotifyCollectionChanged;

    //                    CreateConstantLines(axis, referenceLineViewModels as IEnumerable<ReferenceLineViewModel>);

    //                    referenceLineViewModels.CollectionChanged += (sender, args) =>
    //                    {
    //                        if (args.OldItems != null)
    //                        {
    //                            var referenceLineMapping = GetReferenceLineMappings(axis);

    //                            foreach (var referenceLineViewModel in args.OldItems.OfType<ReferenceLineViewModel>())
    //                            {
    //                                ConstantLine toRemove;
    //                                if (referenceLineMapping.TryGetValue(referenceLineViewModel, out toRemove))
    //                                {
    //                                    axis.ConstantLinesBehind.Remove(toRemove);
    //                                }
    //                                referenceLineMapping.Remove(referenceLineViewModel);
    //                            }
    //                        }

    //                        if (args.NewItems != null)
    //                        {
    //                            CreateConstantLines(axis, args.NewItems.OfType<ReferenceLineViewModel>());
    //                        }
    //                    };
    //                }
    //            }
    //        });
    //    }

    //    private static void CreateConstantLines(AxisX2D axis, IEnumerable<ReferenceLineViewModel> referenceLineViewModels)
    //    {
    //        var referenceLineMapping = GetReferenceLineMappings(axis);

    //        foreach (var referenceLineViewModel in referenceLineViewModels)
    //        {
    //            if (!referenceLineMapping.ContainsKey(referenceLineViewModel))
    //            {
    //                //_referenceLineMapping.Remove(referenceLineViewModel);
    //                //}

    //                var referenceLine = CreateConstantLine(referenceLineViewModel, "Position", referenceLineViewModel.Color, false);
    //                axis.ConstantLinesBehind.Add(referenceLine);

    //                referenceLineMapping.Add(referenceLineViewModel, referenceLine);
    //            }
    //        }
    //    }

    //    //private static void CreateConstantLines(AxisX2D axis, IEnumerable<ChartCursorViewModel> cursorViewModels)
    //    private static void CreateStrips(AxisX2D axis, IEnumerable<ChartCursorViewModel> cursorViewModels)
    //    {
    //        //var cursorMappings = GetCursorMappings(axis);

    //        foreach (var cursorViewModel in cursorViewModels)
    //        {
    //            //if (!cursorMappings.ContainsKey(cursorViewModel))
    //            //{
    //            //_cursorMapping.Remove(cursorViewModel);
    //            //}

    //            //var minLine = CreateConstantLine(cursorViewModel, "MinLimitSmoothed", cursorViewModel.Color, true);
    //            //axis.ConstantLinesBehind.Add(minLine);

    //            //var maxLine = CreateConstantLine(cursorViewModel, "MaxLimitSmoothed", cursorViewModel.Color, false);
    //            //axis.ConstantLinesBehind.Add(maxLine);

    //            //var strip = CreateStrip(cursorViewModel, "MinLimitSmoothed", "MaxLimitSmoothed");
    //            //axis.Strips.Add(strip);

    //              //  cursorMappings.Add(cursorViewModel, new[] { minLine, maxLine });
    //            //}
    //        }
    //    }

    //    private static Strip CreateStrip(object dataContext, string minValueBindingName, string maxValueBindingName)
    //    {
    //        Strip strip = new Strip();
    //        strip.DataContext = dataContext;

    //        var minValueBinding = new Binding(minValueBindingName);
    //        minValueBinding.Source = dataContext;
    //        strip.SetBinding(Strip.MinLimitProperty, minValueBinding);

    //        var maxValueBinding = new Binding(maxValueBindingName);
    //        maxValueBinding.Source = dataContext;
    //        strip.SetBinding(Strip.MaxLimitProperty, maxValueBinding);

    //        return strip;
    //    }

    //    private static ConstantLine CreateConstantLine(object dataContext, string valueBindingName, string color, bool setNameBinding)
    //    {
    //        var constantLine = new ConstantLine();
    //        constantLine.DataContext = dataContext;

    //        Binding colorBinding = null;

    //        if (!string.IsNullOrEmpty(color))
    //        {
    //            //var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
    //            colorBinding = new Binding("Color");
    //            colorBinding.Source = dataContext;
    //            colorBinding.Converter = new StringBrushConverter();
    //        }
    //        if (setNameBinding)
    //        {
    //            var nameBinding = new Binding("Name");
    //            nameBinding.Source = dataContext;

    //            constantLine.Title = new ConstantLineTitle();
    //            constantLine.Title.SetBinding(ConstantLineTitle.ContentProperty, nameBinding);
    //            constantLine.Title.Alignment = ConstantLineTitleAlignment.Far;

    //            if (colorBinding != null)
    //            {
    //                constantLine.Title.SetBinding(ConstantLineTitle.ForegroundProperty, colorBinding);
    //            }
    //        }

    //        var valueBinding = new Binding(valueBindingName);
    //        valueBinding.Source = dataContext;
    //        constantLine.SetBinding(ConstantLine.ValueProperty, valueBinding);

    //        if (colorBinding != null)
    //        {
    //            constantLine.SetBinding(ConstantLine.BrushProperty, colorBinding);
    //        }
            
    //        return constantLine;
    //    }
    }
}
