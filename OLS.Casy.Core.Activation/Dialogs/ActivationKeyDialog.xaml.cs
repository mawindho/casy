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

namespace OLS.Casy.Core.Activation.Dialogs
{
    /// <summary>
    /// Interaction logic for CasyMessageDialog.xaml
    /// </summary>
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(ActivationKeyDialog))]
    public partial class ActivationKeyDialog : CustomDialog
    {
        public ActivationKeyDialog()
        {
            InitializeComponent();
        }
    }
}
