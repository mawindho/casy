namespace OLS.Casy.Ui.Core.Api
{
    public interface IUndoItem
    {
        bool PrepareCommand();
        bool DoCommand();
        void Undo();
        void Redo();
        object ModelObject { get; }
    }
}
