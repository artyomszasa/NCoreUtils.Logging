using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Logging.V2;
using Grpc.Core;

namespace NCoreUtils.Logging.Google
{
    public class GoogleClientPayloadWriter : IBulkPayloadWriter<LogEntry>
    {
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

        protected IGoogleClientSinkConfiguration Configuration { get; }

        public GoogleClientPayloadWriter(IGoogleClientSinkConfiguration configuration, LoggingServiceV2Client? client = default)
        {
            _client = client;
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        [ExcludeFromCodeCoverage]
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

        public void Dispose() { }

        public ValueTask DisposeAsync()
            => default;

        public ValueTask WritePayloadAsync(LogEntry payload, CancellationToken cancellationToken = default)
            => WritePayloadsAsync(new [] { payload }, cancellationToken);

        public async ValueTask WritePayloadsAsync(IEnumerable<LogEntry> payloads, CancellationToken cancellationToken = default)
        {
            try
            {
                var client = await GetClientAsync(cancellationToken);
                await client.WriteLogEntriesAsync(Configuration.LogName, Configuration.Resource, null, payloads, cancellationToken);
            }
            catch (Exception exn) when (TryAsRcpException(exn, out var rpcExn))
            {
                Console.Error.WriteLine($"Unable to write log entries: {rpcExn.Message}.");
                Console.Error.WriteLine(rpcExn);
            }
        }
    }
}