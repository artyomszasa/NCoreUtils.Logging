using System.Collections.Generic;

namespace NCoreUtils.AspNetCore
{
    public interface IGoogleAspNetCoreLoggingConfiguration
    {
        string? ProjectId { get; }

        string? ServiceName { get; }

        string? ServiceVersion { get; }

        string? ResourceType { get; }

        IReadOnlyDictionary<string, string>? ResourceLabels { get; }
    }
}