using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace NCoreUtils.Logging
{
    internal static class ChannelReaderExtensions
    {

        private static async ValueTask DoReadAllAvailableWithinAsync<T>(
            this ChannelReader<T> reader,
            T[] buffer,
            RefBox<int> counter,
            CancellationToken cancellationToken)
        {
            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (buffer.Length > counter.Value && reader.TryRead(out T item))
                {
                    buffer[counter.Value] = item;
                    ++counter.Value;
                }
            }
        }

        public static async ValueTask<int> ReadAllAvailableWithinAsync<T>(
            this ChannelReader<T> reader,
            T[] buffer,
            int index,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            using var timeoutCancellation = new CancellationTokenSource(timeout);
            using var compoisteCancellation = CancellationTokenSource.CreateLinkedTokenSource(timeoutCancellation.Token, cancellationToken);
            var counter = new RefBox<int>(index);
            try
            {
                await reader.DoReadAllAvailableWithinAsync(buffer, counter, compoisteCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                if (!timeoutCancellation.IsCancellationRequested)
                {
                    throw;
                }
            }
            return counter.Value;
        }
    }
}