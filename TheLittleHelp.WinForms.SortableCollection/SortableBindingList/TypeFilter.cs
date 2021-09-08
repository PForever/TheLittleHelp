using System;
using System.Collections.Generic;

namespace TheLittleHelp.WinForms.SortableCollection.SortableBindingList
{
    /// <summary>
    /// Class of property predicate expression
    /// </summary>
    public class TypeFilter
    {
        /// <summary>
        /// Text filter expression
        /// </summary>
        public string Filter { get; set; }
        /// <summary>
        /// Dictionary of inner property's filters
        /// </summary>
        public IDictionary<string, TypeFilter> PropertyFilters { get; set; }
        /// <summary>
        /// Predicate filter expression
        /// </summary>
        public Delegate Predicate { get; set; }
        public TypeFilter(string filter)
        {
            Filter = filter;
        }
        public TypeFilter(IDictionary<string, TypeFilter> propertyFilters)
        {
            PropertyFilters = propertyFilters;
        }
        public TypeFilter(Delegate predicate)
        {
            Predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        }
        public static TypeFilter Create<T>(Func<T, bool> predicate) => new TypeFilter(predicate);
    }
}
