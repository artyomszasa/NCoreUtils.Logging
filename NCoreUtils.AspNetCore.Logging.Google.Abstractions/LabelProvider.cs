using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Logging.Google
{
    public sealed class LabelProvider : ILabelProvider
    {
        public static LabelProvider Create(Action<string, EventId, LogLevel, AspNetCoreContext, IDictionary<string, string>> provider)
            => new LabelProvider(provider);

        public static LabelProvider Create(Action<AspNetCoreContext, IDictionary<string, string>> provider)
            => Create((_, __, ___, context, labels) => provider(context, labels));

        private readonly Action<string, EventId, LogLevel, AspNetCoreContext, IDictionary<string, string>> _delegate;

        private LabelProvider(Action<string, EventId, LogLevel, AspNetCoreContext, IDictionary<string, string>> @delegate)
            => _delegate = @delegate ?? throw new ArgumentNullException(nameof(@delegate));

        public void UpdateLabels(string category, EventId eventId, LogLevel logLevel, in AspNetCoreContext context, IDictionary<string, string> labels)
            => _delegate(category, eventId, logLevel, context, labels);
    }
}