using System;
using System.Reflection;
using System.Windows.Threading;

namespace burningmime.util.wpf
{
    /// <summary>
    /// Wrapper around an event so that any events added from a Dispatcher thread are invoked on that thread. This means
    /// that if the UI adds an event and that event is called on a different thread, the callback will be dispatched
    /// to the UI thread and called asynchronously. If an event is added from a non-dispatcher thread, or the event
    /// is raised from within the same thread as it was added from, it will be called normally.
    /// 
    /// Note that this means that the callback will be asynchronous and may happen at some time in the future rather than as
    /// soon as the event is raised.
    /// 
    /// Example usage:
    /// -----------
    /// 
    ///     private readonly AsyncDispatcherEvent{PropertyChangedEventHandler, PropertyChangedEventArgs} _propertyChanged = 
    ///        new DispatcherEventHelper{PropertyChangedEventHandler, PropertyChangedEventArgs}();
    ///
    ///     public event PropertyChangedEventHandler PropertyChanged
    ///     {
    ///         add { _propertyChanged.add(value); }
    ///         remove { _propertyChanged.remove(value); }
    ///     }
    ///     
    ///     private void OnPropertyChanged(PropertyChangedEventArgs args)
    ///     {
    ///         _propertyChanged.invoke(this, args);
    ///     }
    /// 
    /// This class is thread-safe.
    /// </summary>
    /// <typeparam name="TEvent">The delagate type to wrap (ie PropertyChangedEventHandler). Must have a void delegate(object, TArgs) signature.</typeparam>
    /// <typeparam name="TArgs">Second argument of the TEvent. Must be of type EventArgs.</typeparam>
    public sealed class AsyncDispatcherEvent<TEvent, TArgs> where TEvent : class where TArgs : EventArgs
    {
        /// <summary>
        /// Type of a delegate that invokes a delegate. Okay, that sounds weird, but basically, calling this
        /// with a delegate and its arguments will call the Invoke() method on the delagate itself with those
        /// arguments.
        /// </summary>
        private delegate void InvokeMethod(TEvent @event, object sender, TArgs args);

        /// <summary>
        /// Method to invoke the given delegate with the given arguments quickly. It uses reflection once (per type)
        /// to create this, then it's blazing fast to call because the JIT knows everything is type-safe.
        /// </summary>
        private static readonly InvokeMethod _invoke;

        /// <summary>
        /// Using List{DelegateWrapper} and locking it on every access is what scrubs would do.
        /// </summary>
        private event EventHandler<TArgs> _event;

        /// <summary>
        /// Barely worth worrying about this corner case, but we need to lock on removes in case two identical non-dispatcher
        /// events are being removed at once.
        /// </summary>
        private readonly object _removeLock = new object();

        /// <summary>
        /// This is absolutely required to have a static constructor, otherwise it would be beforefieldinit which means
        /// that any type exceptions would be delayed until it's actually called. We can also do some extra checks here to
        /// make sure the types are correct.
        /// </summary>
        static AsyncDispatcherEvent()
        {
            Type tEvent = typeof(TEvent);
            Type tArgs = typeof(TArgs);
            if(!tEvent.IsSubclassOf(typeof(MulticastDelegate)))
                throw new InvalidOperationException("TEvent " + tEvent.Name + " is not a subclass of MulticastDelegate");
            MethodInfo method = tEvent.GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            if(method == null)
                throw new InvalidOperationException("Could not find method Invoke() on TEvent " + tEvent.Name);
            if(method.ReturnType != typeof(void))
                throw new InvalidOperationException("TEvent " + tEvent.Name + " must have return type of void");
            ParameterInfo[] paramz = method.GetParameters();
            if(paramz.Length != 2)
                throw new InvalidOperationException("TEvent " + tEvent.Name + " must have 2 parameters");
            if(paramz[0].ParameterType != typeof(object))
                throw new InvalidOperationException("TEvent " + tEvent.Name + " must have first parameter of type object, instead was " + paramz[0].ParameterType.Name);
            if(paramz[1].ParameterType != tArgs)
                throw new InvalidOperationException("TEvent " + tEvent.Name + " must have second paramater of type TArgs " + tArgs.Name + ", instead was " + paramz[1].ParameterType.Name);
            _invoke = (InvokeMethod) method.CreateDelegate(typeof(InvokeMethod));
            if(_invoke == null)
                throw new InvalidOperationException("CreateDelegate() returned null");
        }

        /// <summary>
        /// Adds the delegate to the event.
        /// </summary>
        public void add(TEvent value)
        {
            if(value == null)
                return;
            _event += (new DelegateWrapper(UiUtils.currentDispatcher(), value)).invoke;
        }

        /// <summary>
        /// Removes the last instance of delegate from the event (if it exists). Only removes events that were added from the current
        /// dispatcher thread (if they were added from one), so make sure to remove from the same thread that added.
        /// </summary>
        public void remove(TEvent value)
        {
            if(value == null)
                return;
            Dispatcher dispatcher = UiUtils.currentDispatcher();
            lock(_removeLock) // because events are intrinsically threadsafe, and dispatchers are thread-local, the only time this lock matters is when removing non-dispatcher events
            {
                EventHandler<TArgs> evt = _event;
                if(evt != null)
                {
                    Delegate[] invList = evt.GetInvocationList();
                    for(int i = invList.Length - 1; i >= 0; i--) // Need to go backwards since that's what event -= something does.
                    {
                        DelegateWrapper wrapper = (DelegateWrapper) invList[i].Target;
                        // need to use Equals instead of == for delegates
                        if(wrapper.handler.Equals(value) && wrapper.dispatcher == dispatcher)
                        {
                            _event -= wrapper.invoke;
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks if any delegate has been added to this event.
        /// </summary>
        public bool isEmpty
        {
            get
            {
                return _event == null;
            }
        }

        /// <summary>
        /// Calls the event.
        /// </summary>
        public void raise(object sender, TArgs args)
        {
            EventHandler<TArgs> evt = _event;
            if(evt != null)
                evt(sender, args);
        }

        private sealed class DelegateWrapper
        {
            public readonly TEvent handler;
            public readonly Dispatcher dispatcher;

            public DelegateWrapper(Dispatcher dispatcher, TEvent handler)
            {
                this.dispatcher = dispatcher;
                this.handler = handler;
            }

            public void invoke(object sender, TArgs args)
            {
                if(dispatcher == null || dispatcher == UiUtils.currentDispatcher())
                    _invoke(handler, sender, args);
                else
                    // ReSharper disable once AssignNullToNotNullAttribute
                    dispatcher.BeginInvoke(handler as Delegate, DispatcherPriority.DataBind, sender, args);
            }
        }
    }
}