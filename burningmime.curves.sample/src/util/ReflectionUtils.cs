using System;
using System.Linq.Expressions;
using System.Reflection;

namespace burningmime.util
{
    /// <summary>
    /// Collection of helper methods for reflection. The getters and setters for fields and properties come in two flavors. The "slow" versions are
    /// about 100x slower (~2000x slower on first use) than directt access. These should be used if you only want to set the field once or only a few
    /// times. The "create" versions create a delegate that's ~1.5x-2.5x slower than direct access (ie REALLY FAST), but they take some setup (so you
    /// only call them once and cache the delegate).
    /// 
    /// If you're going to be accessing the field or p repeatedly, create a function to do it. For example...
    /// 
    /// <code><![CDATA[
    ///     private static readonly Func<TreeViewItem, FrameworkElement> getHeaderElement = ReflectionUtils.createGetInstanceProperty<TreeViewItem, FrameworkElement>("HeaderElement");
    ///     // ... later on in some function ...
    ///     FrameworkElement header = getHeaderElement(tvi);
    /// ]]></code>
    /// 
    /// However, if you're only ever going to access the memeber once, creating the delegate will be much slower than just directly using reflection, for example:
    /// 
    /// <code><![CDATA[
    ///     public static readonly int defaultDpiX = (int) ReflectionUtils.slowGetStaticProperty(typeof(SystemParameters), "DpiX");
    /// ]]></code>
    /// </summary>
    public static class ReflectionUtils
    {
        #region Default Flags
        // these flags are non-ideal since by default they flatten hierarchy and force the runtime to search more than it needs to, but they make the API nicer
        public const BindingFlags BINDING_FLAGS_BASE = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy;
        public const BindingFlags BINDING_FLAGS_INSTANCE = BINDING_FLAGS_BASE | BindingFlags.Instance;
        public const BindingFlags BINDING_FLAGS_STATIC = BINDING_FLAGS_BASE | BindingFlags.Static;
        public const BindingFlags BINDING_FLAGS_ANY = BINDING_FLAGS_BASE | BindingFlags.Instance | BindingFlags.Static;

        // helper functions
        private static BindingFlags makeInstanceOnly(BindingFlags flags) { return (flags & ~BindingFlags.Static) | BindingFlags.Instance; }
        private static BindingFlags makeStaticOnly(BindingFlags flags) { return (flags & ~BindingFlags.Instance) | BindingFlags.Static; }
        #endregion

        #region Info Getters
        // these are straight wrappers around the functions on Type (except they throw on null returns), but since they have a default for flags, they're easier to use

        public static FieldInfo getFieldInfo(Type type, string name, BindingFlags flags = BINDING_FLAGS_ANY)
        {
            FieldInfo info = type.GetField(name, flags);
            if(info == null)
                throw new InvalidOperationException("Could not find field " + name + " on type " + type.Name);
            return info;
        }

        public static PropertyInfo getPropertyInfo(Type type, string name, BindingFlags flags = BINDING_FLAGS_ANY)
        {
            PropertyInfo info = type.GetProperty(name, flags);
            if(info == null)
                throw new InvalidOperationException("Could not find p " + name + " on type " + type.Name);
            return info;
        }

        public static MethodInfo getMethodInfo(Type type, string name, BindingFlags flags = BINDING_FLAGS_ANY)
        {
            MethodInfo info = type.GetMethod(name, flags);
            if(info == null)
                throw new InvalidOperationException("Could not find method " + name + " on type " + type.Name);
            return info;
        }

        public static MethodInfo getMethodInfo(Type type, string name, Type[] paramTypes, BindingFlags flags = BINDING_FLAGS_ANY)
        {
            MethodInfo info = type.GetMethod(name, flags, null, paramTypes, null);
            if(info == null)
                throw new InvalidOperationException("Could not find method " + name + " on type " + type.Name);
            return info;
        }
        #endregion

        #region Generators for fast (multi-use) methods for fields
        // These require code generation to create a method that gets/sets the field. Expression trees are the easiest way to do this (instead of directly using IL generator)

