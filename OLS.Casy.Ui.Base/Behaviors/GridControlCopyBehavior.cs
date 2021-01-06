using DevExpress.Xpf.Grid;
using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Input;

namespace OLS.Casy.Ui.Base.Behaviors
{
    public class GridControlCopyBehavior : Behavior<GridControl>
    {
        public static readonly DependencyProperty CopyCommandProperty = DependencyProperty.Register("CopyCommand", typeof(ICommand), typeof(GridControlCopyBehavior), null);
        public ICommand CopyCommand
        {
            get { return (ICommand)GetValue(CopyCommandProperty); }
            set { SetValue(CopyCommandProperty, value); }
        }
        GridControl Grid
        {
            get
            {
                return AssociatedObject as GridControl;
            }
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            Grid.PastingFromClipboard += Grid_PastingFromClipboard;
            Grid.CopyingToClipboard += Grid_CopyingToClipboard;
            //Grid.Loaded += Grid_Loaded;
        }

        private void Grid_CopyingToClipboard(object sender, CopyingToClipboardEventArgs e)
        {
            //Clipboard.Clear();

            //Clipboard.SetData(DataFormats.Text, GetRowData());
            //e.Handled = true;

            ((TableView)Grid.View).Grid.CopySelectedItemsToClipboard();

            //CopyCommand.Execute(((TableView)Grid.View).GetSelectedCells());
        }

        private object GetRowData()
        {
            string result = string.Empty;
            foreach(var item in Grid.SelectedItems)
            {

            }
            return result;
        }

        //void Grid_Loaded(object sender, RoutedEventArgs e)
        //{
        //    ObservableCollection<string> fieldNames = new ObservableCollection<string>();
        //    foreach (GridColumn column in Grid.Columns)
        //    {
        //        fieldNames.Add(column.FieldName);
        //    }
        //    FieldNames = fieldNames;
        //}

        void Grid_PastingFromClipboard(object sender, PastingFromClipboardEventArgs e)
        {
            //CopyCommand.Execute(((TableView)Grid.View).GetSelectedCells()); // -- call the command and pass selected cells as a parameter
        }

        //public static readonly DependencyProperty FieldNamesProperty = DependencyProperty.Register("FieldNames", typeof(ObservableCollection<string>), typeof(GridHelper), null);
        //public ObservableCollection<string> FieldNames
        //{
        //    get { return (ObservableCollection<string>)GetValue(FieldNamesProperty); }
        //    set { SetValue(FieldNamesProperty, value); }
        //}
    }
}
