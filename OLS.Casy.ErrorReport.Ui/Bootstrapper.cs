using System.Windows;

namespace OLS.Casy.ErrorReport.Ui
{
    public class Bootstrapper
    {
        private MainViewModel _mainViewModel;

        public void Startup()
        {
            _mainViewModel = new MainViewModel();
            var mainView = new MainWindow();
            Application.Current.MainWindow = mainView;
            Application.Current.MainWindow.DataContext = this._mainViewModel;
            Application.Current.MainWindow.Show();
        }
    }
}
