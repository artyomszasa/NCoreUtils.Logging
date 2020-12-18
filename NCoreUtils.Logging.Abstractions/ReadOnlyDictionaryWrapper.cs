using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace NCoreUtils.Logging
{
    public static class ReadOnlyDictionaryWrapper
    {
        public static ReadOnlyDictionaryWrapper<TKey, TValue> WrapMutable<TKey, TValue>(IDictionary<TKey, TValue> data)
            where TKey : notnull
            => new ReadOnlyDictionaryWrapper<TKey, TValue>(data, default);

        public static ReadOnlyDictionaryWrapper<TKey, TValue> WrapReadOnly<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> data)
            where TKey : notnull
            => new ReadOnlyDictionaryWrapper<TKey, TValue>(default, data);
    }

    /// <summary>
    /// NUll-safe readonly dictionary wrapper.
    /// </summary>
    public struct ReadOnlyDictionaryWrapper<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
        where TKey : notnull
    {
        private readonly IDictionary<TKey, TValue>? _rwData;

        private readonly IReadOnlyDictionary<TKey, TValue>? _roData;

        public TValue this[TKey key]
            => _rwData is null
                ? _roData is null
                    ? throw new KeyNotFoundException()
                    : _roData[key]
                : _rwData[key];

        public IEnumerable<TKey> Keys
            => _rwData is null
                ? _roData is null
                    ? Enumerable.Empty<TKey>()
                    : _roData.Keys
                : _rwData.Keys;

        public IEnumerable<TValue> Values
            => _rwData is null
                ? _roData is null
                    ? Enumerable.Empty<TValue>()
                    : _roData.Values
                : _rwData.Values;

        public int Count
            => _rwData is null
                ? _roData is null
                    ? 0
                    : _roData.Count
                : _rwData.Count;

        internal ReadOnlyDictionaryWrapper(
            IDictionary<TKey, TValue>? rwData,
            IReadOnlyDictionary<TKey, TValue>? roData)
        {
            _rwData = rwData;
            _roData = roData;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool ContainsKey(TKey key)
            => _rwData is null
                ? !(_roData is null) && _roData.ContainsKey(key)
                : _rwData.ContainsKey(key);

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
            => _rwData is null
                ? _roData is null
                    ? Enumerable.Empty<KeyValuePair<TKey, TValue>>().GetEnumerator()
                    : _roData.GetEnumerator()
                : _rwData.GetEnumerator();

        public bool TryGetValue(
            TKey key,
            #if NETSTANDARD2_1
            [NotNullWhen(true)] out TValue value
            #else
            out TValue value
            #endif
            )
        {
            if (_rwData is null)
            {
                if (_roData is null)
                {
                    value = default!;
                    return false;
                }
                return _roData.TryGetValue(key, out value!);
            }
            return _rwData.TryGetValue(key, out value!);
        }
    }
}