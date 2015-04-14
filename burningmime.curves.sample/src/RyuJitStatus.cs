using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace burningmime.curves.sample
{
    public static class RyuJitStatus
    {
        /// <summary>
        /// Checks if vector types are SIMD-enabled.
        /// </summary>
        public static readonly bool SimdEnabled = false;

        /// <summary>
        /// Checks if protojit.dll has been loaded, which as of now (April 2015) will check if the new JIT is running or not.
        /// </summary>
        public static readonly bool RyuJitEnabled;

        static RyuJitStatus()
        {
#if SYSTEM_NUMERICS_VECTOR
            try
            {
                // System.Numerics.Vector.IsHardwareAccelerated (to check if SIMD is really enabled) is internal in the newest version of the SIMD package.
                // So, we need to use some trickery to get to it. Unfortunately, just using reflection will call the IL method, which is just "return false".
                // What we really need to do is trick the JIT into thinking it's a legitimate direct call to a [JitIntrinsic] method so it works its magic.
                // How do you get the JIT to do something? Ask it to compile something! This means runtime code generation.
                PropertyInfo property = typeof(System.Numerics.Vector2).Assembly.GetType("System.Numerics.Vector").GetProperty("IsHardwareAccelerated");
                Func<bool> func = Expression.Lambda<Func<bool>>(Expression.Property(null, property)).Compile();
                SimdEnabled = func();
            }
            catch(Exception e)
            {
                if(Debugger.IsAttached)
                    Debugger.Break();
                Console.WriteLine("Error trying to get hardware accelerated status:");
                Console.WriteLine(e);
                Console.WriteLine();
                SimdEnabled = false;
            }
#endif
            try
            {
                // To check if RyuJit is enabled, we look to see if the "protojit.dll" module is loaded (note: this works as of april 2015, but probably not for much longer)
                RyuJitEnabled = Process.GetCurrentProcess().Modules.OfType<ProcessModule>().Any(pm => pm.ModuleName.Contains("protojit.dll"));
            }
            catch(Exception e)
            {
                if(Debugger.IsAttached)
                    Debugger.Break();
                Console.WriteLine("Error trying to get protojit.dll status:");
                Console.WriteLine(e);
                Console.WriteLine();
                RyuJitEnabled = false;
            }
        }

    }
}