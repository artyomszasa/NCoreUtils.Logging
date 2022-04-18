using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NCoreUtils.Logging.FormattedString;

namespace NCoreUtils.Logging
{
    public static class LoggingBuilderFormattedStringExtensions
    {
        public static ILoggingBuilder AddFormattedString<TLoggerProvider>(this ILoggingBuilder builder, string nameOrUri)
            where TLoggerProvider : LoggerProvider
        {
            builder.Services.AddSingleton<ILoggerProvider>(serviceProvider =>
            {
                var output = serviceProvider.CreateByteSequenceOutput(nameOrUri);
                var payloadFactory = new FormattedStringPayloadFactory();
                var payloadWriter = new FormattedStringPayloadWriter(output);
                var sink = new FormattedStringSink(payloadWriter, payloadFactory);
                return ActivatorUtilities.CreateInstance<TLoggerProvider>(serviceProvider, sink);
            });
            return builder;
        }
    }
}