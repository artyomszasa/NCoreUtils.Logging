using Google.Protobuf.WellKnownTypes;

namespace NCoreUtils.Logging.Google
{
    internal static class ProtoDataExtensions
    {
        public static Struct Add(this Struct obj, string key, string? value, bool force = false)
        {
            if (force || !string.IsNullOrEmpty(value))
            {
                obj.Fields.Add(key, Value.ForString(value ?? string.Empty));
            }
            return obj;
        }

        public static Struct Add(this Struct obj, string key, int? value, bool force = false)
        {
            if (force || value.HasValue)
            {
                obj.Fields.Add(key, Value.ForNumber(value ?? default));
            }
            return obj;
        }

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