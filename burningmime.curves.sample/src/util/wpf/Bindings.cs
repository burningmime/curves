using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace burningmime.util.wpf
{
    /// <summary>
    /// Helpers for WPF data binding.
    /// </summary>
    public static class Bindings
    {
        public static void set<TSource, TTarget>(DependencyObject target, DependencyProperty<TTarget> property, DependencyObject source, DependencyProperty<TSource> sourceProperty, Func<TSource, TTarget> converter) { set(target, property, convert(get(source, sourceProperty), ValueConverter.get(converter))); }
        public static void set<TSource, TTarget>(DependencyObject target, DependencyProperty<TTarget> property, DependencyObject source, DependencyProperty<TSource> sourceProperty, IValueConverter<TSource, TTarget> converter) { set(target, property, convert(get(source, sourceProperty), converter)); }
        public static void set<TSource, TTarget>(DependencyObject target, DependencyProperty<TTarget> property, DependencyObject source, DependencyProperty<TSource> sourceProperty) where TSource : TTarget { set(target, property, get(source, sourceProperty)); }
        public static void set<TSource, TTarget>(DependencyObject target, DependencyProperty<TTarget> property, object source, string sourceProperty, Func<TSource, TTarget> converter) { set(target, property, convert(get<TSource>(source, sourceProperty), ValueConverter.get(converter))); }
        public static void set<TSource, TTarget>(DependencyObject target, DependencyProperty<TTarget> property, object source, string sourceProperty, IValueConverter<TSource, TTarget> converter) { set(target, property, convert(get<TSource>(source, sourceProperty), converter)); }
        public static void set<TTarget>(DependencyObject target, DependencyProperty<TTarget> property, object source, string sourceProperty) { set(target, property, get<TTarget>(source, sourceProperty)); }
        public static void set<TSource, TTarget>(DependencyObject target, DependencyProperty<TTarget> property, Binding<TSource> binding) where TSource : TTarget { BindingOperations.SetBinding(target, property, binding); }
        public static void set<TSource, TTarget>(DependencyObject target, DependencyProperty<TTarget> property, MultiBinding<TSource> binding) where TSource : TTarget { BindingOperations.SetBinding(target, property, binding); }

        public static Binding<T> constant<T>(T value) { return (new Binding { Source = value, Mode = BindingMode.OneTime, BindsDirectlyToSource = true }).of<T>(); }

        public static Binding<T> get<T>(DependencyObject source, DependencyProperty<T> dp) { return (new Binding { Source = source, Path = new PropertyPath((DependencyProperty)dp), Mode = BindingMode.OneWay }).of<T>(); }
        public static Binding<T> get<T>(object source, string path) { return (new Binding(path) { Source = source, Mode = source is INotifyPropertyChanged ? BindingMode.OneWay : BindingMode.OneTime }).of<T>(); }
        public static Binding<T> get<T>(string path) { return (new Binding(path) { Mode = BindingMode.OneWay }).of<T>(); }

        public static MultiBinding<TOut> multi<T1, T2, TOut>(Binding<T1> b1, Binding<T2> b2, IValueConverter<T1, T2, TOut> converter) { return getMultiBindingUntyped(converter, b1, b2); }
        public static MultiBinding<TOut> multi<T1, T2, T3, TOut>(Binding<T1> b1, Binding<T2> b2, Binding<T3> b3, IValueConverter<T1, T2, T3, TOut> converter) { return getMultiBindingUntyped(converter, b1, b2, b3); }
        public static MultiBinding<TOut> multi<T1, T2, T3, T4, TOut>(Binding<T1> b1, Binding<T2> b2, Binding<T3> b3, Binding<T4> b4, IValueConverter<T1, T2, T3, T4, TOut> converter) { return getMultiBindingUntyped(converter, b1, b2, b3, b4); }
        public static MultiBinding<TOut> multi<T1, T2, T3, T4, T5, TOut>(Binding<T1> b1, Binding<T2> b2, Binding<T3> b3, Binding<T4> b4, Binding<T5> b5, IValueConverter<T1, T2, T3, T4, T5, TOut> converter) { return getMultiBindingUntyped(converter, b1, b2, b3, b4, b5); }
        public static MultiBinding<TOut> multi<T1, T2, T3, T4, T5, T6, TOut>(Binding<T1> b1, Binding<T2> b2, Binding<T3> b3, Binding<T4> b4, Binding<T5> b5, Binding<T6> b6, IValueConverter<T1, T2, T3, T4, T5, T6, TOut> converter) { return getMultiBindingUntyped(converter, b1, b2, b3, b4, b5, b6); }
        public static MultiBinding<TOut> multi<T1, T2, T3, T4, T5, T6, T7, TOut>(Binding<T1> b1, Binding<T2> b2, Binding<T3> b3, Binding<T4> b4, Binding<T5> b5, Binding<T6> b6, Binding<T7> b7, IValueConverter<T1, T2, T3, T4, T5, T6, T7, TOut> converter) { return getMultiBindingUntyped(converter, b1, b2, b3, b4, b5, b6, b7); }
        public static MultiBinding<TOut> multi<T1, T2, T3, T4, T5, T6, T7, T8, TOut>(Binding<T1> b1, Binding<T2> b2, Binding<T3> b3, Binding<T4> b4, Binding<T5> b5, Binding<T6> b6, Binding<T7> b7, Binding<T8> b8, IValueConverter<T1, T2, T3, T4, T5, T6, T7, T8, TOut> converter) { return getMultiBindingUntyped(converter, b1, b2, b3, b4, b5, b6, b7, b8); }

        public static MultiBinding<TOut> multi<T1, T2, TOut>(Binding<T1> b1, Binding<T2> b2, Func<T1, T2, TOut> converter) { return getMultiBindingUntyped(ValueConverter.get(converter), b1, b2); }
        public static MultiBinding<TOut> multi<T1, T2, T3, TOut>(Binding<T1> b1, Binding<T2> b2, Binding<T3> b3, Func<T1, T2, T3, TOut> converter) { return getMultiBindingUntyped(ValueConverter.get(converter), b1, b2, b3); }
        public static MultiBinding<TOut> multi<T1, T2, T3, T4, TOut>(Binding<T1> b1, Binding<T2> b2, Binding<T3> b3, Binding<T4> b4, Func<T1, T2, T3, T4, TOut> converter) { return getMultiBindingUntyped(ValueConverter.get(converter), b1, b2, b3, b4); }
        public static MultiBinding<TOut> multi<T1, T2, T3, T4, T5, TOut>(Binding<T1> b1, Binding<T2> b2, Binding<T3> b3, Binding<T4> b4, Binding<T5> b5, Func<T1, T2, T3, T4, T5, TOut> converter) { return getMultiBindingUntyped(ValueConverter.get(converter), b1, b2, b3, b4, b5); }
        public static MultiBinding<TOut> multi<T1, T2, T3, T4, T5, T6, TOut>(Binding<T1> b1, Binding<T2> b2, Binding<T3> b3, Binding<T4> b4, Binding<T5> b5, Binding<T6> b6, Func<T1, T2, T3, T4, T5, T6, TOut> converter) { return getMultiBindingUntyped(ValueConverter.get(converter), b1, b2, b3, b4, b5, b6); }
        public static MultiBinding<TOut> multi<T1, T2, T3, T4, T5, T6, T7, TOut>(Binding<T1> b1, Binding<T2> b2, Binding<T3> b3, Binding<T4> b4, Binding<T5> b5, Binding<T6> b6, Binding<T7> b7, Func<T1, T2, T3, T4, T5, T6, T7, TOut> converter) { return getMultiBindingUntyped(ValueConverter.get(converter), b1, b2, b3, b4, b5, b6, b7); }
        public static MultiBinding<TOut> multi<T1, T2, T3, T4, T5, T6, T7, T8, TOut>(Binding<T1> b1, Binding<T2> b2, Binding<T3> b3, Binding<T4> b4, Binding<T5> b5, Binding<T6> b6, Binding<T7> b7, Binding<T8> b8, Func<T1, T2, T3, T4, T5, T6, T7, T8, TOut> converter) { return getMultiBindingUntyped(ValueConverter.get(converter), b1, b2, b3, b4, b5, b6, b7, b8); }

        public static Binding<TOut> convert<TIn, TOut>(this Binding<TIn> binding, Func<TIn, TOut> converter) { return convert(binding, ValueConverter.get(converter)); }
        public static Binding<TOut> convert<TIn, TOut>(this Binding<TIn> binding, IValueConverter<TIn, TOut> converter)
        {
            if (binding.Converter != null) 
                throw new InvalidOperationException("Binding already has a converter");
            binding.Converter = converter;
            if(binding.Mode == BindingMode.OneWay && converter is ITwoWayConverter<TIn, TOut>)
                binding.Mode = BindingMode.TwoWay;
            return new Binding<TOut>(binding);
        }

        public static Binding<T> with<T>(this Binding<T> binding, BindingMode mode) { binding.Mode = mode; return binding; }
        public static Binding<T> with<T>(this Binding<T> binding, UpdateSourceTrigger ust) { binding.UpdateSourceTrigger = ust; return binding; }
        public static MultiBinding<T> with<T>(this MultiBinding<T> binding, BindingMode mode) { binding.Mode = mode; return binding; }
        public static MultiBinding<T> with<T>(this MultiBinding<T> binding, UpdateSourceTrigger ust) { binding.UpdateSourceTrigger = ust; return binding; }

        public static void clear(DependencyObject target, DependencyProperty dp) { BindingOperations.ClearBinding(target, dp); }
        public static void clearAll(DependencyObject target) { BindingOperations.ClearAllBindings(target); }

        public static MultiBinding<T> getMultiBindingUntyped<T>(IMultiValueConverter<T> converter, params Binding[] bindings)
        {
            MultiBinding result = new MultiBinding();
            foreach (Binding binding in bindings)
                result.Bindings.Add(binding);
            result.Converter = converter;
            result.Mode = BindingMode.OneWay;
            return result.of<T>();
        }

        // Disabling these for performance
        // ReSharper disable ParameterTypeCanBeEnumerable.Local
        // ReSharper disable LoopCanBeConvertedToQuery

        public static MultiBinding<bool> anyOf(params Binding<bool>[] bindings) { return createAnyOrAll(_anyConverter, bindings); }
        public static MultiBinding<bool> allOf(params Binding<bool>[] bindings) { return createAnyOrAll(_allConverter, bindings); }

        private static MultiBinding<bool> createAnyOrAll(IMultiValueConverter<bool> conv, Binding<bool>[] bindings)
        {
            MultiBinding<bool> binding = new MultiBinding().of<bool>();
            binding.Converter = conv;
            binding.Mode = BindingMode.OneWay;
            foreach(Binding<bool> b in bindings)
                binding.Bindings.Add(b);
            return binding;
        }

        private static readonly AnyConverter _anyConverter = new AnyConverter();
        private sealed class AnyConverter : IMultiValueConverter<bool>
        {
            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) { throw new NotSupportedException(); }
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                foreach(object obj in values)
                    if((bool) obj) 
                        return true;
                return false;
            }
        }
        
        private static readonly AllConverter _allConverter = new AllConverter();
        private sealed class AllConverter : IMultiValueConverter<bool>
        {
            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) { throw new NotSupportedException(); }
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                foreach(object obj in values)
                    if(!(bool) obj) 
                        return false;
                return true;
            }
        }
    }
}