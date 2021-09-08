using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using TheLittleHelp.System.CollectionHelp;

namespace TheLittleHelp.System.DynamicHelp
{
    /// <summary>
    /// Class contains helpers for work with dymanicly constructed/detected objects
    /// </summary>
    public static class TheLittleDynamicHelper
    {
        /// <summary>
        /// Get property display name set in <see cref="DisplayNameAttribute"/>, otherwise property name
        /// <code lang="csharp">
        ///     typeof(T).GetProperty("Property1").GetPropertyDisplayName()
        /// </code>
        /// </summary>
        /// <example>
        /// declare:
        /// <code lang="csharp">
        /// class MyClass
        /// {
        ///     [DisplayName("My Property 1")]
        ///     public int MyProperty1 { get; set; }
        ///     public int MyProperty2 { get; set; }
        /// }
        /// </code>
        /// usage:
        /// <code lang="csharp">
        ///     typeof(MyClass).GetProperty("Property1").GetPropertyDisplayName() //My Property 1
        ///     typeof(MyClass).GetProperty("Property2").GetPropertyDisplayName() //MyProperty2
        /// </code>
        /// </example>
        /// <param name="p"></param>
        /// <returns></returns>
        public static string GetPropertyDisplayName(this PropertyInfo p) => p.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? p.Name;

        private static readonly TypedKeyCashe<Type, Delegate> _dictionary = new TypedKeyCashe<Type, Delegate>();
        /// <summary>
        /// Copy all public non static properties from <paramref name="src"/> to <paramref name="dst"/>
        /// </summary>
        /// <remarks>
        /// <para>Method use a <see cref="TypedKeyCashe{TKey, TValue}"/> for cashe a result delegate</para>
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        public static void MemberCopy<T>(T src, T dst)
        {
            ((Action<T, T>)_dictionary.GetOrAdd(typeof(T), () => CreateCopyMethod<T>()))(src, dst);
        }
        private static Action<T, T> CreateCopyMethod<T>()
        {
            var type = typeof(T);
            var srcExpression = Expression.Parameter(type);
            var dstExpression = Expression.Variable(type);
            var block = new List<Expression>();
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && p.CanWrite);
            foreach (var prop in props)
            {
                block.Add(Expression.Assign(Expression.Property(dstExpression, prop), Expression.Property(srcExpression, prop)));
            }

            var body = Expression.Block(block);
            return Expression.Lambda<Action<T, T>>(body, srcExpression, dstExpression).Compile();

        }
        private static readonly ValueTypedKeyCashe<(Type, Type), Delegate> MapDelegates = new ValueTypedKeyCashe<(Type, Type), Delegate>();
        private static readonly TypedKeyCashe<Type, Delegate> _cashedConverters = new TypedKeyCashe<Type, Delegate>();
        /// <summary>
        /// Transfers all object from <paramref name="src"/> to constructor of type <typeparamref name="TResult"/> whitch has only one parameter and that parameter of type <typeparamref name="TSource"/>.
        /// <code lang="csharp">
        /// _repository.GetViewModels().To&lt;ViewModel, View&gt;()
        /// </code>
        /// </summary>
        /// <remarks>
        /// <para>Method use a <see cref="TypedKeyCashe{TKey, TValue}"/> for cashe a result delegate</para>
        /// </remarks>
        /// <example>
        /// Type <typeparamref name="TResult"/> mast contains constructor whitch has only one parameter and that parameter of type <typeparamref name="TSource"/>
        /// <code lang="csharp">
        /// class View
        /// {
        ///     private readonly ViewModel _viewModel;
        ///     public View(ViewModel viewModel)
        ///     {
        ///         _viewModel = viewModel;
        ///     }
        /// }
        /// </code>
        /// then usege is _repository.GetViewModels().&lt;ToViewModel, View&gt;()
        /// </example>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="src"></param>
        /// <returns></returns>
        public static IEnumerable<TResult> To<TSource, TResult>(this IEnumerable<TSource> src)
        {
            var typeFunc = typeof(Func<TSource, TResult>);
            Func<TSource, TResult> lambda;
            IEnumerable<TResult> Worker()
            {
                foreach (var item in src)
                {
                    yield return lambda.Invoke(item);
                }
            }
            if (_cashedConverters.TryGet(typeFunc, out var result))
            {
                lambda = (Func<TSource, TResult>)result;
                return Worker();
            }
            var typeDest = typeof(TResult);
            var typeSrc = typeof(TSource);
            var ctrInfo = typeDest.GetConstructor(new[] { typeof(TSource) });
            if (ctrInfo == null) throw new ArgumentException($"Type {typeDest.Name} has no constructor within type {typeSrc.Name}");
            var srcParam = Expression.Parameter(typeSrc);
            var ctr = Expression.New(ctrInfo, srcParam);
            lambda = Expression.Lambda<Func<TSource, TResult>>(ctr, srcParam).Compile();
            _cashedConverters.TryAdd(typeFunc, lambda);
            return Worker();
        }
        private static readonly TypedKeyCashe<Type, Delegate> ActivateDelegates = new TypedKeyCashe<Type, Delegate>();
        /// <summary>
        /// Activate instance of <typeparamref name="T"/> if it contains parameterless constructor via expression tree
        /// <para>TheLittleDynamicHelper.ActivateInstance&lt;MyClass&gt;()</para>
        /// </summary>
        /// <remarks>
        /// <para>Method use a <see cref="TypedKeyCashe{TKey, TValue}"/> for cashe a result delegate</para>
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <returns>new instance of type <typeparamref name="T"/></returns>
        public static T ActivateInstance<T>() where T : new()
        {
            Delegate Activate(Type type)
            {
                var ctor = Expression.New(type);
                var lambda = Expression.Lambda(ctor);
                return lambda.Compile();
            }
            var activator = (Func<T>)ActivateDelegates.GetOrAdd(typeof(T), Activate);
            return activator();
        }

