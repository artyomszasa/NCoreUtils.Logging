namespace NCoreUtils.Logging.Google.Data
{
    public class ServiceContext
    {
        public string Service { get; }

        public string? Version { get; }

        public ServiceContext(string service, string? version)
        {
            Service = service;
            Version = version;
        }
    }
}