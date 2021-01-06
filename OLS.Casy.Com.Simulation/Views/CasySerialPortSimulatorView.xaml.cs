using OLS.Casy.Ui.Base.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
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

namespace OLS.Casy.Com.Simulation.Views
{
    /// <summary>
    /// Interaktionslogik für CasySerialPortSimulatorView.xaml
    /// </summary>
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(CasySerialPortSimulatorView))]
    public partial class CasySerialPortSimulatorView : Window
    {
        public CasySerialPortSimulatorView()
        {
            InitializeComponent();
        }
    }
}
