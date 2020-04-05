using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

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
        private static void UpdateContext(ref AspNetCoreContext context, HttpContext? httpContext)
        {
            if (httpContext is null)
            {
                return;
            }
            // User
            context.User = GetUser(httpContext.User);
            // ResponseStatusCode
            context.ResponseStatusCode = httpContext.Response?.StatusCode;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void PopulateContext(ref AspNetCoreContext context, HttpContext? httpContext)
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
            context.Headers = new ReadOnlyDictionaryWrapper<string, StringValues>(ImmutableDictionary.CreateRange(StringComparer.OrdinalIgnoreCase, request.Headers));
            // ConnectionId
            context.ConnectionId = httpContext.Connection?.Id;
            // RemoteIp
            context.RemoteIp = httpContext.Connection?.RemoteIpAddress?.ToString();
            // User and ResponseStatusCode
            UpdateContext(ref context, httpContext);
        }

        AspNetCoreContext _aspNetCoreContext;

        public AspNetCoreContext AspNetCoreContext
        {
            get => _aspNetCoreContext;
            set => _aspNetCoreContext = value;
        }

        internal void PopulateFrom(HttpContext? httpContext)
            => PopulateContext(ref _aspNetCoreContext, httpContext);

        internal void UpdateFrom(HttpContext? httpContext)
            => UpdateContext(ref _aspNetCoreContext, httpContext);
    }
}