using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NCoreUtils.Logging.Google.Data;

[method: JsonConstructor]
public sealed class LogEntry(
    string logName,
    LogSeverity severity,
    string message,
    DateTimeOffset timestamp,
    ServiceContext? serviceContext,
    ErrorContext? context,
    HttpRequest? httpRequest,
    string? trace,
    IReadOnlyDictionary<string, string>? labels) : IDisposable
{
    private static readonly IReadOnlyDictionary<string, string> _noLabels = new Dictionary<string, string>();

    [JsonPropertyName("logName")]
    public string LogName { get; private set; } = logName;

    [JsonConverter(typeof(SeverityConverter))]
    [JsonPropertyName("severity")]
    public LogSeverity Severity { get; private set; } = severity;

    [JsonPropertyName("message")]
    public string Message { get; private set; } = message;

    [JsonConverter(typeof(TimestampConverter))]
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; private set; } = timestamp;

    [JsonPropertyName("serviceContext")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ServiceContext? ServiceContext { get; private set; } = serviceContext;

    [JsonPropertyName("context")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ErrorContext? Context { get; private set; } = context;

    [JsonPropertyName("httpRequest")]
    public HttpRequest? HttpRequest { get; private set; } = httpRequest;

    [JsonPropertyName("logging.googleapis.com/trace")]
    public string? Trace { get; private set; } = trace;

    [JsonPropertyName("logging.googleapis.com/labels")]
    public IReadOnlyDictionary<string, string> Labels { get; private set; } = labels ?? _noLabels;

    public LogEntry Update(
        string logName,
        LogSeverity severity,
        string message,
        DateTimeOffset timestamp,
        ServiceContext? serviceContext,
        ErrorContext? context,
        HttpRequest? httpRequest,
        string? trace,
        IReadOnlyDictionary<string, string>? labels)
    {
        LogName = logName;
        Severity = severity;
        Message = message;
        Timestamp = timestamp;
        ServiceContext = serviceContext;
        Context = context;
        HttpRequest = httpRequest;
        Trace = trace;
        Labels = labels ?? _noLabels;
        return this;
    }

    void IDisposable.Dispose()
    {
        var httpRequest = HttpRequest;
        HttpRequest = null;
        if (httpRequest is not null)
        {
            Pool.HttpRequest.Return(httpRequest);
        }
        var serviceContext = ServiceContext;
        ServiceContext = null;
        if (serviceContext is not null)
        {
            Pool.ServiceContext.Return(serviceContext);
        }
        var errorContext = Context;
        Context = null;
        if (errorContext is not null)
        {
            Pool.ErrorContext.Return(errorContext);
        }
        var labels = Labels;
        Labels = null!;
        if (labels is Dictionary<string, string> labelDictionary)
        {
            labelDictionary.Clear();
            Pool.Labels.Return(labelDictionary);
        }
        Pool.LogEntry.Return(this);
    }
}