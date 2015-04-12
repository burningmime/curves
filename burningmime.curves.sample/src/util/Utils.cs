using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

// don't pollute namespace with System.Xml
using IXmlLineInfo = System.Xml.IXmlLineInfo;

namespace burningmime.util
{
    /// <summary>
    /// Class for static functions without anywhere better to be.
    /// </summary>
    public static class Utils
    {
        #region Arrays & Collections

        /// <summary>
        /// Performs a shallow copy of an array.
        /// </summary>
        public static T[] dup<T>(this T[] arr)
        {
            int len = arr.Length;
            T[] ret = new T[len];
            Array.Copy(arr, 0, ret, 0, len);
            return ret;
        }

        /// <summary>
        /// Creates a typed shallow copy of an array. You (almost) always should use <see cref="dup{T}(T[])"/> instead of this one.
        /// This is useful for changing the type of the array or creating an object[] array (boxing value types) if you have a generic System.Array.
        /// </summary>
        public static T[] dupWithType<T>(this Array arr)
        {
            int len = arr.Length;
            T[] ret = new T[len];
            Array.Copy(arr, 0, ret, 0, len);
            return ret;
        }

        /// <summary>
        /// Formats an IEnumerable into a string like: [1, 2, 3]
        /// </summary>
        public static string toStringPretty<T>(this IEnumerable<T> arr, bool braces = true)
        {
            // PERHAPS for long arrays this could be rewritten to do something like: "[1, 2, 3, ... 10]" instead of "[1, 2, 3, 4, 5, 6, 7, 8, 9, 10]"
            if (null == arr)
                return braces ? "[]" : "";
            StringBuilder res = new StringBuilder();
            if (braces)
                res.Append("[");
            int i = 0;
            foreach (T obj in arr)
            {
                if (i != 0)
                    res.Append(", ");
                res.Append(obj);
                i++;
            }
            if (braces)
                res.Append("]");
            return res.ToString();
        }

        /// <summary>
        /// Creates an IComparer&lt;T&gt; that uses the given comparison function. Be sure to cache it if possible.
        /// </summary>
        public static IComparer<T> getComparer<T>(Comparison<T> compare) { return new ComparerImpl<T>(compare); }
        private sealed class ComparerImpl<T> : IComparer<T>
        {
            private readonly Comparison<T> _compare;
            public ComparerImpl(Comparison<T> compare) { _compare = compare; }
            public int Compare(T x, T y) { return _compare(x, y); }
        }
        
        /// <summary>
        /// Gets a value or returns the given default.
        /// </summary>
        public static TValue getOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue d = default(TValue))
            { TValue value; return dictionary.TryGetValue(key, out value) ? value : d; }
        
