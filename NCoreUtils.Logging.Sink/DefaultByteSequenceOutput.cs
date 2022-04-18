using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using NCoreUtils.Logging.DefaultOutputs;

namespace NCoreUtils.Logging
{
    public class DefaultByteSequenceOutput : IByteSequenceOutputFactory
    {
        public const string StdOut = "file:///dev/stdout";

        public const string StdErr = "file:///dev/stderr";

        public static IByteSequenceOutput Create(string uriOrName)
        {
            if (new DefaultByteSequenceOutput().TryCreate(uriOrName, out var output))
            {
                return output;
            }
            throw new NotSupportedException($"Not supported default by sequence output URI \"{uriOrName}\".");
        }

        public bool TryCreate(string uriOrName, [MaybeNullWhen(false)] out IByteSequenceOutput output)
        {
            // special case -- stdout/stderr
            if (StdOut == uriOrName || StringComparer.InvariantCultureIgnoreCase.Equals("stdout", uriOrName))
            {
                output = new StdOutOutput();
                return true;
            }
            if (StdErr == uriOrName || StringComparer.InvariantCultureIgnoreCase.Equals("stderr", uriOrName))
            {
                output = new StdErrOutput();
                return true;
            }
            // otherwise it is an uri
            if (!Uri.TryCreate(uriOrName, UriKind.Absolute, out var uri))
            {
                output = default;
                return false;
            }
            if (uri.Scheme == "file")
            {
                output = new FileOutput(uri.AbsolutePath);
                return true;
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
                output = new TcpOutput(new IPEndPoint(ip, uri.Port));
                return true;
            }
            output = default;
            return false;
        }
    }
}