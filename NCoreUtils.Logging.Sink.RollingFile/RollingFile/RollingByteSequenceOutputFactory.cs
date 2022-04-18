using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace NCoreUtils.Logging.RollingFile
{
    public class RollingByteSequenceOutputFactory : IByteSequenceOutputFactory
    {
        public static class QueryKeys
        {
            public const string Roll = "roll";

            public const string Triggers = "triggers";

            public const string MaxSize = "max-size";

            public const string Compress = "compress";
        }

        private static Regex QueryParameterRegex { get; } = new Regex(@"[?&](\w[\w.]*)(=([^?&]+))?", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static IReadOnlyDictionary<string, string?> EmptyQuery { get; } = new Dictionary<string, string?>();

        private static bool IsTruthy(string? value) => value switch
        {
            null => true,
            "true" => true,
            "1" => true,
            "on" => true,
            _ => false
        };

        private static IReadOnlyDictionary<string, string?> ParseQueryString(Uri uri)
        {
            if (string.IsNullOrEmpty(uri.Query))
            {
                return EmptyQuery;
            }
            var match = QueryParameterRegex.Match(uri.Query);
            var parameters = new Dictionary<string, string?>(StringComparer.InvariantCultureIgnoreCase);
            while (match.Success)
            {
                parameters.Add(match.Groups[1].Value, match.Groups[2].Success ? match.Groups[3].Value : default);
                match = match.NextMatch();
            }
            return parameters;
        }

        protected virtual IFileRollerOptions ReadOptions(IReadOnlyDictionary<string, string?> raw)
        {
            var options = new DefaultFileRollerOptions();
            if (raw.TryGetValue(QueryKeys.Triggers, out var rawTriggers)
                && rawTriggers is not null
                && Enum.TryParse<FileRollTrigger>(rawTriggers, true, out var triggers))
            {
                options.Triggers = triggers;
            }
            if (raw.TryGetValue(QueryKeys.MaxSize, out var rawMaxSize)
                && rawMaxSize is not null
                && long.TryParse(rawMaxSize, NumberStyles.Integer, CultureInfo.InvariantCulture, out var maxSize))
            {
                options.MaxFileSize = maxSize;
            }
            if (raw.TryGetValue(QueryKeys.Compress, out var compress))
            {
                options.CompressRolled = IsTruthy(compress);
            }
            return options;
        }

        protected virtual IFileRoller CreateRoller(IFileRollerOptions options)
            => new DefaultFileRoller(options);

        public bool TryCreate(string uriOrName, [MaybeNullWhen(false)] out IByteSequenceOutput output)
        {
            if (Uri.TryCreate(uriOrName, UriKind.Absolute, out var uri))
            {
                var query = ParseQueryString(uri);
                if (query.TryGetValue(QueryKeys.Roll, out var isEnabled) && IsTruthy(isEnabled))
                {
                    // parse arguments
                    var options = ReadOptions(query);
                    // create roller
                    var roller = CreateRoller(options);
                    // create output
                    output = new RollingByteSequenceOutput(roller, uri.LocalPath);
                    return true;
                }
            }
            output = default;
            return false;
        }
    }
}