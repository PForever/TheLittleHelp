using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TheLittleHelp.Collections.Old
{
    public sealed class CashedCollection<TValue, TKey>
    {
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private class Node
        {
            public TKey Key { get; }
            public bool IsLoaded { get; private set; }
            public TValue Value { get; private set; }

            public Node(TKey key)
            {
                Key = key;
                IsLoaded = false;
                Value = default;
            }
            public void Set(TValue value)
            {
                if (IsLoaded) throw new ArgumentException("Value already loaded");
                IsLoaded = true;
                Value = value;
            }
        }

        private readonly List<Node> _innerList;
        private readonly Func<TValue, TKey> _keySelector;
        private readonly int _loadInterval;

        public int Count
        {
            get
            {
                if (_tokenSource.IsCancellationRequested) return 0;
                return _innerList.Count;
            }
        }

        public TValue FindByKey(TKey key)
        {
            var node = _innerList.FirstOrDefault(n => n.Key.Equals(key));
            return node == null ? default : node.Value;
        }
        public int PositionByKey(TKey key)
        {
            return _innerList.FindIndex(n => n.Key.Equals(key));
        }

        private readonly Func<IEnumerable<TKey>, Task<IEnumerable<TValue>>> _valueFactoryAsync;

        public CashedCollection(Func<TValue, TKey> keySelector, int loadInterval, Func<IEnumerable<TKey>, Task<IEnumerable<TValue>>> valueFactoryAsync, IEnumerable<TKey> keys)
        {
            _innerList = keys.Select(k => new Node(k)).ToList();
            _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
            _loadInterval = loadInterval;
            _valueFactoryAsync = valueFactoryAsync ?? throw new ArgumentNullException(nameof(valueFactoryAsync));
        }
        public async Task Clear()
        {
            if (_tokenSource.IsCancellationRequested) return;
            Task last;
            lock (_lockKey)
            {
                if (_tokenSource.IsCancellationRequested) return;
                last = _loadQueue;
                _tokenSource.Cancel();
            }
            await last;
            _innerList.Clear();
        }
        private async Task<Node> Load(TKey key, int index)
        {
            if (_tokenSource.IsCancellationRequested) return null;
            await LoadReangeAsync(GetInterval(index)).ConfigureAwait(false);
            return _innerList[index];
        }
        private (int from, int to) GetInterval(int index)
        {
            int count = Count;
            int next = _loadInterval;
            int i = 0;
            while (next < index)
            {
                i = next;
                next += _loadInterval;
            }
            return (i, next < count ? next : count - 1);
        }

        private Task _loadQueue = Task.Delay(0);

        private readonly object _lockKey = new object();

        public async Task<TValue> GetValueAsync(int index)
        {
            if (_tokenSource.IsCancellationRequested) return default;
            var item = _innerList[index];
            if (item.IsLoaded) return item.Value;
            Func<Task, Node> loading = t => Load(item.Key, index).Result;
            Task<Node> last;
            lock (_lockKey)
            {
                last = _loadQueue.ContinueWith(loading);
                _loadQueue = last;
            }
            try
            {
                var result = await last.ConfigureAwait(false);
                if (result == null) return default;
                return result.Value;
            }
            catch (TaskCanceledException) { return default; }
        }

        private async Task LoadReangeAsync((int from, int to) tuple)
        {
            if (tuple.to >= _innerList.Count) tuple.to = _innerList.Count - 1;
            var dictionary = new Dictionary<TKey, int>(tuple.to - tuple.from);
            for (var i = tuple.from; i <= tuple.to; i++)
            {
                var item = _innerList[i];
                if (item.IsLoaded) continue;
                dictionary.Add(item.Key, i);
            }
            if (dictionary.Count == 0) return;

            var values = await _valueFactoryAsync(dictionary.Keys).ConfigureAwait(false);
            if (_tokenSource.IsCancellationRequested) return;
            foreach (var value in values)
            {
                _innerList[dictionary[_keySelector(value)]].Set(value);
            }
        }
    }
}
