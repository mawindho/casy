using MahApps.Metro.Controls.Dialogs;
using System.ComponentModel.Composition;

namespace OLS.Casy.Ui.MainControls.Views
{
    /// <summary>
    /// Interaktionslogik für SettingsContainerView.xaml
    /// </summary>
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(SettingsContainerView))]
    public partial class SettingsContainerView : CustomDialog
    {
        public SettingsContainerView()
        {
            InitializeComponent();
        }
    }
}
