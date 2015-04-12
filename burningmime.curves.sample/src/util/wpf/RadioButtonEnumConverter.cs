using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace burningmime.util.wpf
{
    /// <summary>
    /// A two-way converter to bind the RadioButton IsChecked property to an enum value (MUST BIND TWO-WAY!). This can be used in XAML with
    /// either <see cref="RadioButtonEnumBindingExtension"/> (the preferred way since it sets the two-way property) or
    /// <see cref="RadioButtonEnumConverterExtension"/>.
    /// </summary>
    public sealed class RadioButtonEnumConverter : IValueConverter
    {
        private readonly string _targetName;
        private object _targetValue;

        public RadioButtonEnumConverter(string enumValue)
        {
            if(string.IsNullOrWhiteSpace(enumValue))
                throw new ArgumentNullException("enumValue");
            _targetName = enumValue;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value == null || value == DependencyProperty.UnsetValue)
                return false;
            if(_targetValue == null)
                _targetValue = resolveTargetValue(_targetName, value.GetType());
            return value.Equals(_targetValue);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(_targetValue == null)
                _targetValue = resolveTargetValue(_targetName, targetType);
            return ((bool) value) ? _targetValue : DependencyProperty.UnsetValue;
        }

        private static object resolveTargetValue(string name, Type type)
        {
            EnumInfo info;
            if(!_enumInfoCache.TryGetValue(type, out info))
            {
                info = getEnumInfo(type);
                _enumInfoCache[type] = info;
            }
            for(int i = 0; i < info.names.Length; i++)
                if(string.Equals(info.names[i], name, StringComparison.InvariantCultureIgnoreCase))
                    return info.values[i];
            throw new InvalidOperationException("Could not find enum member " + name + " in enum " + type.Name);
        }

        // Cache this so we don't have to reflect every time
        private struct EnumInfo { public string[] names; public object[] values; }
        private static readonly Dictionary<Type, EnumInfo> _enumInfoCache = new Dictionary<Type, EnumInfo>(); 
        private static EnumInfo getEnumInfo(Type type)
        {
            if(!type.IsEnum)
                throw new InvalidOperationException("Type " + type.Name + " is not an enum type");
            string[] names = Enum.GetNames(type);
            Array unboxedValues = Enum.GetValues(type);
            Debug.Assert(names.Length == unboxedValues.Length, "names and values arrays should be the same length");
            object[] boxedValues = new object[unboxedValues.Length];
            Array.Copy(unboxedValues, 0, boxedValues, 0, unboxedValues.Length);
            return new EnumInfo { names = names, values = boxedValues };
        }
    }

    [MarkupExtensionReturnType(typeof(IValueConverter))]
    public sealed class RadioButtonEnumConverterExtension : MarkupExtension
    {
        public string enumValue { get; set; }
        public RadioButtonEnumConverterExtension(string enumValue) { this.enumValue = enumValue; }
        public override object ProvideValue(IServiceProvider serviceProvider) { return new RadioButtonEnumConverter(enumValue);  }
    }

    public sealed class RadioButtonEnumBindingExtension : Binding
    {
        public RadioButtonEnumBindingExtension(string path, string enumValue) : base(path)
        {
            Mode = BindingMode.TwoWay;
            Converter = new RadioButtonEnumConverter(enumValue);
        }
    }
}