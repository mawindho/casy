using OLS.Casy.Ui.Core.Api;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using OLS.Casy.Ui.Core.UndoRedo;

namespace OLS.Casy.Ui.Core.Services
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IUIProjectManager))]
    public class UIProjectManager : IUIProjectManager
    {
        private readonly UndoManager _undoManager = new UndoManager();

        public bool IgnoreUndoRedo { get; set; }

        public event EventHandler UndoRedoStackChanged;

        private bool _isUndoGroupActive;
        private UndoItemGroup _currentUndoItemGroup;

        public void StartUndoGroup()
        {
            _isUndoGroupActive = true;
            if(_currentUndoItemGroup == null)
                _currentUndoItemGroup = new UndoItemGroup();
        }

        public void SubmitUndoGroup()
        {
            _isUndoGroupActive = false;
            if (_currentUndoItemGroup == null || !_currentUndoItemGroup.HasUndoItems()) return;
            Submit(_currentUndoItemGroup);
            _currentUndoItemGroup = null;
        }

        public void Submit(IUndoItem op)
        {
            if (IgnoreUndoRedo || !op.PrepareCommand()) return;
            if(_isUndoGroupActive)
            {
                _currentUndoItemGroup.AddUndoItem(op);
            }
            else
            { 
                _undoManager.AddUndoItem(op);
                RaiseUndoRedoStackChanged();
                op.DoCommand();
            }
        }

        public void Undo()
        {
            if (!_undoManager.CanUndo) return;
            IgnoreUndoRedo = true;
            _undoManager.Undo();
            RaiseUndoRedoStackChanged();
            IgnoreUndoRedo = false;
        }

        public void Redo()
        {
            if (!_undoManager.CanRedo) return;
            IgnoreUndoRedo = true;
            _undoManager.Redo();
            RaiseUndoRedoStackChanged();
            IgnoreUndoRedo = false;
        }

        public bool CanUndo => _undoManager.CanUndo;

        public bool CanRedo => _undoManager.CanRedo;

        public void Clear()
        {
            _undoManager.Clear();
            RaiseUndoRedoStackChanged();
        }

        public bool IsModified(object o)
        {
            return _undoManager.ContainsUndoItems(o);
        }

        public void UndoAll()
        {
            IgnoreUndoRedo = true;
            _undoManager.UndoAll();
            RaiseUndoRedoStackChanged();
            IgnoreUndoRedo = false;
        }

        public IEnumerable<object> GetModifiedObjects()
        {
            return _undoManager.GetModifiedObjects();
        }

        public void SendUIEdit(object modelObject, string propStr, object newValue, object oldValue)
        {
            var op = new UIPropertyUndoItem
            {
                ModelObject = modelObject, PropertyStr = propStr, NewValue = newValue, OldValue = oldValue
            };
            Submit(op);
        }

        public void SendUIEdit(object modelObject, string propStr, object newValue)
        {
            SendUIEdit(modelObject, propStr, newValue, null);
        }

        private void RaiseUndoRedoStackChanged()
        {
            UndoRedoStackChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
