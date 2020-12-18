using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Logging.Google.Internal;

namespace NCoreUtils.Logging.Google
{
    public class GoogleFluentdSinkConfiguration : IGoogleFluentdSinkConfiguration
    {
        public const int DefaultBufferSize = 32 * 1024;

        public const string StdOut = "file:///dev/stdout";

        public const string StdErr = "file:///dev/stderr";

        private string? _logName;

        private string _projectId = string.Empty;

        private string _service = string.Empty;

        public JsonSerializerOptions JsonSerializerOptions { get; set; } = new JsonSerializerOptions();

        public string ProjectId
        {
            get => _projectId;
            set
            {
                _projectId = value;
                _logName = default;
            }
        }

        public string Service
        {
            get => _service;
            set
            {
                _service = value;
                _logName = default;
            }
        }

        public string? ServiceVersion { get; set; }

        public string LogName
        {
            get
            {
                _logName ??= Fmt.LogName(ProjectId, Service);
                return _logName;
            }
        }

        public string Output { get; set; } = StdOut;

        public int BufferSize { get; set; } = DefaultBufferSize;

        public CategoryHandling CategoryHandling { get; set; }

        public EventIdHandling EventIdHandling { get; set; }

        public TraceHandling TraceHandling { get; set; }

        public ValueTask<Stream> CreateOutputStreamAsync(CancellationToken cancellationToken = default)
        {
            // special case -- stdout/stderr
            if (StdOut == Output)
            {
                return new ValueTask<Stream>(Console.OpenStandardOutput(BufferSize));
            }
            if (StdErr == Output)
            {
                return new ValueTask<Stream>(Console.OpenStandardError(BufferSize));
            }
            if (!Uri.TryCreate(Output, UriKind.Absolute, out var uri))
            {
                throw new InvalidOperationException($"Invalid fluentd output URI: \"{Output}\".");
            }
            if (uri.Scheme == "file")
            {
                return new ValueTask<Stream>(new FileStream(uri.AbsolutePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite, BufferSize, true));
            }
            if (uri.Scheme == "tcp")
            {
                if (!IPAddress.TryParse(uri.Host, out var ip))
                {
                    var hostEntry = Dns.GetHostEntry(uri.Host);
                    if (hostEntry is null || hostEntry.AddressList.Length == 0)
                    {
                        throw new InvalidOperationException($"Could not resolve \"{uri.Host}\".");
                    }
                    ip = hostEntry.AddressList[0];
                }
                return new ValueTask<Stream>(new BufferedStream(new TcpStream(new System.Net.IPEndPoint(ip, uri.Port)), BufferSize));
            }
            throw new NotSupportedException($"Not supported fluentd output URI \"{Output}\".");
        }
    }
}