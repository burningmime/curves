using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Media;

namespace burningmime.util.wpf
{
    /// <summary>
    /// Really simple color struct for a packed color. It is in BGRA order, that is it conforms to <see cref="PixelFormats.Bgra32"/>,
    /// but all the methods are in ARGB order to make it easier to work with. Only tested on little-endian (don't think WPF is supported on
    /// any big endian devices? maybe ARM in Win 10?).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ColorArgb : IEquatable<ColorArgb>
    {
        public byte b;
        public byte g;
        public byte r;
        public byte a;

        public ColorArgb(byte a, byte r, byte g, byte b)
        {
            this.b = b;
            this.g = g;
            this.r = r;
            this.a = a;
        }

        // Equality
        public bool Equals(ColorArgb other) { return b == other.b && g == other.g && r == other.r && a == other.a; }
        public override bool Equals(object obj) { return obj is ColorArgb && Equals((ColorArgb) obj); }
        public override int GetHashCode() { return toPackedBgra(); }
        public static bool operator ==(ColorArgb left, ColorArgb right) { return left.Equals(right); }
        public static bool operator !=(ColorArgb left, ColorArgb right) { return !left.Equals(right); }

        // To/from WPF color
        public Color toWpfColor() { return Color.FromArgb(a, r, g, b); }
        public static ColorArgb fromWpfColor(Color c) { return new ColorArgb(c.A, c.R, c.G, c.B); }
        
        // To/from int
        public int toPackedBgra() { return ((a << 24) | (r << 16) | (g << 8) | b); }
        public static ColorArgb fromPackedBgra(int bgra) { return new ColorArgb((byte) ((bgra & 0xff000000) >> 24), (byte)((bgra & 0x00ff0000) >> 16), (byte)((bgra & 0x0000ff00) >> 8), (byte)(bgra & 0x000000ff)); }
        public static ColorArgb fromPackedBgra(uint bgra) { return new ColorArgb((byte) ((bgra & 0xff000000) >> 24), (byte)((bgra & 0x00ff0000) >> 16), (byte)((bgra & 0x0000ff00) >> 8), (byte)(bgra & 0x000000ff)); }
        
        // To/from floats in 0-1 range
        // ReSharper disable ParameterHidesMember
        public static ColorArgb fromFloats(float a, float r, float g, float b) { return new ColorArgb(toByte(a), toByte(r), toByte(g), toByte(b)); }
        public void toFloats(out float a, out float r, out float g, out float b) { a = toFloat(this.a); r = toFloat(this.r); g = toFloat(this.g); b = toFloat(this.b);  }
        private static byte toByte(float f) { return (byte) (f * byte.MaxValue).clamp(0, byte.MaxValue); }
        private static float toFloat(byte b) { return b / (float) byte.MaxValue; }
        // ReSharper restore ParameterHidesMember
        
        // To/from string
        public override string ToString() { return string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}{3:X2}", a, r, g, b); }
        // ReSharper disable once PossibleNullReferenceException
        public static ColorArgb parse(string s) { return fromWpfColor((Color) UiUtils.colorConverter.ConvertFromInvariantString(s)); }
        public static bool tryParse(string s, out ColorArgb value)
        {
            try
            {
                // OPTIMIZE could implement this locally for performance
                object obj = UiUtils.colorConverter.ConvertFromInvariantString(s);
                if(!(obj is Color))
                {
                    value = default(ColorArgb);
                    return false;
                }
                value = fromWpfColor((Color) obj);
                return true;
            }
            catch(Exception)
            {
                value = default(ColorArgb);
                return false;
            }
        }
    }
}