namespace OLS.Casy.App.Models
{
    public interface ITreeItem
    {
        string Name { get; }
        bool IsSelected { get; set; }
    }
}