        /// <summary>
        /// If key exists in dictionary, returns dictionary[key]. Otherwise, creates a new TValue(), adds it to the dictionary, and returns it.
        /// </summary>
        public static TValue getOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) where TValue : new()
        {
            TValue value;
            if(dictionary.TryGetValue(key, out value))
                return value;
            value = new TValue();
            dictionary[key] = value;
            return value;
        }

        /// <summary>
        /// If key exists in dictionary, returns dictionary[key]. Otherwise, adds the default value to dicrionary, and returns it.
        /// </summary>
        public static TValue getOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue d)
        {
            TValue value;
            if(dictionary.TryGetValue(key, out value))
                return value;
            dictionary[key] = d;
            return d;
        }

        /// <summary>
        /// If key exists in the dictionary, returns dictionary[key]. Otherwise uses the given function to construct a new value,
        /// adds that value, then returns it.
        /// </summary>
        public static TValue getOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> create)
        {
            TValue value;
            if(dictionary.TryGetValue(key, out value))
                return value;
            value = create(key);
            dictionary[key] = value;
            return value;
        }
        
        /// <summary>
        /// Adds all the items from the source to the target collection.
        /// </summary>
        public static void addAll<TSource, TDest>(this ICollection<TDest> collection, IEnumerable<TSource> items) where TSource : TDest
        {
            foreach(TSource item in items)
                collection.Add(item);
        }

        /// <summary>
        /// Removes items in the list which match the predicate. Potentially slow [O(n^2)ish] for long lists because it shifts the entire array back each time an item is
        /// removed, but sequential access = incredibly fast on most processors so it shouldn't be an issue unless the list is huge (>10,000 elements).
        /// </summary>
        public static void removeWhere<T>(this List<T> list, Predicate<T> condition)
        {
            int i = 0;
            while(i < list.Count)
                if(condition(list[i]))
                    list.RemoveAt(i);
                else
                    i++;
        }
        #endregion

        #region Event Handling
        /// <summary>
        /// Raises event if handler is not null.
        /// </summary>
        public static void raise(this PropertyChangedEventHandler handler, object sender, string propertyName)
        {
            if(handler != null)
                handler(sender, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Raises event if handler is not null.
        /// </summary>
        public static void raise(this PropertyChangedEventHandler handler, object sender, PropertyChangedEventArgs args)
        {
            if(handler != null)
                handler(sender, args);
        }

        /// <summary>
        /// Raises event if handler is not null.
        /// </summary>
        public static void raise<TArgs>(this EventHandler<TArgs> handler, object sender, TArgs args) where TArgs : EventArgs
        {
            if(handler != null)
                handler(sender, args);
        }

        /// <summary>
        /// Raises event if handler is not null.
        /// </summary>
        public static void raise(this Action handler)
        {
            if(handler != null)
                handler();
        }
        #endregion

        #region Math

        /// <summary>
        /// Epsilon used for lessThanOrClose, greaterThanOrClose, equalsOrClose, etc. Used for both doubles and floats.
        /// </summary>
        public const float EPSILON = 2.4e-6f;

        #region clamp for many types
        /// <summary>
        /// Clamps a value to the given min and max (inclusive).
        /// </summary>
        public static int clamp(this int v, int min, int max)
        {
            return Math.Max(min, Math.Min(max, v));
        }

        /// <summary>
        /// Clamps a value to the given min and max (inclusive).
        /// </summary>
        public static long clamp(this long v, long min, long max)
        {
            return Math.Max(min, Math.Min(max, v));
        }

        /// <summary>
        /// Clamps a value to the given min and max (inclusive).
        /// </summary>
        public static short clamp(this short v, short min, short max)
        {
            return Math.Max(min, Math.Min(max, v));
        }

        /// <summary>
        /// Clamps a value to the given min and max (inclusive).
        /// </summary>
        public static sbyte clamp(this sbyte v, sbyte min, sbyte max)
        {
            return Math.Max(min, Math.Min(max, v));
        }
        
        /// <summary>
        /// Clamps a value to the given min and max (inclusive).
        /// </summary>
        public static ulong clamp(this ulong v, ulong min, ulong max)
        {
            return Math.Max(min, Math.Min(max, v));
        }

        /// <summary>
        /// Clamps a value to the given min and max (inclusive).
        /// </summary>
        public static uint clamp(this uint v, uint min, uint max)
        {
            return Math.Max(min, Math.Min(max, v));
        }

        /// <summary>
        /// Clamps a value to the given min and max (inclusive).
        /// </summary>
        public static ushort clamp(this ushort v, ushort min, ushort max)
        {
            return Math.Max(min, Math.Min(max, v));
        }

        /// <summary>
        /// Clamps a value to the given min and max (inclusive).
        /// </summary>
        public static byte clamp(this byte v, byte min, byte max)
        {
            return Math.Max(min, Math.Min(max, v));
        }

        /// <summary>
        /// Clamps a value to the given min and max (inclusive).
        /// </summary>
        public static float clamp(this float v, float min, float max)
        {
            return Math.Max(min, Math.Min(max, v));
        }

        /// <summary>
        /// Clamps a value to the given min and max (inclusive).
        /// </summary>
        public static double clamp(this double v, double min, double max)
        {
            return Math.Max(min, Math.Min(max, v));
        }
        #endregion

        #region isWithin for many types
        /// <summary>
        /// Check if value is within range (inclusive).
        /// </summary>
        public static bool isWithin(this int v, int min, int max)
        {
            return v >= min && v <= max;
        }

        /// <summary>
        /// Check if value is within range (inclusive).
        /// </summary>
        public static bool isWithin(this long v, long min, long max)
        {
            return v >= min && v <= max;
        }

        /// <summary>
        /// Check if value is within range (inclusive).
        /// </summary>
        public static bool isWithin(this short v, short min, short max)
        {
            return v >= min && v <= max;
        }

        /// <summary>
        /// Check if value is within range (inclusive).
        /// </summary>
        public static bool isWithin(this sbyte v, sbyte min, sbyte max)
        {
            return v >= min && v <= max;
        }

        /// <summary>
        /// Check if value is within range (inclusive).
        /// </summary>
        public static bool isWithin(this ulong v, ulong min, ulong max)
        {
            return v >= min && v <= max;
        }

        /// <summary>
        /// Check if value is within range (inclusive).
        /// </summary>
        public static bool isWithin(this uint v, uint min, uint max)
        {
            return v >= min && v <= max;
        }

        /// <summary>
        /// Check if value is within range (inclusive).
        /// </summary>
        public static bool isWithin(this ushort v, ushort min, ushort max)
        {
            return v >= min && v <= max;
        }

        /// <summary>
        /// Check if value is within range (inclusive).
        /// </summary>
        public static bool isWithin(this byte v, byte min, byte max)
        {
            return v >= min && v <= max;
        }

        /// <summary>
        /// Check if value is within range (inclusive).
        /// </summary>
        public static bool isWithin(this float v, float min, float max)
        {
            return v >= min && v <= max;
        }

        /// <summary>
        /// Check if value is within range (inclusive).
        /// </summary>
        public static bool isWithin(this double v, double min, double max)
        {
            return v >= min && v <= max;
        }
        #endregion

        #region utility functions for double

        /// <summary>
        /// Returns true iff n is not NaN or infinity.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public static bool isValid(this double n)
        {
            return !Double.IsNaN(n) && !Double.IsInfinity(n);
        }

        /// <summary>
        /// Extension method version of IsNaN.
        /// </summary>
        public static bool isNaN(this double n)
        {
            return Double.IsNaN(n);
        }

        /// <summary>
        /// Extension method version of IsInfinity.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public static bool isInfinity(this double n)
        {
            return Double.IsInfinity(n);
        }

        /// <summary>
        /// Returns 0 if the double is NaN or infinity.
        /// </summary>
        public static double zeroIfInvalid(this double n)
        {
            return n.isValid() ? n : 0;
        }

        /// <summary>
        /// Extension method verison of Sqrt (also returns correct type for floats).
        /// </summary>
        public static double sqrt(this double v)
        {
            return Math.Sqrt(v);
        }

        /// <summary>
        /// Extension method version of Abs.
        /// </summary>
        public static double abs(this double v)
        {
            return v < 0 ? -v : v;
        }

        /// <summary>
        /// Converts degrees to radians.
        /// </summary>
        public static double toRadians(this double degrees)
        {
            return (degrees * Math.PI) / 180;
        }

        /// <summary>
        /// Converts radians to degrees.
        /// </summary>
        public static double toDegrees(this double radians)
        {
            return radians * (180 / Math.PI);
        }

        /// <summary>
        /// Wraps angle to within [-PI, PI].
        /// </summary>
        public static double wrapAngle(double a)
        {
            return (a % (2 * Math.PI)) - Math.PI;
        }

        /// <summary>
        /// Checks if f1 and f2 are within diff of each other.
        /// </summary>
        public static bool equalsWithin(double f1, double f2, double diff)
        {
            return Math.Abs(f2 - f1) <= diff;
        }

        /// <summary>
        /// Checks iftwo numbers are equal within EPSILON
        /// </summary>
        public static bool equalsOrClose(this double f1, double f2)
        {
            return equalsWithin(f1, f2, EPSILON);
        }

        /// <summary>
        /// Checks if f1 is less than, equal to, or just barely greater than (ie basically equal to) f2. Use this instead of &lt;= when floating point accuracy is an issue.
        /// </summary>
        public static bool lessThanOrClose(this double f1, double f2)
        {
            return f2 - f1 > EPSILON;
        }

        /// <summary>
        /// Checks if f1 is greater than, equal to, or just barely less than (ie basically equal to) f2. Use this instead of &gt;= when floating point accuracy is an issue.
        /// </summary>
        public static bool greaterThanOrClose(this double f1, double f2)
        {
            return f1 - f2 > EPSILON;
        }
        
        /// <summary>
        /// Rounds a double to an int.
        /// </summary>
        public static int round(double value)
        {
            return (int) Math.Round(value);
        }

        /// <summary>
        /// Linearly interpolates between x and y. If r is 0, returns x. If r is 1, returns y. Works for values outside that range by extrapolating linearly.
        /// </summary>
        public static double lerp(double x, double y, double r)
        {
            return x + r * (y - x);
        }

        /// <summary>
        /// Inverse of lerp. If v = min, returns 0. If v = max, returns 1. Works for values outside that range by interpolating linearly
        /// (ex linstep(5, 2, 4) would return 1.5). Use linstepBounded if you want it clamped.
        /// </summary>
        public static double linstep(double v, double min, double max)
        {
            return (v - min) / (max - min);
        }

        /// <summary>
        /// Inverse of lerp, clamped to [0, 1]. If v &lt;= min, returns 0. If v &gt;= max, returns 1. Otherwise returns a linear ratio of how far v is between min and max.
        /// </summary>
        public static double linstepBounded(double v, double min, double max)
        {
            return v < min ? 0 : v > max ? 1 : linstep(v, min, max);
        }

        /// <summary>
        /// Returns the inverse square root of x
        /// </summary>
        public static double invSqrt(double x)
        {
            return 1.0f / Math.Sqrt(x);
        }
        #endregion

        #region same as above for float
        /// <summary>
        /// Returns true iff n is not NaN or infinity.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public static bool isValid(this float n)
        {
            return !Single.IsNaN(n) && !Single.IsInfinity(n);
        }

        /// <summary>
        /// Extension method version of IsNaN.
        /// </summary>
        public static bool isNaN(this float n)
        {
            return Single.IsNaN(n);
        }

        /// <summary>
        /// Extension method version of IsInfinity.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public static bool isInfinity(this float n)
        {
            return Single.IsInfinity(n);
        }

        /// <summary>
        /// Returns 0 if the double is NaN or infinity.
        /// </summary>
        public static double zeroIfInvalid(this float n)
        {
            return n.isValid() ? n : 0;
        }

        /// <summary>
        /// Extension method verison of Sqrt (also returns correct type for floats).
        /// </summary>
        public static float sqrt(this float v)
        {
            return (float) Math.Sqrt(v);
        }

        /// <summary>
        /// Extension method version of Abs.
        /// </summary>
        public static float abs(this float v)
        {
            return v < 0 ? -v : v;
        }

        /// <summary>
        /// Converts degrees to radians.
        /// </summary>
        public static float toRadians(this float degrees)
        {
            return (degrees * (float) Math.PI) / 180;
        }

        /// <summary>
        /// Converts radians to degrees.
        /// </summary>
        public static float toDegrees(this float radians)
        {
            return radians * (180 / (float) Math.PI);
        }

        /// <summary>
        /// Wraps angle to within [-PI, PI].
        /// </summary>
        public static float wrapAngle(float a)
        {
            return (a % (2 * (float) Math.PI)) - (float) Math.PI;
        }

        /// <summary>
        /// Checks if f1 and f2 are within diff of each other.
        /// </summary>
        public static bool equalsWithin(float f1, float f2, float diff)
        {
            return Math.Abs(f2 - f1) <= diff;
        }

        /// <summary>
        /// Checks iftwo numbers are equal within EPSILON
        /// </summary>
        public static bool equalsOrClose(this float f1, float f2)
        {
            return equalsWithin(f1, f2, EPSILON);
        }

        /// <summary>
        /// Checks if f1 is less than, equal to, or just barely greater than (ie basically equal to) f2. Use this instead of &lt;= when floating point accuracy is an issue.
        /// </summary>
        public static bool lessThanOrClose(this float f1, float f2)
        {
            return f2 - f1 > EPSILON;
        }

        /// <summary>
        /// Checks if f1 is greater than, equal to, or just barely less than (ie basically equal to) f2. Use this instead of &gt;= when floating point accuracy is an issue.
        /// </summary>
        public static bool greaterThanOrClose(this float f1, float f2)
        {
            return f1 - f2 > EPSILON;
        }
        
        /// <summary>
        /// Rounds a float to an int.
        /// </summary>
        public static int round(float value)
        {
            return (int) Math.Round(value);
        }

        /// <summary>
        /// Linearly interpolates between x and y. If r is 0, returns x. If r is 1, returns y. Works for values outside that range by extrapolating linearly.
        /// </summary>
        public static float lerp(float x, float y, float r)
        {
            return x + r * (y - x);
        }

        /// <summary>
        /// Inverse of lerp. If v = min, returns 0. If v = max, returns 1. Works for values outside that range by interpolating linearly
        /// (ex linstep(5, 2, 4) would return 1.5). Use linstepBounded if you want it clamped.
        /// </summary>
        public static float linstep(float v, float min, float max)
        {
            return (v - min) / (max - min);
        }

        /// <summary>
        /// Inverse of lerp, clamped to [0, 1]. If v &lt;= min, returns 0. If v &gt;= max, returns 1. Otherwise returns a linear ratio of how far v is between min and max.
        /// </summary>
        public static float linstepBounded(float v, float min, float max)
        {
            return v < min ? 0 : v > max ? 1 : linstep(v, min, max);
        }

        /// <summary>
        /// Returns the inverse square root of x
        /// </summary>
        public static float invSqrt(float x)
        {
            return 1.0f / (float) Math.Sqrt(x);
        }
        #endregion

        #endregion

        #region Parsing Strings
        /// <summary>
        /// Typeseafe parse enum. Ignores case.
        /// </summary>
        public static T parseEnum<T>(string s)
        {
            return (T) Enum.Parse(typeof(T), s, true);
        }

        /// <summary>
        /// Parses int with invariant culture.
        /// </summary>
        public static int parseInt(string s)
        {
            return Int32.Parse(s, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Parses float with invariant culture.
        /// </summary>
        public static float parseFloat(string s)
        {
            return Single.Parse(s, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Parses double with invariant culture.
        /// </summary>
        public static double parseDouble(string s)
        {
            return Double.Parse(s, CultureInfo.InvariantCulture);
        }
        #endregion

        #region XML

        /// <summary>
        /// Loads an XML document with line info (for debugging/error reporting purposes) and returns the root element.
        /// </summary>
        public static XElement loadXml(string path)
        {
            return XDocument.Load(path, LoadOptions.SetLineInfo).Root;
        }

        /// <summary>
        /// Gets a string representing the line info of an XML element. It needs to have been loaded with SetLineInfo (ie using <see cref="loadXml" />.
        /// </summary>
        public static string getLineInfo(this XElement e)
        {
            return String.Format("[{0}:{1},{2}]", e.Name.LocalName, ((IXmlLineInfo) e).LineNumber, ((IXmlLineInfo) e).LinePosition);
        }

        /// <summary>
        /// Gets a string attribute or returns a default value if attribute not found.
        /// </summary>
        public static string attrOrDefault(this XElement e, XName name, string d = null)
        {
            XAttribute a = e.Attribute(name);
            return a == null ? d : a.Value;
        }

        /// <summary>
        /// Gets a string attribute. Throws a detailed exception if attribute not found.
        /// </summary>
        public static string attrOrThrow(this XElement e, XName name)
        {
            XAttribute a = e.Attribute(name);
            if(a == null)
                throw new InvalidOperationException(String.Format("Expected attribute {0} on element {1}", name, e.getLineInfo()));
            return a.Value;
        }

        /// <summary>
        /// Gets a child element. Throws a detailed exception if element is not found.
        /// </summary>
        public static XElement elemOrThrow(this XElement e, XName name)
        {
            XElement c = e.Element(name);
            if(c == null)
                throw new InvalidOperationException(String.Format("Expected element {0} as a child of element {1}", name.LocalName, e.getLineInfo()));
            return c;
        }

        /// <summary>
        /// Gets the inner text of a child element with the given name. Throws a detailed exception if the element is not found.
        /// Returns an empty string if the inner text of the element is empty, but the element is present.
        /// </summary>
        public static string stringElemOrThrow(this XElement e, XName name)
        {
            XElement v = e.Element(name);
            if(v == null || v.Value == null)
                throw new InvalidOperationException(String.Format("Expected element {0} as a child of element {1}", name.LocalName, e.getLineInfo()));
            return v.Value;
        }

        /// <summary>
        /// Gets the inner text of a child element with the given name. Returns d (which is null by default) if the element is not present.
        /// Returns an empty string if the inner text of the element is empty, but the element is present.
        /// </summary>
        public static string stringElemOrDefault(this XElement e, XName name, string d = null)
        {
            XElement v = e.Element(name);
            return v == null ? d : v.Value;
        }

        /// <summary>
        /// Gets attribute and converts to given type. Throws detailed exception if attribute is not found or if there is parsing exception.
        /// </summary>
        public static bool boolAttrOrThrow(this XElement e, XName name)
        {
            return e.valueAttrOrThrow(name, Boolean.Parse);
        }

        /// <summary>
        /// Gets attribute and converts to given type. Throws detailed exception if attribute is not found or if there is parsing exception.
        /// </summary>
        public static float floatAttrOrThrow(this XElement e, XName name)
        {
            return e.valueAttrOrThrow(name, parseFloat);
        }

        /// <summary>
        /// Gets attribute and converts to given type. Throws detailed exception if attribute is not found or if there is parsing exception.
        /// </summary>
        public static double doubleAttrOrThrow(this XElement e, XName name)
        {
            return e.valueAttrOrThrow(name, parseDouble);
        }

        /// <summary>
        /// Gets attribute and converts to given type. Throws detailed exception if attribute is not found or if there is parsing exception.
        /// </summary>
        public static int intAttrOrThrow(this XElement e, XName name)
        {
            return e.valueAttrOrThrow(name, parseInt);
        }

        /// <summary>
        /// Gets attribute and converts to given type. Throws detailed exception if attribute is not found or if there is parsing exception. T is the enum type.
        /// </summary>
        public static T enumAttrOrThrow<T>(this XElement e, XName name)
        {
            return e.valueAttrOrThrow(name, parseEnum<T>);
        }

        /// <summary>
        /// Gets attribute and converts to given type. Throws detailed exception if attribute is not found or if there is parsing exception.
        /// </summary>
        public static T valueAttrOrThrow<T>(this XElement e, XName name, Func<string, T> parse)
        {
            string value = e.attrOrThrow(name); // don't wrap this exception
            try
            {
                return parse(value);
            }
            catch(Exception ex)
            {
                throw wrapParseException(e, name, ex);
            }
        }

        /// <summary>
        /// Gets attribute and converts to given type. If attribute is missing, returns the default value. If attribute format is wrong, can optionally throw an exception (if throwFormatException is true),
        /// otherwise returns the default value.
        /// </summary>
        public static bool boolAttrOrDefault(this XElement e, XName name, bool d = false, bool throwFormatException = false)
        {
            return e.valueAttrOrDefault(name, Boolean.Parse, d, throwFormatException);
        }

        /// <summary>
        /// Gets attribute and converts to given type. If attribute is missing, returns the default value. If attribute format is wrong, can optionally throw an exception (if throwFormatException is true),
        /// otherwise returns the default value.
        /// </summary>
        public static float floatAttrOrDefault(this XElement e, XName name, float d = 0, bool throwFormatException = false)
        {
            return e.valueAttrOrDefault(name, parseFloat, d, throwFormatException);
        }

        /// <summary>
        /// Gets attribute and converts to given type. If attribute is missing, returns the default value. If attribute format is wrong, can optionally throw an exception (if throwFormatException is true),
        /// otherwise returns the default value.
        /// </summary>
        public static double doubleAttrOrDefault(this XElement e, XName name, double d = 0, bool throwFormatException = false)
        {
            return e.valueAttrOrDefault(name, parseDouble, d, throwFormatException);
        }

        /// <summary>
        /// Gets attribute and converts to given type. If attribute is missing, returns the default value. If attribute format is wrong, can optionally throw an exception (if throwFormatException is true),
        /// otherwise returns the default value.
        /// </summary>
        public static int intAttrOrDefault(this XElement e, XName name, int d = 0, bool throwFormatException = false)
        {
            return e.valueAttrOrDefault(name, parseInt, d, throwFormatException);
        }

        /// <summary>
        /// Gets attribute and converts to given type. If attribute is missing, returns the default value. If attribute format is wrong, can optionally throw an exception (if throwFormatException is true),
        /// otherwise returns the default value. T is the enum type.
        /// </summary>
        public static T enumAttrOrDefault<T>(this XElement e, XName name, T d = default(T), bool throwFormatException = false)
        {
            return e.valueAttrOrDefault(name, parseEnum<T>, d, throwFormatException);
        }

        /// <summary>
        /// Gets attribute and converts to given type. If attribute is missing, returns the default value. If attribute format is wrong, can optionally throw an exception (if throwFormatException is true),
        /// otherwise returns the default value.
        /// </summary>
        public static T valueAttrOrDefault<T>(this XElement e, XName name, Func<string, T> parse, T d = default(T), bool throwFormatException = false)
        {
            string s = e.attrOrDefault(name);
            if(s == null)
                return d;
            try
            {
                return parse(s);
            }
            catch(Exception ex)
            {
                if(throwFormatException)
                    throw wrapParseException(e, name, ex); // wrap the exception with info about the location in the XML file
                else
                    return d;
            }
        }

        private static Exception wrapParseException(XElement e, XName name, Exception inner)
        {
            // NOTE: the reason that the inner exception is not explicitly passed is because Unity likes to unwrap exceptions and only show the innermost in
            // the log. In this case, the line where the XML is malformed is *usually* what we want to show in the log, so we completely drop the outside
            // exception object. The stack trace of the parse exception is in Exception.ToString() so not much info will be lost.
            return new FormatException(String.Format("Error parsing attribute {0} on element {1}:{2}   {3}", name.LocalName, e.getLineInfo(), Environment.NewLine, inner));
        }
        #endregion

        #region Time Zones
        /// <summary>
        /// Gets the time zone name for the given time (ie "Pacific Standard Time" or "Pacific Daylight Time")
        /// </summary>
        public static string toStringForTime(this TimeZone tz, DateTime time)
        {
            return tz.IsDaylightSavingTime(time) ? tz.DaylightName : tz.StandardName;
        }

        /// <summary>
        /// Formats time with time zone for given time zone.
        /// </summary>
        public static string toStringWithTimeZone(this DateTime time, TimeZone tz, string format = null)
        {
            return (format == null ? time.ToString(CultureInfo.InvariantCulture) : time.ToString(format)) + " " + tz.toStringForTime(time);
        }
        
        /// <summary>
        /// Formats time with time zone for current time zone.
        /// </summary>
        public static string toStringWithTimeZone(this DateTime time, string format = null)
        {
            return time.toStringWithTimeZone(TimeZone.CurrentTimeZone, format);
        }
        #endregion

        #region Misc

        /// <summary>
        /// Typesafe wrapper around <see cref="IServiceProvider.GetService"/>.
        /// </summary>
        public static T getService<T>(this IServiceProvider serviceProvider)
        {
            return (T) serviceProvider.GetService(typeof(T));
        }

        /// <summary>
        /// Reads all bytes of a stream into an array. The stream's <see cref="Stream.Length"/> property must be accurate, so it works
        /// for things like file streams but not for network streams.
        /// </summary>
        public static byte[] readAllBytes(this Stream stream)
        {
            int length = (int) stream.Length;
            byte[] data = new byte[length];
            stream.Read(data, 0, length);
            return data;
        }

        /// <summary>
        /// Tries to cast obj to the specific class and returns true if it worked.
        /// </summary>
        public static bool tryCast<TIn, TOut>(TIn obj, out TOut result) where TOut : class
        {
            result = obj as TOut;
            return null != result;
        }

        /// <summary>
        /// Returns true if b is non-null and true.
        /// </summary>
        public static bool isTrue(this bool? b)
        {
            return b.HasValue && b.Value;
        }

        /// <summary>
        /// Returns true if b is null or true.
        /// </summary>
        public static bool isTrueOrNull(this bool? b)
        {
            return !b.HasValue || b.Value;
        }

        /// <summary>
        /// If d is non-null, disposes it and sets it to null.
        /// </summary>
        public static void disposeAndNullify<T>(ref T d) where T : class, IDisposable
        {
            if(d != null)
            {
                d.Dispose();
                d = null;
            }
        }

        /// <summary>
        /// Disposes each member of the array, then sets the array to null.
        /// </summary>
        public static void disposeAndNullify<T>(ref T[] arr) where T : class, IDisposable
        {
            if(arr != null)
            {
                foreach(T d in arr)
                    if(d != null)
                        d.Dispose();
                arr = null;
            }
        }
        #endregion

        #region Random numbers
        [ThreadStatic] private static Random _random;
        
        /// <summary>
        /// Gets a random number using a thread-local Random instance.
        /// </summary>
        public static int rand(int max)
        {
            if(_random == null)
                _random = new Random();
            return _random.Next(max);
        }

        /// <summary>
        /// Gets a random number using a thread-local Random instance.
        /// </summary>
        public static int rand(int min, int max)
        {
            if(_random == null)
                _random = new Random();
            return _random.Next(min, max);
        }

        /// <summary>
        /// Gets a random number using a thread-local Random instance.
        /// </summary>
        public static float frand(float max = 1)
        {
            if(_random == null)
                _random = new Random();
            return _random.frand(0, max);
        }

        /// <summary>
        /// Gets a random number using a thread-local Random instance.
        /// </summary>
        public static float frand(float min, float max)
        {
            if(_random == null)
                _random = new Random();
            return _random.frand(min, max);
        }

        /// <summary>
        /// Gets a random float using the provided random instance.
        /// </summary>
        public static float frand(this Random r, float max = 1)
        {
            return r.frand(0.0f, max);
        }

        /// <summary>
        /// Gets a random float using the provided random instance.
        /// </summary>
        public static float frand(this Random r, float min, float max)
        {
            return (((float) r.NextDouble()) * (max - min)) + min;
        }
        #endregion

        #region Find Unique Identifier
        /// <summary>
        /// Finds a name that's not in the dictionary. <see cref="findUniqueName(string, Func{string, bool})"/>
        /// </summary>
        /// <param name="name">The candidate name.</param>
        /// <param name="names">The dictionary.</param>
        /// <returns>A name that is not a key in the dictionary based on the name parameter.</returns>
        public static string findUniqueName<TValue>(string name, Dictionary<string, TValue> names)
        {
            return findUniqueName(name, names.ContainsKey);
        }
        
        /// <summary>
        /// Finds a name in the enumerable. May enumerate the enumerable more than once.
        /// </summary>
        /// <param name="name">The candidate name.</param>
        /// <param name="names">The list of existing names.</param>
        /// <returns>A name that is not a key in the list based on the name parameter.</returns>
        public static string findUniqueName(string name, ICollection<string> names)
        {
            return findUniqueName(name, names.Contains);
        }
		
        /// <summary>
        /// Finds a unique name base don the candidate name. If the set does not contain the candidate name, returns that name
        /// unmodified. If it does, tries to find a name that's unique by appending an underscor and number. For example, given the
        /// candidate name "helen" and a set that contains {steve, helen, helen_2}, it will return "helen_3".
        /// </summary>
        /// <param name="name">The candidate name.</param>
        /// <param name="containsName">A function to determine if the name is in the set.</param>
        /// <returns>A name that is not a key in the list based on the name parameter.</returns>
        public static string findUniqueName(string name, Func<string, bool> containsName)
        {
            if(name == null) throw new ArgumentNullException("name");
            if(containsName == null) throw new ArgumentNullException("containsName");
            string baseName = name = name.Trim();
            int idx = name.LastIndexOf('_');
            int suffix = 2;
            if(idx > 0)
            {
                string remainder = name.Substring(idx + 1);
                int oldSuffix;
                if(Int32.TryParse(remainder, out oldSuffix))
                {
                    baseName = name.Substring(0, idx);
                    suffix = oldSuffix + 1;
                }
            }
            while(containsName(name))
                name = baseName + "_" + (suffix++);
            return name;
        }
        #endregion

        #region Enum utilities

        /// <summary>
        /// Wrapper around <see cref="Enum.GetName"/> that infers type.
        /// </summary>
        public static string getEnumName<T>(T obj)
        {
            return Enum.GetName(typeof(T), obj);
        }

        /// <summary>
        /// Uses reflection (SLOW, so cache result!) to get the values of an enum type as an array of the proper type.
        /// </summary>
        public static T[] getEnumValues<T>()
        {
            return (T[]) Enum.GetValues(typeof(T));
        }

        /// <summary>
        /// Uses reflection (SLOW, so cache result!) to get the values of an enum type.
        /// </summary>
        public static object[] getEnumValues(Type type)
        {
            return Enum.GetValues(type).dupWithType<object>();
        }

        /// <summary>
        /// Uses reflection (SLOW, so cache result!) to get the names and values of an enum type with the correct array type.
        /// </summary>
        public static void getEnumInfo<T>(out string[] names, out T[] values)
        {
            Type type = typeof(T);
            names = Enum.GetNames(type);
            values = getEnumValues<T>();
        }

        /// <summary>
        /// Uses reflection (SLOW, so cache result!) to get the names and values of an enum type with a generic object array.
        /// </summary>
        public static void getEnumInfo(Type type, out string[] names, out object[] values)
        {
            names = Enum.GetNames(type);
            values = getEnumValues(type);
        }

        #endregion
    }
}

