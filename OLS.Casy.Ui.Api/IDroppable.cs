namespace OLS.Casy.Ui.Api
{
    public interface IDroppable
    {
        int DraggableOverLocation { get; set; }
        void PerformDrop(IDraggable draggable);
        int DisplayOrder { get; }
    }
    
}
