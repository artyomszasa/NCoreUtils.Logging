using System.Runtime.CompilerServices;
using Google.Protobuf.WellKnownTypes;

namespace NCoreUtils.Logging.Google
{
    internal static class ProtoDataExtensions
    {
#if NETSTANDARD2_1
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#endif
        public static Struct Add(this Struct obj, string key, string? value, bool force = false)
        {
            if (force || !string.IsNullOrEmpty(value))
            {
                obj.Fields.Add(key, Value.ForString(value ?? string.Empty));
            }
            return obj;
        }

#if NETSTANDARD2_1
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#endif
        public static Struct Add(this Struct obj, string key, int? value, bool force = false)
        {
            if (force || value.HasValue)
            {
                obj.Fields.Add(key, Value.ForNumber(value ?? default));
            }
            return obj;
        }

#if NETSTANDARD2_1
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#endif
        public static Struct Add(this Struct obj, string key, Struct? value)
        {
            if (null != value)
            {
                obj.Fields.Add(key, Value.ForStruct(value));
            }
            return obj;
        }
    }
}