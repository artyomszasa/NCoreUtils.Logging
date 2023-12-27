using System.Text.Json.Serialization;

namespace NCoreUtils.Logging.Google.Data;

[JsonSerializable(typeof(LogEntry))]
public partial class LogEntryJsonContext : JsonSerializerContext { }