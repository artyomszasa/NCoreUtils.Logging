using System;
using System.Net;
using NCoreUtils.Logging.DefaultOutputs;

namespace NCoreUtils.Logging
{
    public static class DefaultByteSequenceOutput
    {
        public const string StdOut = "file:///dev/stdout";

        public const string StdErr = "file:///dev/stderr";

        public static IByteSequenceOutput Create(string uriOrName)
        {
            // special case -- stdout/stderr
            if (StdOut == uriOrName || StringComparer.InvariantCultureIgnoreCase.Equals("stdout", uriOrName))
            {
                return new StdOutOutput();
            }
            if (StdErr == uriOrName || StringComparer.InvariantCultureIgnoreCase.Equals("stderr", uriOrName))
            {
                return new StdErrOutput();
            }
            // otherwise it is an uri
            if (!Uri.TryCreate(uriOrName, UriKind.Absolute, out var uri))
            {
                throw new InvalidOperationException($"Invalid default by sequence output URI: \"{uriOrName}\".");
            }
            if (uri.Scheme == "file")
            {
                return new FileOutput(uri.AbsolutePath);
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
                return new TcpOutput(new IPEndPoint(ip, uri.Port));
            }
            throw new NotSupportedException($"Not supported default by sequence output URI \"{uriOrName}\".");
        }
    }
}