        public static Func<TObject, TField> createGetInstanceField<TObject, TField>(string fieldName, BindingFlags flags = BINDING_FLAGS_BASE)  where TObject : class { return createGetInstanceField<TObject, TField>(getFieldInfo(typeof(TObject), fieldName, makeInstanceOnly(flags))); }
        public static Func<TObject, TField> createGetInstanceField<TObject, TField>(FieldInfo field)  where TObject : class
        {
            ParameterExpression paramObj = Expression.Parameter(typeof(TObject), "obj");
            return Expression.Lambda<Func<TObject, TField>>(Expression.Field(paramObj, field), paramObj).Compile();
        }

        public static Func<TField> createGetStaticField<TField>(Type type, string fieldName, BindingFlags flags = BINDING_FLAGS_BASE) { return createGetStaticField<TField>(getFieldInfo(type, fieldName, makeStaticOnly(flags))); }
        public static Func<TField> createGetStaticField<TField>(FieldInfo field)
        {
            return Expression.Lambda<Func<TField>>(Expression.Field(null, field)).Compile();
        }

        public static Action<TObject, TField> createSetInstanceField<TObject, TField>(string fieldName, BindingFlags flags = BINDING_FLAGS_BASE)  where TObject : class { return createSetInstanceField<TObject, TField>(getFieldInfo(typeof(TObject), fieldName, makeInstanceOnly(flags)));}
        public static Action<TObject, TField> createSetInstanceField<TObject, TField>(FieldInfo field)  where TObject : class
        {
            ParameterExpression paramObj = Expression.Parameter(typeof(TObject), "obj");
            ParameterExpression paramValue = Expression.Parameter(typeof(TField), "value");
            return Expression.Lambda<Action<TObject, TField>>(Expression.Assign(Expression.Field(paramObj, field), paramValue), paramObj, paramValue).Compile();
        }

        public static Action<TField> createSetStaticField<TField>(Type type, string fieldName, BindingFlags flags = BINDING_FLAGS_BASE) { return createSetStaticField<TField>(getFieldInfo(type, fieldName, makeStaticOnly(flags))); }
        public static Action<TField> createSetStaticField<TField>(FieldInfo field)
        {
            ParameterExpression paramValue = Expression.Parameter(typeof(TField), "value");
            return Expression.Lambda<Action<TField>>(Expression.Assign(Expression.Field(null, field), paramValue), paramValue).Compile();
        }
        #endregion

        #region The same for properties
        // these don't use expression trees since there's already methods to bind to
        public static Func<TObject, TProperty> createGetInstanceProperty<TObject, TProperty>(string propertyName, BindingFlags flags = BINDING_FLAGS_BASE) where TObject : class { return createGetInstanceProperty<TObject, TProperty>(getPropertyInfo(typeof(TObject), propertyName, makeInstanceOnly(flags))); }
        public static Func<TObject, TProperty> createGetInstanceProperty<TObject, TProperty>(PropertyInfo property) where TObject : class { return (Func<TObject, TProperty>) property.getGetOrThrow().CreateDelegate(typeof(Func<TObject, TProperty>)); }
        public static Func<TProperty> createGetStaticProperty<TProperty>(Type type, string propertyName, BindingFlags flags = BINDING_FLAGS_BASE) { return createGetStaticProperty<TProperty>(getPropertyInfo(type, propertyName, makeStaticOnly(flags))); }
        public static Func<TProperty> createGetStaticProperty<TProperty>(PropertyInfo property) { return (Func<TProperty>) property.getGetOrThrow().CreateDelegate(typeof(Func<TProperty>)); }
        public static Action<TObject, TProperty> createSetInstanceProperty<TObject, TProperty>(string propertyName, BindingFlags flags = BINDING_FLAGS_BASE) where TObject : class { return createSetInstanceProperty<TObject, TProperty>(getPropertyInfo(typeof(TObject), propertyName, makeInstanceOnly(flags))); } 
        public static Action<TObject, TProperty> createSetInstanceProperty<TObject, TProperty>(PropertyInfo property) where TObject : class { return (Action<TObject, TProperty>) property.getSetOrThrow().CreateDelegate(typeof(Action<TObject, TProperty>)); } 
        public static Action<TProperty> createSetStaticProperty<TProperty>(Type type, string propertyName, BindingFlags flags = BINDING_FLAGS_BASE) { return createSetStaticProperty<TProperty>(getPropertyInfo(type, propertyName, makeStaticOnly(flags))); }
        public static Action<TProperty> createSetStaticProperty<TProperty>(PropertyInfo property) { return (Action<TProperty>) property.getSetOrThrow().CreateDelegate(typeof(Action<TProperty>)); } 
        
