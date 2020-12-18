using Microsoft.Extensions.DependencyInjection;

namespace NCoreUtils.Logging
{
    public static class HttpClientBuilderTraceExtensions
    {
        public static IHttpClientBuilder InjectTraceId(this IHttpClientBuilder builder)
            => builder.AddHttpMessageHandler<InjectTraceIdHandler>();
    }
}