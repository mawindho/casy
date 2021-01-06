using System;

namespace OLS.Casy.Ui.Core.Api
{
    public interface IOpenFileDialogService : IDisposable
    {
        string Filter { get; set; }
        string Title { get; set; }
        bool Multiselect { get; set; }
        string InitialDirectory { get; set; }
        string[] FileNames { get; set; }
        bool? ShowDialog();
    }
}
