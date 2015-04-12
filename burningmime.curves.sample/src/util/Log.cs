using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

// ReSharper disable ExplicitCallerInfoArgument
namespace burningmime.util
{
    public enum LogLevel { DEBUG, INFO, WARNING, ERROR, FATAL, MAX }
    public sealed class LogLine
    {
        private readonly DateTime _time;
        private readonly LogLevel _level;
        private readonly string _header;
        private readonly string _message;
        private readonly Exception _exception;
        private readonly string _function;
        private readonly string _file;
        private readonly int _line;
        private readonly string _toString;

        public LogLine(DateTime time, LogLevel level, string header, string message, Exception exception = null, string function = null, string file = null, int line = 0)
        {
            _time = time;
            _level = level;
            _header = header;
            _message = message;
            _exception = exception;
            _function = function;
            _file = file;
            _line = line;

            StringBuilder s = new StringBuilder();

            s.Append("[");
            s.Append(time.ToString("s", CultureInfo.InvariantCulture));
            s.Append("] ");

            switch(level)
            {
                case LogLevel.DEBUG: s.Append("[DEBUG] "); break;
                case LogLevel.INFO: s.Append("[INFO] "); break;
                case LogLevel.WARNING: s.Append("[WARNING] "); break;
                case LogLevel.ERROR: s.Append("[ERROR] "); break;
                case LogLevel.FATAL: s.Append("[FATAL] "); break;
                default: s.Append("[?] "); break;
            }

            if(null != header)
            {
                s.Append("[");
                s.Append(header);
                s.Append("] ");

            }

            if(null != function)
            {
                Debug.Assert(file != null);
                s.Append("[");
                s.Append(function);
                s.Append("@");
                s.Append(file);
                s.Append(":");
                s.Append(line);
                s.Append("] ");
            }
            
            if (null != message)
                s.Append(message);

            if (null != exception)
            {
                if(null != message)
                {
                    s.AppendLine();
                    s.Append("   ");
                }
                    
                s.Append(exception.GetType().Name);
                s.Append(": ");
                s.AppendLine(exception.Message);
                s.Append(exception.StackTrace);
            }

            _toString = s.ToString();
        }

        public DateTime time { get { return _time; } }
        public LogLevel level { get { return _level; } }
        public string header { get { return _header; } }
        public string message { get { return _message; } }
        public Exception exception { get { return _exception; } }
        public string function { get { return _function; } }
        public string file {  get { return _file; } }
        public int? line { get { return _line; } }
        public override string ToString() { return _toString; }
    }

    /// <summary>
    /// Base interface for all log listeners.
    /// 
    /// WARNING: Be extra-careful about threading in these! The write() method will be called under a global lock, so
    /// don't wait on the caprices of your underlying stream. Writing a couple log lines out of order is a lot better
    /// than halting the program while waiting on disk access.
    /// </summary>
    public interface ILogListener  : IDisposable
    {
        LogLevel minLevel { get; }
        void write(LogLine line);
        void attach(DateTime time);
    }

    public struct Log
    {
        private readonly string _header;
        public string header { get { return _header; } }

        public Log(string header) { _header = header; }
        public Log(Type type) { _header = type == null ? null : type.Name; }

        public void write(LogLevel level, string message,              [CallerLineNumber] int lineNum = 0, [CallerMemberName] string function = null, [CallerFilePath] string file = null) { LogManager.log(level, header, true, null, message, lineNum, function, file); }
        public void write(LogLevel level, Exception e,                 [CallerLineNumber] int lineNum = 0, [CallerMemberName] string function = null, [CallerFilePath] string file = null) { LogManager.log(level, header, true, e,    null,    lineNum, function, file); }
        public void write(LogLevel level, Exception e, string message, [CallerLineNumber] int lineNum = 0, [CallerMemberName] string function = null, [CallerFilePath] string file = null) { LogManager.log(level, header, true, e,    message, lineNum, function, file); }

