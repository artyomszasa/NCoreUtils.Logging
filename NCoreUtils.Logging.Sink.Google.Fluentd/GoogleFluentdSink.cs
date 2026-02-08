using NCoreUtils.Logging.Google.Data;

namespace NCoreUtils.Logging.Google;

public class GoogleFluentdSink(GoogleFluentdPayloadWriter payloadWriter, GoogleFluentdPayloadFactory payloadFactory)
    : GenericSink<LogEntry>(payloadWriter, payloadFactory)
{ }