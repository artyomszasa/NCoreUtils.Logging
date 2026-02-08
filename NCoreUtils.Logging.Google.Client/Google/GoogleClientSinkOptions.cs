using Google.Api;
using NCoreUtils.Logging.Google.Internal;

namespace NCoreUtils.Logging.Google;

public class GoogleClientSinkConfiguration : IGoogleClientSinkConfiguration
{
    private readonly object _sync = new();

    private string? _logName;

    private string _projectId = string.Empty;

    private string? _service;

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

    public string? Service
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
            if (_logName is null)
            {
                lock (_sync)
                {
                    // FIXME: use resource...
                    _logName ??= Fmt.LogName(ProjectId, Service ?? "unknown");
                }
            }
            return _logName;
        }
    }

    public CategoryHandling CategoryHandling { get; set; }

    public EventIdHandling EventIdHandling { get; set; }

    public TraceHandling TraceHandling { get; set; }
}