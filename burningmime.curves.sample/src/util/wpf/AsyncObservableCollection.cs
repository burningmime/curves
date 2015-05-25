using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Threading;

namespace burningmime.util.wpf
{
    /// <summary>
    /// A version of <see cref="ObservableCollection{T}"/> that is locked so that it can be accessed by multiple threads. When you enumerate it (foreach),
    /// you will get a snapshot of the current contents. Also the <see cref="CollectionChanged"/> event will be called on the thread that added it if that
    /// thread is a Dispatcher (WPF/Silverlight/WinRT) thread. This means that you can update this from any thread and recieve notifications of those updates
    /// on the UI thread.
    /// 
    /// You can't modify the collection during a callback (on the thread that recieved the callback -- other threads can do whatever they want). This is the
    /// same as <see cref="ObservableCollection{T}"/> -- any code that works with a regular ObservableCollection should continue to work with this one.
    /// </summary>
    [Serializable, DebuggerDisplay("Count = {Count}")]
    public sealed class AsyncObservableCollection<T> : IList<T>, IReadOnlyList<T>, IList, INotifyCollectionChanged, INotifyPropertyChanged, ISerializable
    {
		// we implement IReadOnlyList<T> because ObservableCollection<T> does, and we want to mostly keep API compatability...
		// this collection is NOT read only, but neither is ObservableCollection<T>
		
        private readonly ObservableCollection<T> _collection;       // actual collection
        private readonly ThreadLocal<ThreadView> _threadView;       // every thread has its own view of this collection
        private readonly ReaderWriterLockSlim _lock;                // whenever accessing the collection directly, you must aquire the lock
        private volatile int _version;                              // whenever collection is changed, increment this (should only be changed from within write lock, so no atomic needed)
        
        public AsyncObservableCollection()
        {
            _collection = new ObservableCollection<T>();
            _lock = new ReaderWriterLockSlim();
            _threadView = new ThreadLocal<ThreadView>(() => new ThreadView(this));
			// It was a design decision to NOT implement IDisposable here for disposing the ThreadLocal instance. ThreaLocal has a finalizer
			// so it will be taken care of eventually. Since the cache itself is a weak reference, the only difference between explicitly
			// disposing of it and waiting for finalization will be ~80 bytes per thread of memory in the TLS table that will stay around for
			// an extra couple GC cycles. This is a tiny, tiny cost, and reduces the API complexity of this class.
        }

        public AsyncObservableCollection(IEnumerable<T> collection)
        {
            _collection = new ObservableCollection<T>(collection);
            _lock = new ReaderWriterLockSlim();
            _threadView = new ThreadLocal<ThreadView>(() => new ThreadView(this));
        }

        #region ThreadView -- every thread that acceses this collection gets a unique view of it
        /// <summary>
        /// The "view" that a thread has of this collection. One of these exists for every thread that has accesed this
        /// collection, and a new one is automatically created when a new thread accesses it. Therefore, we can assume
        /// thate everything in here is being called from the correct thread and don't need to worry about threading issues.
        /// </summary>
        private sealed class ThreadView
        {
            // These fields will always be accessed from the correct thread, so no sync issues
            public readonly List<EventArgs> waitingEvents = new List<EventArgs>();    // events waiting to be dispatched
            public bool dissalowReenterancy;                                          // don't allow write methods to be called on the thread that's executing events

            // Private stuff all used for snapshot/enumerator
            private readonly int _threadId;                                           // id of the current thread
            private readonly AsyncObservableCollection<T> _owner;                     // the collection
            private readonly WeakReference<List<T>> _snapshot;                        // cache of the most recent snapshot
            private int _listVersion;                                                 // version at which the snapshot was taken
            private int _snapshotId;                                                  // incremented every time a new snapshot is created
            private int _enumeratingCurrentSnapshot;                                  // # enumerating snapshot with current ID; reset when a snapshot is created

            public ThreadView(AsyncObservableCollection<T> owner)
            {
                _owner = owner;
                _threadId = Thread.CurrentThread.ManagedThreadId;
                _snapshot = new WeakReference<List<T>>(null);
            }

