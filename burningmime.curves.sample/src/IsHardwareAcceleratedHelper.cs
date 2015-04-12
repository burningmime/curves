using System;
using System.Linq.Expressions;
using System.Reflection;

namespace burningmime.curves.sample
{
    public static class IsHardwareAcceleratedHelper
    {
        /// <summary>
        /// Checks if vector types are SIMD-enabled.
        /// </summary>
        public static readonly bool isHardwareAccelerated = false;

#if SYSTEM_NUMERICS_VECTOR
        static IsHardwareAcceleratedHelper()
        {
            // System.Numerics.Vector.IsHardwareAccelerated (to check if SIMD is really enabled) is internal in the newest version of the SIMD package.
            // So, we need to use some trickery to get to it. Unfortunately, just using reflection will call the IL method, which is just "return false".
            // What we really need to do is trick the JIT into thinking it's a legitimate direct call to a [JitIntrinsic] method so it works its magic.
            // How do you get the JIT to do something? Ask it to compile something! This means runtime code generation.
            PropertyInfo property = typeof(System.Numerics.Vector2).Assembly.GetType("System.Numerics.Vector").GetProperty("IsHardwareAccelerated");
            Func<bool> func = Expression.Lambda<Func<bool>>(Expression.Property(null, property)).Compile();
            isHardwareAccelerated = func();
        }
#endif
    }
}