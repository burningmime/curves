using System;
using System.Diagnostics;
using System.Windows;
using JetBrains.Annotations;

namespace burningmime.util.wpf
{
    public static class WpfMain
    {
        private static readonly Log _log = LogManager.getLog(typeof(WpfMain));

        /// <summary>
        /// Implements a *very* simple Main() function for a WPF app with some error handling. This is a replacement
        /// for setting the Startup Object to the application instance in Visual Studio. It instantly kills the application
        /// on unhandled exceptions and is generally kind of crap, so it should be replaced for any mildly interesting
        /// program.
        /// </summary>
        public static int run<TApplication>() where TApplication : Application, new()
        {
            try
            {
                Application app;
                try
                {
                    LogManager.attach(new ConsoleLogListener(LogLevel.DEBUG));
                    app = new TApplication();
                    if(app.StartupUri == null)
                        throw new InvalidOperationException("StartupUri not set");
                    app.DispatcherUnhandledException += (_, args) => onError(args.Exception, "Dispatcher unhandled exception");
                }
                catch(Exception e)
                {
                    app = null;
                    onError(e, "Error starting up");
                }

                app.Run();
                return 0;
            }
            finally
            {
                LogManager.dispose();
            }
        }

        // ReSharper disable EmptyGeneralCatchClause
        [ContractAnnotation("=> halt")]
        private static void onError(Exception e, string text)
        {
            // want to do each of these things even if another one fails... we're not too concerned about more exceptions at this point
            try { if(Debugger.IsAttached) Debugger.Break(); } catch(Exception) { }
            try { _log.fatal(e, text); LogManager.dispose(); } catch(Exception) { }
            try { MessageBox.Show(text + ":" +  Environment.NewLine + Environment.NewLine + e, text, MessageBoxButton.OK, MessageBoxImage.Error); } catch(Exception) { }
            Environment.Exit(-1);
        }
        // ReSharper restore EmptyGeneralCatchClause
    }
}