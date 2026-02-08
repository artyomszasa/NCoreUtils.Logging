using Google.Cloud.Logging.V2;

namespace NCoreUtils.Logging.Google;

public class GoogleClientSink(GoogleClientPayloadWriter payloadWriter, GoogleClientPayloadFactory payloadFactory)
    : GenericBulkSink<LogEntry>(payloadWriter, payloadFactory)
{
}