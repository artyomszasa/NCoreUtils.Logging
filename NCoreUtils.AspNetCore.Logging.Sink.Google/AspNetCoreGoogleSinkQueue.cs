using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Logging.V2;

namespace NCoreUtils.Logging.Google
{
    public class AspNetCoreGoogleSinkQueue : IAspNetCoreSinkQueue
    {
        private readonly List<LogEntry> _entries = new List<LogEntry>(20);

        private readonly AspNetCoreGoogleSink _sink;

        public AspNetCoreGoogleSinkQueue(AspNetCoreGoogleSink sink)
            => _sink = sink ?? throw new ArgumentNullException(nameof(sink));

        public ValueTask DisposeAsync()
            => FlushAsync(CancellationToken.None);

        public void Enqueue<TState>(LogMessage<TState> message)
            => _entries.Add(_sink.CreateLogEntry(message));

        public void Enqueue<TState>(AspNetCoreLogMessage<TState> message)
            => _entries.Add(_sink.CreateLogEntry(message));

        public ValueTask FlushAsync(CancellationToken cancellationToken = default)
        {
            if (_entries.Count == 0)
            {
                return default;
            }
            var entries = _entries.ToArray();
            _entries.Clear();
            return _sink.SendAsync(entries, cancellationToken);
        }
    }
}