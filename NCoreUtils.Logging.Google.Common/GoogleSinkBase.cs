using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Logging.Type;
using Google.Cloud.Logging.V2;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Logging.Google
{
    public abstract class GoogleSinkBase
    {
        protected static Dictionary<LogLevel, LogSeverity> _level2severity = new Dictionary<LogLevel, LogSeverity>
        {
            { LogLevel.Trace,       LogSeverity.Debug },
            { LogLevel.Debug,       LogSeverity.Debug },
            { LogLevel.Information, LogSeverity.Info },
            { LogLevel.Warning,     LogSeverity.Warning },
            { LogLevel.Error,       LogSeverity.Error },
            { LogLevel.Critical,    LogSeverity.Critical }
        };

        protected static LogSeverity GetLogSeverity(LogLevel logLevel)
            => _level2severity.TryGetValue(logLevel, out var severity) ? severity : LogSeverity.Default;

        protected static bool TryAsRcpException(
            Exception exn,
            #if NETSTANDARD2_1
            [NotNullWhen(true)] out RpcException? rpcExn
            #else
            out RpcException rpcExn
            #endif
            )
        {
            var processed = new HashSet<Exception>(ByReferenceEqualityComparer<Exception>.Instance);
            Exception? e = exn;
            while (null != e)
            {
                if (e is RpcException r)
                {
                    rpcExn = r;
                    return true;
                }
                if (e is AggregateException ae && ae.InnerExceptions.Count == 1)
                {
                    e = ae.InnerExceptions[0];
                }
                else
                {
                    e = e.InnerException;
                }
            }
            #if NETSTANDARD2_1
            rpcExn = default;
            #else
            rpcExn = default!;
            #endif
            return false;
        }

        private LoggingServiceV2Client? _client;

        protected abstract bool IncludeCategory { get; }

        protected virtual ValueTask<LoggingServiceV2Client> GetClientAsync(CancellationToken cancellationToken)
        {
            if (_client is null)
            {
                return new ValueTask<LoggingServiceV2Client>(DoGetClientAsync());
            }
            return new ValueTask<LoggingServiceV2Client>(_client);

            async Task<LoggingServiceV2Client> DoGetClientAsync()
            {
                _client = await LoggingServiceV2Client.CreateAsync(cancellationToken).ConfigureAwait(false);
                return _client;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected string CreateTextPayload(Span<char> buffer, EventId eventId, string categoryName, string message, string? exception)
        {
            var builder = new SpanBuilder(buffer);
            if (IncludeEventId(eventId))
            {
                builder.Append('[');
                builder.Append(eventId.Id);
                builder.Append("] ");
            }
            if (IncludeCategory)
            {
                builder.Append('[');
                builder.Append(categoryName);
                builder.Append("] ");
            }
            builder.Append(message);
            if (!string.IsNullOrEmpty(exception))
            {
                builder.Append("\n");
                #if NETSTANDARD2_1
                builder.Append(exception);
                #else
                builder.Append(exception!);
                #endif
            }
            return builder.ToString();
        }

        protected abstract bool IncludeEventId(EventId eventId);
    }
}