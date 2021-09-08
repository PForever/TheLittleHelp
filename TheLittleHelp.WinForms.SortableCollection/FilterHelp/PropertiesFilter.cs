using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using TheLittleHelp.System;

namespace TheLittleHelp.WinForms.SortableCollection.FilterHelp
{
    /// <summary>
    /// Filter's data dictionary of <see cref="PropertyValidate"/>, where key is a position of validated property into the source.
    /// </summary>
    public class PropertiesFilter : Dictionary<int, PropertyValidate>
    {
        public PropertiesFilter()
        {

        }
        public PropertiesFilter(IList<PropertyValidate> filters) : base(filters.ToDictionary(f => f.Key.Position, f => f))
        {

        }
        public PropertiesFilter(IEnumerable<PropertyValidate> filters) : base(filters.ToDictionary(f => f.Key.Position, f => f))
        {

        }
        public PropertiesFilter(Type source)
        {
            var properties = TypeDescriptor.GetProperties(source);
            int i = 0;
            foreach (PropertyDescriptor propertyDescriptor in properties)
            {
                Add(new PropertyValidate(propertyDescriptor.PropertyType, (i++, propertyDescriptor.Name)));
            }
        }
        /// <summary>
        /// Create <see cref="PropertiesFilter"/>, where DisplayName <see cref="PropertyValidate.Key"/> is a <see cref="DataGridViewColumn.Name"/>
        /// </summary>
        /// <param name="columns"></param>
        public PropertiesFilter(IEnumerable<DataGridViewColumn> columns)
        {
            int i = 0;
            foreach (DataGridViewColumn column in columns.Cast<DataGridViewColumn>().Where(c => c.ValueType != null))
            {
                if (column is DataGridViewComboBoxColumn cb)
                    Add(new PropertyValidate(column.ValueType, (i++, column.DataPropertyName, column.HeaderText), CreateSourceList(cb)));
                else Add(new PropertyValidate(column.ValueType, (i++, column.DataPropertyName, column.HeaderText)));
            }
        }

        private ILookup<string, object> CreateSourceList(DataGridViewComboBoxColumn cb)
        {
            if (cb.DataSource is IList list)
            {
                if (list.Count == 0) return Enumerable.Empty<(string, object)>().ToLookup(v => v.Item1, v => v.Item2);
                var result = new List<(string Key, object Value)>(list.Count);

                var type = list[0].GetType();
                var display = type.GetProperty(cb.DisplayMember);
                var value = type.GetProperty(cb.ValueMember);
                Func<object, string> getDisplay = display != null ? (Func<object, string>) (i => (string)display.GetValue(i)) : i => i.ToString();
                Func<object, object> getValue = value != null ? (Func<object, object>) (i => value.GetValue(i)) : i => i;
                foreach (var item in list)
                {
                    result.Add((getDisplay(item), getValue(item)));
                }
                return result.ToLookup(kvp => kvp.Key, kvp => kvp.Value);
            }
            else throw new ArgumentException();
        }
        /// <summary>
        /// Add <see cref="PropertyValidate"/> to dictionary, where key is a Position into <see cref="PropertyValidate.Key"/>
        /// </summary>
        /// <param name="value"></param>
        public void Add(PropertyValidate value)
        {
            base.Add(value.Key.Position, value);
        }

        private static MethodInfo ValidatorExpressionInfo { get; } = typeof(PropertiesFilter).GetMethod(nameof(ValidatorExpression), BindingFlags.NonPublic | BindingFlags.Static);
        private static MethodInfo TypeValidatorExpressionInfo { get; } = typeof(PropertiesFilter).GetMethod(nameof(TypeValidatorExpression), BindingFlags.NonPublic | BindingFlags.Static);
        private static MethodInfo PredicateExpressionInfo { get; } = typeof(PropertiesFilter).GetMethod(nameof(PredicateExpression), BindingFlags.NonPublic | BindingFlags.Static);
        private static MethodInfo AllPredicateInfo { get; } = typeof(PropertiesFilter).GetMethod(nameof(AllPredicate), BindingFlags.NonPublic | BindingFlags.Static);
        private static TypedKeyCashe<Type, MethodInfo> AllPredicateInfoGenerics = new TypedKeyCashe<Type, MethodInfo>();
        private static MethodInfo AllPredicateInfoGeneric(Type type) => AllPredicateInfoGenerics.GetOrAdd(type, t => AllPredicateInfo.MakeGenericMethod(t));
        private static Delegate ValidatorExpression(Type srcType, TypeFilter filter, params object[] args)
        {
            var parameter = Expression.Parameter(srcType);
            var toStringConst = Expression.Call(parameter, "ToString", null);
            var stingFilterConst = Expression.Constant(filter.Filter);
            var result = Expression.Equal(toStringConst, stingFilterConst);
            var lambda = Expression.Lambda(result, parameter);
            return _propertyStringValidators.GetOrAdd(filter, (f) => lambda.Compile());
        }
        private static TypedKeyCashe<TypeFilter, Delegate> _propertyStringValidators = new TypedKeyCashe<TypeFilter, Delegate>();
        private static Delegate PredicateExpression(TypeFilter filter)
        {
            return filter.Predicate;
            //return _propertyPredicates.GetOrAdd(filter, f => f.Predicate.Compile());
        }
        //private static ConcurrentDictionary<TypeFilter, Delegate> _propertyPredicates = new ConcurrentDictionary<TypeFilter, Delegate>();

