using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;

namespace burningmime.util.wpf
{
    // ReSharper disable InconsistentNaming
    // ReSharper disable MemberCanBePrivate.Local
    // ReSharper disable UnusedMember.Local
    public static class DependencyPropertyChangedListeners
    {
        public static void addListener<TObject, TProperty>(TObject obj, DependencyProperty<TProperty> property,  DependencyObject refHolder, PropertyChangedCallback<TObject, TProperty> callback) where TObject : DependencyObject
        {
            if(obj == null) throw new ArgumentNullException("obj");
            if(refHolder == null) throw new ArgumentNullException("refHolder");
            if(callback == null) throw new ArgumentNullException("callback");
            Binding binding = new Binding { Source = obj, Path = new PropertyPath(property), Mode = BindingMode.OneWay };
            DependencyPropertyChangedListener listener = new DependencyPropertyChangedListener(obj, property, (d, e) => callback((TObject) d, e.of<TProperty>()));
            BindingOperations.SetBinding(listener, DependencyPropertyChangedListener.ValueProperty, binding);
            GetAttachedChangeListeners(refHolder).Add(listener);
        }
        
        private static readonly DependencyPropertyKey AttachedChangeListenersPropertyKey = DependencyProperty.RegisterAttachedReadOnly("%AttachedChangeListeners%", typeof(List<DependencyPropertyChangedListener>), typeof(DependencyPropertyChangedListeners), new PropertyMetadata(null));
        private static readonly DependencyProperty AttachedChangeListenersProperty = AttachedChangeListenersPropertyKey.DependencyProperty;
        private static void SetAttachedChangeListeners(DependencyObject obj, List<DependencyPropertyChangedListener> value) { obj.SetValue(AttachedChangeListenersPropertyKey, value);}
        private static List<DependencyPropertyChangedListener> GetAttachedChangeListeners(DependencyObject obj)
        {
            List<DependencyPropertyChangedListener> value = obj.GetValue(AttachedChangeListenersProperty) as List<DependencyPropertyChangedListener>;
            if(value == null)
            {
                value = new List<DependencyPropertyChangedListener>();
                SetAttachedChangeListeners(obj, value);
            }
            return value;
        }
        
        private sealed class DependencyPropertyChangedListener : DependencyObject
        {
            public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(object), typeof(DependencyPropertyChangedListener), new PropertyMetadata(null, notifyValueChanged));
            public object Value { get { return GetValue(ValueProperty); } set { SetValue(ValueProperty, value); } }
            private readonly DependencyProperty _property;
            private readonly DependencyObject _source;
            private readonly PropertyChangedCallback _callback;
            
            public DependencyPropertyChangedListener(DependencyObject source, DependencyProperty property, PropertyChangedCallback callback)
            {
                _property = property;
                _source = source;
                _callback = callback;
            }

            private static void notifyValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
            {
                DependencyPropertyChangedListener listener = (DependencyPropertyChangedListener) sender;
                listener._callback(listener._source, new DependencyPropertyChangedEventArgs(listener._property, args.OldValue, args.NewValue));
            }
        }
    }
}