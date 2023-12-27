using System;

namespace NCoreUtils.Logging.RollingFile;

public interface IDateProvider
{
    DateOnly CurrentDate { get; }
}