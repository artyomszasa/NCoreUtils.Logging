using NCoreUtils.Logging.FormattedString.Internal;

namespace NCoreUtils.Logging.FormattedString
{
    public class FormattedStringSink : GenericSink<InMemoryByteSequence>
    {
        public FormattedStringSink(
            FormattedStringPayloadWriter payloadWriter,
            FormattedStringPayloadFactory payloadFactory)
            : base(payloadWriter, payloadFactory)
        { }
    }
}