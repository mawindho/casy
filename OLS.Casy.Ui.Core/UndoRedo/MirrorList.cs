using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;

namespace OLS.Casy.Ui.Core.UndoRedo
{
    //public delegate void SubmitUndoItemCollectionChanged(NotifyCollectionChangedEventArgs info);

    public sealed class MirrorList<TViewModel, TModel> : Observable, IList<TViewModel>, IList, ICollection<TViewModel>, INotifyCollectionChanged, IDisposable
    {
        private IList<TModel> _modelList;
        private List<TViewModel> _mirrorList;
        private Queue<NotifyCollectionChangedEventArgs> _changes = new Queue<NotifyCollectionChangedEventArgs>();
        private object _changesLock = new object();
        private object _mirrorLock = new object();

        private event EventHandler<NotifyCollectionChangedEventArgs> _submitComandoCollectionChangedEvent;

        private IMirrorListConversor<TViewModel, TModel> _mirrorItemConversor;

        public MirrorList(IList<TModel> baseList, IMirrorListConversor<TViewModel, TModel> mirrorItemConversor)
        {
            if (baseList == null)
            {
                throw new ArgumentNullException("baseList");
            }
            this._mirrorItemConversor = mirrorItemConversor;


            _modelList = baseList;
            ICollection collection = _modelList as ICollection;
            INotifyCollectionChanged changeable = _modelList as INotifyCollectionChanged;
            if (changeable == null)
            {
                throw new ArgumentException("List must support "
                  + "INotifyCollectionChanged", "baseList");
            }

            if (collection != null)
            {
                Monitor.Enter(collection.SyncRoot);
            }
            try
            {
                ResetList();
                changeable.CollectionChanged += new NotifyCollectionChangedEventHandler(changeable_CollectionChanged);
            }
            finally
            {
                if (collection != null)
                {
                    Monitor.Exit(collection.SyncRoot);
                }
            }
        }

        void changeable_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ProcessChange(e);
        }


        public event EventHandler<NotifyCollectionChangedEventArgs> SubmitUndoItem
        {
            add => _submitComandoCollectionChangedEvent += value;
            remove => _submitComandoCollectionChangedEvent -= value;
        }

        private void ResetList()
        {
            _mirrorList = new List<TViewModel>();
            int count = 0;
            //llenamos la lista a partir de los originales
            foreach (TModel res in _modelList)
            {
                TViewModel viewItem = _mirrorItemConversor.CreateViewItem(res, count);
                count++;
                _mirrorList.Add(viewItem);
            }
        }

        private void RecordChange(NotifyCollectionChangedEventArgs changeInfo)
        {
            bool isFirstChange = false;
            lock (_changesLock)
            {
                isFirstChange = (_changes.Count == 0);
                _changes.Enqueue(changeInfo);
            }
            if (isFirstChange)
            {
                OnCollectionDirty();
            }
        }

        private void OnCollectionDirty()
        {
            // This is virtual so that derived classes can eg. redirect
            // this to a different thread...
            ProcessChanges();
        }

        private void ProcessChanges()
        {
            bool locked = false;
            Monitor.Enter(_changesLock);
            try
            {
                locked = true;
                while (_changes.Count > 0)
                {
                    NotifyCollectionChangedEventArgs info =
                        _changes.Dequeue();
                    Monitor.Exit(_changesLock);
                    locked = false;

                    // ProcessChange occurs outside the ChangesLock,
                    // permitting other threads to queue things up behind us.
                    // Note that this means that if your change producer is
                    // running faster than your change consumer, this
                    // method may never exit.  But it does avoid making the
                    // producer wait for the consumer to process.
                    ProcessChange(info);

                    Monitor.Enter(_changesLock);
                    locked = true;
                }
            }
            finally
            {
                if (locked)
                {
                    Monitor.Exit(_changesLock);
                }
            }
        }

