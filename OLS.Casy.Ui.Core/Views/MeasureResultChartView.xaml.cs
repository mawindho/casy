using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OLS.Casy.Ui.Core.Views
{
    /// <summary>
    /// Interaktionslogik für MeasureResultChartView.xaml
    /// </summary>
    public partial class MeasureResultChartView : UserControl
    {
        public MeasureResultChartView()
        {
            InitializeComponent();
            //Dispatcher.ShutdownStarted += OnDispatcherShutDownStarted;
        }

        //private void OnDispatcherShutDownStarted(object sender, EventArgs e)
        //{
        //    var disposable = DataContext as IDisposable;
        //    if (!ReferenceEquals(null, disposable))
        //    {
        //        disposable.Dispose();
        //    }
        //}
    }
}
