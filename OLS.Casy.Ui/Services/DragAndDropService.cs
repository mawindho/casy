using OLS.Casy.Ui.Api;
using System;
using System.ComponentModel.Composition;

namespace OLS.Casy.Ui.Services
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IDragAndDropService))]
    public class DragAndDropService : IDragAndDropService
    {
        private IDraggable _activeDraggable;

        public IDraggable ActiveDraggable
        {
            get { return _activeDraggable; }
            set
            {
                if (value != _activeDraggable)
                {
                    _activeDraggable = value;
                    RaiseActiveDragableChanged();
                }
            }
        }

        public event EventHandler ActiveDragableChanged;

        public event EventHandler<DragCompletedEventArgs> DragCompleted;

        private void RaiseActiveDragableChanged()
        {
            if (ActiveDragableChanged != null)
            {
                ActiveDragableChanged.Invoke(this, EventArgs.Empty);
            }
        }

        public void RaiseDragCompleted(object source = null, bool success = false)
        {
            if (DragCompleted != null)
            {
                DragCompleted.Invoke(source, new DragCompletedEventArgs(success));
            }
        }
    }
}
