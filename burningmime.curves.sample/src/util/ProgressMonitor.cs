using System;
using System.Threading;

/*
                        dM
                        MMr
                        4MMML                  .
                        MMMMM.                xf
        .              "M6MMM               .MM-
        Mh..          +MM5MMM            .MMMM
        .MMM.         .MMMMML.          MMMMMh
        )MMMh.        MM5MMM         MMMMMMM
        3MMMMx.     'MMM3MMf      xnMMMMMM"
        '*MMMMM      MMMMMM.     nMMMMMMP"
            *MMMMMx    "MMM5M\    .MMMMMMM=
            *MMMMMh   "MMMMM"   JMMMMMMP
                MMMMMM   GMMMM.  dMMMMMM            .
                MMMMMM  "MMMM  .MMMMM(        .nnMP"
    ..          *MMMMx  MMM"  dMMMM"    .nnMMMMM*
    "MMn...     'MMMMr 'MM   MMM"   .nMMMMMMM*"
        "4MMMMnn..   *MMM  MM  MMP"  .dMMMMMMM""
        ^MMMMMMMMx.  *ML "M .M*  .MMMMMM**"
            *PMMMMMMhn. *x > M  .MMMM**""
                ""**MMMMhx/.h/ .=*"
                        .3P"%....
                    nP"     "*MMnx
    */
#pragma warning disable 420 // "access to a volatile field by reference" -- since we're passing volatiles into Interlocked methods

namespace burningmime.util
{
    /// <summary>
    /// Provides a read-only view of a <see cref="IProgressMonitor"/>. Well, read-only except it can cancel. Meant for "watching"
    /// progress, rather than reporting it.
    /// </summary>
    public interface IProgressViewer
    {
        /// <summary>
        /// Gets the name of the current task being performed.
        /// </summary>
        string task { get; }

        /// <summary>
        /// Gets the total number of work units of the progress monitor.
        /// </summary>
        int totalWork { get; }

        /// <summary>
        /// Gets the number of work units complete. This may be greater than <see cref="totalWork"/>, which means the task is complete.
        /// </summary>
        int workComplete { get; }

        /// <summary>
        /// Gets the number of work units remaining to be completed.
        /// </summary>
        int workRemaining { get; }

        /// <summary>
        /// Gets a number between 0 and 1 representing just how complete the progress is.
        /// </summary>
        double ratioComplete { get; }

        /// <summary>
        /// Has the task been started?
        /// </summary>
        bool started { get; }

        /// <summary>
        /// Is the task complete?
        /// </summary>
        bool completed { get; }

        /// <summary>
        /// Can the task be cancelled?
        /// </summary>
        bool cancellable { get; }

        /// <summary>
        /// Has the task been cancelled?
        /// </summary>
        bool cancelled { get; }

        /// <summary>
        /// Tries to cancel the task (throws an exception if the monitor is not <see cref="cancellable"/>.
        /// Cancelling a sub-task will also cancel the base task (all the way up the chain).
        /// </summary>
        void cancel();
    }

    /// <summary>
    /// Main ProgressMonitor interface. Closely follows the Eclipse API but with less error checking. See
    /// http://www.eclipse.org/articles/Article-Progress-Monitors/article.html for a general overview of what this is or
    /// how to use it (note the API is a bit different, but the concepts are there). SHould always pass progress
    /// monitors using this interface instead of <see cref="ProgressMonitor"/> instances directly.
    /// </summary>
    public interface IProgressMonitor : IProgressViewer
    {
        /// <summary>
        /// Sets the name of the current task being performed.
        /// </summary>
        new string task { set; }

        /// <summary>
        /// Notifies that the main task is beginning. This must only be called once on a given progress monitor instance.
        /// </summary>
        /// <param name="totalWork">the total number of work units into which the main task is been subdivided.
        /// there is no UNKNOWN value unlike the eclipse version.</param>
        /// <param name="name"> the name (or description) of the main task</param>
        void begin(int totalWork, string name);

        /// <summary>
        /// Notifies that a given number of work unit of the main task has been completed. 
        /// Note that this amount represents an installment, as opposed to a cumulative amount of work done to date.
        /// </summary>
        /// <param name="work">a non-negative number of work units just completed</param>
        void worked(int work);

        /// <summary>
        /// Notifies that the work is done. You do not need to call this if the task is cancelled, unlike the Eclipse version.
        /// </summary>
        void done();
        
        /// <summary>
        /// Creates a new sub-progress monitor for the this monitor. The sub progress monitor uses the given number of work ticks from its parent monitor. 
        /// 
        /// See http://help.eclipse.org/indigo/index.jsp?topic=%2Forg.eclipse.platform.doc.isv%2Freference%2Fapi%2Forg%2Feclipse%2Fcore%2Fruntime%2FIProgressMonitor.html for
        /// a general overview (API is quite different, but idea is similar).
        /// </summary>
        /// <param name="workFromTotal">the number of work ticks allocated from the parent monitor</param>
        /// <param name="propogateTaskName">Should task names of the sub task overwrite task names of the main task?</param>
        /// <returns>A new progress monitor to use for the sub task.</returns>
        IProgressMonitor subTask(int workFromTotal, bool propogateTaskName);
    }

    // TODO implement NullProgressReporter for when we don't care

