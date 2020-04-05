using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NCoreUtils.Logging.Google
{
    internal class ByReferenceEqualityComparer<T> : IEqualityComparer<T>
        where T : class
    {
        public static ByReferenceEqualityComparer<T> Instance { get; } = new ByReferenceEqualityComparer<T>();

        private ByReferenceEqualityComparer() { }

        public bool Equals(T x, T y)
            => ReferenceEquals(x, y);

        public int GetHashCode(T obj)
            => RuntimeHelpers.GetHashCode(obj);
    }
}