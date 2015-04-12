using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace burningmime.util.wpf
{
    /// <summary>
    /// Bindable progress monitor that updates occasionally. Useful for monitoring progress that's happening in another thread.
    /// This is a DependencyObject with most of its properties bindable, but since it is a DependencyObject, it can only be
    /// accessed from the thread that created it. Therefore, if the thing you're monitoring is progress in the UI thread, you'll
    /// need to create a seperate thread and window that shows the progress bar.
    /// 
    /// This is Disposable if you no longer wish to recieve updates, but disposing is not required.
    /// </summary>
    public sealed partial class AsyncProgressViewer : DependencyObject, IProgressViewer, IDisposable
    {
        public const double DEFAULT_UPDATE_FREQUENCY = .15;

        private readonly IProgressViewer _pm;
        private readonly object _state;
        private readonly bool _cancellable;
        private DispatcherTimer _timer;
        private int _isTimerStopped;

        /// <summary>
        /// Creates a new progress watcher.
        /// </summary>
        /// <param name="pm">Progress monitor to watch. Should be threadsafe (for example, ConcurrentProgressReporter).</param>
        /// <param name="updateFrequency">Frequency to update this view.</param>
        /// <param name="callbackState">State object to pass to the <see cref="progressChanged"/> event if you want one.</param>
        public AsyncProgressViewer(IProgressViewer pm, TimeSpan? updateFrequency = null, object callbackState = null)
        {
            if (pm == null) throw new ArgumentNullException("pm");

            _pm = pm;
            _state = callbackState;
            _cancellable = pm.cancellable;
            workComplete = pm.workComplete;
            totalWork = pm.totalWork;
            task = pm.task ?? string.Empty;
            cancelled = pm.cancelled;

            if(!cancelled && !completed)
            {
                _timer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher);
                _timer.Tick += onTick;
                _timer.Interval = updateFrequency ?? TimeSpan.FromSeconds(DEFAULT_UPDATE_FREQUENCY);
                _timer.Start();
            }
            else
            {
                _isTimerStopped = 1;
            }
        }

        /// <summary>
        /// Event called whenever the progress is updated.
        /// </summary>
        public event ProgressChangedEventHandler progressChanged;

        private void onTick(object sender, EventArgs eventArgs) { update(); }
        private void update()
        {
            VerifyAccess();
            task = _pm.task;
            cancelled = _pm.cancelled;
            int tw = totalWork = _pm.totalWork;
            int wc = workComplete = _pm.workComplete;
            
            // Calculate these ones to reduce threading mismatches
            started = tw >= 0;
            completed = tw >= 0 && wc >= tw;
            workRemaining = tw < 0 ? 0 : Math.Max(0, tw - wc);
            ratioComplete = tw < 0 ? 0 : wc >= tw ? 1 : (double) wc / tw;

            if(completed || cancelled)
                killTimer();

            ProgressChangedEventHandler handler = progressChanged;
            if(handler != null)
                handler(this, new ProgressChangedEventArgs((int) (ratioComplete * 100), _state));
        }

        /// <summary>
        /// Can we cancel this operation?
        /// </summary>
        public bool cancellable { get { return _cancellable; } }

        /// <summary>
        /// Cancels the operation if it's not complete, does nothing if it is complete/already cancelled, and throws an exception if
        /// the operation is not cancellable.
        /// </summary>
        public void cancel()
        {
            VerifyAccess();
            if(cancelled)
                return;
            if(!cancellable)
                throw new InvalidOperationException("The progress viewer is not cancellable");
            _pm.cancel();
            update();
        }
        
        public bool isDisposed { get; private set; }
        public void Dispose() { if(!isDisposed) { isDisposed = true; GC.SuppressFinalize(this); killTimer(); } }
        ~AsyncProgressViewer() { if(!isDisposed) { killTimer(); } }
        private void killTimer()
        {
            if(Interlocked.CompareExchange(ref _isTimerStopped, 1, 0) == 0)
            {
                _timer.Stop();
                _timer.Tick -= onTick;
                _timer = null;
            }
        }
    }
}