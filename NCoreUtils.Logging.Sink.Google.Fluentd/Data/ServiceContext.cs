using System.Text.Json.Serialization;

namespace NCoreUtils.Logging.Google.Data;

[method: JsonConstructor]
public class ServiceContext(string service, string? version)
{
    [JsonPropertyName("service")]
    public string Service { get; private set; } = service;

    [JsonPropertyName("version")]
    public string? Version { get; private set; } = version;

    public ServiceContext Update(string service, string? version)
    {
        Service = service;
        Version = version;
        return this;
    }
}