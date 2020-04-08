using System;
using System.IO;
using System.Net;

namespace NCoreUtils.Logging.Google.Fluentd
{
    public static class FluentdSink
    {
        private static FileStream OpenFile(string path)
            => new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite, 16 * 1024, FileOptions.Asynchronous);

        public static IFluentdSink Create(Uri uri)
        {
            if (uri.AbsoluteUri == "file:///dev/stdout")
            {
                return new TextWriterFluentdSink(Console.Out, true);
            }
            if (uri.Scheme == "file")
            {
                return new StreamFluentdSink(OpenFile(uri.AbsolutePath));
            }
            if (uri.Scheme == "tcp")
            {
                return new TcpFluentdSink(new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port));
            }
            throw new InvalidOperationException($"Unsupported fluentd target: \"{uri}\".");
        }
    }
}