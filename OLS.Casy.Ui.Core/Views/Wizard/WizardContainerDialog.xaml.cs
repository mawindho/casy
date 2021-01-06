using MahApps.Metro.Controls.Dialogs;
using OLS.Casy.Ui.Base.Api;
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
    /// Interaction logic for WizzardBaseView.xaml
    /// </summary>
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(IWizardContainerDialog))]
    public partial class WizardContainerDialog : CustomDialog, IWizardContainerDialog
    {
        public WizardContainerDialog()
        {
            InitializeComponent();
        }
    }
}
