using System;
using System.Collections;
using System.Collections.Generic;

namespace burningmime.util
{
    public interface IImplicitListNode<T>
        where T : class, IImplicitListNode<T>
    {
        T next { get; set; }
        T prev { get; set; }
    }

    public sealed class ImplicitList<T> : ICollection<T>
        where T : class, IImplicitListNode<T>
    {
        public T head { get; private set; }
        public int Count { get; private set; }
        public bool IsReadOnly { get { return false; } }
        public bool IsEmpty { get { return head == null; } }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        public IEnumerator<T> GetEnumerator()
        {
            T cur = head;
            while(cur != null)
            {
                T next = cur.next; // Cache this here in case the node is removed
                yield return cur;
                cur = next;
            }
        }
        
        public void Add(T item)
        {
            if (head != null)
                head.prev = item;
            item.next = head;
            item.prev = null;
            head = item;
            Count++;
        }

        public void Clear()
        {
            T cur = head;
            head = null;
            while (cur != null)
            {
                T next = cur.next;
                cur.next = null;
                cur.prev = null;
                cur = next;
            }
            Count = 0;
        }

        public bool Contains(T item)
        {
            T cur = head;
            while (cur != null)
            {
                if (cur == item)
                    return true;
                cur = cur.next;
            }
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            T cur = head;
            while (cur != null)
            {
                array[arrayIndex++] = cur;
                cur = cur.next;
            }
        }

        public bool Remove(T item)
        {
            if (item.next != null)
                item.next.prev = item.prev;
            if (item.prev != null)
                item.prev.next = item.next;
            else
                head = item.next;
            item.next = null;
            item.prev = null;
            Count--;
            return true;
        }

        public T Pop()
        {
            if(head == null)
                throw new InvalidOperationException("List is empty");
            T item = head;
            head = item.next;
            item.next = null;
            item.prev = null;
            Count--;
            return item;
        }
    }
}