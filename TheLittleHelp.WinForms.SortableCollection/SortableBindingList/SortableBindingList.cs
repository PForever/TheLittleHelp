using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Linq.Dynamic;
using DynamicExpression = System.Linq.Dynamic.DynamicExpression;
using TheLittleHelp.WinForms.SortableCollection.FilterHelp;

namespace TheLittleHelp.WinForms.SortableCollection.SortableBindingList
{
    /// <summary>
    /// Collection, that supports sorting, filtering and notification
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SortableBindingList<T> : IList<T>, IReadOnlyList<T>, ICancelAddNew, IRaiseItemChangedEvents, IBindingListView, IDynamicFiltrable, IDisposable
    {
        private BindingList<T> _innerList = new BindingList<T>();

        private void InnerListChanged(object _, ListChangedEventArgs e) => OnListChanged(e);
        private void InnerAddingNew(object _, AddingNewEventArgs e) => OnAddingNew(e.NewObject);

        private BindingList<T> InnerList
        {
            get => _innerList;
            set
            {
                ClearEvents();
                _innerList = value;

                if (RaisesItemChangedEvents)
                {
                    foreach (T item in value)
                        InsertRaiseItemChangedEvents(item);
                }
                _innerList.AddingNew += InnerAddingNew;
                _innerList.ListChanged += InnerListChanged;
                OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
            }
        }

        private void ClearEvents()
        {
            if (RaisesItemChangedEvents)
            {
                foreach (T item in InnerList)
                    RemoveRaiseItemChangedEvents(item);
            }
            _innerList.ListChanged -= InnerListChanged;
            _innerList.AddingNew -= InnerAddingNew;
        }

        void ResetInnerList() => InnerList = _innerList;


        private readonly IList<T> _defaultItems;
        private IList<T> DefaultItems => _defaultItems.ToList();
        public Func<T> DefaultItem { get; set; } = () => CreateNewItem();

        //protected bool SupportsSortingCore { get; } = true;

        //protected void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
        //{
        //    ApplySortCore(prop, direction);
        //}

        public SortableBindingList() : this(new List<T>())
        {
        }

        public SortableBindingList(IList<T> items)
        {
            _defaultItems = items;
            InnerList = new BindingList<T>(DefaultItems);
        }

        #region Sort

        public virtual bool IsSorted { get; protected set; }

        public virtual PropertyDescriptor SortProperty { get; protected set; }

        public virtual ListSortDirection SortDirection { get; protected set; }

        public virtual void ApplySort(PropertyDescriptor property, ListSortDirection direction)
        {
            SortProperty = property ?? throw new ArgumentNullException(nameof(property));
            ApplySortPrivate(property, direction);
            ResetInnerList();
        }
        private static bool IsComparable(Type type) => typeof(IComparable).IsAssignableFrom(type)
                ? true
                : type.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IComparable<>) && i.GenericTypeArguments[0].IsAssignableFrom(type));
        private void ApplySortPrivate(PropertyDescriptor property, ListSortDirection direction)
        {
            if (!IsComparable(property.PropertyType))
            {
                RemoveSort();
                return;
            }
            IsSorted = true;
            SortDirection = direction;
            var items = _innerList
                .OrderBy(direction == ListSortDirection.Ascending ? property.Name : $"{property.Name} DESC").ToList();
            _innerList = new BindingList<T>(items);
        }

        public virtual void RemoveSort()
        {
            IsSorted = false;
            SortDirection = default;
            InnerList = new BindingList<T>(DefaultItems);
        }

        public void ResetSort()
        {
            if (IsSorted) ApplySort(SortProperty, SortDirection);
        }

        private void ResetSortPrivate()
        {
            ApplySortPrivate(SortProperty, SortDirection);
        }

        public void ApplySort(ListSortDescriptionCollection sorts)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Find

        public int Find(PropertyDescriptor property, object key)
        {
            return ((IBindingList)InnerList).Find(property, key);
        }

        #endregion

        #region Filter

        public virtual bool IsFiltered { get; protected set; }

        public void AddIndex(PropertyDescriptor property)
        {
            ((IBindingList)InnerList).AddIndex(property);
        }

        public void RemoveIndex(PropertyDescriptor property)
        {
            ((IBindingList)InnerList).RemoveIndex(property);
        }

        public void ResetFilter()
        {
            if (IsFiltered) ResetFilterPrivate();
            ResetInnerList();
        }
        private void ResetFilterPrivate()
        {
            _innerList = new BindingList<T>(DefaultItems);
            ApplyFilter(FilterValidation);
            if (IsSorted) ResetSortPrivate();
        }

        //public void ApplyFilterPrivate(params (string propertyName, string pattern)[] values)
        //{
        //    string filter = PropertiesFilter.GetFilterExpression(values);
        //    ApplyFilterPrivate(filter);
        //}

        //public void ApplyFilter(params (string propertyName, string pattern)[] values)
        //{
        //    if (IsFiltered) RemoveFilterPrivate();
        //    ApplyFilterPrivate(values);
        //    ResetInnerList();
        //}

        public void ApplyFilterPrivate(string filter)
        {
            _filterString = filter;
            var filterValidation = DynamicExpression.ParseLambda<T, bool>(filter, FilterExpression).Compile();
            ApplyFilterPrivate(filterValidation);
        }
        public void ApplyFilter(string filter)
        {
            if (String.IsNullOrWhiteSpace(filter))
            {
                RemoveFilter();
                return;
            }
            _filterString = filter;
            if (IsFiltered) RemoveFilterPrivate();
            ApplyFilterPrivate(filter);
            ResetInnerList();
        }
        public void ApplyFilterPrivate(PropertiesFilter filter)
        {
            var propertyFilters = FilterHelper.FilterConverter(filter);
            if (propertyFilters == null) return;
            var validator = PropertiesFilter.CreateFilter<T>(propertyFilters);
            ApplyFilterPrivate(validator);
        }
        public void ApplyFilter(PropertiesFilter filter)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            if (IsFiltered) RemoveFilterPrivate();
            ApplyFilterPrivate(filter);
            ResetInnerList();
        }
        public void ApplyFilterPrivate(Func<T, bool> filter)
        {
            FilterValidation = filter;
            ApplyFilterPrivate(_innerList.Where(filter).ToList());
        }
        public void ApplyFilter(Func<T, bool> filter)
        {
            _filter = filter ?? throw new ArgumentNullException(nameof(filter));
            if (IsFiltered) RemoveFilterPrivate();
            ApplyFilterPrivate(filter);
            ResetInnerList();
        }
        private void ApplyFilterPrivate(IList<T> newInnerList)
        {
            IsFiltered = true;
            var items = newInnerList;
            _innerList = new BindingList<T>(items);
        }
        [Obsolete("перенести в использование на стороне фильтра")]
        private static Expression<Func<string, string, bool>> FilterExpression { get; } = (value, pattern) => !String.IsNullOrEmpty(value) && Regex.IsMatch(value, pattern, RegexOptions.IgnoreCase);

        private void RemoveFilterPrivate()
        {
            _filter = null;
            _filterString = null;
            FilterValidation = null;
            IsFiltered = false;
            _innerList = new BindingList<T>(DefaultItems);
            if (IsSorted) ResetSortPrivate();
        }

        public void RemoveFilter()
        {
            RemoveFilterPrivate();
            ResetInnerList();
        }

        private Func<T, bool> _filter;

        public Func<T, bool> Filter
        {
            get => _filter;
            set => _filter = value;
        }

        private string _filterString;

        string IBindingListView.Filter
        {
            get => _filterString;
            set
            {
                if (!String.IsNullOrEmpty(value))
                    ApplyFilter(value);
                else RemoveFilter();
            }
        }

        public virtual ListSortDescriptionCollection SortDescriptions { get; protected set; }

        #endregion

        #region Insert

        protected virtual int InsertToInner(T item)
        {
            return InsertToInner(item, _innerList.Count);
        }

        private int GetFilteredIndex(T item, int index)
        {
            if (IsFiltered && !FilterValidation(item)) return -1;
            return index;
        }
        private void InsertToSorted(T item, ref int index)
        {
            //TODO оптимизация
            _innerList.Insert(index, item);
            ResetSort();
            index = _innerList.IndexOf(item);
        }
        private void SetToSorted(T item, ref int index)
        {
            //TODO оптимизация
            _innerList[index] = item;
            ResetSort();
            index = _innerList.IndexOf(item);
        }

        protected virtual int InsertToInner(T item, int index)
        {
            if (RaisesItemChangedEvents) InsertRaiseItemChangedEvents(item);
            index = GetFilteredIndex(item, index);
            if (index == -1) return index;
            if (IsSorted) InsertToSorted(item, ref index);
            else _innerList.Insert(index, item);
            return index;
        }


        protected virtual int SetToInner(T item, int index)
        {
            if (RaisesItemChangedEvents)
            {
                RemoveRaiseItemChangedEvents(_innerList[index]);
                InsertRaiseItemChangedEvents(item);
            }
            index = GetFilteredIndex(item, index);
            if (index == -1) return index;
            if (IsSorted) SetToSorted(item, ref index);
            else _innerList[index] = item;
            return index;
        }

        public void Add(T item)
        {
            int index = _innerList.Count;
            if (!OnItemAdding(ref item, ref index)) return;
            InsertToInner(item);
        }

        public int Add(object value)
        {
            T item = (T)value;
            int index = _innerList.Count;
            if (!OnItemAdding(ref item, ref index)) return -1;
            return InsertToInner(item);
        }

        public void Insert(int index, object value)
        {
            T item = (T)value;
            if (!OnItemAdding(ref item, ref index)) return;
            InsertToInner(item, index);
        }

        public void Insert(int index, T item)
        {
            if (!OnItemAdding(ref item, ref index)) return;
            InsertToInner(item, index);
        }

        private T _newItem;
        private int _newItemIndex = -1;
        public virtual bool IsNewAdding { get; protected set; }

        public object AddNew()
        {
            if (IsNewAdding) EndNew(_newItemIndex);
            IsNewAdding = true;
            _newItemIndex = _innerList.Count;
            return _newItem = _innerList.AddNew();
        }

        public void CancelNew(int itemIndex)
        {
            if (!IsNewAdding) return;
            IsNewAdding = false;
            _innerList.CancelNew(_newItemIndex);
            _newItem = default;
            _newItemIndex = -1;
        }

        public void EndNew(int itemIndex)
        {
            if (!IsNewAdding || itemIndex != _newItemIndex) return;

            int index = _innerList.IndexOf(_newItem);
            IsNewAdding = false;
            _innerList.CancelNew(index);

            Add(_newItem);
            _newItem = default;
            _newItemIndex = -1;
        }

        object IList.this[int index]
        {
            get => ((IList)InnerList)[index];
            set
            {
                T item = (T)value;
                if (!OnItemSetting(ref item, ref index)) return;
                SetToInner(item, index);
            }
        }

        public T this[int index]
        {
            get => InnerList[index];
            set
            {
                if (!OnItemSetting(ref value, ref index)) return;
                SetToInner(value, index);
            }
        }

        #endregion

        #region IRaiseItemChangedEvents

        private void InsertRaiseItemChangedEvents(T item)
        {
            var properties = TypeDescriptor.GetProperties(item);
            foreach (PropertyDescriptor propertyDescriptor in properties)
            {
                if (!propertyDescriptor.SupportsChangeEvents) propertyDescriptor.AddValueChanged(item, OnItemChanged);
            }
        }

        private void RemoveRaiseItemChangedEvents(T item)
        {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(item);
            foreach (PropertyDescriptor propertyDescriptor in properties)
            {
                if (propertyDescriptor.SupportsChangeEvents) propertyDescriptor.RemoveValueChanged(item, OnItemChanged);
            }
        }

        private bool _raisesItemChangedEvents = true;

        public bool RaisesItemChangedEvents
        {
            get => _raisesItemChangedEvents;
            set
            {
                if (value == _raisesItemChangedEvents) return;
                if (value)
                {
                    foreach (T item in InnerList)
                    {
                        InsertRaiseItemChangedEvents(item);
                    }
                }
                else
                {
                    foreach (T item in InnerList)
                    {
                        RemoveRaiseItemChangedEvents(item);
                    }
                }

                _raisesItemChangedEvents = value;
            }
        }

        protected virtual void OnItemChanged(object sender, EventArgs args)
        {
            if (_innerList == null) return;
            int index = _innerList.IndexOf((T)sender);
            OnListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, index));
        }

        #endregion

        #region Remove

        public void Remove(object value)
        {
            T item = (T)value;
            Remove(item);
        }

        public void RemoveAt(int index)
        {
            T item = _innerList[index];
            Remove(item);
        }

        public void Clear()
        {
            if (!OnListClear()) return;
            if (RaisesItemChangedEvents)
            {
                foreach (T item in InnerList) RemoveRaiseItemChangedEvents(item);
            }

            InnerList.Clear();
        }

        public bool Remove(T item)
        {
            if (!OnItemDeleting(ref item)) return false;
            if (RaisesItemChangedEvents) RemoveRaiseItemChangedEvents(item);
            return InnerList.Remove(item);
        }

        #endregion

        #region Collection

        public IEnumerator<T> GetEnumerator()
        {
            return InnerList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)InnerList).GetEnumerator();
        }

        public bool Contains(object value)
        {
            return ((IList)InnerList).Contains(value);
        }

        public int IndexOf(object value)
        {
            return ((IList)InnerList).IndexOf(value);
        }

        public bool Contains(T item)
        {
            return InnerList.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            InnerList.CopyTo(array, arrayIndex);
        }

        public void CopyTo(Array array, int index)
        {
            ((ICollection)InnerList).CopyTo(array, index);
        }

        int ICollection.Count => InnerList.Count;

        public object SyncRoot => ((ICollection)InnerList).SyncRoot;

        public bool IsSynchronized => ((ICollection)InnerList).IsSynchronized;

        int ICollection<T>.Count => InnerList.Count;

        bool ICollection<T>.IsReadOnly => ((ICollection<T>)InnerList).IsReadOnly;

        public int IndexOf(T item)
        {
            return InnerList.IndexOf(item);
        }

        int IReadOnlyCollection<T>.Count => InnerList.Count;

        #endregion

        #region Accessability

        public bool AllowNew => ((IBindingList)InnerList).AllowNew;
        public bool AllowEdit => ((IBindingList)InnerList).AllowEdit;
        public bool AllowRemove => ((IBindingList)InnerList).AllowRemove;
        public bool SupportsChangeNotification => ((IBindingList)InnerList).SupportsChangeNotification;
        public bool SupportsSearching => ((IBindingList)InnerList).SupportsSearching;
        public event AddingNewEventHandler AddingNew;
        private readonly Func<T, bool> _defaultValidation = v => true;
        public virtual Func<T, bool> Validation { get; set; } = v => true;
        public virtual Func<T, bool> FilterValidation { get; protected set; }
        bool IList.IsReadOnly => ((IList)InnerList).IsReadOnly;
        public bool IsFixedSize => ((IList)InnerList).IsFixedSize;
        public virtual bool SupportsSorting => true;
        public bool SupportsAdvancedSorting { get; }
        public bool SupportsFiltering => true;

        #endregion

        #region Events

        [Obsolete]
        private static T CreateNewItem()
        {
            try
            {
                return Activator.CreateInstance<T>();
            }
            catch (MissingMemberException e)
            {
                return default;
            }

            //return Expression.New(typeof(T).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance).First(c => c.GetParameters().Length == 0))
        }

        protected virtual void OnAddingNew(object item)
        {
            var e = new AddingNewEventArgs(item);
            OnAddingNew(e);
            if (item == null) item = DefaultItem();
        }

        protected virtual void OnAddingNew(AddingNewEventArgs e) => AddingNew?.Invoke(this, e);

        protected virtual bool OnItemSetting(ref T item, ref int index)
        {
            if (!Validation(item)) return false;
            var e = new ListChangingEventArgs<T>(ListChangedType.ItemChanged, item, index);
            OnListChanging(e);
            if (e.Cancel) return false;
            index = e.Index1;
            item = e.Value1;
            if (IsSorted || IsFiltered) _defaultItems[_defaultItems.IndexOf(item)] = item;
            else _defaultItems[index] = item;
            return true;
        }

        protected virtual bool OnItemAdding(ref T item, ref int index)
        {
            if (!Validation(item)) return false;
            var e = new ListChangingEventArgs<T>(ListChangedType.ItemAdded, item, index);
            OnListChanging(e);
            if (e.Cancel) return false;
            index = e.Index1;
            item = e.Value1;
            if (IsSorted || IsFiltered) _defaultItems.Add(item);
            else _defaultItems.Insert(index, item);
            return true;
        }

        protected virtual bool OnListClear()
        {
            var e = new ListChangingEventArgs<T>(ListChangedType.Reset);
            OnListChanging(e);
            if (e.Cancel) return false;
            _defaultItems.Clear();
            return true;
        }

        protected virtual bool OnItemDeleting(ref T item)
        {
            var e = new ListChangingEventArgs<T>(ListChangedType.ItemDeleted, item);
            OnListChanging(e);
            if (e.Cancel) return false;
            item = e.Value1;
            _defaultItems.Remove(item);
            return true;
        }

        protected bool OnItemsMoving(T item1, T item2, int index1, int index2)
        {
            if (IsSorted) return false;
            var e = new ListChangingEventArgs<T>(ListChangedType.ItemMoved, item1, index1, item2, index2);
            OnListChanging(e);
            if (e.Cancel) return false;
            index1 = e.Index1;
            item1 = e.Value1;
            index2 = e.Index2;
            item2 = e.Value2;
            if (IsFiltered)
            {
                index1 = _defaultItems.IndexOf(item1);
                index2 = _defaultItems.IndexOf(item2);
            }

            _defaultItems[index1] = item2;
            _defaultItems[index2] = item1;
            return true;
        }

        protected virtual void OnListChanging(ListChangingEventArgs<T> e)
        {
            ListChanging?.Invoke(this, e);
        }

        public event ListChangingEventHandler<T> ListChanging;

        protected virtual void OnListChanged(ListChangedEventArgs e)
        {
            if (e.ListChangedType == ListChangedType.ItemChanged)
            {
            }

            ListChanged?.Invoke(this, e);
        }

        public void Dispose()
        {
            ClearEvents();
            _innerList.Clear();
            _innerList = null;
        }

        public event ListChangedEventHandler ListChanged;

        #endregion
    }
}
