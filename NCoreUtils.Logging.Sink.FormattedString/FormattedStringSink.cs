using NCoreUtils.Logging.FormattedString.Internal;

namespace NCoreUtils.Logging.FormattedString;

public class FormattedStringSink(
    FormattedStringPayloadWriter payloadWriter,
    FormattedStringPayloadFactory payloadFactory)
    : GenericSink<InMemoryByteSequence>(payloadWriter, payloadFactory)
{ }