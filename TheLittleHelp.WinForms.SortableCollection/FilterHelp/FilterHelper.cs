using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TheLittleHelp.WinForms.SortableCollection.FilterHelp
{
    public static class FilterHelper
    {
        /// <summary>
        /// Create predicate from filter's data
        /// </summary>
        /// <typeparam name="T">Type of filtered source</typeparam>
        /// <param name="filter">|Filter's data</param>
        /// <returns>Filter's predicate</returns>
        public static Func<T, bool> CreateFilter<T>(PropertiesFilter filter)
        {
            var propertyFilters = FilterConverter(filter);
            if (propertyFilters == null) return null;
            var validator = PropertiesFilter.CreateFilter<T>(propertyFilters);
            return validator;
        }
        internal static TypeFilter FilterConverter(PropertiesFilter filters)
        {
            var result = new TypeFilter(new Dictionary<string, TypeFilter>(filters.Count));
            foreach (var filter in filters.Values.Where(f => f.Value != null))
            {
                switch (filter.Value)
                {
                    case PropertiesFilter innerFilter:
                        result.PropertyFilters.Add(filter.Key.Name, FilterConverter(innerFilter));
                        break;
                    case Delegate predecate:
                        result.PropertyFilters.Add(filter.Key.Name, new TypeFilter(predecate));
                        break;
                    default:
                        if (filter.SourceList != null) result.PropertyFilters.Add(filter.Key.Name, new TypeFilter(CreateStringFilter(filter.Value.ToString(), filter.SourceList)));
                        else result.PropertyFilters.Add(filter.Key.Name, new TypeFilter(CreateStringFilter(filter.Value.ToString())));
                        break;
                }
            }
            return result.PropertyFilters.Count > 0 ? result : null;
        }
        private static Func<string, bool> CreateStringFilter(string value)
        {
            Func<string, bool> StringFilter(string p) => s => !String.IsNullOrEmpty(s) && Regex.IsMatch(s, p, RegexOptions.IgnoreCase);
            string pattern = "";
            foreach (char c in value) pattern += $"[{c}]+.*";
            return StringFilter(pattern);
        }

        private static Func<object, bool> CreateStringFilter(string value, ILookup<string, object> sourceList)
        {
            var stringPredicate = CreateStringFilter(value);
            var list = sourceList.Where(l => stringPredicate(l.Key)).SelectMany(l => l).ToList();
            return src => list.Any(i => Equals(i, src) || i?.ToString() == src?.ToString());
        }
    }
}
