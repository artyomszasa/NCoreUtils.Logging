using System.Runtime.CompilerServices;
using System.Threading;

namespace NCoreUtils.Logging.Internal;

internal struct ScopeStack
{
    private Scope? _root;

    public readonly Scope? Root
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _root;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Scope? CompareExchange(Scope? value, Scope? comparand)
        => Interlocked.CompareExchange(ref _root, value, comparand);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int Count()
        => Scope.Count(_root);
}