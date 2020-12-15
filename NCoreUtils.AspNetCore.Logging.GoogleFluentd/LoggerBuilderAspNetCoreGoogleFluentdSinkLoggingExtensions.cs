using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NCoreUtils.Logging;
using NCoreUtils.Logging.Google;

namespace NCoreUtils.AspNetCore
{
    public static class LoggerBuilderAspNetCoreGoogleFluentdSinkLoggingExtensions
    {
        private sealed class AspNetCoreConnectionIdLabelProvider : ILabelProvider
        {
            public void UpdateLabels(
                string category,
                EventId eventId,
                LogLevel logLevel,
                in AspNetCoreContext context,
                IDictionary<string, string> labels)
            {
                if (!string.IsNullOrEmpty(context.ConnectionId))
                {
                    labels.Add("aspnetcore-connection-id", context.ConnectionId);
                }
            }
        }

        private static string GetProjectId(string? projectId, bool force)
        {
            string result;
            if (!force && string.IsNullOrEmpty(projectId))
            {
                try
                {
                    var metaEndpoint = "http://metadata.google.internal/computeMetadata/v1/project/project-id";
                    using var client = new HttpClient();
                    using var request = new HttpRequestMessage(HttpMethod.Get, metaEndpoint);
                    request.Headers.Add("Metadata-Flavor", "Google");
                    var response = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).Result;
                    result = response.Content.ReadAsStringAsync().Result;
                }
                catch
                {
                    Console.Error.WriteLine();
                }
            }
            return string.IsNullOrEmpty(projectId)
                ? throw new InvalidOperationException("Unable to get project id.")
                : projectId;
        }

        public static ILoggingBuilder AddGoogleFluentdSink(
            this ILoggingBuilder builder,
            AspNetCoreGoogleFluentdLoggingContext loggingContext,
            Action<GoogleFluentdOptions>? configureOptions = default)
        {
            builder.Services
                .AddLoggingContext()
                .AddSingleton(loggingContext)
                .AddDefaultGoogleTraceIdProvider();
            var options = builder.Services.AddOptions<GoogleFluentdOptions>();
            if (!(configureOptions is null))
            {
                options.Configure(configureOptions);
            }
            return builder
                .AddGoogleLabelProvider(new AspNetCoreConnectionIdLabelProvider())
                .AddSink<AspNetCoreLoggerProvider<AspNetCoreGoogleFluentdSink>, AspNetCoreGoogleFluentdSink>();
        }

        public static ILoggingBuilder AddGoogleFluentdSink(
            this ILoggingBuilder builder,
            string uri = "file:///dev/stdout",
            string? projectId = default,
            string? logId = default,
            string? serviceVersion = default,
            bool force = false,
            TraceHandling? traceHandling = default,
            CategoryHandling? categoryHandling = default,
            EventIdHandling? eventIdHandling = default,
            Action<GoogleFluentdOptions>? configureOptions = default)
        {
            var pid = GetProjectId(projectId, force);
            return builder.AddGoogleFluentdSink(new AspNetCoreGoogleFluentdLoggingContext(
                uri,
                pid,
                logId ?? Assembly.GetEntryAssembly()?.GetName()?.Name?.Replace(".", "-")?.ToLowerInvariant() ?? throw new InvalidOperationException("Unable to get log id."),
                serviceVersion
            ), options =>
            {
                if (traceHandling.HasValue)
                {
                    options.TraceHandling = traceHandling.Value;
                }
                if (categoryHandling.HasValue)
                {
                    options.CategoryHandling = categoryHandling.Value;
                }
                if (eventIdHandling.HasValue)
                {
                    options.EventIdHandling = eventIdHandling.Value;
                }
                configureOptions?.Invoke(options);
            });
        }

        public static ILoggingBuilder AddGoogleLabelProvider(this ILoggingBuilder builder, ILabelProvider provider)
        {
            builder.Services.AddSingleton<ILabelProvider>(provider);
            return builder;
        }

        public static ILoggingBuilder AddGoogleLabelProvider(this ILoggingBuilder builder, Action<string, EventId, LogLevel, AspNetCoreContext, IDictionary<string, string>> provider)
            => builder.AddGoogleLabelProvider(LabelProvider.Create(provider));

        public static ILoggingBuilder AddGoogleLabelProvider(this ILoggingBuilder builder, Action<AspNetCoreContext, IDictionary<string, string>> provider)
            => builder.AddGoogleLabelProvider(LabelProvider.Create(provider));
    }
}