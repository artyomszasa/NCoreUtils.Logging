using NCoreUtils.Logging.Google.Data;

namespace NCoreUtils.Logging.Google
{
    public class GoogleFluentdSink : GenericSink<LogEntry>
    {
        public GoogleFluentdSink(GoogleFluentdPayloadWriter payloadWriter, GoogleFluentdPayloadFactory payloadFactory)
            : base(payloadWriter, payloadFactory)
        { }
    }
}