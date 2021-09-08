using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace TheLittleHelp.Collections.Old
{
    public class FilteredCashedCollection<TValue, TKey>
    {
        private CashedCollection<TValue, TKey> _cashedCollection;
        private (string, string) _filter;
        private (string, ListSortDirection) _order;
        private readonly Func<TValue, TKey> _keySelector;
        private readonly Func<IEnumerable<TKey>, Task<IEnumerable<TValue>>> _valueFactoryAsync;
        private Func<(string Property, string Pattern), (string Property, ListSortDirection Direction), CancellationToken, Task<IEnumerable<TKey>>> _keysSelectorAsync;
        private int _loadInterval;
        private CancellationTokenSource _tokenSource;

        public int Count => _cashedCollection.Count;
        public async Task<TValue> GetValueAsync(int index) => await _cashedCollection.GetValueAsync(index);
        protected FilteredCashedCollection(Func<TValue, TKey> keySelector, int loadInterval, Func<IEnumerable<TKey>, Task<IEnumerable<TValue>>> valueFactoryAsync, Func<(string Property, string Pattern), (string Property, ListSortDirection Direction), CancellationToken, Task<IEnumerable<TKey>>> keysSelectorAsync, CashedCollection<TValue, TKey> cashedCollection, CancellationTokenSource tokenSource)
        {
            _tokenSource = tokenSource ?? throw new ArgumentNullException(nameof(tokenSource));
            _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
            _loadInterval = loadInterval;
            _valueFactoryAsync = valueFactoryAsync ?? throw new ArgumentNullException(nameof(valueFactoryAsync));
            _keysSelectorAsync = keysSelectorAsync ?? throw new ArgumentNullException(nameof(keysSelectorAsync));
            _cashedCollection = cashedCollection ?? throw new ArgumentNullException(nameof(cashedCollection));
        }
        public static async Task<FilteredCashedCollection<TValue, TKey>> Create(Func<TValue, TKey> keySelector, int loadInterval, Func<IEnumerable<TKey>, Task<IEnumerable<TValue>>> valueFactoryAsync, Func<(string Property, string Pattern), (string Property, ListSortDirection Direction), CancellationToken, Task<IEnumerable<TKey>>> keysSelectorAsync)
        {
            if (keysSelectorAsync == null) throw new ArgumentNullException(nameof(keySelector));
            var tokenSource = new CancellationTokenSource();
            var keys = await keysSelectorAsync((null, null), (null, ListSortDirection.Ascending), tokenSource.Token);
            var inner = new CashedCollection<TValue, TKey>(keySelector, loadInterval, valueFactoryAsync, keys);
            return new FilteredCashedCollection<TValue, TKey>(keySelector, loadInterval, valueFactoryAsync, keysSelectorAsync, inner, tokenSource);

        }
        public (string Property, string Pattern) Filter { get => _filter; }
        public (string Property, ListSortDirection Direction) Order { get => _order; }
        public async Task SetFilter((string Property, string Value) filter)
        {
            _filter = filter;
            await ResetInnerList();
        }
        public async Task SetOrder((string, ListSortDirection) order)
        {
            _order = order;
            await ResetInnerList();
        }
        Task _last = Task.Delay(0);
        private async Task ResetInnerList()
        {
            _tokenSource.Cancel();
            await _last;
            _tokenSource = new CancellationTokenSource();
            await _cashedCollection.Clear();
            _last = _keysSelectorAsync(_filter, _order, _tokenSource.Token);
            var keys = await (Task<IEnumerable<TKey>>)_last;
            _cashedCollection = new CashedCollection<TValue, TKey>(_keySelector, _loadInterval, _valueFactoryAsync, keys);
        }

        public TValue FindByKey(TKey key) => _cashedCollection.FindByKey(key);
        public int PositionByKey(TKey key) => _cashedCollection.PositionByKey(key);
    }
}
