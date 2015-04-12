using System.Linq;
#if SYSTEM_WINDOWS_VECTOR
using VECTOR = System.Windows.Vector;
using FLOAT = System.Double;
#elif SYSTEM_NUMERICS_VECTOR
using VECTOR = System.Numerics.Vector2;
using FLOAT = System.Single;
#elif UNITY
using VECTOR = UnityEngine.Vector2;
using FLOAT = System.Single;
#else
#error Unknown vector type -- must define one of SYSTEM_WINDOWS_VECTOR, SYSTEM_NUMERICS_VECTOR or UNITY
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using burningmime.curves.sample;

namespace burningmime.curves.perftest
{
    public static class Program
    {
#if SYSTEM_NUMERICS_VECTOR
        // Code required to get SIMD working with RyuJIT CTP5
        // from: http://www.drdobbs.com/windows/64-bit-simd-code-from-c/240168851
        internal static System.Numerics.Vector4 dummy;
        static Program() { dummy = System.Numerics.Vector4.One; }
#endif
        

        public static void Main(string[] args)
        {
            Console.WriteLine("typeof(VECTOR) = " + typeof(VECTOR).FullName);
#if SYSTEM_NUMERICS_VECTOR
            Console.WriteLine("Vector.IsHardwareAccelerated = " + IsHardwareAcceleratedHelper.isHardwareAccelerated);
#endif

            const int N_ITERS = 10000;
            List<VECTOR> list = new List<VECTOR>(TestData.data);
            doFit(list); // once so it's JITted and in cache

            Stopwatch sw = new Stopwatch();
            sw.Start();
            for(int i = 0; i < N_ITERS; i++)
                doFit(list);
            sw.Stop();

            Console.WriteLine("Fitting " + list.Count + " points for " + N_ITERS + " iterations took: " + sw.Elapsed.TotalSeconds + " seconds");
            Console.WriteLine("Average time: " + (sw.Elapsed.TotalMilliseconds / N_ITERS) + "ms per iteration");
            Console.ReadLine();
        }

        private static void doFit(List<VECTOR> list)
        {
            List<VECTOR> reduced = CurvePreprocess.RdpReduce(list, 2);
            CurveFit.Fit(reduced, 8);
        }
    }
}