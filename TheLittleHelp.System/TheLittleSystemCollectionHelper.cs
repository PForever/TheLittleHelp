using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using TheLittleHelp.System.DynamicHelp;

namespace TheLittleHelp.System.CollectionHelp
{
    public static class TheLittleSystemCollectionHelper
    {
        public static IEnumerable<T> ForEachTransform<T>(this IEnumerable<T> src, Action<T> transformer)
            where T : class
        {
            foreach (var i in src)
            {
                transformer(i);
                yield return i;
            }
        }
        public static T Transform<T>(this T src, Action<T> transformer)
            where T : class
        {
            transformer(src);
            return src;
        }
        public static Dest Convert<TSource, Dest>(this TSource src, Func<TSource, Dest> transformer)
            where TSource : class
        {
            return src == default ? default : transformer(src);
        }
        public static IEnumerable<Dest> ForEachConvert<TSource, Dest>(this IEnumerable<TSource> src, Func<TSource, Dest> transformer)
        {
            if (src == null) return null;
            IEnumerable<Dest> ForEachConvert(IEnumerable<TSource> s, Func<TSource, Dest> t)
            {
                foreach (var item in s) yield return t(item);
            }
            return ForEachConvert(src, transformer);
        }

        public static IEnumerable<T> Replace<T>(this IEnumerable<T> src, T oldValue, T newValue)
        {
            foreach (var item in src) yield return Equals(item, oldValue) ? newValue : item;
        }