        //todo ввести визитёра
        public static Func<T, bool> CreateFilter<T>(TypeFilter filter)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            Func<T, bool> propertyValidator;
            if (!String.IsNullOrEmpty(filter.Filter))
                propertyValidator = (Func<T, bool>)ValidatorExpression(typeof(T), filter, FilterExpression);
            else if (filter.PropertyFilters != null && filter.PropertyFilters.Count != 0)
                propertyValidator = (Func<T, bool>)TypeValidatorExpression(typeof(T), filter);
            else propertyValidator = (Func<T, bool>)PredicateExpression(filter);
            return propertyValidator;
        }
        private static Func<T, bool> AllPredicate<T>(List<Delegate> delegates)
        {
            return src => delegates.Cast<Func<T, bool>>().All(d => d(src));
        }
        private static Delegate TypeValidatorExpression(Type srcType, TypeFilter filter)
        {
            Delegate CreateTypeValidatorExpression(Type t, TypeFilter f)
            {
                //TODO Add
                var list = new List<Delegate>(f.PropertyFilters.Count);
                foreach (var propertyFilter in f.PropertyFilters)
                {
                    Delegate propertyValidator;
                    if (!String.IsNullOrEmpty(propertyFilter.Value.Filter))
                        propertyValidator = CreatePropertyStringValidator(t, propertyFilter.Key, propertyFilter.Value, FilterExpression).Compile();
                    else if (propertyFilter.Value.PropertyFilters != null && propertyFilter.Value.PropertyFilters.Count != 0)
                        propertyValidator = CreatePropertyTypeValidator(t, propertyFilter.Key, propertyFilter.Value).Compile();
                    else if (propertyFilter.Value.Predicate != null) propertyValidator = CreatePropertyPredicate(t, propertyFilter.Key, propertyFilter.Value).Compile();
                    else throw new ArgumentNullException();
                    list.Add(propertyValidator);
                }
                var addPredicate = AllPredicateInfoGeneric(t);
                return (Delegate)addPredicate.Invoke(null, new[] { list });
            }
            return _propertyTypeValidators.GetOrAdd(filter, (f) => CreateTypeValidatorExpression(srcType, f));
        }

        private static TypedKeyCashe<TypeFilter, Delegate> _propertyTypeValidators = new TypedKeyCashe<TypeFilter, Delegate>();

        private static LambdaExpression CreatePropertyTypeValidator(Type srcType, string propertyName, TypeFilter filter)
        {
            var parameter = Expression.Parameter(srcType);
            var property = Expression.Property(parameter, propertyName);
            var propertyTypeConst = Expression.Constant(property.Type, typeof(Type));
            var filterConst = Expression.Constant(filter, typeof(TypeFilter));
            var validator = Expression.Call(TypeValidatorExpressionInfo, propertyTypeConst, filterConst);
            var propertyAsObject = Expression.Convert(property, typeof(object));
            var propertyArray = Expression.NewArrayInit(typeof(object), propertyAsObject);
            var invokeInfo = Expression.Call(validator, nameof(Delegate.DynamicInvoke), null, propertyArray);
            var resultConvert = Expression.Convert(invokeInfo, typeof(bool));
            return Expression.Lambda(resultConvert, parameter);
        }
        private static LambdaExpression CreatePropertyStringValidator(Type srcType, string propertyName, TypeFilter filter, params object[] args)
        {
            var parameter = Expression.Parameter(srcType);
            var property = Expression.Property(parameter, propertyName);
            var propertyTypeConst = Expression.Constant(property.Type, typeof(Type));
            var filterConst = Expression.Constant(filter, typeof(TypeFilter));
            var argsConst = Expression.Constant(args, typeof(object[]));
            var validator = Expression.Call(ValidatorExpressionInfo, propertyTypeConst, filterConst, argsConst);
            var propertyAsObject = Expression.Convert(property, typeof(object));
            var propertyArray = Expression.NewArrayInit(typeof(object), propertyAsObject);
            var invokeInfo = Expression.Call(validator, nameof(Delegate.DynamicInvoke), null, propertyArray);
            var resultConvert = Expression.Convert(invokeInfo, typeof(bool));
            return Expression.Lambda(resultConvert, parameter);
        }
        private static LambdaExpression CreatePropertyPredicate(Type srcType, string propertyName, TypeFilter filter)
        {
            var parameter = Expression.Parameter(srcType);
            var property = Expression.Property(parameter, propertyName);
            var filterConst = Expression.Constant(filter, typeof(TypeFilter));
            var validator = Expression.Call(PredicateExpressionInfo, filterConst);
            var argType = filter.Predicate.Method.GetParameters()[0].ParameterType;
            var convertedProperty = Expression.Convert(property, argType);
            var propertyAsObject = Expression.Convert(convertedProperty, typeof(object));
            var propertyArray = Expression.NewArrayInit(typeof(object), propertyAsObject);
            var invokeInfo = Expression.Call(validator, nameof(Delegate.DynamicInvoke), null, propertyArray);
            var resultConvert = Expression.Convert(invokeInfo, typeof(bool));
            return Expression.Lambda(resultConvert, parameter);
        }

        private static Expression<Func<string, string, bool>> FilterExpression { get; } = (value, pattern) => !String.IsNullOrEmpty(value) && Regex.IsMatch(value, pattern);

    }
}