        private void ProcessChange(NotifyCollectionChangedEventArgs info)
        {
            lock (_mirrorLock)
            {
                TViewModel viewItem;
                bool changedCount = true;

                NotifyCollectionChangedEventArgs infoMirror = null;
                IList newItems;
                IList oldItems;

                switch (info.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        if (info.OldItems != null)
                        {
                            throw new ArgumentException("Old items present in "
                              + "Add?!", "info");
                        }
                        if (info.NewItems == null)
                        {
                            throw new ArgumentException("New items not present "
                              + "in Add?!", "info");
                        }

                        newItems = new List<TViewModel>();
                        for (int itemIndex = 0; itemIndex < info.NewItems.Count;
                            ++itemIndex)
                        {
                            viewItem = _mirrorItemConversor.CreateViewItem((TModel)info.NewItems[itemIndex], info.NewStartingIndex + itemIndex);
                            newItems.Add(viewItem);
                            _mirrorList.Insert(info.NewStartingIndex + itemIndex, viewItem);

                            //ojo, aquí las notificaciones son de uno en uno (a diferencia de wpf), por eso ponemos lugo infoMirror=null
                            infoMirror = new NotifyCollectionChangedEventArgs(info.Action, viewItem, info.NewStartingIndex);
                            if (infoMirror != null)
                                OnCollectionChanged(infoMirror);

                        }
                        infoMirror = null;

                        break;
                    case NotifyCollectionChangedAction.Remove:
                        if (info.OldItems == null)
                        {
                            throw new ArgumentException("Old items not present "
                              + "in Remove?!", "info");
                        }
                        if (info.NewItems != null)
                        {
                            throw new ArgumentException("New items present in "
                              + "Remove?!", "info");
                        }
                        oldItems = new List<TViewModel>();
                        for (int itemIndex = 0; itemIndex < info.OldItems.Count;
                            ++itemIndex)
                        {
                            viewItem = _mirrorList[itemIndex];
                            oldItems.Add(viewItem);
                            _mirrorList.RemoveAt(info.OldStartingIndex);

                            infoMirror = new NotifyCollectionChangedEventArgs(info.Action, viewItem, info.OldStartingIndex);
                            if (infoMirror != null)
                                OnCollectionChanged(infoMirror);
                        }
                        infoMirror = null;
                        
                        break;

                    // this Actions are not implemented to mantain compatibility with Silverlight 2.0
                    //case NotifyCollectionChangedAction.Move:
                    //    if (info.NewItems == null)
                    //    {
                    //        throw new ArgumentException("New items not present "
                    //          + "in Move?!", "info");
                    //    }
                    //    if (info.NewItems.Count != 1)
                    //    {
                    //        throw new NotSupportedException("Move operations "
                    //          + "only supported for one item at a time.");
                    //    }
                    //    _MirrorList.RemoveAt(info.OldStartingIndex);
                    //    viewItem = _MirrorItemConversor.GetViewItem((R)info.NewItems[0], info.NewStartingIndex );
                    //    _MirrorList.Insert(info.NewStartingIndex, viewItem);
                    //    changedCount = false;
                    //    break;
                    case NotifyCollectionChangedAction.Replace:
                        if (info.OldItems == null)
                        {
                            throw new ArgumentException("Old items not present "
                              + "in Replace?!", "info");
                        }
                        if (info.NewItems == null)
                        {
                            throw new ArgumentException("New items not present "
                              + "in Replace?!", "info");
                        }
                        for (int itemIndex = 0; itemIndex < info.NewItems.Count;
                            ++itemIndex)
                        {
                            _mirrorList[info.NewStartingIndex + itemIndex]
                              = _mirrorItemConversor.CreateViewItem((TModel)info.NewItems[itemIndex], info.NewStartingIndex + itemIndex);
                        }
                        changedCount = false;
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        ICollection collection = _modelList as ICollection;
                        if (collection != null)
                        {
                            Monitor.Enter(collection.SyncRoot);
                        }
                        try
                        {
                            lock (_changesLock)
                            {
                                ResetList();
                                _changes.Clear();
                            }
                        }
                        finally
                        {
                            if (collection != null)
                            {
                                Monitor.Exit(collection.SyncRoot);
                            }
                        }
                        infoMirror = new NotifyCollectionChangedEventArgs(info.Action);
                        break;
                    default:
                        throw new ArgumentException("Unrecognised collection "
                          + "change operation.", "info");
                }

                if (infoMirror != null)
                    OnCollectionChanged(infoMirror);
                OnPropertyChanged("Items[]");
                if (changedCount)
                {
                    OnPropertyChanged("Count");
                }
            }
        }

        public object SyncRoot
        {
            get { return _mirrorLock; }
        }

        public int IndexOf(TViewModel item)
        {
            lock (_mirrorLock)
            {
                return _mirrorList.IndexOf(item);
            }
        }

        public TViewModel this[int index]
        {
            get
            {
                lock (_mirrorLock)
                {
                    return _mirrorList[index];
                }
            }
        }
        public bool Contains(TViewModel item)
        {
            lock (_mirrorLock)
            {
                return _mirrorList.Contains(item);
            }
        }

        public bool Contains(TModel model)
        {
            return _modelList.Contains(model);
        }

        public void CopyTo(TViewModel[] array)
        {
            lock (_mirrorLock)
            {
                _mirrorList.CopyTo(array);
            }
        }

        public void CopyTo(TViewModel[] array, int arrayIndex)
        {
            lock (_mirrorLock)
            {
                _mirrorList.CopyTo(array, arrayIndex);
            }
        }

        public int Count
        {
            get
            {
                lock (_mirrorLock)
                {
                    return _mirrorList.Count;
                }
            }
        }