            /// <summary>
            /// Gets a list that's a "snapshot" of the current state of the collection, ie it's a copy of whatever elements
            /// are currently in the collection.
            /// </summary>
            public List<T> getSnapshot()
            {
                Debug.Assert(Thread.CurrentThread.ManagedThreadId == _threadId);
                List<T> list;
                // if we have a cached snapshot that's up to date, just use that one
                if(!_snapshot.TryGetTarget(out list) || _listVersion != _owner._version)
                {
                    // need to create a new snapshot
                    // if nothing is using the old snapshot, we can clear and reuse the existing list instead
                    // of allocating a brand new list. yay for eco-friendly solutions!
                    int enumCount = _enumeratingCurrentSnapshot;
                    _snapshotId++;
                    _enumeratingCurrentSnapshot = 0;

                    _owner._lock.EnterReadLock();
                    try
                    {
                        _listVersion = _owner._version;
                        if(list == null || enumCount > 0)
                        {
                            // if enumCount > 0 here that means something is currently using the instance of list. we create a new list
                            // here and "strand" the old list so the enumerator can finish enumerating it in peace.
                            list = new List<T>(_owner._collection);
                            _snapshot.SetTarget(list);
                        }
                        else
                        {
                            // clear & reuse the old list
                            list.Clear();
                            list.AddRange(_owner._collection);
                        }
                    }
                    finally
                    {
                        _owner._lock.ExitReadLock();
                    }
                }
                return list;
            }

            /// <summary>
            /// Called when an enumerator is allocated (NOT when enumeration begins, because by that point we could've moved onto
            /// a new snapshot).
            /// </summary>
            /// <returns>The ID to pass into <see cref="exitEnumerator"/>.</returns>
            public int enterEnumerator()
            {
                Debug.Assert(Thread.CurrentThread.ManagedThreadId == _threadId);
                _enumeratingCurrentSnapshot++;
                return _snapshotId;
            }

            /// <summary>
            /// Cleans up after an enumerator.
            /// </summary>
            /// <param name="oldId">The value that <see cref="enterEnumerator"/> returns.</param>
            public void exitEnumerator(int oldId)
            {
                // if the enumerator is being disposed from a different thread than the one that creatd it, there's no way
                // to garuntee the atomicity of this operation. if this (EXTREMELY rare) case happens, we'll ditch the list next
                // time we need to make a new snapshot. this can never happen with a regular foreach()
                if(Thread.CurrentThread.ManagedThreadId == _threadId)
                {
                    if(_snapshotId == oldId)
                        _enumeratingCurrentSnapshot--;
                }
            }
        }
        #endregion

        #region Read methods
        public bool Contains(T item)
        {
            _lock.EnterReadLock();
            try
            {
                return _collection.Contains(item);
            } 
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public int Count
        {
            get
            {
                _lock.EnterReadLock(); 
                try
                {
                    return _collection.Count;
                } 
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        public int IndexOf(T item)
        {
            _lock.EnterReadLock();
            try
            {
                return _collection.IndexOf(item);
            } 
            finally
            {
                _lock.ExitReadLock();
            }
        }
        #endregion

        #region Write methods -- VERY repetitive, don't say I didn't warn you
        // ARRRRRRGH!!! C# really needs macros! While it would be possible to do this using closures, it would be a huge performance cost
        // With #define this would look so much nicer and be much easier/less error-prone when it needs to be changed.

        public void Add(T item)
        {
            ThreadView view = _threadView.Value;
            if(view.dissalowReenterancy)
                throwReenterancyException();
            _lock.EnterWriteLock();
            try
            {
                _version++;
                _collection.Add(item);
            }
            catch(Exception)
            {
                view.waitingEvents.Clear();
                throw;
            }
            finally
            {
                _lock.ExitWriteLock();
            } 
            dispatchWaitingEvents(view);
        }

        public void AddRange(IEnumerable<T> items)
        {
            ThreadView view = _threadView.Value;
            if(view.dissalowReenterancy)
                throwReenterancyException();
            _lock.EnterWriteLock();
            try
            {
                _version++;
                foreach(T item in items)
                    _collection.Add(item);
            }
            catch(Exception)
            {
                view.waitingEvents.Clear();
                throw;
            }
            finally
            {
                _lock.ExitWriteLock();
            } 
            dispatchWaitingEvents(view);
        }
        
        int IList.Add(object value)
        {
            ThreadView view = _threadView.Value;
            if(view.dissalowReenterancy)
                throwReenterancyException();
            int result;
            _lock.EnterWriteLock();
            try
            {
                _version++;
                result = ((IList) _collection).Add(value);
            }
            catch(Exception)
            {
                view.waitingEvents.Clear();
                throw;
            }
            finally
            {
                _lock.ExitWriteLock();
            } 
            dispatchWaitingEvents(view);
            return result;
        }

        public void Insert(int index, T item)
        {
            ThreadView view = _threadView.Value;
            if(view.dissalowReenterancy)
                throwReenterancyException();
            _lock.EnterWriteLock();
            try
            {
                _version++;
                _collection.Insert(index, item);
            }
            catch(Exception)
            {
                view.waitingEvents.Clear();
                throw;
            }
            finally
            {
                _lock.ExitWriteLock();
            } 
            dispatchWaitingEvents(view);
        }
        
        public bool Remove(T item)
        {
            ThreadView view = _threadView.Value;
            if(view.dissalowReenterancy)
                throwReenterancyException();
            bool result;
            _lock.EnterWriteLock();
            try
            {
                _version++;
                result = _collection.Remove(item);
            }
            catch(Exception)
            {
                view.waitingEvents.Clear();
                throw;
            }
            finally
            {
                _lock.ExitWriteLock();
            } 
            dispatchWaitingEvents(view);
            return result;
        }

        public void RemoveAt(int index)
        {
            ThreadView view = _threadView.Value;
            if(view.dissalowReenterancy)
                throwReenterancyException();
            _lock.EnterWriteLock();
            try
            {
                _version++;
                _collection.RemoveAt(index);
            }
            catch(Exception)
            {
                view.waitingEvents.Clear();
                throw;
            }
            finally
            {
                _lock.ExitWriteLock();
            } 
            dispatchWaitingEvents(view);
        }

        public void Clear()
        {
            ThreadView view = _threadView.Value;
            if(view.dissalowReenterancy)
                throwReenterancyException();
            _lock.EnterWriteLock();
            try
            {
                _version++;
                _collection.Clear();
            }
            catch(Exception)
            {
                view.waitingEvents.Clear();
                throw;
            }
            finally
            {
                _lock.ExitWriteLock();
            } 
            dispatchWaitingEvents(view);
        }

        public void Move(int oldIndex, int newIndex)
        {
            ThreadView view = _threadView.Value;
            if(view.dissalowReenterancy)
                throwReenterancyException();
            _lock.EnterWriteLock();
            try
            {
                _version++;
                _collection.Move(oldIndex, newIndex);
            }
            catch(Exception)
            {
                view.waitingEvents.Clear();
                throw;
            }
            finally
            {
                _lock.ExitWriteLock();
            } 
            dispatchWaitingEvents(view);
        }
        #endregion

        #region A little bit o' both
        public T this[int index]
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _collection[index];
                } 
                finally
                {
                    _lock.ExitReadLock();
                }
            }

            set
            {
                ThreadView view = _threadView.Value;
                if(view.dissalowReenterancy)
                    throwReenterancyException();
                _lock.EnterWriteLock();
                try
                {
                    _version++;
                    _collection[index] = value;
                }
                catch(Exception)
                {
                    view.waitingEvents.Clear();
                    throw;
                }
                finally
                {
                    _lock.ExitWriteLock();
                } 
                dispatchWaitingEvents(view);
            }
        }
        #endregion

