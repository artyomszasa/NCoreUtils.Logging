using Google.Api;

namespace NCoreUtils.Logging.Google
{
    public interface IGoogleClientSinkConfiguration : IGoogleSinkConfiguration
    {
        MonitoredResource Resource { get; }
    }
}