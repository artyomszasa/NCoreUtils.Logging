using System;

namespace NCoreUtils.Logging.RollingFile
{
    [Flags]
    public enum FileRollTrigger
    {
        Size = 0x01,
        Date = 0x02
    }
}