using OLS.Casy.Core;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using OLS.Casy.Ui.Core.Api;
using OLS.Casy.Ui.Core.UndoRedo;

namespace OLS.Casy.Ui.Core.Services
{
    internal class UndoManager
    {
        object lockref = new object();

        private volatile OmniStack<IUndoItem> _undoStack = new OmniStack<IUndoItem>();
        private volatile OmniStack<IUndoItem> _redoStack = new OmniStack<IUndoItem>();

        public void AddUndoItem(IUndoItem op)
        {
            _undoStack.Push(op);
            _redoStack.Clear();
        }

        public void Undo()
        {
            if (_undoStack.Count > 0)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var op = _undoStack.Pop();
                    op.Undo();
                    _redoStack.Push(op);
                });
            }
        }

        public void Redo()
        {
            if (_redoStack.Count > 0)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var op = _redoStack.Pop();
                    op.Redo();
                    _undoStack.Push(op);
                });
            }
        }

        public bool CanUndo
        {
            get { return (_undoStack.Count > 0); }
        }

        public bool CanRedo
        {
            get { return _redoStack.Count > 0; }
        }

        public void Clear(/*object o*/)
        {
            lock (lockref)
            {
                this._undoStack.Clear();
                this._redoStack.Clear();
                /*
            
                List<UndoItem> toRemove = new List<UndoItem>();
                foreach (var item in _undoStack)
                {
                    FindUndoItemsInternal(item, o, ref toRemove);
                }

                List<UndoItemGroup> groupsToRemove = new List<UndoItemGroup>();
                foreach (var undoItem in toRemove)
                {
                    if (_undoStack.Contains(undoItem))
                    {
                        _undoStack.Remove(undoItem);
                    }
                    else
                    {
                        foreach (var groupItem in _undoStack.OfType<UndoItemGroup>())
                        {
                            groupItem.RemoveUndoItem(undoItem);

                            if (groupItem.GroupItems.Count() == 0)
                            {
                                groupsToRemove.Add(groupItem);
                            }
                        }
                    }
                }

                foreach (var group in groupsToRemove)
                {
                    _undoStack.Remove(group);
                }

                toRemove.Clear();
                foreach (var item in _redoStack)
                {
                    FindUndoItemsInternal(item, o, ref toRemove);
                }

                groupsToRemove.Clear();
                foreach (var undoItem in toRemove)
                {
                    if (_redoStack.Contains(undoItem))
                    {
                        _redoStack.Remove(undoItem);
                    }
                    else
                    {
                        foreach (var groupItem in _redoStack.OfType<UndoItemGroup>())
                        {
                            groupItem.RemoveUndoItem(undoItem);

                            if (groupItem.GroupItems.Count() == 0)
                            {
                                groupsToRemove.Add(groupItem);
                            }
                        }
                    }
                }

                foreach (var group in groupsToRemove)
                {
                    _redoStack.Remove(group);
                }
            
            */
            }
        }

        private void FindUndoItemsInternal(IUndoItem undoItem, object o, ref List<IUndoItem> items)
        {
            UndoItemGroup undoItemGroup = undoItem as UndoItemGroup;
            if (undoItemGroup == null)
            {
                if (undoItem.ModelObject == o)
                {
                    items.Add(undoItem);
                }
            }
            else
            {
                foreach (var groupItem in undoItemGroup.GroupItems)
                {
                    this.FindUndoItemsInternal(groupItem, o, ref items);
                }
            }
        }


        public bool ContainsUndoItems(object o)
        {
            foreach (var undoItem in this._undoStack)
            {
                if(this.ContainsUndoItemsInternal(undoItem, o))
                {
                    return true;
                }
            }
            return false;
        }

        private bool ContainsUndoItemsInternal(IUndoItem undoItem, object o)
        {
            if(undoItem == null)
            {
                return false;
            }

            UndoItemGroup undoItemGroup = undoItem as UndoItemGroup;
            if (undoItemGroup == null)
            {
                return undoItem.ModelObject == o || object.ReferenceEquals(undoItem.ModelObject, o);
            }
            else
            {
                foreach (var groupItem in undoItemGroup.GroupItems)
                {
                    if(this.ContainsUndoItemsInternal(groupItem, o))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void UndoAll(/*object o*/)
        {
            while (this._undoStack.Count > 0)
            {
                var op = _undoStack.Pop();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    op.Undo();
                });
            }

            /*
            List<UndoItem> doneUndoItems = new List<UndoItem>();
            lock (lockref)
            {
                //foreach (var undoItem in this._undoStack)
                //{
                //    this.UndoAllInternal(undoItem, o, ref doneUndoItems);
                //}
                for(int i = this._undoStack.Count; i > 0; i--)
                {
                    this.UndoAllInternal(this._undoStack.ElementAt(i - 1), o, ref doneUndoItems);
                }
            
                List<UndoItemGroup> doneUndoItemGroups = new List<UndoItemGroup>();
                foreach(var undoItem in doneUndoItems)
                {
                    if (_undoStack.Contains(undoItem))
                    {
                        _undoStack.Remove(undoItem);
                    }
                    else
                    {
                        foreach(var groupItem in _undoStack.OfType<UndoItemGroup>())
                        {
                            groupItem.RemoveUndoItem(undoItem);

                            if(groupItem.GroupItems.Count() == 0)
                            {
                                doneUndoItemGroups.Add(groupItem);
                            }
                        }
                    }
                }

                foreach(var groupItem in doneUndoItemGroups)
                {
                    _undoStack.Remove(groupItem);
                }
            }
            */
        }

        /*
        private void UndoAllInternal(UndoItem undoItem, object o, ref List<UndoItem> doneUndoItems)
        {
            UndoItemGroup undoItemGroup = undoItem as UndoItemGroup;
            if (undoItemGroup == null)
            {
                if(undoItem.ModelObject == o)
                { 
                    Application.Current.Dispatcher.Invoke(() => undoItem.Undo());
                    doneUndoItems.Add(undoItem);
                }
            }
            else
            {
                foreach (var groupItem in undoItemGroup.GroupItems)
                {
                    this.UndoAllInternal(groupItem, o, ref doneUndoItems);
                }
            }
        }
        */

        public IEnumerable<object> GetModifiedObjects()
        {
            List<object> result = new List<object>();
            foreach(var undoItem in this._undoStack)
            {
                this.GetModifiedObjectsInternal(undoItem, ref result);
            }
            
            return result.Distinct().ToList();
        }

        private void GetModifiedObjectsInternal(IUndoItem undoItem, ref List<object> result)
        {
            UndoItemGroup undoItemGroup = undoItem as UndoItemGroup;
            if (undoItemGroup == null)
            {
                result.Add(undoItem.ModelObject);
            }
            else
            {
                foreach(var groupItem in undoItemGroup.GroupItems)
                {
                    this.GetModifiedObjectsInternal(groupItem, ref result);
                }
            }
        }
    }
}
