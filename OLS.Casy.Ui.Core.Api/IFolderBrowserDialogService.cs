using System;
using System.Windows;

namespace OLS.Casy.Ui.Core.Api
{
    public interface IFolderBrowserDialogService
    {
        string Description { get; set; }
        Environment.SpecialFolder RootFolder { get; set; }
        string SelectedPath { get; set; }
        bool ShowNewFolderButton { get; set; }
        bool? ShowDialog();
    }
}
