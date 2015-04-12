using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

// ReSharper disable InconsistentNaming
namespace burningmime.util.wpf
{
    public struct Binding<T>
    {
        private readonly Binding _obj;
        public Binding(Binding obj) { _obj = obj; }
        public static implicit operator Binding(Binding<T> b) { return b._obj; }

        public bool ShouldSerializeFallbackValue() { return _obj.ShouldSerializeFallbackValue(); }
        public bool ShouldSerializeTargetNullValue() { return _obj.ShouldSerializeTargetNullValue(); }
        public object ProvideValue(IServiceProvider serviceProvider) { return _obj.ProvideValue(serviceProvider); }
        public object FallbackValue { get { return _obj.FallbackValue; } set { _obj.FallbackValue = value; } }
        public string StringFormat { get { return _obj.StringFormat; } set { _obj.StringFormat = value; } }
        public object TargetNullValue { get { return _obj.TargetNullValue; } set { _obj.TargetNullValue = value; } }
        public string BindingGroupName { get { return _obj.BindingGroupName; } set { _obj.BindingGroupName = value; } }
        public bool ShouldSerializeValidationRules() { return _obj.ShouldSerializeValidationRules(); }
        public bool ShouldSerializePath() { return _obj.ShouldSerializePath(); }
        public bool ShouldSerializeSource() { return _obj.ShouldSerializeSource(); }
        public Collection<ValidationRule> ValidationRules { get { return _obj.ValidationRules; } }
        public bool ValidatesOnExceptions { get { return _obj.ValidatesOnExceptions; } set { _obj.ValidatesOnExceptions = value; } }
        public bool ValidatesOnDataErrors { get { return _obj.ValidatesOnDataErrors; } set { _obj.ValidatesOnDataErrors = value; } }
        public PropertyPath Path { get { return _obj.Path; } set { _obj.Path = value; } }
        public string XPath { get { return _obj.XPath; } set { _obj.XPath = value; } }
        public BindingMode Mode { get { return _obj.Mode; } set { _obj.Mode = value; } }
        public UpdateSourceTrigger UpdateSourceTrigger { get { return _obj.UpdateSourceTrigger; } set { _obj.UpdateSourceTrigger = value; } }
        public bool NotifyOnSourceUpdated { get { return _obj.NotifyOnSourceUpdated; } set { _obj.NotifyOnSourceUpdated = value; } }
        public bool NotifyOnTargetUpdated { get { return _obj.NotifyOnTargetUpdated; } set { _obj.NotifyOnTargetUpdated = value; } }
        public bool NotifyOnValidationError { get { return _obj.NotifyOnValidationError; } set { _obj.NotifyOnValidationError = value; } }
        public IValueConverter Converter { get { return _obj.Converter; } set { _obj.Converter = value; } }
        public object ConverterParameter { get { return _obj.ConverterParameter; } set { _obj.ConverterParameter = value; } }
        public CultureInfo ConverterCulture { get { return _obj.ConverterCulture; } set { _obj.ConverterCulture = value; } }
        public object Source { get { return _obj.Source; } set { _obj.Source = value; } }
        public RelativeSource RelativeSource { get { return _obj.RelativeSource; } set { _obj.RelativeSource = value; } }
        public string ElementName { get { return _obj.ElementName; } set { _obj.ElementName = value; } }
        public bool IsAsync { get { return _obj.IsAsync; } set { _obj.IsAsync = value; } }
        public object AsyncState { get { return _obj.AsyncState; } set { _obj.AsyncState = value; } }
        public bool BindsDirectlyToSource { get { return _obj.BindsDirectlyToSource; } set { _obj.BindsDirectlyToSource = value; } }
        public UpdateSourceExceptionFilterCallback UpdateSourceExceptionFilter { get { return _obj.UpdateSourceExceptionFilter; } set { _obj.UpdateSourceExceptionFilter = value; } }
    }

    public struct MultiBinding<T>
    {
        private readonly MultiBinding _obj;
        public MultiBinding(MultiBinding obj) { _obj = obj; }
        public static implicit operator MultiBinding(MultiBinding<T> b) { return b._obj; }

