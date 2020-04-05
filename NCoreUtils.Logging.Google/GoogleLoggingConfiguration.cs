using System.Collections.Generic;

namespace NCoreUtils.Logging
{
    internal class GoogleLoggingConfiguration : IGoogleLoggingConfiguration
    {
        IReadOnlyDictionary<string, string>? IGoogleLoggingConfiguration.ResourceLabels => ResourceLabels;

        public string? ProjectId { get; set; }

        public string? LogId { get; set; }

        public string? ResourceType { get; set; }

        public Dictionary<string, string> ResourceLabels { get; } = new Dictionary<string, string>();
    }
}