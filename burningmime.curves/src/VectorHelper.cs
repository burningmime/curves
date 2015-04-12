using System.Runtime.CompilerServices;

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
#error Unknown vector type -- must define one of SYSTEM_WINDOWS_VECTOR, SYSTEM_NUMERICS_VECTOR, or UNITY
#endif

namespace burningmime.curves
{
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