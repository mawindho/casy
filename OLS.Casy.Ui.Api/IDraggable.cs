namespace OLS.Casy.Ui.Api
{
    public interface IDraggable
    {
        bool IsDragging { get; set; }
        double DragPositionLeft { get; set; }
        double DragPositionTop { get; set; }
        int DisplayOrder { get; }
    }
}
