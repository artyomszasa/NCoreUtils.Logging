using System;
using Microsoft.Extensions.Configuration;

namespace NCoreUtils.Logging.Google.Internal;

public static class ConfigurationGoogleLoggingExtensions
{
    // https://github.com/dotnet/runtime/issues/36540

    private static string GetFullPath(IConfiguration configuration, string key)
    {
        if (configuration is IConfigurationSection section)
        {
            return $"{section.Path}:{key}";
        }
        return key;
    }

    public static CategoryHandling? GetCategoryHandlingOrNull(this IConfiguration configuration, string key)
    {
        var raw = configuration[key];
        if (!string.IsNullOrEmpty(raw))
        {
            return raw switch
            {
                nameof(CategoryHandling.Ignore) => CategoryHandling.Ignore,
                nameof(CategoryHandling.IncludeAsLabel) => CategoryHandling.IncludeAsLabel,
                nameof(CategoryHandling.IncludeInMessage) => CategoryHandling.IncludeInMessage,
                _ => throw new FormatException($"Invalid CategoryHandling value at {GetFullPath(configuration, key)}")
            };
        }
        return default;
    }

    public static EventIdHandling? GetEventIdHandlingOrNull(this IConfiguration configuration, string key)
    {
        var raw = configuration[key];
        if (!string.IsNullOrEmpty(raw))
        {
            return raw switch
            {
                nameof(EventIdHandling.Ignore) => EventIdHandling.Ignore,
                nameof(EventIdHandling.IncludeAlways) => EventIdHandling.IncludeAlways,
                nameof(EventIdHandling.IncludeValidIds) => EventIdHandling.IncludeValidIds,
                _ => throw new FormatException($"Invalid EventIdHandling value at {GetFullPath(configuration, key)}")
            };
        }
        return default;
    }

    public static TraceHandling? GetTraceHandlingOrNull(this IConfiguration configuration, string key)
    {
        var raw = configuration[key];
        if (!string.IsNullOrEmpty(raw))
        {
            return raw switch
            {
                nameof(TraceHandling.Disabled) => TraceHandling.Disabled,
                nameof(TraceHandling.Enabled) => TraceHandling.Enabled,
                nameof(TraceHandling.Summary) => TraceHandling.Summary,
                _ => throw new FormatException($"Invalid TraceHandling value at {GetFullPath(configuration, key)}")
            };
        }
        return default;
    }
}