        #region GetEnumerator and related methods that work on snapshots
        public IEnumerator<T> GetEnumerator()
        {
            ThreadView view = _threadView.Value; 
            return new EnumeratorImpl(view.getSnapshot(), view);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            // don't need to worry about re-entry/other iterators here since we're at the bottom of the stack
            _threadView.Value.getSnapshot().CopyTo(array, arrayIndex);
        }

        public T[] ToArray()
        {
            // don't need to worry about re-entry/other iterators here since we're at the bottom of the stack
            return _threadView.Value.getSnapshot().ToArray();
        }

        private sealed class EnumeratorImpl : IEnumerator<T>
        {
            private readonly ThreadView _view;
            private readonly int _myId;
            private List<T>.Enumerator _enumerator;
            private bool _isDisposed;

            public EnumeratorImpl(List<T> list, ThreadView view)
            {
                _enumerator = list.GetEnumerator();
                _view = view;
                _myId = view.enterEnumerator();
            }

            object IEnumerator.Current { get { return Current; } }
            public T Current
            {
                get
                {
                    if(_isDisposed) 
                        throwDisposedException();
                    return _enumerator.Current;
                }
            }
            
            public bool MoveNext()
            {
                if(_isDisposed) 
                    throwDisposedException(); 
                return _enumerator.MoveNext();
            }

            public void Dispose()
            {
                if(!_isDisposed)
                {
                    _enumerator.Dispose();
                    _isDisposed = true;
                    _view.exitEnumerator(_myId);
                }
            }

            void IEnumerator.Reset()
            {
                throw new NotSupportedException("This enumerator doesn't support Reset()");
            }

            private static void throwDisposedException()
            {
                throw new ObjectDisposedException("The enumerator was disposed");
            }
        }
        #endregion

        #region Events
        // Because we want to hold the write lock for as short a time as possible, we enqueue events and dispatch them in a group
        // as soon as the write method is complete

