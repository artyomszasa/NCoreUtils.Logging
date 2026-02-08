namespace NCoreUtils.Logging;

public class InjectTraceIdHandler(ITraceIdProvider traceIdProvider) : DelegatingHandler
{
    private readonly ITraceIdProvider _traceIdProvider = traceIdProvider ?? throw new ArgumentNullException(nameof(traceIdProvider));

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Add("X-Trace-Id", _traceIdProvider.TraceId);
        return base.SendAsync(request, cancellationToken);
    }
}