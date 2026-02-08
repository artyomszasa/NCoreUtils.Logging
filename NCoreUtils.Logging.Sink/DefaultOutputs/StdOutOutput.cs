namespace NCoreUtils.Logging.DefaultOutputs;

public sealed class StdOutOutput : StreamOutput
{
    protected override Stream InitializeStream()
        => Console.OpenStandardOutput();
}