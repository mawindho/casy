using System;
using System.Collections.Generic;

namespace OLS.Casy.Ui.Core.Api
{
    public interface IUIProjectManager
    {
        bool IgnoreUndoRedo { get; set; }
        void StartUndoGroup();
        void SubmitUndoGroup();
        void Submit(IUndoItem op);
        void Undo();
        void Redo();
        bool CanUndo { get; }
        bool CanRedo { get; }
        void Clear(/*object o*/);

        bool IsModified(object o);
        void UndoAll(/*object o*/);
        IEnumerable<object> GetModifiedObjects();

        event EventHandler UndoRedoStackChanged;

        void SendUIEdit(object modelObject, string propStr, object newValue, object oldValue);
        void SendUIEdit(object modelObject, string propStr, object newValue);
    }
}
