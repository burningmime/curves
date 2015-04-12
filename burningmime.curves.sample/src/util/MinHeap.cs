using System;
using System.Collections.Generic;

namespace burningmime.util
{
    public sealed class MinHeap<T>
    {
        private readonly List<T> _values;
        private readonly IComparer<T> _comparer;

        public MinHeap(int capacity = 0, IComparer<T> comparer = null)
        {
            _values = capacity <= 0 ? new List<T>() : new List<T>(capacity);
            _comparer = comparer ?? Comparer<T>.Default;
        }

        public int length { get { return _values.Count; } }
        public void clear() { _values.Clear(); }

        public void push(T item)
        {
            _values.Add(item);
            int pos = length - 1;
            while (pos > 0)
            {
                int parent = (pos - 1) / 2;
                if(isGreater(pos, parent)) break;
                swap(pos, parent);
                pos = parent;
            }
        }

        public T peek() { T result; if(!peek(out result)) throw new InvalidOperationException("Heap is empty"); return result; }
        public bool peek(out T result)
        {
            if(length == 0)
            {
                result = default(T);
                return false;
            }
            result = _values[0];
            return true;
        }

        public T pop() { T result; if (!pop(out result)) throw new InvalidOperationException("Heap is empty"); return result; }
        public bool pop(out T result)
        {
            int len = length;
            if(len == 0)
            {
                result = default(T);
                return false;
            }
            int last = len - 1;
            result = _values[0];
            _values[0] = _values[last];
            _values.RemoveAt(last);
            int max = last / 2;
            int pos = 0;
            while(pos < max)
            {
                int child = 2 * pos + 1;
                if(child < (len - 2) && isGreater(child, child + 1)) child++;
                if(isGreater(child, pos)) break;
                swap(pos, child);
                pos = child;
            }
            return true;
        }

        private bool isGreater(int first, int second) { return _comparer.Compare(_values[first], _values[second]) > 0; }
        private void swap(int first, int second) { T temp = _values[first]; _values[first] = _values[second]; _values[second] = temp; }
    }
}