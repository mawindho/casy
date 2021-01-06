using DevExpress.Xpf.Charts;
using DevExpress.XtraPrinting;
using OLS.Casy.Core;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Models;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace OLS.Casy.Ui.Base.Extensions
{
    public static class CaptureChartImageExtension
    {
        public static FileInfo GetFileInfo(DependencyObject obj)
        {
            return (FileInfo)obj.GetValue(FileInfoProperty);
        }

        public static void SetFileInfo(DependencyObject obj, FileInfo value)
        {
            obj.SetValue(FileInfoProperty, value);
        }

        public static readonly DependencyProperty FileInfoProperty =
            DependencyProperty.RegisterAttached("FileInfo", typeof(FileInfo), typeof(CaptureChartImageExtension), new PropertyMetadata(null));

        public static MeasureSetup GetDoCaptureImage(DependencyObject obj)
        {
            return (MeasureSetup)obj.GetValue(DoCaptureImageProperty);
        }

        public static void SetDoCaptureImage(DependencyObject obj, MeasureSetup value)
        {
            obj.SetValue(DoCaptureImageProperty, value);
        }

        public static readonly DependencyProperty DoCaptureImageProperty =
            DependencyProperty.RegisterAttached("DoCaptureImage", typeof(MeasureSetup), typeof(CaptureChartImageExtension), new PropertyMetadata(null, CaptureImagePropertyChanged));

        public static byte[] GetCapturedImage(DependencyObject obj)
        {
            return (byte[])obj.GetValue(CapturedImageProperty);
        }

        public static void SetCapturedImage(DependencyObject obj, byte[] value)
        {
            obj.SetValue(CapturedImageProperty, value);
        }

        public static readonly DependencyProperty CapturedImageProperty =
            DependencyProperty.RegisterAttached("CapturedImage", typeof(byte[]), typeof(CaptureChartImageExtension), new PropertyMetadata(null));

        public static void CaptureImagePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var chartControl = obj as ChartControl;
            var measureSetup = e.NewValue as MeasureSetup;

            var localizationService = GlobalCompositionContainerFactory.GetExport<ILocalizationService>();

            if (chartControl != null && measureSetup != null)
            {
                var diagram = chartControl.Diagram as XYDiagram2D;
                var origLineBrush = diagram.ActualAxisX.GridLinesBrush;
                var origTitleBrush = diagram.ActualAxisX.Title.Foreground;
                var origLabelBrush = diagram.ActualAxisX.Label.Foreground;

                // Create an image of the chart.
                var printBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BEBEBE"));
                var printBrush2 = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#000000"));

                diagram.ActualAxisX.GridLinesBrush = printBrush;
                diagram.ActualAxisX.Title.Foreground = printBrush2;
                diagram.ActualAxisX.Label.Foreground = printBrush2;

                diagram.ActualAxisY.GridLinesBrush = printBrush;
                diagram.ActualAxisY.Title.Foreground = printBrush2;
                diagram.ActualAxisY.Label.Foreground = printBrush2;

                if (measureSetup.Cursors != null)
                {
                    int i = 1;
                    foreach (var cursor in measureSetup.Cursors)
                    {
                        if (cursor != null)
                        {
                            //diagram.ActualAxisX.Strips.Add(new Strip()
                            //{
                            //    MinLimit = measureSetup.SmoothedDiameters[cursor.MinLimit],
                            //    MaxLimit = measureSetup.SmoothedDiameters[cursor.MaxLimit],
                            //    //BorderBrush = Application.Current.Resources[string.Format("StripBorderColor{0}", i)] as SolidColorBrush
                            //    Brush = Application.Current.Resources[string.Format("StripBrushColor{0}", i)] as SolidColorBrush
                            //});

                            diagram.ActualAxisX.ConstantLinesBehind.Add(new ConstantLine()
                            {
                                Value = cursor.MinLimit,
                                Brush = Application.Current.Resources[string.Format("StripBrushColor{0}", i)] as SolidColorBrush,
                                LineStyle = new LineStyle()
                                {
                                    Thickness = 2
                                },
                                Title = new ConstantLineTitle()
                                {
                                    Content = localizationService.Value.GetLocalizedString(cursor.Name),
                                    Foreground = Application.Current.Resources[string.Format("StripBrushColor{0}", i)] as SolidColorBrush,
                                    Alignment = ConstantLineTitleAlignment.Far,
                                    ShowBelowLine = true
                                }
                            });

                            diagram.ActualAxisX.ConstantLinesBehind.Add(new ConstantLine()
                            {
                                Value = cursor.MaxLimit,//measureSetup.SmoothedDiameters[cursor.MaxLimit],
                                Brush = Application.Current.Resources[string.Format("StripBrushColor{0}", i)] as SolidColorBrush,
                                LineStyle = new LineStyle()
                                {
                                    Thickness = 2
                                }
                            });
                            i++;
                            if (i > 10)
                            {
                                i = 1;
                            }
                        }
                    }
                }

                var fileInfo = GetFileInfo(chartControl);

                if (fileInfo == null)
                {
                    using (MemoryStream stream = new MemoryStream())
                    {
                        chartControl.ExportToImage(stream);

                        stream.Seek(0, SeekOrigin.Begin);
                        int count = (int)stream.Length;
                        byte[] data = new byte[count];
                        stream.Read(data, 0, count);

                        SetCapturedImage(chartControl, data);
                    }
                }
                else
                {
                    ImageExportOptions imageExportOptions = new ImageExportOptions();
                    
                    switch(fileInfo.Extension.ToLower())
                    {
                        case ".png":
                            imageExportOptions.Format = ImageFormat.Png;
                            break;
                        case ".jpg":
                            imageExportOptions.Format = ImageFormat.Jpeg;
                            break;
                        case ".bmp":
                            imageExportOptions.Format = ImageFormat.Bmp;
                            break;
                        case ".tiff":
                            imageExportOptions.Format = ImageFormat.Tiff;
                            break;
                    }

                    chartControl.ExportToImage(fileInfo.FullName, imageExportOptions);

                    SetCapturedImage(chartControl, null);
                }

                diagram.ActualAxisX.GridLinesBrush = origLineBrush;
                diagram.ActualAxisX.Title.Foreground = origTitleBrush;
                diagram.ActualAxisX.Label.Foreground = origLabelBrush;

                diagram.ActualAxisY.GridLinesBrush = origLineBrush;
                diagram.ActualAxisY.Title.Foreground = origTitleBrush;
                diagram.ActualAxisY.Label.Foreground = origLabelBrush;

                diagram.ActualAxisX.ConstantLinesBehind.Clear();
            }
        }
    }
}
