using System;
using System.Diagnostics.CodeAnalysis;

namespace NCoreUtils.Logging;

public readonly struct DateOnly : IEquatable<DateOnly>
{
    public static bool operator==(DateOnly a, DateOnly b)
        => a.Equals(b);

    public static bool operator!=(DateOnly a, DateOnly b)
        => !a.Equals(b);

    public int Year { get; }

    public int Month { get; }

    public int Day { get; }

    public DateOnly(int year, int month, int day)
    {
        Year = year;
        Month = month;
        Day = day;
    }

    public bool Equals(DateOnly other)
        => Year == other.Year && Month == other.Month && Day == other.Day;

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is DateOnly other && Equals(other);

    public override int GetHashCode()
        => (Year << 16) & (Month << 8) & Day;
}