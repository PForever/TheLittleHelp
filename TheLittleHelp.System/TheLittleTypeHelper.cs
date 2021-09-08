using System;
using System.Collections.Concurrent;
using System.Linq;
using TheLittleHelp.System.ExpressionHelp;

namespace TheLittleHelp.System.TypeHelp
{
    public static class TheLittleTypeHelper
    {
        private static readonly Type[] _additionalPrimitiveTypes = new[] { typeof(DateTime), typeof(DateTime?), typeof(string), typeof(bool?), typeof(byte?), typeof(short?), typeof(int?), typeof(long?), typeof(decimal?), typeof(float?), typeof(double?) };
        private static readonly Type StringType = typeof(string);
        private static readonly Type[] _intNumbers = new[] { typeof(byte), typeof(short), typeof(int), typeof(long), typeof(byte?), typeof(short?), typeof(int?), typeof(long?), };
        private static readonly Type[] _lineSpaceType = new[] { typeof(DateTime), typeof(DateTime?), typeof(byte?), typeof(short?), typeof(int?), typeof(long?), typeof(decimal?), typeof(float?), typeof(double?), typeof(byte), typeof(short), typeof(int), typeof(long), typeof(decimal), typeof(float), typeof(double) };

        private static readonly Type[] _decNumbers = new[] { typeof(decimal), typeof(double), typeof(float), typeof(decimal?), typeof(double?), typeof(float?) };
        private static readonly Type[] _dateTypes = new[] { typeof(DateTime), typeof(DateTime?) };
        private static readonly Type[] _boolTypes = new[] { typeof(bool), typeof(bool?) };
        private static readonly Type[] _guidTypes = new[] { typeof(Guid), typeof(Guid?) };


        public static bool IsCollection(this Type type) => type.GetInterfaces().Any(i => i.Name.Contains("IEnumerable") && i.IsGenericType);
        internal static Type GetCollectionGenericArg(Type type) => type.GetInterfaces().FirstOrDefault(i => i.Name.Contains("IEnumerable") && i.IsGenericType).GetGenericArguments().Single();
        public static bool IsNulluble(this Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        public static bool IsLineSpace(this Type type) => _lineSpaceType.Contains(type);
        //public static bool IsComparable(this Type type) => type.IsAssignableFrom(typeof(IComparable<>)) || type.IsAssignableFrom(typeof(IComparable));
        public static bool IsComparable(this Type type) => typeof(IComparable).IsAssignableFrom(type) ||
            type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IComparable<>) && i.GenericTypeArguments[0].IsAssignableFrom(type));

        public static bool IsString(this Type t) => t == StringType;
        public static bool IsIntNumber(this Type t) => _intNumbers.Contains(t);
        public static bool IsDecNumber(this Type t) => _decNumbers.Contains(t);
        public static bool IsDate(this Type t) => _dateTypes.Contains(t);
        public static bool IsBool(this Type t) => _boolTypes.Contains(t);
        public static bool IsGuid(this Type t) => _guidTypes.Contains(t);
        public static object CreateNew(this Type type)
        {
            if (type == StringType) return "";
            return Activator.CreateInstance(type);
        }

        internal static string PrintValue(object value)
        {
            switch (value)
            {
                case DateTime d: return d.ToString("d");
                case null: return null;
                default: return value.ToString();
            }
        }

        //public static Type GetTypeOfParent(this IFilterData f)
        //{
        //    if (f == null) throw new ArgumentNullException(nameof(f));
        //    else return GetTypeOfData(f.Parent);
        //}
        //public static Type GetTypeOfData(IFilterData data)
        //{
        //    if (data == null) throw new ArgumentNullException(nameof(data));

        //    var t = data.PropertyType;
        //    if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
        //        return t.GetGenericArguments().Single();
        //    else return t;
        //}

        //internal static IFilterData GetGrandFather(this IFilterData data)
        //{
        //    while (data.Parent is IFilterData parent) data = parent;
        //    return data;
        //}
        //public static (Type SrcTypem, string PropertyName, string DisplayProperty) ToDisplay(this IFilterData data) => (data.GetTypeOfParent(), data.PropertyName, data.DisplayName);
        //public static (Type SrcType, string PropertyName, string DisplayMember, string ValueMember, Func<IEnumerable<ComboContainer>> ValidValues) ToDisplayValues(this IFilterData data)
        //{
        //    var (DisplayMember, ValueMember, Values) = data.ValidValues;
        //    return (data.GetTypeOfParent(), data.PropertyName, DisplayMember, ValueMember, Values);
        //}
        public static bool IsPrimitive(this Type type)
        {
            return type.IsPrimitive || _additionalPrimitiveTypes.Contains(type);
        }

        public static void SetProperty<T>(object defaultObject, string propertyName, T propertyValue)
        {
            var type = defaultObject.GetType();
            var setter = DictionatyHost<T>.SetPropertyList.GetOrAdd((type, propertyName), TheLittleExpressionHelper.CreateSetProperty<T>(defaultObject, propertyName).Compile());
            setter(defaultObject, propertyValue);
        }
        public static T GetProperty<T>(object defaultObject, string propertyName)
        {
            if (defaultObject == null) return default(T);
            var type = defaultObject.GetType();
            var getter = DictionatyHost<T>.GetPropertyList.GetOrAdd((type, propertyName), TheLittleExpressionHelper.CreateGetProperty<T>(defaultObject, propertyName).Compile());
            return getter(defaultObject);
        }

        static class DictionatyHost<T>
        {
            public static ConcurrentDictionary<(Type Type, string Property), Action<object, T>> SetPropertyList { get; } = new ConcurrentDictionary<(Type Type, string Property), Action<object, T>>();
            public static ConcurrentDictionary<(Type Type, string Property), Func<object, T>> GetPropertyList { get; } = new ConcurrentDictionary<(Type Type, string Property), Func<object, T>>();
        }


        public static object GetPropertyObject(object v, string member) => string.IsNullOrEmpty(member) ? v : GetProperty<object>(v, member);

        public static string GetPropertyString(object v, string member) => string.IsNullOrEmpty(member) ? v.ToString() : GetProperty<string>(v, member);

        private static bool IsSimleTypeInternal(this Type t)
        {
            return t.IsPrimitive || t == typeof(string) || t == typeof(DateTime);
        }
        public static bool IsSimleType(this Type t)
        {
            return IsSimleTypeInternal(t) || t == typeof(Nullable<>) && IsSimleTypeInternal(t.GenericTypeArguments[0]);
        }
    }
}
