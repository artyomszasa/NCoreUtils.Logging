namespace NCoreUtils.Logging;

public class GenericSink<TPayload>(IPayloadWriter<TPayload> payloadWriter, IPayloadFactory<TPayload> payloadFactory)
    : Internal.GenericSinkBase<TPayload, IPayloadWriter<TPayload>>(payloadWriter, payloadFactory)
{ }