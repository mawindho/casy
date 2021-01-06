using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace OLS.Casy.Core
{
    /// <summary>
    /// Extension of the <see cref="System.Collections.ObjectModel.ObservableCollection{T}"/>.
    /// Contains functionality to add a range of objects without firing property changed events for each single item
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SmartCollection<T> : ObservableCollection<T>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public SmartCollection()
            : base()
        {
        }

        /// <summary>
        /// Constructor to intialize from an existing <see cref="List{T}"/>
        /// </summary>
        /// <param name="list"><see cref="List{T}"/> with inital value</param>
        public SmartCollection(List<T> list)
            : base(list)
        {
        }

        /// <summary>
        /// Constructor to intialize from an existing <see cref="IEnumerable{T}"/>
        /// </summary>
        /// <param name="list"><see cref="IEnumerable{T}"/> with inital value</param>
        public SmartCollection(IEnumerable<T> list)
            :base(list)
        {
        }

        /// <summary>
        /// Adds a range of objects without firing collection changed events for every single insert
        /// </summary>
        /// <param name="range"><see cref="IEnumerable{T}"/> of objects to be inserted</param>
        public void AddRange(IEnumerable<T> range)
        {
            this.AddRange(range, null);
        }

        private void AddRange(IEnumerable<T> range, IList<T> removedItems = null)
        {
            if(range == null)
            {
                throw new ArgumentNullException("range");
            }

            List<T> addedItems = new List<T>();
            if (range != null && range.Count() > 0)
            {
                foreach (var item in range)
                {
                    Items.Add(item);
                    addedItems.Add(item);
                }
            }
            this.OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Count"));
            this.OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Item[]"));

            //if (removedItems == null)
            //{
                //this.OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Add, changedItems: addedItems));
            //}
            //else
            //{
                this.OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
            //}
        }

        public void InsertRange(int index, IEnumerable<T> range)
        {
            if (range == null)
            {
                throw new ArgumentNullException("range");
            }

            if(index < 0 || index > this.Count)
            {
                throw new IndexOutOfRangeException();
            }

            List<T> addedItems = new List<T>();
            if (range != null && range.Count() > 0)
            {
                for (int i = range.Count(); i > 0; i--)
                {
                    var item = range.ElementAt(i - 1);
                    Items.Insert(index, item);
                    addedItems.Add(item);
                }
            }
            this.OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Count"));
            this.OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Item[]"));
            //this.OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Add, changedItems: addedItems));
            this.OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
        }

        public void RemoveRange(IEnumerable<T> range)
        {
            if (range == null)
            {
                throw new ArgumentNullException("range");
            }

            List<T> removedItems = new List<T>();
            if (range != null && range.Count() > 0)
            {
                foreach (var item in range)
                {
                    Items.Remove(item);
                    removedItems.Add(item);
                }
            }
            this.OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Count"));
            this.OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Item[]"));
            //this.OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Remove, changedItems: removedItems));
            this.OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Resets the items of the collection.
        /// </summary>
        /// <param name="range">New <see cref="IEnumerable{T}"/> of items</param>
        public void Reset(IEnumerable<T> range)
        {
            if (range == null)
            {
                throw new ArgumentNullException("range");
            }

            var removedItems = new List<T>();
            removedItems.AddRange(this.Items.ToArray());
            this.Items.Clear();

            this.AddRange(range, removedItems);
        }


        /// <summary>
        /// Adds an item sorted using binary search
        /// </summary>
        /// <param name="item"></param>
        public void AddSorted(T item)
        {
            if(item == null)
            {
                throw new ArgumentNullException("item");
            }

            var items = base.Items as List<T>;
            var index = items.BinarySearch(item);
            if (index < 0) index = ~index;

            base.InsertItem(index, item);
        }
    }
}
