using System;
using System.Threading;

namespace burningmime.util
{
    // ReSharper disable InconsistentNaming
    public abstract class Disposable : IDisposable
    {
        private int _isDisposed;
        public bool isDisposed { get { return Thread.VolatileRead(ref _isDisposed) != 0; } }
        public void Dispose() { if(Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0) { disposeImpl(); GC.SuppressFinalize(this); } }
        public void throwIfDisposed() { if(isDisposed) throw new ObjectDisposedException(ToString()); }
        protected abstract void disposeImpl();
    }

    public abstract class Finalizable : IDisposable
    {
        private int _isDisposed;
        public bool isDisposed { get { return Thread.VolatileRead(ref _isDisposed) != 0; } }
        public void Dispose() { if(Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0) { disposeImpl(true); GC.SuppressFinalize(this); } }
        public void throwIfDisposed() { if(isDisposed) throw new ObjectDisposedException(ToString()); }
        protected abstract void disposeImpl(bool isDisposing);
        ~Finalizable()
        {
            if(Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0)
            {
                LogManager.log(LogLevel.DEBUG, "NOT DISPOSED", false, null, "Object of type " + GetType() + " not disposed: " + ToString());
                disposeImpl(false); 
            } 
        }
    }
}