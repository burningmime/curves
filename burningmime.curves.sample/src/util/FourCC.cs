using System;
using System.Runtime.InteropServices;

namespace burningmime.util
{
    /// <summary>
    /// Based on SharpDX FourCC struct. Simple struct for storing a 4 character code as a uint, for file format specifiers
    /// and the like.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 4)]
    public struct FourCC : IEquatable<FourCC>
    {
        private readonly int value;
        public FourCC(int value) { this.value = value; }
        public FourCC(char c1, char c2, char c3, char c4) { value = (((byte) c1) << 0 | ((byte) c2) << 8 | ((byte) c3) << 16 | ((byte) c4) << 24); }

        public FourCC(string s)
        {
            if(s == null || s.Length != 4)
                throw new ArgumentException("string s (" + s + ") must have length of 4");
            value = (((byte) s[0]) << 0 | ((byte) s[1]) << 8 | ((byte) s[2]) << 16 | ((byte) s[3] << 24));
        }

        public FourCC(byte[] b)
        {
            if(b == null || b.Length < 4)
                throw new ArgumentException("byte array must be at least 4 elements long");
            value = (b[0] << 0 | b[1] << 8 | b[2] << 16 | b[3] << 24);
        }

        public FourCC(byte[] b, int ofs)
        {
            if(b == null || ofs + 4 >= b.Length)
                throw new ArgumentException("Array must have at least 4 elements after given offset");
            value = (b[ofs + 0] << 0 | b[ofs + 1] << 8 | b[ofs + 2] << 16 | b[ofs + 3] << 24);
        }

        public static implicit operator int(FourCC c) { return c.value; }
        public static explicit operator FourCC(int v) { return new FourCC(v); }
        public static explicit operator FourCC(string s) { return new FourCC(s); }

        public override string ToString()
        {
            return new string(new[]
            {
                (char) (value >> 0  & byte.MaxValue),
                (char) (value >> 8  & byte.MaxValue),
                (char) (value >> 16 & byte.MaxValue),
                (char) (value >> 24 & byte.MaxValue),
            });
        }

        public bool Equals(FourCC other) { return value == other.value; }
        public override bool Equals(object obj) { return obj is FourCC && Equals((FourCC) obj); }
        public override int GetHashCode() { return value; }
        public static bool operator ==(FourCC left, FourCC right) { return left.Equals(right); }
        public static bool operator !=(FourCC left, FourCC right) { return !left.Equals(right); }
    }
}
