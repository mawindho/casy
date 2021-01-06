using System.Collections.Generic;
using OLS.Casy.Ui.Core.Api;

namespace OLS.Casy.Ui.Core.UndoRedo
{
    public class UndoItemGroup : UndoItem
    {
        private readonly List<IUndoItem> _groupItems;

        public UndoItemGroup()
        {
            _groupItems = new List<IUndoItem>();
        }

        public void AddUndoItem(IUndoItem undoItem)
        {
            _groupItems.Add(undoItem);
            undoItem.DoCommand();
        }

        public void RemoveUndoItem(IUndoItem undoItem)
        {
            _groupItems.Remove(undoItem);
        }

        public IEnumerable<IUndoItem> GroupItems => _groupItems;

        public bool HasUndoItems()
        {
            return _groupItems.Count > 0;
        }

        public override bool DoCommand()
        {
            return true;
        }

        public override void Undo()
        {
            for(int i = _groupItems.Count; i > 0; i--)
            {
                var undoItem = _groupItems[i-1];
                undoItem.Undo();
            }
        }

        public override void Redo()
        {
            for(int i = _groupItems.Count; i > 0; i--)
            {
                var undoItem = _groupItems[i - 1];
                undoItem.Redo();
            }
        }
    }
}
