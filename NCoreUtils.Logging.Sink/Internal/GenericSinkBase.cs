using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Logging
{
    public abstract class GenericSinkBase<TPayload, TWriter> : ISink
        where TWriter : class, IPayloadWriter<TPayload>
    {
        protected TWriter PayloadWriter { get; }

        protected IPayloadFactory<TPayload> PayloadFactory { get; }

        private int _isDisposed;

        protected bool IsDisposed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => 0 != Interlocked.CompareExchange(ref _isDisposed, 0, 0);
        }

        protected GenericSinkBase(TWriter payloadWriter, IPayloadFactory<TPayload> payloadFactory)
        {
            PayloadWriter = payloadWriter ?? throw new ArgumentNullException(nameof(payloadWriter));
            PayloadFactory = payloadFactory ?? throw new ArgumentNullException(nameof(payloadFactory));
        }

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            // NOTE: no unmanaged disposables
            if (0 == Interlocked.CompareExchange(ref _isDisposed, 1, 0) && disposing)
            {
                PayloadWriter.Dispose();
                PayloadFactory.Dispose();
            }
        }

        /// <summary>
        /// Only disposes managed resources.
        /// </summary>
        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (0 == Interlocked.CompareExchange(ref _isDisposed, 1, 0))
            {
                await PayloadWriter.DisposeAsync().ConfigureAwait(false);
                await PayloadFactory.DisposeAsync().ConfigureAwait(false);
            }
        }


        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore();
            Dispose(disposing: false);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Sink

        protected internal virtual TPayload CreatePayload<TState>(LogMessage<TState> message)
            => PayloadFactory.CreatePayload(message);

        protected virtual ValueTask WritePayloadAsync(TPayload payload, CancellationToken cancellationToken)
            => PayloadWriter.WritePayloadAsync(payload, cancellationToken);

        public ValueTask LogAsync<TState>(LogMessage<TState> message, CancellationToken cancellationToken = default)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }
            var payload = CreatePayload(message);
            return WritePayloadAsync(payload, cancellationToken);
        }

        #endregion
    }
}