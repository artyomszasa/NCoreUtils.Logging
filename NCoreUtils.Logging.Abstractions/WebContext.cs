using System;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace NCoreUtils.Logging;

public struct WebContext
{
    public string? ConnectionId { get; internal set; }

    public string? Method { get; internal set; }

    public string? Url { get; internal set; }

    public string? UserAgent { get; internal set; }

    public string? Referrer { get; internal set; }

    public int? ResponseStatusCode { get; internal set; }

    public string? RemoteIp { get; internal set; }

    public string? TraceId { get; internal set; }

    public ReadOnlyDictionaryWrapper<string, StringValues> Headers { get; internal set; }

    public string? User { get; internal set; }

    public TimeSpan? Latency { get; internal set; }

    public string? ResponseContentType { get; internal set; }

    public long? ResponseContentLength { get; internal set; }

    public WebContext(
        string? connectionId,
        string? method,
        string? url,
        string? userAgent,
        string? referrer,
        int? responseStatusCode,
        string? remoteIp,
        string? traceId,
        IReadOnlyDictionary<string, StringValues>? headers,
        string? user)
    {
        ConnectionId = connectionId;
        Method = method;
        Url = url;
        UserAgent = userAgent;
        Referrer = referrer;
        ResponseStatusCode = responseStatusCode;
        RemoteIp = remoteIp;
        TraceId = traceId;
        Headers = headers is null ? default : ReadOnlyDictionaryWrapper.WrapReadOnly(headers);
        User = user;
        Latency = default;
        ResponseContentType = default;
        ResponseContentLength = default;
    }

    public WebContext(
        string? connectionId,
        string? method,
        string? url,
        string? userAgent,
        string? referrer,
        int? responseStatusCode,
        string? remoteIp,
        string? traceId,
        IDictionary<string, StringValues>? headers,
        string? user)
    {
        ConnectionId = connectionId;
        Method = method;
        Url = url;
        UserAgent = userAgent;
        Referrer = referrer;
        ResponseStatusCode = responseStatusCode;
        RemoteIp = remoteIp;
        TraceId = traceId;
        Headers = headers is null ? default : ReadOnlyDictionaryWrapper.WrapMutable(headers);
        User = user;
        Latency = default;
        ResponseContentType = default;
        ResponseContentLength = default;
    }
}