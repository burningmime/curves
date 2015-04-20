using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using burningmime.curves.sample;

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

// ReSharper disable PossibleNullReferenceException
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
        
        private static readonly FLOAT[] _paramRdpError = { 1, 2, 4, 8, 16 };
        private static readonly FLOAT[] _paramFitError = { 4, 8, 16 };
        private const int N_ITERS = 3000;

        public static void Main(string[] args)
        {
            // do once so it's in cache
            List<VECTOR> data = new List<VECTOR>(TestData.data);
            List<VECTOR> reduced = CurvePreprocess.RdpReduce(data, 2);
            CurveFit.Fit(reduced, 8);

            Console.WriteLine("{0,-40}{1}", "typeof(VECTOR)", typeof(VECTOR).FullName);
            Console.WriteLine("{0,-40}{1}", "RyuJIT enabled", RyuJitStatus.RyuJitEnabled);
            Console.WriteLine("{0,-40}{1}", "SIMD enabled", RyuJitStatus.SimdEnabled);
            Console.WriteLine("{0,-40}{1}", "Iterations Per Test", N_ITERS);
            Console.WriteLine("{0,-40}{1}", "Test Data Size", data.Count);
            Console.WriteLine();

            double totalTime = 0;
            Stopwatch sw = new Stopwatch();
            Console.WriteLine("{0,-10} {1,-10} {2,-10} {3,-10} {4,-10} {5}", "RDP Error", "Fit Error", "Points", "Curves", "Time (s)", "Time Per Iter (ms)");
            Console.WriteLine("{0,-10} {1,-10} {2,-10} {3,-10} {4,-10} {5}", "---------", "---------", "------", "------", "--------", "------------------");
            foreach(FLOAT rdpError in _paramRdpError)
            foreach(FLOAT fitError in _paramFitError)
            {
                int nPts, nCurves;
                double t = runTest(sw, data, rdpError, fitError, out nPts, out nCurves);
                totalTime += t;
                Console.WriteLine("{0,-10} {1,-10} {2,-10} {3,-10} {4,-10:N4} {5,-10:N4}", rdpError, fitError, nPts, nCurves, t, (t / N_ITERS) * 1000);
            }

            Console.WriteLine();
            Console.WriteLine("TOTAL TIME: " + totalTime);
            Console.WriteLine();
        }

        private static double runTest(Stopwatch sw, List<VECTOR> data, FLOAT rdpError, FLOAT fitError, out int nPtsReduced, out int nCurves)
        {
            List<VECTOR> reduced = null;
            CubicBezier[] curves = null;
            sw.Reset();
            sw.Start();
            for(int i = 0; i < N_ITERS; i++)
            {
                reduced = CurvePreprocess.RdpReduce(data, rdpError);
                curves = CurveFit.Fit(reduced, fitError);
            }
            sw.Stop();
            nPtsReduced = reduced.Count;
            nCurves = curves.Length;
            return sw.Elapsed.TotalSeconds;
        }
    }
}