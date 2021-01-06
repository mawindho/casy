using System;

namespace OLS.Casy.Ui.Api
{
    public class DragCompletedEventArgs : EventArgs
    {
        public DragCompletedEventArgs(bool success)
        {
            this.Success = success;
        }
        public bool Success { get; private set; }
    }

    public interface IDragAndDropService
    {
        IDraggable ActiveDraggable { get; set; }
        event EventHandler ActiveDragableChanged;
        void RaiseDragCompleted(object source = null, bool success = false);
        event EventHandler<DragCompletedEventArgs> DragCompleted;
    }
    
}
