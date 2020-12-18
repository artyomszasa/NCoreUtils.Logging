using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Google.Api;
using Google.Cloud.Logging.V2;
using Grpc.Core;
using Moq;

namespace NCoreUtils.Logging.Unit
{
    public class FakeGoogleLoggingClient
    {
        private class BooleanBox
        {
            public bool Value { get; set; }
        }

        public class WrapperException : Exception
        {
            public WrapperException(Exception innerException) : base("wrapper", innerException) { }
        }

        private static Task<T> ThrowRpcException<T>()
            => Task.Run<T>(new Func<T>(() => throw new WrapperException(new AggregateException(new RpcException(Status.DefaultCancelled)))));

        public static FakeGoogleLoggingClient Create()
        {
            var builder = new Mock<LoggingServiceV2Client>();
            var target = new List<Entry>();
            var shouldThrow = new BooleanBox();

            builder
                .Setup(client => client.WriteLogEntriesAsync(
                    It.IsAny<string>(),
                    It.IsAny<MonitoredResource>(),
                    It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<IEnumerable<LogEntry>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<string, MonitoredResource, IDictionary<string, string>, IEnumerable<LogEntry>, CancellationToken>((_, __, labels, entries, ___) =>
                {
                    if (shouldThrow.Value)
                    {
                        return ThrowRpcException<WriteLogEntriesResponse>();
                    }
                    foreach (var entry in entries)
                    {
                        if (string.IsNullOrEmpty(entry.TextPayload))
                        {
                            var message = entry.JsonPayload.Fields["message"].StringValue;
                            var context = entry.JsonPayload.Fields["context"].StructValue;
                            target.Add(new Entry(
                                labels: labels is null ? ImmutableDictionary<string, string>.Empty : labels.ToImmutableDictionary(),
                                timestamp: DateTimeOffset.Parse(entry.JsonPayload.Fields["eventTime"].StringValue),
                                message: message,
                                method: GetValueSafe("method"),
                                url: GetValueSafe("url"),
                                userAgent: GetValueSafe("userAgent"),
                                referrer: GetValueSafe("referer"),
                                remoteIp: GetValueSafe("remoteIp"),
                                user: GetValueSafe("user")
                            ));

                            string? GetValueSafe(string key)
                            {
                                try
                                {
                                    return context.Fields[key].StringValue!;
                                }
                                catch
                                {
                                    return default!;
                                }
                            }
                        }
                        else
                        {
                            target.Add(new Entry(
                                labels: labels is null ? ImmutableDictionary<string, string>.Empty : labels.ToImmutableDictionary(),
                                timestamp: entry.Timestamp.ToDateTimeOffset(),
                                message: entry.TextPayload,
                                method: default,
                                url: default,
                                userAgent: default,
                                referrer: default,
                                remoteIp: default,
                                user: default
                            ));
                        }
                    }
                    return Task.FromResult(new WriteLogEntriesResponse());
                });
            return new FakeGoogleLoggingClient(target, builder.Object, shouldThrow);
        }

        private readonly BooleanBox _shouldThrow;

        public IReadOnlyList<Entry> WrittenEntries { get; }

        public LoggingServiceV2Client Instance { get; }

        public bool ShouldThrow
        {
            get => _shouldThrow.Value;
            set => _shouldThrow.Value = value;
        }

        private FakeGoogleLoggingClient(IReadOnlyList<Entry> writtenEntries, LoggingServiceV2Client instance, BooleanBox shouldThrow)
        {
            WrittenEntries = writtenEntries;
            Instance = instance;
            _shouldThrow = shouldThrow;
        }
    }
}