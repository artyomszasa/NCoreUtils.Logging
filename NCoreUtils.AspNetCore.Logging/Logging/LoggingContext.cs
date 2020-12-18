using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using NCoreUtils.Logging.Internal;

namespace NCoreUtils.Logging
{
    public class LoggingContext
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetEffectiveHost(in HostString host) => host.HasValue switch
        {
            true => host.Host,
            _    => "localhost"
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetEffectivePort(in HostString host, bool isHttps) => (host.HasValue && host.Port.HasValue) switch
        {
            true => host.Port!.Value switch
            {
                443 when (isHttps) => -1,
                80 when (!isHttps) => -1,
                int port => port
            },
            _ => -1
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string? GetUserAgentString(IHeaderDictionary headers)
            => headers.TryGetValue("User-Agent", out var values) && values.Count > 0 ? values[0] : default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string? GetReferrer(IHeaderDictionary headers)
            => headers.TryGetValue("Referer", out var values) && values.Count > 0 ? values[0] : default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string? GetUser(ClaimsPrincipal? principal)
            => principal?.Identity?.Name;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void UpdateContext(ref WebContext context, HttpContext? httpContext)
        {
            if (httpContext is null)
            {
                return;
            }
            // Trace ID -- may be generated during the request
            if (httpContext.RequestServices.TryGetOptionalService(out ITraceIdProvider? traceIdProvider))
            {
                context.TraceId = traceIdProvider.TraceId;
            }
            // id request scope has been disposed trace id may have been written into HttpContext.Items
            else if (httpContext.Items.TryGetValue(HttpContextItemIds.TraceId, out var boxedTraceId) && boxedTraceId is string traceId)
            {
                context.TraceId = traceId;
            }
            // User
            context.User = GetUser(httpContext.User);
            // ResponseStatusCode
            context.ResponseStatusCode = httpContext.Response?.StatusCode;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void PopulateContext(ref WebContext context, HttpContext? httpContext)
        {
            if (httpContext is null)
            {
                return;
            }
            var request = httpContext.Request;
            if (request is null)
            {
                return;
            }
            // Url
            var uriBuilder = new UriBuilder
            {
                Scheme = request.Scheme,
                Host = GetEffectiveHost(request.Host),
                Port = GetEffectivePort(request.Host, request.IsHttps),
                Path = request.Path.ToUriComponent(),
                Query = request.QueryString.ToUriComponent()
            };
            context.Url = uriBuilder.Uri.AbsoluteUri;
            // Method
            context.Method = request.Method;
            // User-Agent
            context.UserAgent = GetUserAgentString(request.Headers);
            // Referrer
            context.Referrer = GetReferrer(request.Headers);
            // Headers
            context.Headers = ReadOnlyDictionaryWrapper.WrapMutable(request.Headers);
            // ConnectionId
            context.ConnectionId = httpContext.Connection?.Id;
            // RemoteIp
            context.RemoteIp = httpContext.Connection?.RemoteIpAddress?.ToString();
            // User and ResponseStatusCode
            UpdateContext(ref context, httpContext);
        }

        private WebContext _webContext;

        public ref WebContext WebContext
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _webContext;
        }

        internal void PopulateFrom(HttpContext? httpContext)
            => PopulateContext(ref _webContext, httpContext);

        internal void UpdateFrom(HttpContext? httpContext)
            => UpdateContext(ref _webContext, httpContext);
    }
}