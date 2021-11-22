using System;
using System.IO;

namespace NCoreUtils.Logging.DefaultOutputs
{
    public sealed class StdOutOutput : StreamOutput
    {
        protected override Stream InitializeStream()
            => Console.OpenStandardOutput();
    }
}