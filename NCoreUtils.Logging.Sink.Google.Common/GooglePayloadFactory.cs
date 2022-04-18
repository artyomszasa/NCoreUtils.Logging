using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Logging.Google
{
    public abstract class GooglePayloadFactory<TPayload, TConfiguration> : IPayloadFactory<TPayload>
        where TConfiguration : class, IGoogleSinkConfiguration
    {
        private const int MaxCharStackAllocSize = 8 * 1024;

        // TODO: optimize
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetStringifiedLength(int value)
        {
            var isSigned = value < 0 ? 1 : 0;
            return (int)Math.Floor(Math.Log10(Math.Abs(value))) + 1 + isSigned;
        }

        protected TConfiguration Configuration { get; }

        protected IEnumerable<ILabelProvider> LabelProviders { get; }

        protected GooglePayloadFactory(TConfiguration configuration, IEnumerable<ILabelProvider> labelProviders)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            LabelProviders = labelProviders ?? throw new ArgumentNullException(nameof(labelProviders));
        }

        public abstract TPayload CreatePayload<TState>(LogMessage<TState> message);

        /// <summary>
        /// Computes buffer size required to text payload for the specified parameters.
        /// </summary>
        /// <param name="options">Formatting options.</param>
        /// <param name="eventId">Event ID.</param>
        /// <param name="categoryName">Category name.</param>
        /// <param name="message">Message.</param>
        /// <param name="exception">Optional exception.</param>
        /// <returns>Size in characters required to store payload.</returns>
        protected int ComputeTextPayloadSize(IGoogleSinkConfiguration options, EventId eventId, string categoryName, string message, string? exception)
        {
            var total = 0;
            if (options.EventIdHandling == EventIdHandling.IncludeAlways || (options.EventIdHandling == EventIdHandling.IncludeValidIds && eventId.Id != -1 && eventId != 0))
            {
                total += 3 + GetStringifiedLength(eventId.Id);
            }
            if (options.CategoryHandling == CategoryHandling.IncludeInMessage && !string.IsNullOrEmpty(categoryName))
            {
                total += 3 + categoryName.Length;
            }
            total += message.Length;
            if (!string.IsNullOrEmpty(exception))
            {
                total += Environment.NewLine.Length + exception.Length;
            }
            return total;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected string CreateTextPayload(Span<char> buffer, IGoogleSinkConfiguration options, EventId eventId, string categoryName, string message, string? exception)
        {
            var builder = new SpanBuilder(buffer);
            if (options.EventIdHandling == EventIdHandling.IncludeAlways || (options.EventIdHandling == EventIdHandling.IncludeValidIds && eventId.Id != -1 && eventId != 0))
            {
                builder.Append('[');
                builder.Append(eventId.Id);
                builder.Append("] ");
            }
            if (options.CategoryHandling == CategoryHandling.IncludeInMessage)
            {
                builder.Append('[');
                builder.Append(categoryName);
                builder.Append("] ");
            }
            builder.Append(message);
            if (!string.IsNullOrEmpty(exception))
            {
                builder.Append(Environment.NewLine);
                builder.Append(exception);
            }
            return builder.ToString();
        }

        protected string CreateTextPayload(IGoogleSinkConfiguration options, EventId eventId, string categoryName, string message, string? exception)
        {
            var payloadSize = ComputeTextPayloadSize(options, eventId, categoryName, message, exception);
            string payload;
            if (payloadSize <= MaxCharStackAllocSize)
            {
                Span<char> buffer = stackalloc char[payloadSize];
                payload = CreateTextPayload(buffer, options, eventId, categoryName, message, exception);
            }
            else
            {
                var buffer = ArrayPool<char>.Shared.Rent(payloadSize);
                payload = CreateTextPayload(buffer.AsSpan(), options, eventId, categoryName, message, exception);
                ArrayPool<char>.Shared.Return(buffer);
            }
            return payload;
        }

        #region disposable

        protected virtual void Dispose(bool disposing) { /* noop */ }

        protected virtual ValueTask DisposeAsyncCore() => default;

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore();
            Dispose(disposing: false);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}