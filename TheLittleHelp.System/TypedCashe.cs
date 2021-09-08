using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace TheLittleHelp.System
{
    public class TypedKeyCashe<TKey, TValue>
        where TKey : class
    {
        private readonly IMemoryCache _cache;
        public TypedKeyCashe() => _cache = new MemoryCache(new MemoryCacheOptions { });
        public TypedKeyCashe(IMemoryCache cache) => _cache = cache;

        public MemoryCacheEntryOptions DefaultOption = new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromSeconds(30)/*, Size = 1024*/ };

        public TValue GetOrAdd(TKey key, Func<TValue> factory) => _cache.TryGetValue<TValue>(key, out var result) ? result : _cache.Set(key, factory(), DefaultOption);
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory) => _cache.TryGetValue<TValue>(key, out var result) ? result : _cache.Set(key, factory(key), DefaultOption);

        internal bool ContainsKey(TKey key) => _cache.TryGetValue(key, out _);

        internal bool TryGet(TKey key, out TValue result) => _cache.TryGetValue(key, out result);

        internal bool TryAdd(TKey key, TValue result)
        {
            if (ContainsKey(key)) return false;
            _ = _cache.Set(key, result);
            return true;
        }
    }
    public class ValueTypedKeyCashe<TKey, TValue>
        where TKey : struct
    {
        private readonly ConcurrentDictionary<TKey, object> _keyProvider;
        private readonly IMemoryCache _cache;
        public ValueTypedKeyCashe() : this(new MemoryCache(new MemoryCacheOptions { })) { }
        public ValueTypedKeyCashe(IMemoryCache cache)
        {
            _keyProvider = new ConcurrentDictionary<TKey, object>();
            DefaultOption = new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromSeconds(30)/*, Size = 1024*/ }.RegisterPostEvictionCallback(PostEviction);
            _cache = cache;
        }


        private void PostEviction(object key, object value, EvictionReason reason, object state) => _keyProvider.TryRemove((TKey)key, out _);
        public readonly MemoryCacheEntryOptions DefaultOption;
        private object AddKey(TKey key)
        {
            var objectKey = (object)key;//new object(); objectKey нужен будет только как ссылка. Анбоксинг будет сделан только один раз при удалении
            return _keyProvider.TryAdd(key, objectKey) ? objectKey : throw new Exception("hm...");
        }
        public TValue GetOrAdd(TKey key, Func<TValue> factory) => _keyProvider.TryGetValue(key, out var objectKey) && _cache.TryGetValue<TValue>(objectKey, out var result) ? result : _cache.Set(AddKey(key), factory(), DefaultOption);
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory) => _keyProvider.TryGetValue(key, out var objectKey) && _cache.TryGetValue<TValue>(objectKey, out var result) ? result : _cache.Set(AddKey(key), factory(key), DefaultOption);

        internal bool ContainsKey(TKey key) => _keyProvider.ContainsKey(key) && _cache.TryGetValue(_keyProvider[key], out _);

        internal bool TryGet(TKey key, out TValue result)
        {
            result = default;
            return _keyProvider.TryGetValue(key, out var objectKey) && _cache.TryGetValue(objectKey, out result);
        }

        internal bool TryAdd(TKey key, TValue result)
        {
            if (ContainsKey(key)) return false;
            _ = _cache.Set(AddKey(key), result);
            return true;
        }
    }
}
