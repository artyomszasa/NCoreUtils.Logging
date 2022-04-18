using System;

namespace NCoreUtils.Logging.Google.Internal
{
    public static class Fmt
    {
        public static string LogName(string projectId, string logId)
        {
            if (projectId.Length + logId.Length + 17 < 1024)
            {
                Span<char> buffer = stackalloc char[1024];
                var builder = new SpanBuilder(buffer);
                builder.Append("projects/");
                builder.Append(projectId);
                builder.Append("/logs/");
                builder.Append(logId);
                return new string(buffer[..builder.Length]);
            }
            return $"projects/{projectId}/logs/{logId}";
        }

        public static string Trace(string projectId, string traceId)
        {
            if (projectId.Length + traceId.Length + 19 < 1024)
            {
                Span<char> buffer = stackalloc char[1024];
                var builder = new SpanBuilder(buffer);
                builder.Append("projects/");
                builder.Append(projectId);
                builder.Append("/traces/");
                builder.Append(traceId);
                return new string(buffer[..builder.Length]);
            }
            return $"projects/{projectId}/traces/{traceId}";
        }
    }
}