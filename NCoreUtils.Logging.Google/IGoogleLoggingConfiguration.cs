using System.Collections.Generic;

namespace NCoreUtils.Logging
{
    public interface IGoogleLoggingConfiguration
    {
        string? ProjectId { get; }

        string? LogId { get; }

        string? ResourceType { get; }

        IReadOnlyDictionary<string, string>? ResourceLabels { get; }
    }
}