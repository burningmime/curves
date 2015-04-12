using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace burningmime.curves
{
    /// <summary>
    /// Implements a least-squares bezier curve fitting routine based on http://tog.acm.org/resources/GraphicsGems/gems/FitCurves.c with a few 
    /// optimzations made by me. You can read the article here: http://read.pudn.com/downloads141/ebook/610086/Graphics_Gems_I.pdf page 626.
    /// To use, call the <see cref="Fit"/> static function and wait for magic to happen.
    /// </summary>
    public sealed class CurveFit : CurveFitBase
    {
        /// <summary>
        /// Use a thread-static instance to prevent multithreading issues without needing to re-allocate on each run.
        /// TODO: test if this works correctly in Unity.
        /// </summary>
        [ThreadStatic] private static CurveFit _instance;

        /// <summary>
        /// Private constructor so it can't be constructed externally.
        /// </summary>
        private CurveFit() { }

        /// <summary>
        /// Curves we've found so far.
        /// </summary>
        private readonly List<CubicBezier> _result = new List<CubicBezier>(16);

        /// <summary>
        /// Shared zero-curve array.
        /// </summary>
        private static readonly CubicBezier[] NO_CURVES = new CubicBezier[0];

        /// <summary>
        /// Attempts to fit a set of Bezier curves to the given data. Throws exceptions on bad data, so you might
        /// want to wrap any calls to this in a try/catch. This may fail for points that are REALLY close together
        /// since it uses a fairly large epsilon and error must be >= 1. It returns a set of curves that form a 
        /// http://en.wikipedia.org/wiki/Composite_B%C3%A9zier_curve with C1 continuity (that is, each curve's start
        /// point is coincident with the previous curve's end point, and the tangent vectors of the start and end
        /// points are going in the same direction, so the curves will join up smoothly). Might return an empty list
        /// if fitting failed.
        /// </summary>
        /// <param name="points">Set of points to fit to.</param>
        /// <param name="maxError">Maximum distance from any data point to a point on the generated curve.</param>
        /// <returns>Fitted curves or an empty list if it could not fit.</returns>
        public static CubicBezier[] Fit(List<VECTOR> points, FLOAT maxError)
        {
            if(maxError < EPSILON)
                throw new InvalidOperationException("maxError cannot be negative/zero/less than epsilon value");
            if(points == null)
                throw new ArgumentNullException("points");
            if(points.Count < 2)
                return NO_CURVES; // need at least 2 points to do anything

            // get a pre-allocated instance
            if(_instance == null)
                _instance = new CurveFit();
            CurveFit instance = _instance;
            
            try
            {
                // should be cleared after each run
                Debug.Assert(instance._pts.Count == 0 && instance._result.Count == 0 && 
                    instance._u.Count == 0 && instance._arclen.Count == 0);

                // initialize arrays
                instance._pts.AddRange(points);
                instance.InitializeArcLengths();
                instance._squaredError = maxError * maxError;

                // Find tangents at ends
                int last = points.Count - 1;
                VECTOR tanL = instance.GetLeftTangent(last);
                VECTOR tanR = instance.GetRightTangent(0);

                // do the actual fit
                instance.FitRecursive(0, last, tanL, tanR);
                return _instance._result.ToArray();
            }
            finally
            {
                instance._pts.Clear();
                instance._result.Clear();
                instance._arclen.Clear();
                instance._u.Clear();
            }
        }

        /// <summary>
        /// Main fit function that attempts to fit a segment of curve and recurses if unable to.
        /// </summary>
        private void FitRecursive(int first, int last, VECTOR tanL, VECTOR tanR)
        {
            int split;
            CubicBezier curve;
            if(FitCurve(first, last, tanL, tanR, out curve, out split))
            {
                _result.Add(curve);
            }
            else
            {
                // If we get here, fitting failed, so we need to recurse
                // first, get mid tangent
                VECTOR tanM1 = GetCenterTangent(first, last, split);
                VECTOR tanM2 = -tanM1;
                
                // our end tangents might be based on points outside the new curve (this is possible for mid tangents too
                // but since we need to maintain C1 continuity, it's too late to do anything about it)
                if(first == 0 && split < END_TANGENT_N_PTS)
                    tanL = GetLeftTangent(split);
                if(last == _pts.Count - 1 && split > (_pts.Count - (END_TANGENT_N_PTS + 1)))
                    tanR = GetRightTangent(split);

                // do actual recursion
                FitRecursive(first, split, tanL, tanM1);
                FitRecursive(split, last, tanM2, tanR);
            }
        }
    }
}
