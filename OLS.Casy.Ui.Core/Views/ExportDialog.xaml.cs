using MahApps.Metro.Controls.Dialogs;
using OLS.Casy.Ui.Core.Api;
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

namespace OLS.Casy.Ui.Core.Views
{
    /// <summary>
    /// Interaktionslogik für ExportDialog.xaml
    /// </summary>
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(IExportDialog))]
    public partial class ExportDialog : CustomDialog, IExportDialog
    {
        public ExportDialog()
        {
            InitializeComponent();
        }
    }
}
