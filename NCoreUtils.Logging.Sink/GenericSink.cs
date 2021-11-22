namespace NCoreUtils.Logging
{
    public class GenericSink<TPayload> : Internal.GenericSinkBase<TPayload, IPayloadWriter<TPayload>>
    {
        public GenericSink(IPayloadWriter<TPayload> payloadWriter, IPayloadFactory<TPayload> payloadFactory)
            : base(payloadWriter, payloadFactory)
        { }
    }
}