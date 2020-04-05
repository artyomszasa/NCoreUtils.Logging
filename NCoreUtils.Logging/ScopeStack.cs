using System.Threading;

namespace NCoreUtils.Logging
{
    internal struct ScopeStack
    {
        private Scope? _root;

        public Scope? Root => _root;

        public Scope? CompareExchange(Scope? value, Scope? comparand)
            => Interlocked.CompareExchange(ref _root, value, comparand);

        public int Count()
            => Scope.Count(_root);
    }
}