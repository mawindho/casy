using System.Collections;
using System.Collections.Generic;

namespace OLS.Casy.Core
{
    public class OmniStack<T> : IEnumerable<T>
    {
        private List<T> _items = new List<T>();

        public void Push(T item)
        {
            _items.Add(item);
        }

        public T Pop()
        {
            if(_items.Count > 0)
            {
                T temp = _items[_items.Count - 1];
                _items.RemoveAt(_items.Count - 1);
                return temp;
            }
            else
            {
                return default(T);
            }
        }

        public void Remove(T item)
        {
            _items.Remove(item);
        }

        public int Count
        {
            get { return _items.Count; }
        }

        public void Clear()
        {
            _items.Clear();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)_items).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)_items).GetEnumerator();
        }
    }
}