        public static IDictionary<TKey, TValue> AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key, TValue value)
        {
            if (source.ContainsKey(key)) source[key] = value;
            else source.Add(key, value);
            return source;
        }
        public static T GetOrAdd<T>(this IList<T> source, int index, Func<T> factory)
        {
            if (source.Count <= index) for (int i = index - source.Count; i >= 0; i--) source.Add(factory());
            return source[index];
        }
        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key, Func<TValue> factory)
        {
            if (!source.ContainsKey(key))
            {
                var value = factory();
                source.Add(key, value);
                return value;
            }
            return source[key];
        }
        public static IEnumerable<T> StartWith<T>(this IEnumerable<T> src, T item)
        {
            yield return item;
            foreach (var i in src) yield return i;
        }

        public static IEnumerable<T> StartWithDefault<T>(this IEnumerable<T> src)
        {
            yield return default;
            foreach (var i in src) yield return i;
        }
        public static IEnumerable<T> Concat<T>(this IEnumerable<T> first, T second)
        {
            foreach (T item in first)
            {
                yield return item;
            }
            yield return second;
        }
        public static IEnumerable<T> Concat<T>(this T first, IEnumerable<T> second)
        {
            yield return first;
            foreach (T item in second)
            {
                yield return item;
            }
        }
        public static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> src, params (TKey Key, TValue Value)[] values)
        {
            foreach (var (key, value) in values)
            {
                src.Add(key, value);
            }
        }

        public static IEnumerable<T> Convert<T>(this IEnumerable src)
        {
            foreach (var o in src)
            {
                yield return (T)o;
            }
        }

        public static bool In<T>(this T node, params T[] values) => values.Contains(node);
        public static bool NotIn<T>(this T node, params T[] values) => !values.Contains(node);
        public static bool In<T>(this T node, IEnumerable<T> values) => values.Contains(node);
        public static bool NotIn<T>(this T node, IEnumerable<T> values) => !values.Contains(node);
        public static bool In<T>(this T node, ICollection<T> values) => values.Contains(node);
        public static bool NotIn<T>(this T node, ICollection<T> values) => !values.Contains(node);
        public static bool In<T>(this T node, IQueryable<T> values) => values.Contains(node);
        public static bool NotIn<T>(this T node, IQueryable<T> values) => !values.Contains(node);

        public static IEnumerable<T> NullubleSafeWhere<T>(this IEnumerable<T> src, Func<T, bool> predicate)
        {
            return predicate == null ? src : src.Where(predicate);
        }

        public static IList<T> With<T>(this IList<T> list, T item)
        {
            list.Add(item);
            return list;
        }
        public static IList<T> With<T>(this IList<T> list, T item, int position)
        {
            list.Insert(position, item);
            return list;
        }
        public static IList<T> With<T>(this IList<T> list, IEnumerable<T> items)
        {
            foreach (T item in items)
            {
                list.Add(item);
            }
            return list;
        }
        public static IList<T> With<T>(this IList<T> list, params T[] items)
        {
            foreach (T item in items)
            {
                list.Add(item);
            }
            return list;
        }
        public static IList<T> With<T>(this IList<T> list, params (T Item, bool IsNeed)[] items)
        {
            foreach (var (item, isNeed) in items)
            {
                if (!isNeed) continue;
                list.Add(item);
            }
            return list;
        }
        public static IEnumerable<T> Tramsform<T>(this IEnumerable<T> src, Action<T> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            IEnumerable<T> ForEach()
            {
                foreach (T item in src)
                {
                    action(item);
                    yield return item;
                }
            }
            return ForEach();
        }
        public static IDictionary<TValue, TKey> Copy<TValue, TKey>(this IDictionary<TValue, TKey> src) => new Dictionary<TValue, TKey>(src);
        public static void ForEach<T>(this IEnumerable<T> src, Action<T> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            foreach (var item in src)
                action(item);
        }
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> src) => new HashSet<T>(src);
        public static IEnumerable<T> Append<T>(this IEnumerable<T> src, T element)
        {
            foreach (var item in src) yield return item;
            yield return element;
        }

        public static IEnumerable<T> Transform<T>(this IEnumerable<T> src, Action<T> tramsform) where T : class
        {
            foreach (var element in src)
            {
                tramsform(element);
                yield return element;
            }
        }
        public static IEnumerable<T> Transform<T>(this IEnumerable<T> src, Func<T, T> tramsform) where T : struct
        {
            foreach (var element in src)
            {
                yield return tramsform(element);
            }
        }
        public static IEnumerable<TSrc> PriorityDistinct<TSrc, TProperty>(this IEnumerable<TSrc> src, Func<TSrc, TProperty> identitySelector, Func<TSrc, TSrc, TSrc> prioritySelector)
        {
            var dictionry = new Dictionary<TProperty, TSrc>();
            foreach (var item in src)
            {
                var key = identitySelector(item);
                if (dictionry.ContainsKey(key)) dictionry[key] = prioritySelector(dictionry[key], item);
                else dictionry.Add(key, item);
            }
            return dictionry.Values;
        }
        public static IEnumerable<TSrc> PriorityDistinct<TSrc>(this IEnumerable<TSrc> src, Func<TSrc, TSrc, TSrc> prioritySelector)
        {
            var dictionry = new Dictionary<TSrc, TSrc>();
            foreach (var item in src)
            {
                if (dictionry.ContainsKey(item)) dictionry[item] = prioritySelector(dictionry[item], item);
                else dictionry.Add(item, item);
            }
            return dictionry.Values;
        }


        public static IEnumerable<TDst> UpdateAndGetInsertList<TSrc, TDst, TKey>(this IDictionary<TKey, TSrc> src, IDictionary<TKey, TDst> dst, Func<TDst> create, Action<TDst> remove = null) where TDst : new()
            => UpdateAndGetInsertList(src, dst, (s, d) => TheLittleDynamicHelper.Map(s, d), create, remove);
        public static IEnumerable<TDst> UpdateAndGetInsertList<TSrc, TDst, TKey>(this IDictionary<TKey, TSrc> src, IDictionary<TKey, TDst> dst, Action<TDst> remove = null) where TDst : new()
            => UpdateAndGetInsertList(src, dst, (s, d) => TheLittleDynamicHelper.Map(s, d), TheLittleDynamicHelper.ActivateInstance<TDst>, remove);
        public static IEnumerable<TDst> UpdateAndGetInsertList<TSrc, TDst, TKey>(this IDictionary<TKey, TSrc> src, IDictionary<TKey, TDst> dst, Action<TSrc, TDst> copy, Action<TDst> remove = null) where TDst : new()
            => UpdateAndGetInsertList(src, dst, copy, TheLittleDynamicHelper.ActivateInstance<TDst>, remove);
        public static IEnumerable<TDst> UpdateAndGetInsertList<TSrc, TDst, TKey>(this IDictionary<TKey, TSrc> src, IDictionary<TKey, TDst> dst, Action<TSrc, TDst> copy, Func<TDst> create, Action<TDst> remove = null)
        {
            //чтобы не изменяться исходную коллекцию
            dst = dst.Copy();
            var inserted = src.Copy();
            foreach (var s in
                from s in src
                where dst.ContainsKey(s.Key)
                select s)
            {
                var k = s.Key;
                if (!dst.ContainsKey(k))
                    continue;
                //reset(dst[k]);
                copy(s.Value, dst[k]);
                dst.Remove(k);
                inserted.Remove(k);
            }
            if (remove != null)
                foreach (var p in dst.Values)
                    remove(p);
            foreach (var insertedValue in inserted.Values)
            {
                var item = create();
                copy(insertedValue, item);
                yield return item;
            }
        }
    }
}