        public void debug(string message,                [CallerLineNumber] int lineNum = 0, [CallerMemberName] string function = null, [CallerFilePath] string file = null) { LogManager.log(LogLevel.DEBUG,   header, true, null, message, lineNum, function, file); }
        public void debug(Exception e,                   [CallerLineNumber] int lineNum = 0, [CallerMemberName] string function = null, [CallerFilePath] string file = null) { LogManager.log(LogLevel.DEBUG,   header, true, e,    null,    lineNum, function, file); }
        public void debug(Exception e, string message,   [CallerLineNumber] int lineNum = 0, [CallerMemberName] string function = null, [CallerFilePath] string file = null) { LogManager.log(LogLevel.DEBUG,   header, true, e,    message, lineNum, function, file); }
        public void info(string message,                 [CallerLineNumber] int lineNum = 0, [CallerMemberName] string function = null, [CallerFilePath] string file = null) { LogManager.log(LogLevel.INFO,    header, true, null, message, lineNum, function, file); }
        public void info(Exception e,                    [CallerLineNumber] int lineNum = 0, [CallerMemberName] string function = null, [CallerFilePath] string file = null) { LogManager.log(LogLevel.INFO,    header, true, e,    null,    lineNum, function, file); }
        public void info(Exception e, string message,    [CallerLineNumber] int lineNum = 0, [CallerMemberName] string function = null, [CallerFilePath] string file = null) { LogManager.log(LogLevel.INFO,    header, true, e,    message, lineNum, function, file); }
        public void warning(string message,              [CallerLineNumber] int lineNum = 0, [CallerMemberName] string function = null, [CallerFilePath] string file = null) { LogManager.log(LogLevel.WARNING, header, true, null, message, lineNum, function, file); }
        public void warning(Exception e,                 [CallerLineNumber] int lineNum = 0, [CallerMemberName] string function = null, [CallerFilePath] string file = null) { LogManager.log(LogLevel.WARNING, header, true, e,    null,    lineNum, function, file); }
        public void warning(Exception e, string message, [CallerLineNumber] int lineNum = 0, [CallerMemberName] string function = null, [CallerFilePath] string file = null) { LogManager.log(LogLevel.WARNING, header, true, e,    message, lineNum, function, file); }
        public void error(string message,                [CallerLineNumber] int lineNum = 0, [CallerMemberName] string function = null, [CallerFilePath] string file = null) { LogManager.log(LogLevel.ERROR,   header, true, null, message, lineNum, function, file); }
        public void error(Exception e,                   [CallerLineNumber] int lineNum = 0, [CallerMemberName] string function = null, [CallerFilePath] string file = null) { LogManager.log(LogLevel.ERROR,   header, true, e,    null,    lineNum, function, file); }
        public void error(Exception e, string message,   [CallerLineNumber] int lineNum = 0, [CallerMemberName] string function = null, [CallerFilePath] string file = null) { LogManager.log(LogLevel.ERROR,   header, true, e,    message, lineNum, function, file); }
        public void fatal(string message,                [CallerLineNumber] int lineNum = 0, [CallerMemberName] string function = null, [CallerFilePath] string file = null) { LogManager.log(LogLevel.FATAL,   header, true, null, message, lineNum, function, file); }
        public void fatal(Exception e,                   [CallerLineNumber] int lineNum = 0, [CallerMemberName] string function = null, [CallerFilePath] string file = null) { LogManager.log(LogLevel.FATAL,   header, true, e,    null,    lineNum, function, file); }
        public void fatal(Exception e, string message,   [CallerLineNumber] int lineNum = 0, [CallerMemberName] string function = null, [CallerFilePath] string file = null) { LogManager.log(LogLevel.FATAL,   header, true, e,    message, lineNum, function, file); }
    }

    /// <summary>
    /// Really, really simple logger implemantation... possibly buggy, but works well enough in practice.
    /// </summary>
    public static class LogManager
    {
        private static readonly List<ILogListener> _listeners = new List<ILogListener>();
        private static volatile LogLevel _minLevel = LogLevel.MAX;
        private static readonly object _lock = new object();

        public static ILogListener[] getListeners() { lock(_lock) { return _listeners.ToArray(); } } 
        public static Log getLog(string header) { return new Log(header); }
        public static Log getLog(Type type) { return new Log(type); }

        public static void attach(ILogListener listener)
        {
            lock(_lock)
            {
                if (!_listeners.Contains(listener))
                    _listeners.Add(listener);
                _minLevel = listener.minLevel < _minLevel ? listener.minLevel : _minLevel;
            }

            listener.attach(DateTime.Now);
        }

