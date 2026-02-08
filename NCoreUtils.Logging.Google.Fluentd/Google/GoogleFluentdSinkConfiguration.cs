using NCoreUtils.Logging.Google.Internal;

namespace NCoreUtils.Logging.Google;

public class GoogleFluentdSinkConfiguration : IGoogleFluentdSinkConfiguration
{
    private int _dirty = 1;

    private string? _logName;

    private string _projectId = string.Empty;

    private string? _service;

    public string ProjectId
    {
        get => _projectId;
        set
        {
            _projectId = value;
            Interlocked.CompareExchange(ref _dirty, 1, 0);
        }
    }

    public string? Service
    {
        get => _service;
        set
        {
            _service = value;
            Interlocked.CompareExchange(ref _dirty, 1, 0);
        }
    }

    public string? ServiceVersion { get; set; }

    public string? LogName
    {
        get
        {
            if (1 == Interlocked.CompareExchange(ref _dirty, 0, 1))
            {
                _logName = Service is string { Length: > 0 } service
                    ? Fmt.LogName(ProjectId, service)
                    : default;
            }
            return _logName;
        }
    }

    public string Output { get; set; } = DefaultByteSequenceOutput.StdOut;

    public CategoryHandling CategoryHandling { get; set; }

    public EventIdHandling EventIdHandling { get; set; }

    public TraceHandling TraceHandling { get; set; }
}