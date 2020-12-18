using Google.Cloud.Logging.V2;

namespace NCoreUtils.Logging.Google
{
    public class GoogleClientSink : GenericBulkSink<LogEntry>
    {
        public GoogleClientSink(GoogleClientPayloadWriter payloadWriter, GoogleClientPayloadFactory payloadFactory)
            : base(payloadWriter, payloadFactory)
        { }
    }
}