namespace NCoreUtils.Logging.Google
{
    public interface IGoogleFluentdSinkConfiguration : IGoogleSinkConfiguration
    {
        string Output { get; }
    }
}