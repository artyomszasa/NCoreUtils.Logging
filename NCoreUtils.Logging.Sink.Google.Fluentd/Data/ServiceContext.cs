using System.Text.Json.Serialization;

namespace NCoreUtils.Logging.Google.Data
{
    public class ServiceContext
    {
        [JsonPropertyName("service")]
        public string Service { get; }

        [JsonPropertyName("version")]
        public string? Version { get; }

        [JsonConstructor]
        public ServiceContext(string service, string? version)
        {
            Service = service;
            Version = version;
        }
    }
}