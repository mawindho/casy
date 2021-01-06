namespace OLS.Casy.Ui.Core.UndoRedo
{
    public interface IMirrorListConversor<TViewModel, TModel>
    {
        TViewModel CreateViewItem(TModel modelItem, int index);
        TModel GetModelItem(TViewModel viewItem, int index);
    }
}