        public IEnumerator<TViewModel> GetEnumerator()
        {
            lock (_mirrorLock)
            {
                foreach (TViewModel item in _mirrorList)
                {
                    yield return item;
                }
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(this, e);
            }
        }

        private void ThrowReadOnly()
        {
            throw new NotSupportedException("Collection is read-only.");
        }

        #region IList<V> Members
        TViewModel IList<TViewModel>.this[int index]
        {
            get
            {
                return this[index];
            }
            set
            {
                ThrowReadOnly();
            }
        }

        void IList<TViewModel>.Insert(int index, TViewModel item)
        {
            ThrowReadOnly();
        }

        public void Insert(int index, TModel modelItem)
        {
            if (_submitComandoCollectionChangedEvent == null)
                ThrowReadOnly();
            else
            {
                NotifyCollectionChangedEventArgs info =
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, modelItem, index);
                _submitComandoCollectionChangedEvent.Invoke(this, info);
            }
        }

        void IList<TViewModel>.RemoveAt(int index)
        {
            if (_submitComandoCollectionChangedEvent == null)
                ThrowReadOnly();
            else
            {
                TModel modelItem = _mirrorItemConversor.GetModelItem(this[index], index);
                NotifyCollectionChangedEventArgs info =
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, modelItem, index);
                _submitComandoCollectionChangedEvent.Invoke(this, info);
            }

        }
        #endregion

        #region ICollection<TViewModel> Members
        void ICollection<TViewModel>.Add(TViewModel item)
        {
            ThrowReadOnly();
        }

        public void Add(TModel model)
        {
            this.Insert(this.Count, model);
        }

        void ICollection<TViewModel>.Clear()
        {
            ThrowReadOnly();
        }

        bool ICollection<TViewModel>.IsReadOnly => (_submitComandoCollectionChangedEvent != null);

        public bool Remove(TViewModel item)
        {
            int index = IndexOf(item);
            (this as IList<TViewModel>).RemoveAt(index);
            return true;
            //ThrowReadOnly();
            //return false;   // never reaches here
        }
        #endregion

        #region IEnumerable Members
        System.Collections.IEnumerator
            System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
        #endregion

        #region IList Members
        int IList.Add(object value)
        {
            if (value is TViewModel)
            {
                var model = _mirrorItemConversor.GetModelItem((TViewModel)value, 0);
                this.Insert(0, model);
                return 0;
            }
            else
            {
                ThrowReadOnly();
                return -1;      // never reaches here
            }
        }

        void IList.Clear()
        {
            ThrowReadOnly();
        }

        bool IList.Contains(object value)
        {
            if(value is TViewModel)
            {
                lock (_mirrorLock)
                {
                    return ((IList)_mirrorList).Contains(value);
                }
            }
            else if(value is TModel)
            {
                return ((IList)_modelList).Contains(value);
            }
            return false;
        }

        int IList.IndexOf(object value)
        {
            lock (_mirrorLock)
            {
                return ((IList)_mirrorList).IndexOf(value);
            }
        }

        void IList.Insert(int index, object value)
        {
            ThrowReadOnly();
        }

        bool IList.IsFixedSize
        {
            get { return ((IList)_mirrorList).IsFixedSize; }
        }

        bool IList.IsReadOnly => (_submitComandoCollectionChangedEvent != null);

        void IList.Remove(object value)
        {
            ThrowReadOnly();
        }

        public void Remove(TModel modelItem)
        {
            if (_submitComandoCollectionChangedEvent == null)
                ThrowReadOnly();
            else
            {
                var index = _modelList.IndexOf(modelItem);
                NotifyCollectionChangedEventArgs info =
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, modelItem, index);
                _submitComandoCollectionChangedEvent.Invoke(this, info);
            }
        }

        void IList.RemoveAt(int index)
        {
            ThrowReadOnly();
        }

        object IList.this[int index]
        {
            get
            {
                lock (_mirrorLock)
                {
                    return ((IList)_mirrorList)[index];
                }
            }
            set
            {
                ThrowReadOnly();
            }
        }
        #endregion

        #region ICollection Members
        void ICollection.CopyTo(Array array, int index)
        {
            lock (_mirrorLock)
            {
                ((IList)_mirrorList).CopyTo(array, index);
            }
        }

        bool ICollection.IsSynchronized
        {
            get { return true; }
        }
        #endregion

        private bool disposedValue = false; // To detect redundant calls

        protected void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    INotifyCollectionChanged changeable = _modelList as INotifyCollectionChanged;
                    if (changeable != null)
                    {
                        changeable.CollectionChanged -= new NotifyCollectionChangedEventHandler(changeable_CollectionChanged);
                    }
                }

                disposedValue = true;
            }
        }

        ~MirrorList()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