        // helpers
        // ReSharper disable PossibleNullReferenceException
        private static MethodInfo getGetOrThrow(this PropertyInfo p) { MethodInfo m = p.GetGetMethod(true); if(m == null) throw new InvalidOperationException("Property " + p.DeclaringType.Name + "." + p.Name + " does not have a getter"); return m; }
        private static MethodInfo getSetOrThrow(this PropertyInfo p) { MethodInfo m = p.GetSetMethod(true); if(m == null) throw new InvalidOperationException("Property " + p.DeclaringType.Name + "." + p.Name + " does not have a setter"); return m; }
        // ReSharper restore PossibleNullReferenceException
        #endregion

        #region typesafe CreateDelegate wrappers
        // wrappers around MethodInfo.CreateDelegate that return the correct type
        public static TDelegate createDelegate<TDelegate>(Type type, string methodName, BindingFlags flags = BINDING_FLAGS_ANY) where TDelegate : class { return createDelegate<TDelegate>(getMethodInfo(type, methodName, flags)); }
        public static TDelegate createDelegate<TDelegate>(object obj, string methodName, BindingFlags flags = BINDING_FLAGS_ANY) where TDelegate : class { return createDelegate<TDelegate>(getMethodInfo(obj.GetType(), methodName, makeInstanceOnly(flags) | BindingFlags.FlattenHierarchy), obj); }
        public static TDelegate createDelegate<TDelegate>(MethodInfo method, object obj = null) where TDelegate : class
        {
            Delegate dg = method.CreateDelegate(typeof(TDelegate), obj);
            if(dg == null)
                throw new InvalidOperationException("CreateDelegate() returned null");
            TDelegate result = dg as TDelegate;
            if(result == null)
                throw new InvalidOperationException("Could not cast delegate to " + typeof(TDelegate).Name);
            return result;
        }
        #endregion

        #region Slow (single-use) reflection wrappers

        // Get field/p
        public static object slowGetStaticField(Type type, string name, BindingFlags flags = BINDING_FLAGS_STATIC) { return getFieldInfo(type, name, makeStaticOnly(flags)).GetValue(null); }
        public static object slowGetStaticProperty(Type type, string name, BindingFlags flags = BINDING_FLAGS_STATIC) { return getPropertyInfo(type, name, makeStaticOnly(flags)).GetValue(null); }
        public static object slowGetInstanceField(object obj, string name, BindingFlags flags = BINDING_FLAGS_INSTANCE) { return getFieldInfo(obj.GetType(), name, makeInstanceOnly(flags) | BindingFlags.FlattenHierarchy).GetValue(obj); }
        public static object slowGetInstanceProperty(object obj, string name, BindingFlags flags = BINDING_FLAGS_INSTANCE) { return getPropertyInfo(obj.GetType(), name, makeInstanceOnly(flags) | BindingFlags.FlattenHierarchy).GetValue(obj); }

        // Set field/p
        public static void slowSetStaticField(Type type, string name, object value, BindingFlags flags = BINDING_FLAGS_STATIC) { getFieldInfo(type, name, makeStaticOnly(flags)).SetValue(null, value); }
        public static void slowSetStaticProperty(Type type, string name, object value, BindingFlags flags = BINDING_FLAGS_STATIC) { getPropertyInfo(type, name, makeStaticOnly(flags)).SetValue(null, value); }
        public static void slowSetInstanceField(object obj, string name, object value,  BindingFlags flags = BINDING_FLAGS_INSTANCE) { getFieldInfo(obj.GetType(), name, makeInstanceOnly(flags) | BindingFlags.FlattenHierarchy).SetValue(obj, value); }
        public static void slowSetInstanceProperty(object obj, string name, object value,  BindingFlags flags = BINDING_FLAGS_INSTANCE) { getPropertyInfo(obj.GetType(), name, makeInstanceOnly(flags) | BindingFlags.FlattenHierarchy).SetValue(obj, value); }
        
        #endregion
    }
}