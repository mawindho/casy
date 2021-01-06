using MahApps.Metro.Controls.Dialogs;
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

namespace OLS.Casy.Ui.MainControls.Views
{
    /// <summary>
    /// Interaktionslogik für ToolboxView.xaml
    /// </summary>
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(ToolboxView))]
    public partial class ToolboxView : CustomDialog
    {
        public ToolboxView()
        {
            InitializeComponent();
        }
    }
}
