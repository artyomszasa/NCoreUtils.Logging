using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace NCoreUtils.Logging
{
    /// <summary>
    /// NUll-safe readonly ditionary wrapper.
    /// </summary>
    public struct ReadOnlyDictionaryWrapper<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
        where TKey : notnull
    {
        private readonly IReadOnlyDictionary<TKey, TValue>? _data;

        public TValue this[TKey key]
            => _data is null
                ? throw new KeyNotFoundException()
                : _data[key];

        public IEnumerable<TKey> Keys
            => _data?.Keys ?? Enumerable.Empty<TKey>();

        public IEnumerable<TValue> Values
            => _data?.Values ?? Enumerable.Empty<TValue>();

        public int Count
            => _data?.Count ?? 0;

        public ReadOnlyDictionaryWrapper(IReadOnlyDictionary<TKey, TValue>? data)
            => _data = data;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool ContainsKey(TKey key)
            => _data?.ContainsKey(key) ?? false;

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
            => (_data ?? Enumerable.Empty<KeyValuePair<TKey, TValue>>()).GetEnumerator();

        public bool TryGetValue(
            TKey key,
            #if NETSTANDARD2_1
            [NotNullWhen(true)] out TValue value
            #else
            out TValue value
            #endif
            )
        {
            if (_data is null)
            {
                value = default!;
                return false;
            }
            return _data.TryGetValue(key, out value!);
        }
    }
}