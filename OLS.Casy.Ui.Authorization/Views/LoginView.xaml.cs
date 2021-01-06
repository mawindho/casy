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
using System.Windows.Threading;

namespace OLS.Casy.Ui.Authorization.Views
{
    /// <summary>
    /// Interaktionslogik für LoginDialogView.xaml
    /// </summary>
    [PartCreationPolicy(CreationPolicy.NonShared)]
	[Export("LoginView", typeof(UserControl))]
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
            this.IsVisibleChanged += UserControl_IsVisibleChanged;
        }

        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.Visibility == Visibility.Visible)
            {
                this.Dispatcher.BeginInvoke((Action)delegate
                {
                    Keyboard.Focus(txtUserName);
                }, DispatcherPriority.Render);
            }
        }
    }
}
