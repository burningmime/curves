using System;
using System.Globalization;
using System.Windows.Data;

namespace burningmime.util.wpf
{
	public interface IValueConverter<out TOut> { }
    public interface IValueConverter<in TIn, out TOut> : IValueConverter<TOut>, IValueConverter { TOut convert(TIn p); }
	public interface IMultiValueConverter<out TOut> : IValueConverter<TOut>, IMultiValueConverter { }
    public interface IValueConverter<in T1, in T2, out TOut> : IMultiValueConverter<TOut> { TOut convert(T1 p1, T2 p2); }
    public interface IValueConverter<in T1, in T2, in T3, out TOut> : IMultiValueConverter<TOut> { TOut convert(T1 p1, T2 p2, T3 p3); }
    public interface IValueConverter<in T1, in T2, in T3, in T4, out TOut> : IMultiValueConverter<TOut> { TOut convert(T1 p1, T2 p2, T3 p3, T4 p4); }
    public interface IValueConverter<in T1, in T2, in T3, in T4, in T5, out TOut> : IMultiValueConverter<TOut> { TOut convert(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5); }
    public interface IValueConverter<in T1, in T2, in T3, in T4, in T5, in T6, out TOut> : IMultiValueConverter<TOut> { TOut convert(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6); }
    public interface IValueConverter<in T1, in T2, in T3, in T4, in T5, in T6, in T7, out TOut> : IMultiValueConverter<TOut> { TOut convert(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, T7 p7); }
    public interface IValueConverter<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, out TOut> : IMultiValueConverter<TOut> { TOut convert(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, T7 p7, T8 p8); }
    public interface ITwoWayConverter<TIn, TOut> : IValueConverter<TIn, TOut> { TIn convertBack(TOut p); }

    public static class ValueConverter
    {
        public static IValueConverter<TIn, TOut> get<TIn, TOut>(Func<TIn, TOut> f) { return new ValueConverterImpl<TIn, TOut>(f); }
        public static IValueConverter<T1, T2, TOut> get<T1, T2, TOut>(Func<T1, T2, TOut> f) { return new ValueConverterImpl<T1, T2, TOut>(f); }
        public static IValueConverter<T1, T2, T3, TOut> get<T1, T2, T3, TOut>(Func<T1, T2, T3, TOut> f) { return new ValueConverterImpl<T1, T2, T3, TOut>(f); }
        public static IValueConverter<T1, T2, T3, T4, TOut> get<T1, T2, T3, T4, TOut>(Func<T1, T2, T3, T4, TOut> f) { return new ValueConverterImpl<T1, T2, T3, T4, TOut>(f); }
        public static IValueConverter<T1, T2, T3, T4, T5, TOut> get<T1, T2, T3, T4, T5, TOut>(Func<T1, T2, T3, T4, T5, TOut> f) { return new ValueConverterImpl<T1, T2, T3, T4, T5, TOut>(f); }
        public static IValueConverter<T1, T2, T3, T4, T5, T6, TOut> get<T1, T2, T3, T4, T5, T6, TOut>(Func<T1, T2, T3, T4, T5, T6, TOut> f) { return new ValueConverterImpl<T1, T2, T3, T4, T5, T6, TOut>(f); }
        public static IValueConverter<T1, T2, T3, T4, T5, T6, T7, TOut> get<T1, T2, T3, T4, T5, T6, T7, TOut>(Func<T1, T2, T3, T4, T5, T6, T7, TOut> f) { return new ValueConverterImpl<T1, T2, T3, T4, T5, T6, T7, TOut>(f); }
        public static IValueConverter<T1, T2, T3, T4, T5, T6, T7, T8, TOut> get<T1, T2, T3, T4, T5, T6, T7, T8, TOut>(Func<T1, T2, T3, T4, T5, T6, T7, T8, TOut> f) { return new ValueConverterImpl<T1, T2, T3, T4, T5, T6, T7, T8, TOut>(f); }
        public static ITwoWayConverter<TIn, TOut> get<TIn, TOut>(Func<TIn, TOut> convert, Func<TOut, TIn> convertBack) { return new TwoWayConverterImpl<TIn, TOut>(convert, convertBack); }

