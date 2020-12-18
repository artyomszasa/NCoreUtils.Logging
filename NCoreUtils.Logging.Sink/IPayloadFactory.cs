using System;

namespace NCoreUtils.Logging
{
    public interface IPayloadFactory<TPayload> : IDisposable, IAsyncDisposable
    {
        TPayload CreatePayload<TState>(LogMessage<TState> message);
    }
}