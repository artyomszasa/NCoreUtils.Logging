using Google.Api;
using NCoreUtils.Logging.Google.Internal;

namespace NCoreUtils.Logging.Google
{
    public class GoogleClientSinkConfiguration : IGoogleClientSinkConfiguration
    {
        private string? _logName;

        private string _projectId = string.Empty;

        private string _service = string.Empty;

        public MonitoredResource Resource { get; set; } = new MonitoredResource();

        public string ProjectId
        {
            get => _projectId;
            set
            {
                _projectId = value;
                _logName = default;
            }
        }

        public string Service
        {
            get => _service;
            set
            {
                _service = value;
                _logName = default;
            }
        }

        public string? ServiceVersion { get; set; }

        public string LogName
        {
            get
            {
                _logName ??= Fmt.LogName(ProjectId, Service);
                return _logName;
            }
        }

        public CategoryHandling CategoryHandling { get; set; }

        public EventIdHandling EventIdHandling { get; set; }

        public TraceHandling TraceHandling { get; set; }
    }
}