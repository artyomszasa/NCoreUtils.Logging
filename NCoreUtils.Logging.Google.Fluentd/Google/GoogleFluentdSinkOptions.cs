using System;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.Logging.Google.Internal;

namespace NCoreUtils.Logging.Google
{
    /// <summary>
    /// Allows named google fluentd sink configuration. Used to implement usage of multiple google fluentd sinks.
    /// </summary>
    public class GoogleFluentdSinkOptions
    {
        private Type _payloadFactoryType = typeof(GoogleFluentdPayloadFactory);

        private Type _payloadWriterType = typeof(GoogleFluentdPayloadWriter);

        private Type _sinkType = typeof(GoogleFluentdSink);

        public IGoogleFluentdSinkConfiguration Configuration { get; internal set; } = default!;

        public GoogleFluentdPayloadWriter CreatePayloadWriter(IServiceProvider serviceProvider, params object[] parameters)
            => (GoogleFluentdPayloadWriter)ActivatorUtilities.CreateInstance(
                    serviceProvider.Override(Configuration),
                    _payloadWriterType,
                    parameters
                );

        public GoogleFluentdPayloadFactory CreatePayloadFactory(IServiceProvider serviceProvider, params object[] parameters)
            => (GoogleFluentdPayloadFactory)ActivatorUtilities.CreateInstance(
                    serviceProvider.Override(Configuration),
                    _payloadFactoryType,
                    parameters
                );

        public GoogleFluentdSink CreateSink(IServiceProvider serviceProvider, params object[] parameters)
            => (GoogleFluentdSink)ActivatorUtilities.CreateInstance(
                    serviceProvider.Override(Configuration),
                    _sinkType,
                    parameters
                );

        public GoogleFluentdSinkOptions OverridePayloadWriter(Type payloadWriterType)
        {
            if (!typeof(GoogleFluentdPayloadWriter).IsAssignableFrom(payloadWriterType))
            {
                throw new InvalidOperationException($"{payloadWriterType} cannot be used as {typeof(GoogleFluentdPayloadWriter)}.");
            }
            _payloadWriterType = payloadWriterType;
            return this;
        }

        public GoogleFluentdSinkOptions OverridePayloadWriter<TPayloadWriter>()
            where TPayloadWriter : GoogleFluentdPayloadWriter
            => OverridePayloadWriter(typeof(TPayloadWriter));

        public GoogleFluentdSinkOptions OverridePayloadFactory(Type payloadFactoryType)
        {
            if (!typeof(GoogleFluentdPayloadFactory).IsAssignableFrom(payloadFactoryType))
            {
                throw new InvalidOperationException($"{payloadFactoryType} cannot be used as {typeof(GoogleFluentdPayloadFactory)}.");
            }
            _payloadFactoryType = payloadFactoryType;
            return this;
        }

        public GoogleFluentdSinkOptions OverridePayloadFactory<TPayloadFactory>()
            where TPayloadFactory : GoogleFluentdPayloadFactory
            => OverridePayloadFactory(typeof(TPayloadFactory));

        public GoogleFluentdSinkOptions OverrideSink(Type sinkType)
        {
            if (!typeof(GoogleFluentdSink).IsAssignableFrom(sinkType))
            {
                throw new InvalidOperationException($"{sinkType} cannot be used as {typeof(GoogleFluentdSink)}.");
            }
            _sinkType = sinkType;
            return this;
        }

        public GoogleFluentdSinkOptions OverrideSink<TSink>()
            where TSink : GoogleFluentdSink
            => OverrideSink(typeof(TSink));
    }
}