        // Collection changed
        private readonly AsyncDispatcherEvent<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs> _collectionChanged =  new AsyncDispatcherEvent<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>();
        private void onCollectionChangedInternal(object sender, NotifyCollectionChangedEventArgs args) { _threadView.Value.waitingEvents.Add(args); }
        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add
            {
                if(value == null) return;
                _lock.EnterWriteLock(); // can't add/remove event during write operation
                try
                {
                    // even though this is technically a write operation, there's no reason to check reenterancy since it won't ever call handler
                    // in fact, removing handlers in the callback could be a useful scenario
                    if(_collectionChanged.isEmpty) // if we were empty before, the handler wasn't attached
                        _collection.CollectionChanged += onCollectionChangedInternal;
                    _collectionChanged.add(value);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
            remove
            {
                if(value == null) return;
                _lock.EnterWriteLock(); // can't add/remove event during write operation
                try
                {
                    // even though this is technically a write operation, there's no reason to check reenterancy since it won't ever call handler
                    // in fact, removing handlers in the callback could be a useful scenario
                    _collectionChanged.remove(value);
                    if(_collectionChanged.isEmpty) // if we're now empty, detatch handler
                        _collection.CollectionChanged -= onCollectionChangedInternal;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
        }
        
        // Property changed
        private readonly AsyncDispatcherEvent<PropertyChangedEventHandler, PropertyChangedEventArgs> _propertyChanged = new AsyncDispatcherEvent<PropertyChangedEventHandler, PropertyChangedEventArgs>();
        private void onPropertyChangedInternal(object sender, PropertyChangedEventArgs args) { _threadView.Value.waitingEvents.Add(args); }
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                if(value == null) return;
                _lock.EnterWriteLock(); // can't add/remove event during write operation
                try
                {
                    // even though this is technically a write operation, there's no reason to check reenterancy since it won't ever call handler
                    // in fact, removing handlers in the callback could be a useful scenario
                    if(_propertyChanged.isEmpty) // if we were empty before, the handler wasn't attached
                        ((INotifyPropertyChanged) _collection).PropertyChanged += onPropertyChangedInternal;
                    _propertyChanged.add(value);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
            remove
            {
                if(value == null) return;
                _lock.EnterWriteLock(); // can't add/remove event during write operation
                try
                {
                    // even though this is technically a write operation, there's no reason to check reenterancy since it won't ever call handler
                    // in fact, removing handlers in the callback could be a useful scenario
                    _propertyChanged.remove(value);
                    if(_propertyChanged.isEmpty) // if we're now empty, detatch handler
                        ((INotifyPropertyChanged) _collection).PropertyChanged -= onPropertyChangedInternal;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
        }

        private void dispatchWaitingEvents(ThreadView view)
        {
            List<EventArgs> waitingEvents = view.waitingEvents;
            try
            {
                if(waitingEvents.Count == 0) return; // fast path for no events
                if(view.dissalowReenterancy)
                {
                    // Write methods should have checked this before we got here. Since we didn't that means there's a bugg in this class
                    // itself. However, we can't dispatch the events anyways, so we'll have to throw an exception.
                    if(Debugger.IsAttached)
                        Debugger.Break();
                    throwReenterancyException();
                }
                view.dissalowReenterancy = true;
                foreach(EventArgs args in waitingEvents)
                {
                    NotifyCollectionChangedEventArgs ccArgs = args as NotifyCollectionChangedEventArgs;
                    if(ccArgs != null)
                    {
                        _collectionChanged.raise(this, ccArgs);
                    }
                    else
                    {
                        PropertyChangedEventArgs pcArgs = args as PropertyChangedEventArgs;
                        if(pcArgs != null)
                        {
                            _propertyChanged.raise(this, pcArgs);
                        }
                    }
                }
            }
            finally
            {
                view.dissalowReenterancy = false;
                waitingEvents.Clear();
            }
        }

        private static void throwReenterancyException()
        {
            throw new InvalidOperationException("ObservableCollectionReentrancyNotAllowed -- don't modify the collection during callbacks from it!");
        }
        #endregion

        #region Methods to make interfaces happy -- most of these just foreward to the appropriate methods above
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        void IList.Remove(object value) { Remove((T) value); }
        object IList.this[int index] { get { return this[index]; } set { this[index] = (T) value; } }
        void IList.Insert(int index, object value) { Insert(index, (T) value); }
        bool ICollection<T>.IsReadOnly { get { return false; } }
        bool IList.IsReadOnly { get { return false; } }
        bool IList.IsFixedSize { get { return false; } }
        bool IList.Contains(object value) { return Contains((T) value); }
        object ICollection.SyncRoot { get { throw new NotSupportedException("AsyncObservableCollection doesn't need external synchronization"); } }
        bool ICollection.IsSynchronized { get { return false; } }
        void ICollection.CopyTo(Array array, int index) { CopyTo((T[]) array, index); }
        int IList.IndexOf(object value)  { return IndexOf((T) value); }
        #endregion

        #region Serialization
        /// <summary>
        /// Constructor is only here for serialization, you should use the default constructor instead.
        /// </summary>
        public AsyncObservableCollection(SerializationInfo info, StreamingContext context)
            : this((T[]) info.GetValue("values", typeof(T[])))
        {
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("values", ToArray(), typeof(T[]));
        }
        #endregion
    }
}