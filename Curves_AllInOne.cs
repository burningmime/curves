// Change this depending on what vector type you have
#define SYSTEM_NUMERICS_VECTOR

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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace burningmime.curves
{
    /// <summary>
    /// Cubic Bezier curve in 2D consisting of 4 control points.
    /// </summary>
    public struct CubicBezier : IEquatable<CubicBezier>
    {
        // Control points
        public readonly VECTOR p0;
        public readonly VECTOR p1;
        public readonly VECTOR p2;
        public readonly VECTOR p3;

        /// <summary>
        /// Creates a new cubic bezier using the given control points.
        /// </summary>
        public CubicBezier(VECTOR p0, VECTOR p1, VECTOR p2, VECTOR p3)
        {
            this.p0 = p0;
            this.p1 = p1;
            this.p2 = p2;
            this.p3 = p3;
        }

        /// <summary>
        /// Samples the bezier curve at the given t value.
        /// </summary>
        /// <param name="t">Time value at which to sample (should be between 0 and 1, though it won't fail if outside that range).</param>
        /// <returns>Sampled point.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VECTOR Sample(FLOAT t)
        {
            FLOAT ti = 1 - t;
            FLOAT t0 = ti * ti * ti;
            FLOAT t1 = 3 * ti * ti * t;
            FLOAT t2 = 3 * ti * t * t;
            FLOAT t3 = t * t * t;
            return (t0 * p0) + (t1 * p1) + (t2 * p2) + (t3 * p3);
        }

        /// <summary>
        /// Gets the first derivative of the curve at the given T value.
        /// </summary>
        /// <param name="t">Time value at which to sample (should be between 0 and 1, though it won't fail if outside that range).</param>
        /// <returns>First derivative of curve at sampled point.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VECTOR Derivative(FLOAT t)
        {
            FLOAT ti = 1 - t;
            FLOAT tp0 = 3 * ti * ti;
            FLOAT tp1 = 6 * t * ti;
            FLOAT tp2 = 3 * t * t;
            return (tp0 * (p1 - p0)) + (tp1 * (p2 - p1)) + (tp2 * (p3 - p2));
        }

        /// <summary>
        /// Gets the tangent (normalized derivative) of the curve at a given T value.
        /// </summary>
        /// <param name="t">Time value at which to sample (should be between 0 and 1, though it won't fail if outside that range).</param>
        /// <returns>Direction the curve is going at that point.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VECTOR Tangent(FLOAT t)
        {
            return VectorHelper.Normalize(Derivative(t));
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("CubicBezier: (<");
            sb.Append(VectorHelper.GetX(p0).ToString("N3", CultureInfo.InvariantCulture));
            sb.Append(", ");
            sb.Append(VectorHelper.GetY(p0).ToString("N3", CultureInfo.InvariantCulture));
            sb.Append("> <");
            sb.Append(VectorHelper.GetX(p1).ToString("N3", CultureInfo.InvariantCulture));
            sb.Append(", ");
            sb.Append(VectorHelper.GetY(p1).ToString("N3", CultureInfo.InvariantCulture));
            sb.Append("> <");
            sb.Append(VectorHelper.GetX(p2).ToString("N3", CultureInfo.InvariantCulture));
            sb.Append(", ");
            sb.Append(VectorHelper.GetY(p2).ToString("N3", CultureInfo.InvariantCulture));
            sb.Append("> <");
            sb.Append(VectorHelper.GetX(p3).ToString("N3", CultureInfo.InvariantCulture));
            sb.Append(", ");
            sb.Append(VectorHelper.GetY(p3).ToString("N3", CultureInfo.InvariantCulture));
            sb.Append(">)");
            return sb.ToString();
        }

        // Equality members -- pretty straightforeward
        public static bool operator ==(CubicBezier left, CubicBezier right) { return left.Equals(right); }
        public static bool operator !=(CubicBezier left, CubicBezier right) { return !left.Equals(right); }
        public bool Equals(CubicBezier other) { return p0.Equals(other.p0) && p1.Equals(other.p1) && p2.Equals(other.p2) && p3.Equals(other.p3); }
        public override bool Equals(object obj) { return obj is CubicBezier && Equals((CubicBezier) obj); }
        public override int GetHashCode()
        {
            JenkinsHash hash = new JenkinsHash();
            hash.Mixin(VectorHelper.GetX(p0).GetHashCode());
            hash.Mixin(VectorHelper.GetY(p0).GetHashCode());
            hash.Mixin(VectorHelper.GetX(p1).GetHashCode());
            hash.Mixin(VectorHelper.GetY(p1).GetHashCode());
            hash.Mixin(VectorHelper.GetX(p2).GetHashCode());
            hash.Mixin(VectorHelper.GetY(p2).GetHashCode());
            hash.Mixin(VectorHelper.GetX(p3).GetHashCode());
            hash.Mixin(VectorHelper.GetY(p3).GetHashCode());
            return hash.GetValue();
        }

        /// <summary>
        /// Simple implementation of Jenkin's hashing algorithm.
        /// http://en.wikipedia.org/wiki/Jenkins_hash_function
        /// I forget where I got these magic numbers from; supposedly they're good.
        /// 
        /// Copied from the utils because the curve code should be usable without them.
        /// </summary>
        private struct JenkinsHash
        {
            private int _current;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Mixin(int hash)
            {
                unchecked
                {
                    int num = _current;
                    if(num == 0)
                        num = 0x7e53a269;
                    else
                        num *= -0x5aaaaad7;
                    num += hash;
                    num += (num << 10);
                    num ^= (num >> 6);
                    _current = num;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetValue()
            {
                unchecked
                {
                    int num = _current;
                    num += (num << 3);
                    num ^= (num >> 11);
                    num += (num << 15);
                    return num;
                }
            }
        }
    }

    /// <summary>
    /// Maps a set of 2D Bezier curves so that samples are equally spaced across the spline. Basically, it does a lot of preprocessing and
    /// such on a set of curves so that when you call sample(0.5) you get a point that's halfway along the spline. This means that if you
    /// "move" something along the spline, it will move at a constant velocity. This is also useful for rendering the spline since the points 
    /// will be evenly spaced.
    /// </summary>
    public sealed class Spline
    {
        public const int MIN_SAMPLES_PER_CURVE = 8;
        public const int MAX_SAMPLES_PER_CURVE = 1024;
        private const FLOAT EPSILON = VectorHelper.EPSILON;

        private readonly List<CubicBezier> _curves; 
        private readonly ReadOnlyCollection<CubicBezier> _curvesView; 
        private readonly List<FLOAT> _arclen;
        private readonly int _samplesPerCurve;
        
        /// <summary>
        /// Creates an emoty spline.
        /// </summary>
        /// <param name="samplesPerCurve">Resolution of the curve. Values 32-256 work well. You may need more or less depending on how big the curves are.</param>
        public Spline(int samplesPerCurve)
        {
            if(samplesPerCurve < MIN_SAMPLES_PER_CURVE || samplesPerCurve > MAX_SAMPLES_PER_CURVE)
                throw new InvalidOperationException("samplesPerCurve must be between " + MIN_SAMPLES_PER_CURVE + " and " + MAX_SAMPLES_PER_CURVE);
            _samplesPerCurve = samplesPerCurve;
            _curves = new List<CubicBezier>(16);
            _curvesView = new ReadOnlyCollection<CubicBezier>(_curves);
            _arclen = new List<FLOAT>(16 * samplesPerCurve);
        }

        /// <summary>
        /// Creates a new spline from the given curves.
        /// </summary>
        /// <param name="curves">Curves to create the spline from.</param>
        /// <param name="samplesPerCurve">Resolution of the curve. Values 32-256 work well. You may need more or less depending on how big the curves are.</param>
        public Spline(ICollection<CubicBezier> curves, int samplesPerCurve)
        {
            if(curves == null)
                throw new ArgumentNullException("curves");
            if(samplesPerCurve < MIN_SAMPLES_PER_CURVE || samplesPerCurve > MAX_SAMPLES_PER_CURVE)
                throw new InvalidOperationException("samplesPerCurve must be between " + MIN_SAMPLES_PER_CURVE + " and " + MAX_SAMPLES_PER_CURVE);
            _samplesPerCurve = samplesPerCurve;
            _curves = new List<CubicBezier>(curves.Count);
            _curvesView = new ReadOnlyCollection<CubicBezier>(_curves);
            _arclen = new List<FLOAT>(_curves.Count * samplesPerCurve);
            foreach(CubicBezier curve in curves)
                Add(curve);
        }

        /// <summary>
        /// Adds a curve to the end of the spline.
        /// </summary>
        public void Add(CubicBezier curve)
        {
             if(_curves.Count > 0 && !VectorHelper.EqualsOrClose(_curves[_curves.Count - 1].p3, curve.p0))
                throw new InvalidOperationException("The new curve does at index " + _curves.Count + " does not connect with the previous curve at index " + (_curves.Count - 1));
            _curves.Add(curve);
            for(int i = 0; i < _samplesPerCurve; i++) // expand the array since updateArcLengths expects these values to be there
                _arclen.Add(0);
            UpdateArcLengths(_curves.Count - 1);
        }

        /// <summary>
        /// Modifies a curve in the spline. It must connect with the previous and next curves (if applicable). This requires that the
        /// arc length table be recalculated for that curve and all curves after it -- for example, if you update the first curve in the
        /// spline, each curve after that would need to be recalculated (could avoid this by caching the lengths on a per-curve basis if you're
        /// doing this often, but since the typical case is only updating the last curve, and the entire array needs to be visited anyway, it
        /// wouldn't save much).
        /// </summary>
        /// <param name="index">Index of the curve to update in <see cref="Curves"/>.</param>
        /// <param name="curve">The new curve with which to replace it.</param>
        public void Update(int index, CubicBezier curve)
        {
            if(index < 0)
                throw new IndexOutOfRangeException("Negative index");
            if(index >= _curves.Count)
                throw new IndexOutOfRangeException("Curve index " + index + " is out of range (there are " + _curves.Count + " curves in the spline)");
            if(index > 0 && !VectorHelper.EqualsOrClose(_curves[index - 1].p3, curve.p0))
                throw new InvalidOperationException("The updated curve at index " + index + " does not connect with the previous curve at index " + (index - 1));
            if(index < _curves.Count - 1 && !VectorHelper.EqualsOrClose(_curves[index + 1].p0, curve.p3))
                throw new InvalidOperationException("The updated curve at index " + index + " does not connect with the next curve at index " + (index + 1));
            _curves[index] = curve;
            for(int i = index; i < _curves.Count; i++)
                UpdateArcLengths(i);
        }

        /// <summary>
        /// Clears the spline.
        /// </summary>
        public void Clear()
        {
            _curves.Clear();
            _arclen.Clear();
        }

        /// <summary>
        /// Gets the total length of the spline.
        /// </summary>
        public FLOAT Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                List<FLOAT> arclen = _arclen;
                int count = arclen.Count;
                return count == 0 ? 0 : arclen[count - 1];
            }
        }

        /// <summary>
        /// Gets a read-only view of the current curves collection.
        /// </summary>
        public ReadOnlyCollection<CubicBezier> Curves
        {
            get { return _curvesView; }
        }
        
        /// <summary>
        /// Gets the position of a point on the spline that's close to the desired point along the spline. For example, if u = 0.5, then a point
        /// that's about halfway through the spline will be returned. The returned point will lie exactly on one of the curves that make up the
        /// spline.
        /// </summary>
        /// <param name="u">How far along the spline to sample (for example, 0.5 will be halfway along the length of the spline). Should be between 0 and 1.</param>
        /// <returns>The position on the spline.</returns>
        public VECTOR Sample(FLOAT u)
        {
            SamplePos pos = GetSamplePosition(u);
            return _curves[pos.Index].Sample(pos.Time);
        }

        /// <summary>
        /// Gets the curve index and t-value to sample to get a point at the desired part of the spline.
        /// </summary>
        /// <param name="u">How far along the spline to sample (for example, 0.5 will be halfway along the length of the spline). Should be between 0 and 1.</param>
        /// <returns>The position to sample at.</returns>
        public SamplePos GetSamplePosition(FLOAT u)
        {
            if(_curves.Count == 0) 
                throw new InvalidOperationException("No curves have been added to the spline");
            if(u < 0) 
                return new SamplePos(0, 0);
            if(u > 1)
                return new SamplePos(_curves.Count - 1, 1);

            List<FLOAT> arclen = _arclen;
            FLOAT total = Length;
            FLOAT target = u * total;
            Debug.Assert(target >= 0);

            // Binary search to find largest value <= target
            int index = 0;
            int low = 0;
            int high = arclen.Count - 1;
            FLOAT found = float.NaN;
            while(low < high)
            {
                index = (low + high) / 2;
                found = arclen[index];
                if (found < target)
                    low = index + 1;
                else
                    high = index;
            }

            // this should be a rather rare scenario: we're past the end, but this wasn't picked up by the test for u >= 1
            if(index >= arclen.Count - 1)
                return new SamplePos(_curves.Count - 1, 1);

            // this can happen because the binary search can give us either index or index + 1
            if(found > target)
                index--;

            if(index < 0)
            {
                // We're at the beginning of the spline
                FLOAT max = arclen[0];
                Debug.Assert(target <= max + EPSILON);
                FLOAT part = target / max;
                FLOAT t = part / _samplesPerCurve;
                return new SamplePos(0, t);
            }
            else
            {
                // interpolate between two values to see where the index would be if continuous values
                FLOAT min = arclen[index];
                FLOAT max = arclen[index + 1];
                Debug.Assert(target >= min - EPSILON && target <= max + EPSILON);
                FLOAT part = target < min ? 0 : target > max ? 1 : (target - min) / (max - min);
                FLOAT t = (((index + 1) % _samplesPerCurve) + part) / _samplesPerCurve;
                int curveIndex = (index + 1) / _samplesPerCurve;
                return new SamplePos(curveIndex, t);
            }
        }

        /// <summary>
        /// Updates the internal arc length array for a curve. Expects the list to contain enough elements.
        /// </summary>
        private void UpdateArcLengths(int iCurve)
        {
            CubicBezier curve = _curves[iCurve];
            int nSamples = _samplesPerCurve;
            List<FLOAT> arclen = _arclen;
            FLOAT clen = iCurve > 0 ? arclen[iCurve * nSamples - 1] : 0;
            VECTOR pp = curve.p0;
            Debug.Assert(arclen.Count >= ((iCurve + 1) * nSamples));
            for(int iPoint = 0; iPoint < nSamples; iPoint++)
            {
                int idx = (iCurve * nSamples) + iPoint;
                FLOAT t = (iPoint + 1) / (FLOAT) nSamples;
                VECTOR np = curve.Sample(t);
                FLOAT d = VectorHelper.Distance(np, pp);
                clen += d;
                arclen[idx] = clen;
                pp = np;
            }
        }

        /// <summary>
        /// Point at which to sample the spline.
        /// </summary>
        public struct SamplePos
        {
            /// <summary>
            /// Index of sampled curve in the spline curves array.
            /// </summary>
            public readonly int Index;

            /// <summary>
            /// The "t" value from which to sample the curve.
            /// </summary>
            public readonly FLOAT Time;

            public SamplePos(int curveIndex, FLOAT t)
            {
                Index = curveIndex;
                Time = t;
            }
        }
    }

    public static class CurvePreprocess
    {
        private const FLOAT EPSILON = VectorHelper.EPSILON;

        /// <summary>
        /// Creates a list of equally spaced points that lie on the path described by straight line segments between
        /// adjacent points in the source list.
        /// </summary>
        /// <param name="src">Source list of points.</param>
        /// <param name="md">Distance between points on the new path.</param>
        /// <returns>List of equally-spaced points on the path.</returns>
        public static List<VECTOR> Linearize(List<VECTOR> src, FLOAT md)
        {
            if(src == null) throw new ArgumentNullException("src");
            if(md <= VectorHelper.EPSILON) throw new InvalidOperationException("md " + md + " is be less than epislon " + EPSILON);
            List<VECTOR> dst = new List<VECTOR>();
            if(src.Count > 0)
            {
                VECTOR pp = src[0];
                dst.Add(pp);
                FLOAT cd = 0;
                for(int ip = 1; ip < src.Count; ip++)
                {
                    VECTOR p0 = src[ip - 1];
                    VECTOR p1 = src[ip];
                    FLOAT td = VectorHelper.Distance(p0, p1);
                    if(cd + td > md)
                    {
                        FLOAT pd = md - cd;
                        dst.Add(VectorHelper.Lerp(p0, p1, pd / td));
                        FLOAT rd = td - pd;
                        while(rd > md)
                        {
                            rd -= md;
                            VECTOR np = VectorHelper.Lerp(p0, p1, (td - rd) / td);
                            if(!VectorHelper.EqualsOrClose(np, pp))
                            {
                                dst.Add(np);
                                pp = np;
                            }
                        }
                        cd = rd;
                    }
                    else
                    {
                        cd += td;
                    }
                }
                // last point
                VECTOR lp = src[src.Count - 1];
                if(!VectorHelper.EqualsOrClose(pp, lp))
                    dst.Add(lp);
            }
            return dst;
        }

        /// <summary>
        /// Removes any repeated points (that is, one point extemely close to the previous one). The same point can
        /// appear multiple times just not right after one another. This does not modify the input list. If no repeats
        /// were found, it returns the input list; otherwise it creates a new list with the repeats removed.
        /// </summary>
        /// <param name="pts">Initial list of points.</param>
        /// <returns>Either pts (if no duplciates were found), or a new list containing pts with duplicates removed.</returns>
        public static List<VECTOR> RemoveDuplicates(List<VECTOR> pts)
        {
            if(pts.Count < 2)
                return pts;

            // Common case -- no duplicates, so just return the source list
            VECTOR prev = pts[0];
            int len = pts.Count;
            int nDup = 0;
            for(int i = 1; i < len; i++)
            {
                VECTOR cur = pts[i];
                if(VectorHelper.EqualsOrClose(prev, cur))
                    nDup++;
                else
                    prev = cur;
            }

            if(nDup == 0)
                return pts;
            else
            {
                // Create a copy without them
                List<VECTOR> dst = new List<VECTOR>(len - nDup);
                prev = pts[0];
                dst.Add(prev);
                for(int i = 1; i < len; i++)
                {
                    VECTOR cur = pts[i];
                    if(!VectorHelper.EqualsOrClose(prev, cur))
                    {
                        dst.Add(cur);
                        prev = cur;
                    }
                }
                return dst;
            }
        }

        /// <summary>
        /// "Reduces" a set of line segments by removing points that are too far away. Does not modify the input list; returns
        /// a new list with the points removed.
        /// The image says it better than I could ever describe: http://upload.wikimedia.org/wikipedia/commons/3/30/Douglas-Peucker_animated.gif
        /// The wiki article: http://en.wikipedia.org/wiki/Ramer%E2%80%93Douglas%E2%80%93Peucker_algorithm
        /// Based on:  http://www.codeproject.com/Articles/18936/A-Csharp-Implementation-of-Douglas-Peucker-Line-Ap
        /// </summary>
        /// <param name="pts">Points to reduce</param>
        /// <param name="error">Maximum distance of a point to a line. Low values (~2-4) work well for mouse/touchscreen data.</param>
        /// <returns>A new list containing only the points needed to approximate the curve.</returns>
        public static List<VECTOR> RdpReduce(List<VECTOR> pts, FLOAT error)
        {
            if(pts == null) throw new ArgumentNullException("pts");
            pts = RemoveDuplicates(pts);
            if(pts.Count < 3)
                return new List<VECTOR>(pts);
            List<int> keepIndex = new List<int>(Math.Max(pts.Count / 2, 16));
            keepIndex.Add(0);
            keepIndex.Add(pts.Count - 1);
            RdpRecursive(pts, error, 0, pts.Count - 1, keepIndex);
            keepIndex.Sort();
            List<VECTOR> res = new List<VECTOR>(keepIndex.Count);
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach(int idx in keepIndex)
                res.Add(pts[idx]);
            return res;
        }

        private static void RdpRecursive(List<VECTOR> pts, FLOAT error, int first, int last, List<int> keepIndex)
        {
            int nPts = last - first + 1;
            if(nPts < 3)
                return;

            VECTOR a = pts[first];
            VECTOR b = pts[last];
            FLOAT abDist = VectorHelper.Distance(a, b);
            FLOAT aCrossB = VectorHelper.GetX(a) * VectorHelper.GetY(b) - VectorHelper.GetX(b) * VectorHelper.GetY(a);
            FLOAT maxDist = error;
            int split = 0;
            for(int i = first + 1; i < last - 1; i++)
            {
                VECTOR p = pts[i];
                FLOAT pDist = PerpendicularDistance(a, b, abDist, aCrossB, p);
                if(pDist > maxDist)
                {
                    maxDist = pDist;
                    split = i;
                }
            }

            if(split != 0)
            {
                keepIndex.Add(split);
                RdpRecursive(pts, error, first, split, keepIndex);
                RdpRecursive(pts, error, split, last, keepIndex);
            }
        }

        /// <summary>
        /// Finds the shortest distance between a point and a line. See: http://en.wikipedia.org/wiki/Distance_from_a_point_to_a_line
        /// </summary>
        /// <param name="a">First point of the line.</param>
        /// <param name="b">Last point of the line.</param>
        /// <param name="abDist">Distance between a and b (length of the line).</param>
        /// <param name="aCrossB">"a.X*b.Y - b.X*a.Y" This would be the Z-component of (⟪a.X, a.Y, 0⟫ ⨯ ⟪b.X, b.Y, 0⟫) in 3-space.</param>
        /// <param name="p">The point to test.</param>
        /// <returns>The perpendicular distance to the line.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // originally this method wasn't be inlined
        private static FLOAT PerpendicularDistance(VECTOR a, VECTOR b, FLOAT abDist, FLOAT aCrossB, VECTOR p)
        {
            // a profile with the test data showed that originally this was eating up ~44% of the runtime. So, this went through
            // several iterations of optimization and staring at the dissasembly. I tried different methods of using cross
            // products, doing the computation with larger vector types, etc... this is the best I could do in ~45 minutes
            // running on 3 hours of sleep, which is all scalar math, but RyuJIT puts it into XMM registers and does
            // ADDSS/SUBSS/MULSS/DIVSS because that's what it likes to do whenever it sees a vector in a function.
            FLOAT area = Math.Abs(aCrossB + 
                VectorHelper.GetX(b) * VectorHelper.GetY(p) + VectorHelper.GetX(p) * VectorHelper.GetY(a) -
                VectorHelper.GetX(p) * VectorHelper.GetY(b) - VectorHelper.GetX(a) * VectorHelper.GetY(p));
            FLOAT height = area / abDist;
            return height;
        }
    }

    /// <summary>
    /// This is the base class containing implementations common to <see cref="CurveFit"/> and <see cref="CurveBuilder"/>. Most of this
    /// is ported from http://tog.acm.org/resources/GraphicsGems/gems/FitCurves.c
    /// </summary>
    public abstract class CurveFitBase
    {
        protected const FLOAT EPSILON = VectorHelper.EPSILON;  // below this, we can't trust floating point values
        protected const int MAX_ITERS = 4;                     // maximum number of iterations of newton's method to run before giving up and splitting curve
        protected const int END_TANGENT_N_PTS = 8;             // maximum number of points to base end tangent on
        protected const int MID_TANGENT_N_PTS = 4;             // maximum number of points on each side to base mid tangent on
        
        /// <summary>
        /// Points in the whole line being used for fitting.
        /// </summary>
        protected readonly List<VECTOR> _pts = new List<VECTOR>(256);

        /// <summary>
        /// length of curve before each point (so, arclen[0] = 0, arclen[1] = distance(pts[0], pts[1]),
        /// arclen[2] = arclen[1] + distance(pts[1], pts[2]) ... arclen[n -1] = length of the entire curve, etc).
        /// </summary>
        protected readonly List<FLOAT> _arclen = new List<FLOAT>(256);

        /// <summary>
        /// current parametrization of the curve. When fitting, u[i] is the pameterization for the point in pts[first + i]. This is
        /// an optimization for CurveBuilder, since it might not need to allocate as big of a _u as is nesescary to hold the whole
        /// curve.
        /// </summary>
        protected readonly List<FLOAT> _u = new List<FLOAT>(256);

        /// <summary>
        /// maximum squared error before we split the curve
        /// </summary>
        protected FLOAT _squaredError;

        /// <summary>
        /// Tries to fit single Bezier curve to the points in [first ... last]. Destroys anything in <see cref="_u"/> in the process.
        /// Assumes there are at least two points to fit.
        /// </summary>
        /// <param name="first">Index of first point to consider.</param>
        /// <param name="last">Index of last point to consider (inclusive).</param>
        /// <param name="tanL">Tangent at teh start of the curve ("left").</param>
        /// <param name="tanR">Tangent on the end of the curve ("right").</param>
        /// <param name="curve">The fitted curve.</param>
        /// <param name="split">Point at which to split if this method returns false.</param>
        /// <returns>true if the fit was within error tolerence, false if the curve should be split. Even if this returns false, curve will contain
        /// a curve that somewhat fits the points; it's just outside error tolerance.</returns>
        protected bool FitCurve(int first, int last, VECTOR tanL, VECTOR tanR, out CubicBezier curve, out int split)
        {
            List<VECTOR> pts = _pts;
            int nPts = last - first + 1;
            if(nPts < 2)
            {
                throw new InvalidOperationException("INTERNAL ERROR: Should always have at least 2 points here");
            }
            else if(nPts == 2)
            {
                // if we only have 2 points left, estimate the curve using Wu/Barsky
                VECTOR p0 = pts[first];
                VECTOR p3 = pts[last];
                FLOAT alpha = VectorHelper.Distance(p0, p3) / 3;
                VECTOR p1 = (tanL * alpha) + p0;
                VECTOR p2 = (tanR * alpha) + p3;
                curve = new CubicBezier(p0, p1, p2, p3);
                split = 0;
                return true;
            }
            else
            {
                split = 0;
                ArcLengthParamaterize(first, last); // initially start u with a simple chord-length paramaterization
                curve = default(CubicBezier);
                for(int i = 0; i < MAX_ITERS + 1; i++)
                {
                    if(i != 0) Reparameterize(first, last, curve);                                  // use newton's method to find better parameters (except on first run, since we don't have a curve yet)
                    curve = GenerateBezier(first, last, tanL, tanR);                                // generate the curve itself
                    FLOAT error = FindMaxSquaredError(first, last, curve, out split);               // calculate error and get split point (point of max error)
                    if(error < _squaredError)  return true;                                         // if we're within error tolerence, awesome!
                }
                return false;
            }
        }

        /// <summary>
        /// Gets the tangent for the start of the cure.
        /// </summary>
        protected VECTOR GetLeftTangent(int last)
        {
            List<VECTOR> pts = _pts;
            List<FLOAT> arclen = _arclen;
            FLOAT totalLen = arclen[arclen.Count - 1];
            VECTOR p0 = pts[0];
            VECTOR tanL = VectorHelper.Normalize(pts[1] - p0);
            VECTOR total = tanL;
            FLOAT weightTotal = 1;
            last = Math.Min(END_TANGENT_N_PTS, last - 1);
            for(int i = 2; i <= last; i++)
            {
                FLOAT ti = 1 - (arclen[i] / totalLen);
                FLOAT weight = ti * ti * ti;
                VECTOR v = VectorHelper.Normalize(pts[i] - p0);
                total += v * weight;
                weightTotal += weight;
            }
            // if the vectors add up to zero (ie going opposite directions), there's no way to normalize them
            if(VectorHelper.Length(total) > EPSILON)
                tanL = VectorHelper.Normalize(total / weightTotal);
            return tanL;
        }

        /// <summary>
        /// Gets the tangent for the the end of the curve.
        /// </summary>
        protected VECTOR GetRightTangent(int first)
        {
            List<VECTOR> pts = _pts;
            List<FLOAT> arclen = _arclen;
            FLOAT totalLen = arclen[arclen.Count - 1];
            VECTOR p3 = pts[pts.Count - 1];
            VECTOR tanR = VectorHelper.Normalize(pts[pts.Count - 2] - p3);
            VECTOR total = tanR;
            FLOAT weightTotal = 1;
            first = Math.Max(pts.Count - (END_TANGENT_N_PTS + 1), first + 1);
            for(int i = pts.Count - 3; i >= first; i--)
            {
                FLOAT t = arclen[i] / totalLen;
                FLOAT weight = t * t * t;
                VECTOR v = VectorHelper.Normalize(pts[i] - p3);
                total += v * weight;
                weightTotal += weight;
            }
            if(VectorHelper.Length(total) > EPSILON)
                tanR = VectorHelper.Normalize(total / weightTotal);
            return tanR;
        }

        /// <summary>
        /// Gets the tangent at a given point in the curve.
        /// </summary>
        protected VECTOR GetCenterTangent(int first, int last, int split)
        {
            List<VECTOR> pts = _pts;
            List<FLOAT> arclen = _arclen;

            // because we want to maintain C1 continuity on the spline, the tangents on either side must be inverses of one another
            Debug.Assert(first < split && split < last);
            FLOAT splitLen = arclen[split];
            VECTOR pSplit = pts[split];

            // left side
            FLOAT firstLen = arclen[first];
            FLOAT partLen = splitLen - firstLen;
            VECTOR total = default(VECTOR);
            FLOAT weightTotal = 0;
            for(int i = Math.Max(first, split - MID_TANGENT_N_PTS); i < split; i++)
            {
                FLOAT t = (arclen[i] - firstLen) / partLen;
                FLOAT weight = t * t * t;
                VECTOR v = VectorHelper.Normalize(pts[i] - pSplit);
                total += v * weight;
                weightTotal += weight;
            }
            VECTOR tanL = VectorHelper.Length(total) > EPSILON && weightTotal > EPSILON ? 
                VectorHelper.Normalize(total / weightTotal) :
                VectorHelper.Normalize(pts[split - 1] - pSplit);

            // right side
            partLen = arclen[last] - splitLen;
            int rMax = Math.Min(last, split + MID_TANGENT_N_PTS);
            total = default(VECTOR);
            weightTotal = 0;
            for(int i = split + 1; i <= rMax; i++)
            {
                FLOAT ti = 1 - ((arclen[i] - splitLen) / partLen);
                FLOAT weight = ti * ti * ti;
                VECTOR v = VectorHelper.Normalize(pSplit- pts[i]);
                total += v * weight;
                weightTotal += weight;
            }
            VECTOR tanR = VectorHelper.Length(total) > EPSILON && weightTotal > EPSILON ?
                VectorHelper.Normalize(total / weightTotal) :
                VectorHelper.Normalize(pSplit - pts[split + 1]);

            // The reason we seperate this into two halves is because we want the right and left tangents to be weighted
            // equally no matter the weights of the individual parts of them, so that one of the curves doesn't get screwed
            // for the pleasure of the other half
            total = tanL + tanR;
            
            // Since the points are never coincident, the vector between any two of them will be normalizable, however this can happen in some really
            // odd cases when the points are going directly opposite directions (therefore the tangent is undefined)
            if(VectorHelper.LengthSquared(total) < EPSILON)
            {
                // try one last time using only the three points at the center, otherwise just use one of the sides
                tanL = VectorHelper.Normalize(pts[split - 1] - pSplit);
                tanR = VectorHelper.Normalize(pSplit - pts[split + 1]);
                total = tanL + tanR;
                return VectorHelper.LengthSquared(total) < EPSILON ? tanL : VectorHelper.Normalize(total / 2);
            }
            else
            {
                return VectorHelper.Normalize(total / 2);
            }
        }

        /// <summary>
        /// Builds the arc length array using the points array. Assumes _pts has points and _arclen is empty.
        /// </summary>
        protected void InitializeArcLengths()
        {
            List<VECTOR> pts = _pts;
            List<FLOAT> arclen = _arclen;
            int count = pts.Count;
            Debug.Assert(arclen.Count == 0);
            arclen.Add(0);
            FLOAT clen = 0;
            VECTOR pp = pts[0];
            for(int i = 1; i < count; i++)
            {
                VECTOR np = pts[i];
                clen += VectorHelper.Distance(pp, np);
                arclen.Add(clen);
                pp = np;
            }
        }

        /// <summary>
        /// Initializes the first (last - first) elements of u with scaled arc lengths.
        /// </summary>
        protected void ArcLengthParamaterize(int first, int last)
        {
            List<FLOAT> arclen = _arclen;
            List<FLOAT> u = _u;

            u.Clear();
            FLOAT diff = arclen[last] - arclen[first];
            FLOAT start = arclen[first];
            int nPts = last - first;
            u.Add(0);
            for(int i = 1; i < nPts; i++)
                u.Add((arclen[first + i] - start) / diff);
            u.Add(1);
        }

        /// <summary>
        /// generates a bezier curve for the segment using a least-squares approximation. for the derivation of this and why it works,
        /// see http://read.pudn.com/downloads141/ebook/610086/Graphics_Gems_I.pdf page 626 and beyond.
        /// </summary>
        protected CubicBezier GenerateBezier(int first, int last, VECTOR tanL, VECTOR tanR)
        {
            List<VECTOR> pts = _pts;
            List<FLOAT> u = _u;
            int nPts = last - first + 1;
            VECTOR p0 = pts[first], p3 = pts[last]; // first and last points of curve areactual points on data
            FLOAT c00 = 0, c01 = 0, c11 = 0, x0 = 0, x1 = 0; // matrix members -- both C[0,1] and C[1,0] are the same, stored in c01
            for(int i = 1; i < nPts; i++)
            {
                // Calculate cubic bezier multipliers
                FLOAT t = u[i];
                FLOAT ti = 1 - t;
                FLOAT t0 = ti * ti * ti;
                FLOAT t1 = 3 * ti * ti * t;
                FLOAT t2 = 3 * ti * t * t;
                FLOAT t3 = t * t * t;

                // For X matrix; moving this up here since profiling shows it's better up here (maybe a0/a1 not in registers vs only v not in regs)
                VECTOR s = (p0 * t0) + (p0 * t1) + (p3 * t2) + (p3 * t3); // NOTE: this would be Q(t) if p1=p0 and p2=p3
                VECTOR v = pts[first + i] - s;

                // C matrix
                VECTOR a0 = tanL * t1;
                VECTOR a1 = tanR * t2;
                c00 += VectorHelper.Dot(a0, a0);
                c01 += VectorHelper.Dot(a0, a1);
                c11 += VectorHelper.Dot(a1, a1);

                // X matrix
                x0 += VectorHelper.Dot(a0, v);
                x1 += VectorHelper.Dot(a1, v);
            }

            // determinents of X and C matrices
            FLOAT det_C0_C1 = c00 * c11 - c01 * c01;
            FLOAT det_C0_X = c00 * x1 - c01 * x0;
            FLOAT det_X_C1 = x0 * c11 - x1 * c01;
            FLOAT alphaL = det_X_C1 / det_C0_C1;
            FLOAT alphaR = det_C0_X / det_C0_C1;
            
            // if alpha is negative, zero, or very small (or we can't trust it since C matrix is small), fall back to Wu/Barsky heuristic
            FLOAT linDist = VectorHelper.Distance(p0, p3);
            FLOAT epsilon2 = EPSILON * linDist;
            if(Math.Abs(det_C0_C1) < EPSILON || alphaL < epsilon2 || alphaR < epsilon2)
            {
                FLOAT alpha = linDist / 3;
                VECTOR p1 = (tanL * alpha) + p0;
                VECTOR p2 = (tanR * alpha) + p3;
                return new CubicBezier(p0, p1, p2, p3);
            }
            else
            {
                VECTOR p1 = (tanL * alphaL) + p0;
                VECTOR p2 = (tanR * alphaR) + p3;
                return new CubicBezier(p0, p1, p2, p3);
            }
        }

        /// <summary>
        /// Attempts to find a slightly better parameterization for u on the given curve.
        /// </summary>
        protected void Reparameterize(int first, int last, CubicBezier curve)
        {
            List<VECTOR> pts = _pts;
            List<FLOAT> u = _u;
            int nPts = last - first;
            for(int i = 1; i < nPts; i++)
            {
                VECTOR p = pts[first + i];
                FLOAT t = u[i];
                FLOAT ti = 1 - t;

                // Control vertices for Q'
                VECTOR qp0 = (curve.p1 - curve.p0) * 3;
                VECTOR qp1 = (curve.p2 - curve.p1) * 3;
                VECTOR qp2 = (curve.p3 - curve.p2) * 3;

                // Control vertices for Q''
                VECTOR qpp0 = (qp1 - qp0) * 2;
                VECTOR qpp1 = (qp2 - qp1) * 2;

                // Evaluate Q(t), Q'(t), and Q''(t)
                VECTOR p0 = curve.Sample(t);
                VECTOR p1 = ((ti * ti) * qp0) + ((2 * ti * t) * qp1) + ((t * t) * qp2);
                VECTOR p2 = (ti * qpp0) + (t * qpp1);

                // these are the actual fitting calculations using http://en.wikipedia.org/wiki/Newton%27s_method
                // We can't just use .X and .Y because Unity uses lower-case "x" and "y".
                FLOAT num = ((VectorHelper.GetX(p0) - VectorHelper.GetX(p)) * VectorHelper.GetX(p1)) + ((VectorHelper.GetY(p0) - VectorHelper.GetY(p)) * VectorHelper.GetY(p1));
                FLOAT den = (VectorHelper.GetX(p1) * VectorHelper.GetX(p1)) + (VectorHelper.GetY(p1) * VectorHelper.GetY(p1)) + ((VectorHelper.GetX(p0) - VectorHelper.GetX(p)) * VectorHelper.GetX(p2)) + ((VectorHelper.GetY(p0) - VectorHelper.GetY(p)) * VectorHelper.GetY(p2));
                if(Math.Abs(den) > EPSILON)
                    u[i] = t - num/den;
            }
        }

        /// <summary>
        /// Computes the maximum squared distance from a point to the curve using the current paramaterization.
        /// </summary>
        protected FLOAT FindMaxSquaredError(int first, int last, CubicBezier curve, out int split)
        {
            List<VECTOR> pts = _pts;
            List<FLOAT> u = _u;
            int s = (last - first + 1) / 2;
            int nPts = last - first + 1;
            FLOAT max = 0;
            for(int i = 1; i < nPts; i++)
            {
                VECTOR v0 = pts[first + i];
                VECTOR v1 = curve.Sample(u[i]);
                FLOAT d = VectorHelper.DistanceSquared(v0, v1);
                if(d > max)
                {
                    max = d;
                    s = i;
                }
            }

            // split at point of maximum error
            split = s + first;
            if(split <= first)
                split = first + 1;
            if(split >= last)
                split = last - 1;

            return max;
        }
    }

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

    /// <summary>
    /// This is a version of <see cref="CurveFit"/> that works on partial curves so that a spline can be built in "realtime"
    /// as the user is drawing it. The quality of the generated spline may be lower, and it might use more Bezier curves
    /// than is necessary. Only the most recent two curves will be modified, once another curve is being built on top of it, curves
    /// lower in the "stack" are permanent. This reduces visual jumpiness as the user draws since the entire spline doesn't move
    /// around as points are added. It only uses linearization-based preprocessing; it doesn't support the RDP method.
    /// 
    /// Add points using the <see cref="AddPoint"/> method.To get the results, either enumerate (foreach) the CurveBuilder itself
    /// or use the <see cref="Curves"/> property. The results might be updated every time a point is added.
    /// </summary>
    public sealed class CurveBuilder : CurveFitBase, IEnumerable<CubicBezier>
    {
        private readonly List<CubicBezier> _result;                      // result curves (updated whenever a new point is added)
        private readonly ReadOnlyCollection<CubicBezier> _resultView;    // ReadOnlyCollection view of _result
        private readonly FLOAT _linDist;                                 // distance between points
        private VECTOR _prev;                                            // most recent point added
        private VECTOR _tanL;                                            // left tangent of current curve (can't change this except on first curve or we'll lose C1 continuity)
        private FLOAT _totalLength;                                      // Total length of the curve so far (for updating arclen)
        private int _first;                                              // Index of first point in current curve

        public CurveBuilder(FLOAT linDist, FLOAT error)
        {
            _squaredError = error * error;
            _result = new List<CubicBezier>(16);
            _resultView = new ReadOnlyCollection<CubicBezier>(_result);
            _linDist = linDist;
        }

        /// <summary>
        /// Adds a data point to the curve builder. This doesn't always result in the generated curve changing immediately.
        /// </summary>
        /// <param name="p">The data point to add.</param>
        /// <returns><see cref="AddPointResult"/> for info about this.</returns>
        public AddPointResult AddPoint(VECTOR p)
        {
            VECTOR prev = _prev;
            List<VECTOR> pts = _pts;
            int count = pts.Count;
            if(count != 0)
            {
                FLOAT td = VectorHelper.Distance(prev, p);
                FLOAT md = _linDist;
                if(td > md)
                {
                    int first = int.MaxValue;
                    bool add = false;
                    FLOAT rd = td - md;
                    // OPTIMIZE if we're adding many points at once, we could do them in a batch
                    VECTOR dir = VectorHelper.Normalize(p - prev);
                    do
                    {
                        VECTOR np = prev + dir * md;
                        AddPointResult res = AddInternal(np);
                        first = Math.Min(first, res.FirstChangedIndex);
                        add |= res.WasAdded;
                        prev = np;
                        rd -= md;
                    } while(rd > md);
                    _prev = prev;
                    return new AddPointResult(first, add);
                }
                return AddPointResult.NO_CHANGE;
            }
            else
            {
                _prev = p;
                _pts.Add(p);
                _arclen.Add(0);
                return AddPointResult.NO_CHANGE; // no curves were actually added yet
            }
        }

        private AddPointResult AddInternal(VECTOR np)
        {
            List<VECTOR> pts = _pts;
            int last = pts.Count;
            Debug.Assert(last != 0); // should always have one point at least
            _pts.Add(np);
            _arclen.Add(_totalLength = _totalLength + _linDist);
            if(last == 1)
            {
                // This is the second point
                Debug.Assert(_result.Count == 0);
                VECTOR p0 = pts[0];
                VECTOR tanL = VectorHelper.Normalize(np - p0);
                VECTOR tanR = -tanL;
                _tanL = tanL;
                FLOAT alpha = _linDist / 3;
                VECTOR p1 = (tanL * alpha) + p0;
                VECTOR p2 = (tanR * alpha) + np;
                _result.Add(new CubicBezier(p0, p1, p2, np));
                return new AddPointResult(0, true);
            }
            else
            {
                int lastCurve = _result.Count - 1;
                int first = _first;

                // If we're on the first curve, we're free to improve the left tangent
                VECTOR tanL = lastCurve == 0 ? GetLeftTangent(last) : _tanL;

                // We can always do the end tangent
                VECTOR tanR = GetRightTangent(first);

                // Try fitting with the new point
                int split;
                CubicBezier curve;
                if(FitCurve(first, last, tanL, tanR, out curve, out split))
                {
                    _result[lastCurve] = curve;
                    return new AddPointResult(lastCurve, false);
                }
                else
                {
                    // Need to split
                    // first, get mid tangent
                    VECTOR tanM1 = GetCenterTangent(first, last, split);
                    VECTOR tanM2 = -tanM1;

                    // PERHAPS do a full fitRecursive here since its our last chance?

                    // our left tangent might be based on points outside the new curve (this is possible for mid tangents too
                    // but since we need to maintain C1 continuity, it's too late to do anything about it)
                    if(first == 0 && split < END_TANGENT_N_PTS)
                        tanL = GetLeftTangent(split);

                    // do a final pass on the first half of the curve
                    int unused;
                    FitCurve(first, split, tanL, tanM1, out curve, out unused);
                    _result[lastCurve] = curve;

                    // perpare to fit the second half
                    FitCurve(split, last, tanM2, tanR, out curve, out unused);
                    _result.Add(curve);
                    _first = split;
                    _tanL = tanM2;

                    return new AddPointResult(lastCurve, true);
                }
            }
        }

        /// <summary>
        /// Clears the curve builder.
        /// </summary>
        public void Clear()
        {
            _result.Clear();
            _pts.Clear();
            _arclen.Clear();
            _u.Clear();
            _totalLength = 0;
            _first = 0;
            _tanL = default(VECTOR);
            _prev = default(VECTOR);
        }

        // We provide these for both convience and performance, since a call to List<T>.GetEnumerator() doesn't actually allocate if
        // the type is never boxed
        public List<CubicBezier>.Enumerator GetEnumerator() { return _result.GetEnumerator(); } 
        IEnumerator<CubicBezier> IEnumerable<CubicBezier>.GetEnumerator() { return GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        /// <summary>
        /// The current curves in the builder.
        /// </summary>
        public ReadOnlyCollection<CubicBezier> Curves { get { return _resultView; } }

        /// <summary>
        /// Changes made to the CurveBuilder.curves list after a call to <see cref="CurveBuilder.AddPoint"/>.
        /// This seems like a prime candidate for an F#-style discriminated union/algebreic data type.
        /// </summary>
        public struct AddPointResult
        {
            private readonly int _data; // packed value... need this so that default(AddPointResult) is no change

            /// <summary>
            /// No changes were made.
            /// </summary>
            public static readonly AddPointResult NO_CHANGE = default(AddPointResult);
            
            /// <summary>
            /// Were any curves changed or added?
            /// </summary>
            public bool WasChanged { get { return _data != 0; } }

            /// <summary>
            /// Index into curves array of first curve that was changed, or -1 if no curves were changed.
            /// All curves after this are assumed to have changed/been added as well. If a curve was added
            /// this is a considered a "change" so <see cref="WasAdded"/> will always be true.
            /// </summary>
            public int FirstChangedIndex { get { return Math.Abs(_data) - 1; } }

            /// <summary>
            /// Were any curves added?
            /// </summary>
            public bool WasAdded { get { return _data < 0; } }

            public AddPointResult(int firstChangedIndex, bool curveAdded)
            {
                if(firstChangedIndex < 0 || firstChangedIndex == int.MaxValue)
                    throw new InvalidOperationException("firstChangedIndex must be greater than zero");
                _data = (firstChangedIndex + 1) * (curveAdded ? -1 : 1);
            }
        }
    }

    /// <summary>
    /// Wraps a <see cref="CurveBuilder"/> and <see cref="Spline"/> together. Allows you to add data points as they come in and
    /// generate a smooth spline from them without doing unesescary computation.
    /// </summary>
    public sealed class SplineBuilder
    {
        private readonly CurveBuilder _builder;      // Underlying curve fitter
        private readonly Spline _spline;             // Underlyig spline

        public SplineBuilder(FLOAT pointDistance, FLOAT error, int samplesPerCurve)
        {
            _builder = new CurveBuilder(pointDistance, error);
            _spline = new Spline(samplesPerCurve);
        }

        /// <summary>
        /// Adds a data point.
        /// </summary>
        /// <param name="p">Data point to add.</param>
        /// <returns>True if the spline was modified.</returns>
        public bool Add(VECTOR p)
        {
            CurveBuilder.AddPointResult res = _builder.AddPoint(p);
            if(!res.WasChanged)
                return false;

            // update spline
            ReadOnlyCollection<CubicBezier> curves = _builder.Curves;
            if(res.WasAdded && curves.Count == 1)
            {
                // first curve
                Debug.Assert(_spline.Curves.Count == 0);
                _spline.Add(curves[0]);
            }
            else if(res.WasAdded)
            {
                // split
                _spline.Update(_spline.Curves.Count - 1, curves[res.FirstChangedIndex]);
                for(int i = res.FirstChangedIndex + 1; i < curves.Count; i++)
                    _spline.Add(curves[i]);
            }
            else
            {
                // last curve updated
                Debug.Assert(res.FirstChangedIndex == curves.Count - 1);
                _spline.Update(_spline.Curves.Count - 1, curves[curves.Count - 1]);
            }

            return true;
        }

        /// <summary>
        /// Gets the position of a point on the spline that's close to the desired point along the spline. For example, if u = 0.5, then a point
        /// that's about halfway through the spline will be returned. The returned point will lie exactly on one of the curves that make up the
        /// spline.
        /// </summary>
        /// <param name="u">How far along the spline to sample (for example, 0.5 will be halfway along the length of the spline). Should be between 0 and 1.</param>
        /// <returns>The position on the spline.</returns>
        public VECTOR Sample(FLOAT u)
        {
            return _spline.Sample(u);
        }

        /// <summary>
        /// Gets the tangent of a point on the spline that's close to the desired point along the spline. For example, if u = 0.5, then the direction vector
        /// that's about halfway through the spline will be returned. The returned value will be a normalized direction vector.
        /// </summary>
        /// <param name="u">How far along the spline to sample (for example, 0.5 will be halfway along the length of the spline). Should be between 0 and 1.</param>
        /// <returns>The position on the spline.</returns>
        public VECTOR Tangent(FLOAT u)
        {
            Spline.SamplePos pos = _spline.GetSamplePosition(u);
            return _spline.Curves[pos.Index].Tangent(pos.Time);
        }

        /// <summary>
        /// Clears the SplineBuilder.
        /// </summary>
        public void Clear()
        {
            _builder.Clear();
            _spline.Clear();
        }

        /// <summary>
        /// The curves that make up the spline.
        /// </summary>
        public ReadOnlyCollection<CubicBezier> Curves
        {
            get
            {
                return _spline.Curves;
            }
        }
    }

    /// <summary>
    /// The point of this class is to abstract some of the functions of Vector2 so they can be used with either System.Windows.Vector,
    /// System.Numerics.Vector2, UnityEngine.Vector2.
    /// </summary>
    public static class VectorHelper
    {
        /// <summary>
        /// Below this, don't trust the results of floating point calculations.
        /// </summary>
        public const FLOAT EPSILON = 1.2e-12f;

#if SYSTEM_WINDOWS_VECTOR
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static FLOAT Distance(VECTOR a, VECTOR b) { return (a - b).Length; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static FLOAT DistanceSquared(VECTOR a, VECTOR b) { return (a - b).LengthSquared; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static FLOAT Dot(VECTOR a, VECTOR b) { return a.X * b.X + a.Y * b.Y; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static VECTOR Normalize(VECTOR v) { v.Normalize(); return v; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static FLOAT Length(VECTOR v) { return v.Length; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static FLOAT LengthSquared(VECTOR v) { return v.LengthSquared; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static VECTOR Lerp(VECTOR a, VECTOR b, FLOAT amount) { return new VECTOR(a.X + ((b.X - a.X) * amount), a.Y + ((b.Y - a.Y) * amount)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static FLOAT GetX(VECTOR v) { return v.X; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static FLOAT GetY(VECTOR v) { return v.Y; }
#elif UNITY
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static FLOAT Distance(VECTOR a, VECTOR b) { return VECTOR.Distance(a, b); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static FLOAT DistanceSquared(VECTOR a, VECTOR b) { float dx = a.x - b.x; float dy = a.y - b.y; return dx*dx + dy*dy; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static FLOAT Dot(VECTOR a, VECTOR b) { return VECTOR.Dot(a, b); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static VECTOR Normalize(VECTOR v) { v.Normalize(); return v; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static FLOAT Length(VECTOR v) { return v.magnitude; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static FLOAT LengthSquared(VECTOR v) { return v.sqrMagnitude; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static VECTOR Lerp(VECTOR a, VECTOR b, FLOAT amount) { return VECTOR.Lerp(a, b, amount); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static FLOAT GetX(VECTOR v) { return v.x; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static FLOAT GetY(VECTOR v) { return v.y; }
#else // SYSTEM_NUMERICS_VECTOR -- also works for SharpDX.Vector2 and Microsoft.Xna.Framework.Vector2 AFAICT
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static FLOAT Distance(VECTOR a, VECTOR b) { return VECTOR.Distance(a, b); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static FLOAT DistanceSquared(VECTOR a, VECTOR b) { return VECTOR.DistanceSquared(a, b); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static FLOAT Dot(VECTOR a, VECTOR b) { return VECTOR.Dot(a, b); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static VECTOR Normalize(VECTOR v) { return VECTOR.Normalize(v); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static FLOAT Length(VECTOR v) { return v.Length(); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static FLOAT LengthSquared(VECTOR v) { return v.LengthSquared(); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static VECTOR Lerp(VECTOR a, VECTOR b, FLOAT amount) { return VECTOR.Lerp(a, b, amount); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static FLOAT GetX(VECTOR v) { return v.X; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static FLOAT GetY(VECTOR v) { return v.Y; }
#endif

        /// <summary>
        /// Checks if two vectors are equal within a small bounded error.
        /// </summary>
        /// <param name="v1">First vector to compare.</param>
        /// <param name="v2">Second vector to compare.</param>
        /// <returns>True iff the vectors are almost equal.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsOrClose(VECTOR v1, VECTOR v2)
        {
            return DistanceSquared(v1, v2) < EPSILON;
        }
    }
}