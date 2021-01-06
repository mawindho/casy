using System;
using System.Windows;

namespace OLS.Casy.Ui.Core.Api
{
    public interface ISaveFileDialogService : IDisposable
    {
        string Filter { get; set; }
        string Title { get; set; }
        string InitialDirectory { get; set; }
        string FileName { get; set; }
        bool? ShowDialog();
    }
}
