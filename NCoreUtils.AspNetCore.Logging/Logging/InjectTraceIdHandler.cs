using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Logging
{
    public class InjectTraceIdHandler : DelegatingHandler
    {
        private readonly ITraceIdProvider _traceIdProvider;

        public InjectTraceIdHandler(ITraceIdProvider traceIdProvider)
        {
            _traceIdProvider = traceIdProvider ?? throw new ArgumentNullException(nameof(traceIdProvider));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Add("X-Trace-Id", _traceIdProvider.TraceId);
            return base.SendAsync(request, cancellationToken);
        }
    }
}