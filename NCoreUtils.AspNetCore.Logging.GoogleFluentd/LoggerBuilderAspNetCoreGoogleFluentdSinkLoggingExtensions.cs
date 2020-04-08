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
            public void UpdateLabels(string category, EventId eventId, LogLevel logLevel, in AspNetCoreContext context, IDictionary<string, string> labels)
            {
                if (!string.IsNullOrEmpty(context.ConnectionId))
                {
                    labels.Add("aspnetcore-connection-id", context.ConnectionId);
                }
            }
        }

        public static ILoggingBuilder AddGoogleFluentdSink(this ILoggingBuilder builder, AspNetCoreGoogleFluentdLoggingContext loggingContext)
        {
            builder.Services.AddLoggingContext();
            builder.Services.AddSingleton(loggingContext);
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
            bool force = false)
        {
            if (!force && string.IsNullOrEmpty(projectId))
            {
                // Try get from meta.
                try
                {
                    var metaEndpoint = "http://metadata.google.internal/computeMetadata/v1/project/project-id";
                    using var client = new HttpClient();
                    using var request = new HttpRequestMessage(HttpMethod.Get, metaEndpoint);
                    request.Headers.Add("Metadata-Flavor", "Google");
                    var response = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).Result;
                    projectId = response.Content.ReadAsStringAsync().Result;
                }
                catch (Exception exn)
                {
                    if (string.IsNullOrEmpty(projectId))
                    {
                        throw new InvalidOperationException("Unable to get project id.", exn);
                    }
                }
            }
            return builder.AddGoogleFluentdSink(new AspNetCoreGoogleFluentdLoggingContext(
                uri,
                projectId!,
                logId ?? Assembly.GetEntryAssembly()?.GetName()?.Name?.Replace(".", "-")?.ToLowerInvariant() ?? throw new InvalidOperationException("Unable to get log id."),
                serviceVersion
            ));
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