    /// <summary>
    /// Thread-safe class to report progress. Closely follows the Eclipse API but with less error checking. See
    /// http://www.eclipse.org/articles/Article-Progress-Monitors/article.html for a general overview of what this is or
    /// how to use it (note the API is a bit different, but the concepts are there).
    /// 
    /// This can't be externally constructed by design. To get a progress monitor call <see cref="create"/>.
    /// </summary>
    public class ProgressMonitor : IProgressMonitor
    {
        private readonly bool _cancellable;
        private volatile bool _cancelled;
        private volatile int _totalWork = -1;
        private volatile int _workComplete;
        private volatile string _task;

        public static IProgressMonitor create(bool cancellable, string initialTask = null) { return new ProgressMonitor(cancellable, initialTask); }
        private ProgressMonitor(bool cancellable, string initialTask)
        {
            _cancellable = cancellable;
            _task = initialTask ?? string.Empty;
        }

        // ReSharper disable ParameterHidesMember
        public void begin(int totalWork, string task)
        // ReSharper restore ParameterHidesMember
        {
            if(totalWork < 0) throw new ArgumentOutOfRangeException("totalWork", "totalWork must be >= 0");
            if(Interlocked.CompareExchange(ref _totalWork, totalWork, -1) != -1)
                throw new InvalidOperationException("The progress reporter has already been started");
            this.task = task ?? string.Empty;
        }

        public virtual void worked(int work)
        {
            if(work <= 0) return;
            int tw = _totalWork;
            if(tw < 0) throw new InvalidOperationException("The progress reporter has not been started");
            Interlocked.Add(ref _workComplete, work);
        }

        public IProgressMonitor subTask(int workFromTotal, bool propogateTaskName)
        {
            int tw = _totalWork;
            if(tw < 0) throw new InvalidOperationException("The progress reporter has not been started");
            if(workFromTotal < 1) throw new ArgumentOutOfRangeException("workFromTotal", "workFromTotal must be at least 1");
            return new SubProgressMonitor(this, workFromTotal, propogateTaskName);
        }

        public void done()
        {
            int tw = _totalWork;
            int wc = _workComplete;
            if(wc < tw) worked(tw - wc);
        }

        public virtual void cancel()
        {
            if(!_cancellable) throw new InvalidOperationException("The progress monitor is not cancellable");
            _cancelled = true;
        }

        public virtual string task { get { return _task; } set { _task = value ?? string.Empty; } }
        public int totalWork { get { return _totalWork; } }
        public int workComplete { get { int tw = _totalWork; return tw < 0 ? 0 : _workComplete; } }
        public int workRemaining { get { int tw = _totalWork; int wc = _workComplete; return tw < 0 ? 0 : Math.Max(0, tw - wc); } }
        public double ratioComplete { get { int tw = _totalWork; int wc = _workComplete; return tw <= 0 ? 0 : Math.Min(1f, (double) wc / tw); } }
        public bool started { get { int tw = _totalWork; return tw > 0; } }
        public bool completed { get { int tw = _totalWork; int wc = _workComplete; return tw > 0 && wc >= tw; } }
        public bool cancellable { get { return _cancellable; } }
        public bool cancelled { get { return _cancelled; } }

        private sealed class SubProgressMonitor : ProgressMonitor
        {
            private readonly ProgressMonitor _parent;
            private readonly int _workFromTotal;
            private readonly bool _propogateName;
            private volatile int _completedParentWork;

            public SubProgressMonitor(ProgressMonitor parent, int workFromTotal, bool propogateTaskName) : base(parent.cancellable, null)
            {
                _parent = parent;
                _workFromTotal = workFromTotal;
                _propogateName = propogateTaskName;
                _task = string.Empty;
            }

            public override void worked(int work)
            {
                if(work <= 0) return;
                int tw = _totalWork;
                if(tw < 0) throw new InvalidOperationException("The progress reporter has not been started");
                int wc = Interlocked.Add(ref _workComplete, work);
                
                // Okay, here's where it gets messy, multithreading-wise. Basically, we read the current state
                // of the _completedParentWork, calculate what the new value should be, then try to store it.
                // if another thread got there in the meantime, start over
                while(true)
                {
                    double ratio = Math.Min((double) wc / tw, 1);                                   // how complete is this PM?
                    int targetWork = (int) (ratio * _workFromTotal);                                // how many work units should we tell the parent PM we've done?
                    int cpw = Thread.VolatileRead(ref _completedParentWork);                        // how many have we already told them we've done?
                    int workAmmount = targetWork - cpw;                                             // how many more do we need to tell them to make up the difference?
                    if(workAmmount <= 0) return;                                                    // if we don't need to notify them of any new work, cool!
                    int newCpw = cpw + workAmmount;                                                 // what will our new total be?
                    if(Interlocked.CompareExchange(ref _completedParentWork, newCpw, cpw) == cpw)   // has another thread accessed this PM yet? if not, store the new work
                    {
                        _parent.worked(workAmmount);                                                // tell the parent the work and complete -- only ever do this if we're SURE we got the right #
                        return;
                    }
                    wc = Thread.VolatileRead(ref _workComplete);                                    // another thread got here before us. start over with the new work complete
                }
            }

            public override void cancel() { base.cancel(); _parent.cancel(); }
            public override string task {  get { return _task; } set { _task = value; if(_propogateName) _parent.task = value; } }
        }
    }
}