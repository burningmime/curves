using System;
using System.Collections.Generic;
using burningmime.util.wpf;

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

namespace burningmime.curves.sample
{
    public partial class App
    {
#if SYSTEM_NUMERICS_VECTOR
        // Code required to get SIMD working with RyuJIT CTP5
        // from: http://www.drdobbs.com/windows/64-bit-simd-code-from-c/240168851
        internal static System.Numerics.Vector4 dummy;
        static App() { dummy = System.Numerics.Vector4.One; }
#endif

        public App()
        {
            InitializeComponent();
        }

        [STAThread]
        public static int Main(string[] args)
        {
            // Before we do anything, we want to call these to force them to be JITed. Ohterwise the first time they are called will give wildly different
            // performance results than the user expects. This only takes a few ms, so it won't slow down app startup that much.
            List<VECTOR> data = new List<VECTOR>(TestData.data);
            CurvePreprocess.Linearize(data, 8);
            List<VECTOR> reduced = CurvePreprocess.RdpReduce(data, 2);
            CurveFit.Fit(reduced, 8);

            // Okay, now run the app
            return WpfMain.run<App>();
        }
    }
}
