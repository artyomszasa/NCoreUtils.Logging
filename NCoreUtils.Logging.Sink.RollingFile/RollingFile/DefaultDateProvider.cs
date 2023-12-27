using System;

namespace NCoreUtils.Logging.RollingFile;

public sealed class DefaultDateProvider : IDateProvider
{
    public static DefaultDateProvider Singleton { get; } = new();

    public DateOnly CurrentDate
    {
        get
        {
            var now = DateTimeOffset.Now;
            return new DateOnly(now.Year, now.Month, now.Day);
        }
    }
}