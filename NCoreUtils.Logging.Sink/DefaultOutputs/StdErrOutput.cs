using System;
using System.IO;

namespace NCoreUtils.Logging.DefaultOutputs
{
    public sealed class StdErrOutput : StreamOutput
    {
        protected override Stream InitializeStream()
            => Console.OpenStandardError();
    }
}