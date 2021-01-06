using OLS.Casy.Ui.Base.ViewModels;
using OLS.Casy.Ui.Base.Views;
using System.Windows;

namespace OLS.Casy.Ui.Base.Extensions
{
    public static class PrintExtension
    {
        public static PdfPreviewViewModel GetShowPrintPreview(DependencyObject obj)
        {
            return obj.GetValue(ShowPrintPreviewProperty) as PdfPreviewViewModel;
        }

        public static void SetShowPrintPreview(DependencyObject obj, PdfPreviewViewModel value)
        {
            obj.SetValue(ShowPrintPreviewProperty, value);
        }

        public static readonly DependencyProperty ShowPrintPreviewProperty =
            DependencyProperty.RegisterAttached("ShowPrintPreview", typeof(PdfPreviewViewModel), typeof(PrintExtension), new PropertyMetadata(null, ShowPrintPreviewChanged));

        public static void ShowPrintPreviewChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var viewModel = GetShowPrintPreview(obj);

            if (viewModel != null)
            {
                var pdfView = new PdfView();
                pdfView.DataContext = viewModel;
                pdfView.ShowDialog();

                viewModel.Awaiter.Set();

                SetShowPrintPreview(obj, null);
            }
        }
    }
}