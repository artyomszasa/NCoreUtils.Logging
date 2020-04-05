using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Logging
{
    public static class LoggerBuilderSinkLoggingExtensions
    {
        public static ILoggingBuilder AddSink<TLoggerProvider, TSink>(this ILoggingBuilder builder)
            where TSink : class, ISink
            where TLoggerProvider : class, ISinkLoggerProvider<TSink>
        {
            builder.Services
                .AddSingleton<TSink>()
                .AddSingleton<ILoggerProvider, TLoggerProvider>();
            return builder;
        }


        public static ILoggingBuilder AddSink<TSink>(this ILoggingBuilder builder)
            where TSink : class, ISink
            => builder.AddSink<LoggerProvider<TSink>, TSink>();
    }
}