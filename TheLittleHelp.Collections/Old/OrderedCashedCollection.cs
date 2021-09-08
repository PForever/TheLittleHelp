using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace TheLittleHelp.Collections.Old
{
    /// <summary>
    /// Класс служит исключительно для информирования винформы о том, что наша коллекция поддерживает сортировку
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public class OrderedCashedCollection<TValue> : List<TValue>, IBindingList
    {
        public bool AllowNew => false;
        public bool AllowEdit => false;
        public bool AllowRemove => false;
        public bool SupportsChangeNotification => false;
        public bool SupportsSearching => false;
        public bool SupportsSorting => true;
        public bool IsSorted { get; private set; }
        public TValue DefaultElement { get; }
        private readonly Action<Exception> _onError;

        public PropertyDescriptor SortProperty { get; private set; }
        public ListSortDirection SortDirection { get; private set; }

        public event ListChangedEventHandler ListChanged;

        public void AddIndex(PropertyDescriptor property)
        {
            throw new NotImplementedException();
        }
        public OrderedCashedCollection(TValue value, int count, Action<Exception> onError = null) : base(Enumerable.Repeat(value, count))
        {
            DefaultElement = value;
            _onError = onError;
        }
        public void Reset(int count)
        {
            Clear();
            AddRange(Enumerable.Repeat(DefaultElement, count));
        }
        public object AddNew()
        {
            throw new NotImplementedException();
        }
        public event Func<SortEventArgs, Task> SortAppledAsync;

        public async void ApplySort(PropertyDescriptor property, ListSortDirection direction)
        {
            SortProperty = property;
            SortDirection = direction;
            IsSorted = true;
            var hendler = SortAppledAsync;
            if (hendler == null) return;
            try
            {
                await hendler(new SortEventArgs((property.Name, direction)));
            }
            catch (Exception e)
            {
                if (_onError != null) _onError(e);
                else throw;
            }
        }

        public int Find(PropertyDescriptor property, object key)
        {
            throw new NotImplementedException();
        }

        public void RemoveIndex(PropertyDescriptor property)
        {
            throw new NotImplementedException();
        }

        public async void RemoveSort()
        {
            IsSorted = false;
            var hendler = SortAppledAsync;
            if (hendler == null) return;
            try
            {
                await hendler(new SortEventArgs((null, ListSortDirection.Ascending)));
            }
            catch (Exception e)
            {
                if (_onError != null) _onError(e);
                else throw;
            }
        }
    }
}