        /// <summary>
        /// Create new instance of type <typeparamref name="TDst"/> and copy all public non static properties from <paramref name="src"/> to result.
        /// </summary>
        /// <remarks>
        /// <para>Method use a <see cref="TypedKeyCashe{TKey, TValue}"/> for cashe a result delegate</para>
        /// <para>Can't handle properties with the same name but with different types. It become to <see cref="InvalidCastException"/></para>
        /// </remarks>
        /// <example>
        /// if you have class like this
        /// <code lang="csharp">
        /// class PersonDto
        /// {
        ///     string Name { get; set; }
        ///     string ParentName { get; set; }
        ///     int Age { get; set; }
        /// }
        /// class Person
        /// {
        ///     string Name { get; set; }
        ///     string City { get; set; }
        ///     int Age { get; set; }
        /// }
        /// </code>
        /// then this action
        /// <para>_repostiory.GetPersonDto().Map()</para>
        /// return instance of type Perosn where Name and Age will be initialized by Name and Age of PersonDto type's instance but City will by null
        /// </example>
        /// <exception cref="InvalidCastException"/>
        /// <typeparam name="TSrc"></typeparam>
        /// <typeparam name="TDst"></typeparam>
        /// <param name="src"></param>
        /// <returns></returns>
        public static TDst Map<TSrc, TDst>(this TSrc src) where TDst : new() => ((Func<TSrc, TDst, TDst>)MapDelegates.GetOrAdd((typeof(TSrc), typeof(TDst)), () => CreateCopyMethod<TSrc, TDst>()))(src, ActivateInstance<TDst>());
        /// <summary>
        /// Copy all public non static properties from <paramref name="src"/> to <paramref name="dst"/> that contains in last one.
        /// </summary>
        /// <remarks>
        /// <para>Method use a <see cref="TypedKeyCashe{TKey, TValue}"/> for cashe a result delegate</para>
        /// </remarks>
        /// <typeparam name="TSrc"></typeparam>
        /// <typeparam name="TDst"></typeparam>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <returns></returns>
        public static TDst Map<TSrc, TDst>(TSrc src, TDst dst) => ((Func<TSrc, TDst, TDst>)MapDelegates.GetOrAdd((typeof(TSrc), typeof(TDst)), () => CreateCopyMethod<TSrc, TDst>()))(src, dst);
        private static Func<TSrc, TDst, TDst> CreateCopyMethod<TSrc, TDst>()
        {
            var typeSrc = typeof(TSrc);
            var typeDst = typeof(TDst);
            var srcExpression = Expression.Parameter(typeSrc);
            var dstExpression = Expression.Parameter(typeDst);
            var block = new List<Expression>();
            var propsSrc = typeSrc.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && p.CanWrite).Select(p => p.Name);
            var propsDst = typeDst.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && p.CanWrite).Select(p => p.Name);
            var propNames = propsSrc.Intersect(propsDst);
            foreach (string prop in propNames)
            {
                block.Add(Expression.Assign(Expression.Property(dstExpression, prop), Expression.Property(srcExpression, prop)));
            }
            block.Add(dstExpression);
            var body = Expression.Block(block);
            return Expression.Lambda<Func<TSrc, TDst, TDst>>(body, srcExpression, dstExpression).Compile();
        }
        private static Delegate CreateCopyMethod<TDst>(Type[] srcTypes)
        {
            var typeDst = typeof(TDst);
            var srcExpressions = srcTypes.ToDictionary(t => t, t => Expression.Parameter(t));
            var dstExpression = Expression.Parameter(typeDst);
            var block = new List<Expression>();
            var propsSrc = new Dictionary<string, Type>();
            var propsDst = typeDst.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && p.CanWrite).Select(p => p.Name).ToHashSet();

            foreach (var (name, type) in from t in srcTypes
                                         from p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                         where p.CanRead && p.CanWrite && !propsSrc.ContainsKey(p.Name) && propsDst.Contains(p.Name)
                                         select (p.Name, Type: t))
                propsSrc.Add(name, type);

            foreach (var prop in propsSrc) block.Add(Expression.Assign(Expression.Property(dstExpression, prop.Key), Expression.Property(srcExpressions[prop.Value], prop.Key)));
            block.Add(dstExpression);
            var body = Expression.Block(block);
            return Expression.Lambda(body, srcExpressions.Values.AsEnumerable().Append(dstExpression)).Compile();
        }
        /// <summary>
        /// Sort <see cref="IQueryable{T}"/> collection of <typeparamref name="T"/> by expression selecected in <paramref name="lambda"/>
        /// <code lang="csharp">_repository.GetPeople().OrderBy(lambda, direction);</code>
        /// </summary>
        /// <remarks>
        /// <para>Method use a <see cref="ValueTypedKeyCashe{TKey, TValue}"/> for cashe a LINQ's methods</para>
        /// </remarks>
        /// <example>
        /// <code lang="csharp">
        /// var parameter = Expression.Parameter(typeof(Person));
        /// var sortedPropertyName = Console.ReadLine();
        /// var sortedProperty = Expression.PropertyOrField(parameter, sortedPropertyName);
        /// var lambda = Expression.Lambda(sortedProperty, parameter);
        /// _repository.GetPeople().OrderBy(lambda, SortingDirection.Asc);
        /// </code>
        /// </example>
        /// <typeparam name="T"></typeparam>
        /// <param name="src"></param>
        /// <param name="lambda"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> src, LambdaExpression lambda, SortingDirection direction = SortingDirection.Asc)
        {
            var typeResult = lambda.ReturnType;
            var orderby = GetOrder<T>(typeResult, direction);
            return (IOrderedQueryable<T>)orderby.Invoke(null, new object[] { src, lambda });
        }
        /// <summary>
        /// Sort <see cref="IQueryable"/> collection of <paramref name="type"/> by expression selecected in <paramref name="lambda"/>
        /// <code lang="csharp">_repository.GetData(type).OrderBy(type, lambda, direction);</code>
        /// </summary>
        /// <remarks>
        /// <para>Method use a <see cref="ValueTypedKeyCashe{TKey, TValue}"/> for cashe a LINQ's methods</para>
        /// </remarks>
        /// <example>
        /// <code lang="csharp">
        /// var typeName = Console.ReadLine();
        /// var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(typeName)).First(t => !(t is null));
        /// var parameter = Expression.Parameter(type);
        /// var sortedPropertyName = Console.ReadLine();
        /// var sortedProperty = Expression.PropertyOrField(parameter, sortedPropertyName);
        /// var lambda = Expression.Lambda(sortedProperty, parameter);
        /// _repository.GetData(type).OrderBy(lambda, SortingDirection.Asc);
        /// </code>
        /// </example>
        /// <param name="src"></param>
        /// <param name="type"></param>
        /// <param name="lambda"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static IOrderedQueryable OrderBy(this IQueryable src, Type type, LambdaExpression lambda, SortingDirection direction = SortingDirection.Asc)
        {
            var typeResult = lambda.ReturnType;
            var orderby = GetOrder(type, typeResult, direction);
            return (IOrderedQueryable)orderby.Invoke(null, new object[] { src, lambda });
        }

        private static MethodInfo GetOrder<T>(Type typeResult, SortingDirection direction) => GetOrder(typeof(T), typeResult, direction);

        private static MethodInfo GetOrder(Type typeSrc, Type typeResult, SortingDirection direction)
        {
            string name;
            switch (direction)
            {
                case SortingDirection.Asc:
                    name = nameof(Queryable.OrderBy);
                    break;
                case SortingDirection.Desc:
                    name = nameof(Queryable.OrderByDescending);
                    break;
                default: throw new NotSupportedException(direction.ToString());
            }
            return _methodTwoGenericsDictionary.GetOrAdd((name, typeSrc, typeResult, 2), MethodFactory);
        }
        private static MethodInfo GetThen<T>(Type typeResult, SortingDirection direction) => GetThen(typeof(T), typeResult, direction);

        private static MethodInfo GetThen(Type typeSrc, Type typeResult, SortingDirection direction)
        {
            string name;
            switch (direction)
            {
                case SortingDirection.Asc:
                    name = nameof(Queryable.ThenBy);
                    break;
                case SortingDirection.Desc:
                    name = nameof(Queryable.ThenByDescending);
                    break;
                default: throw new NotSupportedException(direction.ToString());
            }
            return _methodTwoGenericsDictionary.GetOrAdd((name, typeSrc, typeResult, 2), MethodFactory);
        }
        /// <summary>
        /// Sort <see cref="IQueryable{T}"/> collection of <typeparamref name="T"/> by expressions selecected in <paramref name="lambda"/>
        /// <code lang="csharp">_repository.GetPeople().OrderBy((lambda1, direction1), (lambda2, direction2));</code>
        /// </summary>
        /// <remarks>
        /// <para>Method use a <see cref="ValueTypedKeyCashe{TKey, TValue}"/> for cashe a LINQ's methods</para>
        /// </remarks>
        /// <example>
        /// <code lang="csharp">
        /// var parameter = Expression.Parameter(typeof(Person));
        /// var sortedPropertyName1 = Console.ReadLine();
        /// var sortedProperty1 = Expression.PropertyOrField(parameter, sortedPropertyName1);
        /// var lambda1 = Expression.Lambda(sortedProperty1, parameter);
        /// var direction1 = Console.ReadLine() == "desc" ? SortingDirection.Desc : SortingDirection.Asc;
        /// var sortedPropertyName2 = Console.ReadLine();
        /// var sortedProperty2 = Expression.PropertyOrField(parameter, sortedPropertyName2);
        /// var lambda2 = Expression.Lambda(sortedProperty2, parameter);
        /// var direction2 = Console.ReadLine() == "desc" ? SortingDirection.Desc : SortingDirection.Asc;
        /// _repository.GetPeople().OrderBy((lambda1, direction1), (lambda2, direction2));
        /// </code>
        /// </example>
        /// <typeparam name="T"></typeparam>
        /// <param name="src"></param>
        /// <param name="lambda">property selector and sorting direcrion tuples</param>
        /// <returns></returns>
        public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> src, params (LambdaExpression Selector, SortingDirection Direction)[] lambda)
        {
            var l = lambda[0];
            var s = src.OrderBy(l.Selector, l.Direction);
            for (int i = 1; i < lambda.Length; i++)
            {
                l = lambda[i];
                s = s.ThenBy(l.Selector, l.Direction);
            }
            return s;
        }
        /// <summary>
        /// Sort <see cref="IQueryable"/> collection of <paramref name="type"/> by expressions selecected in <paramref name="lambda"/>
        /// <code lang="csharp">_repository.GetData(type).OrderBy(type, (lambda1, direction1), (lambda2, direction2));</code>
        /// </summary>
        /// <remarks>
        /// <para>Method use a <see cref="ValueTypedKeyCashe{TKey, TValue}"/> for cashe a LINQ's methods</para>
        /// </remarks>
        /// <example>
        /// <code lang="csharp">
        /// var typeName = Console.ReadLine();
        /// var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(typeName)).First(t => !(t is null));
        /// var parameter = Expression.Parameter(type);
        /// var sortedPropertyName1 = Console.ReadLine();
        /// var sortedProperty1 = Expression.PropertyOrField(parameter, sortedPropertyName1);
        /// var lambda1 = Expression.Lambda(sortedProperty1, parameter);
        /// var direction1 = Console.ReadLine() == "desc" ? SortingDirection.Desc : SortingDirection.Asc;
        /// var sortedPropertyName2 = Console.ReadLine();
        /// var sortedProperty2 = Expression.PropertyOrField(parameter, sortedPropertyName2);
        /// var lambda2 = Expression.Lambda(sortedProperty2, parameter);
        /// var direction2 = Console.ReadLine() == "desc" ? SortingDirection.Desc : SortingDirection.Asc;
        /// _repository.GetData(type).OrderBy(type, (lambda1, direction1), (lambda2, direction2));
        /// </code>
        /// </example>        
        /// <param name="src"></param>
        /// <param name="type"></param>
        /// <param name="lambda"></param>
        /// <returns></returns>
        public static IOrderedQueryable OrderBy(this IQueryable src, Type type, params (LambdaExpression Selector, SortingDirection Direction)[] lambda)
        {
            var l = lambda[0];
            var s = src.OrderBy(type, l.Selector, l.Direction);
            for (int i = 1; i < lambda.Length; i++)
            {
                l = lambda[i];
                s = s.ThenBy(type, l.Selector, l.Direction);
            }
            return s;
        }
        /// <summary>
        /// Sort <see cref="IQueryable{T}"/> collection of <typeparamref name="T"/> by expressions selecected in <paramref name="lambda"/>
        /// </summary>
        /// <remarks>
        /// <para>Method use a <see cref="ValueTypedKeyCashe{TKey, TValue}"/> for cashe a LINQ's methods</para>
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="src"></param>
        /// <param name="lambda">property selector and sorting direcrion tuples</param>
        /// <returns></returns>
        public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> src, IEnumerable<(LambdaExpression Selector, SortingDirection Direction)> lambda)
        {
            return OrderBy(src, lambda.ToArray());
        }
        /// <summary>
        /// Sort <see cref="IQueryable"/> collection of <paramref name="type"/> by expressions selecected in <paramref name="lambda"/>
        /// </summary>
        /// <remarks>
        /// <para>Method use a <see cref="ValueTypedKeyCashe{TKey, TValue}"/> for cashe a LINQ's methods</para>
        /// </remarks>
        /// <param name="src"></param>
        /// <param name="type"></param>
        /// <param name="lambda"></param>
        /// <returns></returns>
        public static IOrderedQueryable OrderBy(this IQueryable src, Type type, IEnumerable<(LambdaExpression Selector, SortingDirection Direction)> lambda)
        {
            return OrderBy(src, type, lambda.ToArray());
        }
        /// <summary>
        /// Subsort <see cref="IQueryable{T}"/> collection of <typeparamref name="T"/> by expression selecected in <paramref name="lambda"/>
        /// <code lang="csharp">_repository.GetOrderedPeople().ThenBy(lambda, direction);</code>
        /// </summary>
        /// <remarks>
        /// <para>Method use a <see cref="ValueTypedKeyCashe{TKey, TValue}"/> for cashe a LINQ's methods</para>
        /// </remarks>
        /// <example>
        /// <code lang="csharp">
        /// var parameter = Expression.Parameter(typeof(Person));
        /// var sortedPropertyName = Console.ReadLine();
        /// var sortedProperty = Expression.PropertyOrField(parameter, sortedPropertyName);
        /// var lambda = Expression.Lambda(sortedProperty, parameter);
        /// _repository.GetOrderedPeople().ThenBy(lambda, SortingDirection.Asc);
        /// </code>
        /// </example>
        /// <typeparam name="T"></typeparam>
        /// <param name="src"></param>
        /// <param name="lambda"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static IOrderedQueryable<T> ThenBy<T>(this IOrderedQueryable<T> src, LambdaExpression lambda, SortingDirection direction)
        {
            var typeResult = lambda.ReturnType;
            var thenBy = GetThen<T>(typeResult, direction);
            return (IOrderedQueryable<T>)thenBy.Invoke(null, new object[] { src, lambda });
        }
        /// <summary>
        /// Subsort <see cref="IQueryable"/> collection of <paramref name="type"/> by expression selecected in <paramref name="lambda"/>
        /// <code lang="csharp">_repository.GetOrderedData(type).ThenBy(type, lambda, direction);</code>
        /// </summary>
        /// <remarks>
        /// <para>Method use a <see cref="ValueTypedKeyCashe{TKey, TValue}"/> for cashe a LINQ's methods</para>
        /// </remarks>
        /// <example>
        /// <code lang="csharp">
        /// var typeName = Console.ReadLine();
        /// var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(typeName)).First(t => !(t is null));
        /// var parameter = Expression.Parameter(type);
        /// var sortedPropertyName = Console.ReadLine();
        /// var sortedProperty = Expression.PropertyOrField(parameter, sortedPropertyName);
        /// var lambda = Expression.Lambda(sortedProperty, parameter);
        /// _repository.GetOrdereddata(type).ThenBy(type, lambda, SortingDirection.Asc);
        /// </code>
        /// </example>       
        /// <param name="src"></param>
        /// <param name="type"></param>
        /// <param name="lambda"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static IOrderedQueryable ThenBy(this IOrderedQueryable src, Type type, LambdaExpression lambda, SortingDirection direction)
        {
            var typeResult = lambda.ReturnType;
            var thenBy = GetThen(type, typeResult, direction);
            return (IOrderedQueryable)thenBy.Invoke(null, new object[] { src, lambda });
        }
        /// <summary>
        /// Invoke <see cref="Queryable.Take"/> for <paramref name="src"/> with elements of <paramref name="type"/>
        /// </summary>
        /// <param name="src"></param>
        /// <param name="type"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static IQueryable Take(this IQueryable src, Type type, int count) => (IQueryable)Invoke(nameof(Queryable.Take), type, src, count);
        /// <summary>
        /// Invoke <see cref="Queryable.Skip"/> for <paramref name="src"/> with elements of <paramref name="type"/>
        /// </summary>
        /// <param name="src"></param>
        /// <param name="type"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static IQueryable Skip(this IQueryable src, Type type, int count) => (IQueryable)Invoke(nameof(Queryable.Skip), type, src, count);
        /// <summary>
        /// Invoke <see cref="Queryable.Where{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}})"/> for <paramref name="src"/> with elements of <paramref name="type"/> and <paramref name="predicate"/>
        /// </summary>
        /// <param name="src"></param>
        /// <param name="type"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static IQueryable Where(this IQueryable src, Type type, LambdaExpression predicate) => (IQueryable)Invoke(nameof(Queryable.Where), type, src, predicate);
        /// <summary>
        /// Invoke <see cref="Queryable.Count{TSource}(IQueryable{TSource})"/> for <paramref name="src"/> with elements of <paramref name="type"/>
        /// </summary>
        /// <param name="src"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static int Count(this IQueryable src, Type type) => (int)Invoke(nameof(Queryable.Count), type, src);
        //public static Task<int> CountAsync (this IQueryable src, Type type, CancellationToken token) => (Task<int>)InvokeData(nameof(QueryableExtensions.CountAsync), type, src, token);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static List<object> ToListExt(this IQueryable src)
        {
            try
            {
                var list = new List<object>();
                foreach (var item in src) list.Add(item);
                return list;
            }
            catch (Exception)
            {
                return null;
            }

        }
        private static object Invoke(string name, Type type, params object[] args)
        {
            var method = _methodGenericDictionary.GetOrAdd((name, type, args.Length), MethodFactory);
            return method.Invoke(null, args);
        }
        //private static object InvokeData(string name, Type type, params object[] args)
        //{
        //    var method = _methodDictionary.GetOrAdd((name, type, args.Length), MethodFactoryData);
        //    return method.Invoke(null, args);
        //}
        private static MethodInfo MethodFactory((string name, Type src, Type dst, int len) key)
        {
            return typeof(Queryable).GetMethods(BindingFlags.Static | BindingFlags.Public).First(m => m.Name == key.name && m.GetParameters().Length == key.len).MakeGenericMethod(new[] { key.src, key.dst });
        }
        private static MethodInfo MethodFactory((string name, Type generic, int len) key)
        {
            return typeof(Queryable).GetMethods(BindingFlags.Static | BindingFlags.Public).First(m => m.Name == key.name && m.GetParameters().Length == key.len).MakeGenericMethod(new[] { key.generic });
        }
        //private static MethodInfo MethodFactoryData((string name, Type generic, int len) key)
        //{
        //    return typeof(QueryableExtensions).GetMethods(BindingFlags.Static | BindingFlags.Public).First(m => m.Name == key.name && m.GetParameters().Length == key.len).MakeGenericMethod(new[] { key.generic });
        //}

        private static readonly ValueTypedKeyCashe<(string Name, Type Generic, int Len), MethodInfo> _methodGenericDictionary = new ValueTypedKeyCashe<(string Name, Type Generic, int Len), MethodInfo>();
        private static readonly ValueTypedKeyCashe<(string Name, Type Src, Type Dst, int Len), MethodInfo> _methodTwoGenericsDictionary = new ValueTypedKeyCashe<(string Name, Type Generic, Type Dst, int Len), MethodInfo>();

        private static readonly ValueTypedKeyCashe<(Type Type, string PropertyName), Func<object, object>> Getters = new ValueTypedKeyCashe<(Type Type, string PropertyName), Func<object, object>>();
        public static object GetPropertyValue(object instance, string name)
        {
            var key = (instance.GetType(), name);
            Func<object, object> GetProperty((Type Type, string PropertyName) k)
            {
                var o = Expression.Parameter(typeof(object));
                var i = Expression.Convert(o, k.Type);
                var property = Expression.Property(i, k.PropertyName);
                var result = Expression.Convert(property, typeof(object));
                var lambda = Expression.Lambda<Func<object, object>>(result, o);
                return lambda.Compile();
            }
            var getter = Getters.GetOrAdd(key, GetProperty);
            return getter(instance);
        }
        private static readonly ValueTypedKeyCashe<(Type Type, string PropertyName, Type ValueType), Delegate> Setters = new ValueTypedKeyCashe<(Type Type, string PropertyName, Type ValueType), Delegate>();
        public static void SetPropertyValue<TInstance, TProperty>(TInstance instance, string name, TProperty value)
        {
            var key = (instance.GetType(), name, typeof(TProperty));
            Action<TInstance, TProperty> SetProperty((Type Type, string PropertyName, Type ValueType) k)
            {
                var valueParameter = Expression.Parameter(typeof(TProperty));
                var instanceParameter = Expression.Parameter(typeof(TInstance));
                var i = Expression.Convert(instanceParameter, k.Type);
                var property = Expression.Property(i, k.PropertyName);
                var v = Expression.Convert(valueParameter, property.Type);
                var result = Expression.Assign(property, v);
                var lambda = Expression.Lambda<Action<TInstance, TProperty>>(result, instanceParameter, valueParameter);
                return lambda.Compile();
            }
            var setter = (Action<TInstance, TProperty>)Setters.GetOrAdd(key, SetProperty);
            setter(instance, value);
        }

        public static IEnumerable<TDst> Map<TSrc, TDst>(this IEnumerable<TSrc> src) where TDst : new() => src.Select(i => i.Map<TSrc, TDst>());
        public static object ActivateInstance(Type type)
        {
            Delegate Activate(Type t)
            {
                var ctor = Expression.New(t);
                var lambda = Expression.Lambda(ctor);
                return lambda.Compile();
            }
            var activator = ActivateDelegates.GetOrAdd(type, Activate);
            return activator.DynamicInvoke();
        }
        
    }
}
