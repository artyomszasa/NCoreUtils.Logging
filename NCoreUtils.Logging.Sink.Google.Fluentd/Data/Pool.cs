using System.Collections.Generic;
using NCoreUtils.Logging.Internal;

namespace NCoreUtils.Logging.Google.Data;

public static class Pool
{
    public static FixSizePool<HttpRequest> HttpRequest { get; } = new(8);

    public static FixSizePool<ServiceContext> ServiceContext { get; } = new(8);

    public static FixSizePool<ErrorContext> ErrorContext { get; } = new(8);

    public static FixSizePool<Dictionary<string, string>> Labels { get; } = new(8);

    public static FixSizePool<LogEntry> LogEntry { get; } = new(8);
}