        public bool ShouldSerializeFallbackValue() { return _obj.ShouldSerializeFallbackValue(); }
        public bool ShouldSerializeTargetNullValue() { return _obj.ShouldSerializeTargetNullValue(); }
        public object ProvideValue(IServiceProvider serviceProvider) { return _obj.ProvideValue(serviceProvider); }
        public object FallbackValue { get { return _obj.FallbackValue; } set { _obj.FallbackValue = value; } }
        public string StringFormat { get { return _obj.StringFormat; } set { _obj.StringFormat = value; } }
        public object TargetNullValue { get { return _obj.TargetNullValue; } set { _obj.TargetNullValue = value; } }
        public string BindingGroupName { get { return _obj.BindingGroupName; } set { _obj.BindingGroupName = value; } }
        public void AddChild(object value) { ((IAddChild)_obj).AddChild(value); }
        public void AddText(string text) { ((IAddChild)_obj).AddText(text); }
        public bool ShouldSerializeBindings() { return _obj.ShouldSerializeBindings(); }
        public bool ShouldSerializeValidationRules() { return _obj.ShouldSerializeValidationRules(); }
        public Collection<BindingBase> Bindings { get { return _obj.Bindings; } }
        public BindingMode Mode { get { return _obj.Mode; } set { _obj.Mode = value; } }
        public UpdateSourceTrigger UpdateSourceTrigger { get { return _obj.UpdateSourceTrigger; } set { _obj.UpdateSourceTrigger = value; } }
        public bool NotifyOnSourceUpdated { get { return _obj.NotifyOnSourceUpdated; } set { _obj.NotifyOnSourceUpdated = value; } }
        public bool NotifyOnTargetUpdated { get { return _obj.NotifyOnTargetUpdated; } set { _obj.NotifyOnTargetUpdated = value; } }
        public bool NotifyOnValidationError { get { return _obj.NotifyOnValidationError; } set { _obj.NotifyOnValidationError = value; } }
        public IMultiValueConverter Converter { get { return _obj.Converter; } set { _obj.Converter = value; } }
        public object ConverterParameter { get { return _obj.ConverterParameter; } set { _obj.ConverterParameter = value; } }
        public CultureInfo ConverterCulture { get { return _obj.ConverterCulture; } set { _obj.ConverterCulture = value; } }
        public Collection<ValidationRule> ValidationRules { get { return _obj.ValidationRules; } }
        public UpdateSourceExceptionFilterCallback UpdateSourceExceptionFilter { get { return _obj.UpdateSourceExceptionFilter; } set { _obj.UpdateSourceExceptionFilter = value; } }
        public bool ValidatesOnExceptions { get { return _obj.ValidatesOnExceptions; } set { _obj.ValidatesOnExceptions = value; } }
        public bool ValidatesOnDataErrors { get { return _obj.ValidatesOnDataErrors; } set { _obj.ValidatesOnDataErrors = value; } }
    }

    public struct DependencyProperty<T>
    {
        private readonly DependencyProperty _dp;
        public DependencyProperty(DependencyProperty dp) { _dp = dp; }
        public static implicit operator DependencyProperty(DependencyProperty<T> typedDependencyProperty) { return typedDependencyProperty._dp; }
        public T GetValue(DependencyObject dependencyObject) { return (T)dependencyObject.GetValue(_dp); }
        public void SetValue(DependencyObject dependencyObject, T value) { dependencyObject.SetValue(_dp, value); }

        public void OverrideMetadata(Type forType, PropertyMetadata typeMetadata) { _dp.OverrideMetadata(forType, typeMetadata); }
        public void OverrideMetadata(Type forType, PropertyMetadata typeMetadata, DependencyPropertyKey key) { _dp.OverrideMetadata(forType, typeMetadata, key); }
        public PropertyMetadata GetMetadata(Type forType) { return _dp.GetMetadata(forType); }
        public PropertyMetadata GetMetadata(DependencyObject dependencyObject) { return _dp.GetMetadata(dependencyObject); }
        public PropertyMetadata GetMetadata(DependencyObjectType dependencyObjectType) { return _dp.GetMetadata(dependencyObjectType); }
        public DependencyProperty AddOwner(Type ownerType) { return _dp.AddOwner(ownerType); }
        public DependencyProperty AddOwner(Type ownerType, PropertyMetadata typeMetadata) { return _dp.AddOwner(ownerType, typeMetadata); }
        public bool IsValidType(object value) { return _dp.IsValidType(value); }
        public bool IsValidValue(object value) { return _dp.IsValidValue(value); }
        public string Name { get { return _dp.Name; } }
        public Type PropertyType { get { return _dp.PropertyType; } }
        public Type OwnerType { get { return _dp.OwnerType; } }
        public PropertyMetadata DefaultMetadata { get { return _dp.DefaultMetadata; } }
        public ValidateValueCallback ValidateValueCallback { get { return _dp.ValidateValueCallback; } }
        public int GlobalIndex { get { return _dp.GlobalIndex; } }
        public bool ReadOnly { get { return _dp.ReadOnly; } }
    }

    public struct DependencyPropertyChangedEventArgs<T>
    {
        private readonly DependencyPropertyChangedEventArgs _args;
        public DependencyPropertyChangedEventArgs(DependencyPropertyChangedEventArgs args) { _args = args; }
        public DependencyProperty<T> Property { get { return _args.Property.of<T>(); } }
        public T OldValue { get { return (T) _args.OldValue; } }
        public T NewValue { get { return (T) _args.NewValue; } }
    }

    public delegate void PropertyChangedCallback<T>(DependencyObject d, DependencyPropertyChangedEventArgs<T> e);
    public delegate void PropertyChangedCallback<in TObject, TProperty>(TObject d, DependencyPropertyChangedEventArgs<TProperty> e);
    public static class WrapperExtensions
    {
        public static DependencyProperty<T> of<T>(this DependencyProperty dp) { return new DependencyProperty<T>(dp); }
        public static Binding<T> of<T>(this Binding binding) { return new Binding<T>(binding); }
        public static MultiBinding<T> of<T>(this MultiBinding binding) { return new MultiBinding<T>(binding); }
        public static DependencyPropertyChangedEventArgs<T> of<T>(this DependencyPropertyChangedEventArgs args) { return new DependencyPropertyChangedEventArgs<T>(args); }
    }
}