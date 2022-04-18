using System.Text.Json.Serialization;

namespace NCoreUtils.Logging.Google.Data
{
    [JsonSerializable(typeof(TimestampConverter.Proxy))]
    public partial class TimestampProxyJsonContext : JsonSerializerContext { }
}