using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace NCoreUtils.Logging.Internal;

public class FixSizePool<T>
    where T : class
{
    private struct Index : IEquatable<Index>
    {
        private const uint MaskValue = 0x0000FFFF;

        private const uint MaskLocked = 0x80000000;

        private const uint MaskLoop = 0x40000000;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        public static bool operator==(Index a, Index b)
            => a.Equals(b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        public static bool operator!=(Index a, Index b)
            => !a.Equals(b);

        private uint _data;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        public Index(uint data)
            => _data = data;

        public uint Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [DebuggerStepThrough]
            get => _data & MaskValue;
        }

        public bool Loop
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [DebuggerStepThrough]
            get => (_data & MaskLoop) != 0;
        }

        public bool Locked
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [DebuggerStepThrough]
            get => (_data & MaskLocked) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        public Index Inc()
            => new(_data + 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        public Index ToggleLoop()
            => new((_data & (~MaskValue)) ^ MaskLoop);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        public Index Lock()
            => new(_data | MaskLocked);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        public Index CompareExchange(Index value, Index comparand)
#if NET6_0_OR_GREATER
            => new(Interlocked.CompareExchange(ref _data, value._data, comparand._data));
#else
        {
            var ival = Interlocked.CompareExchange(ref Unsafe.As<uint, int>(ref _data), unchecked((int)value._data), unchecked((int)comparand._data));
            return new(unchecked((uint)ival));
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        public bool Equals(Index other)
            => _data == other._data;

        public override bool Equals([NotNullWhen(true)] object? obj)
            => obj is Index other && Equals(other);

        public override int GetHashCode()
            => unchecked((int)_data);

        public override string ToString()
            => $"{Value} [Loop = {Loop}, Locked = {Locked}]";
    }

    private static int ComputeSize(Index start, Index end, int capacity)
    {
        if (start.Loop == end.Loop)
        {
            return unchecked((int)(end.Value - start.Value));
        }
        return unchecked((int)end.Value) + capacity - unchecked((int)start.Value);
    }

    private readonly T?[] _items;

    private uint _maxIndex;

    private Index _start;

    private Index _end;

    public FixSizePool(int size)
    {
        if (size < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(size));
        }
        _items = new T[size];
        _maxIndex = unchecked((uint)size - 1);
    }

    public bool TryRent([MaybeNullWhen(false)] out T item)
    {
        while (true)
        {
            var actualStart = _start.CompareExchange(default, default);
            var actualEnd = _end.CompareExchange(default, default);
            // check if pool is not locked (no operation is in progress)
            if (!actualEnd.Locked)
            {
                if (actualStart == actualEnd)
                {
                    // no items avaliable
                    item = default;
                    return false;
                }
                // preload item --> it will be returned if the operation succeeds
                var candidate = _items[actualStart.Value];
                Index newStart;
                if (actualStart.Value == _maxIndex)
                {
                    newStart = actualStart.ToggleLoop();
                }
                else
                {
                    newStart = actualStart.Inc();
                }
                if (actualStart == _start.CompareExchange(newStart, actualStart))
                {
                    // operation has succeeded
                    item = candidate!;
                    return true;
                }
            }
            // in all other cases --> operation should be retried
        }
    }

    public void Return(T item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }
        bool success;
        do
        {
            var actualStart = _start.CompareExchange(default, default);
            var actualEnd = _end.CompareExchange(default, default);
            // check if pool is not locked (no operation is in progress)
            if (!actualEnd.Locked)
            {
                if (_items.Length == ComputeSize(actualStart, actualEnd, _items.Length))
                {
                    return; // allow GC to claim an item
                }
                Index newEnd;
                if (actualEnd.Value == _maxIndex)
                {
                    newEnd = actualEnd.ToggleLoop();
                }
                else
                {
                    newEnd = actualEnd.Inc();
                }
                // Two step value application:
                // If first step succeedes --> pool is in locked state and the item can be stored safely
                // second step --> pool is unlocked
                var maskedEnd = newEnd.Lock();
                if (actualEnd == _end.CompareExchange(maskedEnd, actualEnd))
                {
                    // pool is locked --> proceed to store value and unlock pool
                    _items[actualEnd.Value] = item;
                    _end.CompareExchange(newEnd, maskedEnd); // should always succeed
                    success = true;
                }
                else
                {
                    // update has failed --> retry operation
                    success = false;
                }
            }
            else
            {
                // pool is locked (operation is in progress) --> retry current operation
                success = false;
            }
        }
        while (!success);
    }
}
