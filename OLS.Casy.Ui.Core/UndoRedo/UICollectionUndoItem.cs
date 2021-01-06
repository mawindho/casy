using System;
using System.Collections;
using System.Collections.Specialized;
using OLS.Casy.Core;

namespace OLS.Casy.Ui.Core.UndoRedo
{
    public class UICollectionUndoItem : UndoItem
    {
        private object _undoItemLock = new object();

        public UICollectionUndoItem(IList modelList)
        {
            ModelList = modelList;
        }

        private NotifyCollectionChangedEventArgs _infoUndo;

        public NotifyCollectionChangedEventArgs Info { get; set; }

        public IList ModelList { get; }
       

        public override bool DoCommand()
        {
            return DoCollectionChange(Info);
        }

        public bool DoCollectionChange(NotifyCollectionChangedEventArgs infoComm)
        {
            switch (infoComm.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (infoComm.OldItems != null)
                    {
                        throw new ArgumentException("Old items present in "
                          + "Add?!", "info");
                    }
                    if (infoComm.NewItems == null)
                    {
                        throw new ArgumentException("New items not present "
                          + "in Add?!", "info");
                    }

                    lock (_undoItemLock)
                    {
                        ModelList.Add(infoComm.NewItems[0]);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (infoComm.OldItems == null)
                    {
                        throw new ArgumentException("Old items not present "
                          + "in Remove?!", "info");
                    }
                    if (infoComm.NewItems != null)
                    {
                        throw new ArgumentException("New items present in "
                          + "Remove?!", "info");
                    }

                    if (ModelList != null)
                    {
                        foreach (var oldItem in infoComm.OldItems)
                        {
                            using(TimedLock.Lock(_undoItemLock))
                            {
                                ModelList.Remove(oldItem);
                            }

                            //if (infoComm.OldStartingIndex <= ModelList.Count)
                            //{
                            //    ModelList.RemoveAt(infoComm.OldStartingIndex);
                            //}
                        }
                    }
                    break;
                //case NotifyCollectionChangedAction.Move:
                //    if (infoComm.NewItems == null)
                //    {
                //        throw new ArgumentException("New items not present "
                //          + "in Move?!", "info");
                //    }
                //    if (infoComm.NewItems.Count != 1)
                //    {
                //        throw new NotSupportedException("Move operations "
                //          + "only supported for one item at a time.");
                //    }
                //    ItemsResource.RemoveAt(infoComm.OldStartingIndex);
                //    ItemsResource.Insert(infoComm.NewStartingIndex, infoComm.NewItems[0]);
                //    break;
                case NotifyCollectionChangedAction.Replace:
                    if (infoComm.OldItems == null)
                    {
                        throw new ArgumentException("Old items not present "
                          + "in Replace?!", "info");
                    }
                    if (infoComm.NewItems == null)
                    {
                        throw new ArgumentException("New items not present "
                          + "in Replace?!", "info");
                    }

                    lock (_undoItemLock)
                    {
                        for (var itemIndex = 0;
                            itemIndex < infoComm.NewItems.Count;
                            ++itemIndex)
                        {
                            ModelList[infoComm.NewStartingIndex + itemIndex]
                                = infoComm.NewItems[itemIndex];
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    break;
                default:
                    throw new ArgumentException("Unrecognised collection "
                      + "change operation.", "info");
            }

            return true;
        }

        NotifyCollectionChangedEventArgs GetInfoUndo(NotifyCollectionChangedEventArgs infoComm)
        {
            switch (infoComm.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    return new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                        infoComm.NewItems[0], infoComm.NewStartingIndex);
                case NotifyCollectionChangedAction.Remove:

                    //return new NotifyCollectionChangedEventArgs(
                    //    NotifyCollectionChangedAction.Add, modelItem, index);

                    return new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Add,
                        infoComm.OldItems[0],
                        infoComm.OldStartingIndex);
                //case NotifyCollectionChangedAction.Move:
                //    return new NotifyCollectionChangedEventArgs(
                //        NotifyCollectionChangedAction.Add,
                //        infoComm.OldItems,
                //        infoComm.OldStartingIndex);                    
                case NotifyCollectionChangedAction.Replace:
                    return new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Add,
                        infoComm.OldItems[0],
                        infoComm.OldStartingIndex);
                case NotifyCollectionChangedAction.Reset:
                    return new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Reset);
                default:
                    throw new ArgumentException("Unrecognised collection "
                      + "change operation.", "info");
            }
        }

        public override void Undo()
        {
            if (_infoUndo == null)
                _infoUndo = GetInfoUndo(Info);
            DoCollectionChange(_infoUndo);
        }

        public override void Redo()
        {
            DoCollectionChange(Info);
        }
    }
}
