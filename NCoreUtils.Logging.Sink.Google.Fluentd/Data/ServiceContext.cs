using System.Text.Json.Serialization;

namespace NCoreUtils.Logging.Google.Data
{
    public class ServiceContext
    {
        [JsonPropertyName("service")]
        public string Service { get; private set; }

        [JsonPropertyName("version")]
        public string? Version { get; private set; }

        [JsonConstructor]
        public ServiceContext(string service, string? version)
        {
            Service = service;
            Version = version;
        }

        public ServiceContext Update(string service, string? version)
        {
            Service = service;
            Version = version;
            return this;
        }
    }
}