        private sealed class ValueConverterImpl<TIn, TOut> : IValueConverter<TIn, TOut>
        {
            private readonly Func<TIn, TOut> _f;
            public ValueConverterImpl(Func<TIn, TOut> f) { _f = f; }
            public TOut convert(TIn p) { return _f(p); }
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture) { return _f((TIn) value); }
            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotSupportedException(); }
        }
        
        private sealed class ValueConverterImpl<T1, T2, TOut> : IValueConverter<T1, T2, TOut>
        {
            private readonly Func<T1, T2, TOut> _f;
            public ValueConverterImpl(Func<T1, T2, TOut> f) { _f = f; }
            public TOut convert(T1 p1, T2 p2) { return _f(p1, p2); }
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) { return _f((T1) values[0], (T2) values[1]); }
            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) { throw new NotSupportedException(); }
        }
        
        private sealed class ValueConverterImpl<T1, T2, T3, TOut> : IValueConverter<T1, T2, T3, TOut>
        {
            private readonly Func<T1, T2, T3, TOut> _f;
            public ValueConverterImpl(Func<T1, T2, T3, TOut> f) { _f = f; }
            public TOut convert(T1 p1, T2 p2, T3 p3) { return _f(p1, p2, p3); }
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) { return _f((T1) values[0], (T2) values[1], (T3) values[2]); }
            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) { throw new NotSupportedException(); }
        }
        
        private sealed class ValueConverterImpl<T1, T2, T3, T4, TOut> : IValueConverter<T1, T2, T3, T4, TOut>
        {
            private readonly Func<T1, T2, T3, T4, TOut> _f;
            public ValueConverterImpl(Func<T1, T2, T3, T4, TOut> f) { _f = f; }
            public TOut convert(T1 p1, T2 p2, T3 p3, T4 p4) { return _f(p1, p2, p3, p4); }
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) { return _f((T1) values[0], (T2) values[1], (T3) values[2], (T4) values[3]); }
            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) { throw new NotSupportedException(); }
        }
        
        private sealed class ValueConverterImpl<T1, T2, T3, T4, T5, TOut> : IValueConverter<T1, T2, T3, T4, T5, TOut>
        {
            private readonly Func<T1, T2, T3, T4, T5, TOut> _f;
            public ValueConverterImpl(Func<T1, T2, T3, T4, T5, TOut> f) { _f = f; }
            public TOut convert(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5) { return _f(p1, p2, p3, p4, p5); }
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) { return _f((T1) values[0], (T2) values[1], (T3) values[2], (T4) values[3], (T5) values[4]); }
            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) { throw new NotSupportedException(); }
        }
        
        private sealed class ValueConverterImpl<T1, T2, T3, T4, T5, T6, TOut> : IValueConverter<T1, T2, T3, T4, T5, T6, TOut>
        {
            private readonly Func<T1, T2, T3, T4, T5, T6, TOut> _f;
            public ValueConverterImpl(Func<T1, T2, T3, T4, T5, T6, TOut> f) { _f = f; }
            public TOut convert(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6) { return _f(p1, p2, p3, p4, p5, p6); }
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) { return _f((T1) values[0], (T2) values[1], (T3) values[2], (T4) values[3], (T5) values[4], (T6) values[5]); }
            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) { throw new NotSupportedException(); }
        }
        
        private sealed class ValueConverterImpl<T1, T2, T3, T4, T5, T6, T7, TOut> : IValueConverter<T1, T2, T3, T4, T5, T6, T7, TOut>
        {
            private readonly Func<T1, T2, T3, T4, T5, T6, T7, TOut> _f;
            public ValueConverterImpl(Func<T1, T2, T3, T4, T5, T6, T7, TOut> f) { _f = f; }
            public TOut convert(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, T7 p7) { return _f(p1, p2, p3, p4, p5, p6, p7); }
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) { return _f((T1) values[0], (T2) values[1], (T3) values[2], (T4) values[3], (T5) values[4], (T6) values[5], (T7) values[6]); }
            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) { throw new NotSupportedException(); }
        }
        
        private sealed class ValueConverterImpl<T1, T2, T3, T4, T5, T6, T7, T8, TOut> : IValueConverter<T1, T2, T3, T4, T5, T6, T7, T8, TOut>
        {
            private readonly Func<T1, T2, T3, T4, T5, T6, T7, T8, TOut> _f;
            public ValueConverterImpl(Func<T1, T2, T3, T4, T5, T6, T7, T8, TOut> f) { _f = f; }
            public TOut convert(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, T7 p7, T8 p8) { return _f(p1, p2, p3, p4, p5, p6, p7, p8); }
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) { return _f((T1) values[0], (T2) values[1], (T3) values[2], (T4) values[3], (T5) values[4], (T6) values[5], (T7) values[6], (T8) values[7]); }
            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) { throw new NotSupportedException(); }
        }

        private sealed class TwoWayConverterImpl<TIn, TOut> : ITwoWayConverter<TIn, TOut>
        {
            private readonly Func<TIn, TOut> _convert; 
            private readonly Func<TOut, TIn> _convertBack; 
            public TwoWayConverterImpl(Func<TIn, TOut> convert, Func<TOut, TIn> convertBack) { _convert = convert; _convertBack = convertBack; }
            public TOut convert(TIn p) { return _convert(p); }
            public TIn convertBack(TOut p) { return _convertBack(p); }
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture) { return convert((TIn) value); }
            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { return convertBack((TOut) value); }
        }
    }
}