        public static void detatch(ILogListener listener)
        {
            lock(_lock)
            {
                int count = _listeners.Count;
                LogLevel minLevel = LogLevel.MAX;
                int i = 0;
                while (i < count)
                {
                    ILogListener l = _listeners[i];
                    if (l == listener)
                    {
                        _listeners.RemoveAt(i);
                    }
                    else
                    {
                        minLevel = l.minLevel < minLevel ? l.minLevel : minLevel;
                        i++;
                    }
                }
                _minLevel = minLevel;
            }

            listener.Dispose();
        }
        
        /// <summary>
        /// When possible, use the methods in the <see cref="Log"/> struct instead of this method.
        /// </summary>
        public static void log(LogLevel level, string header, bool includeLineInfo, Exception exception, string message, [CallerLineNumber] int lineNum = 0, [CallerMemberName] string function = null, [CallerFilePath] string file = null)
        {
            if (_minLevel > level) return;
            LogLine line = new LogLine(DateTime.Now, level, header, message, exception,  includeLineInfo ? function : null, includeLineInfo ? Path.GetFileName(file) : null, lineNum);
            lock(_lock)
            {
                foreach(ILogListener listener in _listeners)
                    if(listener.minLevel <= level)
                        listener.write(line);
            }
        }

        public static void dispose()
        {
            lock(_lock)
            {
                foreach(ILogListener listener in _listeners)
                    listener.Dispose();
                _listeners.Clear();
            }
        }
    }

    public sealed class ConsoleLogListener : ILogListener
    {
        private static readonly object _consoleLock = new object();
        private static readonly WaitCallback _writeImpl = writeImpl;
        private readonly LogLevel _minLevel;

        public ConsoleLogListener(LogLevel minLevel) { _minLevel = minLevel; }
        public LogLevel minLevel { get { return _minLevel; } }
        public void attach(DateTime time) { }
        void IDisposable.Dispose() { }

        private static readonly ConsoleColor[] _colors =
        {
            ConsoleColor.Gray,
            ConsoleColor.Gray,
            ConsoleColor.Yellow,
            ConsoleColor.Red,
            ConsoleColor.DarkRed
        };

        public void write(LogLine line) { ThreadPool.QueueUserWorkItem(_writeImpl, line); }
        private static void writeImpl(object obj)
        {
            LogLine line = (LogLine) obj;
            lock(_consoleLock)
            {
                ConsoleColor oldColor = Console.ForegroundColor;
                Console.ForegroundColor = _colors[(int) line.level];
                Console.WriteLine(line.ToString());
                Console.ForegroundColor = oldColor;
            }
        }
    }

    public sealed class StreamLogListener : Disposable, ILogListener
    {
        private readonly LogLevel _minLevel;
        private readonly TextWriter _writer;
        private readonly bool _isStreamOwner;
        private readonly WaitCallback _writeImpl;

        public StreamLogListener(LogLevel level, string filename) : this(level, new FileStream(filename, FileMode.Append), true) { }
        public StreamLogListener(LogLevel level, Stream stream, bool isStreamOwner) : this(level, new StreamWriter(stream), isStreamOwner) { }
        public StreamLogListener(LogLevel level, TextWriter writer, bool isStreamOwner) { _minLevel = level; _writer = writer; _isStreamOwner = isStreamOwner; _writeImpl = writeImpl; }

        private void queueWrite(string str) { ThreadPool.QueueUserWorkItem(_writeImpl, str); }
        private void writeImpl(object obj)
        {
            lock(_writer)
            {
                if(isDisposed)
                    return;
                _writer.WriteLine((string) obj);
                _writer.Flush();
            }
        }

        public LogLevel minLevel { get { return _minLevel; } }
        public void attach(DateTime time) { queueWrite(string.Format("\r\n\r\n***** New session started at {0} -- Version {1} *****\r\n\r\n", time, "0.2"/* TODO version */)); }
        public void write(LogLine line) { queueWrite(line.ToString()); }
        protected override void disposeImpl() { if(_isStreamOwner) lock(_writer) _writer.Dispose(); }
    }
}