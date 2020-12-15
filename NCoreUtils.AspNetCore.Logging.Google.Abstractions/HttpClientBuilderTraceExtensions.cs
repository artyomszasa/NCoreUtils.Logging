using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.Logging.Google;

namespace NCoreUtils
{
    public static class HttpClientBuilderTraceExtensions
    {
        public static IHttpClientBuilder InjectTraceId(this IHttpClientBuilder builder)
            => builder.AddHttpMessageHandler<InjectTraceIdHandler>();
    }
}